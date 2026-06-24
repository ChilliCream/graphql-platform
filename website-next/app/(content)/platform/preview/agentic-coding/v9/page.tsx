import type { Metadata } from "next";
import Link from "next/link";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * Preview variant (v9) of the Agentic coding page. Concept seed: confetti
 * scatter, reframed as a "confetti registry" where each tiny dot is one
 * published operation an agent can ground itself in. The hero is a centered,
 * single-column stage holding a deterministic constellation of operations;
 * downstream sections inherit the dot motif as dividers, progress beads, and a
 * convergent MCP hub.
 *
 * Scene accent is cc-accent teal (#5eead4). Coral (#f0786a) is reserved for
 * the lone destructive marker. The brand spectrum (cyan, violet, coral)
 * appears exactly once in the hero lead phrase "governed feedback loop".
 *
 * Motion is restrained and time-driven only: a slow opacity pulse on three
 * picked "agent reach" dots and a 4s ring-expand halo on the single coral
 * destructive dot. Both are pure CSS keyframes gated by
 * prefers-reduced-motion, so this file stays a Server Component and can export
 * `metadata` (required for the static SEO / robots no-index export below).
 */

export const metadata: Metadata = {
  title: "Agentic Coding: A Confetti Registry of Governed Tools",
  description:
    "Agentic coding on GraphQL MCP: every published operation is a tool your coding agent can hold, authored in repo, validated in CI, staged, and traced per call.",
  keywords: [
    "agentic coding feedback loop",
    "GraphQL MCP server",
    "operations as MCP tools",
    "agent tool lifecycle governance",
    "MCP behavior annotations",
    "idempotent destructive openWorld hints",
    "client registry grounding for agents",
    "skillz agent conventions",
    "validate MCP tools in CI",
    ".NET GraphQL agents",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Every Operation Is a Tool Your Agent Can Hold",
    description:
      "Your GraphQL server is already an MCP server. Published operations become a confetti registry of governed tools, authored in repo, validated in CI, staged, and traced in production.",
  },
};

const ACCENT = "#5eead4";
const CYAN = "#16b9e4";
const VIOLET = "#7c92c6";
const CORAL = "#f0786a";

/** One spectrum gradient is permitted per screen; used once, in the hero lead. */
const SPECTRUM = "linear-gradient(100deg,#16b9e4 0%,#7c92c6 50%,#f0786a 100%)";

/* ------------------------------------------------------------------ *
 * Scoped animation styles (server-rendered, reduced-motion aware)
 * ------------------------------------------------------------------ */

const ANIMATION_CSS = `
@keyframes acv9-breath {
  0%, 100% { opacity: 0.4; }
  50% { opacity: 0.9; }
}
@keyframes acv9-halo {
  0% { transform: scale(1); opacity: 0.55; }
  100% { transform: scale(3.2); opacity: 0; }
}
.acv9-breath {
  transform-origin: center;
  animation: acv9-breath 3s ease-in-out infinite;
}
.acv9-halo {
  transform-origin: center;
  animation: acv9-halo 4s ease-out infinite;
}
@media (prefers-reduced-motion: reduce) {
  .acv9-breath {
    animation: none !important;
    opacity: 0.75 !important;
  }
  .acv9-halo {
    animation: none !important;
    opacity: 0 !important;
  }
}
`;

/* ------------------------------------------------------------------ *
 * Deterministic dot constellation
 * ------------------------------------------------------------------ */

/** Tiny seeded LCG so positions are stable across SSR and CSR. */
function makeRng(seed: number) {
  let s = seed >>> 0;
  return function next() {
    s = (s * 1664525 + 1013904223) >>> 0;
    return s / 0xffffffff;
  };
}

type DotKind = "accent" | "cyan" | "violet" | "coral";

interface Dot {
  readonly cx: number;
  readonly cy: number;
  readonly r: number;
  readonly kind: DotKind;
  readonly opacity: number;
  readonly id: number;
}

/**
 * Build a deterministic scatter at module scope so the SVG is stable on the
 * server and the client. Roughly 70% accent, 18% cyan, 9% violet, 3% coral.
 */
function buildConstellation(
  seed: number,
  count: number,
  exactlyOneCoral: boolean,
): readonly Dot[] {
  const rng = makeRng(seed);
  const dots: Dot[] = [];
  let coralPlaced = false;

  for (let i = 0; i < count; i++) {
    const cx = rng() * 1000;
    const cy = rng() * 480;
    const r = 1 + rng() * 2;
    const roll = rng();

    let kind: DotKind;
    if (exactlyOneCoral && !coralPlaced && i === Math.floor(count * 0.62)) {
      kind = "coral";
      coralPlaced = true;
    } else if (roll < 0.7) {
      kind = "accent";
    } else if (roll < 0.88) {
      kind = "cyan";
    } else if (roll < 0.97) {
      kind = "violet";
    } else {
      kind = "accent";
    }

    const opacity =
      kind === "coral"
        ? 0.95
        : kind === "accent"
          ? 0.35 + rng() * 0.3
          : kind === "cyan"
            ? 0.25 + rng() * 0.25
            : 0.25 + rng() * 0.2;

    dots.push({ cx, cy, r, kind, opacity, id: i });
  }

  // If we somehow missed the coral, append exactly one in a predictable spot.
  if (exactlyOneCoral && !coralPlaced) {
    dots.push({
      cx: 720,
      cy: 220,
      r: 2.4,
      kind: "coral",
      opacity: 0.95,
      id: count,
    });
  }

  return dots;
}

const HERO_DOTS = buildConstellation(1337, 120, true);
const CTA_DOTS = buildConstellation(909, 60, false);

function dotFill(kind: DotKind): string {
  switch (kind) {
    case "accent":
      return ACCENT;
    case "cyan":
      return CYAN;
    case "violet":
      return VIOLET;
    case "coral":
      return CORAL;
  }
}

interface DotFieldProps {
  readonly dots: readonly Dot[];
  readonly breathIds?: readonly number[];
  readonly className?: string;
  readonly ariaLabel: string;
  readonly showCoralHalo?: boolean;
}

function DotField({
  dots,
  breathIds = [],
  className,
  ariaLabel,
  showCoralHalo = false,
}: DotFieldProps) {
  const breathSet = new Set(breathIds);
  return (
    <svg
      viewBox="0 0 1000 480"
      preserveAspectRatio="xMidYMid slice"
      className={className}
      role="img"
      aria-label={ariaLabel}
    >
      <defs>
        <radialGradient id="acv9-fade" cx="50%" cy="50%" r="58%">
          <stop offset="0%" stopColor="#fff" stopOpacity="1" />
          <stop offset="70%" stopColor="#fff" stopOpacity="0.85" />
          <stop offset="100%" stopColor="#000" stopOpacity="0" />
        </radialGradient>
        <mask id="acv9-fade-mask">
          <rect x="0" y="0" width="1000" height="480" fill="url(#acv9-fade)" />
        </mask>
      </defs>
      <g mask="url(#acv9-fade-mask)">
        {dots.map((d) => {
          const fill = dotFill(d.kind);
          const breath = breathSet.has(d.id);
          return (
            <g key={d.id}>
              {showCoralHalo && d.kind === "coral" && (
                <circle
                  cx={d.cx}
                  cy={d.cy}
                  r={d.r}
                  fill="none"
                  stroke={CORAL}
                  strokeWidth={0.6}
                  className="acv9-halo"
                />
              )}
              <circle
                cx={d.cx}
                cy={d.cy}
                r={d.r}
                fill={fill}
                opacity={d.opacity}
                className={breath ? "acv9-breath" : undefined}
                style={
                  breath
                    ? ({
                        animationDelay: `${(d.id % 3) * 0.7}s`,
                      } as CSSProperties)
                    : undefined
                }
              />
            </g>
          );
        })}
      </g>
    </svg>
  );
}

/* ------------------------------------------------------------------ *
 * Small shared parts
 * ------------------------------------------------------------------ */

interface DotClusterProps {
  readonly count?: number;
  readonly className?: string;
}

/** Tiny 3-dot cluster used as eyebrow prefix and section divider. */
function DotCluster({ count = 3, className }: DotClusterProps) {
  return (
    <span
      className={`inline-flex items-center gap-1 ${className ?? ""}`}
      aria-hidden="true"
    >
      {Array.from({ length: count }).map((_, i) => (
        <span
          key={i}
          className="inline-block h-1 w-1 rounded-full"
          style={{ backgroundColor: ACCENT, opacity: 0.55 + (i % 3) * 0.15 }}
        />
      ))}
    </span>
  );
}

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label flex items-center gap-2 font-mono text-[0.7rem] tracking-[0.22em] uppercase">
      <DotCluster />
      <span>{children}</span>
    </p>
  );
}

