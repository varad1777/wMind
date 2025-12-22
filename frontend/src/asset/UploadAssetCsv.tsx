import React, { useEffect, useRef, useState } from "react";
import Papa from "papaparse";
import * as XLSX from "xlsx";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import apiAsset from "@/api/axiosAsset";
import { toast } from "react-toastify";


type ParsedRow = Record<string, unknown> & { __rowNum?: number };

type Asset = {
  assetName: string;
  parentName?: string | null;
  level: number;
  sourceRows: number[];
};

type FieldError = {
  assetIndex: number;
  field: "assetName" | "parentName" | "level";
  messages: string[];
  rowInfo?: string;
};

type ApiResponse = {
  addedAssets: string[];
  skippedAssets: string[];
};

const ASSET_NAME_RE = /^[A-Za-z0-9 _-]+$/;

export default function AssetBulkUpload() {
  const [rows, setRows] = useState<ParsedRow[]>([]);
  const [assets, setAssets] = useState<Asset[]>([]);
  const [globalErrors, setGlobalErrors] = useState<string[]>([]);
  const [fieldErrors, setFieldErrors] = useState<FieldError[]>([]);
  const [dragOver, setDragOver] = useState(false);
  const [saving, setSaving] = useState(false);
  const [apiResponse, setApiResponse] = useState<ApiResponse | null>(null);

  const fileInputRef = useRef<HTMLInputElement | null>(null);

  /** Normalize column headers */
  const normalizeKey = (k: string | undefined) =>
    String(k || "")
      .replace(/\s+/g, "")
      .replace(/[^a-zA-Z0-9]/g, "")
      .toLowerCase();

  /** Convert rows to assets + deduplicate while merging rows */
  function dedupAndMap(parsedRows: ParsedRow[]): Asset[] {
    const map = new Map<string, Asset>();

    for (const r of parsedRows) {
      const normalized: Record<string, unknown> = {};

      for (const k of Object.keys(r)) {
        normalized[normalizeKey(k)] = r[k];
      }

      const assetName = String(normalized["assetname"] ?? "").trim();
      const parentName = String(normalized["parentname"] ?? "").trim();
      const level = Number(normalized["level"] ?? 0);
      const rowNum = r.__rowNum ?? null;

      if (!assetName) continue;

      const key = assetName.toLowerCase();

      if (!map.has(key)) {
        map.set(key, {
          assetName,
          parentName: parentName || null,
          level: isNaN(level) ? 0 : level,
          sourceRows: rowNum ? [rowNum] : [],
        });
      } else {
        map.get(key)!.sourceRows.push(rowNum!);
      }
    }

    return Array.from(map.values());
  }

  /** Validation */
  function validate(astList: Asset[]) {
    const global: string[] = [];
    const flatErrors: FieldError[] = [];
    const seen = new Map<string, number>();

    if (astList.length > 20) {
      global.push(`Maximum 20 assets allowed (found ${astList.length})`);
    }

    astList.forEach((a, i) => {
      const rowInfo =
        a.sourceRows.length ? `Rows: ${a.sourceRows.join(",")}` : "";

      // Asset name required
      if (!a.assetName.trim()) {
        flatErrors.push({
          assetIndex: i,
          field: "assetName",
          messages: ["AssetName is required"],
          rowInfo,
        });
      }

      // Length validation
      if (a.assetName.length < 3 || a.assetName.length > 100) {
        flatErrors.push({
          assetIndex: i,
          field: "assetName",
          messages: ["AssetName must be 3-100 characters"],
          rowInfo,
        });
      }

      // Character validation
      if (!ASSET_NAME_RE.test(a.assetName)) {
        flatErrors.push({
          assetIndex: i,
          field: "assetName",
          messages: ["AssetName contains invalid characters"],
          rowInfo,
        });
      }

      // Level validation
      if (a.level <= 0) {
        flatErrors.push({
          assetIndex: i,
          field: "level",
          messages: ["Level must be greater than 0"],
          rowInfo,
        });
      }

      if (!Number.isInteger(a.level)) {
        flatErrors.push({
          assetIndex: i,
          field: "level",
          messages: ["Level must be an integer"],
          rowInfo,
        });
      }

      // Duplicate asset detection
      const key = a.assetName.toLowerCase();
      if (seen.has(key)) {
        flatErrors.push({
          assetIndex: i,
          field: "assetName",
          messages: [`Duplicate asset name (row ${seen.get(key)! + 2})`],
          rowInfo,
        });
      } else {
        seen.set(key, i);
      }
    });

    return { global, fieldErrors: flatErrors };
  }

  /** When assets change → revalidate */
  useEffect(() => {
    const { global, fieldErrors } = validate(assets);
    setGlobalErrors(global);
    setFieldErrors(fieldErrors);
  }, [assets]);

  /** File picker */
  const openFilePicker = () => fileInputRef.current?.click();

  const REQUIRED_HEADERS = ["assetname", "level"];

  function hasRequiredHeaders(data: Record<string, unknown>[]) {
    if (!data.length) return false;

    const headers = Object.keys(data[0]).map((h) =>
      h.replace(/\s+/g, "").replace(/[^a-zA-Z0-9]/g, "").toLowerCase()
    );

    return REQUIRED_HEADERS.every((h) => headers.includes(h));
    }


  
  /** File processing */
function handleFile(file: File) {
  const fileName = file.name.toLowerCase();

  const processRows = (data: Record<string, unknown>[]) => {
    if (!data.length) {
      toast.error("Uploaded file is empty");
      return;
    }

    if (!hasRequiredHeaders(data)) {
      toast.error(
        "Invalid file format. Required columns: AssetName, ParentName, Level"
      );
      return;
    }

    const annotated = data.map((r, i) => ({
      __rowNum: i + 2,
      ...r,
    }));

    const parsedAssets = dedupAndMap(annotated);

    if (!parsedAssets.length) {
      toast.error("No valid assets found in file");
      return;
    }

    setRows(annotated);
    setAssets(parsedAssets);

    toast.success(`${parsedAssets.length} assets uploaded successfully`);
  };

  // ✅ CSV ONLY
  if (fileName.endsWith(".csv")) {
    Papa.parse(file, {
      header: true,
      skipEmptyLines: true,
      complete: (res) => {
        if (res.errors?.length) {
          toast.error("Error parsing CSV file");
          return;
        }
        processRows(res.data as Record<string, unknown>[]);
      },
      error: () => toast.error("Failed to read CSV file"),
    });
    return;
  }

  // ✅ EXCEL ONLY
  if (fileName.endsWith(".xlsx") || fileName.endsWith(".xls")) {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const buffer = e.target?.result as ArrayBuffer;
        const workbook = XLSX.read(new Uint8Array(buffer), { type: "array" });
        const sheet = workbook.Sheets[workbook.SheetNames[0]];
        const json = XLSX.utils.sheet_to_json(sheet, { defval: "" });
        processRows(json as Record<string, unknown>[]);
      } catch {
        toast.error("Error reading Excel file");
      }
    };
    reader.readAsArrayBuffer(file);
    return;
  }

  // ❌ INVALID FILE
  toast.error("Invalid file format. Please upload CSV or Excel");
}

  /** Save to API */
  async function handleSave() {
    const { global, fieldErrors } = validate(assets);

    if (global.length || fieldErrors.length) return;

    setSaving(true);

    try {
      const response = await apiAsset.post("/AssetHierarchy/bulk-upload", {
        assets: assets.map((a) => ({
          assetName: a.assetName.trim(),
          parentName: a.parentName?.trim() ?? null,
          level: a.level,
        })),
      });

      setApiResponse(response.data);
      setRows([]);
      setAssets([]);
      setGlobalErrors([]);
      setFieldErrors([]);
    } catch (err: any) {
      setGlobalErrors([err.message || "Error saving assets"]);
    } finally {
      setSaving(false);
    }
  }

  return (
    <Card className="p-4">
      <CardHeader>
        <CardTitle className="text-sm text-foreground">
          Asset Bulk Upload
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* File Drop */}
        <div
          onDrop={(e) => {
            e.preventDefault();
            setDragOver(false);
            const f = e.dataTransfer.files[0];
            if (f) handleFile(f);
          }}
          onDragOver={(e) => {
            e.preventDefault();
            setDragOver(true);
          }}
          onDragLeave={() => setDragOver(false)}
          className={`rounded-lg p-4 flex items-center justify-between transition-shadow border cursor-pointer 
          ${dragOver ? "shadow-lg border-primary/40 bg-primary/5" : "bg-card"}`}
        >
          <div className="leading-tight">
            <div className="text-sm font-medium text-foreground">
              Upload CSV / Excel
            </div>
            <div className="text-xs text-muted-foreground mt-1">
              Columns: <strong>AssetName</strong>,{" "}
              <strong>ParentName</strong>, <strong>Level</strong>
            </div>
            <div className="text-xs text-muted-foreground mt-1">
              Drag & drop or click “Choose file”
            </div>
          </div>

          <div className="flex items-center gap-3">
            <input
              ref={fileInputRef}
              type="file"
              accept=".csv,.xlsx,.xls"
              className="hidden"
              onChange={(e) => {
                const f = e.target.files?.[0];
                if (f) handleFile(f);
                if (fileInputRef.current) fileInputRef.current.value = "";
              }}
            />
            <Button size="sm" onClick={openFilePicker}>
              Choose file
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                setRows([]);
                setAssets([]);
                setGlobalErrors([]);
                setFieldErrors([]);
                setApiResponse(null);
              }}
            >
              Clear
            </Button>
          </div>
        </div>

        <Separator />

        {/* Global Errors */}
        {globalErrors.length > 0 && (
          <div className="p-3 bg-destructive/10 border border-destructive/30 text-destructive rounded text-sm">
            <ul className="list-disc ml-5">
              {globalErrors.map((e, i) => (
                <li key={i}>{e}</li>
              ))}
            </ul>
          </div>
        )}

        {/* Field-level Errors */}
        <div>
          {fieldErrors.length > 0 ? (
            <div className="p-3 bg-warning/10 border border-warning/30 rounded text-sm">
              <div className="font-medium mb-2 text-warning">
                Validation issues
              </div>
              <ScrollArea className="h-auto overflow-auto">
                <ul className="space-y-2">
                  {fieldErrors.map((fe, i) => (
                    <li
                      key={i}
                      className="p-2 bg-card border rounded shadow-sm"
                    >
                      <div className="text-xs text-muted-foreground">
                        {fe.rowInfo}
                      </div>
                      <div className="font-medium text-foreground">
                        Field: {fe.field}
                      </div>
                      <div className="text-xs text-red-700 mt-1">
                        {fe.messages.map((m, i2) => (
                          <div key={i2}>• {m}</div>
                        ))}
                      </div>
                    </li>
                  ))}
                </ul>
              </ScrollArea>
            </div>
          ) : (
            <div className="p-3 bg-emerald-50/20 border border-emerald-500/20 text-emerald-600 rounded text-sm">
              {assets.length > 0
                ? "🎉 CSV is ready to upload."
                : "Upload a CSV/XLSX to validate."}
            </div>
          )}
        </div>

        {/* Save Button */}
        <div className="flex justify-end gap-2">
          <Button onClick={handleSave} disabled={assets.length === 0 || saving}>
            {saving ? "Saving..." : "Save Assets"}
          </Button>
        </div>

        {/* API Response */}
        {apiResponse && (
          <div className="space-y-3 mt-4">
            {apiResponse.addedAssets?.length > 0 && (
              <div className="p-3 bg-emerald-50/20 border border-emerald-500/20 rounded text-sm">
                <div className="font-medium text-emerald-600 mb-1">
                  ✅ Successfully Added ({apiResponse.addedAssets.length})
                </div>
                <ScrollArea className="max-h-40">
                  <ul className="list-disc ml-5 text-emerald-800">
                    {apiResponse.addedAssets.map((msg, i) => (
                      <li key={i}>{msg}</li>
                    ))}
                  </ul>
                </ScrollArea>
              </div>
            )}

            {apiResponse.skippedAssets?.length > 0 && (
              <div className="p-3 bg-destructive/10 border border-destructive/30 rounded text-sm">
                <div className="font-medium text-destructive mb-1">
                  ⚠️ Skipped ({apiResponse.skippedAssets.length})
                </div>
                <ScrollArea className="max-h-40">
                  <ul className="list-disc ml-5 text-red-800">
                    {apiResponse.skippedAssets.map((msg, i) => (
                      <li key={i}>{msg}</li>
                    ))}
                  </ul>
                </ScrollArea>
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
