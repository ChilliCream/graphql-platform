"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";

const Check: FC = () => (
  <svg viewBox="0 0 16 16" width="14" height="14" aria-hidden>
    <path
      d="M3 8.5 L6.5 12 L13 4.5"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </svg>
);

interface SubgraphNode {
  readonly key: string;
  readonly language: string;
  readonly service: string;
  readonly cx: number;
  readonly cy: number;
  readonly color: string;
}

// Six polyglot subgraphs composing into one Fusion gateway. Each node is a
// real (named) backend with an ownership label so the diagram reads as
// "every team in their language, one mesh" rather than a stock node graph.
const SUBGRAPHS: readonly SubgraphNode[] = [
  {
    key: "java",
    language: "Java",
    service: "Identity",
    cx: 110,
    cy: 90,
    color: "var(--cc-col-cat)",
  },
  {
    key: "go",
    language: "Go",
    service: "Inventory",
    cx: 500,
    cy: 60,
    color: "var(--cc-col-bil)",
  },
  {
    key: "python",
    language: "Python",
    service: "Pricing ML",
    cx: 890,
    cy: 90,
    color: "var(--cc-col-ord)",
  },
  {
    key: "rust",
    language: "Rust",
    service: "Risk",
    cx: 110,
    cy: 410,
    color: "var(--cc-col-shi)",
  },
  {
    key: "kotlin",
    language: "Kotlin",
    service: "Orders",
    cx: 500,
    cy: 440,
    color: "var(--cc-col-usr)",
  },
  {
    key: "dotnet",
    language: ".NET",
    service: "Billing",
    cx: 890,
    cy: 410,
    color: "var(--cc-accent, var(--cc-col-shi))",
  },
];

