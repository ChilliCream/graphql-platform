"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import type { DiagramKind } from "@/data/solutions/types";

// Section 04: the concept diagram. Switch on DiagramKind, render an inline
// SVG using the same brewer-icon vocabulary as FederationDeepDive and
// SelfHostedAirGapped: stroke 1.6, rounded caps, no fills, language tints
// drawn from --cc-col-* tokens. Each variant is opinionated, not a generic
// flow chart.
//
// The diagram lives in its own inverted Band so it reads as the page beat,
// not a card-side-by-side-with-text. Polyglot in particular renders real
// language text marks (not monogram circles) with per-node accent tints
// and a glowing accent-colored gateway core.

interface ConceptDiagramProps {
  readonly kind: DiagramKind;
}

interface LanguageNode {
  readonly label: string;
  readonly color: string;
}

const POLYGLOT_NODES: readonly LanguageNode[] = [
  { label: "JAVA", color: "var(--cc-col-cat)" },
  { label: "GO", color: "var(--cc-col-bil)" },
  { label: "PYTHON", color: "var(--cc-col-ord)" },
  { label: "RUST", color: "var(--cc-col-shi)" },
  { label: ".NET", color: "var(--cc-col-usr)" },
];

const FEDERATION_TEAMS: readonly { label: string }[] = [
  { label: "TEAM IDENTITY" },
  { label: "TEAM ORDERS" },
  { label: "TEAM CATALOG" },
  { label: "TEAM SUPPORT" },
];

const AGENT_CLIENTS: readonly { label: string }[] = [
  { label: "CURSOR" },
  { label: "CLAUDE" },
  { label: "COPILOT" },
  { label: "IN-HOUSE" },
];

const EVENT_TOPICS: readonly { label: string }[] = [
  { label: "orders.placed" },
  { label: "orders.shipped" },
  { label: "billing.charged" },
  { label: "audit.events" },
];

// Reusable subgraph node: a rounded rect with a colored ring, a small
// filled inner dot, and a tag label below. Mirrors FederationDeepDive's
// "lang" helper but parameterized for both polyglot (colored) and
// federation (cream-on-glass) nodes.
const SubgraphNode: FC<{
  readonly label: string;
  readonly cx: number;
  readonly cy: number;
  readonly color: string;
  readonly r?: number;
}> = ({ label, cx, cy, color, r = 22 }) => (
  <g>
    <circle
      cx={cx}
      cy={cy}
      r={r}
      fill="none"
      stroke={color}
      strokeWidth={1.6}
    />
    <circle cx={cx} cy={cy} r={4} fill={color} stroke="none" />
    <text
      x={cx}
      y={cy + r + 16}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.14em"
      textAnchor="middle"
    >
      {label}
    </text>
  </g>
);

const GatewayCore: FC<{
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  readonly title: string;
  readonly subtitle: string;
}> = ({ x, y, w, h, title, subtitle }) => (
  <g>
    <rect
      x={x}
      y={y}
      width={w}
      height={h}
      rx={14}
      fill="rgba(245,241,234,0.04)"
      stroke="var(--cc-ink)"
      strokeWidth={1.6}
    />
    <text
      x={x + w / 2}
      y={y + h / 2 - 4}
      fill="var(--cc-ink)"
      fontFamily="var(--cc-font-sans), sans-serif"
      fontSize="14"
      fontWeight={500}
      textAnchor="middle"
    >
      {title}
    </text>
    <text
      x={x + w / 2}
      y={y + h / 2 + 14}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="9"
      letterSpacing="0.16em"
      textAnchor="middle"
    >
      {subtitle}
    </text>
  </g>
);

// Polyglot language node: real text label inside a colored pill, not a
// monogram circle. The pill is per-language tinted using the --cc-col-*
// palette so the polyglot pitch ("five different stacks") is encoded in
// chrome before the labels are read.
const LanguagePill: FC<{
  readonly label: string;
  readonly cx: number;
  readonly cy: number;
  readonly color: string;
}> = ({ label, cx, cy, color }) => {
  const w = 110;
  const h = 44;
  return (
    <g>
      <rect
        x={cx - w / 2}
        y={cy - h / 2}
        width={w}
        height={h}
        rx={10}
        fill="rgba(245,241,234,0.04)"
        stroke={color}
        strokeWidth={1.6}
      />
      <text
        x={cx}
        y={cy + 5}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="15"
        fontWeight={500}
        letterSpacing="-0.01em"
        textAnchor="middle"
      >
        {label}
      </text>
    </g>
  );
};

