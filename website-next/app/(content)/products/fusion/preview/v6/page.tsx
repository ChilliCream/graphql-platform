import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";
import { NitroFusion } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Fusion: Distributed GraphQL Gateway for .NET",
  description:
    "Fusion is ChilliCream's distributed GraphQL gateway .NET. Compose subgraphs at planning time, prove answerable, serve one composite schema from a self-run endpoint.",
  keywords: [
    "Fusion",
    "distributed GraphQL gateway",
    "distributed GraphQL gateway .NET",
    "GraphQL federation",
    "composite schema",
    "GraphQL Composite Schemas",
    "Apollo Federation",
    "Hot Chocolate",
    "query plan",
    "satisfiability",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Fusion: Distributed GraphQL Gateway for .NET",
    description:
      "Compose subgraphs at planning time, not runtime. Apollo Federation spec compatible. .NET-native, self-run gateway built on Hot Chocolate.",
    type: "website",
  },
};

// Brand spectrum, allowed at most once per screen. Used on the closing CTA rule.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// -----------------------------------------------------------------------------
// Small primitives
// -----------------------------------------------------------------------------

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

interface IndexTagProps {
  readonly value: string;
  readonly icon?: ReactNode;
}

function IndexTag({ value, icon }: IndexTagProps) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim inline-flex h-6 items-center justify-center gap-1.5 rounded-full border px-2 font-mono text-[11px] tabular-nums">
      {icon ? (
        <span className="text-cc-accent inline-flex" aria-hidden>
          {icon}
        </span>
      ) : null}
      {value}
    </span>
  );
}

// -----------------------------------------------------------------------------
// Hero "Today's Pour" menu card. Same three subgraph rows + gateway pour node
// as v1, restyled as a small cafe menu card. Palette stays cc-* dark teal.
// -----------------------------------------------------------------------------

function HeroPourMenuCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-6 shadow-2xl sm:p-8">
      {/* Soft accent glow anchored over the pour node, the lone color event. */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-70"
        style={{
          background:
            "radial-gradient(420px 220px at 78% 50%, rgba(94, 234, 212, 0.18), transparent 70%), radial-gradient(280px 180px at 82% 56%, rgba(22, 185, 228, 0.14), transparent 70%)",
        }}
      />

      <div className="relative">
        <div className="mb-5 flex items-center justify-between gap-3">
          <div className="flex items-center gap-2.5">
            <span
              className="text-cc-accent inline-flex h-5 w-5 items-center justify-center"
              aria-hidden
            >
              <PourOver className="h-5 w-5" />
            </span>
            <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
              today&apos;s pour
            </span>
          </div>
          <span className="border-cc-accent/40 text-cc-accent bg-cc-accent/10 inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[10.5px] tracking-wider uppercase">
            <span className="text-cc-accent" aria-hidden>
              <CheckIcon size={11} />
            </span>
            satisfiable
          </span>
        </div>

        <svg
          viewBox="0 0 520 320"
          className="h-auto w-full"
          role="img"
          aria-label="Three subgraph schemas composed into one gateway endpoint with a satisfiability check"
        >
          <defs>
            <linearGradient
              id="fusion-v6-hero-flow"
              x1="0"
              x2="1"
              y1="0"
              y2="0"
            >
              <stop offset="0%" stopColor="#5eead4" stopOpacity="0.15" />
              <stop offset="100%" stopColor="#5eead4" stopOpacity="0.9" />
            </linearGradient>
            <radialGradient id="fusion-v6-hero-node" cx="0.5" cy="0.5" r="0.6">
              <stop offset="0%" stopColor="#5eead4" stopOpacity="0.25" />
              <stop offset="100%" stopColor="#5eead4" stopOpacity="0" />
            </radialGradient>
          </defs>

          {/* Three subgraph rows on the left, framed as menu items with owner stamps. */}
          {[
            { y: 28, label: "catalog", sub: "Hot Chocolate" },
            { y: 130, label: "checkout", sub: "Federation v2" },
            { y: 232, label: "reviews", sub: "Hot Chocolate" },
          ].map((s) => (
            <g key={s.label}>
              <rect
                x="12"
                y={s.y}
                width="172"
                height="60"
                rx="8"
                fill="rgba(245,241,234,0.04)"
                stroke="rgba(245,241,234,0.16)"
              />
              <text
                x="28"
                y={s.y + 26}
                fontFamily="var(--font-body)"
                fontSize="13"
                fill="#f5f0ea"
              >
                {s.label}
              </text>
              <text
                x="28"
                y={s.y + 44}
                fontFamily="ui-monospace, monospace"
                fontSize="10.5"
                fill="rgba(245,241,234,0.62)"
              >
                {s.sub}
              </text>
              <text
                x="172"
                y={s.y + 26}
                textAnchor="end"
                fontFamily="ui-monospace, monospace"
                fontSize="10"
                fill="rgba(245,241,234,0.45)"
                dx="-8"
              >
                subgraph
              </text>
              <path
                d={`M 184 ${s.y + 30} C 260 ${s.y + 30}, 280 160, 348 160`}
                stroke="url(#fusion-v6-hero-flow)"
                strokeWidth="1.5"
                fill="none"
              />
            </g>
          ))}

          {/* Signature pour node, the Fusion gateway. */}
          <circle cx="402" cy="160" r="74" fill="url(#fusion-v6-hero-node)" />
          <rect
            x="348"
            y="128"
            width="148"
            height="64"
            rx="10"
            fill="rgba(12,19,34,0.85)"
            stroke="rgba(94,234,212,0.6)"
          />
          <text
            x="422"
            y="152"
            textAnchor="middle"
            fontFamily="var(--font-body)"
            fontSize="13"
            fill="#f5f0ea"
          >
            Fusion gateway
          </text>
          <text
            x="422"
            y="170"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="#5eead4"
          >
            /graphql
          </text>
          <text
            x="422"
            y="184"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9.5"
            fill="rgba(245,241,234,0.5)"
          >
            one composite schema
          </text>

          {/* Plan artifact tag below the gateway. */}
          <rect
            x="364"
            y="244"
            width="116"
            height="22"
            rx="11"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="422"
            y="259"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            gateway.far (built)
          </text>
        </svg>

        <div className="border-cc-card-border text-cc-ink-dim mt-5 flex items-center justify-between border-t pt-4 font-mono text-[11px]">
          <span>3 subgraphs, 0 conflicts, 0 unreachable paths</span>
          <span className="text-cc-accent">planned at build time</span>
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Feature row, alternating sides, with an optional drink icon next to the index.
// -----------------------------------------------------------------------------

interface FeatureRowProps {
  readonly id: string;
  readonly index: string;
  readonly indexIcon?: ReactNode;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
  readonly visual: ReactNode;
  readonly reverse?: boolean;
}

function FeatureRow({
  id,
  index,
  indexIcon,
  eyebrow,
  title,
  body,
  bullets,
  visual,
  reverse = false,
}: FeatureRowProps) {
  return (
    <section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <div
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex items-center gap-3">
            <IndexTag value={index} icon={indexIcon} />
            <Eyebrow>{eyebrow}</Eyebrow>
          </div>
          <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            {title}
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            {body}
          </p>
          <ul className="mt-6 flex flex-col gap-2.5">
            {bullets.map((b) => (
              <li
                key={b}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon size={14} />
                </span>
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </div>
        <div
          className={[
            "lg:col-span-7",
            reverse ? "lg:order-1" : "lg:order-2",
          ].join(" ")}
        >
          <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 sm:p-6">
            {visual}
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Inline diagrams. Same straight technical drawings as v1, cc-* tokens only.
// -----------------------------------------------------------------------------

/** Composition pipeline: source SDLs into the phase chain, out as a .far. */
function CompositionPipelineDiagram() {
  const phases = ["parse", "enrich", "validate", "merge", "satisfiability"];
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Composition phases run in CI and emit a versioned Fusion archive"
    >
      <text
        x="12"
        y="18"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        nitro fusion compose
      </text>

      {[
        { y: 36, label: "catalog.graphql" },
        { y: 76, label: "checkout.graphql" },
        { y: 116, label: "reviews.graphql" },
      ].map((s) => (
        <g key={s.label}>
          <rect
            x="12"
            y={s.y}
            width="120"
            height="26"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="22"
            y={s.y + 17}
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.62)"
          >
            {s.label}
          </text>
          <path
            d={`M 132 ${s.y + 13} C 160 ${s.y + 13}, 160 80, 188 80`}
            stroke="rgba(94,234,212,0.35)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}

      {phases.map((p, i) => (
        <g key={p}>
          <rect
            x={188 + i * 52}
            y="68"
            width="44"
            height="24"
            rx="4"
            fill={
              i === phases.length - 1
                ? "rgba(94,234,212,0.16)"
                : "rgba(245,241,234,0.04)"
            }
            stroke={
              i === phases.length - 1
                ? "rgba(94,234,212,0.55)"
                : "rgba(245,241,234,0.16)"
            }
          />
          <text
            x={188 + i * 52 + 22}
            y="83"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9"
            fill={
              i === phases.length - 1 ? "#5eead4" : "rgba(245,241,234,0.62)"
            }
          >
            {p}
          </text>
          {i < phases.length - 1 && (
            <path
              d={`M ${188 + i * 52 + 44} 80 L ${188 + (i + 1) * 52} 80`}
              stroke="rgba(245,241,234,0.25)"
              strokeWidth="1"
              fill="none"
            />
          )}
        </g>
      ))}

      <path
        d="M 410 92 L 410 140"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1.2"
        fill="none"
      />
      <polygon points="406,140 410,150 414,140" fill="rgba(94,234,212,0.7)" />
      <rect
        x="332"
        y="152"
        width="156"
        height="40"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="410"
        y="170"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        gateway.far
      </text>
      <text
        x="410"
        y="184"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.62)"
      >
        versioned, inspectable
      </text>

      <text
        x="12"
        y="170"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        fails like a compile error if a phase
      </text>
      <text
        x="12"
        y="184"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        emits a diagnostic, before deploy
      </text>
    </svg>
  );
}

/** Satisfiability proof: reachable paths checked across the composed graph. */
function SatisfiabilityDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Every reachable field has a resolver path across the composed graph"
    >
      <text
        x="12"
        y="18"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        reachability walk over Query.*
      </text>

      <rect
        x="20"
        y="92"
        width="68"
        height="32"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="54"
        y="112"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        Query
      </text>

      {[
        { y: 40, label: "order(id)", owner: "checkout" },
        { y: 96, label: "order.items", owner: "catalog" },
        { y: 152, label: "order.shipping", owner: "checkout" },
      ].map((n) => (
        <g key={n.label}>
          <path
            d={`M 88 108 C 130 108, 130 ${n.y + 14}, 172 ${n.y + 14}`}
            stroke="rgba(94,234,212,0.4)"
            strokeWidth="1.2"
            fill="none"
          />
          <rect
            x="172"
            y={n.y}
            width="156"
            height="28"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(94,234,212,0.45)"
          />
          <text
            x="184"
            y={n.y + 18}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#f5f0ea"
          >
            {n.label}
          </text>
          <text
            x="320"
            y={n.y + 18}
            textAnchor="end"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.55)"
          >
            {n.owner}
          </text>
          <g transform={`translate(336 ${n.y + 6})`}>
            <rect
              width="120"
              height="16"
              rx="8"
              fill="rgba(94,234,212,0.12)"
              stroke="rgba(94,234,212,0.55)"
            />
            <text
              x="60"
              y="12"
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="9"
              fill="#5eead4"
            >
              path resolvable
            </text>
          </g>
        </g>
      ))}

      <text
        x="12"
        y="204"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        unresolvable shapes fail composition with UNSATISFIABLE_QUERY_PATH
      </text>
    </svg>
  );
}

/** Apollo Federation interop: existing Apollo subgraphs flow through Fusion. */
function FederationInteropDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Apollo Federation v2 subgraphs are valid Fusion subgraphs"
    >
      {[
        { y: 30, label: "Apollo Federation v2", sub: "@key, @requires" },
        { y: 96, label: "Hot Chocolate", sub: "@lookup, plain Query" },
        {
          y: 162,
          label: "Hot Chocolate (entities)",
          sub: "@lookup, @key",
        },
      ].map((row) => (
        <g key={row.label}>
          <rect
            x="12"
            y={row.y}
            width="200"
            height="40"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="24"
            y={row.y + 18}
            fontFamily="var(--font-body)"
            fontSize="12"
            fill="#f5f0ea"
          >
            {row.label}
          </text>
          <text
            x="24"
            y={row.y + 33}
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.55)"
          >
            {row.sub}
          </text>
          <path
            d={`M 212 ${row.y + 20} C 260 ${row.y + 20}, 280 110, 320 110`}
            stroke="rgba(94,234,212,0.4)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="320"
        y="86"
        width="148"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="394"
        y="108"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#5eead4"
      >
        Fusion gateway
      </text>
      <text
        x="394"
        y="124"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.62)"
      >
        GraphQL Composite Schemas spec
      </text>
    </svg>
  );
}

/** Distributed query plan: parallel + batched fetches across subgraphs. */
function QueryPlanDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Distributed query plan with parallel and batched subgraph fetches"
    >
      <text
        x="12"
        y="18"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        query plan
      </text>
      <rect
        x="12"
        y="32"
        width="100"
        height="28"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="62"
        y="51"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        client op
      </text>

      {[
        { y: 76, label: "fetch catalog", ms: "12ms" },
        { y: 110, label: "fetch checkout", ms: "10ms" },
      ].map((n) => (
        <g key={n.label}>
          <path
            d={`M 112 46 C 150 46, 150 ${n.y + 12}, 180 ${n.y + 12}`}
            stroke="rgba(94,234,212,0.45)"
            strokeWidth="1.2"
            fill="none"
          />
          <rect
            x="180"
            y={n.y}
            width="160"
            height="24"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="192"
            y={n.y + 16}
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.62)"
          >
            {n.label}
          </text>
          <text
            x="332"
            y={n.y + 16}
            textAnchor="end"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(94,234,212,0.85)"
          >
            {n.ms}
          </text>
        </g>
      ))}

      <path
        d="M 340 88 C 372 88, 372 156, 180 156"
        stroke="rgba(94,234,212,0.45)"
        strokeWidth="1.2"
        fill="none"
        strokeDasharray="3 3"
      />
      <rect
        x="180"
        y="144"
        width="220"
        height="36"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(94,234,212,0.45)"
      />
      <text
        x="192"
        y="162"
        fontFamily="ui-monospace, monospace"
        fontSize="10.5"
        fill="#f5f0ea"
      >
        batch reviews(productIds: [...])
      </text>
      <text
        x="192"
        y="174"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.55)"
      >
        one HTTP/2 call, no N+1 across the graph
      </text>

      <text
        x="12"
        y="200"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        parallel where independent, sequenced where required
      </text>
    </svg>
  );
}

