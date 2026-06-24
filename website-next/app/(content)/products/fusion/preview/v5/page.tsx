import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroFusion } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Fusion: Distributed GraphQL Gateway for .NET",
  description:
    "Reference page for Fusion, the distributed GraphQL gateway .NET teams self-run. Composition at planning time, satisfiability proof, Apollo Federation interop.",
  robots: { index: false, follow: false },
  openGraph: {
    title: "Fusion: Distributed GraphQL Gateway for .NET",
    description:
      "Compose subgraphs into one validated graph at planning time. Apollo Federation spec compatible. .NET-native, self-run gateway built on Hot Chocolate.",
    type: "website",
  },
};

// Brand spectrum, allowed at most once per screen. Used on the closing CTA rule.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// Single accent for this stance. Cyan from the brand family.
const ACCENT = "#16b9e4";

// -----------------------------------------------------------------------------
// Chapter index, shared between the sticky TOC and each chapter heading.
// -----------------------------------------------------------------------------

interface Chapter {
  readonly num: string;
  readonly id: string;
  readonly label: string;
}

const CHAPTERS: readonly Chapter[] = [
  { num: "01", id: "composition", label: "Composition" },
  { num: "02", id: "satisfiability", label: "Satisfiability proof" },
  { num: "03", id: "federation", label: "Federation interop" },
  { num: "04", id: "plan", label: "Distributed query plan" },
  { num: "05", id: "dotnet", label: ".NET-native gateway" },
  { num: "06", id: "self-run", label: "Self-run, always" },
  { num: "07", id: "colophon", label: "Colophon" },
];

// -----------------------------------------------------------------------------
// Sidebar table of contents. Sticky on lg, horizontal breadcrumb below lg.
// -----------------------------------------------------------------------------

function SidebarToc() {
  return (
    <aside className="lg:sticky lg:top-24 lg:self-start">
      {/* lg+: vertical sticky TOC. */}
      <nav aria-label="Document outline" className="hidden lg:block">
        <p
          className="text-caption font-mono tracking-widest uppercase"
          style={{ color: ACCENT }}
        >
          Contents
        </p>
        <ol className="border-cc-card-border mt-4 flex flex-col gap-0 border-l">
          {CHAPTERS.map((c) => (
            <li key={c.id} className="relative">
              <a
                href={`#${c.id}`}
                className="text-cc-ink hover:text-cc-heading group flex items-baseline gap-3 py-2 pl-4 font-mono text-[12px] tracking-widest uppercase transition-colors"
              >
                <span className="text-cc-ink-dim group-hover:text-cc-ink tabular-nums">
                  {c.num}
                </span>
                <span>{c.label}</span>
              </a>
            </li>
          ))}
        </ol>

        {/* Metadata block below the TOC, key:value mono pairs. */}
        <dl className="border-cc-card-border mt-10 flex flex-col gap-3 border-t pt-6">
          {[
            { k: "License", v: "MIT" },
            { k: "Runtime", v: "ASP.NET Core" },
            { k: "Spec", v: "Composite Schemas" },
            { k: "Built on", v: "Hot Chocolate" },
          ].map((m) => (
            <div
              key={m.k}
              className="flex items-baseline justify-between gap-3"
            >
              <dt className="text-cc-nav-label font-mono text-[10.5px] tracking-widest uppercase">
                {m.k}
              </dt>
              <dd className="text-cc-ink font-mono text-[11px]">{m.v}</dd>
            </div>
          ))}
        </dl>
      </nav>

      {/* Below lg: horizontal mono breadcrumb strip. */}
      <nav
        aria-label="Document outline"
        className="border-cc-card-border -mx-4 overflow-x-auto border-y px-4 py-3 sm:mx-0 sm:px-0 lg:hidden"
      >
        <ol className="flex items-center gap-5 whitespace-nowrap">
          {CHAPTERS.map((c) => (
            <li key={c.id} className="flex items-center gap-2">
              <a
                href={`#${c.id}`}
                className="text-cc-ink hover:text-cc-heading font-mono text-[11px] tracking-widest uppercase transition-colors"
              >
                <span className="tabular-nums" style={{ color: ACCENT }}>
                  {c.num}
                </span>
                <span className="ml-2">{c.label}</span>
              </a>
            </li>
          ))}
        </ol>
      </nav>
    </aside>
  );
}

