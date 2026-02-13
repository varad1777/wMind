import api from "./axios";

/**
 * DTOs matching backend responses
 */

export interface OpcUaNode {
  nodeId: string;
  displayName?: string;
  dataType?: string;
  unit?: string;
}

export interface AddOpcUaNodesDto {
  nodeIds: string[];
}

export interface AddOpcUaNodesResponse {
  deviceId: string;
  nodeIds: string[];
}

/**
 * ---------------------------------
 * OPC UA NODE APIs
 * ---------------------------------
 */

/**
 * 1️⃣ Add OPC UA Nodes
 * POST /api/devices/{deviceId}/opcua-nodes
 */
export const addOpcUaNodes = async (
  deviceId: string,
  data: AddOpcUaNodesDto
): Promise<AddOpcUaNodesResponse> => {
  if (!deviceId) {
    throw new Error("DeviceId is required");
  }

  const response = await api.post(
    `/devices/${deviceId}/opcua-nodes`,
    data
  );

  return response.data;
};

/**
 * 2️⃣ Update OPC UA Nodes (Bulk)
 * PUT /api/devices/{deviceId}/opcua-nodes
 */
export const updateOpcUaNodes = async (
  deviceId: string,
  data: AddOpcUaNodesDto
): Promise<void> => {
  if (!deviceId) {
    throw new Error("DeviceId is required");
  }

  await api.put(
    `/devices/${deviceId}/opcua-nodes`,
    data
  );
};

/**
 * 3️⃣ Get All OPC UA Nodes for Device
 * GET /api/devices/{deviceId}/opcua-nodes
 */
export const getOpcUaNodes = async (
  deviceId: string
): Promise<OpcUaNode[]> => {
  const response = await api.get(
    `/devices/${deviceId}/opcua-nodes`
  );

  return response.data;
};

/**
 * 4️⃣ Get Single OPC UA Node
 * GET /api/devices/{deviceId}/opcua-nodes/{nodeId}
 */
export const getSingleOpcUaNode = async (
  deviceId: string,
  nodeId: string
): Promise<OpcUaNode> => {
  const response = await api.get(
    `/devices/${deviceId}/opcua-nodes/${encodeURIComponent(nodeId)}`
  );

  return response.data;
};

/**
 * 5️⃣ Delete OPC UA Node
 * DELETE /api/devices/{deviceId}/opcua-nodes/{nodeId}
 */
export const deleteOpcUaNode = async (
  deviceId: string,
  nodeId: string
): Promise<void> => {
  await api.delete(
    `/devices/${deviceId}/opcua-nodes/${encodeURIComponent(nodeId)}`
  );
};

/**
 * ---------------------------------
 * FRONTEND SEARCH HELPER
 * ---------------------------------
 */

export const searchOpcUaNodes = (
  nodes: OpcUaNode[],
  searchTerm: string
): OpcUaNode[] => {
  if (!searchTerm) return nodes;

  const term = searchTerm.toLowerCase();

  return nodes.filter(
    (n) =>
      n.nodeId.toLowerCase().includes(term) ||
      (n.displayName || "").toLowerCase().includes(term)
  );
};
