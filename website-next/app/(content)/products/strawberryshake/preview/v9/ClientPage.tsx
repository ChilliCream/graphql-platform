"use client";

import { motion } from "motion/react";
import type { CSSProperties, ReactNode } from "react";
import { useEffect, useRef, useState } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// The brand spectrum is allowed at most ONCE per page. It lives as the
// hairline on the closing CTA. Nowhere else.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// Very faint inline-SVG dot grid behind the hero only. 1px dots at 24px
// spacing at 6% opacity, encoded as a data URI on a single fixed div.
const HERO_DOTS =
  "url(\"data:image/svg+xml;utf8,%3Csvg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24'%3E%3Ccircle cx='1' cy='1' r='1' fill='%23f5f1ea' fill-opacity='0.06'/%3E%3C/svg%3E\")";

// -----------------------------------------------------------------------------
// Tokens for inline code panels (GitHub-dark approximations).
// -----------------------------------------------------------------------------

const T: Record<string, CSSProperties> = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  num: { color: "#79c0ff" },
  comment: { color: "#8b949e", fontStyle: "italic" },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
  gqlKw: { color: "#ff7b72" },
  gqlType: { color: "#ffa657" },
  gqlField: { color: "#7ee787" },
  gqlVar: { color: "#79c0ff" },
};

interface CodeLineProps {
  readonly n: number;
  readonly children: ReactNode;
}

function CodeLine({ n, children }: CodeLineProps) {
  return (
    <div className="flex gap-4 px-5">
      <span
        className="w-6 shrink-0 text-right font-mono text-[11px] text-[#484f58] tabular-nums select-none"
        aria-hidden
      >
        {n}
      </span>
      <span className="font-mono text-[12.5px] leading-6 whitespace-pre">
        {children}
      </span>
    </div>
  );
}

interface InlineCodePanelProps {
  readonly file: string;
  readonly tag: string;
  readonly lines: ReactNode;
  readonly footer?: ReactNode;
}

function InlineCodePanel({ file, tag, lines, footer }: InlineCodePanelProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
        <span className="text-cc-ink-dim font-mono text-[11px]">{file}</span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {tag}
        </span>
      </div>
      <div className="py-3">{lines}</div>
      {footer ? (
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-3 border-t px-4 py-2 font-mono text-[10.5px]">
          {footer}
        </div>
      ) : null}
    </div>
  );
}

// -----------------------------------------------------------------------------
// Pill chip: the page's primary visual unit. Acts as anchor link.
// -----------------------------------------------------------------------------

interface ChipProps {
  readonly href: string;
  readonly children: ReactNode;
  readonly size?: "sm" | "md" | "lg";
  readonly active?: boolean;
}

function Chip({ href, children, size = "md", active = false }: ChipProps) {
  const sizing =
    size === "lg"
      ? "h-9 px-4 text-[13px]"
      : size === "sm"
        ? "h-7 px-2.5 text-[11px]"
        : "h-8 px-3 text-[12px]";
  const stateClasses = active
    ? "border-cc-accent text-cc-accent"
    : "border-cc-card-border text-cc-ink hover:border-cc-card-border-hover hover:text-cc-accent";
  return (
    <a
      href={href}
      className={[
        "inline-flex items-center justify-center rounded-full border font-mono whitespace-nowrap transition-colors",
        sizing,
        stateClasses,
      ].join(" ")}
    >
      {children}
    </a>
  );
}

// Non-anchor chip used in the closing "shipped" band.
interface ShippedChipProps {
  readonly children: ReactNode;
}

function ShippedChip({ children }: ShippedChipProps) {
  return (
    <span className="border-cc-card-border text-cc-ink hover:border-cc-card-border-hover inline-flex h-8 items-center justify-center rounded-full border px-3 font-mono text-[12px] whitespace-nowrap transition-colors">
      {children}
    </span>
  );
}

// -----------------------------------------------------------------------------
// Hero tag cluster. ~30 chips spanning the whole vocabulary of the client.
// Each chip jumps to the section that explains it.
// -----------------------------------------------------------------------------