// -----------------------------------------------------------------------------
// Chapter heading. Hairline rule, oversized cyan mono numeral next to title.
// -----------------------------------------------------------------------------

interface ChapterHeadingProps {
  readonly num: string;
  readonly id: string;
  readonly eyebrow: string;
  readonly title: string;
}

function ChapterHeading({ num, id, eyebrow, title }: ChapterHeadingProps) {
  return (
    <header id={id} className="scroll-mt-24">
      <hr className="border-cc-card-border" />
      <div className="mt-8 flex items-baseline gap-6">
        <span
          className="font-mono font-semibold tabular-nums"
          style={{
            color: ACCENT,
            fontSize: "var(--text-h1)",
            lineHeight: "var(--text-h1--line-height)",
          }}
        >
          {num}
        </span>
        <div className="flex min-w-0 flex-col gap-2">
          <span
            className="text-caption font-mono tracking-widest uppercase"
            style={{ color: ACCENT }}
          >
            Chapter {num} / {eyebrow}
          </span>
          <h2 className="text-cc-heading font-heading text-h4 sm:text-h3 font-semibold tracking-tight text-balance">
            {title}
          </h2>
        </div>
      </div>
      <hr className="border-cc-card-border mt-6" />
    </header>
  );
}

// -----------------------------------------------------------------------------
// Sub-heading within a chapter.
// -----------------------------------------------------------------------------

interface SubHeadingProps {
  readonly children: ReactNode;
}

function SubHeading({ children }: SubHeadingProps) {
  return (
    <h3 className="text-cc-heading font-heading text-h5 mt-10 font-semibold tracking-tight">
      {children}
    </h3>
  );
}

// -----------------------------------------------------------------------------
// Hairline frame: replaces the v1 card. No shadow, no rounded card-bg fill.
// -----------------------------------------------------------------------------

interface HairlineFrameProps {
  readonly caption?: string;
  readonly children: ReactNode;
}

function HairlineFrame({ caption, children }: HairlineFrameProps) {
  return (
    <figure className="border-cc-card-border mt-8 border">
      <div className="p-5 sm:p-6">{children}</div>
      {caption ? (
        <figcaption className="border-cc-card-border text-cc-ink-dim border-t px-5 py-2.5 font-mono text-[11px] tracking-widest uppercase">
          {caption}
        </figcaption>
      ) : null}
    </figure>
  );
}

// -----------------------------------------------------------------------------
// Bullet list with the chosen accent on the check.
// -----------------------------------------------------------------------------

interface BulletsProps {
  readonly items: readonly string[];
}

function Bullets({ items }: BulletsProps) {
  return (
    <ul className="mt-6 flex flex-col gap-2.5">
      {items.map((b) => (
        <li
          key={b}
          className="text-cc-ink text-body flex items-start gap-3 leading-relaxed"
        >
          <span className="mt-1 shrink-0" style={{ color: ACCENT }}>
            <CheckIcon size={14} />
          </span>
          <span>{b}</span>
        </li>
      ))}
    </ul>
  );
}

// -----------------------------------------------------------------------------
// Console card (hairline only, no shadow).
// -----------------------------------------------------------------------------

interface ConsoleProps {
  readonly file: string;
  readonly tag: string;
  readonly children: ReactNode;
}