/** .NET-native gateway: ASP.NET Core middleware pipeline owns the gateway. */
function DotNetGatewayDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Fusion gateway is an ASP.NET Core app with DI, auth, and middleware"
    >
      <text
        x="12"
        y="18"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        Program.cs
      </text>

      {[
        { x: 12, label: "AuthN" },
        { x: 96, label: "Headers" },
        { x: 180, label: "Fusion" },
        { x: 264, label: "Cache" },
        { x: 348, label: "Telemetry" },
      ].map((m) => (
        <g key={m.label}>
          <rect
            x={m.x}
            y="60"
            width="76"
            height="28"
            rx="4"
            fill={
              m.label === "Fusion"
                ? "rgba(94,234,212,0.16)"
                : "rgba(245,241,234,0.04)"
            }
            stroke={
              m.label === "Fusion"
                ? "rgba(94,234,212,0.6)"
                : "rgba(245,241,234,0.18)"
            }
          />
          <text
            x={m.x + 38}
            y="78"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill={m.label === "Fusion" ? "#5eead4" : "rgba(245,241,234,0.7)"}
          >
            {m.label}
          </text>
        </g>
      ))}
      <path
        d="M 12 100 L 424 100"
        stroke="rgba(245,241,234,0.18)"
        strokeWidth="1"
        fill="none"
      />
      <text
        x="12"
        y="116"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        ASP.NET Core middleware pipeline
      </text>

      <text
        x="12"
        y="150"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        no separate Rust binary, no YAML config, no Node runtime
      </text>
      <rect
        x="12"
        y="166"
        width="412"
        height="48"
        rx="6"
        fill="rgba(94,234,212,0.06)"
        stroke="rgba(94,234,212,0.4)"
      />
      <text
        x="24"
        y="184"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        builder.Services.AddGraphQLGateway()
      </text>
      <text
        x="24"
        y="202"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        .AddFileSystemConfiguration(&quot;./gateway.far&quot;);
      </text>
    </svg>
  );
}

