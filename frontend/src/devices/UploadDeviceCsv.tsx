import React, { useEffect, useRef, useState } from "react";
import Papa from "papaparse";
import * as XLSX from "xlsx";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { ScrollArea } from "@/components/ui/scroll-area";
import { cn } from "@/lib/utils";
import api from "@/api/axios";

const DEVICE_NAME_RE = /^[A-Za-z0-9 _-]+$/;

type ParsedRow = Record<string, unknown> & { __rowNum?: number };
type Device = { name: string; description?: string | null; sourceRows: number[] };
type FieldError = { deviceIndex: number; field: "name" | "description"; messages: string[]; rowInfo?: string };

export default function DeviceBulkUpload() {
  const [rows, setRows] = useState<ParsedRow[]>([]);
  const [devices, setDevices] = useState<Device[]>([]);
  const [globalErrors, setGlobalErrors] = useState<string[]>([]);
  const [fieldErrors, setFieldErrors] = useState<FieldError[]>([]);
  const [saving, setSaving] = useState(false);
  const [dragOver, setDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  function normalizeKey(k: string | undefined) {
    return String(k || "").replace(/\s+/g, "").toLowerCase();
  }

  function dedupAndMap(parsedRows: ParsedRow[]): Device[] {
    const map = new Map<string, Device>();
    for (const r of parsedRows) {
      const norm: Record<string, unknown> = {};
      for (const k of Object.keys(r)) norm[normalizeKey(k)] = r[k as keyof ParsedRow];
      const name = String((norm["devicename"] ?? "")).trim();
      const desc = String((norm["devicedescription"] ?? "")).trim();
      const rowNum = r.__rowNum ?? null;
      if (!name) continue;
      const key = name.toLowerCase();
      if (!map.has(key)) map.set(key, { name, description: desc || null, sourceRows: rowNum ? [rowNum] : [] });
      else map.get(key)!.sourceRows.push(rowNum!);
    }
    return Array.from(map.values());
  }

  function handleFile(file: File) {
    const name = file.name.toLowerCase();

    const processRows = (data: Record<string, unknown>[]) => {
      const annotated = data.map((r, i) => ({ __rowNum: i + 2, ...r }));
      setRows(annotated);
      setDevices(dedupAndMap(annotated));
      setGlobalErrors([]);
      setFieldErrors([]);
    };

    if (name.endsWith(".csv")) {
      file.text()
        .then(txt => {
          const parsed = Papa.parse<{ [k: string]: string }>(txt, { header: true, skipEmptyLines: true });
          processRows(parsed.data);
        })
        .catch(e => setGlobalErrors([String(e)]));
      return;
    }

    if (name.endsWith(".xlsx") || name.endsWith(".xls")) {
      const reader = new FileReader();
      reader.onload = (e) => {
        try {
          const ab = e.target?.result as ArrayBuffer;
          const wb = XLSX.read(new Uint8Array(ab), { type: "array" });
          const sheet = wb.Sheets[wb.SheetNames[0]];
          const json: Record<string, unknown>[] = XLSX.utils.sheet_to_json(sheet, { defval: "" });
          processRows(json);
        } catch (err) {
          setGlobalErrors([String(err)]);
        }
      };
      reader.readAsArrayBuffer(file);
      return;
    }

    setGlobalErrors(["Unsupported file type. Upload CSV or Excel (.xlsx)"]);
  }

  function validate(devs: Device[]) {
    const global: string[] = [];
    const flatFieldErrors: FieldError[] = [];
    const seen = new Map<string, number>();

    if (devs.length > 20) global.push(`Device count exceeds maximum of 20 (got ${devs.length})`);

    devs.forEach((d, i) => {
      const errors: { name: string[]; description: string[]; combined: string[] } = { name: [], description: [], combined: [] };
      const nameTrim = (d.name ?? "").trim();
      if (!nameTrim) errors.name.push("DeviceName is required");
      else {
        if (nameTrim.length < 3 || nameTrim.length > 100) errors.name.push("3-100 chars");
        if (!DEVICE_NAME_RE.test(nameTrim)) errors.name.push("Allowed: letters, numbers, space, _ and -");
      }

      const desc = d.description ?? "";
      if (desc && desc.length > 255) errors.description.push("Max 255 chars");

      const key = nameTrim.toLowerCase();
      if (key) {
        if (seen.has(key)) errors.combined.push(`Duplicate device name (also found at index ${seen.get(key)! + 1})`);
        else seen.set(key, i);
      }

      const rowInfo = d.sourceRows.length ? ` (source rows: ${d.sourceRows.join(",")})` : "";
      if (errors.name.length) flatFieldErrors.push({ deviceIndex: i, field: "name", messages: errors.name, rowInfo });
      if (errors.description.length) flatFieldErrors.push({ deviceIndex: i, field: "description", messages: errors.description, rowInfo });
      if (errors.combined.length) flatFieldErrors.push({ deviceIndex: i, field: "name", messages: errors.combined, rowInfo });
    });

    return { global, fieldErrors: flatFieldErrors } as const;
  }

  useEffect(() => {
    const { global, fieldErrors: fe } = validate(devices);
    setGlobalErrors(global);
    setFieldErrors(fe);
  }, [devices]);

  const openFilePicker = () => fileInputRef.current?.click();

  async function handleSave() {
    const { global, fieldErrors: fe } = validate(devices);
    setGlobalErrors(global);
    setFieldErrors(fe);
    if (global.length || fe.length) return;

    setSaving(true);
    try {
      const payload = devices.map(d => ({ name: d.name.trim(), description: d.description?.trim() ?? null }));
      const res = await api.post("/devices/bulk", { devices: payload });

      // Backend errors are returned even with status 400, so handle via res.data
      const data = res.data as { createdDeviceIds?: string[]; errors?: string[] };

      if (data.errors && data.errors.length > 0) {
        const backendFieldErrors: FieldError[] = [];
        const backendGlobalErrors: string[] = [];

        data.errors.forEach(errStr => {
          const match = errStr.match(/^Device '(.+?)': (.+)$/);
          if (match) {
            const [_, name, message] = match;
            const idx = devices.findIndex(d => d.name.toLowerCase() === name.toLowerCase());
            if (idx >= 0) {
              backendFieldErrors.push({
                deviceIndex: idx,
                field: "name",
                messages: [message],
                rowInfo: devices[idx].sourceRows.length ? `rows: ${devices[idx].sourceRows.join(",")}` : undefined,
              });
            } else backendGlobalErrors.push(message);
          } else backendGlobalErrors.push(errStr);
        });

        setFieldErrors(prev => [...prev, ...backendFieldErrors]);
        setGlobalErrors(prev => [...prev, ...backendGlobalErrors]);
        return;
      }

      // Success
      setRows([]);
      setDevices([]);
      setGlobalErrors([]);
      setFieldErrors([]);
    } catch (err: any) {
      if (err.response?.data?.errors) {
        // Handle backend errors from 4xx/5xx responses
        const data = err.response.data as { createdDeviceIds?: string[]; errors?: string[] };
        const backendFieldErrors: FieldError[] = [];
        const backendGlobalErrors: string[] = [];

        data.errors.forEach(errStr => {
          const match = errStr.match(/^Device '(.+?)': (.+)$/);
          if (match) {
            const [_, name, message] = match;
            const idx = devices.findIndex(d => d.name.toLowerCase() === name.toLowerCase());
            if (idx >= 0) {
              backendFieldErrors.push({
                deviceIndex: idx,
                field: "name",
                messages: [message],
                rowInfo: devices[idx].sourceRows.length ? `rows: ${devices[idx].sourceRows.join(",")}` : undefined,
              });
            } else backendGlobalErrors.push(message);
          } else backendGlobalErrors.push(errStr);
        });

        setFieldErrors(prev => [...prev, ...backendFieldErrors]);
        setGlobalErrors(prev => [...prev, ...backendGlobalErrors]);
      } else {
        setGlobalErrors([err.message || "Unknown error occurred"]);
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <Card className="p-4">
      <CardHeader>
        <CardTitle className="text-sm">Device Bulk Upload</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div
          onDrop={e => { e.preventDefault(); setDragOver(false); const f = e.dataTransfer.files[0]; f && handleFile(f); }}
          onDragOver={e => { e.preventDefault(); setDragOver(true); }}
          onDragLeave={() => setDragOver(false)}
          className={cn(
          "rounded-lg p-4 flex items-center justify-between transition-shadow border cursor-pointer",
          dragOver
            ? "shadow-lg border-primary/40 bg-primary/5"
            : "bg-card"
        )}
        >
        <div className="leading-tight">
          <div className="text-sm font-medium text-foreground">
            Upload CSV / Excel
          </div>

          <div className="text-xs text-muted-foreground mt-1">
            Columns: <span className="font-semibold">DeviceName</span>,{" "}
            <span className="font-semibold">DeviceDescription</span>
          </div>

          <div className="text-xs text-muted-foreground mt-1">
            Drag & drop or click “Choose file”
          </div>
        </div>
          <div className="flex items-center gap-3">
            <input ref={fileInputRef} type="file" accept=".csv,.xlsx,.xls" className="hidden"
              onChange={e => { const f = e.target.files?.[0]; f && handleFile(f); if (fileInputRef.current) fileInputRef.current.value = ""; }} />
            <Button size="sm" onClick={openFilePicker}>Choose file</Button>
            <Button variant="ghost" size="sm" onClick={() => { setRows([]); setDevices([]); setGlobalErrors([]); setFieldErrors([]); }}>Clear</Button>
          </div>
        </div>

        <Separator />

        {globalErrors.length > 0 && (
           <div className="p-3 bg-destructive/10 border border-destructive/30 text-destructive rounded text-sm">
             <ul className="list-disc ml-5">
              {globalErrors.map((e, i) => <li key={i}>{e}</li>)}
            </ul>
          </div>
        )}

        <div>
          {fieldErrors.length > 0 ? (
            <div className="p-3 bg-warning/10 border border-warning/30 rounded text-sm">
            <div className="font-medium mb-2 text-warning">Validation issues</div>
              <ScrollArea className="h-auto overflow-auto">
                <ul className="space-y-2">
                  {fieldErrors.map((fe, i) => (
                    <li key={i} className="p-2 bg-white border rounded shadow-sm">
                      <div className="text-xs text-muted-foreground">
                        {fe.rowInfo || devices[fe.deviceIndex]?.sourceRows?.join(",") || "?"}
                      </div>
                      <div className="font-medium text-foreground">Field: {fe.field}</div>
                      <div className="text-xs text-red-700 mt-1">
                        {fe.messages.map((m, i2) => <div key={i2}>• {m}</div>)}
                      </div>
                    </li>
                  ))}
                </ul>
              </ScrollArea>
            </div>
          ) : (
            <div className="p-3 bg-emerald-50/20 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 rounded text-sm">
            {devices.length > 0
              ? "🎉 CSV is ready to upload."
              : "Upload a CSV/XLSX to validate."}
          </div>
          )}
        </div>

        <div className="flex justify-end gap-2">
          <Button onClick={handleSave} disabled={devices.length === 0 || saving}>
            {saving ? "Saving..." : "Save Devices"}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
