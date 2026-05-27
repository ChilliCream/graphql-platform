"use client";

import React, { useEffect, useRef, useState } from "react";

import { SHARED_PRODUCTS } from "./LandingRoot";

interface Act2Props {
  activeTab: string;
  setActiveTab: (key: string) => void;
}

interface Tab {
  key: string;
  title: string;
  kind: string;
  body: string;
  bullets: string[];
}

export const Act2: React.FC<Act2Props> = ({ activeTab, setActiveTab }) => {
  const tabs: Tab[] = [
    {
      key: "platform",
      title: "Platform",
      kind: "Default",
      body: "Four products, one cohesive system. Every layer shares the same type system, telemetry pipeline, and operator interface — so you stop wiring together unrelated libraries and start shipping features.",
      bullets: ["Composable", "Shared types", "Unified observability"],
    },
    {
      key: "hot-chocolate",
      title: "Hot Chocolate",
      kind: "Foundation",
      body: "The GraphQL server. Start schema-first from a contract, or code-first from your domain types. Both come with the strictest type safety in the .NET ecosystem.",
      bullets: ["Schema-first", "Code-first", "Strict typing"],
    },
    {
      key: "nitro",
      title: "Nitro",
      kind: "Operator",
      body: "The operator surface for your platform. Inspect any deployed schema, replay production queries against staging, push schema deltas with safety checks.",
      bullets: ["Schema explorer", "Query replay", "Health"],
    },
    {
      key: "mocha",
      title: "Mocha",
      kind: "Messaging",
      body: "Streaming, subscriptions, and durable messaging — backed by your choice of broker. Kafka, NATS, RabbitMQ, or Postgres without changing the application surface.",
      bullets: ["Subscriptions", "Durable streams", "At-least-once"],
    },
    {
      key: "strawberry-shake",
      title: "Strawberry Shake",
      kind: "Client",
      body: "The strongly-typed .NET GraphQL client. Reads your schema and generates C# clients you can use straight from your view models — no string queries.",
      bullets: ["Codegen", "Federated", "Live queries"],
    },
  ];

  const active = tabs.find((t) => t.key === activeTab) || tabs[0];

  const STRIP_W = 360;
  const STRIP_H = 310;
  const ENTRY_X = [171, 177, 183, 189];
  const BEND_Y = [40, 80, 120, 160];
  const STRIPE_X = [24, 40, 56, 72];
  const R = 8;
  const ANCHOR_Y = 180;
  const MERGE_X = 48;
  const MERGE_Y = 260;
  const BOX_SIZE = 14;
  const BOX_RADIUS = 3;
  const MERGE_LABEL = "CATALOG";
  const MERGE_COLOR = "var(--cc-col-cat)";

  const stripPath = (i: number) => {
    const xIn = ENTRY_X[i];
    const yBend = BEND_Y[i];
    const xCol = STRIPE_X[i];
    return [
      `M ${xIn} 0`,
      `L ${xIn} ${yBend - R}`,
      `Q ${xIn} ${yBend} ${xIn - R} ${yBend}`,
      `L ${xCol + R} ${yBend}`,
      `Q ${xCol} ${yBend} ${xCol} ${yBend + R}`,
      `L ${xCol} ${ANCHOR_Y}`,
      `C ${xCol} ${MERGE_Y - 24} ${MERGE_X} ${
        MERGE_Y - 24
      } ${MERGE_X} ${MERGE_Y}`,
    ].join(" ");
  };

  const exitPath = `M ${MERGE_X} ${
    MERGE_Y + BOX_SIZE / 2
  } L ${MERGE_X} ${STRIP_H}`;

  const SVC_W = 360;
  const ROW_H = 30;
  const SVC_DROP = 70;
  const SVC_ROWS = [
    { key: "billing", label: "BILLING", color: "var(--cc-col-bil)", x: 100 },
    { key: "shipping", label: "SHIPPING", color: "var(--cc-col-shi)", x: 240 },
    { key: "ordering", label: "ORDERING", color: "var(--cc-col-ord)", x: 150 },
    { key: "users", label: "USERS", color: "var(--cc-col-usr)", x: 310 },
  ];
  const SVC_H = SVC_ROWS.length * ROW_H + SVC_DROP;
  const CATALOG_X = 40;
  const SWATCH = 14;
  const SWATCH_R = 3;

  const tabbarRef = useRef<HTMLDivElement>(null);
  const [chevronVisible, setChevronVisible] = useState(true);

  useEffect(() => {
    const el = tabbarRef.current;
    if (!el) return;
    const onScroll = () => {
      if (el.scrollLeft > 8) setChevronVisible(false);
    };
    el.addEventListener("scroll", onScroll, { passive: true });
    return () => el.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <section className="act build act-spills" data-screen-label="02 Build">
      <div className="act-label">
        <span className="num">02</span> Build
      </div>
      <div className="act-heading section-headline-fade">
        <div className="eyebrow">Build</div>
        <h2 className="display">
          Build the App.
          <br />
          We did the plumbing.
        </h2>
      </div>

      <svg
        width="100%"
        height={STRIP_H}
        viewBox={`0 0 ${STRIP_W} ${STRIP_H}`}
        preserveAspectRatio="xMidYMin meet"
        style={{ display: "block", overflow: "visible" }}
        aria-hidden
      >
        <defs>
          <linearGradient
            id="cc-act2-line-fade"
            x1="0"
            y1="0"
            x2="0"
            y2={STRIP_H}
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0" stopColor="black" />
            <stop offset={16 / STRIP_H} stopColor="white" />
            <stop offset={1 - 16 / STRIP_H} stopColor="white" />
            <stop offset="1" stopColor="black" />
          </linearGradient>
          <mask
            id="cc-act2-line-mask"
            maskUnits="userSpaceOnUse"
            x="0"
            y="0"
            width={STRIP_W}
            height={STRIP_H}
          >
            <rect
              x="0"
              y="0"
              width={STRIP_W}
              height={STRIP_H}
              fill="url(#cc-act2-line-fade)"
            />
          </mask>
        </defs>
        {SHARED_PRODUCTS.map((p, i) => {
          const isActive = activeTab === "platform" || activeTab === p.key;
          return (
            <g key={p.key} opacity={isActive ? 1 : 0.32}>
              <g mask="url(#cc-act2-line-mask)">
                <path
                  d={stripPath(i)}
                  stroke="var(--cc-ink)"
                  strokeWidth="var(--cc-line-w)"
                  fill="none"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </g>
              <circle
                cx={STRIPE_X[i]}
                cy={BEND_Y[i]}
                r="5"
                fill={activeTab === p.key ? "var(--cc-ink)" : "#0c1322"}
                stroke="var(--cc-ink)"
                strokeWidth="2"
              />
              <rect
                x={STRIPE_X[i] - 22}
                y={BEND_Y[i] - 22}
                width="44"
                height="44"
                fill="transparent"
                style={{ cursor: "pointer" }}
                onClick={() => setActiveTab(p.key)}
              />
              <rect
                x={STRIPE_X[i] + 12}
                y={BEND_Y[i] - 8}
                width={p.label.length * 7 + 8}
                height="16"
                fill="#0c1322"
              />
              <text
                x={STRIPE_X[i] + 16}
                y={BEND_Y[i] + 4}
                fontFamily="JetBrains Mono, monospace"
                fontSize="11"
                letterSpacing="1.6"
                fill={
                  activeTab === p.key ? "var(--cc-ink)" : "var(--cc-ink-dim)"
                }
                style={{ cursor: "pointer", userSelect: "none" }}
                onClick={() => setActiveTab(p.key)}
              >
                {p.label.toUpperCase()}
              </text>
            </g>
          );
        })}

        <g mask="url(#cc-act2-line-mask)">
          <path
            d={exitPath}
            stroke={MERGE_COLOR}
            strokeWidth="var(--cc-line-w)"
            fill="none"
            strokeLinecap="round"
          />
        </g>

        <rect
          x={MERGE_X - BOX_SIZE / 2}
          y={MERGE_Y - BOX_SIZE / 2}
          width={BOX_SIZE}
          height={BOX_SIZE}
          rx={BOX_RADIUS}
          fill={MERGE_COLOR}
        />
        <text
          x={MERGE_X + BOX_SIZE / 2 + 8}
          y={MERGE_Y + 4}
          fontFamily="JetBrains Mono, monospace"
          fontSize="11"
          letterSpacing="1.6"
          fill="var(--cc-ink)"
          style={{ userSelect: "none" }}
        >
          {MERGE_LABEL}
        </text>
      </svg>

      <div className="tabbar-wrap" style={{ marginTop: 8 }}>
        <div className="tabbar" role="tablist" ref={tabbarRef}>
          {tabs.map((t) => (
            <button
              key={t.key}
              role="tab"
              aria-selected={activeTab === t.key}
              className={
                "tabbar-tab " + (activeTab === t.key ? "is-active" : "")
              }
              onClick={() => setActiveTab(t.key)}
            >
              {t.title}
            </button>
          ))}
        </div>
        <div
          className="tabbar-fade"
          style={{ opacity: chevronVisible ? 1 : 0 }}
        />
        <div
          className="tabbar-chevron"
          style={{ opacity: chevronVisible ? 1 : 0 }}
        >
          ›
        </div>
      </div>

      <div className="tab-panel" role="tabpanel" key={active.key}>
        <div className="tab-key">{active.kind}</div>
        <h3 className="tab-title">{active.title}</h3>
        <p className="tab-body">{active.body}</p>
        <ul className="tab-bullets">
          {active.bullets.map((b) => (
            <li key={b}>{b}</li>
          ))}
        </ul>
        <div className="tab-meta">
          <a href="#">↗ Read about {active.title}</a>
          <a href="#">↗ Examples</a>
        </div>
      </div>

      <svg
        width="100%"
        height={SVC_H}
        viewBox={`0 0 ${SVC_W} ${SVC_H}`}
        preserveAspectRatio="xMidYMin meet"
        style={{ display: "block", overflow: "visible" }}
        aria-hidden
      >
        <defs>
          <linearGradient
            id="cc-act2-svc-fade"
            x1="0"
            y1="0"
            x2="0"
            y2={SVC_H}
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0" stopColor="white" />
            <stop offset={1 - 16 / SVC_H} stopColor="white" />
            <stop offset="1" stopColor="black" />
          </linearGradient>
          <mask
            id="cc-act2-svc-mask"
            maskUnits="userSpaceOnUse"
            x="0"
            y="0"
            width={SVC_W}
            height={SVC_H}
          >
            <rect
              x="0"
              y="0"
              width={SVC_W}
              height={SVC_H}
              fill="url(#cc-act2-svc-fade)"
            />
          </mask>
        </defs>

        <g mask="url(#cc-act2-svc-mask)">
          <line
            x1={CATALOG_X}
            y1="0"
            x2={CATALOG_X}
            y2={SVC_H}
            stroke="var(--cc-col-cat)"
            strokeWidth="var(--cc-line-w)"
          />
        </g>

        <g mask="url(#cc-act2-svc-mask)">
          {SVC_ROWS.map((s, i) => {
            const yCenter = i * ROW_H + ROW_H / 2;
            const yStart = yCenter + SWATCH / 2;
            return (
              <line
                key={s.key + "-drop"}
                x1={s.x}
                y1={yStart}
                x2={s.x}
                y2={SVC_H}
                stroke={s.color}
                strokeWidth="var(--cc-line-w)"
              />
            );
          })}
        </g>

        {SVC_ROWS.map((s, i) => {
          const yCenter = i * ROW_H + ROW_H / 2;
          return (
            <g key={s.key}>
              <rect
                x={s.x - SWATCH / 2}
                y={yCenter - SWATCH / 2}
                width={SWATCH}
                height={SWATCH}
                rx={SWATCH_R}
                fill={s.color}
              />
              <text
                x={s.x + SWATCH / 2 + 8}
                y={yCenter + 4}
                fontFamily="JetBrains Mono, monospace"
                fontSize="11"
                letterSpacing="1.6"
                fill="var(--cc-ink)"
                style={{ userSelect: "none" }}
              >
                {s.label}
              </text>
            </g>
          );
        })}
      </svg>
    </section>
  );
};