// Glowing accent-colored gateway core. Distinct from any other rectangle
// on the diagram: filled with the page accent at low opacity, ringed with
// the accent line, with an inner radial glow so it reads as a translation
// surface, not just another labeled box.
const GatewayCoreGlow: FC<{
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  readonly title: string;
  readonly subtitle: string;
  readonly gradientId: string;
}> = ({ x, y, w, h, title, subtitle, gradientId }) => (
  <g>
    <defs>
      <radialGradient id={gradientId} cx="50%" cy="50%" r="60%">
        <stop
          offset="0%"
          stopColor="var(--cc-accent, #f5f1ea)"
          stopOpacity="0.42"
        />
        <stop
          offset="100%"
          stopColor="var(--cc-accent, #f5f1ea)"
          stopOpacity="0"
        />
      </radialGradient>
    </defs>
    <rect
      x={x - 16}
      y={y - 16}
      width={w + 32}
      height={h + 32}
      rx={20}
      fill={`url(#${gradientId})`}
    />
    <rect
      x={x}
      y={y}
      width={w}
      height={h}
      rx={14}
      fill="rgba(245,241,234,0.06)"
      stroke="var(--cc-accent, var(--cc-ink))"
      strokeWidth={1.8}
    />
    <text
      x={x + w / 2}
      y={y + h / 2 - 4}
      fill="var(--cc-ink)"
      fontFamily="var(--cc-font-sans), sans-serif"
      fontSize="16"
      fontWeight={500}
      textAnchor="middle"
    >
      {title}
    </text>
    <text
      x={x + w / 2}
      y={y + h / 2 + 16}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.18em"
      textAnchor="middle"
    >
      {subtitle}
    </text>
  </g>
);

// ============================================================
// Variant: polyglot — five real-language nodes feeding a Fusion gateway
// ============================================================
const PolyglotDiagram: FC = () => (
  <svg viewBox="0 0 800 450" preserveAspectRatio="xMidYMid meet" aria-hidden>
    <defs>
      <linearGradient id="cc-sl-edge" x1="0" x2="1" y1="0" y2="0">
        <stop offset="0%" stopColor="rgba(245,241,234,0.08)" />
        <stop offset="50%" stopColor="rgba(245,241,234,0.42)" />
        <stop offset="100%" stopColor="rgba(245,241,234,0.08)" />
      </linearGradient>
    </defs>

    <g stroke="url(#cc-sl-edge)" strokeWidth={1.4} fill="none">
      <line x1={100} y1={90} x2={400} y2={235} />
      <line x1={250} y1={70} x2={400} y2={235} />
      <line x1={400} y1={50} x2={400} y2={235} />
      <line x1={550} y1={70} x2={400} y2={235} />
      <line x1={700} y1={90} x2={400} y2={235} />
    </g>

    {/* language subgraphs along the top arc, real text marks per node */}
    <LanguagePill
      label="Java"
      cx={100}
      cy={90}
      color={POLYGLOT_NODES[0].color}
    />
    <LanguagePill label="Go" cx={250} cy={70} color={POLYGLOT_NODES[1].color} />
    <LanguagePill
      label="Python"
      cx={400}
      cy={50}
      color={POLYGLOT_NODES[2].color}
    />
    <LanguagePill
      label="Rust"
      cx={550}
      cy={70}
      color={POLYGLOT_NODES[3].color}
    />
    <LanguagePill
      label=".NET"
      cx={700}
      cy={90}
      color={POLYGLOT_NODES[4].color}
    />

    <GatewayCoreGlow
      x={310}
      y={200}
      w={180}
      h={72}
      title="Fusion gateway"
      subtitle="BUILD-TIME COMPOSITION"
      gradientId="cc-sl-gateway-glow-poly"
    />

    {/* dashed clients link */}
    <line
      x1={400}
      y1={272}
      x2={400}
      y2={340}
      stroke="rgba(245,241,234,0.32)"
      strokeWidth={1.4}
      strokeDasharray="4 4"
    />
    <rect
      x={290}
      y={340}
      width={220}
      height={42}
      rx={10}
      fill="none"
      stroke="var(--cc-ink-faint)"
      strokeWidth={1.4}
    />
    <text
      x={400}
      y={366}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="11"
      letterSpacing="0.18em"
      textAnchor="middle"
    >
      WEB · MOBILE · AGENTS
    </text>
  </svg>
);

