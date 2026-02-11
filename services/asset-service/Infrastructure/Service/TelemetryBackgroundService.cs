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

    // DTO matching the new format being published by ModbusPollerHostedService
    public class SignalTelemetryDto
    {
        public Guid SignalId { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class TelemetryBackgroundService : BackgroundService
    {
        private readonly ILogger<TelemetryBackgroundService> _logger;
        private readonly TelemetryOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAlertStateStore _alertStore;

        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public TelemetryBackgroundService(
            ILogger<TelemetryBackgroundService> logger,
            IOptions<TelemetryOptions> options,
            IServiceProvider serviceProvider,
            IAlertStateStore alertStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TelemetryBackgroundService waiting for initialization...");
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
            _consumer.Received += async (sender, ea) => await OnReceivedAsync(ea);
            _channel.BasicConsume(queue: _options.Queue, autoAck: false, consumer: _consumer);

            _logger.LogInformation("✅ TelemetryBackgroundService started consuming queue {Queue}", _options.Queue);
            await base.StartAsync(cancellationToken);
        }

        private async Task OnReceivedAsync(BasicDeliverEventArgs ea)
        {
            await Task.Yield(); // Don't block RabbitMQ thread

            try
            {
                var body = ea.Body.ToArray();
                SignalTelemetryDto dto;

                try
                {
                    dto = JsonSerializer.Deserialize<SignalTelemetryDto>(body, _jsonOptions);
                    if (dto == null) throw new Exception("Telemetry DTO deserialized to null.");
                }
                catch (Exception ex)
                {
                    try
                    {
                        var jsonString = Encoding.UTF8.GetString(body).Trim('\0', '\r', '\n', ' ');
                        dto = JsonSerializer.Deserialize<SignalTelemetryDto>(jsonString, _jsonOptions);
                        if (dto == null) throw new Exception("Telemetry DTO deserialized to null from string fallback.");
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogWarning(innerEx, "❌ Failed to deserialize telemetry message; acking to drop");
                        try { _channel.BasicAck(ea.DeliveryTag, false); } catch { }
                        return;
                    }
                }

                if (dto == null || dto.SignalId == Guid.Empty)
                {
                    _logger.LogDebug("⚠️  Invalid telemetry DTO - empty SignalId");
                    try { _channel.BasicAck(ea.DeliveryTag, false); } catch { }
                    return;
                }

                // ═══════════════════════════════════════════════════════
                // STEP 1: Lookup Signal metadata from Signals table
                // ═══════════════════════════════════════════════════════
                using var scope = _serviceProvider.CreateScope();
                var assetService = scope.ServiceProvider.GetRequiredService<IAssetHierarchyService>();
                
                // Retrieve signal details (contains AssetId, DeviceId, SignalTypeId)
                var signal = await assetService.GetSignalByIdAsync(dto.SignalId);
                
                if (signal == null)
                {
                    _logger.LogDebug("⚠️  Signal {SignalId} not found in database - dropping message", dto.SignalId);
                    try { _channel.BasicAck(ea.DeliveryTag, false); } catch { }
                    return;
                }

                Console.WriteLine($"📥 Received → SignalId: {dto.SignalId}, Value: {dto.Value}, Time: {dto.Timestamp:HH:mm:ss}");

                // ═══════════════════════════════════════════════════════
                // STEP 2: Build InfluxDB DTO (now simplified with SignalId)
                // ═══════════════════════════════════════════════════════
                var influxDto = new InfluxTelementryDto
                {
                    SignalId = dto.SignalId,
                    AssetId = signal.AssetId,
                    DeviceId = signal.DeviceId,
                    SignalTypeId = signal.SignalTypeId,
                    SignalType = signal.SignalName,
                    Value = dto.Value,
                    Unit = signal.Unit,
                    Timestamp = dto.Timestamp
                };

                Console.WriteLine($"💾 Writing to InfluxDB → Signal: {signal.SignalName}, Value: {dto.Value}");

                // ═══════════════════════════════════════════════════════
                // STEP 3: Write to InfluxDB
                // ═══════════════════════════════════════════════════════
                var influxService = scope.ServiceProvider.GetRequiredService<IInfluxTelementryService>();
                await influxService.WriteTelemetryAsync(influxDto);

                // ═══════════════════════════════════════════════════════
                // STEP 4: Alert Detection and Notification
                // ═══════════════════════════════════════════════════════
                var _alertRepo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // Get asset name
                var assetName = await assetService.GetAssetNameAsync(signal.AssetId) ?? "Unknown Asset";

                // Get signal type thresholds
                var signalType = await assetService.GetSignalTypeAsync(signal.SignalTypeId);

                if (signalType != null)
                {
                    var now = DateTime.UtcNow;
                    bool isOutOfRange = dto.Value < signalType.MinThreshold || dto.Value > signalType.MaxThreshold;
                    
                    // Use SignalId as the unique key for alert tracking
                    var alertKey = dto.SignalId;
                    var activeAlert = await _alertRepo.GetActiveBySignalAsync(dto.SignalId);

                    Console.WriteLine($"🔍 Alert Check → Value: {dto.Value}, Min: {signalType.MinThreshold}, Max: {signalType.MaxThreshold}, OutOfRange: {isOutOfRange}");

                    if (isOutOfRange)
                    {
                        // ═══════════════════════════════════════════════════════
                        // ALERT START / UPDATE
                        // ═══════════════════════════════════════════════════════
                        if (activeAlert == null)
                        {
                            Console.WriteLine("🚨 Starting NEW alert");
                            
                            var alert = new Alert
                            {
                                AlertId = Guid.NewGuid(),
                                AssetId = signal.AssetId,
                                AssetName = assetName,
                                SignalTypeId = signal.SignalTypeId,
                                SignalName = signal.SignalName,
                                SignalId = dto.SignalId,  // NEW: Track which signal triggered this
                                AlertStartUtc = now,
                                IsActive = true,
                                IsAnalyzed = false,
                                MinThreshold = signalType.MinThreshold,
                                MaxThreshold = signalType.MaxThreshold,
                                MinObservedValue = dto.Value,
                                MaxObservedValue = dto.Value,
                                ReminderTimeHours = 24,
                                CreatedUtc = now,
                                UpdatedUtc = now
                            };

                            await _alertRepo.CreateAsync(alert);

                            // Send START notification
                            var startPayload = BuildNotificationPayload(
                                assetName,
                                signal.SignalName,
                                dto.Value,
                                signalType.MinThreshold,
                                signalType.MaxThreshold
                            );

                            if (startPayload != null)
                            {
                                var notificationRequest = new NotificationCreateRequest(
                                    Title: $"🚨 Alert START: {signal.SignalName} exceeded",
                                    Text: JsonSerializer.Serialize(startPayload),
                                    ExpiresAt: null,
                                    Priority: 0
                                );

                                await notificationService.CreateForUsersAsync(notificationRequest);
                                _logger.LogInformation("✅ Sent START notification for {Asset} / {Signal}", assetName, signal.SignalName);
                            }

                            // Update in-memory alert state
                            await _alertStore.SetActiveAsync(alertKey, now, dto.Value);
                        }
                        else
                        {
                            Console.WriteLine("🔄 Updating existing alert");
                            
                            // Update database alert stats
                            await _alertRepo.UpdateStatsAsync(activeAlert.AlertId, dto.Value);
                            
                            // Update in-memory alert state
                            await _alertStore.UpdateActiveAsync(alertKey, dto.Value, now);
                        }
                    }
                    else
                    {
                        // ═══════════════════════════════════════════════════════
                        // ALERT RESOLVED
                        // ═══════════════════════════════════════════════════════
                        if (activeAlert != null)
                        {
                            Console.WriteLine("✅ Resolving alert");
                            
                            await _alertRepo.ResolveAsync(activeAlert.AlertId, now);

                            // Clear in-memory state and send notification
                            var saved = await _alertStore.ClearActiveAsync(alertKey, now);

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
                                    Title: $"✅ Alert RESOLVED: {signal.SignalName} normalized",
                                    Text: JsonSerializer.Serialize(resolvedPayload),
                                    ExpiresAt: null,
                                    Priority: 0
                                );

                                await notificationService.CreateForUsersAsync(notificationRequest);
                                _logger.LogInformation("✅ Sent RESOLVED notification for {Asset} / {Signal}", assetName, signal.SignalName);
                            }
                        }
                    }
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing telemetry message; acking to avoid poison queue");
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