/** Self-run gateway: every request and every byte stay in your network. */
function SelfRunDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="The gateway runs in your own infrastructure, never a hosted hop"
    >
      <rect
        x="12"
        y="22"
        width="456"
        height="176"
        rx="12"
        fill="rgba(245,241,234,0.03)"
        stroke="rgba(245,241,234,0.22)"
        strokeDasharray="4 4"
      />
      <text
        x="28"
        y="42"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        your network
      </text>

      <rect
        x="32"
        y="92"
        width="76"
        height="32"
        rx="6"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(245,241,234,0.18)"
      />
      <text
        x="70"
        y="112"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#f5f0ea"
      >
        client
      </text>
      <path
        d="M 108 108 L 168 108"
        stroke="rgba(94,234,212,0.5)"
        strokeWidth="1.2"
        fill="none"
      />
      <polygon points="168,104 180,108 168,112" fill="rgba(94,234,212,0.7)" />

      <rect
        x="180"
        y="80"
        width="140"
        height="56"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="250"
        y="104"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#5eead4"
      >
        Fusion gateway
      </text>
      <text
        x="250"
        y="120"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        ASP.NET Core
      </text>

      {[
        { y: 56, label: "catalog" },
        { y: 100, label: "checkout" },
        { y: 144, label: "reviews" },
      ].map((s) => (
        <g key={s.label}>
          <path
            d={`M 320 108 C 350 108, 350 ${s.y + 14}, 388 ${s.y + 14}`}
            stroke="rgba(94,234,212,0.4)"
            strokeWidth="1.2"
            fill="none"
          />
          <rect
            x="388"
            y={s.y}
            width="64"
            height="28"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="420"
            y={s.y + 18}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.62)"
          >
            {s.label}
          </text>
        </g>
      ))}

      <text
        x="28"
        y="186"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        no hosted hop, no third party in the request path
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Proof item used in the closing open-source band.
// -----------------------------------------------------------------------------