interface SectionHeadingProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

function SectionHeading({ eyebrow, title, children }: SectionHeadingProps) {
  return (
    <div className="max-w-2xl">
      <Eyebrow>{eyebrow}</Eyebrow>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.08] font-semibold text-balance">
        {title}
      </h2>
      {children ? (
        <div className="text-cc-ink mt-5 space-y-4 text-base/relaxed text-pretty">
          {children}
        </div>
      ) : null}
    </div>
  );
}

type Hint = "idempotent" | "read-only" | "open-world" | "destructive";

interface HintBadgeProps {
  readonly hint: Hint;
}

function HintBadge({ hint }: HintBadgeProps) {
  const label =
    hint === "idempotent"
      ? "idempotentHint"
      : hint === "read-only"
        ? "readOnlyHint"
        : hint === "open-world"
          ? "openWorldHint"
          : "destructiveHint";

  if (hint === "destructive") {
    return (
      <span
        className="rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.06em] whitespace-nowrap"
        style={{
          color: CORAL,
          borderColor: "rgba(240,120,106,0.45)",
          backgroundColor: "rgba(240,120,106,0.08)",
        }}
      >
        {label}
      </span>
    );
  }

  return (
    <span className="border-cc-card-border text-cc-ink-dim bg-cc-surface rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.06em] whitespace-nowrap">
      {label}
    </span>
  );
}

