"use client";

import React, { useLayoutEffect, useRef } from "react";

import { ScaledCanvas } from "./ScaledCanvas";
import { DESKTOP_PRODUCTS, DESKTOP_SERVICES } from "./constants";
import { useAnchorContext } from "./AnchorContext";
import type { LaneKey } from "./anchorConfig";

interface Act2Props {
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

export const Act2: React.FC<Act2Props> = ({ activeTab, setActiveTab }) => {
  const tabs: Tab[] = [
    {
      key: "platform",
      title: "Platform",
      kind: "Default",
      body: "Four products, one cohesive system. From the API surface to the operator dashboards, every layer shares the same type system, telemetry pipeline, and operator interface — so you stop wiring together unrelated libraries and start shipping features.",
      body2:
        "Compose them in any combination. Use just Hot Chocolate for a single GraphQL service, or pull in Mocha, Nitro, and Strawberry Shake as you outgrow the basics.",
      bullets: [
        "Composable architecture",
        "Shared type system",
        "Unified observability",
      ],
    },
    {
      key: "hot-chocolate",
      title: "Hot Chocolate",
      kind: "Foundation",
      body: "The GraphQL server. Hot Chocolate is the foundation of every ChilliCream service — start schema-first from a contract, or code-first from your domain types. Both come with the strictest type safety in the .NET ecosystem and zero runtime cost for query planning.",
      body2:
        "Built on top of high-performance ASP.NET Core. Subscriptions, defer/stream, persisted queries, and federation are all first-class — not afterthoughts.",
      bullets: [
        "Schema-first authoring",
        "Code-first authoring",
        "Strict typing",
      ],
    },
    {
      key: "nitro",
      title: "Nitro",
      kind: "Operator",
      body: "The operator surface for your platform. Nitro turns operations from a guessing game into a control room — inspect any deployed schema, replay production queries against staging, push schema deltas with safety checks, and watch federation health from a single pane.",
      body2:
        "Same UI works against your local dev server, your staging cluster, and your production fleet. Schema diffs, query analytics, and audit logs are baked in.",
      bullets: ["Schema explorer", "Query replay", "Health dashboards"],
    },
    {
      key: "mocha",
      title: "Mocha",
      kind: "Messaging",
      body: "Streaming, subscriptions, and durable messaging. Mocha brings event-driven patterns and CQRS into the same toolchain as your GraphQL surface — subscriptions stream over your existing transport, durable streams persist past a restart, and at-least-once delivery means events survive when subscribers don't.",
      body2:
        "Backed by your choice of broker — Kafka, NATS, RabbitMQ, or Postgres — without changing the application surface.",
      bullets: [
        "GraphQL subscriptions",
        "Durable streams",
        "At-least-once delivery",
      ],
    },
    {
      key: "strawberry-shake",
      title: "Strawberry Shake",
      kind: "Client",
      body: "The strongly-typed .NET GraphQL client. Strawberry Shake reads your schema and generates C# clients you can use straight from your view models — no string queries, no untyped responses, no hand-written mapping code.",
      body2:
        "First-class support for federation, subscriptions, and live queries means your client stays in sync with your graph as it evolves. Codegen runs in MSBuild on every build.",
      bullets: [
        "Schema-driven codegen",
        "Federated queries",
        "Subscriptions & live queries",
      ],
    },
  ];

  const active = tabs.find((t) => t.key === activeTab) || tabs[0];

  const W = 1480;

  const BEND_Y_REL = [180, 188, 196, 204];
  const HEADING_OFFSET = 60;
  const BEND_Y = BEND_Y_REL.map((y) => y + HEADING_OFFSET);

  const ENTRY_LANES = [0.22, 0.4, 0.58, 0.76];
  const ENTRY_X = ENTRY_LANES.map((p) => Math.round(W * p));

  const COL_X = [0, 1, 2, 3].map((i) => 60 + i * 8);

  const PANEL_W = 920;
  const PANEL_X = (W - PANEL_W) / 2;
  const PANEL_Y = BEND_Y[3] + 40;
  const PANEL_H = 640;

  // === Catalog merge marker — horizontally centered between the 4 column lines,
  // vertically positioned at the middle of the act so the 4 lines descend in
  // their columns to mid-act before consolidating inward.
  const MERGE_X = (COL_X[0] + COL_X[3]) / 2; // = 72 — center of [60,68,76,84]

  const SWATCH = 14;
  const SWATCH_R = 3;

  // Match Act 3's service-line entry x positions so the lines exiting Act 2
  // land exactly at Act 3's entries.
  const PILL_W_A3 = 220;
  const PILLS_GAP_A3 = 14;
  const PILLS_TOTAL_A3 =
    DESKTOP_SERVICES.length * PILL_W_A3 +
    (DESKTOP_SERVICES.length - 1) * PILLS_GAP_A3;
  const PILLS_X0_A3 = (W - PILLS_TOTAL_A3) / 2;
  const SERVICE_EXIT_X = DESKTOP_SERVICES.map(
    (_, i) => PILLS_X0_A3 + i * (PILL_W_A3 + PILLS_GAP_A3) + PILL_W_A3 / 2
  );

  const FULL_H = PANEL_Y + PANEL_H + 80;

  // Lines descend in their column to mid-act before consolidating into catalog.
  const ANCHOR_Y = FULL_H / 2; // mid-act — column descent ends here
  const MERGE_Y = ANCHOR_Y + 60; // short consolidation curve below the anchor

  const STRIPE_ROW_Y = FULL_H - 60;

  const CATALOG_COLOR = "var(--cc-col-cat)";

  const OTHER_SERVICES = DESKTOP_SERVICES.slice(1).map((s, i) => ({
    ...s,
    x: SERVICE_EXIT_X[i + 1],
  }));

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

      // Product entries land AT the visible circle (which is the bend point).
      // Cup pour lines from Act 1 connect straight down to this position; from
      // here the line bends horizontally to the column lane and descends.
      [0, 1, 2, 3].forEach((i) => {
        const isActive =
          activeTab === "platform" || activeTab === DESKTOP_PRODUCTS[i].key;
        const meta = { opacity: isActive ? 1 : 0.32 };
        register(`act2.entry-${i}`, {
          ...toPage(ENTRY_X[i], BEND_Y[i]),
          kind: "service-entry",
          meta,
        });
        register(`act2.bend-${i}`, {
          ...toPage(COL_X[i], BEND_Y[i]),
          kind: "service-entry",
          meta,
        });
        register(`act2.col-anchor-${i}`, {
          ...toPage(COL_X[i], ANCHOR_Y),
          kind: "service-entry",
          meta,
        });
      });

      register(`act2.merge`, {
        ...toPage(MERGE_X, MERGE_Y),
        kind: "merge",
      });

      // Service exits at the bottom of Act 2 — used by ConnectorLayer to
      // continue the lines into Act 3.
      DESKTOP_SERVICES.forEach((s, i) => {
        const k = s.key as LaneKey;
        register(`act2.exit-${k}`, {
          ...toPage(SERVICE_EXIT_X[i], FULL_H),
          kind: "act-bottom",
        });
      });

      // Bottom-row stripe positions (for the 4 short colored lines exiting
      // Act 2; catalog exits via the merge marker through the catalog line).
      DESKTOP_SERVICES.slice(1).forEach((s, i) => {
        register(`act2.stripe-${s.key}`, {
          ...toPage(SERVICE_EXIT_X[i + 1], STRIPE_ROW_Y),
          kind: "service-exit",
        });
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
      [0, 1, 2, 3].forEach((i) => {
        unregister(`act2.entry-${i}`);
        unregister(`act2.bend-${i}`);
        unregister(`act2.col-anchor-${i}`);
      });
      unregister(`act2.merge`);
      DESKTOP_SERVICES.forEach((s) => {
        unregister(`act2.exit-${s.key}`);
        unregister(`act2.stripe-${s.key}`);
      });
    };
    // ENTRY_X / COL_X / BEND_Y / SERVICE_EXIT_X / MERGE_X / MERGE_Y /
    // ANCHOR_Y / STRIPE_ROW_Y / FULL_H / W are all derived from constants;
    // their identities change each render but the values don't, and register
    // already short-circuits no-op updates. Active-tab change does need to
    // re-run, so it's the only meaningful dep beyond register/unregister.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [register, unregister, activeTab]);

  return (
    <section
      ref={sectionRef}
      className="cc-act cc-act-build cc-act-spills"
      data-screen-label="02 Building Applications"
    >
      <div className="cc-act-label">
        <span className="num">02</span> Building Applications
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
            <div className="eyebrow">Build</div>
            <h2
              className="display"
              style={{
                fontSize: "clamp(36px, 4.4vw, 64px)",
                margin: "8px auto",
                maxWidth: "16ch",
              }}
            >
              Build the App.
              <br />
              We did the plumbing.
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
            {/* Product entry dots (clickable). Lines themselves are drawn by
              the ConnectorLayer using act2.entry-* / act2.bend-* anchors. */}
            {[0, 1, 2, 3].map((i) => (
              <circle
                key={"dot" + i}
                cx={ENTRY_X[i]}
                cy={BEND_Y[i]}
                r="6"
                fill={
                  activeTab === DESKTOP_PRODUCTS[i].key
                    ? "var(--cc-ink)"
                    : "#0c1322"
                }
                stroke="var(--cc-ink)"
                strokeWidth="2"
                style={{ cursor: "pointer", pointerEvents: "all" }}
                onClick={() => setActiveTab(DESKTOP_PRODUCTS[i].key)}
              />
            ))}

            {/* Product labels at the bend column */}
            {DESKTOP_PRODUCTS.map((p, i) => {
              const label = p.label.toUpperCase();
              const fontSize = 11;
              const textX = ENTRY_X[i] + 14;
              const textY = BEND_Y[i];
              return (
                <g
                  key={"plabel" + p.key}
                  onClick={() => setActiveTab(p.key)}
                  style={{ cursor: "pointer", pointerEvents: "all" }}
                >
                  <text
                    x={textX}
                    y={textY}
                    fontFamily="var(--cc-font-mono), monospace"
                    fontSize={fontSize}
                    letterSpacing="1.76"
                    dominantBaseline="middle"
                    paintOrder="stroke"
                    stroke="#0c1322"
                    strokeWidth="3"
                    strokeLinejoin="round"
                    fill={
                      activeTab === p.key
                        ? "var(--cc-ink)"
                        : "var(--cc-ink-dim)"
                    }
                    style={{ userSelect: "none" }}
                  >
                    {label}
                  </text>
                </g>
              );
            })}

            {/* Dotted callout from merge marker swatch to the CATALOG label */}
            <line
              x1={MERGE_X + SWATCH / 2 + 2}
              y1={MERGE_Y}
              x2={MERGE_X + SWATCH / 2 + 30}
              y2={MERGE_Y}
              stroke="var(--cc-ink)"
              strokeWidth="1.2"
              strokeDasharray="2 5"
              strokeLinecap="round"
              opacity="0.6"
            />

            {/* CATALOG merge marker: swatch + mono label */}
            <rect
              x={MERGE_X - SWATCH / 2}
              y={MERGE_Y - SWATCH / 2}
              width={SWATCH}
              height={SWATCH}
              rx={SWATCH_R}
              fill={CATALOG_COLOR}
            />
            <text
              x={MERGE_X + SWATCH / 2 + 36}
              y={MERGE_Y}
              fontFamily="var(--cc-font-mono), monospace"
              fontSize="13"
              letterSpacing="2.08"
              fontWeight="500"
              dominantBaseline="middle"
              fill="var(--cc-ink)"
              style={{ textTransform: "uppercase", userSelect: "none" }}
            >
              <tspan x={MERGE_X + SWATCH / 2 + 36} dy="-0.6em">
                CATALOG
              </tspan>
              <tspan x={MERGE_X + SWATCH / 2 + 36} dy="1.2em">
                SERVICE
              </tspan>
            </text>

            {/* Other 4 service markers (billing/ordering/shipping/users) at the
              bottom of Act 2. The short colored lines below them are drawn
              by the ConnectorLayer via act2.stripe-* / act2.exit-* anchors. */}
            {OTHER_SERVICES.map((s) => (
              <g key={"svc-" + s.key}>
                <rect
                  x={s.x - SWATCH / 2}
                  y={STRIPE_ROW_Y - SWATCH / 2}
                  width={SWATCH}
                  height={SWATCH}
                  rx={SWATCH_R}
                  fill={s.color}
                />
                <text
                  x={s.x + SWATCH / 2 + 8}
                  y={STRIPE_ROW_Y}
                  fontFamily="var(--cc-font-mono), monospace"
                  fontSize="13"
                  letterSpacing="2.08"
                  fontWeight="500"
                  dominantBaseline="middle"
                  fill={s.color}
                  style={{ textTransform: "uppercase", userSelect: "none" }}
                >
                  {s.label.toUpperCase()}
                </text>
              </g>
            ))}
          </svg>

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
                  <a href="#">↗ Read about {active.title}</a>
                  <a href="#">↗ Examples</a>
                </div>
              </div>
            </div>
          </div>
        </ScaledCanvas>
      </div>
    </section>
  );
};