// ============================================================
// Variant: federation — four team-owned subgraphs
// ============================================================
const FederationDiagram: FC = () => (
  <svg viewBox="0 0 800 450" preserveAspectRatio="xMidYMid meet" aria-hidden>
    <defs>
      <linearGradient id="cc-sl-edge-fed" x1="0" x2="1" y1="0" y2="0">
        <stop offset="0%" stopColor="rgba(245,241,234,0.08)" />
        <stop offset="50%" stopColor="rgba(245,241,234,0.42)" />
        <stop offset="100%" stopColor="rgba(245,241,234,0.08)" />
      </linearGradient>
    </defs>

    {/* edges from each team to gateway */}
    <g stroke="url(#cc-sl-edge-fed)" strokeWidth={1.4} fill="none">
      <line x1={140} y1={90} x2={400} y2={235} />
      <line x1={300} y1={70} x2={400} y2={235} />
      <line x1={500} y1={70} x2={400} y2={235} />
      <line x1={660} y1={90} x2={400} y2={235} />
    </g>

    {FEDERATION_TEAMS.map((t, i) => {
      const xs = [140, 300, 500, 660];
      const ys = [90, 70, 70, 90];
      return (
        <SubgraphNode
          key={t.label}
          label={t.label}
          cx={xs[i]}
          cy={ys[i]}
          color="var(--cc-ink)"
        />
      );
    })}

    <GatewayCore
      x={325}
      y={205}
      w={150}
      h={62}
      title="Fusion gateway"
      subtitle="SIGNED · CI-COMPOSED"
    />

    <line
      x1={400}
      y1={267}
      x2={400}
      y2={340}
      stroke="rgba(245,241,234,0.32)"
      strokeWidth={1.4}
      strokeDasharray="4 4"
    />
    <rect
      x={290}
      y={340}
      width={220}
      height={42}
      rx={10}
      fill="none"
      stroke="var(--cc-ink-faint)"
      strokeWidth={1.4}
    />
    <text
      x={400}
      y={366}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="11"
      letterSpacing="0.18em"
      textAnchor="middle"
    >
      ONE TYPED CONTRACT
    </text>
  </svg>
);

// ============================================================
// Variant: single-graph — one Hot Chocolate service, three client lanes
// ============================================================
const SingleGraphDiagram: FC = () => (
  <svg viewBox="0 0 800 450" preserveAspectRatio="xMidYMid meet" aria-hidden>
    <defs>
      <linearGradient id="cc-sl-edge-sg" x1="0" x2="1" y1="0" y2="0">
        <stop offset="0%" stopColor="rgba(245,241,234,0.08)" />
        <stop offset="50%" stopColor="rgba(245,241,234,0.42)" />
        <stop offset="100%" stopColor="rgba(245,241,234,0.08)" />
      </linearGradient>
    </defs>

    {/* one service centered */}
    <GatewayCore
      x={300}
      y={195}
      w={200}
      h={72}
      title="Hot Chocolate"
      subtitle="SCHEMA · RESOLVERS · DI"
    />

    {/* three client lanes below */}
    <g stroke="url(#cc-sl-edge-sg)" strokeWidth={1.4} fill="none">
      <line x1={400} y1={267} x2={180} y2={355} />
      <line x1={400} y1={267} x2={400} y2={355} />
      <line x1={400} y1={267} x2={620} y2={355} />
    </g>

    {[
      { x: 180, label: "WEB" },
      { x: 400, label: "MOBILE" },
      { x: 620, label: "AGENTS" },
    ].map((c) => (
      <g key={c.label}>
        <rect
          x={c.x - 70}
          y={355}
          width={140}
          height={42}
          rx={10}
          fill="none"
          stroke="var(--cc-ink-faint)"
          strokeWidth={1.4}
        />
        <text
          x={c.x}
          y={381}
          fill="var(--cc-ink)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="11"
          letterSpacing="0.18em"
          textAnchor="middle"
        >
          {c.label}
        </text>
      </g>
    ))}

    {/* schema pill above */}
    <rect
      x={310}
      y={92}
      width={180}
      height={36}
      rx={10}
      fill="none"
      stroke="var(--cc-ink-faint)"
      strokeWidth={1.4}
      strokeDasharray="4 4"
    />
    <text
      x={400}
      y={115}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.18em"
      textAnchor="middle"
    >
      ONE TYPED SCHEMA
    </text>
    <line
      x1={400}
      y1={128}
      x2={400}
      y2={195}
      stroke="rgba(245,241,234,0.32)"
      strokeWidth={1.4}
      strokeDasharray="3 3"
    />
  </svg>
);

