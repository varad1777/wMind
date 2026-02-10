import { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardHeader, CardContent, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Settings2, ArrowLeft } from "lucide-react";
import {
  getDeviceById,
  updateDevice,
  DeviceProtocol,
  OpcUaConnectionMode,
} from "@/api/deviceApi";
import { toast } from "react-toastify";

/* ===========================
   TYPES
=========================== */
type DeviceProtocolType =
  (typeof DeviceProtocol)[keyof typeof DeviceProtocol];

type OpcUaConnectionModeType =
  (typeof OpcUaConnectionMode)[keyof typeof OpcUaConnectionMode];

type DeviceDetailsState = {
  name: string;
  description: string;
  gatewayClientId: string;
  protocol: DeviceProtocolType;
};

type ConfigFormState = {
  configName: string;
  pollInterval: number;

  // Modbus
  ipAddress: string;
  port: number;
  slaveId: number;
  endian: "Little" | "Big";

  // OPC UA
  connectionString: string;
  connectionMode: OpcUaConnectionModeType;
};

/* ===========================
   COMPONENT
=========================== */
export default function ConfigureDevice() {
  const navigate = useNavigate();
  const { deviceId } = useParams<{ deviceId: string }>();
  const [loading, setLoading] = useState(false);

  /* ===========================
     DEVICE STATE
  ============================ */
  const [deviceDetails, setDeviceDetails] = useState<DeviceDetailsState>({
    name: "",
    description: "",
    gatewayClientId: "",
    protocol: DeviceProtocol.Modbus,
  });

  /* ===========================
     CONFIG STATE
  ============================ */
  const [formData, setFormData] = useState<ConfigFormState>({
    configName: "",
    pollInterval: 1000,

    ipAddress: "127.0.0.1",
    port: 502,
    slaveId: 1,
    endian: "Little",

    connectionString: "",
    connectionMode: 1, // Polling
  });

  /* ===========================
     FETCH DEVICE
  ============================ */
  useEffect(() => {
    if (!deviceId) return;

    const fetchDevice = async () => {
      try {
        const res = await getDeviceById(deviceId);

        setDeviceDetails({
          name: res.name ?? "",
          description: res.description ?? "",
          gatewayClientId: res.gatewayId ?? "",
          protocol: res.protocol,
        });

        setFormData({
          configName: res.deviceConfiguration?.name ?? `${res.name}_config`,
          pollInterval: res.deviceConfiguration?.pollIntervalMs ?? 1000,

          ipAddress: res.deviceConfiguration?.ipAddress ?? "127.0.0.1",
          port: res.deviceConfiguration?.port ?? 502,
          slaveId: res.deviceConfiguration?.slaveId ?? 1,
          endian: res.deviceConfiguration?.endian ?? "Little",

          connectionString: res.deviceConfiguration?.connectionString ?? "",
          connectionMode: res.deviceConfiguration?.connectionMode ?? 1, // Default to Polling
        });
      } catch {
        toast.error("Failed to load device");
        navigate("/devices");
      }
    };

    fetchDevice();
  }, [deviceId, navigate]);

  /* ===========================
     HANDLERS
  ============================ */
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]:
        name === "pollInterval" || name === "port" || name === "slaveId"
          ? Number(value)
          : value,
    }));
  };

  /* ===========================
     VALIDATION
  ============================ */
  const validateForm = () => {
    if (!formData.configName.trim()) {
      toast.error("Configuration name is required");
      return false;
    }

    // Only validate poll interval if it's relevant for the current protocol/mode
    const isPollIntervalRequired =
      deviceDetails.protocol === DeviceProtocol.Modbus ||
      (deviceDetails.protocol === DeviceProtocol.OpcUa &&
        formData.connectionMode === OpcUaConnectionMode.Polling);

    if (isPollIntervalRequired) {
      if (formData.pollInterval < 100 || formData.pollInterval > 300000) {
        toast.error("Poll interval must be 100–300000 ms");
        return false;
      }
    }

    if (deviceDetails.protocol === DeviceProtocol.Modbus) {
      const ipRegex =
        /^(25[0-5]|2[0-4]\d|1?\d{1,2})(\.(25[0-5]|2[0-4]\d|1?\d{1,2})){3}$/;

      if (!ipRegex.test(formData.ipAddress)) {
        toast.error("Invalid IP address");
        return false;
      }

      if (formData.port < 1 || formData.port > 65535) {
        toast.error("Port must be 1–65535");
        return false;
      }

      if (formData.slaveId < 1 || formData.slaveId > 247) {
        toast.error("Slave ID must be 1–247");
        return false;
      }
    }

    if (deviceDetails.protocol === DeviceProtocol.OpcUa) {
      if (!formData.connectionString.trim()) {
        toast.error("Connection string is required for OPC UA");
        return false;
      }
    }

    return true;
  };

  /* ===========================
     SUBMIT
  ============================ */
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!deviceId || !validateForm()) return;

    setLoading(true);

    try {
      // Only include pollInterval if it's relevant
      const shouldIncludePollInterval =
        deviceDetails.protocol === DeviceProtocol.Modbus ||
        (deviceDetails.protocol === DeviceProtocol.OpcUa &&
          formData.connectionMode === OpcUaConnectionMode.Polling);

      await updateDevice(
        deviceId,
        {
          name: deviceDetails.name,
          description: deviceDetails.description,
          protocol: deviceDetails.protocol,
        },
        {
          name: formData.configName,
          protocol: deviceDetails.protocol,
          pollIntervalMs: shouldIncludePollInterval
            ? formData.pollInterval
            : undefined,

          ipAddress:
            deviceDetails.protocol === DeviceProtocol.Modbus
              ? formData.ipAddress
              : undefined,

          port:
            deviceDetails.protocol === DeviceProtocol.Modbus
              ? formData.port
              : undefined,

          slaveId:
            deviceDetails.protocol === DeviceProtocol.Modbus
              ? formData.slaveId
              : undefined,

          endian:
            deviceDetails.protocol === DeviceProtocol.Modbus
              ? formData.endian
              : undefined,

          connectionString:
            deviceDetails.protocol === DeviceProtocol.OpcUa
              ? formData.connectionString
              : undefined,

          connectionMode:
            deviceDetails.protocol === DeviceProtocol.OpcUa
              ? formData.connectionMode
              : undefined,
        }
      );

      toast.success("Device configured successfully");
      navigate("/devices");
    } catch (err: any) {
      toast.error(err?.response?.data?.message ?? "Failed to update device");
    } finally {
      setLoading(false);
    }
  };

  /* ===========================
     UI
  ============================ */
  return (
    <div className="flex justify-center items-center min-h-[80vh] p-4">
      <Card className="w-full max-w-2xl">
        <CardHeader className="flex flex-row items-center gap-2">
          <Settings2 className="h-5 w-5" />
          <CardTitle>Configure Device</CardTitle>
        </CardHeader>

        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <Label>Configuration Name</Label>
              <Input
                name="configName"
                value={formData.configName}
                onChange={handleChange}
              />
            </div>

            <div>
              <Label>Protocol</Label>
              <select
                className="w-full border rounded px-3 py-2"
                value={deviceDetails.protocol}
                onChange={(e) =>
                  setDeviceDetails((p) => ({
                    ...p,
                    protocol: Number(e.target.value) as DeviceProtocolType,
                  }))
                }
              >
                <option value={DeviceProtocol.Modbus}>Modbus</option>
                <option value={DeviceProtocol.OpcUa}>OPC UA</option>
              </select>
            </div>

            {/* Show OPC UA Connection Mode first if OPC UA is selected */}
            {deviceDetails.protocol === DeviceProtocol.OpcUa && (
              <div>
                <Label>Connection Mode</Label>
                <select
                  className="w-full border rounded px-3 py-2"
                  value={formData.connectionMode}
                  onChange={(e) =>
                    setFormData((p) => ({
                      ...p,
                      connectionMode: Number(
                        e.target.value
                      ) as OpcUaConnectionModeType,
                    }))
                  }
                >
                  <option value={OpcUaConnectionMode.Polling}>Polling</option>
                  <option value={OpcUaConnectionMode.PubSub}>PubSub</option>
                </select>
              </div>
            )}

            {/* Show poll interval for Modbus OR OPC UA Polling mode only */}
            {(deviceDetails.protocol === DeviceProtocol.Modbus ||
              (deviceDetails.protocol === DeviceProtocol.OpcUa &&
                formData.connectionMode === OpcUaConnectionMode.Polling)) && (
              <div>
                <Label>Poll Interval (ms)</Label>
                <Input
                  name="pollInterval"
                  type="number"
                  value={formData.pollInterval}
                  onChange={handleChange}
                />
              </div>
            )}

            <hr />

            <p className="text-sm font-semibold">Protocol Settings</p>

            {deviceDetails.protocol === DeviceProtocol.Modbus && (
              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <Label>IP Address</Label>
                  <Input
                    name="ipAddress"
                    value={formData.ipAddress}
                    onChange={handleChange}
                  />
                </div>

                <div>
                  <Label>Port</Label>
                  <Input
                    name="port"
                    type="number"
                    value={formData.port}
                    onChange={handleChange}
                  />
                </div>

                <div>
                  <Label>Slave ID</Label>
                  <Input
                    name="slaveId"
                    type="number"
                    value={formData.slaveId}
                    onChange={handleChange}
                  />
                </div>
              </div>
            )}

            {deviceDetails.protocol === DeviceProtocol.OpcUa && (
              <div className="space-y-4">
                <div>
                  <Label>Connection String</Label>
                  <Input
                    name="connectionString"
                    value={formData.connectionString}
                    onChange={handleChange}
                  />
                </div>
              </div>
            )}

            <div className="flex justify-between pt-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate("/devices")}
              >
                <ArrowLeft className="h-4 w-4 mr-2" />
                Back
              </Button>

              <Button type="submit" disabled={loading}>
                {loading ? "Saving..." : "Save"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}