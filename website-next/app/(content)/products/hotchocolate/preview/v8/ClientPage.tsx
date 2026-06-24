"use client";

import { motion } from "motion/react";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

// Brand spectrum is allowed exactly once per page, only on the final splash.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// Newsprint half-tone dot field. Sits behind everything, only visible in the
// gutters between bordered panels.
const NEWSPRINT_DOTS =
  "radial-gradient(rgba(245,241,234,0.022) 1px, transparent 1.2px)";

// -----------------------------------------------------------------------------
// Primitives
// -----------------------------------------------------------------------------

interface CaptionBarProps {
  readonly index?: string;
  readonly subject: string;
  readonly note?: string;
  readonly tone?: "default" | "accent";
}

function CaptionBar({
  index,
  subject,
  note,
  tone = "default",
}: CaptionBarProps) {
  const accent = tone === "accent" ? "text-cc-accent" : "text-cc-ink";
  return (
    <div className="border-cc-card-border flex items-center gap-3 border-b px-4 py-2.5">
      <motion.span
        aria-hidden
        initial={{ clipPath: "inset(0 100% 0 0)" }}
        whileInView={{ clipPath: "inset(0 0% 0 0)" }}
        viewport={{ once: true, amount: 0.4 }}
        transition={{ duration: 0.5, ease: "easeOut" }}
        className={[
          "font-mono text-[10.5px] font-medium tracking-[0.2em] uppercase",
          accent,
        ].join(" ")}
      >
        {index ? `PANEL ${index} / ${subject}` : subject}
      </motion.span>
      {note ? (
        <span className="text-cc-ink-dim ml-auto truncate font-mono text-[10.5px] italic">
          {note}
        </span>
      ) : null}
      <span
        aria-hidden
        className="border-cc-card-border bg-cc-bg ml-auto inline-block h-2 w-2 shrink-0 rounded-full border"
      />
    </div>
  );
}

interface FooterCaptionProps {
  readonly dialogue: string;
  readonly narrator?: string;
}

function FooterCaption({ dialogue, narrator }: FooterCaptionProps) {
  return (
    <div className="border-cc-card-border flex flex-wrap items-center justify-between gap-3 border-t px-4 py-2.5">
      <span className="text-cc-ink font-mono text-[11px] tracking-tight">
        {dialogue}
      </span>
      {narrator ? (
        <span className="text-cc-ink-dim font-mono text-[10.5px] italic">
          {narrator}
        </span>
      ) : null}
    </div>
  );
}

interface OnomatopoeiaProps {
  readonly children: ReactNode;
  readonly className?: string;
}

function Onomatopoeia({ children, className }: OnomatopoeiaProps) {
  return (
    <motion.span
      animate={{ scale: [1, 1.04, 1] }}
      transition={{ duration: 2.4, repeat: Infinity, ease: "easeInOut" }}
      className={[
        "border-cc-accent text-cc-accent inline-flex items-center justify-center rounded-md border px-2.5 py-1 font-mono text-[10.5px] font-semibold tracking-[0.2em] uppercase",
        "bg-cc-bg/70 backdrop-blur-sm",
        className ?? "",
      ].join(" ")}
    >
      {children}
    </motion.span>
  );
}

interface PanelProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly delayMs?: number;
  readonly bleed?: boolean;
}

function Panel({
  children,
  className,
  delayMs = 0,
  bleed = false,
}: PanelProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.2 }}
      transition={{
        duration: 0.45,
        ease: "easeOut",
        delay: delayMs / 1000,
      }}
      className={[
        bleed
          ? "border-cc-card-border relative overflow-hidden border-y"
          : "border-cc-card-border bg-cc-surface relative overflow-hidden rounded-xl border",
        className ?? "",
      ].join(" ")}
    >
      {children}
    </motion.div>
  );
}

// -----------------------------------------------------------------------------
// Hero splash (Panel 00)
// -----------------------------------------------------------------------------