// ============================================================
// Variant: agents — graph -> MCP -> four agent clients
// ============================================================
const AgentsDiagram: FC = () => (
  <svg viewBox="0 0 800 450" preserveAspectRatio="xMidYMid meet" aria-hidden>
    <defs>
      <linearGradient id="cc-sl-edge-ag" x1="0" x2="1" y1="0" y2="0">
        <stop offset="0%" stopColor="rgba(245,241,234,0.08)" />
        <stop offset="50%" stopColor="rgba(245,241,234,0.42)" />
        <stop offset="100%" stopColor="rgba(245,241,234,0.08)" />
      </linearGradient>
    </defs>

    {/* graph (top) */}
    <GatewayCore
      x={310}
      y={50}
      w={180}
      h={64}
      title="GraphQL surface"
      subtitle="HOT CHOCOLATE · FUSION"
    />

    {/* mcp (middle) */}
    <line
      x1={400}
      y1={114}
      x2={400}
      y2={180}
      stroke="rgba(245,241,234,0.32)"
      strokeWidth={1.4}
      strokeDasharray="3 3"
    />
    <GatewayCore
      x={310}
      y={180}
      w={180}
      h={64}
      title="MCP"
      subtitle="MODEL CONTEXT PROTOCOL"
    />

    {/* fan-out to four agents */}
    <g stroke="url(#cc-sl-edge-ag)" strokeWidth={1.4} fill="none">
      <line x1={400} y1={244} x2={140} y2={355} />
      <line x1={400} y1={244} x2={313} y2={355} />
      <line x1={400} y1={244} x2={487} y2={355} />
      <line x1={400} y1={244} x2={660} y2={355} />
    </g>

    {AGENT_CLIENTS.map((c, i) => {
      const xs = [140, 313, 487, 660];
      return (
        <g key={c.label}>
          <rect
            x={xs[i] - 60}
            y={355}
            width={120}
            height={42}
            rx={10}
            fill="none"
            stroke="var(--cc-amber)"
            strokeOpacity="0.55"
            strokeWidth={1.4}
          />
          <text
            x={xs[i]}
            y={381}
            fill="var(--cc-ink)"
            fontFamily="var(--cc-font-mono), monospace"
            fontSize="11"
            letterSpacing="0.18em"
            textAnchor="middle"
          >
            {c.label}
          </text>
        </g>
      );
    })}
  </svg>
);

