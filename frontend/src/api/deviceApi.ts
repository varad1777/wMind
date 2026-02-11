import api from "./axios";

/* ============================
   ENUMS
============================ */
export const DeviceProtocol = {
  Modbus: 1,
  OpcUa: 2,
} as const;

export type DeviceProtocol =
  (typeof DeviceProtocol)[keyof typeof DeviceProtocol];


export const OpcUaConnectionMode = {
  Polling: 1,
  PubSub: 2,
} as const;

export type OpcUaConnectionMode =
  (typeof OpcUaConnectionMode)[keyof typeof OpcUaConnectionMode];

/* ============================
   INTERFACES
============================ */

export interface DevicePort {
  slaveIndex: number;
  registerAddress: number;
  registerLength?: number;
  dataType: string;
  scale?: number;
  unit?: string;
  isHealthy?: boolean;
  protocol: DeviceProtocol;
}

export interface DeviceConfiguration {
  name: string;
  protocol: DeviceProtocol;

  // Polling (both protocols)
  pollIntervalMs?: number;

  // 🔹 Modbus only
  ipAddress?: string;
  port?: number;
  slaveId?: number;
  endian?: "Little" | "Big";

  // 🔹 OPC UA only
  connectionString?: string;
  connectionMode?: OpcUaConnectionMode;
}

export interface CreateDevicePayload {
  name: string;
  description?: string;
  gatewayClientId: string;
  protocol: DeviceProtocol;
  ports?: DevicePort[];
  configuration?: DeviceConfiguration;
}

export interface UpdateDevicePayload {
  name?: string;
  description?: string;
  gatewayClientId?: string;
  protocol?: DeviceProtocol;
}

export interface Device {
  deviceId: string; // API returns deviceId, not id
  name: string;
  description?: string;
  gatewayId: string; // API returns gatewayId
  protocol: DeviceProtocol;
  deviceSlave?: DevicePort[]; // API returns deviceSlave
  deviceConfiguration?: DeviceConfiguration; // API returns deviceConfiguration, not configuration
  deviceConfigurationId?: string;
  isDeleted: boolean;
  createdAt: string;
  items ?: any[]; // For matched devices, can contain matched items
  totalPages?: number; // For paginated responses
}
/* ============================
   API FUNCTIONS
============================ */

// GET /api/devices
export const getDevices = async (
  pageNumber = 1,
  pageSize = 10,
  searchTerm = ""
) => {
  const response = await api.get("/devices", {
    params: { pageNumber, pageSize, searchTerm },
  });
  return response.data.data as Device[];
};

// POST /api/devices

export const createDevice = async (payload: CreateDevicePayload) => {
  const response = await api.post("/devices", payload); // no wrapping in dto
  return response.data.data as Device;
};



// GET /api/devices/{id}
export const getDeviceById = async (id: string) => {
  const response = await api.get(`/devices/${id}`);
  return response.data.data as Device;
};

// PUT /api/devices/{id}
export const updateDevice = async (
  id: string,
  device: UpdateDevicePayload,
  configuration?: DeviceConfiguration
) => {
  // Combine device and configuration in the same object, no "dto" wrapper
  const payload = {
    ...device,
    configuration: configuration ?? null,
  };

  console.log("Update Device Payload:", payload);

  const response = await api.put(`/devices/${id}`, payload);
  return response.data.data as Device;
};



// POST /api/devices/{id}/configuration
export const addDeviceConfiguration = async (
  deviceId: string,
  configuration: DeviceConfiguration
) => {
  const response = await api.post(
    `/devices/${deviceId}/configuration`,
    configuration
  );
  return response.data.data;
};

// DELETE /api/devices/{id}
export const deleteDevice = async (id: string) => {
  const response = await api.delete(`/devices/${id}`);
  return response.data.data;
};

// POST /api/devices/{id}/restore
export const restoreDeviceById = async (id: string) => {
  const response = await api.post(`/devices/${id}/restore`);
  return response.data.data;
};

// GET /api/devices/deleted
export const getDeletedDevices = async () => {
  const response = await api.get("/devices/deleted");
  return response.data.data as Device[];
};

// POST /api/devices/match-by-address
export const matchByRegisterAddress = async (registerAddresses: number[]) => {
  try {
    const response = await api.post("/devices/match-by-address", {
      RegisterAddresses: registerAddresses,
    });

    return {
      success: true,
      data: response.data.data ?? [],
    };
  } catch (error: any) {
    console.error("Match API Error:", error.response?.data || error);
    return {
      success: false,
      data: [],
      error,
    };
  }
};

// GET /stats/avg-response-time
export const getAvgApiResponseTime = async () => {
  const response = await api.get("/stats/avg-response-time");
  return response.data.avgResponseTime;
};
