import { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Settings2, Cpu, Save, ArrowLeft } from "lucide-react";
import { getDeviceById, updateDevice } from "@/api/deviceApi";
import { toast } from "react-toastify";

/* -------------------- TYPES -------------------- */

type EndianType = "Little" | "Big";

interface ProtocolSettings {
  IpAddress: string;
  Port: number;
  SlaveId: number;
  Endian: EndianType;
}

interface FormData {
  configName: string;
  pollInterval: number;
  protocolSettings: ProtocolSettings;
}

/* -------------------- COMPONENT -------------------- */

export default function EditDeviceForm() {
  const navigate = useNavigate();
  const { deviceId } = useParams<{ deviceId: string }>();

  const [deviceDetails, setDeviceDetails] = useState({
    name: "",
    description: "",
    protocol: "ModbusTCP",
  });

  const [formData, setFormData] = useState<FormData>({
    configName: "",
    pollInterval: 1000,
    protocolSettings: {
      IpAddress: "127.0.0.1",
      Port: 5020,
      SlaveId: 1,
      Endian: "Little",
    },
  });

  const [loading, setLoading] = useState(false);

  /* -------------------- FETCH DEVICE -------------------- */

  useEffect(() => {
    if (!deviceId) return;

    const fetchDevice = async () => {
      try {
        const res = await getDeviceById(deviceId);

        setDeviceDetails({
          name: res.name ?? "",
          description: res.description ?? "",
          protocol: res.protocol ?? "ModbusTCP",
        });

        const parsedSettings: ProtocolSettings =
          res.deviceConfiguration?.protocolSettingsJson
            ? JSON.parse(res.deviceConfiguration.protocolSettingsJson)
            : {
                IpAddress: "127.0.0.1",
                Port: 5020,
                SlaveId: 1,
                Endian: "Little",
              };

        setFormData({
          configName:
            res.deviceConfiguration?.name ?? `${res.name}_config`,
          pollInterval:
            res.deviceConfiguration?.pollIntervalMs ?? 1000,
          protocolSettings: parsedSettings,
        });
      } catch (err: any) {
        if (err.response?.status === 401) {
          toast.error("Unauthorized! Please login again.");
          navigate("/login");
        } else if (err.response?.status === 404) {
          toast.error("Device not found!");
          navigate("/devices");
        } else {
          toast.error("Error fetching device details.");
        }
      }
    };

    fetchDevice();
  }, [deviceId, navigate]);

  /* -------------------- VALIDATION -------------------- */

  const validateForm = () => {
    const { name, description } = deviceDetails;
    const { configName, pollInterval, protocolSettings } = formData;
    const { IpAddress, Port, SlaveId } = protocolSettings;

    if (!/^[A-Za-z][A-Za-z0-9_\- ]{2,99}$/.test(name.trim())) {
      toast.error("Invalid device name.");
      return false;
    }

    if (description && description.length > 255) {
      toast.error("Description must be under 255 characters.");
      return false;
    }

    if (!configName.trim()) {
      toast.error("Configuration name is required.");
      return false;
    }

    if (pollInterval < 100 || pollInterval > 300000) {
      toast.error("Poll interval must be between 100–300000 ms.");
      return false;
    }

    const ipRegex =
      /^(localhost|((25[0-5]|2[0-4]\d|1?\d{1,2})(\.(25[0-5]|2[0-4]\d|1?\d{1,2})){3}))$/;

    if (!ipRegex.test(IpAddress)) {
      toast.error("Invalid IP address.");
      return false;
    }

    if (Port < 1 || Port > 65535) {
      toast.error("Invalid port.");
      return false;
    }

    if (SlaveId < 1 || SlaveId > 247) {
      toast.error("Invalid Slave ID.");
      return false;
    }

    return true;
  };

  /* -------------------- HANDLERS -------------------- */

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]:
        name === "pollInterval" ? Number(value) : value,
    }));
  };

  const handleProtocolChange = (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      protocolSettings: {
        ...prev.protocolSettings,
        [name]:
          name === "Port" || name === "SlaveId"
            ? Number(value)
            : value,
      },
    }));
  };

  const handleEndianChange = (value: EndianType) => {
    setFormData((prev) => ({
      ...prev,
      protocolSettings: {
        ...prev.protocolSettings,
        Endian: value,
      },
    }));
  };

  /* -------------------- SUBMIT -------------------- */

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!deviceId) return;

    if (!validateForm()) return;

    setLoading(true);

    const payload = {
      device: { ...deviceDetails },
      configuration: {
        name: formData.configName.trim(),
        pollIntervalMs: formData.pollInterval,
        ipAddress: formData.protocolSettings.IpAddress,
        port: formData.protocolSettings.Port,
        slaveId: formData.protocolSettings.SlaveId,
        endian: formData.protocolSettings.Endian,
      },
    };

    try {
      await updateDevice(deviceId, payload);
      toast.success("Device updated successfully!");
      setTimeout(() => navigate("/devices"), 1000);
    } catch (err: any) {
      toast.error(
        err?.response?.data?.message ??
          "Failed to update device."
      );
    } finally {
      setLoading(false);
    }
  };

  /* -------------------- JSX -------------------- */

  return (
    <div className="flex justify-center items-center min-h-[85vh] p-4">
      <Card className="w-full max-w-2xl">
        <CardHeader>
          <CardTitle>Edit Device & Configuration</CardTitle>
        </CardHeader>

        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            <Input
              name="name"
              value={deviceDetails.name}
              onChange={(e) =>
                setDeviceDetails({
                  ...deviceDetails,
                  name: e.target.value,
                })
              }
              placeholder="Device Name"
            />

            <Input
              name="configName"
              value={formData.configName}
              onChange={handleChange}
              placeholder="Configuration Name"
            />

            <Input
              name="pollInterval"
              type="number"
              value={formData.pollInterval}
              onChange={handleChange}
            />

            <Input
              name="IpAddress"
              value={formData.protocolSettings.IpAddress}
              onChange={handleProtocolChange}
            />

            <Input
              name="Port"
              type="number"
              value={formData.protocolSettings.Port}
              onChange={handleProtocolChange}
            />

            <Select
              value={formData.protocolSettings.Endian}
              onValueChange={handleEndianChange}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select Endian" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Little">Little</SelectItem>
                <SelectItem value="Big">Big</SelectItem>
              </SelectContent>
            </Select>

            <div className="flex justify-between">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate("/devices")}
              >
                <ArrowLeft size={16} /> Back
              </Button>

              <Button type="submit" disabled={loading}>
                <Save size={16} />
                {loading ? "Saving..." : "Save"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