interface HeroChip {
  readonly label: string;
  readonly href: string;
  readonly size: "sm" | "md" | "lg";
}

const HERO_CHIPS: readonly HeroChip[] = [
  { label: ".graphql", href: "#contract", size: "lg" },
  { label: ".graphqlrc.json", href: "#contract", size: "md" },
  { label: "schema.graphql", href: "#contract", size: "md" },
  { label: "operations", href: "#contract", size: "sm" },
  { label: "fragments", href: "#contract", size: "sm" },
  { label: "dotnet build", href: "#codegen", size: "lg" },
  { label: "dotnet graphql init", href: "#codegen", size: "md" },
  { label: "dotnet graphql update", href: "#codegen", size: "md" },
  { label: ".csproj", href: "#codegen", size: "sm" },
  { label: "MSBuild", href: "#codegen", size: "sm" },
  { label: "EntityStore", href: "#store", size: "lg" },
  { label: "normalized", href: "#store", size: "sm" },
  { label: "IObservable<Result>", href: "#store", size: "md" },
  { label: "Persisted", href: "#store", size: "sm" },
  { label: ".Watch(strategy)", href: "#strategies", size: "lg" },
  { label: "CacheFirst", href: "#strategies", size: "md" },
  { label: "NetworkOnly", href: "#strategies", size: "md" },
  { label: "CacheAndNetwork", href: "#strategies", size: "md" },
  { label: "@OnPriceChanged", href: "#subscriptions", size: "lg" },
  { label: "WebSocket", href: "#subscriptions", size: "sm" },
  { label: "connection_init", href: "#subscriptions", size: "md" },
  { label: "UseSubscription", href: "#subscriptions", size: "md" },
  { label: "Blazor", href: "#hosts", size: "lg" },
  { label: "Razor", href: "#hosts", size: "md" },
  { label: "MAUI", href: "#hosts", size: "md" },
  { label: "UseQuery", href: "#hosts", size: "sm" },
  { label: "UseFragment", href: "#hosts", size: "sm" },
  { label: "DI", href: "#hosts", size: "sm" },
  { label: "MIT", href: "#ship", size: "sm" },
  { label: "Hot Chocolate", href: "#ship", size: "md" },
];

interface HeroClusterProps {
  readonly active: string;
}

