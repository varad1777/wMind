import React, { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select";
import { Plus, Save, Edit2, Trash2, Database, Network } from "lucide-react";
import { toast } from "react-toastify";

export type OpcUaNodePayload = {
  nodeId: string;
  signalName: string;
  dataType: "int" | "float";
  unit: string;
  scalingFactor: number;
};

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

const defaultNode: OpcUaNodePayload = {
  nodeId: "ns=2;s=",
  signalName: "",
  dataType: "float",
  unit: "V",
  scalingFactor: 1
};

export default function OpcUaNodeForm() {
  const [nodes, setNodes] = useState<OpcUaNodePayload[]>([]);
  const [showNodeForm, setShowNodeForm] = useState(false);
  const [editingNodeIdx, setEditingNodeIdx] = useState<number | null>(null);
  const [nodeForm, setNodeForm] = useState<OpcUaNodePayload>({ ...defaultNode });

  const updateNodeForm = <K extends keyof OpcUaNodePayload>(
    key: K,
    value: OpcUaNodePayload[K]
  ) => setNodeForm(prev => ({ ...prev, [key]: value }));

  const validateNode = (node: OpcUaNodePayload) => {
    if (!node.nodeId.trim()) return "Node ID required";
    if (!node.signalName.trim()) return "Signal Name required";

    const duplicate = nodes.some(
      (n, idx) => idx !== editingNodeIdx && n.nodeId === node.nodeId
    );

    if (duplicate) return "Node already exists";
    return null;
  };

  const handleSaveNode = () => {
    const validationError = validateNode(nodeForm);
    if (validationError) {
      toast.error(validationError);
      return;
    }

    if (editingNodeIdx !== null) {
      const updated = nodes.map((n, i) =>
        i === editingNodeIdx ? { ...nodeForm } : n
      );
      setNodes(updated);
      toast.success("Node updated");
    } else {
      setNodes([...nodes, { ...nodeForm }]);
      toast.success("Node added");
    }

    cancelNodeForm();
  };

  const handleDeleteNode = (idx: number) => {
    setNodes(nodes.filter((_, i) => i !== idx));
    toast.success("Node deleted");
  };

  const handleEditNode = (idx: number) => {
    setNodeForm({ ...nodes[idx] });
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
            <Button onClick={() => setShowNodeForm(true)}>
              <Plus className="w-4 h-4 mr-2" />
              Add Node
            </Button>
          )}
        </div>

        {/* FORM */}
        {showNodeForm && (
          <div className="bg-card border border-border rounded-lg p-6 space-y-5">

            {/* NODE ID */}
            <div className="space-y-1">
              <label className="text-sm font-medium">Node ID</label>
              <Input
                className="focus:bg-primary/5"
                placeholder="Example: ns=2;s=Machine/Speed"
                value={nodeForm.nodeId}
                onChange={e => updateNodeForm("nodeId", e.target.value)}
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
              />
            </div>

            {/* SCALING */}
            <div className="space-y-1">
              <label className="text-sm font-medium">Scaling Factor</label>
              <Input
                type="number"
                className="focus:bg-primary/5"
                placeholder="Example: 0.1"
                value={nodeForm.scalingFactor}
                onChange={e =>
                  updateNodeForm("scalingFactor", Number(e.target.value))
                }
              />
            </div>

            {/* DATATYPE */}
            <div className="space-y-1">
              <label className="text-sm font-medium">Data Type</label>
              <Select
                value={nodeForm.dataType}
                onValueChange={(v: any) => updateNodeForm("dataType", v)}
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
              <Button onClick={handleSaveNode}>
                <Save className="w-4 h-4 mr-2" />
                Save Node
              </Button>
              <Button variant="outline" onClick={cancelNodeForm}>
                Cancel
              </Button>
            </div>
          </div>
        )}

        {/* TABLE */}
        {nodes.length === 0 ? (
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
                  <th className="p-3">Action</th>
                </tr>
              </thead>
              <tbody>
                {nodes.map((n, i) => (
                  <tr key={i} className="border-t border-border">
                    <td className="p-3">{n.nodeId}</td>
                    <td className="p-3">{n.signalName}</td>
                    <td className="p-3">{n.unit}</td>
                    <td className="p-3">{n.dataType}</td>
                    <td className="p-3">{n.scalingFactor}</td>
                    <td className="p-3 flex gap-2">
                      <Button size="sm" onClick={() => handleEditNode(i)}>
                        <Edit2 className="w-4 h-4" />
                      </Button>
                      <Button size="sm" onClick={() => handleDeleteNode(i)}>
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
