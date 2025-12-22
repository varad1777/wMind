import { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardHeader, CardContent, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Settings2, ArrowLeft, AlertTriangle } from "lucide-react";
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from "@/components/ui/select";
import { getDeviceById, updateDevice } from "@/api/deviceApi";
import { toast } from "react-toastify";

export default function ConfigureDevice() {
  const navigate = useNavigate();
  const { deviceId } = useParams<{ deviceId: string }>();
  const [loading, setLoading] = useState(false);

  const [deviceDetails, setDeviceDetails] = useState({
    name: "",
    description: "",
    protocol: "ModbusTCP",
  });

  const [formData, setFormData] = useState({
    configName: "",
    pollInterval: 1000,
    protocolSettings: {
      IpAddress: "127.0.0.1",
      Port: 5020,
      SlaveId: 1,
      Endian: "Little",
    },
  });

  // Fetch device details
  useEffect(() => {
    if (!deviceId) return;

    let toastShown = false;

    const fetchDevice = async () => {
      try {
        const res = await getDeviceById(deviceId);
        console.log("🔹 Fetched device:", res);

        if (res) {
          setDeviceDetails({
            name: res.name || "",
            description: res.description || "",
            protocol: res.protocol || "ModbusTCP",
          });

          setFormData({
            configName: res.deviceConfiguration?.name || `${res.name}_config`,
            pollInterval: res.deviceConfiguration?.pollIntervalMs || 1000,
            protocolSettings: res.deviceConfiguration?.protocolSettingsJson
              ? JSON.parse(res.deviceConfiguration.protocolSettingsJson)
              : {
                  IpAddress: "127.0.0.1",
                  Port: 5020,
                  SlaveId: 1,
                  Endian: "Little",
                },
          });
        }
      } catch (error: any) {
        console.error("Failed to fetch device:", error);

        // Prevent double toasts 
        if (!toastShown) {
          toastShown = true;
          if (error.response?.status === 404) {
            toast.error("Device not found!");
            navigate("/devices");
          } else {
            toast.error("Error fetching device details. Please try again.");
          }
        }
      }
    };

    fetchDevice();
  }, [deviceId]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleProtocolChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData({
      ...formData,
      protocolSettings: {
        ...formData.protocolSettings,
        [name]: name === "Port" || name === "SlaveId" ? Number(value) : value,
      },
    });
  };

  const handleEndianChange = (value: string) => {
    setFormData({
      ...formData,
      protocolSettings: { ...formData.protocolSettings, Endian: value },
    });
  };

  // Validation
  const validateForm = () => {
    const { configName, pollInterval, protocolSettings } = formData;
    const { IpAddress, Port, SlaveId } = protocolSettings;

    const configNameRegex = /^[A-Za-z][A-Za-z0-9_\- ]{0,99}$/;
    if (!configName.trim()) {
      toast.error("Configuration name is required.");
      return false;
    }
    if (!configNameRegex.test(configName.trim())) {
      toast.error(
        "Configuration Name must start with a letter, be 1–100 characters long, and may contain letters, numbers, spaces, underscores, or hyphens (but not start with a hyphen)."
      );
      return false;
    }

    if (isNaN(Number(pollInterval)) || pollInterval < 100 || pollInterval > 300000) {
      toast.error("Poll interval must be between 100 and 300000 milliseconds.");
      return false;
    }

    const ipRegex = /^(25[0-5]|2[0-4]\d|1?\d{1,2})(\.(25[0-5]|2[0-4]\d|1?\d{1,2})){3}$/;
    if (!ipRegex.test(IpAddress)) {
      toast.error("Invalid IP Address format (e.g., 192.168.1.1)");
      return false;
    }

    if (isNaN(Port) || Port < 1 || Port > 65535) {
      toast.error("Port must be between 1 and 65535");
      return false;
    }

    if (isNaN(SlaveId) || SlaveId < 1 || SlaveId > 247) {
      toast.error("Slave ID must be between 1 and 247");
      return false;
    }

    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!deviceId) return toast.error("Missing Device ID!");
    if (!validateForm()) return;

    setLoading(true);
    const payload = {
      device: {
        name: deviceDetails.name,
        description: deviceDetails.description,
        protocol: deviceDetails.protocol || "ModbusTCP",
      },
      configuration: {
        name: formData.configName,
        pollIntervalMs: Number(formData.pollInterval),
        protocolSettingsJson: JSON.stringify(formData.protocolSettings),
      },
    };

    try {
      await updateDevice(deviceId, payload);
      toast.success("Configuration updated successfully!");
      setTimeout(() => navigate("/devices"), 1000);
    } catch (error) {
      console.error("Error updating configuration:", error);
      toast.error("Failed to update configuration.");
    } finally {
      setLoading(false);
    }
  };



  return (
    <div className="flex justify-center items-center min-h-[80vh] bg-background text-foreground p-4">
      <Card className="w-full max-w-2xl shadow-lg border border-border bg-card">
        <CardHeader className="flex flex-row items-center gap-2">
          <Settings2 className="h-5 w-5 text-primary" />
          <CardTitle className="text-xl font-semibold">Configure Device</CardTitle>
        </CardHeader>

        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Config Name */}
            <div className="grid gap-2">
              <Label htmlFor="configName">Configuration Name *</Label>
              <Input
                id="configName"
                name="configName"
                type="text"
                value={formData.configName}
                onChange={handleChange}
                required
              />
            </div>

            {/* Poll Interval */}
            <div className="grid gap-2">
              <Label htmlFor="pollInterval">Poll Interval (ms)</Label>
              <Input
                id="pollInterval"
                name="pollInterval"
                type="number"
                value={formData.pollInterval}
                onChange={handleChange}
                required
              />
            </div>

            <hr className="border-border" />
            <p className="text-sm font-semibold text-muted-foreground">Protocol Settings</p>

            {/* Protocol Settings */}
            <div className="grid gap-3 md:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="IpAddress">IP Address</Label>
                <Input
                  id="IpAddress"
                  name="IpAddress"
                  value={formData.protocolSettings.IpAddress}
                  onChange={handleProtocolChange}
                  required
                />
              </div>

              <div className="grid gap-2">
                <Label htmlFor="Port">Port</Label>
                <Input
                  id="Port"
                  name="Port"
                  type="number"
                  value={formData.protocolSettings.Port}
                  onChange={handleProtocolChange}
                  required
                />
              </div>

              {/* <div className="grid gap-2">
                <Label htmlFor="SlaveId">Slave ID</Label>
                <Select
                  value={formData.protocolSettings.SlaveId?.toString() || ""}
                  onValueChange={(value) =>
                    setFormData((prev) => ({
                      ...prev,
                      protocolSettings: {
                        ...prev.protocolSettings,
                        SlaveId: value,
                      },
                    }))
                  }
                >
                  <SelectTrigger id="SlaveId">
                    <SelectValue placeholder="Select Slave ID" />
                  </SelectTrigger>
                  <SelectContent className="bg-background text-foreground">
                    <SelectItem value="1">1</SelectItem>
                    <SelectItem value="2">2</SelectItem>
                  </SelectContent>
                </Select>
              </div> */}

              {/* <div className="grid gap-2">
                <Label htmlFor="Endian">Endian</Label>
                <Select value={formData.protocolSettings.Endian} onValueChange={handleEndianChange}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select Endian" />
                  </SelectTrigger>
                  <SelectContent className="bg-background text-foreground">
                    <SelectItem value="Little">Little</SelectItem>
                    <SelectItem value="Big">Big</SelectItem>
                  </SelectContent>
                </Select>
              </div> */}
            </div>

            {/* Buttons */}
            <div className="flex justify-between items-center pt-6">
              <Button
                type="button"
                variant="outline"
                className="flex items-center gap-2"
                onClick={() => navigate("/devices")}
              >
                <ArrowLeft className="h-4 w-4" /> Back
              </Button>

              <Button type="submit" disabled={loading}>
                {loading ? "Saving..." : "Save Configuration"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

    </div>
  );
}
