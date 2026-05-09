"use client";

import React, { FC } from "react";

const CHIPS: readonly string[] = [
  "Helm chart",
  "Docker / OCI",
  "Air-gapped tarball",
  "BYO Postgres",
  "BYO object store",
  "BYO OTEL backend",
];

// Stroke diagram: dashed VPC boundary, three internal services + Nitro,
// closed lock — no traffic leaves the box.
const AirGapDiagram: FC = () => (
  <svg viewBox="0 0 500 400" preserveAspectRatio="xMidYMid meet">
    {/* dashed VPC boundary */}
    <rect
      x={28}
      y={28}
      width={444}
      height={344}
      rx={20}
      fill="rgba(245,241,234,0.02)"
      stroke="var(--cc-ink-faint)"
      strokeWidth={1.6}
      strokeDasharray="6 6"
    />
    <text
      x={50}
      y={56}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.18em"
    >
      YOUR VPC · NO EGRESS
    </text>

    {/* lock at top-right */}
    <g
      stroke="var(--cc-col-ord)"
      strokeWidth={1.6}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x={420} y={48} width={32} height={24} rx={3} />
      <path d="M 426 48 V 38 a 10 10 0 0 1 20 0 V 48" />
      <line x1={436} y1={56} x2={436} y2={64} />
    </g>

    {/* three internal subgraph nodes (left column) */}
    <g
      stroke="var(--cc-ink)"
      strokeWidth={1.6}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x={60} y={110} width={100} height={48} rx={10} />
      <rect x={60} y={180} width={100} height={48} rx={10} />
      <rect x={60} y={250} width={100} height={48} rx={10} />
    </g>
    <g
      fill="var(--cc-ink)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.14em"
      textAnchor="middle"
    >
      <text x={110} y={138}>
        SUBGRAPH A
      </text>
      <text x={110} y={208}>
        SUBGRAPH B
      </text>
      <text x={110} y={278}>
        SUBGRAPH C
      </text>
    </g>

    {/* connectors to Nitro */}
    <g stroke="rgba(245,241,234,0.32)" strokeWidth={1.4} fill="none">
      <line x1={160} y1={134} x2={300} y2={200} />
      <line x1={160} y1={204} x2={300} y2={200} />
      <line x1={160} y1={274} x2={300} y2={200} />
    </g>

    {/* Nitro core */}
    <g>
      <rect
        x={300}
        y={170}
        width={140}
        height={64}
        rx={14}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={370}
        y={199}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="14"
        fontWeight={500}
        textAnchor="middle"
      >
        Nitro Self-Hosted
      </text>
      <text
        x={370}
        y={219}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="9"
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        AIR-GAPPED
      </text>
    </g>

    {/* internal clients */}
    <g stroke="var(--cc-ink-faint)" strokeWidth={1.4} fill="none">
      <rect x={300} y={290} width={140} height={36} rx={10} />
    </g>
    <text
      x={370}
      y={313}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.16em"
      textAnchor="middle"
    >
      INTERNAL CLIENTS
    </text>
    <line
      x1={370}
      y1={234}
      x2={370}
      y2={290}
      stroke="rgba(245,241,234,0.32)"
      strokeWidth={1.4}
      strokeDasharray="3 3"
    />
  </svg>
);

export const SelfHostedAirGapped: FC = () => {
  return (
    <section className="cc-ent-section cc-ent-airgap">
      <div className="cc-section-label">
        <span className="num">09</span> Self-hosted
      </div>
      <div className="cc-ent-airgap-inner">
        <div className="cc-ent-airgap-grid">
          <div className="cc-ent-airgap-diagram">
            <AirGapDiagram />
          </div>
          <div className="cc-ent-airgap-copy">
            <div className="eyebrow">Self-hosted & air-gapped</div>
            <h2 className="display">No traffic leaves your VPC. Ever.</h2>
            <p>
              Banks, insurers, defence, and regulated logistics buy on this line
              first. Nitro Self-Hosted runs entirely inside your network, with
              your database, your object store, and your observability backend.
              We ship the binaries; you keep the data.
            </p>
            <div className="cc-ent-airgap-chips">
              {CHIPS.map((c) => (
                <span key={c} className="cc-ent-airgap-chip">
                  {c}
                </span>
              ))}
            </div>
            <a href="/docs/nitro/self-hosted" className="cc-btn cc-btn-ghost">
              Read deployment docs →
            </a>
          </div>
        </div>
      </div>
    </section>
  );
};
