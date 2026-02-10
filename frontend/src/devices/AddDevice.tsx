import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import {
  createDevice,
  DeviceProtocol,
  type CreateDevicePayload,
  type DeviceConfiguration,
} from "@/api/deviceApi";
import { getGateways, type Gateway } from "@/api/GatewayApi";
import { toast } from "react-toastify";

/* ----------------------
   STATE TYPES
---------------------- */
type EndianType = "Little" | "Big";
type OpcUaConnectionModeType = "Polling" | "PubSub";

interface FormData {
  name: string;
  description: string;
  gatewayClientId: string;
  protocol: DeviceProtocol; // numeric enum 1 or 2
  pollInterval: number;
  // Modbus
  ipAddress?: string;
  port?: number;
  slaveId?: number;
  endian?: EndianType;
  // OPC UA
  connectionString?: string;
  connectionMode?: OpcUaConnectionModeType;
}

/* ----------------------
   COMPONENT
---------------------- */
export default function AddDeviceForm() {
  const navigate = useNavigate();

  const [formData, setFormData] = useState<FormData>({
    name: "",
    description: "",
    gatewayClientId: "",
    protocol: DeviceProtocol.Modbus,
    pollInterval: 1000,
    ipAddress: "127.0.0.1",
    port: 502,
    slaveId: 1,
    endian: "Little",
    connectionString: "change your connection string",
    connectionMode: "Polling",
  });

  const [gateways, setGateways] = useState<Gateway[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingGateways, setLoadingGateways] = useState(true);

  /* ----------------------
     FETCH GATEWAYS
  ---------------------- */
  useEffect(() => {
    const fetchGateways = async () => {
      try {
        const data = await getGateways();
        setGateways(data);
      } catch {
        toast.error("Failed to load gateways");
      } finally {
        setLoadingGateways(false);
      }
    };
    fetchGateways();
  }, []);

  /* ----------------------
     VALIDATION
  ---------------------- */
  const validateForm = () => {
    if (!formData.name.trim()) {
      toast.error("Device name is required.");
      return false;
    }
    if (!formData.gatewayClientId) {
      toast.error("Gateway selection is required.");
      return false;
    }
    if (!formData.protocol) {
      toast.error("Protocol selection is required.");
      return false;
    }

    if (formData.description && formData.description.length > 255) {
      toast.error("Description cannot exceed 255 characters.");
      return false;
    }

    // Protocol-specific validation
    if (formData.protocol === DeviceProtocol.Modbus) {
      if (!formData.ipAddress) {
        toast.error("IP Address is required for Modbus.");
        return false;
      }
      if (!formData.port || formData.port < 1 || formData.port > 65535) {
        toast.error("Port must be 1–65535 for Modbus.");
        return false;
      }
      if (
        formData.slaveId === undefined ||
        formData.slaveId < 0 ||
        formData.slaveId > 247
      ) {
        toast.error("Slave ID must be 0–247 for Modbus.");
        return false;
      }
    } else if (formData.protocol === DeviceProtocol.OpcUa) {
      if (!formData.connectionString?.trim()) {
        toast.error("Connection string is required for OPC UA.");
        return false;
      }
      if (!formData.connectionMode) {
        toast.error("Connection mode is required for OPC UA.");
        return false;
      }
    }

    return true;
  };

  /* ----------------------
     HANDLERS
  ---------------------- */
  const handleChange = (
    e: React.ChangeEvent<
      HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
    >,
  ) => {
    const { name, value } = e.target;

    if (name === "protocol") {
      setFormData((prev) => ({
        ...prev,
        protocol:
          value === "Modbus" ? DeviceProtocol.Modbus : DeviceProtocol.OpcUa,
      }));
    } else {
      setFormData((prev) => ({ ...prev, [name]: value }));
    }
  };

  /* ----------------------
     SUBMIT
  ---------------------- */
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setLoading(true);

    try {
      const payload: CreateDevicePayload = {
        name: formData.name.trim(),
        gatewayClientId: formData.gatewayClientId,
        protocol: formData.protocol,
        description: formData.description?.trim() || undefined,
        configuration: {
          name: formData.name.trim(),
          protocol: formData.protocol,
          pollIntervalMs: formData.pollInterval,
        } as any,
      };

      // Modbus
      if (formData.protocol === DeviceProtocol.Modbus) {
        payload.configuration!.ipAddress = formData.ipAddress;
        payload.configuration!.port = formData.port;
        payload.configuration!.slaveId = formData.slaveId;
        payload.configuration!.endian = formData.endian;
      }

      // OPC UA
      if (formData.protocol === DeviceProtocol.OpcUa) {
        payload.configuration!.connectionString = formData.connectionString;
        payload.configuration!.connectionMode =
          formData.connectionMode === "Polling" ? 1 : 2; // 1 = Polling, 2 = PubSub
        if (formData.connectionMode === "Polling") {
          payload.configuration!.pollIntervalMs = formData.pollInterval;
        }
      }

      await createDevice(payload);

      toast.success("Device created successfully!");
      navigate("/devices");
    } catch (err: any) {
      toast.error(
        err?.response?.data?.errors
          ? JSON.stringify(err.response.data.errors)
          : "Failed to create device",
      );
    } finally {
      setLoading(false);
    }
  };

  /* ----------------------
     RENDER
  ---------------------- */
  return (
    <div className="flex justify-center items-center min-h-[80vh]">
      <Card className="w-full max-w-xl">
        <CardHeader>
          <CardTitle>Add New Device</CardTitle>
        </CardHeader>

        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-5">
            {/* Device Name */}
            <div className="grid gap-2">
              <Label htmlFor="name">Device Name *</Label>
              <Input
                id="name"
                name="name"
                value={formData.name}
                onChange={handleChange}
                required
              />
            </div>

            {/* Description */}
            <div className="grid gap-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                name="description"
                value={formData.description}
                onChange={handleChange}
              />
            </div>

            {/* Protocol */}
            <div className="grid gap-2">
              <Label htmlFor="protocol">Protocol *</Label>
              <select
                id="protocol"
                name="protocol"
                value={
                  formData.protocol === DeviceProtocol.Modbus
                    ? "Modbus"
                    : "OPCUA"
                }
                onChange={handleChange}
                className="border rounded-md p-2"
                required
              >
                <option value="Modbus">Modbus</option>
                <option value="OPCUA">OPCUA</option>
              </select>
            </div>

            {/* Gateway */}
            <div className="grid gap-2">
              <Label htmlFor="gatewayClientId">Gateway *</Label>
              <select
                id="gatewayClientId"
                name="gatewayClientId"
                value={formData.gatewayClientId}
                onChange={handleChange}
                disabled={loadingGateways}
                className="border rounded-md p-2"
                required
              >
                <option value="">
                  {loadingGateways ? "Loading gateways..." : "Select Gateway"}
                </option>
                {gateways.map((g) => (
                  <option key={g.clientId} value={g.clientId}>
                    {g.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Buttons */}
            <div className="flex justify-end gap-3 pt-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate("/devices")}
              >
                Cancel
              </Button>

              <Button type="submit" disabled={loading}>
                {loading ? "Saving..." : "Save Device"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
