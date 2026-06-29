"use client";

import { Reorder } from "motion/react";
import { useEffect, useRef, useState } from "react";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";

import { SECTIONS, TRACK_ORDER, type SectionEntry } from "./registry";

const STORAGE_KEY = "landing-customizer-v2";

/** The current default landing: Nitro, the animated Messaging take, then the
 * combined agentic / observability / governance sections, in this order. */
const DEFAULT_ENABLED = [
  "nitro-v2",
  "messaging-events-animated",
  "combined-agentic",
  "combined-observability",
  "combined-governance",
];

function byId(id: string) {
  return SECTIONS.find((s) => s.id === id);
}

/* ── tiny inline icons (decorative, inherit currentColor) ──────────────────── */

function SlidersIcon({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M4 6h10M18 6h2M4 12h2M10 12h10M4 18h12M20 18h0M16 18h0" />
      <circle cx="16" cy="6" r="2" />
      <circle cx="8" cy="12" r="2" />
      <circle cx="14" cy="18" r="2" />
    </svg>
  );
}

function CloseIcon({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M6 6l12 12M18 6L6 18" />
    </svg>
  );
}

function GripIcon({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      aria-hidden="true"
      className={className}
    >
      <circle cx="9" cy="6" r="1.4" />
      <circle cx="15" cy="6" r="1.4" />
      <circle cx="9" cy="12" r="1.4" />
      <circle cx="15" cy="12" r="1.4" />
      <circle cx="9" cy="18" r="1.4" />
      <circle cx="15" cy="18" r="1.4" />
    </svg>
  );
}

/* ── hover preview ─────────────────────────────────────────────────────────── */

// The section is rendered at a real desktop width and scaled down, so the popup
// is a faithful thumbnail. Height is measured from the rendered section so the
// popup hugs its content instead of reserving the full unscaled height.
const PREVIEW_RENDER_W = 1280;
const PREVIEW_W = 480;
const PREVIEW_SCALE = PREVIEW_W / PREVIEW_RENDER_W;

function SectionPreview({ entry }: { readonly entry: SectionEntry }) {
  const innerRef = useRef<HTMLDivElement>(null);
  const [innerH, setInnerH] = useState(0);
  const Section = entry.Component;

  useEffect(() => {
    const el = innerRef.current;
    if (!el) {
      return;
    }
    const ro = new ResizeObserver(() => setInnerH(el.offsetHeight));
    ro.observe(el);
    return () => ro.disconnect();
  }, [entry.id]);

  return (
    <div
      className="border-cc-card-border bg-cc-bg pointer-events-none fixed bottom-6 z-[55] hidden overflow-hidden rounded-2xl border shadow-2xl shadow-black/60 lg:block"
      style={{ right: "calc(1.5rem + 22rem + 1rem)", width: PREVIEW_W }}
    >
      <div className="border-cc-card-border bg-cc-card-bg/80 flex items-center gap-2 border-b px-3 py-2 backdrop-blur">
        <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.14em] uppercase">
          {entry.track}
        </span>
        <span className="text-cc-ink truncate text-xs">{entry.label}</span>
        <span className="text-cc-ink-dim ml-auto font-mono text-[0.55rem] tracking-[0.12em] uppercase">
          Preview
        </span>
      </div>
      <div
        style={{
          height: innerH ? innerH * PREVIEW_SCALE : 280,
          maxHeight: "72vh",
          overflow: "hidden",
        }}
      >
        <div
          ref={innerRef}
          style={{
            width: PREVIEW_RENDER_W,
            transform: `scale(${PREVIEW_SCALE})`,
            transformOrigin: "top left",
          }}
        >
          <Section />
        </div>
      </div>
    </div>
  );
}

/* ── customizer ────────────────────────────────────────────────────────────── */