function HeroCluster({ active }: HeroClusterProps) {
  return (
    <div className="relative">
      <div className="flex flex-wrap items-center gap-2.5 sm:gap-3">
        {HERO_CHIPS.map((c, i) => (
          <motion.span
            key={c.label}
            initial={{ opacity: 0, y: 4 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-40px" }}
            transition={{
              duration: 0.32,
              delay: i * 0.03,
              ease: "easeOut",
            }}
            className="inline-flex"
          >
            <Chip
              href={c.href}
              size={c.size}
              active={active === c.href.slice(1)}
            >
              {c.label}
            </Chip>
          </motion.span>
        ))}
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Sticky chip rail. The page navigation, scroll-spied via IntersectionObserver.
// -----------------------------------------------------------------------------

interface RailEntry {
  readonly id: string;
  readonly label: string;
}

const RAIL: readonly RailEntry[] = [
  { id: "contract", label: "Contract" },
  { id: "codegen", label: "Codegen" },
  { id: "store", label: "Store" },
  { id: "strategies", label: "Strategies" },
  { id: "subscriptions", label: "Subscriptions" },
  { id: "hosts", label: "Hosts" },
  { id: "ship", label: "Ship" },
];

interface StickyRailProps {
  readonly active: string;
}

function StickyRail({ active }: StickyRailProps) {
  return (
    <div className="border-cc-card-border bg-cc-bg/85 sticky top-16 z-30 -mx-4 mt-4 border-y px-4 backdrop-blur sm:-mx-6 sm:px-6">
      <div className="flex [scrollbar-width:none] gap-2 overflow-x-auto py-3 [-ms-overflow-style:none] [&::-webkit-scrollbar]:hidden">
        {RAIL.map((r) => {
          const isActive = active === r.id;
          return (
            <motion.a
              key={r.id}
              href={`#${r.id}`}
              className={[
                "inline-flex h-8 shrink-0 items-center justify-center rounded-full border px-3 font-mono text-[12px] whitespace-nowrap transition-colors",
                isActive
                  ? "border-cc-accent text-cc-accent"
                  : "border-cc-card-border text-cc-ink hover:border-cc-card-border-hover hover:text-cc-accent",
              ].join(" ")}
              animate={isActive ? { opacity: [0.7, 1, 0.7] } : { opacity: 1 }}
              transition={
                isActive
                  ? { duration: 2, repeat: Infinity, ease: "easeInOut" }
                  : { duration: 0.2 }
              }
            >
              {r.label}
            </motion.a>
          );
        })}
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Section envelope. Every concept section uses the same shape: a giant mono
// chip-label, a paragraph, bullets, and one inline visual.
// -----------------------------------------------------------------------------

interface SectionProps {
  readonly id: string;
  readonly chipLabel: string;
  readonly title: string;
  readonly body: ReactNode;
  readonly bullets: readonly string[];
  readonly visual: ReactNode;
}

function ConceptSection({
  id,
  chipLabel,
  title,
  body,
  bullets,
  visual,
}: SectionProps) {
  return (
    <section id={id} className="scroll-mt-32 pt-16 pb-4 sm:pt-20">
      <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-6 sm:p-10">
        <div className="flex flex-wrap items-center gap-3">
          <span className="border-cc-accent text-cc-accent inline-flex items-center justify-center rounded-full border px-4 py-1.5 font-mono text-[15px] sm:text-[16px]">
            {chipLabel}
          </span>
          <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
            #{id}
          </span>
        </div>
        <div className="mt-6 grid items-start gap-10 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <h2 className="text-cc-heading font-heading text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              {title}
            </h2>
            <div className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
              {body}
            </div>
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
          <div className="lg:col-span-7">{visual}</div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Inline code + SVG visuals.
// -----------------------------------------------------------------------------

function GraphqlrcSnippet() {
  return (
    <InlineCodePanel
      file=".graphqlrc.json"
      tag="JSON"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>{`{`}</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.str}>&quot;schema&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;schema.graphql&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.str}>&quot;documents&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;**/*.graphql&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.str}>&quot;extensions&quot;</span>
            <span style={T.punct}>: {`{`}</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.str}>&quot;strawberryShake&quot;</span>
            <span style={T.punct}>: {`{`}</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.str}>&quot;name&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;CatalogClient&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.str}>&quot;namespace&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>&quot;Catalog.Client&quot;</span>
            <span style={T.punct}>,</span>
          </CodeLine>
          <CodeLine n={8}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.str}>&quot;url&quot;</span>
            <span style={T.punct}>: </span>
            <span style={T.str}>
              &quot;https://api.example.com/graphql&quot;
            </span>
          </CodeLine>
          <CodeLine n={9}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
          <CodeLine n={10}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
          <CodeLine n={11}>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>dotnet graphql init &amp; dotnet graphql update</span>
          <span className="text-cc-accent">CLI</span>
        </>
      }
    />
  );
}

function CsprojSnippet() {
  return (
    <InlineCodePanel
      file="Catalog.Client.csproj"
      tag="MSBuild"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>Project</span> <span style={T.attr}>Sdk</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;Microsoft.NET.Sdk&quot;</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>PropertyGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>TargetFramework</span>
            <span style={T.punct}>&gt;</span>
            <span style={T.plain}>net9.0</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>TargetFramework</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>PropertyGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>ItemGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>PackageReference</span>{" "}
            <span style={T.attr}>Include</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;StrawberryShake.Server&quot;</span>{" "}
            <span style={T.punct}>/&gt;</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>PackageReference</span>{" "}
            <span style={T.attr}>Include</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;StrawberryShake.Tools&quot;</span>{" "}
            <span style={T.attr}>PrivateAssets</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;all&quot;</span>{" "}
            <span style={T.punct}>/&gt;</span>
          </CodeLine>
          <CodeLine n={8}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>ItemGroup</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={9}>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>Project</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>dotnet tool install StrawberryShake.Tools</span>
          <span className="text-cc-accent">build-time only</span>
        </>
      }
    />
  );
}

function SubscriptionSnippet() {
  return (
    <InlineCodePanel
      file="PriceTicker.razor"
      tag="Razor"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>UseSubscription</span>{" "}
            <span style={T.param}>TResult</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;OnPriceChangedResult&quot;</span>{" "}
            <span style={T.param}>Subscribe</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>
              &quot;@(c =&gt; c.OnPriceChanged.Watch(sku))&quot;
            </span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>ChildContent</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>@</span>
            <span style={T.kw}>if</span>{" "}
            <span style={T.punct}>(context.Data is {} d)</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>{`{`}</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`      `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>span</span>
            <span style={T.punct}>&gt;@d.PriceChanged.PriceCents&lt;/</span>
            <span style={T.type}>span</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>{`}`}</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>ChildContent</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={8}>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>UseSubscription</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>WebSocket transport, store-backed re-render</span>
          <span className="text-cc-accent">@OnPriceChanged</span>
        </>
      }
    />
  );
}

function BlazorRazorSnippet() {
  return (
    <InlineCodePanel
      file="ProductCard.razor"
      tag="Blazor"
      lines={
        <>
          <CodeLine n={1}>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>UseQuery</span>{" "}
            <span style={T.param}>TResult</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;GetProductResult&quot;</span>{" "}
            <span style={T.param}>Operation</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>
              &quot;@(c =&gt; c.GetProduct.Watch(Id))&quot;
            </span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>Pending</span>
            <span style={T.punct}>&gt;Loading...&lt;/</span>
            <span style={T.type}>Pending</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={3}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>Error</span>{" "}
            <span style={T.param}>Context</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;errors&quot;</span>
            <span style={T.punct}>&gt;@errors[0].Message&lt;/</span>
            <span style={T.type}>Error</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={4}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>ChildContent</span>{" "}
            <span style={T.param}>Context</span>
            <span style={T.punct}>=</span>
            <span style={T.str}>&quot;result&quot;</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={5}>
            <span style={T.plain}>{`    `}</span>
            <span style={T.punct}>&lt;</span>
            <span style={T.type}>h2</span>
            <span style={T.punct}>&gt;@result.Data!.ProductById.Name&lt;/</span>
            <span style={T.type}>h2</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={6}>
            <span style={T.plain}>{`  `}</span>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>ChildContent</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
          <CodeLine n={7}>
            <span style={T.punct}>&lt;/</span>
            <span style={T.type}>UseQuery</span>
            <span style={T.punct}>&gt;</span>
          </CodeLine>
        </>
      }
      footer={
        <>
          <span>UseQuery, UseSubscription, UseFragment</span>
          <span className="text-cc-accent">StrawberryShake.Razor</span>
        </>
      }
    />
  );
}

/** Two queries denormalize into one EntityStore row. */
function StoreDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Two queries denormalize into one EntityStore row, watchers re-render"
    >
      <defs>
        <linearGradient id="ss9-store-line" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="#5eead4" stopOpacity="0.1" />
          <stop offset="100%" stopColor="#5eead4" stopOpacity="0.9" />
        </linearGradient>
      </defs>
      {[
        { y: 24, label: "GetProduct(id: 42)" },
        { y: 84, label: "ListProducts(first: 10)" },
      ].map((q) => (
        <g key={q.label}>
          <rect
            x="12"
            y={q.y}
            width="170"
            height="34"
            rx="6"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.16)"
          />
          <text
            x="22"
            y={q.y + 21}
            fontFamily="ui-monospace, monospace"
            fontSize="11"
            fill="#a1a3af"
          >
            {q.label}
          </text>
          <path
            d={`M 182 ${q.y + 17} C 230 ${q.y + 17}, 230 110, 270 110`}
            stroke="url(#ss9-store-line)"
            strokeWidth="1.5"
            fill="none"
          />
        </g>
      ))}
      <rect
        x="270"
        y="86"
        width="130"
        height="48"
        rx="8"
        fill="rgba(94,234,212,0.08)"
        stroke="rgba(94,234,212,0.55)"
      />
      <text
        x="335"
        y="106"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#5eead4"
      >
        EntityStore
      </text>
      <text
        x="335"
        y="122"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        Product#42 (one row)
      </text>
      {[156, 188].map((y, i) => (
        <g key={y}>
          <path
            d={`M 400 110 C 432 110, 432 ${y}, 462 ${y}`}
            stroke="rgba(94,234,212,0.45)"
            strokeWidth="1.2"
            fill="none"
          />
          <text
            x="462"
            y={y + 3}
            textAnchor="end"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            {i === 0 ? "Watch()" : "UseQuery"}
          </text>
        </g>
      ))}
      <text
        x="12"
        y="180"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.45)"
      >
        normalized, deduplicated, reactive
      </text>
    </svg>
  );
}

/** Three strategy lanes for .Watch(strategy). */
function StrategiesDiagram() {
  return (
    <svg
      viewBox="0 0 480 220"
      className="h-auto w-full"
      role="img"
      aria-label="Three fetch strategies: CacheFirst, NetworkOnly, CacheAndNetwork"
    >
      {[
        {
          y: 16,
          name: "CacheFirst",
          desc: "store hit returns first, no request",
        },
        {
          y: 84,
          name: "NetworkOnly",
          desc: "always fetch, then update the store",
        },
        {
          y: 152,
          name: "CacheAndNetwork",
          desc: "yield cache, refresh in the background",
        },
      ].map((s) => (
        <g key={s.name}>
          <rect
            x="12"
            y={s.y}
            width="180"
            height="48"
            rx="8"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(94,234,212,0.5)"
          />
          <text
            x="24"
            y={s.y + 20}
            fontFamily="var(--font-body)"
            fontSize="12"
            fill="#f5f0ea"
          >
            {s.name}
          </text>
          <text
            x="24"
            y={s.y + 38}
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill="rgba(245,241,234,0.62)"
          >
            {s.desc}
          </text>
          <path
            d={`M 192 ${s.y + 24} L 290 ${s.y + 24}`}
            stroke="rgba(94,234,212,0.35)"
            strokeWidth="1.2"
            fill="none"
          />
          <polygon
            points={`286,${s.y + 20} 298,${s.y + 24} 286,${s.y + 28}`}
            fill="rgba(94,234,212,0.55)"
          />
        </g>
      ))}
      <rect
        x="298"
        y="50"
        width="160"
        height="120"
        rx="10"
        fill="rgba(12,19,34,0.6)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x="378"
        y="76"
        textAnchor="middle"
        fontFamily="var(--font-body)"
        fontSize="12"
        fill="#f5f0ea"
      >
        client.GetProduct
      </text>
      <text
        x="378"
        y="96"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.62)"
      >
        .Watch(strategy)
      </text>
      <text
        x="378"
        y="120"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        per call,
      </text>
      <text
        x="378"
        y="134"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        or set globally
      </text>
      <text
        x="378"
        y="156"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="#5eead4"
      >
        IObservable&lt;Result&gt;
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Scroll-spy hook. IntersectionObserver tracks which section id is centered
// in the viewport, drives the active chip in the sticky rail and the hero.
// -----------------------------------------------------------------------------

const SECTION_IDS = [
  "contract",
  "codegen",
  "store",
  "strategies",
  "subscriptions",
  "hosts",
  "ship",
] as const;

function useScrollSpy(): string {
  const [active, setActive] = useState<string>("contract");
  const ratiosRef = useRef<Map<string, number>>(new Map());

  useEffect(() => {
    const map = ratiosRef.current;
    const elements = SECTION_IDS.map((id) =>
      document.getElementById(id),
    ).filter((el): el is HTMLElement => el !== null);

    if (elements.length === 0) {
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          map.set(entry.target.id, entry.intersectionRatio);
        }
        let best: { id: string; ratio: number } | null = null;
        for (const [id, ratio] of map.entries()) {
          if (ratio > 0 && (best === null || ratio > best.ratio)) {
            best = { id, ratio };
          }
        }
        if (best !== null) {
          setActive(best.id);
        }
      },
      {
        rootMargin: "-30% 0px -50% 0px",
        threshold: [0, 0.25, 0.5, 0.75, 1],
      },
    );

    for (const el of elements) {
      observer.observe(el);
    }

    return () => {
      observer.disconnect();
    };
  }, []);

  return active;
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

const SHIPPED_CHIPS = [
  "MIT",
  "MSBuild",
  "Blazor",
  "MAUI",
  "WebSocket",
  "Persisted",
  "IObservable",
  "DI",
] as const;

export function ClientPage() {
  const active = useScrollSpy();

  return (
    <>
      {/* HERO: short headline left, big tag cluster right. The cluster is the
          page navigation. Single faint dot grid behind the hero only. */}
      <section className="relative pt-12 pb-12 sm:pt-20 sm:pb-16">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 -z-10"
          style={{
            backgroundImage: HERO_DOTS,
            backgroundRepeat: "repeat",
          }}
        />
        <div className="grid items-start gap-10 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-4">
            <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
              typed GraphQL client for .NET
            </span>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              The whole client, in tags.
            </h1>
            <p className="text-cc-prose mt-6 max-w-md text-lg leading-relaxed">
              Strawberry Shake is the open-source typed GraphQL client for .NET.
              Every chip on this page is a real thing the client ships: a file
              extension, a CLI verb, a generated symbol, a runtime, a transport.
              Pick one to jump straight to it.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/strawberryshake">
                Get Started
              </SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>
          </div>
          <div className="lg:col-span-8">
            <div className="text-cc-ink-dim mb-4 flex items-center justify-between font-mono text-[11px] tracking-widest uppercase">
              <span>30 chips, 7 sections</span>
              <span>click to jump</span>
            </div>
            <HeroCluster active={active} />
          </div>
        </div>
      </section>

      {/* STICKY RAIL: category chips, scroll-spied via IntersectionObserver. */}
      <StickyRail active={active} />

      {/* SECTIONS. Each is a wide card whose label is a giant mono chip. */}
      <ConceptSection
        id="contract"
        chipLabel=".graphql"
        title="Your .graphql files are the contract."
        body={
          <>
            Queries, mutations, and subscriptions live in plain .graphql files
            next to the code that uses them. The schema lives in a
            schema.graphql file pulled from any spec-compliant server. The CLI
            reads .graphqlrc.json, then MSBuild emits the typed client class,
            the result records, the fragments, and the DI registration. The call
            sites are ordinary async C# with IntelliSense and refactor support.
          </>
        }
        bullets={[
          "Operations are valid GraphQL documents you can hand to any tool.",
          "Generated records are nullable-aware, immutable, and deconstructible.",
          "Compatible with any GraphQL spec server, not only Hot Chocolate.",
        ]}
        visual={<GraphqlrcSnippet />}
      />

      <ConceptSection
        id="codegen"
        chipLabel="dotnet build"
        title="Code generation that runs with dotnet build."
        body={
          <>
            Strawberry Shake generates code through MSBuild tasks driven by the
            dotnet graphql CLI, not through runtime IL weaving and not through
            reflection. Add the StrawberryShake.Tools NuGet, point
            .graphqlrc.json at your schema, and dotnet build emits .NET source
            for the client, operations, fragments, records, and the DI extension
            method. Stale generated code is a build error, not a runtime
            surprise.
          </>
        }
        bullets={[
          "dotnet graphql init scaffolds the config and downloads the schema in one step.",
          "dotnet graphql update keeps the local schema in sync with the server.",
          "MSBuild codegen runs in CI on every build, the same way your project compiles.",
        ]}
        visual={<CsprojSnippet />}
      />

      <ConceptSection
        id="store"
        chipLabel="EntityStore"
        title="A normalized reactive store, Relay-shaped."
        body={
          <>
            Strawberry Shake denormalizes every GraphQL result into an entity
            store keyed by type and id, the same model Relay and Apollo made the
            standard for client caches. A query that returns the same product as
            a list and as a detail shares one row. Watch a query and your
            component re-renders when that row changes, no matter which
            operation produced the update.
          </>
        }
        bullets={[
          "IObservable<Result> on every Watch(), Razor and Blazor wrappers wire it for you.",
          "Mutations write back into the store, related queries refresh automatically.",
          "Persist the store to SQLite or LiteDB and rehydrate it on next launch.",
        ]}
        visual={<StoreDiagram />}
      />

      <ConceptSection
        id="strategies"
        chipLabel=".Watch(strategy)"
        title="CacheFirst, NetworkOnly, CacheAndNetwork."
        body={
          <>
            Every operation supports three execution strategies. CacheFirst
            returns the store entry without a request when it has one.
            NetworkOnly always fetches and writes the result through the store.
            CacheAndNetwork yields the cached entry immediately and refreshes in
            the background, which is the strategy that powers fast launches and
            snappy detail pages.
          </>
        }
        bullets={[
          "Strategy is a per-Watch override on top of a per-client default.",
          "Combine with persisted state for instant cache hits on cold start.",
          "Result stream emits both cache and network values, in order.",
        ]}
        visual={<StrategiesDiagram />}
      />

      <ConceptSection
        id="subscriptions"
        chipLabel="@OnPriceChanged"
        title="Realtime over WebSocket, into the same store."
        body={
          <>
            Subscription operations look like queries: declare them in a
            .graphql file, get a typed Watch on the generated client. The
            WebSocket transport carries a connection_init payload for auth and
            reconnect handling. Pushed values write through the same entity
            store, so any open query, fragment, or Razor component sees the
            update.
          </>
        }
        bullets={[
          "Token refresh and reconnect are part of the transport, not your code.",
          "UseSubscription Razor component lifts updates straight into Blazor markup.",
          "Same Watch surface as queries, no separate event-handler pipeline.",
        ]}
        visual={<SubscriptionSnippet />}
      />

      <ConceptSection
        id="hosts"
        chipLabel="Blazor / Razor / MAUI"
        title="Razor components built on the reactive store."
        body={
          <>
            StrawberryShake.Razor ships UseQuery, UseSubscription, UseFragment,
            and a DataComponent base that bind generated operations to Blazor
            markup. Pending, Error, and ChildContent slots cover the common UI
            states, and every component reacts to the entity store, so a
            mutation in one corner of the app re-renders dependent components
            everywhere.
          </>
        }
        bullets={[
          "Server, WebAssembly, and hybrid Blazor projects all use the same client.",
          "Fragments map to typed sub-records you can reuse across components.",
          "Works inside .NET MAUI for typed GraphQL on iOS, Android, and desktop.",
        ]}
        visual={<BlazorRazorSnippet />}
      />

      {/* CLOSING TAG-BAND + CTA. Shipped chips, then the single brand-spectrum
          hairline above the final CTA. */}
      <section id="ship" className="scroll-mt-32 pt-16 pb-4 sm:pt-20">
        <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-6 sm:p-10">
          <div className="flex flex-wrap items-center gap-3">
            <span className="border-cc-accent text-cc-accent inline-flex items-center justify-center rounded-full border px-4 py-1.5 font-mono text-[15px] sm:text-[16px]">
              shipped
            </span>
            <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
              #ship
            </span>
          </div>
          <p className="text-cc-prose mt-6 max-w-2xl text-base leading-relaxed sm:text-lg">
            Eight chips that say what the package is the day you take it in.
            MIT-licensed, MSBuild-driven, Blazor and MAUI ready, WebSocket
            transport, persisted operations, IObservable on every Watch, DI
            registration emitted for you.
          </p>
          <div className="mt-8 flex flex-wrap gap-2.5">
            {SHIPPED_CHIPS.map((c) => (
              <ShippedChip key={c}>{c}</ShippedChip>
            ))}
          </div>
        </div>
      </section>

      <section className="relative mt-16 py-20 sm:py-24">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
            Get started
          </span>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            A typed GraphQL client your .NET team can actually own.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            A few .graphql files, a .graphqlrc.json, and a NuGet reference. The
            client, the records, the store, and the DI wiring are emitted for
            you at build time, and the runtime is the .NET you already ship.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/strawberryshake">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
