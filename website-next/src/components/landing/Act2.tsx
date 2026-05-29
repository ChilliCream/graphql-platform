"use client";

import React, { useEffect, useRef } from "react";

import { DESKTOP_PRODUCTS, DESKTOP_SERVICES } from "./constants";
import {
  useAnchorContext,
  useLandingRoot,
  useMeasureEffect,
} from "./AnchorContext";
import type { LaneKey } from "./anchorConfig";
import { ArrowUpRight } from "../../icons/ArrowUpRight";

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

const SWATCH = 14;
const SWATCH_R = 3;
const ENTRY_DOT_SIZE = 12;

// Left-rail column x positions, expressed as offsets (in pixels) from the
// body's left edge. The connector layer uses these to draw the per-product
// column descents that converge into the catalog merge swatch. Mirrors
// Act 3's column spread so the visual rhythm of the two acts matches.
const COL_X_OFFSETS = [44, 52, 60, 68];
// The bend lives AT entry.y so the first segment is purely horizontal —
// the line travels left from the entry dot to the rail at the same Y.
const BEND_BELOW_ENTRY_PX = 0;
// The column descent ends just above the merge swatch.
const COL_ANCHOR_ABOVE_MERGE_PX = 30;

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

  const sectionRef = useRef<HTMLElement>(null);
  const bodyRef = useRef<HTMLDivElement | null>(null);
  const entryRowRef = useRef<HTMLDivElement | null>(null);
  const entryRefs = useRef<Array<HTMLDivElement | null>>([]);
  const mergeSwatchRef = useRef<HTMLDivElement | null>(null);
  const stripeRefs = useRef<Record<string, HTMLDivElement | null>>({});
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
      // Rail X positions are anchored to the body content edge so the merge
      // swatch, callout, and connector rails all share a single coordinate
      // origin — staying aligned on big screens where the body is centered
      // inside the section via max-width + margin auto.
      const railX0 = body.getBoundingClientRect().left - rRect.left;

      const entryPts: Array<{ x: number; y: number }> = [];
      // Connector graphics render at full opacity regardless of the active
      // tab — tab selection drives the panel + tab-bar styling only, not the
      // diagram lines.
      const meta = { opacity: 1 };
      [0, 1, 2, 3].forEach((i) => {
        const dotEl = entryRefs.current[i];
        if (!dotEl) {
          return;
        }
        const dRect = dotEl.getBoundingClientRect();
        const pt = {
          x: dRect.left - rRect.left + dRect.width / 2,
          y: dRect.top - rRect.top + dRect.height / 2,
        };
        entryPts[i] = pt;
        register(`act2.entry-${i}`, { ...pt, kind: "service-entry", meta });
      });

      let mergePt: { x: number; y: number } | null = null;
      const mergeEl = mergeSwatchRef.current;
      if (mergeEl) {
        const mRect = mergeEl.getBoundingClientRect();
        mergePt = {
          x: mRect.left - rRect.left + mRect.width / 2,
          y: mRect.top - rRect.top + mRect.height / 2,
        };
        register(`act2.merge`, { ...mergePt, kind: "merge" });
      }

      [0, 1, 2, 3].forEach((i) => {
        const entry = entryPts[i];
        if (!entry || !mergePt) {
          return;
        }
        const railColX = railX0 + COL_X_OFFSETS[i];
        register(`act2.bend-${i}`, {
          x: railColX,
          y: entry.y + BEND_BELOW_ENTRY_PX,
          kind: "service-entry",
          meta,
        });
        register(`act2.col-anchor-${i}`, {
          x: railColX,
          y: mergePt.y - COL_ANCHOR_ABOVE_MERGE_PX,
          kind: "service-entry",
          meta,
        });
      });

      DESKTOP_SERVICES.forEach((s) => {
        const k = s.key as LaneKey;
        if (k === "catalog") {
          if (mergePt) {
            register(`act2.exit-${k}`, { ...mergePt, kind: "act-bottom" });
          }
          return;
        }
        const stripeEl = stripeRefs.current[s.key];
        if (!stripeEl) {
          return;
        }
        const stRect = stripeEl.getBoundingClientRect();
        const pt = {
          x: stRect.left - rRect.left + stRect.width / 2,
          y: stRect.top - rRect.top + stRect.height / 2,
        };
        register(`act2.exit-${k}`, { ...pt, kind: "act-bottom" });
        register(`act2.stripe-${s.key}`, { ...pt, kind: "service-exit" });
      });
    },
    [sectionRef, bodyRef, entryRowRef],
    [register, root]
  );

  useEffect(
    () => () => {
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
    },
    [unregister]
  );

  return (
    <section
      ref={sectionRef}
      className="cc-act cc-act-build cc-act-spills"
      data-screen-label="02 Building Applications"
    >
      <div className="cc-act2-body" ref={bodyRef}>
        <div className="cc-section-headline-fade cc-act2-headline-wrap">
          <div className="eyebrow">Build</div>
          <h2 className="display cc-act2-headline">
            Build the App.
            <br />
            We did the plumbing.
          </h2>
        </div>

        {/* Product entry row — uses the shared 5-col service grid, with the
            4 product cells occupying columns 2..5. This guarantees each
            product (HC, Mitra, Mocha, SS) sits in the same vertical lane as
            its downstream service (Billing, Ordering, Shipping, Users) in
            Act 2's bottom row and Act 3's top row. The vertical stagger
            (i * 8px paddingTop) matches the left-rail X spacing so the
            entry-to-rail diagonals are parallel. */}
        <div
          className="cc-service-lane-row cc-act2-top-row"
          ref={entryRowRef}
        >
          {DESKTOP_PRODUCTS.map((p, i) => {
            const isActive = activeTab === p.key;
            return (
              <div
                key={"entry-" + p.key}
                className="cc-service-lane-cell cc-act2-entry-cell"
                style={{ paddingTop: i * 8 }}
              >
                <button
                  type="button"
                  className="cc-act2-entry-trigger"
                  onClick={() => setActiveTab(p.key)}
                  aria-label={p.label}
                >
                  <div
                    ref={(el) => {
                      entryRefs.current[i] = el;
                    }}
                    className="cc-act2-entry-dot"
                    style={{
                      width: ENTRY_DOT_SIZE,
                      height: ENTRY_DOT_SIZE,
                      borderRadius: "50%",
                      background: isActive ? "var(--cc-ink)" : "#0c1322",
                      border: "2px solid var(--cc-ink)",
                    }}
                  />
                  <span
                    className={
                      "cc-act2-entry-label" + (isActive ? " is-active" : "")
                    }
                  >
                    {p.label}
                  </span>
                </button>
              </div>
            );
          })}
        </div>

        {/* Content row — mirrors Act 3's layout: narrow merge stage on the
            left (catalog merge swatch + label), tab bar + panel filling the
            right. Both columns share the body's coordinate frame so the
            left-rail descents converge cleanly into the merge. */}
        <div className="cc-act2-content-row">
          {/* Catalog merge stage — sits in the left column where the 4
              product columns converge. Registered as `act2.merge`. */}
          <div className="cc-act2-merge-stage">
            <div
              ref={mergeSwatchRef}
              className="cc-service-swatch cc-act2-merge-swatch"
              style={{
                width: SWATCH,
                height: SWATCH,
                borderRadius: SWATCH_R,
                background: DESKTOP_SERVICES.find((s) => s.key === "catalog")
                  ?.color,
              }}
            />
            <div className="cc-act2-merge-callout">
              <div>Catalog</div>
              <div>Service</div>
            </div>
          </div>

          {/* Tab bar + panel — normal flow; height grows with content. */}
          <div className="cc-act2-panel-wrap">
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
                    Read about {active.title}
                  </a>
                  <a href="#">
                    <ArrowUpRight className="cc-link-icon" />
                    Examples
                  </a>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Bottom stripe row — 4 stripes (billing/ordering/shipping/users)
            positioned at columns 2..5 of an implicit 5-column grid that
            mirrors Act 3's top pill row, so each stripe x matches its
            corresponding Act 3 pill x and the connector lanes are vertical
            by construction. */}
        <div className="cc-service-lane-row cc-act2-bottom-row">
          {DESKTOP_SERVICES.filter((s) => s.key !== "catalog").map((s, i) => (
            <div
              key={"bottom-" + s.key}
              className="cc-service-lane-cell"
              style={{ paddingTop: [0, 60, 30, 90][i] ?? 0 }}
            >
              <div className="cc-act2-entry">
                <div
                  ref={(el) => {
                    stripeRefs.current[s.key] = el;
                  }}
                  className="cc-service-swatch"
                  style={{
                    width: SWATCH,
                    height: SWATCH,
                    borderRadius: SWATCH_R,
                    background: s.color,
                  }}
                />
                <span className="cc-service-label" style={{ color: s.color }}>
                  {s.label}
                </span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};
