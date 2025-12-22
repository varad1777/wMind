/* -------------- FULL WORKING UPDATED VERSION (subtree expand/collapse fix) -------------- */

import React, { useEffect, useMemo, useState, useCallback } from "react";
import {
  ChevronRight,
  ChevronDown,
  Building2,
  Layers,
  Wrench,
  Plus,
  Edit,
  Trash2,
  Factory,
  Signal,
  Tv
} from "lucide-react";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";

import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

import Addroot from "../AssetsHierarchy/Addroot";
import Addasset from "../AssetsHierarchy/Addasset";
import Editasset from "../AssetsHierarchy/Editasset";
import DeleteAsset from "@/AssetsHierarchy/DeleteAsset";
import { useAuth } from "@/context/AuthContext";
import levelToType from "./mapBackendAsset";
import { toast } from "react-toastify";
import ConfigureAsset from "@/AssetsHierarchy/ConfigureAsset";
import { Spinner } from "@/components/ui/spinner";
import { useNavigate } from "react-router-dom";

/* ---------------- Types ---------------- */

export interface BackendAsset {
  assetId: string;
  name: string;
  childrens: BackendAsset[];
  parentId: string | null;
  level: number;
  isDeleted: boolean;
}

/* ---------------- Utility: Escape HTML ---------------- */

const escapeHtml = (unsafe: string) =>
  unsafe
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");

const escapeRegExp = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

/* ---------------- Highlight matched text ---------------- */

const highlightMatchHtml = (name: string, term: string) => {
  if (!term.trim()) return escapeHtml(name);

  const regex = new RegExp(`(${escapeRegExp(term)})`, "ig");
  return escapeHtml(name).replace(
    regex,
    `<span class="text-primary font-bold underline">$1</span>`
  );
};

/* ---------------- Tree Filtering ---------------- */

const filterTreeBySearch = (assets: BackendAsset[], term: string): BackendAsset[] => {
  if (!term.trim()) return assets;

  const lower = term.toLowerCase();

  const helper = (node: BackendAsset): BackendAsset | null => {
    const matches = node.name.toLowerCase().includes(lower);

    const filteredKids = node.childrens
      .map((c) => helper(c))
      .filter(Boolean) as BackendAsset[];

    if (matches || filteredKids.length > 0)
      return { ...node, childrens: filteredKids };

    return null;
  };

  return assets.map(a => helper(a)).filter(Boolean) as BackendAsset[];
};

/* ---------------- Search Match Checker ---------------- */

const nodeOrDescendantMatches = (node: BackendAsset, term: string): boolean => {
  if (!term.trim()) return false;
  const lower = term.toLowerCase();
  if (node.name.toLowerCase().includes(lower)) return true;
  return node.childrens.some(c => nodeOrDescendantMatches(c, term));
};

/* ---------------- Recursive Node Finder ---------------- */

const findNode = (list: BackendAsset[], id: string): BackendAsset | null => {
  for (const node of list) {
    if (node.assetId === id) return node;
    const child = findNode(node.childrens, id);
    if (child) return child;
  }
  return null;
};

/* ---------------- AssetTreeNode Component ---------------- */

interface NodeProps {
  asset: BackendAsset;
  selectedId: string | null;
  onSelect: (a: BackendAsset) => void;
  searchTerm: string;
  isAdmin: boolean;

  expandedMap: Record<string, boolean>;
  setExpandedMap: (m: Record<string, boolean>) => void;

  setShowAddAssetModal: (v: boolean) => void;
  setAssetForAdd: (a: BackendAsset) => void;
  setShowEditModal: (v: boolean) => void;
  setAssetForEdit: (a: BackendAsset) => void;
  setOpenDeleteDialog: (v: boolean) => void;
  setAssetToDelete: (a: BackendAsset) => void;
  setAssetForConfig: (a: any) => void;
  setShowConfigureModal: (v: boolean) => void;
}

