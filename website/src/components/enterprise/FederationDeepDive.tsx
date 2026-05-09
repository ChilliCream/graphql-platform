"use client";

import React, { FC } from "react";

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

// Polyglot mesh diagram: five colored language nodes feeding a Fusion
// gateway. Stroke vocabulary matches Act5. Not a real architecture diagram —
// a readable schematic that says "any backend, any language".
const FederationDiagram: FC = () => {
  const lang = (
    label: string,
    cx: number,
    cy: number,
    color: string
  ): React.ReactNode => (
    <g key={label}>
      <circle
        cx={cx}
        cy={cy}
        r={22}
        fill="none"
        stroke={color}
        strokeWidth={1.6}
      />
      <circle cx={cx} cy={cy} r={4} fill={color} stroke="none" />
      <text
        x={cx}
        y={cy + 38}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.14em"
        textTransform="uppercase"
        textAnchor="middle"
      >
        {label}
      </text>
    </g>
  );

  return (
    <svg viewBox="0 0 500 400" preserveAspectRatio="xMidYMid meet">
      <defs>
        <linearGradient id="cc-fed-edge" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="rgba(245,241,234,0.08)" />
          <stop offset="50%" stopColor="rgba(245,241,234,0.42)" />
          <stop offset="100%" stopColor="rgba(245,241,234,0.08)" />
        </linearGradient>
      </defs>

      {/* connector edges (subgraphs → gateway) */}
      <g stroke="url(#cc-fed-edge)" strokeWidth={1.4} fill="none">
        <line x1={70} y1={70} x2={250} y2={210} />
        <line x1={250} y1={50} x2={250} y2={210} />
        <line x1={430} y1={70} x2={250} y2={210} />
        <line x1={70} y1={350} x2={250} y2={210} />
        <line x1={430} y1={350} x2={250} y2={210} />
      </g>

      {/* language subgraphs */}
      {lang("JAVA", 70, 70, "var(--cc-col-cat)")}
      {lang("GO", 250, 50, "var(--cc-col-bil)")}
      {lang("PYTHON", 430, 70, "var(--cc-col-ord)")}
      {lang("RUST", 70, 350, "var(--cc-col-shi)")}
      {lang(".NET", 430, 350, "var(--cc-col-usr)")}

      {/* fusion gateway core */}
      <g>
        <rect
          x={185}
          y={180}
          width={130}
          height={62}
          rx={14}
          fill="rgba(245,241,234,0.04)"
          stroke="var(--cc-ink)"
          strokeWidth={1.6}
        />
        <text
          x={250}
          y={210}
          fill="var(--cc-ink)"
          fontFamily="var(--cc-font-sans), sans-serif"
          fontSize="14"
          fontWeight={500}
          textAnchor="middle"
        >
          Fusion gateway
        </text>
        <text
          x={250}
          y={228}
          fill="var(--cc-ink-dim)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="9"
          letterSpacing="0.16em"
          textAnchor="middle"
        >
          BUILD-TIME COMPOSITION
        </text>
      </g>

      {/* clients edge */}
      <g
        stroke="rgba(245,241,234,0.32)"
        strokeWidth={1.4}
        strokeDasharray="4 4"
        fill="none"
      >
        <line x1={250} y1={242} x2={250} y2={300} />
      </g>
      <g>
        <rect
          x={170}
          y={300}
          width={160}
          height={36}
          rx={10}
          fill="none"
          stroke="var(--cc-ink-faint)"
          strokeWidth={1.4}
        />
        <text
          x={250}
          y={323}
          fill="var(--cc-ink-dim)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="10"
          letterSpacing="0.16em"
          textAnchor="middle"
        >
          CLIENTS · AGENTS
        </text>
      </g>
    </svg>
  );
};

export const FederationDeepDive: FC = () => {
  return (
    <section className="cc-ent-section cc-ent-federation">
      <div className="cc-section-label">
        <span className="num">06</span> Federation
      </div>
      <div className="cc-ent-federation-inner">
        <div className="cc-ent-federation-grid">
          <div className="cc-ent-federation-copy">
            <div className="eyebrow">Federation deep-dive</div>
            <h2 className="display">
              Federate any backend in any language, on .NET or off.
            </h2>
            <p>
              Fusion composes the supergraph at build time. Subgraphs stay in
              the languages and frameworks your teams already use. There is no
              runtime gateway DSL to learn, no Rust router to keep alive, and no
              one team that owns "the federation".
            </p>
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
                  Polyglot subgraphs — Java, Go, Python, Rust, .NET, Kotlin.
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
                  No vendor lock-in: GraphQL is the only contract between
                  layers.
                </span>
              </li>
            </ul>
            <a href="/docs/fusion" className="cc-btn cc-btn-ghost">
              See the architecture →
            </a>
          </div>
          <div className="cc-ent-federation-diagram">
            <FederationDiagram />
          </div>
        </div>
      </div>
    </section>
  );
};