/* ------------------------------------------------------------------ *
 * Section 2: dot-grid artifact resolving into named tool chips
 * ------------------------------------------------------------------ */

interface ResolvedChipProps {
  readonly name: string;
  readonly hint: Hint;
  readonly x: number;
  readonly y: number;
}

function ResolvedChip({ name, hint, x, y }: ResolvedChipProps) {
  const isDestructive = hint === "destructive";
  return (
    <div
      className="absolute"
      style={{
        left: `${x}%`,
        top: `${y}%`,
        transform: "translate(-50%, -50%)",
      }}
    >
      <div
        className="bg-cc-surface flex items-center gap-2 rounded-full border px-2.5 py-1 shadow-lg backdrop-blur-sm"
        style={{
          borderColor: isDestructive
            ? "rgba(240,120,106,0.45)"
            : "rgba(94,234,212,0.4)",
        }}
      >
        <span
          className="inline-block h-1.5 w-1.5 rounded-full"
          style={{ backgroundColor: isDestructive ? CORAL : ACCENT }}
        />
        <span
          className="font-mono text-[0.62rem]"
          style={{ color: isDestructive ? CORAL : "#cbd5e1" }}
        >
          {name}
        </span>
        <HintBadge hint={hint} />
      </div>
    </div>
  );
}

function ResolveArtifact() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-3xl border">
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-b px-4 py-2.5">
        <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-wide">
          client registry · 38 published ops
        </span>
        <span
          className="font-mono text-[0.55rem] tracking-[0.1em] uppercase"
          style={{ color: ACCENT }}
        >
          grounded
        </span>
      </div>
      <div className="relative h-[280px] sm:h-[320px]">
        <DotField
          dots={buildConstellation(2024, 80, true)}
          ariaLabel="A denser dot field of published operations, three resolving into named tool chips."
          className="absolute inset-0 h-full w-full"
        />
        <ResolvedChip name="getProduct" hint="read-only" x={26} y={32} />
        <ResolvedChip name="tagProduct" hint="idempotent" x={56} y={62} />
        <ResolvedChip name="deleteReview" hint="destructive" x={78} y={28} />
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Section 3: full-bleed convergence band
 * ------------------------------------------------------------------ */

