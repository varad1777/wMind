import React, { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Plus, Save, X, Edit2, Trash2, Database, Network } from "lucide-react";
import { toast } from "react-toastify";

export type OpcUaNodePayload = {
  nodeId: string;
  displayName: string;
  dataType: string;
  unit?: string | null;
  isHealthy: boolean;
};

const defaultNode: OpcUaNodePayload = {
  nodeId: "ns=2;s=",
  displayName: "",
  dataType: "Double",
  unit: "V",
  isHealthy: true
};

export default function OpcUaNodeForm() {
  const [nodes, setNodes] = useState<OpcUaNodePayload[]>([]);
  const [showNodeForm, setShowNodeForm] = useState(false);
  const [editingNodeIdx, setEditingNodeIdx] = useState<number | null>(null);
  const [nodeForm, setNodeForm] = useState<OpcUaNodePayload>({ ...defaultNode });

  const updateNodeForm = <K extends keyof OpcUaNodePayload>(key: K, value: OpcUaNodePayload[K]) => {
    setNodeForm(prev => ({ ...prev, [key]: value }));
  };

  const validateNode = (node: OpcUaNodePayload) => {
    if (!node.nodeId?.trim()) return "Node ID is required";
    if (!node.displayName?.trim()) return "Display Name is required";
    if (!node.dataType?.trim()) return "Data Type is required";

    const duplicate = nodes.some((n, idx) =>
      idx !== editingNodeIdx && n.nodeId === node.nodeId
    );
    if (duplicate) return "Node ID already exists";

    return null;
  };

  const handleSaveNode = () => {
    const form = { ...nodeForm };

    const validationError = validateNode(form);
    if (validationError) {
      toast.error(validationError);
      return;
    }

    if (editingNodeIdx !== null) {
      const updatedNodes = nodes.map((n, idx) =>
        idx === editingNodeIdx ? { ...form } : n
      );
      setNodes(updatedNodes);
      toast.success("Node updated successfully");
    } else {
      setNodes([...nodes, { ...form }]);
      toast.success("Node added successfully");
    }

    cancelNodeForm();
  };

  const handleDeleteNode = (idx: number) => {
    setNodes(nodes.filter((_, i) => i !== idx));
    toast.success("Node deleted successfully");
  };

  const handleEditNode = (idx: number) => {
    const node = nodes[idx];
    setNodeForm({ ...node });
    setEditingNodeIdx(idx);
    setShowNodeForm(true);
  };

  const cancelNodeForm = () => {
    setNodeForm({ ...defaultNode });
    setEditingNodeIdx(null);
    setShowNodeForm(false);
  };

  return (
    <div className="min-h-screen bg-background text-foreground">
      <div className="sticky top-0 z-50 bg-card/80 backdrop-blur-md border-b border-border shadow-sm">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="p-2.5 bg-gradient-to-br from-emerald-600 to-emerald-700 rounded-xl">
              <Network className="w-6 h-6 text-white" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-foreground">OPC UA Node Manager</h1>
              <p className="text-xs text-muted-foreground">Add and manage OPC UA nodes</p>
            </div>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-8">
        <div className="space-y-3">
          <div className="bg-card rounded-2xl border border-border shadow-sm p-6">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h2 className="text-xl font-bold text-foreground">OPC UA Nodes</h2>
                <p className="text-sm text-muted-foreground">{nodes.length} nodes configured</p>
              </div>
              {!showNodeForm && (
                <Button
                  onClick={() => setShowNodeForm(true)}
                  className="bg-gradient-to-r from-emerald-600 to-emerald-700 hover:from-emerald-700 hover:to-emerald-800 text-white shadow-lg hover:shadow-xl transition-all"
                >
                  <Plus className="w-5 h-5 mr-2" />
                  Add Node
                </Button>
              )}
            </div>
          </div>

          {showNodeForm && (
            <div className="bg-card rounded-2xl border border-border shadow-sm p-6">
              <div className="flex items-center justify-between mb-6">
                <h3 className="text-lg font-bold text-foreground">
                  {editingNodeIdx !== null ? "Edit Node" : "Add Node"}
                </h3>
                <button onClick={cancelNodeForm} className="p-1 rounded-lg hover:bg-muted transition">
                  <X className="w-5 h-5 text-muted-foreground" />
                </button>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6">
                <div className="space-y-2 sm:col-span-2">
                  <Label className="text-sm font-medium text-foreground">Node ID</Label>
                  <Input
                    value={nodeForm.nodeId}
                    onChange={(e) => updateNodeForm("nodeId", e.target.value)}
                    className="rounded-xl border-border bg-card font-mono"
                    placeholder="ns=2;s=Plant=MUMBAI_PLANT/Line=ASSEMBLY_01/Machine=CNC_02/Signal=VOLTAGE"
                  />
                  <p className="text-xs text-muted-foreground">
                    Full OPC UA node identifier (e.g., ns=2;s=Plant=MUMBAI_PLANT/Line=ASSEMBLY_01/Machine=CNC_02/Signal=VOLTAGE)
                  </p>
                </div>

                <div className="space-y-2">
                  <Label className="text-sm font-medium text-foreground">Display Name</Label>
                  <Input
                    value={nodeForm.displayName}
                    onChange={(e) => updateNodeForm("displayName", e.target.value)}
                    className="rounded-xl border-border bg-card"
                    placeholder="Belt Speed"
                  />
                </div>

                <div className="space-y-2">
                  <Label className="text-sm font-medium text-foreground">Data Type</Label>
                  <Select value={nodeForm.dataType} onValueChange={(v) => updateNodeForm("dataType", v)}>
                    <SelectTrigger className="rounded-xl border-border bg-card">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent className="bg-card shadow-md border border-border">
                      <SelectItem value="Boolean">Boolean</SelectItem>
                      <SelectItem value="Byte">Byte</SelectItem>
                      <SelectItem value="Int16">Int16</SelectItem>
                      <SelectItem value="UInt16">UInt16</SelectItem>
                      <SelectItem value="Int32">Int32</SelectItem>
                      <SelectItem value="UInt32">UInt32</SelectItem>
                      <SelectItem value="Int64">Int64</SelectItem>
                      <SelectItem value="UInt64">UInt64</SelectItem>
                      <SelectItem value="Float">Float</SelectItem>
                      <SelectItem value="Double">Double</SelectItem>
                      <SelectItem value="String">String</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label className="text-sm font-medium text-foreground">Unit</Label>
                  <Select
                    value={nodeForm.unit ?? "V"}
                    onValueChange={(v) => updateNodeForm("unit", v)}
                  >
                    <SelectTrigger className="rounded-xl border-border bg-card">
                      <SelectValue placeholder="Select unit" />
                    </SelectTrigger>
                    <SelectContent className="bg-card shadow-md border border-border">
                      <SelectItem value="V">V (Volts)</SelectItem>
                      <SelectItem value="A">A (Amperes)</SelectItem>
                      <SelectItem value="°C">°C (Celsius)</SelectItem>
                      <SelectItem value="Hz">Hz (Hertz)</SelectItem>
                      <SelectItem value="mm/s">mm/s (Vibration)</SelectItem>
                      <SelectItem value="L/min">L/min (Flow Rate)</SelectItem>
                      <SelectItem value="rpm">rpm (RPM)</SelectItem>
                      <SelectItem value="kPa">kPa (Pressure)</SelectItem>
                      <SelectItem value="N·m">N·m (Torque)</SelectItem>
                      <SelectItem value="m/s">m/s (Speed)</SelectItem>
                      <SelectItem value="%">% (Percentage)</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="space-y-3 mb-6 p-4 bg-card rounded-xl border border-border">
                <div className="flex items-center gap-3">
                  <Checkbox
                    id="isHealthy"
                    checked={nodeForm.isHealthy}
                    onCheckedChange={(c) => updateNodeForm("isHealthy", !!c)}
                  />
                  <Label htmlFor="isHealthy" className="text-sm font-medium cursor-pointer">Healthy</Label>
                </div>
              </div>

              <div className="flex gap-3">
                <Button
                  onClick={handleSaveNode}
                  className="flex-1 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl"
                >
                  <Save className="w-4 h-4 mr-2" />
                  {editingNodeIdx !== null ? "Update Node" : "Add Node"}
                </Button>
                <Button
                  variant="outline"
                  onClick={cancelNodeForm}
                  className="rounded-xl border-border"
                >
                  Cancel
                </Button>
              </div>
            </div>
          )}

          {nodes.length === 0 ? (
            <div className="bg-card rounded-2xl border border-border shadow-sm p-12 text-center">
              <Database className="w-12 h-12 mx-auto text-muted-foreground mb-3" />
              <p className="text-muted-foreground text-sm">No nodes configured</p>
              <p className="text-muted-foreground/70 text-xs mt-1">Click "Add Node" to get started</p>
            </div>
          ) : (
            <div className="bg-card rounded-2xl border border-border shadow-sm overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead className="bg-gradient-to-r from-card to-muted border-b border-border/70">
                    <tr>
                      <th className="px-6 py-4 text-left text-xs font-semibold text-foreground/80 uppercase tracking-wider">Node ID</th>
                      <th className="px-6 py-4 text-left text-xs font-semibold text-foreground/80 uppercase tracking-wider">Display Name</th>
                      <th className="px-6 py-4 text-left text-xs font-semibold text-foreground/80 uppercase tracking-wider">Data Type</th>
                      <th className="px-6 py-4 text-left text-xs font-semibold text-foreground/80 uppercase tracking-wider">Unit</th>
                      <th className="px-6 py-4 text-left text-xs font-semibold text-foreground/80 uppercase tracking-wider">Status</th>
                      <th className="px-6 py-4 text-left text-xs font-semibold text-foreground/80 uppercase tracking-wider">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border bg-card">
                    {nodes.map((node, idx) => (
                      <tr key={idx} className="hover:bg-accent transition-colors">
                        <td className="px-6 py-4 text-sm font-mono text-foreground max-w-md truncate" title={node.nodeId}>
                          {node.nodeId}
                        </td>
                        <td className="px-6 py-4 text-sm font-medium text-foreground/80">{node.displayName}</td>
                        <td className="px-6 py-4 text-sm font-mono text-foreground/80">{node.dataType}</td>
                        <td className="px-6 py-4 text-sm text-foreground/80">{node.unit || "—"}</td>
                        <td className="px-6 py-4 text-sm">
                          <span className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-medium ${
                            node.isHealthy
                              ? "bg-emerald-100 text-emerald-700 border border-emerald-200"
                              : "bg-red-100 text-red-700 border border-red-200"
                          }`}>
                            {node.isHealthy ? "Healthy" : "Unhealthy"}
                          </span>
                        </td>
                        <td className="px-6 py-4 text-sm">
                          <div className="flex gap-2 justify-end">
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={() => handleEditNode(idx)}
                              className="hover:bg-primary/10 hover:text-primary"
                            >
                              <Edit2 className="w-4 h-4" />
                            </Button>
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={() => handleDeleteNode(idx)}
                              className="hover:bg-red-50 hover:text-red-600"
                            >
                              <Trash2 className="w-4 h-4" />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