function HeroSplash() {
  return (
    <Panel className="px-0">
      <CaptionBar
        index="00"
        subject="COVER"
        note="A QUIET CATALOG SERVICE. DAWN."
      />
      <div className="relative grid items-stretch gap-0 lg:grid-cols-12">
        <div className="px-6 pt-10 pb-12 sm:px-10 sm:pt-14 sm:pb-16 lg:col-span-7">
          <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.2em] uppercase">
            ISSUE 01 / VOL .NET / OPEN SOURCE FOREVER
          </span>
          <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.02] font-semibold tracking-tight text-balance sm:text-6xl">
            Your C# is the schema.
          </h1>
          <p className="text-cc-prose mt-6 max-w-xl text-base leading-relaxed sm:text-lg">
            Hot Chocolate is the open-source GraphQL server for .NET. Annotate a
            partial class, write idiomatic C# resolvers, and a Roslyn source
            generator emits the schema, the resolver pipeline, and DataLoader
            infrastructure at build time. One server speaks HTTP, WebSocket, and
            Server-Sent Events, and the same code can run standalone or as a
            Fusion subgraph later.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
          <dl className="border-cc-card-border mt-10 grid max-w-lg grid-cols-3 gap-6 border-t pt-6">
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
              <dd className="text-cc-ink mt-1 text-sm">GraphQL 2025</dd>
            </div>
          </dl>
        </div>
        <div className="border-cc-card-border bg-cc-card-bg relative flex items-center justify-center px-6 py-10 sm:px-8 lg:col-span-5 lg:border-l">
          <div className="relative w-full max-w-sm">
            <div className="border-cc-card-border bg-cc-bg rounded-lg border p-4">
              <div className="text-cc-ink-dim mb-2 flex items-center justify-between font-mono text-[10px] tracking-widest uppercase">
                <span>Catalog/Query.cs</span>
                <span>C#</span>
              </div>
              <pre className="font-mono text-[11px] leading-5 whitespace-pre">
                <span className="text-cc-ink-dim">{`// the C# is the schema\n`}</span>
                <span className="text-cc-accent">{`[QueryType]\n`}</span>
                <span className="text-cc-ink">{`public partial class Query\n{\n  public static async Task<Product?>\n    GetByIdAsync(\n      Guid id,\n      IProductByIdDataLoader loader,\n      CancellationToken ct)\n      => await loader\n        .LoadAsync(id, ct);\n}`}</span>
              </pre>
            </div>
            <Onomatopoeia className="absolute -top-3 -right-3 rotate-3">
              BUILD!
            </Onomatopoeia>
          </div>
        </div>
      </div>
      <FooterCaption
        dialogue="NARRATOR: One annotation. One partial class. The compiler does the rest."
        narrator="cont. on next panel"
      />
    </Panel>
  );
}

// -----------------------------------------------------------------------------
// Table of Contents strip
// -----------------------------------------------------------------------------

function TableOfContents() {
  const chapters: readonly { readonly id: string; readonly label: string }[] = [
    { id: "01", label: "COMPOSITION" },
    { id: "02", label: "THE GENERATOR" },
    { id: "03", label: "DATALOADER RESCUE" },
    { id: "04", label: "FAN-OUT" },
    { id: "05", label: "THE TRACE" },
    { id: "06", label: "TWO ROADS" },
  ];
  return (
    <Panel>
      <CaptionBar subject="TABLE OF CONTENTS" note="six panels, one request" />
      <ul className="grid grid-cols-2 gap-x-4 gap-y-3 px-4 py-5 sm:grid-cols-3 lg:grid-cols-6">
        {chapters.map((c) => (
          <li
            key={c.id}
            className="border-cc-card-border bg-cc-card-bg flex items-center gap-2 rounded-md border px-3 py-2"
          >
            <span className="text-cc-accent font-mono text-[10.5px] tracking-widest">
              {c.id}
            </span>
            <span className="text-cc-ink font-mono text-[10.5px] tracking-tight uppercase">
              {c.label}
            </span>
          </li>
        ))}
      </ul>
    </Panel>
  );
}

// -----------------------------------------------------------------------------
// Diagrams (panel interiors). All inline SVG, cc-* tokens only.
// -----------------------------------------------------------------------------

