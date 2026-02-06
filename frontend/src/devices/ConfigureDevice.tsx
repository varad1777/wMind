import { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardHeader, CardContent, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Settings2, ArrowLeft } from "lucide-react";
import { getDeviceById, updateDevice } from "@/api/deviceApi";
import { toast } from "react-toastify";

export default function ConfigureDevice() {
  const navigate = useNavigate();
  const { deviceId } = useParams<{ deviceId: string }>();
  const [loading, setLoading] = useState(false);

  const [deviceDetails, setDeviceDetails] = useState({
    name: "",
    description: "",
    gatewayClientId: "",
  });

  const [formData, setFormData] = useState({
    configName: "",
    pollInterval: 1000,
    ipAddress: "127.0.0.1",
    port: 502,
    slaveId: 1,
    endian: "Little",
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
          gatewayClientId: res.gatewayClientId ?? "",
        });

        setFormData({
          configName: res.configuration?.name ?? `${res.name}_config`,
          pollInterval: res.configuration?.pollIntervalMs ?? 1000,
          ipAddress: res.configuration?.ipAddress ?? "127.0.0.1",
          port: res.configuration?.port ?? 502,
          slaveId: res.configuration?.slaveId ?? 1,
          endian: res.configuration?.endian ?? "Little",
        });
      } catch (error) {
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
        name === "pollInterval" ||
        name === "port" ||
        name === "slaveId"
          ? Number(value)
          : value,
    }));
  };

  /* ===========================
     VALIDATION
  ============================ */
  const validateForm = () => {
    const { configName, pollInterval, ipAddress, port, slaveId } = formData;

    if (!configName.trim()) {
      toast.error("Configuration name is required");
      return false;
    }

    if (pollInterval < 100 || pollInterval > 300000) {
      toast.error("Poll interval must be 100–300000 ms");
      return false;
    }

    const ipRegex =
      /^(25[0-5]|2[0-4]\d|1?\d{1,2})(\.(25[0-5]|2[0-4]\d|1?\d{1,2})){3}$/;

    if (!ipRegex.test(ipAddress)) {
      toast.error("Invalid IP Address");
      return false;
    }

    if (port < 1 || port > 65535) {
      toast.error("Port must be 1–65535");
      return false;
    }

    if (slaveId < 1 || slaveId > 247) {
      toast.error("Slave ID must be 1–247");
      return false;
    }

    return true;
  };

  /* ===========================
     SUBMIT
  ============================ */
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!deviceId) return;

    if (!validateForm()) return;

    setLoading(true);

    try {
      await updateDevice(
        deviceId,
        {
          name: deviceDetails.name,
          description: deviceDetails.description,
          gatewayClientId: deviceDetails.gatewayClientId,
        },
        {
          name: formData.configName,
          pollIntervalMs: formData.pollInterval,
          ipAddress: formData.ipAddress,
          port: formData.port,
          slaveId: formData.slaveId,
          endian: formData.endian as "Little" | "Big",
        }
      );

      toast.success("Device configured successfully");
      navigate("/devices");
    } catch (err: any) {
      toast.error(
        err?.response?.data?.message ?? "Failed to update device"
      );
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
              <Label>Poll Interval (ms)</Label>
              <Input
                name="pollInterval"
                type="number"
                value={formData.pollInterval}
                onChange={handleChange}
              />
            </div>

            <hr />

            <p className="text-sm font-semibold">Protocol Settings</p>

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
