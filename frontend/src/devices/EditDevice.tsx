import { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { ArrowLeft, Save } from "lucide-react";
import {
  getDeviceById,
  updateDevice,
  DeviceProtocol,
  OpcUaConnectionMode,
  type DeviceConfiguration,
} from "@/api/deviceApi";
import { Settings2, Cpu, AlertTriangle } from "lucide-react";
import { toast } from "react-toastify";

/* -------------------- TYPES -------------------- */
type EndianType = "Little" | "Big";
type OpcUaConnectionModeType = 1 | 2; // Numeric enum

interface ProtocolSettings {
  ipAddress?: string;
  port?: number;
  slaveId?: number;
  endian?: EndianType;
  connectionString?: string;
  connectionMode?: OpcUaConnectionModeType;
}

interface FormData {
  configName: string;
  pollInterval: number | undefined;
  protocolSettings: ProtocolSettings;
}

const DEVICE_PROTOCOL_LABEL: Record<DeviceProtocol, string> = {
  [DeviceProtocol.Modbus]: "Modbus",
  [DeviceProtocol.OpcUa]: "OPC UA",
};

/* -------------------- COMPONENT -------------------- */
export default function EditDeviceForm() {
  const navigate = useNavigate();
  const { deviceId } = useParams<{ deviceId: string }>();

  const [deviceDetails, setDeviceDetails] = useState<{
    name: string;
    description: string;
    protocol: DeviceProtocol;
  }>({
    name: "",
    description: "",
    protocol: DeviceProtocol.Modbus,
  });

  // Initialize with empty state - will be populated from API
  const [formData, setFormData] = useState<FormData>({
    configName: "",
    pollInterval: undefined,
    protocolSettings: {},
  });

  const [loading, setLoading] = useState(false);
  const [isLoadingDevice, setIsLoadingDevice] = useState(true);

  /* -------------------- FETCH DEVICE -------------------- */
  useEffect(() => {
    if (!deviceId) return;

    const fetchDevice = async () => {
      setIsLoadingDevice(true);
      try {
        const res = await getDeviceById(deviceId);

        console.log("Fetched device:", res);
        console.log("Device protocol:", res.protocol);
        console.log("Configuration:", res.deviceConfiguration);

        const config = res.deviceConfiguration;

        // ⚠️ CRITICAL FIX: Use configuration protocol if it exists, otherwise use device protocol
        const actualProtocol = config?.protocol ?? res.protocol;

        console.log("Using protocol:", actualProtocol, 
          actualProtocol === DeviceProtocol.Modbus ? "(Modbus)" : "(OPC UA)");

        setDeviceDetails({
          name: res.name,
          description: res.description ?? "",
          protocol: actualProtocol, // Use the actual protocol from config
        });

        // Only populate form if configuration exists
        if (config) {
          console.log("Configuration from DB:", config);

          const isModbus = actualProtocol === DeviceProtocol.Modbus;
          const isOpcUa = actualProtocol === DeviceProtocol.OpcUa;

          setFormData({
            configName: config.name || `${res.name}_config`,
            pollInterval: config.pollIntervalMs ?? undefined,
            protocolSettings: {
              // Modbus-specific fields
              ...(isModbus && {
                ipAddress: config.ipAddress || "127.0.0.1",
                port: config.port ?? 502,
                slaveId: config.slaveId !== undefined ? config.slaveId : 1,
                endian: config.endian || "Little",
              }),
              // OPC UA-specific fields
              ...(isOpcUa && {
                connectionString: config.connectionString || "",
                connectionMode:
                  config.connectionMode !== undefined
                    ? config.connectionMode
                    : 1,
              }),
            },
          });
        } else {
          // No configuration exists, set protocol-specific defaults
          const isModbus = actualProtocol === DeviceProtocol.Modbus;
          const isOpcUa = actualProtocol === DeviceProtocol.OpcUa;

          setFormData({
            configName: `${res.name}_config`,
            pollInterval: isModbus ? 1000 : undefined,
            protocolSettings: {
              // Modbus defaults
              ...(isModbus && {
                ipAddress: "127.0.0.1",
                port: 502,
                slaveId: 1,
                endian: "Little" as EndianType,
              }),
              // OPC UA defaults
              ...(isOpcUa && {
                connectionString: "",
                connectionMode: 1 as OpcUaConnectionModeType,
              }),
            },
          });
        }
      } catch (err: any) {
        console.error("Error fetching device:", err);
        toast.error("Failed to load device details");
        navigate("/devices");
      } finally {
        setIsLoadingDevice(false);
      }
    };

    fetchDevice();
  }, [deviceId, navigate]);

  /* -------------------- VALIDATION -------------------- */
  const validateForm = () => {
    const { name, description, protocol } = deviceDetails;
    const { configName, pollInterval, protocolSettings } = formData;

    if (!name.trim() || name.trim().length < 3) {
      toast.error("Device name must be at least 3 characters");
      return false;
    }

    if (description && description.length > 255) {
      toast.error("Description cannot exceed 255 characters");
      return false;
    }

    if (!configName.trim()) {
      toast.error("Configuration name is required");
      return false;
    }

    // Only validate poll interval if it's required for the current protocol/mode
    const isPollIntervalRequired =
      protocol === DeviceProtocol.Modbus ||
      (protocol === DeviceProtocol.OpcUa &&
        protocolSettings.connectionMode === 1); // Polling

    if (isPollIntervalRequired) {
      if (
        pollInterval === undefined ||
        pollInterval < 100 ||
        pollInterval > 300000
      ) {
        toast.error("Poll interval must be between 100-300000 ms");
        return false;
      }
    }

    if (
      protocol === DeviceProtocol.OpcUa &&
      !protocolSettings.connectionString
    ) {
      toast.error("Connection string is required for OPC UA");
      return false;
    }

    if (protocol === DeviceProtocol.Modbus) {
      if (!protocolSettings.ipAddress) {
        toast.error("IP Address is required for Modbus");
        return false;
      }
      if (
        !protocolSettings.port ||
        protocolSettings.port <= 0 ||
        protocolSettings.port > 65535
      ) {
        toast.error("Invalid port for Modbus");
        return false;
      }
      if (
        protocolSettings.slaveId === undefined ||
        protocolSettings.slaveId < 0 ||
        protocolSettings.slaveId > 247
      ) {
        toast.error("Invalid slave ID for Modbus");
        return false;
      }
    }

    return true;
  };

  /* -------------------- HANDLERS -------------------- */
  const handleDeviceChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setDeviceDetails((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleConfigChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]: name === "pollInterval" ? Number(value) : value,
    }));
  };

  const handleProtocolSettingsChange = (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      protocolSettings: {
        ...prev.protocolSettings,
        [name]: name === "port" || name === "slaveId" ? Number(value) : value,
      },
    }));
  };

  const handleEndianChange = (value: EndianType) => {
    setFormData((prev) => ({
      ...prev,
      protocolSettings: {
        ...prev.protocolSettings,
        endian: value,
      },
    }));
  };

  const handleConnectionModeChange = (value: string) => {
    const newMode = value === "Polling" ? 1 : 2;
    
    setFormData((prev) => ({
      ...prev,
      protocolSettings: {
        ...prev.protocolSettings,
        connectionMode: newMode as OpcUaConnectionModeType,
      },
      // Set default poll interval when switching to Polling mode
      pollInterval: newMode === 1 && prev.pollInterval === undefined ? 1000 : prev.pollInterval,
    }));
  };

  /* -------------------- SUBMIT -------------------- */
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!deviceId) return;
    if (!validateForm()) return;

    setLoading(true);

    try {
      // Device DTO
      const dto = {
        name: deviceDetails.name.trim(),
        description: deviceDetails.description.trim(),
        protocol: deviceDetails.protocol,
      };

      // Determine if poll interval should be included
      const shouldIncludePollInterval =
        deviceDetails.protocol === DeviceProtocol.Modbus ||
        (deviceDetails.protocol === DeviceProtocol.OpcUa &&
          formData.protocolSettings.connectionMode === 1); // Polling

      // Configuration DTO
      const configDto: DeviceConfiguration = {
        name: formData.configName.trim(),
        protocol: deviceDetails.protocol,
        pollIntervalMs: shouldIncludePollInterval
          ? formData.pollInterval
          : undefined,
        // Modbus fields
        ...(deviceDetails.protocol === DeviceProtocol.Modbus && {
          ipAddress: formData.protocolSettings.ipAddress,
          port: formData.protocolSettings.port,
          slaveId: formData.protocolSettings.slaveId,
          endian: formData.protocolSettings.endian,
        }),
        // OPC UA fields
        ...(deviceDetails.protocol === DeviceProtocol.OpcUa && {
          connectionString: formData.protocolSettings.connectionString,
          connectionMode: formData.protocolSettings.connectionMode,
        }),
      };

      console.log("Submitting update:", { dto, configDto });

      await updateDevice(deviceId, dto, configDto);

      toast.success("Device updated successfully!");
      setTimeout(() => navigate("/devices"), 1000);
    } catch (err: any) {
      toast.error(err?.response?.data?.message ?? "Failed to update device");
    } finally {
      setLoading(false);
    }
  };

  /* -------------------- LOADING STATE -------------------- */
  if (isLoadingDevice) {
    return (
      <div className="flex justify-center items-center min-h-[85vh] bg-gradient-to-b from-background to-muted/30">
        <Card className="w-full max-w-2xl shadow-lg border border-border/60 bg-card/90 backdrop-blur-sm">
          <CardContent className="pt-6">
            <div className="flex flex-col items-center justify-center py-12">
              <Settings2 className="h-12 w-12 text-primary animate-pulse mb-4" />
              <p className="text-muted-foreground">Loading device details...</p>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  /* -------------------- JSX -------------------- */
  return (
    <div className="flex justify-center items-center min-h-[85vh] bg-gradient-to-b from-background to-muted/30 text-foreground p-4">
      <Card className="w-full max-w-2xl shadow-lg border border-border/60 bg-card/90 backdrop-blur-sm">
        <CardHeader className="flex flex-col items-center space-y-2 pb-2">
          <Settings2 className="h-7 w-7 text-primary" />
          <CardTitle className="text-2xl font-semibold tracking-tight">
            Edit Device & Configuration
          </CardTitle>
          <p className="text-sm text-muted-foreground">
            Update device details and configuration parameters
          </p>
        </CardHeader>

        <CardContent className="pt-4">
          <form onSubmit={handleSubmit} className="space-y-8">
            {/* DEVICE DETAILS */}
            <div className="rounded-xl border border-border/70 bg-muted/30 p-5 shadow-inner">
              <div className="flex items-center gap-2 mb-4">
                <Cpu className="h-5 w-5 text-primary" />
                <h2 className="text-lg font-semibold">Device Details</h2>
              </div>

              <div className="grid gap-4">
                <div className="grid gap-2">
                  <Label>Device Name *</Label>
                  <Input
                    name="name"
                    value={deviceDetails.name}
                    onChange={handleDeviceChange}
                    placeholder="Device Name"
                  />
                </div>

                <div className="grid gap-2">
                  <Label>Description</Label>
                  <Input
                    name="description"
                    value={deviceDetails.description}
                    onChange={handleDeviceChange}
                    placeholder="Description"
                  />
                </div>

                <div className="grid gap-2">
                  <Label>Protocol</Label>
                  <Input
                    value={DEVICE_PROTOCOL_LABEL[deviceDetails.protocol]}
                    disabled
                  />
                </div>
              </div>
            </div>

            {/* CONFIGURATION DETAILS */}
            <div className="rounded-xl border border-border/70 bg-muted/30 p-5 shadow-inner">
              <div className="flex items-center gap-2 mb-4">
                <Settings2 className="h-5 w-5 text-primary" />
                <h2 className="text-lg font-semibold">Configuration Details</h2>
              </div>

              <div className="grid gap-4">
                <div className="grid gap-2">
                  <Label>Configuration Name *</Label>
                  <Input
                    name="configName"
                    value={formData.configName}
                    onChange={handleConfigChange}
                  />
                </div>

                {/* OPC UA Connection Mode */}
                {deviceDetails.protocol === DeviceProtocol.OpcUa && (
                  <div className="grid gap-2">
                    <Label>Connection Mode</Label>
                    <Select
                      value={
                        formData.protocolSettings.connectionMode === 1
                          ? "Polling"
                          : "PubSub"
                      }
                      onValueChange={handleConnectionModeChange}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Connection Mode" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Polling">Polling</SelectItem>
                        <SelectItem value="PubSub">PubSub</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                )}

                {/* Poll Interval */}
                {(deviceDetails.protocol === DeviceProtocol.Modbus ||
                  (deviceDetails.protocol === DeviceProtocol.OpcUa &&
                    formData.protocolSettings.connectionMode === 1)) && (
                  <div className="grid gap-2">
                    <Label>Poll Interval (ms)</Label>
                    <Input
                      name="pollInterval"
                      type="number"
                      value={formData.pollInterval ?? ""}
                      onChange={handleConfigChange}
                    />
                  </div>
                )}

                {/* MODBUS */}
                {deviceDetails.protocol === DeviceProtocol.Modbus && (
                  <div className="grid grid-cols-2 gap-4">
                    <div className="grid gap-2">
                      <Label>IP Address</Label>
                      <Input
                        name="ipAddress"
                        value={formData.protocolSettings.ipAddress ?? ""}
                        onChange={handleProtocolSettingsChange}
                      />
                    </div>

                    <div className="grid gap-2">
                      <Label>Port</Label>
                      <Input
                        name="port"
                        type="number"
                        value={formData.protocolSettings.port ?? ""}
                        onChange={handleProtocolSettingsChange}
                      />
                    </div>

                    <div className="grid gap-2">
                      <Label>Slave ID</Label>
                      <Input
                        name="slaveId"
                        type="number"
                        value={formData.protocolSettings.slaveId ?? ""}
                        onChange={handleProtocolSettingsChange}
                      />
                    </div>

                    <div className="grid gap-2">
                      <Label>Endian</Label>
                      <Select
                        value={formData.protocolSettings.endian ?? "Little"}
                        onValueChange={handleEndianChange}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Endian" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Little">Little</SelectItem>
                          <SelectItem value="Big">Big</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                )}

                {/* OPC UA */}
                {deviceDetails.protocol === DeviceProtocol.OpcUa && (
                  <div className="grid gap-2">
                    <Label>Connection String</Label>
                    <Input
                      name="connectionString"
                      value={formData.protocolSettings.connectionString ?? ""}
                      onChange={handleProtocolSettingsChange}
                    />
                  </div>
                )}
              </div>
            </div>

            {/* FOOTER */}
            <div className="flex justify-between items-center pt-4 border-t border-border/40">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate("/devices")}
              >
                <ArrowLeft className="h-4 w-4 mr-2" />
                Back to Devices
              </Button>

              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" />
                {loading ? "Saving..." : "Save Changes"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}