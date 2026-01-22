import api from "./axios";



export interface DevicePort {
  slaveIndex: number;
  registerAddress: number;
  registerLength?: number;
  dataType: string;
  scale?: number
  unit?: string;
  isHealthy?: boolean;
}

export interface DeviceConfiguration {
  name: string;
  pollIntervalMs: number;
  protocolSettingsJson: string;
}

export interface CreateDevicePayload {
  name: string;
  description?: string;
  gatewayClientId: string;
  ports?: DevicePort[];
  configuration?: DeviceConfiguration;
}

export interface Device {
  id: string;
  name: string;
  description?: string;
  gatewayClientId: string;
  ports?: DevicePort[];
  configuration?: DeviceConfiguration;
}

export const getDevices = async (
  pageNumber = 1,
  pageSize = 10,
  searchTerm = ""
) => {
  const response = await api.get("/devices", {
    params: { pageNumber, pageSize, searchTerm },
  });

  return response.data.data;
};

export const createDevice = async (payload: CreateDevicePayload) => {
  const response = await api.post("/devices", payload);

  return response.data.data;
};

export const getDeviceById = async (id: string) => {
  const response = await api.get(`/devices/${id}`);
  return response.data.data;
};

export const updateDevice = async (
  id: string,
  device: Partial<CreateDevicePayload>,
  configuration?: DeviceConfiguration
) => {
  const payload = {
    Device: device,              
    Configuration: configuration ?? null, 
  };

  const response = await api.put(`/devices/${id}`, payload);
  return response.data.data;
};

export const deleteDevice = async (id: string) => {
  const response = await api.delete(`/devices/${id}`);
  return response.data.data;
};

export const restoreDeviceById = async (id: string) => {
  const response = await api.post(`/devices/${id}/restore`);
  return response.data.data;
};

export const getDeletedDevices = async () => {
  const response = await api.get("/devices/deleted");
  return response.data.data;
};

export const matchByRegisterAddress = async (registerAddresses: number[]) => {
  try {
    const response = await api.post("/devices/match-by-address", {
      RegisterAddresses: registerAddresses 
    });
    
    console.log('API Response:', response.data);
    
    return {
      success: true,
      data: response.data.data || []
    };
  } catch (error: any) {
    console.error('Match API Error:', error.response?.data || error);
    return {
      success: false,
      data: [],
      error: error
    };
  }
};

export const getAvgApiResponseTime = async () => {
  const response = await api.get("/stats/avg-response-time");
  return response.data.avgResponseTime;
};