const AssetTreeNode: React.FC<NodeProps> = ({
  asset,
  selectedId,
  onSelect,
  searchTerm,
  isAdmin,
  expandedMap,
  setExpandedMap,
  setShowAddAssetModal,
  setAssetForAdd,
  setShowEditModal,
  setAssetForEdit,
  setOpenDeleteDialog,
  setAssetToDelete,
  setAssetForConfig,
  setShowConfigureModal
}) => {
  const hasChildren = asset.childrens.length > 0;
  const isSelected = asset.assetId === selectedId;
  const isExpanded = expandedMap[asset.assetId] ?? false;
  const navigate = useNavigate();

  const handleOpenAsset = (asset) => {
    navigate("/signal", {
      state: { asset },
    });
  };

  /* ---------- Click on asset name = expand/collapse entire subtree (recursive, local) ---------- */

const expandRecursivelyToMap = (node: BackendAsset, map: Record<string, boolean>) => {
  map[node.assetId] = true;                  // expand this node
  node.childrens.forEach(child => 
    expandRecursivelyToMap(child, map)      // recursively expand all children
  );
};


  const collapseRecursivelyFromMap = (node: BackendAsset, map: Record<string, boolean>) => {
    delete map[node.assetId];
    node.childrens.forEach(child => collapseRecursivelyFromMap(child, map));
  };

  const handleFullToggle = (e?: React.MouseEvent) => {
  if (e) e.stopPropagation();

  setExpandedMap(prev => {
    const next = { ...prev };
    expandRecursivelyToMap(asset, next);   // ALWAYS fully expand
    return next;
  });

  onSelect(asset);
};

  /* ---------- Toggle single level (chevron only) ---------- */

  const toggleLevel = (e: any) => {
    e.stopPropagation();
    setExpandedMap(prev => ({ ...prev, [asset.assetId]: !prev[asset.assetId] }));
  };

  /* ---------- Search auto-expand ---------- */

  useEffect(() => {
    if (!searchTerm.trim()) return;

    if (nodeOrDescendantMatches(asset, searchTerm)) {
      setExpandedMap(prev => ({ ...prev, [asset.assetId]: true }));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchTerm]);

  /* ---------- Icon ---------- */

  const type = levelToType(asset.level);
  const Icon =
    type === "Plant"
      ? Factory
      : type === "Department"
      ? Building2
      : type === "Line"
      ? Layers
      : Wrench;

  const highlightedName = highlightMatchHtml(asset.name, searchTerm);

  return (
    <div>
      <div
        className={`flex items-center justify-between gap-2 px-3 py-2 cursor-pointer hover:bg-accent rounded-sm ${
          isSelected ? "bg-primary/10 text-primary font-medium" : ""
        } ${asset.isDeleted ? "opacity-50" : ""}`}
        onClick={handleFullToggle}
      >
        <div className="flex items-center gap-2 flex-1">
          {hasChildren ? (
            <button onClick={toggleLevel}>
              {isExpanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
            </button>
          ) : (
            <div className="w-4" />
          )}

          <Icon className="h-4 w-4" />

          <span
            className="text-sm"
            // eslint-disable-next-line react/no-danger
            dangerouslySetInnerHTML={{ __html: highlightedName }}
          />
        </div>

        {/* -------- Admin Actions -------- */}
        {isAdmin && (
          <TooltipProvider>
            <div className="flex gap-1">

              {/* ADD */}
              {asset.level !== 5 && (
                <Tooltip>
                  <TooltipTrigger asChild>
                    <button
                      id="add-subasset-btn"  
                      className="p-1 rounded hover:bg-accent"
                      onClick={(e) => {
                        e.stopPropagation();
                        setAssetForAdd(asset);
                        setShowAddAssetModal(true);
                      }}
                    >
                      <Plus className="h-4 w-4" />
                    </button>
                  </TooltipTrigger>
                  <TooltipContent
                  side="top"
                  align="center"
                  className="bg-background text-popover-foreground border shadow-md rounded-md px-2 py-1">
                    Add Sub-Asset
                  </TooltipContent>
                </Tooltip>
              )}

              {/* EDIT */}
              <Tooltip>
                <TooltipTrigger asChild>
                  <button
                    id="edit-asset-btn"
                    className="p-1 rounded hover:bg-accent"
                    onClick={(e) => {
                      e.stopPropagation();
                      setAssetForEdit(asset);
                      setShowEditModal(true);
                    }}
                  >
                    <Edit className="h-4 w-4" />
                  </button>
                </TooltipTrigger>
                <TooltipContent
                  side="top"
                  align="center"
                  className="bg-background text-popover-foreground border shadow-md rounded-md px-2 py-1">
                    Edit
                </TooltipContent>
              </Tooltip>

              {/* DELETE */}
              {!hasChildren && (
                <Tooltip>
                  <TooltipTrigger asChild>
                    <button
                      id = "delete-asset-btn"
                      className="p-1 rounded bg-destructive/20 text-destructive hover:bg-destructive/30"
                      onClick={(e) => {
                        e.stopPropagation();
                        setAssetToDelete(asset);
                        setOpenDeleteDialog(true);
                      }}
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </TooltipTrigger>
                  <TooltipContent
                  side="top"
                  align="center"
                  className="bg-background text-popover-foreground border shadow-md rounded-md px-2 py-1">
                    Delete
                  </TooltipContent>
                </Tooltip>
              )}

              {/* SIGNAL CONFIG */}
              {asset.level > 2 && (
                <Tooltip>
                  <TooltipTrigger asChild>
                    <button
                      className="p-1 rounded hover:bg-accent"
                      onClick={(e) => {
                        e.stopPropagation();
                        setAssetForConfig(asset);
                        setShowConfigureModal(true);
                      }}
                    >
                      <Signal className="h-4 w-4" />
                    </button>
                  </TooltipTrigger>
                  <TooltipContent
                  side="top"
                  align="center"
                  className="bg-background text-popover-foreground border shadow-md rounded-md px-2 py-1">
                    Configure Signals
                  </TooltipContent>
                </Tooltip>
              )}

              {/* Show Signal */}
              {asset.level > 2 && (
                <Tooltip>
                  <TooltipTrigger asChild>
                    <button
                      className="p-1 rounded hover:bg-accent"
                      onClick={() => handleOpenAsset(asset)}
                    >
                      <Tv className="h-4 w-4" />
                    </button>
                  </TooltipTrigger>
                  <TooltipContent
                  side="top"
                  align="center"
                  className="bg-background text-popover-foreground border shadow-md rounded-md px-2 py-1">
                    Show Signals
                  </TooltipContent>
                </Tooltip>
              )}

            </div>
          </TooltipProvider>
        )}
      </div>

      {hasChildren && isExpanded && (
        <div className="ml-6">
          {asset.childrens.map((child) => (
            <AssetTreeNode
              key={child.assetId}
              asset={child}
              selectedId={selectedId}
              onSelect={onSelect}
              searchTerm={searchTerm}
              isAdmin={isAdmin}
              expandedMap={expandedMap}
              setExpandedMap={setExpandedMap}
              setShowAddAssetModal={setShowAddAssetModal}
              setAssetForAdd={setAssetForAdd}
              setShowEditModal={setShowEditModal}
              setAssetForEdit={setAssetForEdit}
              setOpenDeleteDialog={setOpenDeleteDialog}
              setAssetToDelete={setAssetToDelete}
              setAssetForConfig={setAssetForConfig}
              setShowConfigureModal={setShowConfigureModal}
            />
          ))}
        </div>
      )}
    </div>
  );
};

/* ---------------- Main Tree Component ---------------- */

export const AssetTree: React.FC<{
  assets: BackendAsset[];
  selectedId: string | null;
  onSelect: (a: BackendAsset) => void;
  onAdd: () => void;
  onDelete: (a: BackendAsset) => void;
}> = ({ assets, selectedId, onSelect, onAdd, onDelete }) => {
  
  const [searchTerm, setSearchTerm] = useState("");
  const [debounced, setDebounced] = useState("");

  /* ---------- Debounce search ---------- */
  useEffect(() => {
    const t = setTimeout(() => setDebounced(searchTerm), 250);
    return () => clearTimeout(t);
  }, [searchTerm]);

  const [expandedMap, setExpandedMap] = useState<Record<string, boolean>>({});

  /* ---------- Expand entire subtree (kept for backward compatibility but not required) ---------- */

  const expandWholeSubtree = useCallback((id: string) => {
    const root = findNode(assets, id);
    if (!root) return;

    setExpandedMap(prev => {
      const newMap = { ...prev };
      const rec = (n: BackendAsset) => {
        newMap[n.assetId] = true;
        n.childrens.forEach(rec);
      };
      rec(root);
      return newMap;
    });
  }, [assets]);

  /* ---------- Collapse entire subtree (kept for backward compatibility) ---------- */

  const collapseWholeSubtree = useCallback((id: string) => {
    const root = findNode(assets, id);
    if (!root) return;

    setExpandedMap(prev => {
      const newMap = { ...prev };
      const rec = (n: BackendAsset) => {
        delete newMap[n.assetId];
        n.childrens.forEach(rec);
      };
      rec(root);
      return newMap;
    });
  }, [assets]);

  /* ---------- Filtered tree ---------- */

  const filteredAssets = useMemo(
    () => filterTreeBySearch(assets, debounced),
    [assets, debounced]
  );

  /* ---------- Modals ---------- */

  const [showAddRootModal, setShowAddRootModal] = useState(false);
  const [showAddAssetModal, setShowAddAssetModal] = useState(false);
  const [assetForAdd, setAssetForAdd] = useState<any>(null);
  const [showEditModal, setShowEditModal] = useState(false);
  const [assetForEdit, setAssetForEdit] = useState<any>(null);
  const [openDeleteDialog, setOpenDeleteDialog] = useState(false);
  const [assetToDelete, setAssetToDelete] = useState<any>(null);
  const [showConfigureModal, setShowConfigureModal] = useState(false);
  const [assetForConfig, setAssetForConfig] = useState<any>(null);

  const { user, loading } = useAuth();
  const isAdmin = user?.role?.toLowerCase() === "admin";

  if (loading) return <Spinner />;

  return (
    <div className="h-full flex flex-col">
      
      {/* Header */}
      <div className="p-4 border-b flex flex-col gap-2">
        <div className="flex justify-between items-center">
          <h2 className="text-lg font-semibold">Asset Tree</h2>
          {isAdmin && (
            <Button  id="add-root-btn"  size="sm" onClick={() => setShowAddRootModal(true)}>
              <Plus className="h-4 w-4" /> Add Root
            </Button>
          )}
        </div>

        <Input
          placeholder="Search assets..."
          id="search-asset"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
      </div>

      {/* Tree */}
      <div className="flex-1 overflow-auto p-2">
        {filteredAssets.length === 0 ? (
          <p className="text-center text-sm text-gray-500">No assets found</p>
        ) : (
          filteredAssets.map((a) => (
            <AssetTreeNode
              key={a.assetId}
              asset={a}
              selectedId={selectedId}
              onSelect={onSelect}
              searchTerm={debounced}
              isAdmin={isAdmin}
              expandedMap={expandedMap}
              setExpandedMap={setExpandedMap}
              setShowAddAssetModal={setShowAddAssetModal}
              setAssetForAdd={setAssetForAdd}
              setShowEditModal={setShowEditModal}
              setAssetForEdit={setAssetForEdit}
              setOpenDeleteDialog={setOpenDeleteDialog}
              setAssetToDelete={setAssetToDelete}
              setShowConfigureModal={setShowConfigureModal}
              setAssetForConfig={setAssetForConfig}
            />
          ))
        )}
      </div>

      {/* Modals */}
      {showConfigureModal && assetForConfig && (
        <ConfigureAsset asset={assetForConfig} onClose={() => setShowConfigureModal(false)} />
      )}

      {showAddRootModal && (
        <Addroot onClose={() => setShowAddRootModal(false)} onAdd={onAdd} />
      )}
      {showAddAssetModal && assetForAdd && (
        <Addasset parentAsset={assetForAdd} onClose={() => setShowAddAssetModal(false)} onAdd={onAdd} />
      )}

      {showEditModal && assetForEdit && (
        <Editasset
          asset={assetForEdit}
          onClose={() => setShowEditModal(false)}
          onUpdated={() => {
            onAdd();
            setShowEditModal(false);
          }}
        />
      )}

      <DeleteAsset
        asset={assetToDelete}
        open={openDeleteDialog}
        onClose={() => setOpenDeleteDialog(false)}
        onDeleted={() => {
          if (assetToDelete) onDelete(assetToDelete);
          toast.success(`Deleted successfully`);
          setOpenDeleteDialog(false);
        }}
      />
    </div>
  );
};

export default AssetTree;