export function LandingCustomizer() {
  const [open, setOpen] = useState(false);
  const [enabled, setEnabled] = useState<string[]>(DEFAULT_ENABLED);
  const [hoveredId, setHoveredId] = useState<string | null>(null);
  const hydratedRef = useRef(false);

  // Load saved config after mount (server and first client render both use
  // DEFAULT_ENABLED, then we reconcile from localStorage to avoid a hydration
  // mismatch). The ref gates the persist effect so we never overwrite a stored
  // config with the default before this read runs.
  useEffect(() => {
    let stored: string[] | null = null;
    try {
      const raw = window.localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const ids = JSON.parse(raw) as unknown;
        if (Array.isArray(ids)) {
          const known = new Set(SECTIONS.map((s) => s.id));
          stored = ids.filter(
            (id): id is string => typeof id === "string" && known.has(id),
          );
        }
      }
    } catch {
      // ignore malformed storage
    }
    hydratedRef.current = true;
    if (stored) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- one-time hydration from localStorage
      setEnabled(stored);
    }
  }, []);

  useEffect(() => {
    if (!hydratedRef.current) {
      return;
    }
    try {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(enabled));
    } catch {
      // ignore quota / privacy-mode errors
    }
  }, [enabled]);

  const enabledSet = new Set(enabled);
  const available = SECTIONS.filter((s) => !enabledSet.has(s.id));
  const hoveredEntry = hoveredId ? byId(hoveredId) : undefined;

  function closeAll() {
    setOpen(false);
    setHoveredId(null);
  }
  function addSection(id: string) {
    setEnabled((prev) => (prev.includes(id) ? prev : [...prev, id]));
  }
  function removeSection(id: string) {
    setEnabled((prev) => prev.filter((x) => x !== id));
  }

  return (
    <>
      {/* ── the assembled landing ── */}
      <ProtocolCards />

      {enabled.length === 0 ? (
        <div className="mx-auto max-w-7xl px-5 py-24 text-center sm:px-12">
          <p className="text-cc-ink-dim text-sm">
            No sections enabled. Open the customizer (bottom right) to add and
            order sections.
          </p>
        </div>
      ) : (
        enabled.map((id) => {
          const entry = byId(id);
          if (!entry) {
            return null;
          }
          const Section = entry.Component;
          return <Section key={id} />;
        })
      )}

      <NitroPricing />

      {/* ── hover preview, to the left of the panel ── */}
      {open && hoveredEntry && <SectionPreview entry={hoveredEntry} />}

      {/* ── floating toggle ── */}
      <button
        type="button"
        onClick={() => {
          setOpen((o) => !o);
          setHoveredId(null);
        }}
        aria-expanded={open}
        aria-label={
          open ? "Close landing customizer" : "Open landing customizer"
        }
        className="bg-cc-accent text-cc-bg hover:bg-cc-accent-hover fixed right-6 bottom-6 z-[60] flex size-14 items-center justify-center rounded-full shadow-lg shadow-black/40 transition-colors"
      >
        {open ? (
          <CloseIcon className="size-6" />
        ) : (
          <SlidersIcon className="size-6" />
        )}
      </button>

      {/* ── configurator panel ── */}
      {open && (
        <div className="border-cc-card-border bg-cc-card-bg/95 fixed right-6 bottom-24 z-[60] flex max-h-[75vh] w-[22rem] max-w-[calc(100vw-3rem)] flex-col overflow-hidden rounded-2xl border shadow-2xl shadow-black/50 backdrop-blur">
          <div className="border-cc-card-border flex items-center justify-between border-b px-4 py-3">
            <div>
              <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.2em] uppercase">
                Customize landing
              </p>
              <p className="text-cc-ink-dim mt-0.5 text-xs">
                {enabled.length} of {SECTIONS.length} enabled
              </p>
            </div>
            <button
              type="button"
              onClick={closeAll}
              aria-label="Close"
              className="text-cc-ink-dim hover:text-cc-heading rounded-md p-1 transition-colors"
            >
              <CloseIcon className="size-4" />
            </button>
          </div>

          <div
            className="flex-1 overflow-y-auto px-4 py-3"
            onMouseLeave={() => setHoveredId(null)}
          >
            {/* enabled, drag to reorder */}
            <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.16em] uppercase">
              Enabled · drag to reorder
            </p>
            {enabled.length === 0 ? (
              <p className="text-cc-ink-dim mt-2 text-xs">
                Nothing enabled yet.
              </p>
            ) : (
              <Reorder.Group
                axis="y"
                values={enabled}
                onReorder={setEnabled}
                className="mt-2 space-y-1.5"
              >
                {enabled.map((id) => {
                  const entry = byId(id);
                  return (
                    <Reorder.Item
                      key={id}
                      value={id}
                      onMouseEnter={() => setHoveredId(id)}
                      className="border-cc-card-border bg-cc-surface flex cursor-grab items-center gap-2 rounded-lg border px-2.5 py-2 active:cursor-grabbing"
                    >
                      <GripIcon className="text-cc-ink-faint size-4 shrink-0" />
                      <span className="min-w-0 flex-1">
                        <span className="text-cc-nav-label block font-mono text-[0.55rem] tracking-[0.1em] uppercase">
                          {entry?.track}
                        </span>
                        <span className="text-cc-ink block truncate text-xs">
                          {entry?.label}
                        </span>
                      </span>
                      <button
                        type="button"
                        onClick={() => removeSection(id)}
                        aria-label={`Remove ${entry?.label}`}
                        className="text-cc-ink-dim hover:text-cc-status-firing rounded p-1 transition-colors"
                      >
                        <CloseIcon className="size-3.5" />
                      </button>
                    </Reorder.Item>
                  );
                })}
              </Reorder.Group>
            )}

            {/* available, grouped by track */}
            <p className="text-cc-nav-label mt-5 font-mono text-[0.6rem] tracking-[0.16em] uppercase">
              Available
            </p>
            <div className="mt-2 space-y-3">
              {TRACK_ORDER.map((track) => {
                const items = available.filter((s) => s.track === track);
                if (items.length === 0) {
                  return null;
                }
                return (
                  <div key={track}>
                    <p className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.08em]">
                      {track}
                    </p>
                    <div className="mt-1 space-y-1">
                      {items.map((s) => (
                        <button
                          key={s.id}
                          type="button"
                          onClick={() => addSection(s.id)}
                          onMouseEnter={() => setHoveredId(s.id)}
                          className="border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex w-full items-center justify-between rounded-lg border px-2.5 py-1.5 text-left transition-colors"
                        >
                          <span className="text-cc-ink truncate text-xs">
                            {s.label}
                          </span>
                          <span className="text-cc-accent ml-2 shrink-0 text-sm leading-none">
                            +
                          </span>
                        </button>
                      ))}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          <div className="border-cc-card-border flex items-center justify-between gap-2 border-t px-4 py-3">
            <button
              type="button"
              onClick={() => setEnabled([])}
              className="text-cc-ink-dim hover:text-cc-heading font-mono text-[0.7rem] tracking-tight transition-colors"
            >
              Clear all
            </button>
            <button
              type="button"
              onClick={() => setEnabled(DEFAULT_ENABLED)}
              className="text-cc-accent font-mono text-[0.7rem] tracking-tight"
            >
              Reset to combined
            </button>
          </div>
        </div>
      )}
    </>
  );
}

export default LandingCustomizer;
