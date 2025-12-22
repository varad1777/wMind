using Application.DTOs;
using Application.Interface;
using Domain.Entities;
using Infrastructure.DBs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Service
{
    public class TelemetryOptions
    {
        public string RabbitHost { get; set; } = "localhost";
        public string RabbitUser { get; set; } = "guest";
        public string RabbitPass { get; set; } = "guest";
        public string Queue { get; set; } = "telemetry_queue";
        public string ReportRequestQueue { get; set; }
        public ushort Prefetch { get; set; } = 200;
    }

    public class TelemetryBackgroundService : BackgroundService
    {
        private readonly ILogger<TelemetryBackgroundService> _logger;
        private readonly IMappingCache _mappingCache;
        private readonly TelemetryOptions _options;
        private readonly IServiceProvider _serviceProvider;

        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly IAlertStateStore _alertStore;


        public TelemetryBackgroundService(
            ILogger<TelemetryBackgroundService> logger,
            IMappingCache mappingCache,
            IOptions<TelemetryOptions> options,
            IServiceProvider serviceProvider,
            IAlertStateStore alertStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mappingCache = mappingCache ?? throw new ArgumentNullException(nameof(mappingCache));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _alertStore = alertStore ?? throw new ArgumentNullException(nameof(alertStore));

        }

        private object BuildNotificationPayload(
            string assetName, string signalName, double value, double min, double max)
        {
            string statusType;
            double percent;

            if (value < min)
            {
                percent = ((min - value) / (min == 0 ? 1 : min)) * 100;
                statusType = "LOW";
            }
            else if (value > max)
            {
                percent = ((value - max) / (max == 0 ? 1 : max)) * 100;
                statusType = "HIGH";
            }
            else
            {
                return null;
            }

            return new
            {
                asset = assetName,
                signal = signalName,
                value = value,
                min = min,
                max = max,
                status = statusType,
                percent = Math.Round(percent, 1),
                timestamp = DateTime.UtcNow.ToString("o")  // ISO format
            };
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TelemetryBackgroundService waiting for InfluxDB to initialize...");
            // small startup delay to allow dependent services to come up
            await Task.Delay(3000, cancellationToken);

            var factory = new ConnectionFactory
            {
                HostName = _options.RabbitHost,
                UserName = _options.RabbitUser,
                Password = _options.RabbitPass,
                AutomaticRecoveryEnabled = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.BasicQos(0, _options.Prefetch, false);

            _consumer = new EventingBasicConsumer(_channel);
            // wire up the async consumer handler
            _consumer.Received += async (sender, ea) => await OnReceivedAsync(ea);
            _channel.BasicConsume(queue: _options.Queue, autoAck: false, consumer: _consumer);

            _logger.LogInformation("TelemetryBackgroundService started consuming queue {Queue}", _options.Queue);
            await base.StartAsync(cancellationToken);
        }

        private async Task OnReceivedAsync(BasicDeliverEventArgs ea)
        {
            await Task.Yield(); // ensure we don't block Rabbit thread

            try
            {
                var body = ea.Body.ToArray();

                TelemetryDto dto;
                try
                {
                    // Try to deserialize directly from bytes (works on modern System.Text.Json)
                    dto = JsonSerializer.Deserialize<TelemetryDto>(body, _jsonOptions);
                    if (dto == null) throw new Exception("Telemetry DTO deserialized to null.");
                }
                catch (Exception ex)
                {
                    // fallback: try string-deserialize for robustness
                    try
                    {
                        var jsonString = Encoding.UTF8.GetString(body).Trim('\0', '\r', '\n', ' ');
                        dto = JsonSerializer.Deserialize<TelemetryDto>(jsonString, _jsonOptions);
                        if (dto == null) throw new Exception("Telemetry DTO deserialized to null from string fallback.");
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogWarning(innerEx, "Failed to deserialize telemetry message; acking to drop");
                        try { _channel.BasicAck(ea.DeliveryTag, false); } catch { }
                        return;
                    }
                }

                if (dto == null)
                {
                    try { _channel.BasicAck(ea.DeliveryTag, false); } catch { }
                    return;
                }



                // Try both possible TryGet signatures (defensive)
                bool mapped = false;
                MappingInfo mapping = null;
                try
                {
                    mapped = _mappingCache.TryGet(dto.DeviceId, dto.deviceSlaveId, dto.RegisterAddress, out mapping);
                }
                catch
                {
                    // logger
                    Console.WriteLine("MappingCache TryGet failed with 4-parameter overload, trying 3-parameter overload.");
                }


                if (!mapped || mapping == null)
                {
                    _logger.LogDebug("Telemetry unmapped for Device:{Device} Port:{Port} Reg:{Reg}", dto.DeviceId, dto.deviceSlaveId, dto.RegisterAddress);
                    try { _channel.BasicAck(ea.DeliveryTag, false); } catch { }
                    return;
                }

                // Build DTO for InfluxDB
                var influxDto = new InfluxTelementryDto
                {
                    AssetId = mapping.AssetId,
                    DeviceId = dto.DeviceId,
                    deviceSlaveId = dto.deviceSlaveId,
                    SignalTypeId = mapping.SignalTypeId,
                    MappingId = mapping.MappingId,
                    RegisterAddress = dto.RegisterAddress,
                    SignalType = mapping.SignalName,
                    Value = dto.Value,
                    Unit = mapping.SignalUnit ?? dto.Unit,
                    Timestamp = dto.TimestampUtc
                };
                Console.WriteLine($"value from rabbitmq  { dto.Value}");

                // write telemetry and handle alert/notification flow in a scope
                using var scope = _serviceProvider.CreateScope();
                var influxService = scope.ServiceProvider.GetRequiredService<IInfluxTelementryService>();
                var _alertRepo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();

                await influxService.WriteTelemetryAsync(influxDto);

                var assetService = scope.ServiceProvider.GetRequiredService<IAssetHierarchyService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // Get asset name
                var assetName = await assetService.GetAssetNameAsync(mapping.AssetId) ?? "Unknown Asset";

                // Get signal thresholds
                var signal = await assetService.GetSignalTypeAsync(mapping.SignalTypeId);

                if (signal != null)
                {
                    // compare register addresses - support both typo and correct property names
                    bool registerMatches = false;
                    try
                    {
                        // many codebases sometimes use DefaultRegisterAdress (misspelt) - check both
                        var defaultAddrProp = signal.GetType().GetProperty("DefaultRegisterAdress");
                        if (defaultAddrProp != null)
                        {
                            var val = defaultAddrProp.GetValue(signal);
                            if (val != null && int.TryParse(val.ToString(), out var r) && r == influxDto.RegisterAddress) registerMatches = true;
                        }
                    }
                    catch { /* ignore reflection errors */ }

                    try
                    {
                        var defaultAddrProp2 = signal.GetType().GetProperty("DefaultRegisterAddress");
                        if (!registerMatches && defaultAddrProp2 != null)
                        {
                            var val = defaultAddrProp2.GetValue(signal);
                            if (val != null && int.TryParse(val.ToString(), out var r) && r == influxDto.RegisterAddress) registerMatches = true;
                        }
                    }
                    catch { /* ignore reflection errors */ }

                    // fallback: if signal has an int property named DefaultRegisterAdress/Address not accessible via reflection above,
                    // attempt a safe direct comparison if those properties exist at compile time in your DTO
                    // (you can remove the reflection if you are sure of the property name).
                    if (!registerMatches)
                    {
                        // attempt direct access via common names (works if property exists)
                        try
                        {
                            dynamic dsig = signal;
                            int? defaultAddr = null;
                            try { defaultAddr = (int?)dsig.DefaultRegisterAdress; } catch { }
                            try { if (defaultAddr == null) defaultAddr = (int?)dsig.DefaultRegisterAddress; } catch { }

                            if (defaultAddr.HasValue && defaultAddr.Value == influxDto.RegisterAddress) registerMatches = true;
                        }
                        catch { /* ignore dynamic failures */ }
                    }

                    // if register doesn't match, skip alert checks
                    if (registerMatches)
                    {
                        var now = DateTime.UtcNow;
                        var mappingKey = mapping.MappingId; // unique key for alert store

                        bool isOutOfRange = influxDto.Value < signal.MinThreshold || influxDto.Value > signal.MaxThreshold;
                        var activeAlert = await _alertRepo.GetActiveAsync(mapping.MappingId);
                        Console.WriteLine($"Value: {influxDto.Value}, MinThreshold: {signal.MinThreshold} signal name: {signal.SignalName}");
                        Console.WriteLine(isOutOfRange);

                        if (isOutOfRange)
                        {

                            if (activeAlert == null)
                            {
                                Console.WriteLine("in active akert");
                                var alert = new Alert
                                {
                                    AlertId = Guid.NewGuid(),

                                    AssetId = mapping.AssetId,
                                    AssetName = assetName,

                                    SignalTypeId = mapping.SignalTypeId,
                                    SignalName = signal.SignalName,

                                    MappingId = mapping.MappingId,

                                    AlertStartUtc = now,
                                    IsActive = true,
                                    IsAnalyzed = false,

                                    MinThreshold = signal.MinThreshold,
                                    MaxThreshold = signal.MaxThreshold,

                                    MinObservedValue = influxDto.Value,
                                    MaxObservedValue = influxDto.Value,

                                    ReminderTimeHours = 24,

                                    CreatedUtc = now,
                                    UpdatedUtc = now
                                };

                                await _alertRepo.CreateAsync(alert);
                            }
                            else
                            {
                                Console.WriteLine("updating alert");
                                await _alertRepo.UpdateStatsAsync(activeAlert.AlertId, influxDto.Value);
                            }
                            // check if there is already an active alert
                            var current = await _alertStore.GetAsync(mappingKey);
                            if (current == null || !current.IsActive)
                            {
                                // start alert
                                await _alertStore.SetActiveAsync(mappingKey, now, influxDto.Value);

                                var startPayload = BuildNotificationPayload(
                                    assetName,
                                    signal.SignalName,
                                    influxDto.Value,
                                    signal.MinThreshold,
                                    signal.MaxThreshold
                                );

                                var notificationRequest = new NotificationCreateRequest(
                                    Title: $"Alert START: {signal.SignalName} exceeded",
                                    Text: JsonSerializer.Serialize(startPayload),
                                    ExpiresAt: null,
                                    Priority: 0
                                );

                                await notificationService.CreateForUsersAsync(notificationRequest);
                                _logger.LogInformation("Sent START notification for {Asset} {Signal}", assetName, signal.SignalName);
                            }
                            else
                            {
                                // already active — update stats only
                                await _alertStore.UpdateActiveAsync(mappingKey, influxDto.Value, now);
                            }
                        }
                        else
                        {

                            if (activeAlert != null)
                            {
                                await _alertRepo.ResolveAsync(activeAlert.AlertId, now);
                            }
                            // value back to normal — if active then clear and send resolved notification
                            var active = await _alertStore.GetAsync(mappingKey);
                            if (active != null && active.IsActive)
                            {
                                var saved = await _alertStore.ClearActiveAsync(mappingKey, now);

                                if (saved != null)
                                {
                                    var duration = now - saved.StartUtc;
                                    var resolvedPayload = new
                                    {
                                        asset = assetName,
                                        signal = signal.SignalName,
                                        from = saved.StartUtc.ToString("o"),
                                        to = now.ToString("o"),
                                        durationSeconds = (int)duration.TotalSeconds,
                                        min = saved.MinValue,
                                        max = saved.MaxValue
                                    };

                                    var notificationRequest = new NotificationCreateRequest(
                                        Title: $"Alert RESOLVED: {signal.SignalName} normalised",
                                        Text: JsonSerializer.Serialize(resolvedPayload),
                                        ExpiresAt: null,
                                        Priority: 0
                                    );

                                    await notificationService.CreateForUsersAsync(notificationRequest);
                                    _logger.LogInformation("Sent RESOLVED notification for {Asset} {Signal}", assetName, signal.SignalName);
                                }
                            }
                        }
                    }
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry message; acking to avoid poison queue");
                try { _channel.BasicAck(ea.DeliveryTag, false); } catch { }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try { _channel?.Close(); _channel?.Dispose(); } catch { }
            try { _connection?.Close(); _connection?.Dispose(); } catch { }
            _logger.LogInformation("TelemetryBackgroundService stopped.");
            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }
}
