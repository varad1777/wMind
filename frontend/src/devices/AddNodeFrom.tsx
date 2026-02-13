import React, { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select";
import { Plus, Save, Edit2, Trash2, Database, Network, Loader2 } from "lucide-react";
import { toast } from "react-toastify";
import {
  createOpcUaNode,
  getOpcUaNodes,
  getSingleOpcUaNode,
  updateOpcUaNode,
  deleteOpcUaNode,
  OpcUaNode,
  CreateOpcUaNodeRequest
} from "@/api/opcUaApi";
import { useParams } from "react-router-dom";


const UNIT_OPTIONS = [
  { name: "FlowRate", unit: "L/min" },
  { name: "Voltage", unit: "V" },
  { name: "Torque", unit: "Nm" },
  { name: "RPM", unit: "rpm" },
  { name: "Frequency", unit: "Hz" },
  { name: "Current", unit: "A" },
  { name: "Vibration", unit: "mm/s" },
  { name: "Temperature", unit: "°C" }
];

const defaultNode: CreateOpcUaNodeRequest = {
  nodeId: "ns=2;s=",
  signalName: "",
  dataType: "float",
  unit: "V",
  scalingFactor: 1
};

interface OpcUaNodeFormProps {
  deviceId: string; // Pass deviceId as prop
}

export default function OpcUaNodeForm() {
  const { deviceId } = useParams<{ deviceId: string }>();
  const [nodes, setNodes] = useState<OpcUaNode[]>([]);
  const [showNodeForm, setShowNodeForm] = useState(false);
  const [editingNode, setEditingNode] = useState<OpcUaNode | null>(null);
  const [nodeForm, setNodeForm] = useState<CreateOpcUaNodeRequest>({ ...defaultNode });
  const [loading, setLoading] = useState(false);
  const [fetchingNodes, setFetchingNodes] = useState(true);

  // Fetch all nodes on mount
  useEffect(() => {
    fetchNodes();
  }, [deviceId]);

  const fetchNodes = async () => {
    try {
      setFetchingNodes(true);
      const data = await getOpcUaNodes(deviceId);
      setNodes(data);
    } catch (error: any) {
      toast.error(error?.response?.data?.message || "Failed to fetch nodes");
    } finally {
      setFetchingNodes(false);
    }
  };

  const updateNodeForm = <K extends keyof CreateOpcUaNodeRequest>(
    key: K,
    value: CreateOpcUaNodeRequest[K]
  ) => setNodeForm(prev => ({ ...prev, [key]: value }));

  const validateNode = (node: CreateOpcUaNodeRequest) => {
    if (!node.nodeId.trim()) return "Node ID required";
    if (!node.signalName?.trim()) return "Signal Name required";

    // Check for duplicate nodeId (excluding current editing node)
    const duplicate = nodes.some(
      (n) => n.id !== editingNode?.id && n.nodeId === node.nodeId
    );

    if (duplicate) return "Node ID already exists";
    return null;
  };

  const handleSaveNode = async () => {
    const validationError = validateNode(nodeForm);
    if (validationError) {
      toast.error(validationError);
      return;
    }

    setLoading(true);
    try {
      if (editingNode?.id) {
        // UPDATE existing node
        await updateOpcUaNode(deviceId, editingNode.id, nodeForm);
        toast.success("Node updated successfully");
      } else {
        // CREATE new node
        await createOpcUaNode(deviceId, nodeForm);
        toast.success("Node created successfully");
      }

      await fetchNodes(); // Refresh the list
      cancelNodeForm();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || "Operation failed");
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteNode = async (node: OpcUaNode) => {
    if (!node.id) return;
    
    if (!window.confirm(`Delete node "${node.signalName || node.nodeId}"?`)) {
      return;
    }

    try {
      await deleteOpcUaNode(deviceId, node.id);
      toast.success("Node deleted successfully");
      await fetchNodes(); // Refresh the list
    } catch (error: any) {
      toast.error(error?.response?.data?.message || "Failed to delete node");
    }
  };

  const handleEditNode = (node: OpcUaNode) => {
    setNodeForm({
      nodeId: node.nodeId,
      signalName: node.signalName || "",
      dataType: node.dataType || "float",
      unit: node.unit || "V",
      scalingFactor: node.scalingFactor || 1
    });
    setEditingNode(node);
    setShowNodeForm(true);
  };

  const cancelNodeForm = () => {
    setNodeForm({ ...defaultNode });
    setEditingNode(null);
    setShowNodeForm(false);
  };

  return (
    <div className="min-h-screen bg-background text-foreground">
      {/* HEADER */}
      <div className="sticky top-0 z-50 bg-card border-b border-border">
        <div className="container flex items-center gap-3 py-4">
          <div className="p-3 bg-primary rounded-lg">
            <Network className="w-6 h-6 text-primary-foreground" />
          </div>
          <h1 className="text-2xl font-semibold">OPC UA Node Manager</h1>
        </div>
      </div>

      <div className="container py-8 space-y-6">

        {/* ADD BUTTON */}
        <div className="bg-card border border-border rounded-lg p-6">
          {!showNodeForm && (
            <Button onClick={() => setShowNodeForm(true)} disabled={loading}>
              <Plus className="w-4 h-4 mr-2" />
              Add Node
            </Button>
          )}
        </div>

        {/* FORM */}
        {showNodeForm && (
          <div className="bg-card border border-border rounded-lg p-6 space-y-5">
            <h2 className="text-lg font-semibold">
              {editingNode ? "Edit Node" : "Add New Node"}
            </h2>

            {/* NODE ID */}
            <div className="space-y-1">
              <label className="text-sm font-medium">Node ID</label>
              <Input
                className="focus:bg-primary/5"
                placeholder="Example: ns=2;s=Machine/Speed"
                value={nodeForm.nodeId}
                onChange={e => updateNodeForm("nodeId", e.target.value)}
                disabled={loading}
              />
              <p className="text-xs text-muted-foreground">
                OPC UA format example: ns=2;s=Asset/Signal
              </p>
            </div>

            {/* SIGNAL NAME */}
            <div className="space-y-1">
              <label className="text-sm font-medium">Signal Name</label>
              <Input
                className="focus:bg-primary/5"
                placeholder="Example: Motor Speed"
                value={nodeForm.signalName}
                onChange={e => updateNodeForm("signalName", e.target.value)}
                disabled={loading}
              />
            </div>

            {/* SCALING */}
            <div className="space-y-1">
              <label className="text-sm font-medium">Scaling Factor</label>
              <Input
                type="number"
                step="0.01"
                className="focus:bg-primary/5"
                placeholder="Example: 0.1"
                value={nodeForm.scalingFactor}
                onChange={e =>
                  updateNodeForm("scalingFactor", Number(e.target.value))
                }
                disabled={loading}
              />
            </div>

            {/* DATATYPE */}
            <div className="space-y-1">
              <label className="text-sm font-medium">Data Type</label>
              <Select
                value={nodeForm.dataType}
                onValueChange={(v: any) => updateNodeForm("dataType", v)}
                disabled={loading}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="bg-card shadow-md border border-border">
                  <SelectItem value="int">int</SelectItem>
                  <SelectItem value="float">float</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* UNIT */}
            <div className="space-y-1">
              <label className="text-sm font-medium">Unit</label>
              <Select
                value={nodeForm.unit}
                onValueChange={v => updateNodeForm("unit", v)}
                disabled={loading}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="bg-card shadow-md border border-border">
                  {UNIT_OPTIONS.map(u => (
                    <SelectItem key={u.unit} value={u.unit}>
                      {u.name} ({u.unit})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* BUTTONS */}
            <div className="flex gap-3 pt-2">
              <Button onClick={handleSaveNode} disabled={loading}>
                {loading ? (
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                ) : (
                  <Save className="w-4 h-4 mr-2" />
                )}
                {loading ? "Saving..." : "Save Node"}
              </Button>
              <Button variant="outline" onClick={cancelNodeForm} disabled={loading}>
                Cancel
              </Button>
            </div>
          </div>
        )}

        {/* TABLE */}
        {fetchingNodes ? (
          <div className="bg-card border border-border rounded-lg p-12 text-center">
            <Loader2 className="w-10 h-10 mx-auto mb-3 animate-spin opacity-70" />
            <p>Loading nodes...</p>
          </div>
        ) : nodes.length === 0 ? (
          <div className="bg-card border border-border rounded-lg p-12 text-center">
            <Database className="w-10 h-10 mx-auto mb-3 opacity-70" />
            <p>No OPC UA nodes configured yet</p>
          </div>
        ) : (
          <div className="bg-card border border-border rounded-lg overflow-hidden">
            <table className="w-full">
              <thead className="bg-primary/5">
                <tr className="text-left text-sm">
                  <th className="p-3">NodeId</th>
                  <th className="p-3">Signal</th>
                  <th className="p-3">Unit</th>
                  <th className="p-3">Type</th>
                  <th className="p-3">Scaling</th>
                  <th className="p-3">Actions</th>
                </tr>
              </thead>
              <tbody>
                {nodes.map((n) => (
                  <tr key={n.id} className="border-t border-border">
                    <td className="p-3 font-mono text-sm">{n.nodeId}</td>
                    <td className="p-3">{n.signalName}</td>
                    <td className="p-3">{n.unit}</td>
                    <td className="p-3">{n.dataType}</td>
                    <td className="p-3">{n.scalingFactor}</td>
                    <td className="p-3 flex gap-2">
                      <Button 
                        size="sm" 
                        variant="outline"
                        onClick={() => handleEditNode(n)}
                        disabled={loading}
                      >
                        <Edit2 className="w-4 h-4" />
                      </Button>
                      <Button 
                        size="sm" 
                        variant="destructive"
                        onClick={() => handleDeleteNode(n)}
                        disabled={loading}
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}