// ============================================================
// Variant: event-bus — services -> Mocha bus -> consumers, with topics
// ============================================================
const EventBusDiagram: FC = () => (
  <svg viewBox="0 0 800 450" preserveAspectRatio="xMidYMid meet" aria-hidden>
    {/* producers (left column) */}
    {[
      { y: 80, label: "ORDERS" },
      { y: 170, label: "BILLING" },
      { y: 260, label: "INVENTORY" },
    ].map((p) => (
      <g key={p.label}>
        <rect
          x={60}
          y={p.y}
          width={140}
          height={48}
          rx={10}
          fill="rgba(245,241,234,0.04)"
          stroke="var(--cc-ink)"
          strokeWidth={1.6}
        />
        <text
          x={130}
          y={p.y + 30}
          fill="var(--cc-ink)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="11"
          letterSpacing="0.16em"
          textAnchor="middle"
        >
          {p.label}
        </text>
      </g>
    ))}

    {/* bus (center) */}
    <g>
      <rect
        x={310}
        y={100}
        width={180}
        height={250}
        rx={18}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-amber)"
        strokeOpacity="0.55"
        strokeWidth={1.6}
      />
      <text
        x={400}
        y={128}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="14"
        fontWeight={500}
        textAnchor="middle"
      >
        Mocha bus
      </text>
      <text
        x={400}
        y={148}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="9"
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        KAFKA · SB · NATS
      </text>

      {/* topic chips inside the bus */}
      {EVENT_TOPICS.map((t, i) => (
        <g key={t.label}>
          <rect
            x={326}
            y={170 + i * 38}
            width={148}
            height={26}
            rx={6}
            fill="rgba(255,255,255,0.04)"
            stroke="var(--cc-ink-faint)"
            strokeWidth={1}
          />
          <text
            x={400}
            y={187 + i * 38}
            fill="var(--cc-ink)"
            fontFamily="var(--cc-font-mono), monospace"
            fontSize="10"
            letterSpacing="0.06em"
            textAnchor="middle"
          >
            {t.label}
          </text>
        </g>
      ))}
    </g>

    {/* producer -> bus arrows */}
    <g stroke="rgba(245,241,234,0.32)" strokeWidth={1.4} fill="none">
      <line x1={200} y1={104} x2={310} y2={170} />
      <line x1={200} y1={194} x2={310} y2={208} />
      <line x1={200} y1={284} x2={310} y2={246} />
    </g>

    {/* bus -> consumer arrows */}
    <g stroke="rgba(245,241,234,0.32)" strokeWidth={1.4} fill="none">
      <line x1={490} y1={170} x2={600} y2={104} />
      <line x1={490} y1={208} x2={600} y2={194} />
      <line x1={490} y1={246} x2={600} y2={284} />
    </g>

    {/* consumers (right column) */}
    {[
      { y: 80, label: "WEBHOOKS" },
      { y: 170, label: "ANALYTICS" },
      { y: 260, label: "AUDIT SINK" },
    ].map((c) => (
      <g key={c.label}>
        <rect
          x={600}
          y={c.y}
          width={140}
          height={48}
          rx={10}
          fill="rgba(245,241,234,0.04)"
          stroke="var(--cc-ink)"
          strokeWidth={1.6}
        />
        <text
          x={670}
          y={c.y + 30}
          fill="var(--cc-ink)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="11"
          letterSpacing="0.16em"
          textAnchor="middle"
        >
          {c.label}
        </text>
      </g>
    ))}

    {/* subscriptions out the bottom */}
    <line
      x1={400}
      y1={350}
      x2={400}
      y2={400}
      stroke="rgba(245,241,234,0.32)"
      strokeWidth={1.4}
      strokeDasharray="3 3"
    />
    <rect
      x={290}
      y={400}
      width={220}
      height={36}
      rx={10}
      fill="none"
      stroke="var(--cc-amber)"
      strokeOpacity="0.55"
      strokeWidth={1.4}
    />
    <text
      x={400}
      y={423}
      fill="var(--cc-ink)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.18em"
      textAnchor="middle"
    >
      SUBSCRIPTIONS · WS · SSE
    </text>
  </svg>
);

