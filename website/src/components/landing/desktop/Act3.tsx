"use client";

import React, { useLayoutEffect, useRef } from "react";

import { ScaledCanvas } from "./ScaledCanvas";
import { DESKTOP_SERVICES } from "./constants";
import { useAnchorContext } from "./AnchorContext";
import type { LaneKey } from "./anchorConfig";

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

  const W = 1480;

  const BEND_Y_REL = [40, 48, 56, 64, 72];
  const HEADING_OFFSET = 180;
  const BEND_Y = BEND_Y_REL.map((y) => y + HEADING_OFFSET);

  const PILL_W_A2 = 220;
  const PILLS_GAP_A2 = 14;
  const PILLS_TOTAL_A2 =
    DESKTOP_SERVICES.length * PILL_W_A2 +
    (DESKTOP_SERVICES.length - 1) * PILLS_GAP_A2;
  const PILLS_X0_A2 = (W - PILLS_TOTAL_A2) / 2;
  const ENTRY_X = DESKTOP_SERVICES.map(
    (_, i) => PILLS_X0_A2 + i * (PILL_W_A2 + PILLS_GAP_A2) + PILL_W_A2 / 2
  );

  const COL_X = [0, 1, 2, 3, 4].map((i) => 60 + i * 8);

  const FULL_H = 1000;

  const TWIST_START_Y = 588;
  const PINCH_Y = 648;

  const PINCH_X = COL_X[2];

  const PANEL_W = 920;
  const PANEL_X = (W - PANEL_W) / 2;
  const PANEL_Y = BEND_Y[4] + 60;

  const sectionRef = useRef<HTMLElement>(null);
  const canvasRef = useRef<HTMLDivElement>(null);
  const { register, unregister } = useAnchorContext();

  useLayoutEffect(() => {
    const measure = () => {
      const canvas = canvasRef.current?.querySelector(
        ".cc-canvas"
      ) as HTMLElement | null;
      const root = sectionRef.current?.closest(
        "[data-cc-landing-root]"
      ) as HTMLElement | null;
      if (!canvas || !root) return;
      const cRect = canvas.getBoundingClientRect();
      const rRect = root.getBoundingClientRect();
      const scale = cRect.width / W;
      const toPage = (cx: number, cy: number) => ({
        x: cRect.left - rRect.left + cx * scale,
        y: cRect.top - rRect.top + cy * scale,
      });

      DESKTOP_SERVICES.forEach((s, i) => {
        const k = s.key as LaneKey;
        // Entry lands AT the visible pill (which is the bend Y row), so the
        // line from Act 2 ends there and the funnel continues from this point.
        register(`act3.entry-${k}`, {
          ...toPage(ENTRY_X[i], BEND_Y[i]),
          kind: "service-entry",
        });
        register(`act3.bend-${k}`, {
          ...toPage(COL_X[i], BEND_Y[i]),
          kind: "service-entry",
        });
        register(`act3.twist-start-${k}`, {
          ...toPage(COL_X[i], TWIST_START_Y),
          kind: "service-entry",
        });
      });

      register(`act3.pinch`, {
        ...toPage(PINCH_X, PINCH_Y),
        kind: "pinch",
      });
      // Bottom-of-act exit at x=272 (catalog lane) — used by ConnectorLayer to
      // continue the rainbow line through to Act 4's prism apex.
      register(`act3.exit`, {
        ...toPage(272, FULL_H),
        kind: "act-bottom",
      });
    };
    measure();
    const ro = new ResizeObserver(measure);
    if (canvasRef.current) ro.observe(canvasRef.current);
    if (sectionRef.current) ro.observe(sectionRef.current);
    window.addEventListener("resize", measure);
    return () => {
      ro.disconnect();
      window.removeEventListener("resize", measure);
      DESKTOP_SERVICES.forEach((s) => {
        unregister(`act3.entry-${s.key}`);
        unregister(`act3.bend-${s.key}`);
        unregister(`act3.twist-start-${s.key}`);
      });
      unregister(`act3.pinch`);
      unregister(`act3.exit`);
    };
    // Geometry constants are deterministic per render; register short-circuits
    // no-op updates. Only register/unregister identities need to be tracked.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [register, unregister]);

  return (
    <section
      ref={sectionRef}
      className="cc-act cc-act-fusion cc-act-spills"
      data-screen-label="03 Fusion"
    >
      <div className="cc-act-label">
        <span className="num">03</span> Fusion
      </div>

      <div ref={canvasRef}>
        <ScaledCanvas width={W} height={FULL_H}>
          <div
            className="cc-section-headline-fade"
            style={{
              position: "absolute",
              top: 40,
              left: 0,
              width: W,
              textAlign: "center",
              zIndex: 5,
              pointerEvents: "none",
            }}
          >
            <div className="eyebrow">Fusion</div>
            <h2
              className="display"
              style={{
                fontSize: "clamp(36px, 4.4vw, 64px)",
                margin: "8px auto",
                maxWidth: "16ch",
              }}
            >
              Built apart.
              <br />
              Queried together.
            </h2>
          </div>

          <svg
            width={W}
            height={FULL_H}
            viewBox={`0 0 ${W} ${FULL_H}`}
            overflow="visible"
            style={{
              position: "absolute",
              inset: 0,
              pointerEvents: "none",
              overflow: "visible",
            }}
            aria-hidden
          >
            <defs>
              <radialGradient id="cc-d-pinch-fade" cx="0.5" cy="0.5" r="0.5">
                <stop offset="0" stopColor="white" stopOpacity="0.55" />
                <stop offset="0.45" stopColor="white" stopOpacity="0.12" />
                <stop offset="1" stopColor="white" stopOpacity="0" />
              </radialGradient>
            </defs>

            {/* Service-entry dots — local visual identifier on each lane */}
            {DESKTOP_SERVICES.map((s, i) => (
              <circle
                key={s.key}
                cx={ENTRY_X[i]}
                cy={BEND_Y[i]}
                r="6"
                fill="#0c1322"
                stroke={s.color}
                strokeWidth="2"
              />
            ))}

            {/* Pinch halo + dot + FUSION COMPOSITION callout. The label sits
              ABOVE-RIGHT of the pinch with a diagonal leader line so the
              text isn't sitting in the bright glare band of the lens effect. */}
            <circle
              cx={PINCH_X}
              cy={PINCH_Y}
              r="44"
              fill="url(#cc-d-pinch-fade)"
            />
            <circle cx={PINCH_X} cy={PINCH_Y} r="3.5" fill="white" />
            <line
              x1={PINCH_X + 14}
              y1={PINCH_Y - 10}
              x2={PINCH_X + 60}
              y2={PINCH_Y - 60}
              stroke="var(--cc-ink)"
              strokeWidth="1.2"
              strokeDasharray="2 5"
              strokeLinecap="round"
              opacity="0.6"
            />

            <text
              x={PINCH_X + 70}
              y={PINCH_Y - 70}
              textAnchor="start"
              fontFamily="var(--cc-font-mono), monospace"
              fontSize="13"
              letterSpacing="2.08"
              fontWeight="500"
              fill="var(--cc-ink)"
              style={{ textTransform: "uppercase" }}
            >
              <tspan x={PINCH_X + 70} dy="0">
                FUSION
              </tspan>
              <tspan x={PINCH_X + 70} dy="16">
                COMPOSITION
              </tspan>
            </text>
          </svg>

          {DESKTOP_SERVICES.map((s, i) => (
            <div
              key={s.key}
              className="cc-canvas-stripe-label"
              style={{
                position: "absolute",
                left: ENTRY_X[i] + 14,
                top: BEND_Y[i],
                transform: "translateY(-50%)",
                color: s.color,
                display: "flex",
                alignItems: "center",
                gap: 8,
                lineHeight: 1,
              }}
            >
              <span
                style={{
                  width: 8,
                  height: 8,
                  background: s.color,
                  display: "inline-block",
                  borderRadius: 1,
                }}
              />
              {s.label}
            </div>
          ))}

          <div
            style={{
              position: "absolute",
              left: PANEL_X,
              top: PANEL_Y,
              width: PANEL_W,
            }}
          >
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
                  <a href="#">↗ Fusion docs</a>
                  <a href="#">↗ Architecture</a>
                </div>
              </div>
            </div>
          </div>
        </ScaledCanvas>
      </div>
    </section>
  );
};
