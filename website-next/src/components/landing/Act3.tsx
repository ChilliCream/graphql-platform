"use client";

import React, { useEffect, useRef } from "react";

import { DESKTOP_SERVICES } from "./constants";
import {
  useAnchorContext,
  useLandingRoot,
  useMeasureEffect,
} from "./AnchorContext";
import type { LaneKey } from "./anchorConfig";
import { ArrowUpRight } from "../../icons/ArrowUpRight";

interface Act3Props {
  activeTab: string;
  setActiveTab: (key: string) => void;
}

interface Tab {
  key: string;
  title: string;
  kind: string;
  body: string;
  body2?: string;
  bullets: string[];
}

// Left-rail column x offsets, in pixels from the section's left padding. The
// connector layer uses these for the per-service column descents that funnel
// into the pinch. Shifted in from the body's left edge so the 88px pinch
// halo has room to render fully without getting clipped at the viewport.
const COL_X_OFFSETS = [44, 52, 60, 68, 76];
// Bend sits AT the entry row so each line makes a clean horizontal-then-
// vertical L turn (no diagonal segment between entry and column rail).
const BEND_BELOW_ENTRY_PX = 0;
// twist-start sits a small fraction up from the pinch so the funnel curve
// has room to bend.
const TWIST_START_ABOVE_PINCH_FRACTION = 0.1;
export const Act3: React.FC<Act3Props> = ({ activeTab, setActiveTab }) => {
  const tabs: Tab[] = [
    {
      key: "fusion-overview",
      title: "Fusion",
      kind: "Federation",
      body: "Fusion is the federation engine. It composes independently-developed service schemas into a single graph at planning time — not at runtime — so the gateway stays fast, queries stay typed end to end, and your teams ship without coordination overhead.",
      body2:
        "Every team owns its own schema. Fusion handles the seams: shared types, resolved field ownership, breaking-change detection in CI before a deploy ever lands.",
      bullets: [
        "Compile-time composition",
        "Strict type checks",
        "Zero runtime cost",
      ],
    },
    {
      key: "schema",
      title: "Composition",
      kind: "Authoring",
      body: "Each service ships its own schema in its own repo. Fusion's composer validates compatibility across all of them, resolves field ownership, and detects breaking changes before a deploy lands.",
      body2:
        "Composition runs in CI on every PR. The output is a single composed schema artifact your gateway consumes — no runtime stitching, no surprise schema drift in production.",
      bullets: [
        "Service ownership",
        "Breaking-change detection",
        "CI integration",
      ],
    },
    {
      key: "gateway",
      title: "Gateway",
      kind: "Runtime",
      body: "A federated entry point with query planning, parallel fan-out, and response stitching. The gateway parses each query against the composed schema, plans the cheapest route, and dispatches sub-queries to each owning service in parallel.",
      body2:
        "Cross-service N+1 disappears because Green Donut batches at the federation level — not per service. The gateway is also where authorization, persisted queries, and rate limiting live.",
      bullets: ["Query planning", "Parallel fan-out", "Cross-service batching"],
    },
    {
      key: "operations",
      title: "Operations",
      kind: "Nitro",
      body: "Nitro becomes the operator's window into the entire federation. Inspect schemas per service, replay queries across federation boundaries to debug live incidents, push schema deltas safely with automatic compatibility checks.",
      body2:
        "Same control surface for every environment. Promote a schema from staging to production with one click — the compatibility check runs against live traffic samples before the change is applied.",
      bullets: [
        "Per-service health",
        "Cross-boundary replay",
        "Safe schema deltas",
      ],
    },
    {
      key: "observability",
      title: "Telemetry",
      kind: "Observability",
      body: "Distributed traces span the gateway and every downstream service. Errors surface with the origin field path attached. Latency histograms break down per resolver, per service, per query — the visibility a federated graph needs to actually run in production.",
      body2:
        "Built on OpenTelemetry. Wire it into the backend you already use — Jaeger, Tempo, Datadog, Honeycomb — and federation traces just appear, no glue code required.",
      bullets: [
        "Distributed tracing",
        "Origin-tagged errors",
        "Per-resolver latency",
      ],
    },
  ];

  const active = tabs.find((t) => t.key === activeTab) || tabs[0];

  const sectionRef = useRef<HTMLElement>(null);
  const bodyRef = useRef<HTMLDivElement | null>(null);
  const entryRowRef = useRef<HTMLDivElement | null>(null);
  const entryRefs = useRef<Record<string, HTMLDivElement | null>>({});
  const pinchRef = useRef<HTMLDivElement | null>(null);
  const { register, unregister } = useAnchorContext();
  const root = useLandingRoot();

  useMeasureEffect(
    () => {
      const section = sectionRef.current;
      const body = bodyRef.current;
      if (!section || !body || !root) {
        return;
      }
      const rRect = root.getBoundingClientRect();
      const secRect = section.getBoundingClientRect();
      // Rail X positions are anchored to the body content edge so they stay
      // aligned with the pinch on big screens, where the body is centered
      // inside the section via max-width + margin auto.
      const railX0 = body.getBoundingClientRect().left - rRect.left;

      let pinchPt: { x: number; y: number } | null = null;
      const pinchEl = pinchRef.current;
      if (pinchEl) {
        const pRect = pinchEl.getBoundingClientRect();
        pinchPt = {
          x: pRect.left - rRect.left + pRect.width / 2,
          y: pRect.top - rRect.top + pRect.height / 2,
        };
        register(`act3.pinch`, { ...pinchPt, kind: "pinch" });
      }

      DESKTOP_SERVICES.forEach((s, i) => {
        const k = s.key as LaneKey;
        const entryEl = entryRefs.current[s.key];
        if (!entryEl) {
          return;
        }
        const eRect = entryEl.getBoundingClientRect();
        const entry = {
          x: eRect.left - rRect.left + eRect.width / 2,
          y: eRect.top - rRect.top + eRect.height / 2,
        };
        register(`act3.entry-${k}`, { ...entry, kind: "service-entry" });

        const railColX = railX0 + COL_X_OFFSETS[i];
        register(`act3.bend-${k}`, {
          x: railColX,
          y: entry.y + BEND_BELOW_ENTRY_PX,
          kind: "service-entry",
        });
        if (pinchPt) {
          const twistStartY =
            pinchPt.y -
            (pinchPt.y - entry.y) * TWIST_START_ABOVE_PINCH_FRACTION;
          register(`act3.twist-start-${k}`, {
            x: railColX,
            y: twistStartY,
            kind: "service-entry",
          });
        }
      });

      if (pinchPt) {
        register(`act3.exit`, {
          x: pinchPt.x,
          y: secRect.bottom - rRect.top,
          kind: "act-bottom",
        });
      }
    },
    [sectionRef, bodyRef, entryRowRef, pinchRef],
    [register, root]
  );

  useEffect(
    () => () => {
      DESKTOP_SERVICES.forEach((s) => {
        unregister(`act3.entry-${s.key}`);
        unregister(`act3.bend-${s.key}`);
        unregister(`act3.twist-start-${s.key}`);
      });
      unregister(`act3.pinch`);
      unregister(`act3.exit`);
    },
    [unregister]
  );

  return (
    <section
      ref={sectionRef}
      className="cc-act cc-act-fusion cc-act-spills"
      data-screen-label="03 Fusion"
    >
      <div className="cc-act-label">
        Fusion
      </div>

      <div className="cc-act3-body" ref={bodyRef}>
        <div className="cc-section-headline-fade cc-act3-headline-wrap">
          <div className="eyebrow">Fusion</div>
          <h2 className="display cc-act3-headline">
            Built apart.
            <br />
            Queried together.
          </h2>
        </div>

        {/* Service entry row — 5-col grid identical to Act 2's bottom
            row, so per-service column x values match exactly. Sits BELOW
            the headline; connector lines from Act 2 pass behind the
            headline fade to reach these dots. */}
        <div className="cc-service-lane-row cc-act3-top-row" ref={entryRowRef}>
          {DESKTOP_SERVICES.map((s, i) => (
            <div
              key={"entry-" + s.key}
              className="cc-service-lane-cell"
              style={{ paddingTop: i * 8 }}
            >
              <div className="cc-act3-entry">
                <div
                  ref={(el) => {
                    entryRefs.current[s.key] = el;
                  }}
                  className="cc-act3-entry-dot"
                  style={{
                    width: 12,
                    height: 12,
                    borderRadius: "50%",
                    background: "#0c1322",
                    border: `2px solid ${s.color}`,
                  }}
                />
                <span className="cc-service-label" style={{ color: s.color }}>
                  {s.label}
                </span>
              </div>
            </div>
          ))}
        </div>

        <div className="cc-act3-content-row">
          {/* Fusion visualization stage — pinch + halo + callout, now in
              the left column of the content row. */}
          <div className="cc-act3-fusion-stage">
            {/* Halo behind pinch. */}
            <div className="cc-act3-pinch-halo" aria-hidden />
            {/* Pinch dot — primary anchor for `act3.pinch`. */}
            <div ref={pinchRef} className="cc-pinch-dot" />
            {/* Dotted connector from pinch up-right to the callout. */}
            <div className="cc-act3-callout-connector" aria-hidden />
            {/* FUSION COMPOSITION callout. */}
            <div className="cc-act3-fusion-callout">
              <div>Fusion</div>
              <div>Composition</div>
            </div>
          </div>

          {/* Tab bar + panel. */}
          <div className="cc-act3-panel-wrap">
            <div className="cc-tabbar-h" role="tablist">
              {tabs.map((t) => (
                <button
                  key={t.key}
                  role="tab"
                  aria-selected={activeTab === t.key}
                  className={
                    "cc-tabbar-h-tab " +
                    (activeTab === t.key ? "is-active" : "")
                  }
                  onClick={() => setActiveTab(t.key)}
                >
                  {t.title}
                </button>
              ))}
            </div>
            <div className="cc-tab-panel-d" role="tabpanel" key={active.key}>
              <div className="cc-tab-grid">
                <div className="cc-tab-text">
                  <div className="cc-tab-key">{active.kind}</div>
                  <h3 className="cc-tab-title">{active.title}</h3>
                  <p className="cc-tab-body">{active.body}</p>
                  {active.body2 && (
                    <p className="cc-tab-body">{active.body2}</p>
                  )}
                </div>
                <div className="cc-tab-viz" aria-hidden>
                  <span className="cc-tab-viz-label">Visualization</span>
                </div>
              </div>
              <div className="cc-tab-footer">
                <ul className="cc-tab-bullets-d">
                  {active.bullets.map((b) => (
                    <li key={b}>{b}</li>
                  ))}
                </ul>
                <div className="cc-tab-meta">
                  <a href="#">
                    <ArrowUpRight className="cc-link-icon" />
                    Fusion docs
                  </a>
                  <a href="#">
                    <ArrowUpRight className="cc-link-icon" />
                    Architecture
                  </a>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