interface ProofItemProps {
  readonly label: string;
  readonly value: string;
}

function ProofItem({ label, value }: ProofItemProps) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-cc-heading font-heading text-2xl font-semibold tracking-tight">
        {value}
      </span>
      <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
        {label}
      </span>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Console card used for the composition shell snippet and the Program.cs file.
// -----------------------------------------------------------------------------

interface ConsoleProps {
  readonly file: string;
  readonly tag: string;
  readonly children: ReactNode;
}

function ConsoleCard({ file, tag, children }: ConsoleProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
        <span
          className="bg-cc-status-firing h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-investigating h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-healthy h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span className="text-cc-ink-dim ml-3 font-mono text-[11px]">
          {file}
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {tag}
        </span>
      </div>
      <pre className="text-cc-ink overflow-x-auto px-5 py-4 font-mono text-[12.5px] leading-6">
        {children}
      </pre>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function FusionPreviewV6() {
  return (
    <>
      {/* HERO: barista eyebrow with the primary keyword, technical headline,
          dual CTA, and the "Today's Pour" menu card on the right. */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-6">
            <Eyebrow>On the menu, distributed GraphQL gateway .NET</Eyebrow>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              Compose your graph at planning time, not runtime.
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
              Fusion is ChilliCream&apos;s distributed GraphQL gateway. Point it
              at independent subgraphs, blend them into one composite schema in
              CI, and ship a versioned plan that is proven answerable before a
              client ever sends a query. Built on Hot Chocolate, run as your own
              ASP.NET Core app, the head barista of your graph.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/fusion">Get Started</SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
            <dl className="border-cc-card-border mt-10 grid grid-cols-3 gap-6 border-t pt-6">
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  License
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">MIT</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Runtime
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">ASP.NET Core</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Spec
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">Composite Schemas</dd>
              </div>
            </dl>
          </div>
          <div className="lg:col-span-6">
            <HeroPourMenuCard />
          </div>
        </div>
      </section>

      {/* Capability strip: PourOver leads the line, six promises pulled. */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:gap-6">
          <div className="flex items-center gap-3">
            <span
              className="text-cc-accent inline-flex h-5 w-5 items-center justify-center"
              aria-hidden
            >
              <PourOver className="h-5 w-5" />
            </span>
            <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest whitespace-nowrap uppercase">
              Today&apos;s pour, six promises pulled
            </span>
          </div>
          <ul className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:flex-1 lg:grid-cols-6">
            {[
              "Build-time composition",
              "Satisfiability proof",
              "Federation v2 interop",
              "Distributed query plan",
              ".NET-native gateway",
              "Query plan tracing",
            ].map((label) => (
              <li
                key={label}
                className="text-cc-ink flex items-center gap-2 font-mono text-[11.5px] tracking-tight uppercase"
              >
                <span className="text-cc-accent" aria-hidden>
                  <CheckIcon size={12} />
                </span>
                {label}
              </li>
            ))}
          </ul>
        </div>
      </section>

      {/* 01 Composition, behind the bar. */}
      <FeatureRow
        id="composition"
        index="01"
        indexIcon={<DripBrewer className="h-3.5 w-3.5" />}
        eyebrow="Behind the bar"
        title="Composition runs in CI, not on a hot path."
        body="A composition pipeline reads each subgraph SDL, validates it against the others, and emits a Fusion archive your gateway loads at startup. Think of it as roast, grind, dose, tamp, taste, the diagram below is the literal phase chain. Type, enum, and field conflicts surface as diagnostics with stable codes, on the build server, before deploy. The gateway never sees raw source schemas."
        bullets={[
          "Runs fully offline as a build step or in CI. Nitro cloud is optional, not in the request path.",
          "Halts on the first failing phase with a stable diagnostic code you can match in scripts.",
          "Emits a versioned, inspectable .far artifact you can diff between releases.",
        ]}
        visual={
          <div className="flex flex-col gap-4">
            <CompositionPipelineDiagram />
            <ConsoleCard file="ci/compose.sh" tag="shell">
              <span style={{ color: "#8b949e" }}>
                {"# Compose subgraphs and fail the build on any conflict.\n"}
              </span>
              <span style={{ color: "#ff7b72" }}>nitro</span>
              <span style={{ color: "#c9d1d9" }}> fusion compose \\</span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>
                {"  --subgraph catalog=./catalog.graphql \\"}
              </span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>
                {"  --subgraph checkout=./checkout.graphql \\"}
              </span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>
                {"  --subgraph reviews=./reviews.graphql \\"}
              </span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>
                {"  --output ./gateway.far"}
              </span>
              {"\n\n"}
              <span style={{ color: "#5eead4" }}>{"OK"}</span>
              <span style={{ color: "#c9d1d9" }}>
                {" composed 3 subgraphs, 0 errors, "}
              </span>
              <span style={{ color: "#a5d6ff" }}>gateway.far</span>
              <span style={{ color: "#c9d1d9" }}>{" written"}</span>
            </ConsoleCard>
          </div>
        }
      />

      {/* 02 Satisfiability, quality control on the bar. */}
      <FeatureRow
        id="satisfiability"
        index="02"
        indexIcon={<FrenchPress className="h-3.5 w-3.5" />}
        eyebrow="Quality control"
        title="If it composes, it answers."
        body="Every order on the menu has a recipe behind the bar. Composition's final phase walks every reachable field from the root types and proves it can be resolved across your subgraphs given the available lookups and keys. A query that successfully validates against the gateway is one your services can actually answer. Unreachable shapes fail composition with UNSATISFIABLE_QUERY_PATH."
        bullets={[
          "Reachability analysis over the full composed graph, shipped in Fusion.Composition.Satisfiability.",
          "Catches contract drift between subgraphs before a client ever sends the query.",
          "Failures cite the exact field path, so the broken shape is the next thing you fix.",
        ]}
        visual={<SatisfiabilityDiagram />}
        reverse
      />

      {/* 03 Federation interop, house blend with open origin. */}
      <FeatureRow
        id="federation"
        index="03"
        indexIcon={<Espresso className="h-3.5 w-3.5" />}
        eyebrow="House blend, open origin"
        title="Apollo Federation spec compatible, on an open standard."
        body="Single-origin beans from any roaster blend in the same hopper. Fusion implements the GraphQL Composite Schemas specification under the GraphQL Foundation, and reads Apollo Federation v2 subgraphs through a dedicated connector. Bring existing @key, @requires, and @provides directives into a Fusion composition without rewriting resolvers, on a vendor-neutral spec."
        bullets={[
          "GraphQL Composite Schemas spec, vendor-neutral. Subgraph schemas stay portable.",
          "Apollo Federation v2 interop via Fusion.Connectors.ApolloFederation.",
          "Documented migration path from Federation v2 with directive-by-directive mapping.",
        ]}
        visual={<FederationInteropDiagram />}
      />

      {/* 04 Distributed query plan, the pass. */}
      <FeatureRow
        id="plan"
        index="04"
        indexIcon={<CoffeeTray className="h-3.5 w-3.5" />}
        eyebrow="The pass"
        title="One client request, a planned distributed fetch."
        body="One ticket at the pass, the bar pulls every drink in parallel, the runner carries one tray back. The gateway compiles each incoming operation into a query plan over your subgraphs. Independent fetches run in parallel, dependent fetches sequence behind them, and shared entity keys are batched into single HTTP/2 calls. The result is one response, assembled from the minimum work your fleet needs to do."
        bullets={[
          "Parallel fan-out where fetches are independent, sequencing only where a fetch needs prior data.",
          "Entity keys collected across the plan and batched, no N+1 across services.",
          "Persisted operations and conservative cache control merged from subgraph policies.",
        ]}
        visual={<QueryPlanDiagram />}
        reverse
      />

      {/* 05 .NET-native gateway, your bar, your machine. */}
      <FeatureRow
        id="dotnet"
        index="05"
        indexIcon={<DripBrewer className="h-3.5 w-3.5" />}
        eyebrow="Your bar, your machine"
        title="The gateway is your code, on Hot Chocolate."
        body="You own the espresso machine, the grinder, the water line, no rented counter. Fusion's gateway is an ASP.NET Core app, configured with AddGraphQLGateway() and built on Hot Chocolate. Your DI container, your authentication, your middleware, your logging. No standalone binary, no YAML, no Node runtime in the request path. The same Hot Chocolate server you already ship can be a Fusion subgraph with no resolver changes."
        bullets={[
          "AddGraphQLGateway() integrates with the ASP.NET Core middleware pipeline you already operate.",
          "Auth, header propagation, and cache control land where you expect them in .NET.",
          "An existing Hot Chocolate server is already a valid subgraph, no federation library needed.",
        ]}
        visual={
          <div className="flex flex-col gap-4">
            <DotNetGatewayDiagram />
            <ConsoleCard file="Program.cs" tag="C#">
              <span style={{ color: "#c9d1d9" }}>{"var builder = "}</span>
              <span style={{ color: "#ffa657" }}>WebApplication</span>
              <span style={{ color: "#c9d1d9" }}>{"."}</span>
              <span style={{ color: "#d2a8ff" }}>CreateBuilder</span>
              <span style={{ color: "#c9d1d9" }}>{"(args);"}</span>
              {"\n\n"}
              <span style={{ color: "#c9d1d9" }}>{"builder.Services"}</span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>{"    ."}</span>
              <span style={{ color: "#d2a8ff" }}>AddGraphQLGateway</span>
              <span style={{ color: "#c9d1d9" }}>{"()"}</span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>{"    ."}</span>
              <span style={{ color: "#d2a8ff" }}>
                AddFileSystemConfiguration
              </span>
              <span style={{ color: "#c9d1d9" }}>{"("}</span>
              <span style={{ color: "#a5d6ff" }}>{'"./gateway.far"'}</span>
              <span style={{ color: "#c9d1d9" }}>{");"}</span>
              {"\n\n"}
              <span style={{ color: "#c9d1d9" }}>{"var app = builder."}</span>
              <span style={{ color: "#d2a8ff" }}>Build</span>
              <span style={{ color: "#c9d1d9" }}>{"();"}</span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>{"app."}</span>
              <span style={{ color: "#d2a8ff" }}>MapGraphQL</span>
              <span style={{ color: "#c9d1d9" }}>{"();"}</span>
              {"\n"}
              <span style={{ color: "#c9d1d9" }}>{"app."}</span>
              <span style={{ color: "#d2a8ff" }}>Run</span>
              <span style={{ color: "#c9d1d9" }}>{"();"}</span>
            </ConsoleCard>
          </div>
        }
      />

      {/* 06 Self-run, nothing leaves the counter. */}
      <FeatureRow
        id="self-run"
        index="06"
        indexIcon={<PourOver className="h-3.5 w-3.5" />}
        eyebrow="Nothing leaves the counter"
        title="The gateway is always self-run, never a hosted hop."
        body="Every cup is poured and served at your bar. Nitro cloud is the supplier of the recipe card, never a hop in the request path. Fusion runs in your infrastructure, period. Every client request and every subgraph fetch stay inside your network boundary. You choose the cluster, the auth, the egress, the audit trail."
        bullets={[
          "Runs in any environment that runs ASP.NET Core, on your own compute.",
          "No third-party gateway in the request path, no data egress you did not approve.",
          "Standard ASP.NET Core auth (JWT, cookie, OIDC, mTLS) and header propagation to subgraphs.",
        ]}
        visual={<SelfRunDiagram />}
        reverse
      />

      {/* 07 Nitro trace, tasting notes. */}
      <section className="border-cc-card-border border-t py-20 sm:py-24">
        <div className="mb-10 grid items-end gap-6 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <div className="flex items-center gap-3">
              <IndexTag
                value="07"
                icon={<Espresso className="h-3.5 w-3.5" />}
              />
              <Eyebrow>Tasting notes</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              The query plan, traced into Nitro.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Read each request like a barista&apos;s tasting log. Fusion emits
              OpenTelemetry spans for each request, the planning step, and every
              subgraph fetch. Nitro renders the plan as a navigable trace, so
              when a single subgraph slows down you see which step in the plan,
              which subgraph, and which keys were in the batch.
            </p>
          </div>
          <div className="lg:col-span-5 lg:text-right">
            <p className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
              spans: ExecuteRequest, PlanOperation, ExecutePlanNode
            </p>
          </div>
        </div>
        <div className="border-cc-card-border bg-cc-surface mx-auto max-w-5xl overflow-hidden rounded-xl border">
          <NitroFusion />
        </div>
      </section>

      {/* MIT / open source proof band. */}
      <section
        aria-label="Open source"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <Eyebrow>MIT licensed</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Open source, on an open standard.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Fusion is part of the ChilliCream GraphQL Platform, developed in
              the open under the MIT license and built on the GraphQL Composite
              Schemas specification under the GraphQL Foundation. The codebase,
              the issue tracker, the roadmap, and the release notes all live on
              GitHub.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </SolidButton>
              <OutlineButton href="/docs/fusion">Read the docs</OutlineButton>
            </div>
          </div>
          <div className="lg:col-span-5">
            <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Runtime" value="ASP.NET Core" />
              <ProofItem label="Spec" value="Composite Schemas" />
              <ProofItem label="Built on" value="Hot Chocolate" />
              <ProofItem label="Interop" value="Federation v2" />
              <ProofItem label="Tracing" value="OpenTelemetry" />
            </div>
          </div>
        </div>
      </section>

      {/* Closing CTA with the single brand-spectrum hairline. No coffee in the
          closer, the page lands on the technical promise. */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Get started, pull the first shot</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            One composite graph, tasted before you serve it.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            Point Fusion at your subgraphs, compose in CI, and serve from a
            single .NET endpoint you operate yourself. The plan is built, the
            satisfiability is proven, and the runtime is the ASP.NET Core you
            already run.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/fusion">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