// The federation diagram is the page's load-bearing claim that
// differentiates Fusion from Apollo (polyglot federation). It must dominate
// the band: 1000x540 viewBox, a glowing Fusion gateway at center with a
// distinct accent treatment, and named language + service labels on each
// subgraph so it reads as a real production stack.
const FederationDiagram: FC = () => {
  const cx = 500;
  const cy = 250;

  return (
    <svg
      viewBox="0 0 1000 540"
      preserveAspectRatio="xMidYMid meet"
      role="img"
      aria-label="Six polyglot subgraphs (Java, Go, Python, Rust, Kotlin, .NET) composing into one Fusion gateway, which serves clients and agents."
    >
      <defs>
        <linearGradient id="cc-fed-edge" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="rgba(245,241,234,0.10)" />
          <stop offset="50%" stopColor="rgba(245,241,234,0.45)" />
          <stop offset="100%" stopColor="rgba(245,241,234,0.10)" />
        </linearGradient>
        <radialGradient id="cc-fed-gateway-glow" cx="50%" cy="50%" r="60%">
          <stop
            offset="0%"
            stopColor="var(--cc-accent, #88a8e8)"
            stopOpacity="0.42"
          />
          <stop
            offset="60%"
            stopColor="var(--cc-accent, #88a8e8)"
            stopOpacity="0.10"
          />
          <stop
            offset="100%"
            stopColor="var(--cc-accent, #88a8e8)"
            stopOpacity="0"
          />
        </radialGradient>
        <linearGradient id="cc-fed-gateway-fill" x1="0" x2="1" y1="0" y2="1">
          <stop
            offset="0%"
            stopColor="var(--cc-accent, #88a8e8)"
            stopOpacity="0.18"
          />
          <stop offset="100%" stopColor="rgba(245,241,234,0.04)" />
        </linearGradient>
      </defs>

      {/* gateway glow halo */}
      <circle cx={cx} cy={cy} r={220} fill="url(#cc-fed-gateway-glow)" />

      {/* connector edges from each subgraph into gateway */}
      <g stroke="url(#cc-fed-edge)" strokeWidth={1.6} fill="none">
        {SUBGRAPHS.map((s) => (
          <line key={`edge-${s.key}`} x1={s.cx} y1={s.cy} x2={cx} y2={cy} />
        ))}
      </g>

      {/* polyglot subgraph nodes */}
      {SUBGRAPHS.map((s) => (
        <g key={s.key}>
          <circle
            cx={s.cx}
            cy={s.cy}
            r={32}
            fill="rgba(10,13,24,0.7)"
            stroke={s.color}
            strokeWidth={1.6}
          />
          <circle cx={s.cx} cy={s.cy} r={5} fill={s.color} stroke="none" />
          <text
            x={s.cx}
            y={s.cy + 56}
            fill="var(--cc-ink)"
            fontFamily="var(--cc-font-sans), sans-serif"
            fontSize="14"
            fontWeight={500}
            textAnchor="middle"
          >
            {s.language}
          </text>
          <text
            x={s.cx}
            y={s.cy + 76}
            fill="var(--cc-ink-dim)"
            fontFamily="var(--cc-font-mono), monospace"
            fontSize="10"
            letterSpacing="0.16em"
            textAnchor="middle"
          >
            {s.service.toUpperCase()}
          </text>
        </g>
      ))}

      {/* fusion gateway: distinct accent treatment, scaled up, glowing */}
      <g>
        <rect
          x={cx - 130}
          y={cy - 48}
          width={260}
          height={96}
          rx={20}
          fill="url(#cc-fed-gateway-fill)"
          stroke="var(--cc-accent, var(--cc-ink))"
          strokeWidth={2}
        />
        <text
          x={cx}
          y={cy - 8}
          fill="var(--cc-ink)"
          fontFamily="var(--cc-font-sans), sans-serif"
          fontSize="22"
          fontWeight={500}
          textAnchor="middle"
          letterSpacing="-0.02em"
        >
          Fusion
        </text>
        <text
          x={cx}
          y={cy + 14}
          fill="var(--cc-ink-dim)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="10"
          letterSpacing="0.18em"
          textAnchor="middle"
        >
          BUILD-TIME COMPOSITION
        </text>
        <text
          x={cx}
          y={cy + 32}
          fill="var(--cc-ink-dim)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="10"
          letterSpacing="0.18em"
          textAnchor="middle"
        >
          GATEWAY · REGISTRY · GOVERNANCE
        </text>
      </g>

      {/* downstream client/agent edge */}
      <g
        stroke="rgba(245,241,234,0.32)"
        strokeWidth={1.4}
        strokeDasharray="4 4"
        fill="none"
      >
        <line x1={cx} y1={cy + 48} x2={cx} y2={500} />
      </g>
      <g>
        <rect
          x={cx - 130}
          y={500}
          width={260}
          height={36}
          rx={10}
          fill="none"
          stroke="var(--cc-ink-faint)"
          strokeWidth={1.4}
        />
        <text
          x={cx}
          y={524}
          fill="var(--cc-ink-dim)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="11"
          letterSpacing="0.18em"
          textAnchor="middle"
        >
          CLIENTS · MOBILE · AGENTS
        </text>
      </g>
    </svg>
  );
};

export const FederationDeepDive: FC = () => {
  return (
    <Band variant="default" ariaLabel="Federation deep-dive">
      <div className="cc-section-label">
        <span className="num">06</span> Federation
      </div>
      <div className="cc-ent-federation-inner">
        <div className="cc-ent-federation-head">
          <div className="eyebrow">Federation deep-dive</div>
          <h2 className="display">
            Federate any backend in any language, on .NET or off.
          </h2>
          <p>
            Fusion composes the supergraph at build time. Subgraphs stay in the
            languages and frameworks your teams already use. There is no runtime
            gateway DSL to learn, no Rust router to keep alive, and no one team
            that owns "the federation".
          </p>
        </div>

        <div className="cc-ent-federation-diagram">
          <FederationDiagram />
        </div>

        <div className="cc-ent-federation-foot">
          <ul className="cc-ent-federation-bullets">
            <li>
              <Check />
              <span>
                Build-time composition with full schema lineage and
                breaking-change detection.
              </span>
            </li>
            <li>
              <Check />
              <span>
                Polyglot subgraphs — Java, Go, Python, Rust, Kotlin, .NET.
              </span>
            </li>
            <li>
              <Check />
              <span>
                Run the gateway on Nitro Cloud, on your infra, or air-gapped
                on-prem.
              </span>
            </li>
            <li>
              <Check />
              <span>
                No vendor lock-in: GraphQL is the only contract between layers.
              </span>
            </li>
          </ul>
          <a href="/docs/fusion" className="cc-btn cc-btn-ghost">
            See the architecture →
          </a>
        </div>
      </div>
    </Band>
  );
};
