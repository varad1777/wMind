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
import { Settings2, Cpu, Save, ArrowLeft,AlertTriangle} from "lucide-react";
import { getDeviceById, updateDevice } from "@/api/deviceApi";
import { toast } from "react-toastify";

export default function EditDeviceForm() {
  const navigate = useNavigate();
  const { deviceId } = useParams<{ deviceId: string }>();

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

  const [loading, setLoading] = useState(false);

useEffect(() => {
    if (!deviceId) return;

    let toastShown = false;

    const fetchDevice = async () => {
      try {
        const res = await getDeviceById(deviceId);
        console.log("Fetched device:", res);

        

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
        console.error("❌ Failed to fetch device:", error);

        // Prevent double toasts
        if (!toastShown) {
          toastShown = true;
          if(error.response?.status === 401){
            toast.error("unauthorized! Please login again.");
            navigate("/login");
          }
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


  // 🔹 VALIDATION FUNCTION 
  const validateForm = () => {
  const { name, description } = deviceDetails;
  const { configName, pollInterval, protocolSettings } = formData;
  const { IpAddress, Port, SlaveId } = protocolSettings;

  const nameRegex = /^[A-Za-z][A-Za-z0-9_\- ]{2,99}$/;
  if (!nameRegex.test(name.trim())) {
    toast.error(
      "Device Name must start with a letter, be 3–100 characters long, and may contain letters, numbers, spaces, underscores, or hyphens (but not start with a hyphen)."
    );
    return false;
  }

  if (description && description.length > 255) {
    toast.error("Description must be less than 255 characters.");
    return false;
  }

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

const ipOrLocalhostRegex =
  /^(https?:\/\/)?(localhost|((25[0-5]|2[0-4]\d|1?\d{1,2})(\.(25[0-5]|2[0-4]\d|1?\d{1,2})){3}))$/;

  if (!ipOrLocalhostRegex.test(IpAddress)) {
  toast.error("Invalid IP Address (e.g., 192.168.1.1 or http://localhost)");
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!deviceId) return toast.error("Missing Device ID!");

    // Run validation before submitting
    if (!validateForm()) return;

    setLoading(true);

    const payload = {
      device: { ...deviceDetails },
      configuration: {
        name: formData.configName.trim(),
        pollIntervalMs: Number(formData.pollInterval),
        protocolSettingsJson: JSON.stringify(formData.protocolSettings),
      },
    };

    try {
      await updateDevice(deviceId, payload);
      toast.success("Device updated successfully!");
      setTimeout(() => navigate("/devices"), 1000);
    } catch (err: any) {
      if(err.response?.status === 401){
        toast.error("unauthorized! Please login again.");
        navigate("/login");
      }
      console.error("Error editing device:", err);

      // Extract backend message
      const backendMessage =
        err?.response?.data?.error || 
        err?.response?.data?.message || 
        err?.response?.data?.data?.message ||
        "Failed to Edit device. Please try again.";

      toast.error(backendMessage, {
        position: "top-right",
        autoClose: 4000,
        theme: "colored",
      });
    } 
    finally {
      setLoading(false);
    }
  };

  
  return (
    <div className="flex justify-center items-center min-h-[85vh] bg-gradient-to-b from-background to-muted/30 text-foreground p-4 ">
      <Card className="w-full max-w-2xl shadow-lg border border-border/60 bg-card/90 backdrop-blur-sm ">
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
                  <Label htmlFor="name">Device Name *</Label>
                  <Input
                    id="name"
                    name="name"
                    type="text"
                    value={deviceDetails.name}
                    onChange={(e) =>
                      setDeviceDetails({ ...deviceDetails, name: e.target.value })
                    }
                    required
                  />
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="description">Description</Label>
                  <Textarea
                    id="description"
                    name="description"
                    value={deviceDetails.description}
                    onChange={(e) =>
                      setDeviceDetails({ ...deviceDetails, description: e.target.value })
                    }
                  />
                </div>

                <div className="grid gap-2">
                  <Label>Protocol</Label>
                  <Select
                    value={deviceDetails.protocol}
                    onValueChange={(value) =>
                      setDeviceDetails({ ...deviceDetails, protocol: value })
                    }
                  >
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Select protocol" />
                    </SelectTrigger>
                    <SelectContent className="bg-background text-foreground">
                      <SelectItem value="ModbusTCP">ModbusTCP</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </div>

            <div className="rounded-xl border border-border/70 bg-muted/30 p-5 shadow-inner">
              <div className="flex items-center gap-2 mb-4">
                <Settings2 className="h-5 w-5 text-primary" />
                <h2 className="text-lg font-semibold">Configuration Details</h2>
              </div>

              <div className="grid gap-4">
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

                <div className="grid grid-cols-2 gap-4">
                  <div className="grid gap-2">
                    <Label>IP Address</Label>
                    <Input
                      name="IpAddress"
                      value={formData.protocolSettings.IpAddress}
                      onChange={handleProtocolChange}
                      required
                    />
                  </div>

                  <div className="grid gap-2">
                    <Label>Port</Label>
                    <Input
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
                  </div>
                  <div className="grid gap-2">
                    <Label>Endian</Label>
                    <Select
                      value={formData.protocolSettings.Endian}
                      onValueChange={handleEndianChange}
                    >
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
              </div>
            </div>

            <div className="flex justify-between items-center pt-4 border-t border-border/40">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate("/devices")}
              >
                <ArrowLeft className="h-4 w-4" /> Back to Devices
              </Button>

              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4" /> {loading ? "Updating..." : "Save Changes"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

    </div>
  );
}