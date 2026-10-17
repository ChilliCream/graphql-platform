"use client";

import { useRef, useState } from "react";
import {
  parseBenchmarkHistory,
  type BenchmarkHistory,
} from "@/src/helpers/benchmarkReport";

interface BenchmarkHistorySourceMenuProps {
  readonly onChange: (history: BenchmarkHistory | undefined) => void;
}

const SOURCES = [
  { id: "live", label: "Actual history" },
  { id: "one-week", label: "Mock · 1 week" },
  { id: "two-months", label: "Mock · 2 months" },
  { id: "six-months", label: "Mock · 6 months" },
  { id: "cross-year", label: "Mock · Dec 2025 – Feb 2026" },
] as const;

export function BenchmarkHistorySourceMenu({
  onChange,
}: BenchmarkHistorySourceMenuProps) {
  const [source, setSource] = useState("live");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);
  const request = useRef<AbortController | undefined>(undefined);
  const cache = useRef(new Map<string, BenchmarkHistory>());

  async function selectSource(id: string) {
    request.current?.abort();
    const controller = new AbortController();
    request.current = controller;
    setSource(id);
    setError(false);
    setLoading(false);
    if (id === "live") {
      onChange(undefined);
      return;
    }
    const existing = cache.current.get(id);
    if (existing) {
      onChange(existing);
      return;
    }
    setLoading(true);
    try {
      const response = await fetch(`/benchmarks/history-${id}.json`, {
        signal: controller.signal,
      });
      if (!response.ok) {
        throw new Error("History unavailable");
      }
      const history = parseBenchmarkHistory(await response.text());
      if (controller.signal.aborted) {
        return;
      }
      cache.current.set(id, history);
      onChange(history);
    } catch {
      if (!controller.signal.aborted) {
        setError(true);
        onChange(undefined);
        setSource("live");
      }
    } finally {
      if (!controller.signal.aborted) {
        setLoading(false);
      }
    }
  }

  return (
    <aside
      aria-label="History showcase"
      className="bg-cc-bg text-cc-heading border-cc-card-border fixed right-3 bottom-3 z-50 max-w-[calc(100vw-1.5rem)] rounded-lg border p-3 shadow-xl sm:right-5 sm:bottom-5"
    >
      <label
        htmlFor="benchmark-history-source"
        className="mb-1 block text-xs font-semibold"
      >
        History showcase
      </label>
      <select
        id="benchmark-history-source"
        className="bg-cc-bg focus-visible:outline-cc-accent rounded border border-current/20 px-2 py-1 text-sm"
        value={source}
        onChange={(event) => void selectSource(event.target.value)}
      >
        {SOURCES.map((option) => (
          <option key={option.id} value={option.id}>
            {option.label}
          </option>
        ))}
      </select>
      <p role="status" className="text-cc-ink-dim mt-1 text-xs">
        {loading
          ? "Loading history…"
          : error
            ? "Could not load mock data. Showing actual history."
            : source === "live"
              ? "Charts show actual benchmark runs."
              : "Charts show mock data. Results above stay actual."}
      </p>
    </aside>
  );
}