function ConsoleCard({ file, tag, children }: ConsoleProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border mt-6 overflow-hidden border">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
        <span className="text-cc-ink-dim font-mono text-[11px]">{file}</span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
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
// Inline SVG diagrams. Same content as v1, cyan-tinted strokes/fills.
// -----------------------------------------------------------------------------

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
            rx="0"
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
            stroke="rgba(22,185,228,0.35)"
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
            rx="0"
            fill={
              i === phases.length - 1
                ? "rgba(22,185,228,0.14)"
                : "rgba(245,241,234,0.04)"
            }
            stroke={
              i === phases.length - 1
                ? "rgba(22,185,228,0.6)"
                : "rgba(245,241,234,0.16)"
            }
          />
          <text
            x={188 + i * 52 + 22}
            y="83"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9"
            fill={i === phases.length - 1 ? ACCENT : "rgba(245,241,234,0.62)"}
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
        stroke="rgba(22,185,228,0.6)"
        strokeWidth="1.2"
        fill="none"
      />
      <polygon points="406,140 410,150 414,140" fill="rgba(22,185,228,0.75)" />
      <rect
        x="332"
        y="152"
        width="156"
        height="40"
        rx="0"
        fill="rgba(22,185,228,0.08)"
        stroke="rgba(22,185,228,0.6)"
      />
      <text
        x="410"
        y="170"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill={ACCENT}
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
        rx="0"
        fill="rgba(22,185,228,0.08)"
        stroke="rgba(22,185,228,0.6)"
      />
      <text
        x="54"
        y="112"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill={ACCENT}
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
            stroke="rgba(22,185,228,0.45)"
            strokeWidth="1.2"
            fill="none"
          />
          <rect
            x="172"
            y={n.y}
            width="156"
            height="28"
            rx="0"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(22,185,228,0.5)"
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
              rx="0"
              fill="rgba(22,185,228,0.12)"
              stroke="rgba(22,185,228,0.6)"
            />
            <text
              x="60"
              y="12"
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="9"
              fill={ACCENT}
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
            rx="0"
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
            stroke="rgba(22,185,228,0.45)"
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
        rx="0"
        fill="rgba(22,185,228,0.08)"
        stroke="rgba(22,185,228,0.6)"
      />
      <text
        x="394"
        y="108"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill={ACCENT}
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
        rx="0"
        fill="rgba(22,185,228,0.08)"
        stroke="rgba(22,185,228,0.6)"
      />
      <text
        x="62"
        y="51"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill={ACCENT}
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
            stroke="rgba(22,185,228,0.5)"
            strokeWidth="1.2"
            fill="none"
          />
          <rect
            x="180"
            y={n.y}
            width="160"
            height="24"
            rx="0"
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
            fill={ACCENT}
          >
            {n.ms}
          </text>
        </g>
      ))}

      <path
        d="M 340 88 C 372 88, 372 156, 180 156"
        stroke="rgba(22,185,228,0.5)"
        strokeWidth="1.2"
        fill="none"
        strokeDasharray="3 3"
      />
      <rect
        x="180"
        y="144"
        width="220"
        height="36"
        rx="0"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(22,185,228,0.5)"
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
            rx="0"
            fill={
              m.label === "Fusion"
                ? "rgba(22,185,228,0.16)"
                : "rgba(245,241,234,0.04)"
            }
            stroke={
              m.label === "Fusion"
                ? "rgba(22,185,228,0.65)"
                : "rgba(245,241,234,0.18)"
            }
          />
          <text
            x={m.x + 38}
            y="78"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill={m.label === "Fusion" ? ACCENT : "rgba(245,241,234,0.7)"}
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
        rx="0"
        fill="rgba(22,185,228,0.06)"
        stroke="rgba(22,185,228,0.45)"
      />
      <text
        x="24"
        y="184"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill={ACCENT}
      >
        builder.Services.AddGraphQLGateway()
      </text>
      <text
        x="24"
        y="202"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill={ACCENT}
      >
        .AddFileSystemConfiguration(&quot;./gateway.far&quot;);
      </text>
    </svg>
  );
}

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
        rx="0"
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
        rx="0"
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
        stroke="rgba(22,185,228,0.55)"
        strokeWidth="1.2"
        fill="none"
      />
      <polygon points="168,104 180,108 168,112" fill="rgba(22,185,228,0.75)" />

      <rect
        x="180"
        y="80"
        width="140"
        height="56"
        rx="0"
        fill="rgba(22,185,228,0.08)"
        stroke="rgba(22,185,228,0.6)"
      />
      <text
        x="250"
        y="104"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill={ACCENT}
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
            stroke="rgba(22,185,228,0.45)"
            strokeWidth="1.2"
            fill="none"
          />
          <rect
            x="388"
            y={s.y}
            width="64"
            height="28"
            rx="0"
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
// Page
// -----------------------------------------------------------------------------

