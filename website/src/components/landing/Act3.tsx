"use client";

import React from "react";

import { SHARED_SERVICES } from "./LandingRoot";

interface Act3Props {
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

export const Act3: React.FC<Act3Props> = ({ activeTab, setActiveTab }) => {
  const tabs: Tab[] = [
    {
      key: "fusion-overview",
      title: "Fusion",
      kind: "Federation",
      body: "The federation engine. Composes service schemas into a single graph at planning time — not at runtime — so the gateway stays fast and queries stay typed end to end.",
      bullets: ["Compile-time", "Strict types", "Zero runtime cost"],
    },
    {
      key: "schema",
      title: "Composition",
      kind: "Authoring",
      body: "Each service ships its own schema. Fusion validates compatibility, resolves field ownership, and detects breaking changes before a deploy lands.",
      bullets: ["Service ownership", "Breaking-change", "CI integration"],
    },
    {
      key: "gateway",
      title: "Gateway",
      kind: "Runtime",
      body: "A federated entry point with query planning, parallel fan-out, and response stitching. Cross-service N+1 disappears because batching happens at the federation level.",
      bullets: ["Query planning", "Parallel fan-out", "Cross-svc batch"],
    },
    {
      key: "operations",
      title: "Operations",
      kind: "Nitro",
      body: "Inspect schemas per service, replay queries across federation boundaries, push schema deltas safely with automatic compatibility checks against live traffic samples.",
      bullets: ["Per-svc health", "Cross-replay", "Safe deltas"],
    },
    {
      key: "observability",
      title: "Telemetry",
      kind: "Observability",
      body: "Distributed traces span the gateway and every downstream service. Errors surface with the origin field path attached. Built on OpenTelemetry.",
      bullets: ["Tracing", "Origin errors", "Per-resolver latency"],
    },
  ];
  const active = tabs.find((t) => t.key === activeTab) || tabs[0];

  const W = 360;
  const H = 360;

  const ENTRY_X = [40, 100, 150, 240, 310];

  const PINCH_X = W / 2;
  const PINCH_Y = 180;

  const prePinch = (i: number) => {
    const xIn = ENTRY_X[i];
    const cy = PINCH_Y * 0.5;
    return `M ${xIn} 0 C ${xIn} ${cy} ${PINCH_X} ${cy} ${PINCH_X} ${PINCH_Y}`;
  };

  const postPinchPath = `M ${PINCH_X} ${PINCH_Y} L ${PINCH_X} ${H}`;
  const COMBINED_HEIGHT = H - PINCH_Y;

  return (
    <section className="act fusion act-spills" data-screen-label="03 Fusion">
      <div className="act-label">
        <span className="num">03</span> Fusion
      </div>
      <div className="act-heading section-headline-fade">
        <div className="eyebrow">Fusion</div>
        <h2 className="display">
          Built apart.
          <br />
          Queried together.
        </h2>
      </div>

      <div style={{ position: "relative", width: "100%" }}>
        <svg
          width="100%"
          viewBox={`0 0 ${W} ${H}`}
          preserveAspectRatio="xMidYMid meet"
          style={{ display: "block", overflow: "visible" }}
          aria-hidden
        >
          <defs>
            <linearGradient
              id="cc-svc-rainbow"
              x1="0"
              y1={PINCH_Y}
              x2="0"
              y2={PINCH_Y + COMBINED_HEIGHT}
              gradientUnits="userSpaceOnUse"
            >
              <stop offset="0" stopColor="var(--cc-col-cat)" />
              <stop offset="0.25" stopColor="var(--cc-col-bil)" />
              <stop offset="0.5" stopColor="var(--cc-col-ord)" />
              <stop offset="0.75" stopColor="var(--cc-col-shi)" />
              <stop offset="1" stopColor="var(--cc-col-usr)" />
            </linearGradient>
            <radialGradient id="cc-pinch-fade" cx="0.5" cy="0.5" r="0.5">
              <stop offset="0" stopColor="white" stopOpacity="0.55" />
              <stop offset="0.45" stopColor="white" stopOpacity="0.12" />
              <stop offset="1" stopColor="white" stopOpacity="0" />
            </radialGradient>
            <linearGradient
              id="cc-act3-line-fade"
              x1="0"
              y1="0"
              x2="0"
              y2={H}
              gradientUnits="userSpaceOnUse"
            >
              <stop offset="0" stopColor="black" />
              <stop offset={24 / H} stopColor="white" />
              <stop offset={1 - 24 / H} stopColor="white" />
              <stop offset="1" stopColor="black" />
            </linearGradient>
            <mask
              id="cc-act3-line-mask"
              maskUnits="userSpaceOnUse"
              x="0"
              y="0"
              width={W}
              height={H}
            >
              <rect
                x="0"
                y="0"
                width={W}
                height={H}
                fill="url(#cc-act3-line-fade)"
              />
            </mask>
          </defs>

          <g mask="url(#cc-act3-line-mask)">
            {SHARED_SERVICES.map((s, i) => (
              <path
                key={s.key}
                d={prePinch(i)}
                stroke={s.color}
                strokeWidth="var(--cc-line-w)"
                fill="none"
                strokeLinecap="round"
              />
            ))}
            <path
              d={postPinchPath}
              stroke="url(#cc-svc-rainbow)"
              strokeWidth="var(--cc-line-w)"
              fill="none"
              strokeLinecap="round"
            />
          </g>

          <circle cx={PINCH_X} cy={PINCH_Y} r="36" fill="url(#cc-pinch-fade)" />
          <circle id="pinch-dot" cx={PINCH_X} cy={PINCH_Y} r="4" fill="white" />

          <line
            x1={PINCH_X - 8}
            y1={PINCH_Y}
            x2="20"
            y2={PINCH_Y}
            stroke="var(--cc-ink)"
            strokeWidth="1.2"
            strokeDasharray="2 5"
            strokeLinecap="round"
            opacity="0.7"
          />
          <text
            x="20"
            y={PINCH_Y - 8}
            fontFamily="JetBrains Mono, monospace"
            fontSize="9"
            letterSpacing="1.4"
            fill="var(--cc-ink)"
            opacity="0.85"
          >
            FUSION COMPOSITION
          </text>
        </svg>

        <div
          id="pinch-anchor"
          style={{
            position: "absolute",
            left: "50%",
            top: `${(PINCH_Y / H) * 100}%`,
            width: 8,
            height: 8,
            transform: "translate(-50%, -50%)",
            pointerEvents: "none",
          }}
        />
      </div>

      <div className="tabbar-wrap">
        <div className="tabbar" role="tablist">
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
          <a href="#">↗ Fusion docs</a>
          <a href="#">↗ Architecture</a>
        </div>
      </div>
    </section>
  );
};