function CompositionDiagram() {
  return (
    <svg
      viewBox="0 0 720 260"
      className="h-auto w-full"
      role="img"
      aria-label="Three subgraph schemas composed into a Fusion plan"
    >
      <defs>
        <linearGradient id="v8-comp-line" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.1" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.85" />
        </linearGradient>
      </defs>
      {[
        { y: 28, label: "catalog.graphql" },
        { y: 112, label: "checkout.graphql" },
        { y: 196, label: "reviews.graphql" },
      ].map((n) => (
        <g key={n.label}>
          <rect
            x="20"
            y={n.y}
            width="190"
            height="36"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="115"
            y={n.y + 22}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#a1a3af"
          >
            {n.label}
          </text>
          <path
            d={`M 210 ${n.y + 18} L 360 130`}
            stroke="url(#v8-comp-line)"
            strokeWidth="1.5"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="360"
        y="106"
        width="140"
        height="52"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="430"
        y="128"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="12"
        fill="#5eead4"
      >
        fusion compose
      </text>
      <text
        x="430"
        y="145"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        build time
      </text>
      <path
        d="M 500 132 L 580 132"
        stroke="rgba(94,234,212,0.7)"
        strokeWidth="1.5"
        fill="none"
      />
      <polygon points="580,127 596,132 580,137" fill="rgba(94,234,212,0.7)" />
      {/* speech bubble */}
      <g>
        <rect
          x="596"
          y="92"
          width="108"
          height="36"
          rx="14"
          fill="rgba(12,19,34,0.9)"
          stroke="rgba(94,234,212,0.55)"
        />
        <polygon
          points="596,116 586,124 600,122"
          fill="rgba(12,19,34,0.9)"
          stroke="rgba(94,234,212,0.55)"
        />
        <text
          x="650"
          y="115"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="11"
          fill="#5eead4"
        >
          plan emitted
        </text>
      </g>
    </svg>
  );
}

function GeneratorDiagram() {
  return (
    <svg
      viewBox="0 0 520 320"
      className="h-auto w-full"
      role="img"
      aria-label="Attribute on partial class triggers Roslyn generator emitting schema, resolvers, dataloaders"
    >
      <defs>
        <linearGradient id="v8-gen-line" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.2" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.7" />
        </linearGradient>
      </defs>
      {/* attribute card */}
      <rect
        x="20"
        y="130"
        width="150"
        height="60"
        rx="8"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(94,234,212,0.5)"
      />
      <text
        x="95"
        y="156"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="12"
        fill="#5eead4"
      >
        [QueryType]
      </text>
      <text
        x="95"
        y="174"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        partial class
      </text>
      <path
        d="M 170 160 L 220 160"
        stroke="url(#v8-gen-line)"
        strokeWidth="1.5"
        fill="none"
      />
      {/* cog / generator */}
      <g transform="translate(220 130)">
        <rect
          x="0"
          y="0"
          width="100"
          height="60"
          rx="10"
          fill="rgba(94,234,212,0.08)"
          stroke="rgba(94,234,212,0.55)"
        />
        <circle
          cx="50"
          cy="30"
          r="14"
          fill="none"
          stroke="rgba(94,234,212,0.7)"
          strokeWidth="1.5"
        />
        <circle cx="50" cy="30" r="4" fill="rgba(94,234,212,0.7)" />
        {[0, 60, 120, 180, 240, 300].map((deg) => (
          <line
            key={deg}
            x1="50"
            y1="30"
            x2={50 + Math.cos((deg * Math.PI) / 180) * 19}
            y2={30 + Math.sin((deg * Math.PI) / 180) * 19}
            stroke="rgba(94,234,212,0.7)"
            strokeWidth="1.5"
          />
        ))}
      </g>
      {/* connectors out */}
      {[
        { y: 50, label: "schema.graphql", note: "SDL" },
        { y: 150, label: "resolver pipeline", note: "wiring" },
        { y: 250, label: "*DataLoader", note: "batching" },
      ].map((row) => (
        <g key={row.label}>
          <path
            d={`M 320 160 C 360 160, 360 ${row.y + 24}, 380 ${row.y + 24}`}
            stroke="url(#v8-gen-line)"
            strokeWidth="1.3"
            fill="none"
          />
          {/* speech-bubble style emit */}
          <rect
            x="380"
            y={row.y}
            width="130"
            height="48"
            rx="14"
            fill="rgba(12,19,34,0.85)"
            stroke="rgba(94,234,212,0.5)"
          />
          <polygon
            points={`380,${row.y + 28} 370,${row.y + 36} 384,${row.y + 34}`}
            fill="rgba(12,19,34,0.85)"
            stroke="rgba(94,234,212,0.5)"
          />
          <text
            x="445"
            y={row.y + 22}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#5eead4"
          >
            {row.label}
          </text>
          <text
            x="445"
            y={row.y + 38}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.55)"
          >
            {row.note}
          </text>
        </g>
      ))}
    </svg>
  );
}

function AuthoringDiagram() {
  return (
    <svg
      viewBox="0 0 360 320"
      className="h-auto w-full"
      role="img"
      aria-label="Implementation-first or code-first, one schema"
    >
      {[
        {
          y: 30,
          title: "Implementation-first",
          sub: "[QueryType] partial class",
        },
        { y: 130, title: "Code-first", sub: "ObjectType<T> + descriptor" },
      ].map((row) => (
        <g key={row.title}>
          <rect
            x="20"
            y={row.y}
            width="200"
            height="64"
            rx="8"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(94,234,212,0.45)"
          />
          <text
            x="34"
            y={row.y + 26}
            fontFamily="var(--font-body)"
            fontSize="12"
            fill="#f5f0ea"
          >
            {row.title}
          </text>
          <text
            x="34"
            y={row.y + 46}
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.62)"
          >
            {row.sub}
          </text>
          <path
            d={`M 220 ${row.y + 32} C 260 ${row.y + 32}, 260 230, 180 250`}
            stroke="rgba(94,234,212,0.35)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="60"
        y="240"
        width="240"
        height="56"
        rx="10"
        fill="rgba(12,19,34,0.6)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="180"
        y="266"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="13"
        fill="#f5f0ea"
      >
        One GraphQL schema
      </text>
      <text
        x="180"
        y="284"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10.5"
        fill="rgba(245,241,234,0.6)"
      >
        spec-compliant SDL
      </text>
    </svg>
  );
}

function NPlusOneDiagram() {
  // five sequential field calls, escalating panic
  const calls = [40, 78, 116, 154, 192];
  return (
    <svg
      viewBox="0 0 360 290"
      className="h-auto w-full"
      role="img"
      aria-label="Five repeated product(id) field calls without batching"
    >
      {calls.map((y, i) => (
        <g key={y}>
          <rect
            x="20"
            y={y}
            width="220"
            height="26"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="32"
            y={y + 17}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="rgba(245,241,234,0.62)"
          >
            product(id: {i + 1}) -&gt; db
          </text>
          <text
            x="246"
            y={y + 17}
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.45)"
          >
            roundtrip
          </text>
        </g>
      ))}
      {/* BAM speech */}
      <g transform="translate(220 230)">
        <rect
          x="0"
          y="0"
          width="116"
          height="40"
          rx="14"
          fill="rgba(12,19,34,0.9)"
          stroke="rgba(240,120,106,0.7)"
        />
        <text
          x="58"
          y="25"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="12"
          fill="#f0786a"
          fontWeight="600"
          letterSpacing="2"
        >
          N+1!
        </text>
      </g>
    </svg>
  );
}

function BatchedDiagram() {
  const calls = [40, 70, 100, 130, 160];
  return (
    <svg
      viewBox="0 0 520 290"
      className="h-auto w-full"
      role="img"
      aria-label="Five calls collapsed into a single batched LoadAsync"
    >
      {calls.map((y, i) => (
        <g key={y}>
          <rect
            x="20"
            y={y}
            width="120"
            height="20"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="30"
            y={y + 14}
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.62)"
          >
            id: {i + 1}
          </text>
          <path
            d={`M 140 ${y + 10} C 200 ${y + 10}, 200 130, 260 130`}
            stroke="rgba(94,234,212,0.45)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="260"
        y="108"
        width="130"
        height="44"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="325"
        y="128"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11.5"
        fill="#5eead4"
      >
        LoadAsync(ids)
      </text>
      <text
        x="325"
        y="144"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.6)"
      >
        1 batched call
      </text>
      <path
        d="M 390 130 L 460 130"
        stroke="rgba(94,234,212,0.55)"
        strokeWidth="1.4"
        fill="none"
      />
      <polygon points="460,125 476,130 460,135" fill="rgba(94,234,212,0.7)" />
      <text
        x="476"
        y="118"
        textAnchor="end"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        db
      </text>
      {/* BATCH! */}
      <g transform="translate(280 196)">
        <rect
          x="0"
          y="0"
          width="116"
          height="40"
          rx="14"
          fill="rgba(12,19,34,0.9)"
          stroke="rgba(94,234,212,0.7)"
        />
        <text
          x="58"
          y="25"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="12"
          fill="#5eead4"
          fontWeight="600"
          letterSpacing="2"
        >
          BATCH!
        </text>
      </g>
    </svg>
  );
}

function FanOutDiagram() {
  return (
    <svg
      viewBox="0 0 760 280"
      className="h-auto w-full"
      role="img"
      aria-label="Subscription fan-out over WebSocket and Server-Sent Events"
    >
      {/* ITopicEventSender on left */}
      <rect
        x="20"
        y="116"
        width="160"
        height="56"
        rx="8"
        fill="rgba(245,241,234,0.04)"
        stroke="rgba(94,234,212,0.45)"
      />
      <text
        x="100"
        y="138"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#f5f0ea"
      >
        ITopicEventSender
      </text>
      <text
        x="100"
        y="156"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        publish from anywhere
      </text>
      {/* provider chips */}
      {[
        { x: 20, y: 196, label: "Redis" },
        { x: 84, y: 196, label: "NATS" },
        { x: 142, y: 196, label: "Postgres" },
      ].map((c) => (
        <g key={c.label}>
          <rect
            x={c.x}
            y={c.y}
            width={c.label === "Postgres" ? 58 : 56}
            height="20"
            rx="4"
            fill="rgba(94,234,212,0.06)"
            stroke="rgba(94,234,212,0.35)"
          />
          <text
            x={c.x + (c.label === "Postgres" ? 29 : 28)}
            y={c.y + 14}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="#5eead4"
          >
            {c.label}
          </text>
        </g>
      ))}
      {/* center subscription type */}
      <path
        d="M 180 144 L 280 144"
        stroke="rgba(94,234,212,0.5)"
        strokeWidth="1.4"
        fill="none"
      />
      <rect
        x="280"
        y="116"
        width="160"
        height="56"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="360"
        y="138"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#5eead4"
      >
        [SubscriptionType]
      </text>
      <text
        x="360"
        y="156"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        dynamic topics
      </text>
      {/* fan-out lines */}
      {[
        { y: 60, label: "ws" },
        { y: 144, label: "sse" },
        { y: 228, label: "ws" },
      ].map((row, i) => (
        <g key={i}>
          <path
            d={`M 440 144 C 520 144, 520 ${row.y}, 600 ${row.y}`}
            stroke="rgba(245,241,234,0.35)"
            strokeWidth="1.3"
            fill="none"
          />
          <rect
            x="600"
            y={row.y - 14}
            width="80"
            height="28"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="640"
            y={row.y + 5}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="rgba(245,241,234,0.62)"
          >
            {row.label}
          </text>
        </g>
      ))}
      {/* FAN-OUT! badge */}
      <g transform="translate(450 30)">
        <rect
          x="0"
          y="0"
          width="130"
          height="40"
          rx="14"
          fill="rgba(12,19,34,0.9)"
          stroke="rgba(94,234,212,0.7)"
        />
        <text
          x="65"
          y="25"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="12"
          fill="#5eead4"
          fontWeight="600"
          letterSpacing="2"
        >
          FAN-OUT!
        </text>
      </g>
    </svg>
  );
}

interface SpanPanelProps {
  readonly label: string;
  readonly bars: readonly { readonly x: number; readonly w: number }[];
  readonly footer?: string;
}

function SpanMiniDiagram({ label, bars, footer }: SpanPanelProps) {
  return (
    <svg
      viewBox="0 0 280 180"
      className="h-auto w-full"
      role="img"
      aria-label={`OTel span ${label}`}
    >
      <text
        x="14"
        y="20"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        {label}
      </text>
      {bars.map((b, i) => (
        <rect
          key={i}
          x={b.x}
          y={36 + i * 20}
          width={b.w}
          height="12"
          rx="3"
          fill={`rgba(94,234,212,${0.5 - i * 0.08})`}
        />
      ))}
      <line
        x1="14"
        y1="142"
        x2="266"
        y2="142"
        stroke="rgba(245,241,234,0.16)"
      />
      {footer ? (
        <text
          x="14"
          y="160"
          fontFamily="ui-monospace, monospace"
          fontSize="10"
          fill="rgba(245,241,234,0.5)"
        >
          {footer}
        </text>
      ) : null}
    </svg>
  );
}

function FederationDiagram() {
  return (
    <svg
      viewBox="0 0 720 260"
      className="h-auto w-full"
      role="img"
      aria-label="Hot Chocolate server feeds Fusion gateway and Apollo Federation gateway"
    >
      <rect
        x="240"
        y="100"
        width="200"
        height="60"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="340"
        y="124"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="13"
        fill="#f5f0ea"
      >
        Hot Chocolate server
      </text>
      <text
        x="340"
        y="144"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10.5"
        fill="rgba(245,241,234,0.6)"
      >
        same resolvers, two roads
      </text>
      {[
        { y: 28, label: "Fusion gateway", sub: "build-time plan" },
        { y: 172, label: "Apollo Federation", sub: "spec subgraph" },
      ].map((g) => (
        <g key={g.label}>
          <rect
            x="500"
            y={g.y}
            width="200"
            height="56"
            rx="8"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="600"
            y={g.y + 24}
            textAnchor="middle"
            fontFamily="var(--font-body)"
            fontSize="12"
            fill="#f5f0ea"
          >
            {g.label}
          </text>
          <text
            x="600"
            y={g.y + 42}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.6)"
          >
            {g.sub}
          </text>
          <path
            d={`M 440 130 C 470 130, 480 ${g.y + 28}, 500 ${g.y + 28}`}
            stroke="rgba(94,234,212,0.45)"
            strokeWidth="1.3"
            fill="none"
          />
        </g>
      ))}
      {/* small client glyphs on left */}
      {[60, 130, 200].map((y, i) => (
        <g key={i}>
          <rect
            x="20"
            y={y - 12}
            width="60"
            height="24"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="50"
            y={y + 4}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.6)"
          >
            client
          </text>
          <path
            d={`M 80 ${y} C 140 ${y}, 180 130, 240 130`}
            stroke="rgba(245,241,234,0.3)"
            strokeWidth="1.2"
            fill="none"
          />
        </g>
      ))}
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Character sheet (ProofItem)
// -----------------------------------------------------------------------------

interface StatBoxProps {
  readonly label: string;
  readonly value: string;
}

function StatBox({ label, value }: StatBoxProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-lg border p-4">
      <div className="text-cc-ink-dim font-mono text-[10px] tracking-widest uppercase">
        {label}
      </div>
      <div className="text-cc-heading font-heading mt-2 text-xl font-semibold tracking-tight">
        {value}
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export function ClientPage() {
  return (
    <div className="relative">
      {/* newsprint dot field behind everything, gutter-only because panels cover it */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage: NEWSPRINT_DOTS,
          backgroundSize: "14px 14px",
        }}
      />
      <div className="mx-auto flex max-w-6xl flex-col gap-4 py-10 sm:py-14">
        {/* PANEL 00 / COVER */}
        <HeroSplash />

        {/* TOC strip */}
        <TableOfContents />

        {/* PANEL 01 (full width) */}
        <Panel>
          <CaptionBar
            index="01"
            subject="BUILD TIME"
            note="three subgraphs walk into a CI"
          />
          <div className="grid items-center gap-6 px-4 py-6 sm:px-6 lg:grid-cols-12">
            <div className="lg:col-span-7">
              <CompositionDiagram />
            </div>
            <div className="lg:col-span-5">
              <span className="text-cc-accent font-mono text-[10.5px] tracking-[0.2em] uppercase">
                composition
              </span>
              <h2 className="text-cc-heading font-heading mt-3 text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                Compose subgraphs at build time, not at runtime.
              </h2>
              <p className="text-cc-prose mt-3 text-sm leading-relaxed sm:text-base">
                Fusion plans composition once, in CI, against the source SDLs.
                The gateway loads a finished query plan and stays cheap to run
                at the edge. Schema changes show up as planning errors before
                they show up as production incidents.
              </p>
              <ul className="mt-4 flex flex-col gap-2">
                {[
                  "Compose any mix of Hot Chocolate subgraphs into a single planned gateway schema.",
                  "Resolver paths stay typed end-to-end across the gateway.",
                  "Standalone today, Fusion subgraph tomorrow, no resolver rewrites.",
                ].map((b) => (
                  <li
                    key={b}
                    className="text-cc-ink flex items-start gap-2 text-[13px] leading-relaxed"
                  >
                    <span className="text-cc-accent mt-1 shrink-0">
                      <CheckIcon size={12} />
                    </span>
                    <span>{b}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>
          <FooterCaption
            dialogue="COMPOSER: a finished plan ships to the gateway."
            narrator="cont. in panel 02"
          />
        </Panel>

        {/* PANEL 02 / 02b row (7+5) */}
        <div className="grid gap-4 lg:grid-cols-12">
          <Panel className="lg:col-span-7" delayMs={60}>
            <CaptionBar
              index="02"
              subject="THE GENERATOR ARRIVES"
              note="Roslyn enters, stage left"
            />
            <div className="px-4 py-6 sm:px-6">
              <GeneratorDiagram />
            </div>
            <FooterCaption
              dialogue="GENERATOR: schema, resolvers, and DataLoaders, emitted on save."
              narrator="this used to be hand-wired"
            />
          </Panel>
          <Panel className="lg:col-span-5" delayMs={120}>
            <CaptionBar
              subject="PANEL 02B / AUTHORING"
              note="pick a side, get one schema"
            />
            <div className="px-4 py-6 sm:px-6">
              <AuthoringDiagram />
            </div>
            <FooterCaption
              dialogue="AUTHOR: Implementation-first by default. Code-first when you need it."
              narrator="dotnet new graphql"
            />
          </Panel>
        </div>

        {/* PANEL 03 + 03b (5+7) */}
        <div className="grid gap-4 lg:grid-cols-12">
          <Panel className="lg:col-span-5">
            <CaptionBar
              index="03"
              subject="N+1 APPEARS"
              note="the villain you already met"
              tone="default"
            />
            <div className="relative px-4 py-6 sm:px-6">
              <NPlusOneDiagram />
              <Onomatopoeia className="absolute top-3 right-3 -rotate-2 border-[color:#f0786a] text-[color:#f0786a]">
                BAM!
              </Onomatopoeia>
            </div>
            <FooterCaption
              dialogue="WITHOUT BATCHING: five fields, five roundtrips."
              narrator="this scales poorly"
            />
          </Panel>
          <Panel className="lg:col-span-7" delayMs={80}>
            <CaptionBar
              subject="PANEL 03B / BATCH!"
              note="Green Donut to the rescue"
              tone="accent"
            />
            <div className="relative px-4 py-6 sm:px-6">
              <BatchedDiagram />
            </div>
            <FooterCaption
              dialogue="WITH [DataLoader]: keys deduped per request, one batched call."
              narrator="works against EF Core, Mongo, Marten, Raven"
            />
          </Panel>
        </div>

        {/* PANEL 04 (full width) */}
        <Panel>
          <CaptionBar
            index="04"
            subject="FAN-OUT"
            note="one publish, many sockets"
          />
          <div className="px-4 py-6 sm:px-6">
            <FanOutDiagram />
          </div>
          <FooterCaption
            dialogue="SUBSCRIPTION: [Topic] placeholders give dynamic per-resource streams."
            narrator="graphql-ws and graphql-sse on tap"
          />
        </Panel>

        {/* PANEL 05 trio (4+4+4) */}
        <div className="grid gap-4 lg:grid-cols-12">
          <Panel className="lg:col-span-4" delayMs={0}>
            <CaptionBar index="05" subject="PARSE" note="span 1 of 3" />
            <div className="px-4 py-6 sm:px-6">
              <SpanMiniDiagram
                label="graphql.parse + validate"
                bars={[
                  { x: 14, w: 180 },
                  { x: 14, w: 120 },
                  { x: 14, w: 70 },
                ]}
              />
            </div>
            <FooterCaption dialogue="document parsed, AST cached." />
          </Panel>
          <Panel className="lg:col-span-4" delayMs={60}>
            <CaptionBar subject="PANEL 05B / EXECUTE" note="span 2 of 3" />
            <div className="px-4 py-6 sm:px-6">
              <SpanMiniDiagram
                label="graphql.execute"
                bars={[
                  { x: 14, w: 220 },
                  { x: 40, w: 150 },
                  { x: 80, w: 80 },
                ]}
              />
            </div>
            <FooterCaption dialogue="resolvers run, waves planned." />
          </Panel>
          <Panel className="lg:col-span-4" delayMs={120}>
            <CaptionBar
              subject="PANEL 05C / BATCH"
              note="span 3 of 3"
              tone="accent"
            />
            <div className="px-4 py-6 sm:px-6">
              <SpanMiniDiagram
                label="dataloader.batch"
                bars={[
                  { x: 14, w: 100 },
                  { x: 30, w: 50 },
                  { x: 50, w: 20 },
                ]}
                footer="trace_id 7f8a... 42ms"
              />
            </div>
            <FooterCaption dialogue="one batched db hit, end of trace." />
          </Panel>
        </div>

        {/* PANEL 06 (7+5) */}
        <div className="grid gap-4 lg:grid-cols-12">
          <Panel className="lg:col-span-7">
            <CaptionBar
              index="06"
              subject="TWO ROADS"
              note="operational, not architectural"
            />
            <div className="px-4 py-6 sm:px-6">
              <FederationDiagram />
            </div>
            <FooterCaption
              dialogue="SAME RESOLVERS: ship one server, or compose with Fusion, or join an Apollo Federation gateway."
              narrator="the choice is operational"
            />
          </Panel>
          <Panel className="lg:col-span-5" delayMs={80}>
            <CaptionBar
              subject="PANEL 06B / NOTES"
              note="from the writers' room"
            />
            <div className="px-4 py-6 sm:px-6">
              <ul className="flex flex-col gap-3">
                {[
                  "Start with one server, add Fusion only when you actually need to.",
                  "Apollo Federation spec implemented via the ApolloFederation package.",
                  "Cost analysis (@cost, @listSize) and trusted operations apply at every tier.",
                ].map((b) => (
                  <li
                    key={b}
                    className="text-cc-ink flex items-start gap-2 text-[13px] leading-relaxed"
                  >
                    <span className="text-cc-accent mt-1 shrink-0">
                      <CheckIcon size={12} />
                    </span>
                    <span>{b}</span>
                  </li>
                ))}
              </ul>
              <div className="border-cc-card-border mt-5 border-t pt-4">
                <span className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Inset Panel
                </span>
                <p className="text-cc-prose mt-2 text-[13px] leading-relaxed">
                  Hot Chocolate is source-generated. Strawberry Shake uses
                  MSBuild codegen on the client side. Same C#, different
                  emitter.
                </p>
              </div>
            </div>
          </Panel>
        </div>

        {/* EPILOGUE: Nitro IDE */}
        <Panel>
          <CaptionBar
            subject="EPILOGUE / THE IDE IS ALREADY THERE"
            note="served from /graphql"
          />
          <div className="px-4 pt-5 pb-2 sm:px-6">
            <div className="mb-4 grid items-end gap-4 lg:grid-cols-12">
              <div className="lg:col-span-8">
                <h2 className="text-cc-heading font-heading text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                  A GraphQL IDE ships with every server.
                </h2>
                <p className="text-cc-prose mt-3 max-w-2xl text-sm leading-relaxed sm:text-base">
                  Run your server and the Nitro GraphQL IDE is served from the
                  endpoint. Browse the schema, draft operations against your
                  live resolvers, inspect responses, and share documents with
                  the rest of the team.
                </p>
              </div>
              <div className="lg:col-span-4 lg:text-right">
                <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
                  live at /graphql
                </span>
              </div>
            </div>
            <div className="border-cc-card-border bg-cc-bg overflow-hidden rounded-xl border">
              <NitroCompose />
            </div>
          </div>
          <FooterCaption
            dialogue="NARRATOR: schema, draft, share. The IDE is part of the server."
            narrator="cont. in your repo"
          />
        </Panel>

        {/* PROOF / CHARACTER SHEET */}
        <Panel>
          <CaptionBar subject="CHARACTER SHEET" note="stats of a server" />
          <div className="grid gap-8 px-4 py-8 sm:px-6 lg:grid-cols-12">
            <div className="lg:col-span-6">
              <span className="text-cc-accent font-mono text-[10.5px] tracking-[0.2em] uppercase">
                MIT licensed
              </span>
              <h2 className="text-cc-heading font-heading mt-3 text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                Open source, in production, and free to use.
              </h2>
              <p className="text-cc-prose mt-3 max-w-xl text-sm leading-relaxed sm:text-base">
                Hot Chocolate has been developed in the open for years and is
                released under the MIT license. Use it in commercial work, fork
                it, vendor it, audit it. The codebase, the issue tracker, the
                roadmap, and the release notes all live on GitHub.
              </p>
              <div className="mt-6 flex flex-wrap gap-3">
                <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                  View on GitHub
                </SolidButton>
                <OutlineButton href="/docs/hotchocolate">
                  Read the docs
                </OutlineButton>
              </div>
            </div>
            <div className="lg:col-span-6">
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                <StatBox label="License" value="MIT" />
                <StatBox label="Runtime" value=".NET / ASP.NET" />
                <StatBox label="Spec" value="GraphQL 2025" />
                <StatBox label="Transports" value="HTTP / WS / SSE" />
                <StatBox label="Federation" value="Fusion + Apollo" />
                <StatBox label="Client" value="Strawberry Shake" />
              </div>
            </div>
          </div>
        </Panel>

        {/* FINAL SPLASH: borderless, single spectrum hairline at top */}
        <div className="relative mt-2">
          <div
            aria-hidden
            className="pointer-events-none absolute inset-x-0 top-0 h-px"
            style={{ background: SPECTRUM }}
          />
          <motion.section
            initial={{ opacity: 0, y: 8 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, amount: 0.3 }}
            transition={{ duration: 0.5, ease: "easeOut" }}
            className="py-16 text-center sm:py-24"
          >
            <span className="text-cc-accent font-mono text-[11px] tracking-[0.25em] uppercase">
              To be continued... in your repo.
            </span>
            <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
              Ship a GraphQL API your .NET team can actually own.
            </h2>
            <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
              A C# project, a partial class, a few attributes. The schema, the
              DataLoaders, and the resolver pipeline are generated for you at
              build time, and the runtime is the ASP.NET Core you already run.
            </p>
            <div className="mt-8 flex flex-wrap justify-center gap-3">
              <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
          </motion.section>
        </div>
      </div>
    </div>
  );
}