// ============================================================
// Variant: compliance — dashed VPC boundary, locked egress, internal flow
// ============================================================
const ComplianceDiagram: FC = () => (
  <svg viewBox="0 0 800 450" preserveAspectRatio="xMidYMid meet" aria-hidden>
    {/* dashed VPC boundary */}
    <rect
      x={40}
      y={40}
      width={720}
      height={370}
      rx={20}
      fill="rgba(245,241,234,0.02)"
      stroke="var(--cc-ink-faint)"
      strokeWidth={1.6}
      strokeDasharray="6 6"
    />
    <text
      x={64}
      y={68}
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
      <rect x={700} y={56} width={32} height={24} rx={3} />
      <path d="M 706 56 V 46 a 10 10 0 0 1 20 0 V 56" />
      <line x1={716} y1={64} x2={716} y2={72} />
    </g>

    {/* three internal subgraphs */}
    {[
      { y: 110, label: "SUBGRAPH A" },
      { y: 188, label: "SUBGRAPH B" },
      { y: 266, label: "SUBGRAPH C" },
    ].map((s) => (
      <g key={s.label}>
        <rect
          x={88}
          y={s.y}
          width={150}
          height={52}
          rx={10}
          fill="rgba(245,241,234,0.04)"
          stroke="var(--cc-ink)"
          strokeWidth={1.6}
        />
        <text
          x={163}
          y={s.y + 32}
          fill="var(--cc-ink)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="11"
          letterSpacing="0.14em"
          textAnchor="middle"
        >
          {s.label}
        </text>
      </g>
    ))}

    {/* connectors to gateway */}
    <g stroke="rgba(245,241,234,0.32)" strokeWidth={1.4} fill="none">
      <line x1={238} y1={136} x2={420} y2={224} />
      <line x1={238} y1={214} x2={420} y2={224} />
      <line x1={238} y1={292} x2={420} y2={224} />
    </g>

    {/* gateway core */}
    <GatewayCore
      x={420}
      y={188}
      w={200}
      h={72}
      title="Nitro Self-Hosted"
      subtitle="AIR-GAPPED · SIGNED · AUDITED"
    />

    {/* internal clients */}
    <line
      x1={520}
      y1={260}
      x2={520}
      y2={320}
      stroke="rgba(245,241,234,0.32)"
      strokeWidth={1.4}
      strokeDasharray="3 3"
    />
    <rect
      x={420}
      y={320}
      width={200}
      height={42}
      rx={10}
      fill="none"
      stroke="var(--cc-ink-faint)"
      strokeWidth={1.4}
    />
    <text
      x={520}
      y={346}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="11"
      letterSpacing="0.18em"
      textAnchor="middle"
    >
      INTERNAL CLIENTS ONLY
    </text>

    {/* mocha audit box (right side) */}
    <g>
      <rect
        x={660}
        y={188}
        width={80}
        height={72}
        rx={10}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-amber)"
        strokeOpacity="0.55"
        strokeWidth={1.6}
      />
      <text
        x={700}
        y={221}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="12"
        fontWeight={500}
        textAnchor="middle"
      >
        Mocha
      </text>
      <text
        x={700}
        y={239}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="9"
        letterSpacing="0.14em"
        textAnchor="middle"
      >
        AUDIT
      </text>
    </g>
    <line
      x1={620}
      y1={224}
      x2={660}
      y2={224}
      stroke="rgba(245,241,234,0.32)"
      strokeWidth={1.4}
    />
  </svg>
);

const VARIANTS: Record<DiagramKind, React.FC> = {
  polyglot: PolyglotDiagram,
  federation: FederationDiagram,
  "single-graph": SingleGraphDiagram,
  agents: AgentsDiagram,
  "event-bus": EventBusDiagram,
  compliance: ComplianceDiagram,
};

const TITLES: Record<DiagramKind, string> = {
  polyglot: "Five subgraphs, one supergraph, zero rewrites.",
  federation: "Each team owns a subgraph. The contract is the supergraph.",
  "single-graph": "One service. One typed schema. Every client.",
  agents: "Your graph, exposed over MCP, callable from any agent client.",
  "event-bus": "Producers, topics, consumers — one bus you can read.",
  compliance: "Everything inside the VPC. Nothing leaves the boundary.",
};

export const ConceptDiagram: FC<ConceptDiagramProps> = ({ kind }) => {
  const Variant = VARIANTS[kind];
  return (
    <Band variant="inverted" ariaLabel="Architecture">
      <div className="cc-sl-section cc-sl-diagram">
        <div className="cc-section-label">
          <span className="num">04</span> Architecture
        </div>
        <div className="cc-sl-diagram-head">
          <div className="eyebrow">The shape</div>
          <h3>{TITLES[kind]}</h3>
        </div>
        <div className="cc-sl-diagram-canvas">
          <Variant />
        </div>
      </div>
    </Band>
  );
};

// Title lookup for the architecture band, exposed for variant consumers
// that compose their own band+chrome around the diagram body.
export const DIAGRAM_TITLES = TITLES;

// Renders just the SVG body for a given diagram kind. The default
// `ConceptDiagram` wraps this in an inverted band with the section label
// and heading; the cinematic variant uses this entry point directly so it
// can layer anchors and connector lines on top of the same SVG without
// duplicating geometry.
export const ConceptDiagramBody: FC<ConceptDiagramProps> = ({ kind }) => {
  const Variant = VARIANTS[kind];
  return <Variant />;
};