export default function FusionPreviewV5() {
  return (
    <div className="py-12 sm:py-16">
      {/* Document header */}
      <header className="mb-12 sm:mb-16">
        <p
          className="text-caption font-mono tracking-widest uppercase"
          style={{ color: ACCENT }}
        >
          Distributed GraphQL gateway / .NET
        </p>
        <h1 className="text-cc-heading font-heading text-h2 lg:text-hero mt-6 font-semibold tracking-tight text-balance lg:leading-[1.05]">
          Compose your graph at planning time, not runtime.
        </h1>
        <p className="text-cc-prose text-lead sm:text-lead mt-6 max-w-3xl leading-[1.3]">
          Fusion is ChilliCream&apos;s distributed GraphQL gateway. Point it at
          independent subgraphs, compose them into one composite schema in CI,
          and ship a versioned plan that is proven answerable before a client
          ever sends a query. Built on Hot Chocolate, run as your own ASP.NET
          Core app.
        </p>
        <div className="mt-8 flex flex-wrap gap-3">
          <SolidButton href="/docs/fusion">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>
        {/* Mono metadata row separated by mono dividers. */}
        <div className="text-cc-ink-dim mt-10 flex flex-wrap items-center gap-x-3 gap-y-2 font-mono text-[11px] tracking-widest uppercase">
          <span>
            <span className="text-cc-nav-label">License </span>
            <span className="text-cc-ink">MIT</span>
          </span>
          <span className="text-cc-nav-label">/</span>
          <span>
            <span className="text-cc-nav-label">Runtime </span>
            <span className="text-cc-ink">ASP.NET Core</span>
          </span>
          <span className="text-cc-nav-label">/</span>
          <span>
            <span className="text-cc-nav-label">Spec </span>
            <span className="text-cc-ink">Composite Schemas</span>
          </span>
          <span className="text-cc-nav-label">/</span>
          <span>
            <span className="text-cc-nav-label">Built on </span>
            <span className="text-cc-ink">Hot Chocolate</span>
          </span>
        </div>
      </header>

      {/* Two-column layout: sidebar TOC + article. */}
      <div className="grid gap-10 lg:grid-cols-[240px_minmax(0,1fr)] lg:gap-16">
        <SidebarToc />

        <article className="min-w-0 lg:max-w-3xl">
          {/* Frontmatter / abstract block. */}
          <section
            aria-label="Abstract"
            className="border-cc-card-border border p-5 sm:p-6"
          >
            <p
              className="text-caption font-mono tracking-widest uppercase"
              style={{ color: ACCENT }}
            >
              Abstract
            </p>
            <p className="text-cc-prose mt-3 font-mono text-[12.5px] leading-6">
              Fusion composes independent GraphQL subgraphs into one composite
              schema at build time, proves every reachable field can be
              resolved, and serves the result from a single .NET endpoint your
              team operates. Apollo Federation v2 subgraphs flow through a
              dedicated connector. The gateway is an ASP.NET Core app built on
              Hot Chocolate, always self-run, never a hosted hop in the request
              path.
            </p>
          </section>

          {/* Chapter 01: Composition */}
          <section className="mt-16">
            <ChapterHeading
              num="01"
              id="composition"
              eyebrow="Composition"
              title="Composition runs in CI, not on a hot path."
            />
            <p className="text-cc-prose text-body mt-8 leading-relaxed">
              A composition pipeline reads each subgraph SDL, validates it
              against the others, and emits a Fusion archive your gateway loads
              at startup. Type, enum, and field conflicts surface as diagnostics
              with stable codes, on the build server, before deploy. The gateway
              never sees raw source schemas.
            </p>
            <Bullets
              items={[
                "Runs fully offline as a build step or in CI. Nitro cloud is optional, not in the request path.",
                "Halts on the first failing phase with a stable diagnostic code you can match in scripts.",
                "Emits a versioned, inspectable .far artifact you can diff between releases.",
              ]}
            />
            <SubHeading>Pipeline</SubHeading>
            <HairlineFrame caption="phases: parse, enrich, validate, merge, satisfiability">
              <CompositionPipelineDiagram />
            </HairlineFrame>
            <SubHeading>CI invocation</SubHeading>
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
              <span style={{ color: ACCENT }}>{"OK"}</span>
              <span style={{ color: "#c9d1d9" }}>
                {" composed 3 subgraphs, 0 errors, "}
              </span>
              <span style={{ color: "#a5d6ff" }}>gateway.far</span>
              <span style={{ color: "#c9d1d9" }}>{" written"}</span>
            </ConsoleCard>
          </section>

          {/* Chapter 02: Satisfiability proof */}
          <section className="mt-20">
            <ChapterHeading
              num="02"
              id="satisfiability"
              eyebrow="Satisfiability proof"
              title="If it composes, it answers."
            />
            <p className="text-cc-prose text-body mt-8 leading-relaxed">
              Composition&apos;s final phase walks every reachable field from
              the root types and proves it can be resolved across your subgraphs
              given the available lookups and keys. A query that successfully
              validates against the gateway is one your services can actually
              answer. Unreachable shapes fail composition with
              UNSATISFIABLE_QUERY_PATH.
            </p>
            <Bullets
              items={[
                "Reachability analysis over the full composed graph, shipped in Fusion.Composition.Satisfiability.",
                "Catches contract drift between subgraphs before a client ever sends the query.",
                "Failures cite the exact field path, so the broken shape is the next thing you fix.",
              ]}
            />
            <SubHeading>Reachability walk</SubHeading>
            <HairlineFrame caption="every reachable field has a resolver path">
              <SatisfiabilityDiagram />
            </HairlineFrame>
          </section>

          {/* Chapter 03: Federation interop */}
          <section className="mt-20">
            <ChapterHeading
              num="03"
              id="federation"
              eyebrow="Apollo Federation"
              title="Apollo Federation spec compatible, on an open standard."
            />
            <p className="text-cc-prose text-body mt-8 leading-relaxed">
              Fusion implements the GraphQL Composite Schemas specification
              under the GraphQL Foundation, and reads Apollo Federation v2
              subgraphs through a dedicated connector. Bring existing @key,
              @requires, and @provides directives into a Fusion composition
              without rewriting resolvers, on a vendor-neutral spec.
            </p>
            <Bullets
              items={[
                "GraphQL Composite Schemas spec, vendor-neutral. Subgraph schemas stay portable.",
                "Apollo Federation v2 interop via Fusion.Connectors.ApolloFederation.",
                "Documented migration path from Federation v2 with directive-by-directive mapping.",
              ]}
            />
            <SubHeading>Subgraph sources</SubHeading>
            <HairlineFrame caption="Federation v2 subgraphs are valid Fusion subgraphs">
              <FederationInteropDiagram />
            </HairlineFrame>
            <SubHeading>Directive mapping</SubHeading>
            <div className="border-cc-card-border mt-6 overflow-hidden border">
              <table className="w-full font-mono text-[12px]">
                <thead>
                  <tr className="border-cc-card-border text-cc-nav-label border-b text-left uppercase">
                    <th className="px-4 py-2.5 font-medium tracking-widest">
                      Federation v2
                    </th>
                    <th className="px-4 py-2.5 font-medium tracking-widest">
                      Composite Schemas
                    </th>
                    <th className="px-4 py-2.5 font-medium tracking-widest">
                      Notes
                    </th>
                  </tr>
                </thead>
                <tbody className="text-cc-ink">
                  {[
                    {
                      from: "@key",
                      to: "@key",
                      note: "entity identity preserved",
                    },
                    {
                      from: "@requires",
                      to: "@require",
                      note: "renamed, same intent",
                    },
                    {
                      from: "@provides",
                      to: "selection set",
                      note: "expressed via planner",
                    },
                  ].map((row) => (
                    <tr
                      key={row.from}
                      className="border-cc-card-border border-t"
                    >
                      <td className="px-4 py-2.5" style={{ color: ACCENT }}>
                        {row.from}
                      </td>
                      <td className="px-4 py-2.5" style={{ color: ACCENT }}>
                        {row.to}
                      </td>
                      <td className="text-cc-ink-dim px-4 py-2.5">
                        {row.note}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          {/* Chapter 04: Distributed query plan */}
          <section className="mt-20">
            <ChapterHeading
              num="04"
              id="plan"
              eyebrow="Distributed query plan"
              title="One client request, a planned distributed fetch."
            />
            <p className="text-cc-prose text-body mt-8 leading-relaxed">
              The gateway compiles each incoming operation into a query plan
              over your subgraphs. Independent fetches run in parallel,
              dependent fetches sequence behind them, and shared entity keys are
              batched into single HTTP/2 calls. The result is one response,
              assembled from the minimum work your fleet needs to do.
            </p>
            <Bullets
              items={[
                "Parallel fan-out where fetches are independent, sequencing only where a fetch needs prior data.",
                "Entity keys collected across the plan and batched, no N+1 across services.",
                "Persisted operations and conservative cache control merged from subgraph policies.",
              ]}
            />
            <SubHeading>Plan shape</SubHeading>
            <HairlineFrame caption="parallel where independent, sequenced where required">
              <QueryPlanDiagram />
            </HairlineFrame>
            <SubHeading>Trace in Nitro</SubHeading>
            <p className="text-cc-prose text-body mt-4 leading-relaxed">
              Fusion emits OpenTelemetry spans for each request, the planning
              step, and every subgraph fetch. Nitro renders the plan as a
              navigable trace, so when a single subgraph slows down you see
              which step in the plan, which subgraph, and which keys were in the
              batch.
            </p>
            <div className="border-cc-card-border bg-cc-surface mt-6 overflow-hidden border">
              <NitroFusion />
            </div>
            <p className="text-cc-ink-dim mt-3 font-mono text-[11px] tracking-widest uppercase">
              spans: ExecuteRequest, PlanOperation, ExecutePlanNode
            </p>
          </section>

          {/* Chapter 05: .NET-native gateway */}
          <section className="mt-20">
            <ChapterHeading
              num="05"
              id="dotnet"
              eyebrow=".NET-native gateway"
              title="The gateway is your code, on Hot Chocolate."
            />
            <p className="text-cc-prose text-body mt-8 leading-relaxed">
              Fusion&apos;s gateway is an ASP.NET Core app, configured with
              AddGraphQLGateway() and built on Hot Chocolate. Your DI container,
              your authentication, your middleware, your logging. No standalone
              binary, no YAML, no Node runtime in the request path. The same Hot
              Chocolate server you already ship can be a Fusion subgraph with no
              resolver changes.
            </p>
            <Bullets
              items={[
                "AddGraphQLGateway() integrates with the ASP.NET Core middleware pipeline you already operate.",
                "Auth, header propagation, and cache control land where you expect them in .NET.",
                "An existing Hot Chocolate server is already a valid subgraph, no federation library needed.",
              ]}
            />
            <SubHeading>Middleware pipeline</SubHeading>
            <HairlineFrame caption="Fusion sits inside the ASP.NET Core pipeline">
              <DotNetGatewayDiagram />
            </HairlineFrame>
            <SubHeading>Program.cs</SubHeading>
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
          </section>

          {/* Chapter 06: Self-run */}
          <section className="mt-20">
            <ChapterHeading
              num="06"
              id="self-run"
              eyebrow="Self-run, always"
              title="The gateway is always self-run, never a hosted hop."
            />
            <p className="text-cc-prose text-body mt-8 leading-relaxed">
              Fusion runs in your infrastructure, period. Every client request
              and every subgraph fetch stay inside your network boundary. You
              choose the cluster, the auth, the egress, the audit trail. Nitro
              cloud is available for managed composition delivery, never as a
              hop in the request path.
            </p>
            <Bullets
              items={[
                "Runs in any environment that runs ASP.NET Core, on your own compute.",
                "No third-party gateway in the request path, no data egress you did not approve.",
                "Standard ASP.NET Core auth (JWT, cookie, OIDC, mTLS) and header propagation to subgraphs.",
              ]}
            />
            <SubHeading>Network boundary</SubHeading>
            <HairlineFrame caption="all traffic stays inside your network">
              <SelfRunDiagram />
            </HairlineFrame>
            <p className="text-cc-ink-dim mt-4 font-mono text-[11px] tracking-widest uppercase">
              note: Nitro stays out of the request path. It is opt-in for
              composition delivery and telemetry.
            </p>
          </section>

          {/* Chapter 07: Colophon + CTA */}
          <section className="mt-20">
            <ChapterHeading
              num="07"
              id="colophon"
              eyebrow="Open source / colophon"
              title="Open source, on an open standard."
            />
            <p className="text-cc-prose text-body mt-8 leading-relaxed">
              Fusion is part of the ChilliCream GraphQL Platform, developed in
              the open under the MIT license and built on the GraphQL Composite
              Schemas specification under the GraphQL Foundation. The codebase,
              the issue tracker, the roadmap, and the release notes all live on
              GitHub.
            </p>

            <SubHeading>Colophon</SubHeading>
            <dl className="border-cc-card-border mt-6 grid grid-cols-1 border sm:grid-cols-2">
              {[
                { k: "License", v: "MIT" },
                { k: "Runtime", v: "ASP.NET Core" },
                { k: "Spec", v: "Composite Schemas" },
                { k: "Built on", v: "Hot Chocolate" },
                { k: "Interop", v: "Federation v2" },
                { k: "Tracing", v: "OpenTelemetry" },
              ].map((row, idx) => (
                <div
                  key={row.k}
                  className={[
                    "flex items-baseline justify-between gap-4 px-5 py-3",
                    idx % 2 === 0 ? "border-cc-card-border sm:border-r" : "",
                    idx >= 2 ? "border-cc-card-border border-t" : "",
                  ].join(" ")}
                >
                  <dt className="text-cc-nav-label font-mono text-[11px] tracking-widest uppercase">
                    {row.k}
                  </dt>
                  <dd
                    className="font-mono text-[12px]"
                    style={{ color: ACCENT }}
                  >
                    {row.v}
                  </dd>
                </div>
              ))}
            </dl>

            {/* Closing CTA with the single brand-spectrum hairline. */}
            <div className="relative mt-16 pt-12 text-center">
              <div
                aria-hidden
                className="pointer-events-none absolute inset-x-0 top-0 h-px"
                style={{ background: SPECTRUM }}
              />
              <p
                className="text-caption font-mono tracking-widest uppercase"
                style={{ color: ACCENT }}
              >
                Get started
              </p>
              <h3 className="text-cc-heading font-heading text-h3 mx-auto mt-5 max-w-3xl font-semibold tracking-tight text-balance">
                One composite graph, proven before you ship it.
              </h3>
              <p className="text-cc-prose text-body mx-auto mt-5 max-w-2xl leading-relaxed">
                Point Fusion at your subgraphs, compose in CI, and serve from a
                single .NET endpoint you operate yourself. The plan is built,
                the satisfiability is proven, and the runtime is the ASP.NET
                Core you already run.
              </p>
              <div className="mt-8 flex flex-wrap justify-center gap-3">
                <SolidButton href="/docs/fusion">Get Started</SolidButton>
                <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                  View on GitHub
                </OutlineButton>
              </div>
            </div>
          </section>
        </article>
      </div>
    </div>
  );
}