function ConvergenceBand() {
  // Dense scatter pulled toward the center node.
  const rng = makeRng(4242);
  const cx = 500;
  const cy = 200;
  const dots: { x: number; y: number; r: number; o: number }[] = [];
  for (let i = 0; i < 220; i++) {
    // Sample in polar so density grows toward the center.
    const t = rng();
    const radial = Math.pow(t, 1.6) * 380;
    const angle = rng() * Math.PI * 2;
    const x = cx + Math.cos(angle) * radial;
    const y = cy + Math.sin(angle) * radial * 0.55;
    const r = 1 + rng() * 1.6;
    const o = 0.25 + (1 - t) * 0.55;
    dots.push({ x, y, r, o });
  }

  return (
    <div className="border-cc-card-border bg-cc-surface relative overflow-hidden border-y">
      <svg
        viewBox="0 0 1000 400"
        preserveAspectRatio="xMidYMid slice"
        className="h-[360px] w-full sm:h-[420px]"
        role="img"
        aria-label="A dense scatter of published operations converging into one /graphql/mcp hub."
      >
        <defs>
          <radialGradient id="acv9-conv-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor="rgba(94,234,212,0.35)" />
            <stop offset="100%" stopColor="rgba(94,234,212,0)" />
          </radialGradient>
        </defs>
        {dots.map((d, i) => (
          <circle
            key={i}
            cx={d.x}
            cy={d.y}
            r={d.r}
            fill={ACCENT}
            opacity={d.o}
          />
        ))}
        <circle cx={cx} cy={cy} r={80} fill="url(#acv9-conv-glow)" />
        <circle
          cx={cx}
          cy={cy}
          r={38}
          fill="#0c1322"
          stroke={ACCENT}
          strokeWidth={1.5}
        />
        <text
          x={cx}
          y={cy - 2}
          textAnchor="middle"
          className="font-mono"
          fontSize="10"
          fill="#f5f0ea"
        >
          /graphql
        </text>
        <text
          x={cx}
          y={cy + 11}
          textAnchor="middle"
          className="font-mono"
          fontSize="10"
          fill={ACCENT}
        >
          /mcp
        </text>
      </svg>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Section 4: lifecycle dotted beads
 * ------------------------------------------------------------------ */

interface LifecycleStep {
  readonly key: string;
  readonly title: string;
  readonly note: string;
}

const LIFECYCLE: readonly LifecycleStep[] = [
  { key: "author", title: "Author", note: "in repo · .graphql + settings" },
  { key: "validate", title: "Validate", note: "in CI · nitro mcp validate" },
  { key: "stage", title: "Stage", note: "promote with approval gate" },
  { key: "trace", title: "Trace", note: "per-tool p95 in Nitro" },
];

interface ProgressBeadsProps {
  readonly activeIndex: number;
}

function ProgressBeads({ activeIndex }: ProgressBeadsProps) {
  return (
    <span className="inline-flex items-center gap-1.5" aria-hidden="true">
      {LIFECYCLE.map((_, i) => (
        <span
          key={i}
          className="inline-block h-1.5 w-1.5 rounded-full"
          style={{
            backgroundColor: ACCENT,
            opacity: i === activeIndex ? 0.95 : 0.25,
          }}
        />
      ))}
    </span>
  );
}

function LifecycleStrip() {
  return (
    <ol className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
      {LIFECYCLE.map((item, index) => (
        <li
          key={item.key}
          className="border-cc-card-border bg-cc-card-bg relative rounded-2xl border px-5 py-5 backdrop-blur-sm"
        >
          <div className="flex items-center justify-between">
            <ProgressBeads activeIndex={index} />
            <span
              className="font-mono text-[0.62rem] tracking-[0.14em] uppercase"
              style={{ color: ACCENT }}
            >
              0{index + 1}
            </span>
          </div>
          <p className="font-heading text-cc-heading text-h6 mt-3 font-semibold">
            {item.title}
          </p>
          <p className="text-cc-ink-dim mt-1.5 font-mono text-[0.68rem] leading-relaxed">
            {item.note}
          </p>
        </li>
      ))}
    </ol>
  );
}

/* ------------------------------------------------------------------ *
 * Section 5: behavior hint reference stripe
 * ------------------------------------------------------------------ */

interface HintRowProps {
  readonly hint: Hint;
  readonly description: string;
}

const HINTS: readonly HintRowProps[] = [
  {
    hint: "read-only",
    description: "A safe read. No state changes, no side effects.",
  },
  {
    hint: "idempotent",
    description: "Same input, same outcome. Repeatable without harm.",
  },
  {
    hint: "open-world",
    description: "Calls an external system the schema does not own.",
  },
  {
    hint: "destructive",
    description: "Removes or overwrites data. Approval gate required.",
  },
];

function HintReferenceStripe() {
  return (
    <ul className="border-cc-card-border bg-cc-card-bg divide-cc-card-border divide-y overflow-hidden rounded-2xl border">
      {HINTS.map((row) => {
        const isDestructive = row.hint === "destructive";
        return (
          <li
            key={row.hint}
            className="flex flex-col gap-2 px-5 py-4 sm:flex-row sm:items-center sm:justify-between sm:gap-6"
            style={
              isDestructive
                ? { backgroundColor: "rgba(240,120,106,0.05)" }
                : undefined
            }
          >
            <div className="flex items-center gap-3">
              <HintBadge hint={row.hint} />
            </div>
            <p
              className="text-sm/relaxed"
              style={{ color: isDestructive ? "#f5c4bd" : undefined }}
            >
              <span className={isDestructive ? "" : "text-cc-ink"}>
                {row.description}
              </span>
            </p>
          </li>
        );
      })}
    </ul>
  );
}

/* ------------------------------------------------------------------ *
 * Section 6: skillz tiles
 * ------------------------------------------------------------------ */

interface SkillTileProps {
  readonly name: string;
  readonly body: string;
}

const SKILL_TILES: readonly SkillTileProps[] = [
  {
    name: "pagination.SKILL.md",
    body: "Always page list fields with the registry connection contract.",
  },
  {
    name: "errors.SKILL.md",
    body: "Model failures as typed union results, never thrown exceptions.",
  },
  {
    name: "naming.SKILL.md",
    body: "Mutation inputs and payloads follow the team naming rules.",
  },
  {
    name: "auth.SKILL.md",
    body: "Gate fields with the shared policy directives, not ad-hoc checks.",
  },
];

function SkillTile({ name, body }: SkillTileProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-2xl border p-5 backdrop-blur-sm transition-transform duration-200 hover:-translate-y-1">
      <div className="flex items-center justify-between">
        <p className="font-mono text-[0.72rem]" style={{ color: ACCENT }}>
          {name}
        </p>
        <DotCluster count={4} />
      </div>
      <p className="text-cc-ink-dim mt-3 text-sm/relaxed">{body}</p>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Section divider: tiny dot cluster centered between sections
 * ------------------------------------------------------------------ */

function SectionDivider() {
  return (
    <div className="flex justify-center py-6" aria-hidden="true">
      <span className="inline-flex items-center gap-1.5">
        {Array.from({ length: 5 }).map((_, i) => (
          <span
            key={i}
            className="inline-block h-1 w-1 rounded-full"
            style={{
              backgroundColor: ACCENT,
              opacity: 0.25 + Math.abs(2 - i) * 0.15,
            }}
          />
        ))}
      </span>
    </div>
  );
}

/* ------------------------------------------------------------------ *
 * Page
 * ------------------------------------------------------------------ */

const HONESTY_POINTS: readonly string[] = [
  "Tools and prompts are authored in the repo as reviewed code, not minted at runtime.",
  "nitro mcp validate runs in CI, so a broken tool collection never reaches a stage.",
  "Behavior is declared with idempotentHint, destructiveHint, and openWorldHint.",
  "An edit is checked against published operations; risky changes read “published clients affected.”",
  "Every tool call is traced in Nitro with p95 latency, error rate, and impact.",
];

// Three picked "agent reach" dots get the slow opacity breath in the hero.
const HERO_BREATH_IDS: readonly number[] = [11, 47, 92];

export default function AgenticCodingPreviewV9() {
  return (
    <>
      <style>{ANIMATION_CSS}</style>

      {/* ---------------------------------------------------------- *
       * Hero: centered, full-bleed dot constellation as the artifact
       * ---------------------------------------------------------- */}
      <section className="relative -mx-4 overflow-hidden sm:-mx-6 lg:-mx-8">
        <div className="pointer-events-none absolute inset-0">
          <DotField
            dots={HERO_DOTS}
            breathIds={HERO_BREATH_IDS}
            ariaLabel="A scattered constellation of published GraphQL operations, each one a tool the agent can call. One coral dot marks the single destructive operation."
            className="h-full w-full"
            showCoralHalo
          />
        </div>

        <div className="relative mx-auto max-w-4xl px-4 py-20 text-center sm:px-6 sm:py-24 lg:px-8 lg:py-28">
          <span
            className="inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-[0.62rem] tracking-[0.16em] uppercase"
            style={{
              color: ACCENT,
              borderColor: "rgba(94,234,212,0.4)",
              backgroundColor: "rgba(94,234,212,0.07)",
            }}
          >
            <DotCluster />
            Agentic coding · GraphQL MCP
          </span>

          <h1 className="font-heading text-cc-heading mt-6 text-4xl leading-[1.04] font-semibold tracking-tight text-balance sm:text-5xl lg:text-6xl">
            Every operation is a tool your agent can hold.
          </h1>

          <p className="lead text-cc-ink-dim mx-auto mt-6 max-w-2xl text-pretty">
            Stop letting agents guess at your graph. Ground them in the
            operations your clients already use, gate the risky calls, and turn
            every fast edit into a{" "}
            <span
              className="bg-clip-text font-medium text-transparent"
              style={{ backgroundImage: SPECTRUM }}
            >
              governed feedback loop
            </span>
            .
          </p>

          <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base/relaxed text-pretty">
            Your GraphQL server is already an MCP server. Each published
            operation is one dot in the registry, each dot a tool the agent can
            call with real product context, authored, validated, and traced
            before it ever touches production.
          </p>

          <div className="mt-8 flex flex-wrap items-center justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/apis/client-registry">
              Read the Docs
            </OutlineButton>
          </div>

          {/* Legend chip strip below the constellation. */}
          <div className="mt-10 flex flex-wrap items-center justify-center gap-2">
            <HintBadge hint="read-only" />
            <HintBadge hint="idempotent" />
            <HintBadge hint="open-world" />
            <HintBadge hint="destructive" />
          </div>
          <p className="text-cc-ink-faint mt-4 font-mono text-[0.62rem] tracking-wide">
            120 dots · one coral · the catalog your agent can hold
          </p>
        </div>
      </section>

      <SectionDivider />

      {/* ---------------------------------------------------------- *
       * Grounding strip: prose left, dot-grid resolving to chips right
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="mx-auto grid max-w-5xl items-start gap-10 lg:grid-cols-[1fr_1.1fr] lg:gap-14">
          <SectionHeading
            eyebrow="Grounding"
            title="Scattered ops, one published catalog."
          >
            <p>
              A coding agent that does not know your graph invents fields and
              writes queries no client would ship. The schema and client
              registry change that: your published operations become a catalog
              of callable tools, each one a real, reviewed shape your product
              already depends on.
            </p>
            <p>
              MCP exposes those operations as tools and prompts with behavior
              annotations, so the agent can tell a safe read from a write before
              it acts, and you keep authority over what it is allowed to do.
            </p>
          </SectionHeading>

          <ResolveArtifact />
        </div>
      </section>

      <SectionDivider />

      {/* ---------------------------------------------------------- *
       * Convergence: full-bleed band pulling dots into /graphql/mcp
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t">
        <div className="mx-auto max-w-5xl px-0 pt-16">
          <SectionHeading
            eyebrow="One hub"
            title="Scattered operations converge into one MCP endpoint."
          />
        </div>
        <div className="mt-10">
          <ConvergenceBand />
        </div>
        <div className="mx-auto grid max-w-5xl grid-cols-1 gap-6 py-12 sm:grid-cols-3">
          <div>
            <p
              className="font-mono text-[0.62rem] tracking-[0.16em] uppercase"
              style={{ color: ACCENT }}
            >
              Hub, not mesh
            </p>
            <p className="text-cc-ink-dim mt-2 text-sm/relaxed">
              Every published operation flows inward to a single{" "}
              <code className="text-cc-info">/graphql/mcp</code> hub over
              Streamable HTTP. No second surface to secure.
            </p>
          </div>
          <div>
            <p
              className="font-mono text-[0.62rem] tracking-[0.16em] uppercase"
              style={{ color: ACCENT }}
            >
              Schema-typed params
            </p>
            <p className="text-cc-ink-dim mt-2 text-sm/relaxed">
              Because the schema is typed and introspectable, each tool carries
              an accurate parameter contract, so agents make fewer malformed
              calls.
            </p>
          </div>
          <div>
            <p
              className="font-mono text-[0.62rem] tracking-[0.16em] uppercase"
              style={{ color: ACCENT }}
            >
              Behavior annotations
            </p>
            <p className="text-cc-ink-dim mt-2 text-sm/relaxed">
              Hints travel with the tool definition, so destructive ones stay
              clearly marked across every agent surface.
            </p>
          </div>
        </div>
      </section>

      <SectionDivider />

      {/* ---------------------------------------------------------- *
       * Lifecycle dotted beads
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="mx-auto max-w-5xl">
          <SectionHeading
            eyebrow="Governed lifecycle"
            title="Author. Validate. Stage. Trace."
          >
            <p>
              The point is not that we have MCP, it is that every agent tool
              moves through a lifecycle you control. Tools start as reviewed
              code, get validated before they ship, and are promoted through
              stages with approval gates, then observed in production.
            </p>
          </SectionHeading>
          <div className="mt-10">
            <LifecycleStrip />
          </div>
        </div>
      </section>

      <SectionDivider />

      {/* ---------------------------------------------------------- *
       * Behavior hints inline reference
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="mx-auto max-w-5xl">
          <SectionHeading
            eyebrow="Behavior hints"
            title="Four labels the agent reads before it acts."
          >
            <p>
              Hints are declared once with the schema and carried with every
              tool definition. The destructive row stands alone, because it is
              the only one that has to pass an approval gate.
            </p>
          </SectionHeading>
          <div className="mt-10">
            <HintReferenceStripe />
          </div>
        </div>
      </section>

      <SectionDivider />

      {/* ---------------------------------------------------------- *
       * skillz tiles
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="mx-auto max-w-5xl">
          <SectionHeading
            eyebrow="Conventions"
            title="Teach every agent your conventions once."
          >
            <p>
              skillz packages your team&rsquo;s GraphQL conventions as portable{" "}
              <code className="text-cc-info">SKILL.md</code> files, installable
              across the agents your team already uses, so the next pull request
              looks like your codebase, not a generic one.
            </p>
          </SectionHeading>
          <div className="mt-10 grid grid-cols-1 gap-4 sm:grid-cols-2">
            {SKILL_TILES.map((tile) => (
              <SkillTile key={tile.name} name={tile.name} body={tile.body} />
            ))}
          </div>
        </div>
      </section>

      <SectionDivider />

      {/* ---------------------------------------------------------- *
       * Honesty beat
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border border-t py-16">
        <div className="mx-auto max-w-3xl text-center">
          <Eyebrow>What we actually claim</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 leading-[1.08] font-semibold text-balance">
            What the registries can prove.
          </h2>
          <p className="text-cc-ink mt-5 text-base/relaxed text-pretty">
            Honesty is the differentiator. We do not promise to name every
            published client that breaks or to mint safe tools at runtime. We
            promise a governed, observed path: authored in repo, validated in
            CI, staged with a gate, and traced in production.
          </p>
        </div>
        <ul className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-3xl space-y-4 rounded-3xl border p-7 backdrop-blur-sm">
          {HONESTY_POINTS.map((point) => (
            <li key={point} className="flex items-start gap-3">
              <span className="mt-0.5 shrink-0" style={{ color: ACCENT }}>
                <CheckIcon />
              </span>
              <span className="text-cc-ink text-sm/relaxed text-pretty">
                {point}
              </span>
            </li>
          ))}
        </ul>
      </section>

      <SectionDivider />

      {/* ---------------------------------------------------------- *
       * Closing CTA: sparser dot field
       * ---------------------------------------------------------- */}
      <section className="border-cc-card-border relative -mx-4 overflow-hidden border-t sm:-mx-6 lg:-mx-8">
        <div className="pointer-events-none absolute inset-0">
          <DotField
            dots={CTA_DOTS}
            ariaLabel="A sparser dot field framing the closing call to action."
            className="h-full w-full"
          />
        </div>
        <div className="relative mx-auto max-w-3xl px-4 py-20 text-center sm:px-6 lg:px-8">
          <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold text-balance">
            Put the catalog in your agent&rsquo;s hands.
          </h2>
          <p className="text-cc-ink-dim mx-auto mt-5 max-w-2xl text-base/relaxed">
            Expose your operations as governed tools, ground them in real field
            demand, and trace every call in the platform your team already runs.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-4">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/apis/client-registry">
              Read the Docs
            </OutlineButton>
          </div>
          <p className="text-cc-ink-dim mt-6 text-sm">
            Or explore the{" "}
            <Link
              href="/docs/nitro/apis/client-registry"
              className="text-cc-info hover:text-cc-heading transition-colors"
            >
              client registry
            </Link>{" "}
            and the wider{" "}
            <Link
              href="/platform"
              className="text-cc-info hover:text-cc-heading transition-colors"
            >
              platform
            </Link>
            .
          </p>
        </div>
      </section>
    </>
  );
}
