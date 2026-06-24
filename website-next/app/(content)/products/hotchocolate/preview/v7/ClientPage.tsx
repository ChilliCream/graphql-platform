"use client";

import {
  animate,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
  type MotionValue,
} from "motion/react";
import { useEffect, useRef, type ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

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
}

function IndexTag({ value }: IndexTagProps) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim inline-flex h-6 items-center justify-center rounded-full border px-2 font-mono text-[11px] tabular-nums">
      {value}
    </span>
  );
}

// -----------------------------------------------------------------------------
// The C# tokens used by the left lane of the centerpiece. Each entry becomes a
// motion span that fades in as the scroll timeline advances.
// -----------------------------------------------------------------------------

interface CsToken {
  readonly t: string;
  readonly kind?: "kw" | "type" | "attr" | "fn" | "param" | "punct" | "plain";
  readonly nl?: boolean;
  readonly indent?: number;
}

const CS_TOKENS: readonly CsToken[] = [
  { t: "[", kind: "punct" },
  { t: "QueryType", kind: "attr" },
  { t: "]", kind: "punct", nl: true },
  { t: "public partial class ", kind: "kw" },
  { t: "Query", kind: "type", nl: true },
  { t: "{", kind: "punct", nl: true },
  { t: "public static ", kind: "kw", indent: 1 },
  { t: "Task", kind: "type" },
  { t: "<", kind: "punct" },
  { t: "Product", kind: "type" },
  { t: "?> ", kind: "punct" },
  { t: "GetProductByIdAsync", kind: "fn" },
  { t: "(", kind: "punct", nl: true },
  { t: "Guid", kind: "type", indent: 2 },
  { t: " id, ", kind: "param", nl: true },
  { t: "IProductByIdDataLoader", kind: "type", indent: 2 },
  { t: " loader, ", kind: "param", nl: true },
  { t: "CancellationToken", kind: "type", indent: 2 },
  { t: " ct) =>", kind: "param", nl: true },
  { t: "await ", kind: "kw", indent: 2 },
  { t: "loader.", kind: "param" },
  { t: "LoadAsync", kind: "fn" },
  { t: "(id, ct);", kind: "punct", nl: true },
  { t: "}", kind: "punct" },
];

const KIND_COLOR: Record<NonNullable<CsToken["kind"]>, string> = {
  kw: "#ff7b72",
  type: "#ffa657",
  attr: "#d2a8ff",
  fn: "#d2a8ff",
  param: "#79c0ff",
  punct: "#c9d1d9",
  plain: "#c9d1d9",
};

// SDL lines for the right lane. Each line draws in as the matching particle
// lands on the emit stage.
const SDL_LINES: readonly string[] = [
  "type Query {",
  "  productById(",
  "    id: UUID!",
  "  ): Product",
  "}",
  "",
  "type Product {",
  "  id: UUID!",
  "  name: String!",
  "  price: Decimal!",
  "}",
];

// -----------------------------------------------------------------------------
// HERO
// -----------------------------------------------------------------------------

function StaticPipelinePreview() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-5">
      <div className="text-cc-ink-dim mb-4 flex items-center justify-between font-mono text-[10.5px] tracking-widest uppercase">
        <span>Catalog/Query.cs</span>
        <span className="text-cc-accent">Roslyn source generator</span>
      </div>
      <div className="grid grid-cols-3 items-stretch gap-3">
        <div className="bg-cc-code-bg border-cc-card-border rounded-md border p-3 font-mono text-[10.5px] leading-5">
          <div>
            <span style={{ color: KIND_COLOR.punct }}>[</span>
            <span style={{ color: KIND_COLOR.attr }}>QueryType</span>
            <span style={{ color: KIND_COLOR.punct }}>]</span>
          </div>
          <div>
            <span style={{ color: KIND_COLOR.kw }}>public partial class </span>
            <span style={{ color: KIND_COLOR.type }}>Query</span>
          </div>
          <div style={{ color: KIND_COLOR.punct }}>{"{ ... }"}</div>
        </div>
        <div className="flex flex-col items-center justify-center gap-2">
          {["parse", "analyze", "emit"].map((s) => (
            <div
              key={s}
              className="border-cc-card-border text-cc-ink-dim w-full rounded-md border px-2 py-1 text-center font-mono text-[10px] tracking-widest uppercase"
            >
              {s}
            </div>
          ))}
        </div>
        <div className="bg-cc-code-bg border-cc-card-border rounded-md border p-3 font-mono text-[10.5px] leading-5 text-[#c9d1d9]">
          <div style={{ color: "#79c0ff" }}>type Query {"{"}</div>
          <div className="pl-3">productById: Product</div>
          <div style={{ color: "#79c0ff" }}>{"}"}</div>
        </div>
      </div>
      <div className="text-cc-ink-dim mt-4 font-mono text-[10.5px] tracking-widest uppercase">
        scroll to watch one build step
      </div>
    </div>
  );
}

function Hero() {
  return (
    <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-6">
          <Eyebrow>GraphQL server for .NET</Eyebrow>
          <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
            C# is the schema.
          </h1>
          <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
            Hot Chocolate is the open-source GraphQL server for .NET. Annotate a
            partial class, write idiomatic C# resolvers, and a Roslyn source
            generator turns your code into the schema, the resolver pipeline,
            and DataLoader infrastructure in a single build step. Scroll down
            and watch it happen.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              Star on GitHub
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
              <dd className="text-cc-ink mt-1 text-sm">GraphQL 2025</dd>
            </div>
          </dl>
        </div>
        <div className="lg:col-span-6">
          <StaticPipelinePreview />
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// CENTERPIECE: scroll-locked three-lane source generator pipeline
// -----------------------------------------------------------------------------

interface LaneFrameProps {
  readonly label: string;
  readonly children: ReactNode;
}

function LaneFrame({ label, children }: LaneFrameProps) {
  return (
    <div className="border-cc-card-border bg-cc-code-bg flex h-full min-h-[420px] flex-col rounded-xl border p-4">
      <div className="text-cc-ink-dim mb-3 font-mono text-[10.5px] tracking-widest uppercase">
        {label}
      </div>
      <div className="flex-1 overflow-hidden">{children}</div>
    </div>
  );
}

interface CsLaneProps {
  readonly progress: MotionValue<number>;
  readonly reduced: boolean;
}

function CsLane({ progress, reduced }: CsLaneProps) {
  const visibleCount = useTransform(progress, (v) =>
    Math.round(v * CS_TOKENS.length),
  );

  return (
    <LaneFrame label="C# source">
      <pre className="font-mono text-[12.5px] leading-6 whitespace-pre-wrap">
        {CS_TOKENS.map((tok, i) => (
          <CsTokenSpan
            key={i}
            token={tok}
            index={i}
            visibleCount={visibleCount}
            reduced={reduced}
          />
        ))}
      </pre>
    </LaneFrame>
  );
}

interface CsTokenSpanProps {
  readonly token: CsToken;
  readonly index: number;
  readonly visibleCount: MotionValue<number>;
  readonly reduced: boolean;
}

function CsTokenSpan({
  token,
  index,
  visibleCount,
  reduced,
}: CsTokenSpanProps) {
  const opacity = useTransform(visibleCount, (count) =>
    reduced || count > index ? 1 : 0,
  );
  const color = KIND_COLOR[token.kind ?? "plain"];
  const indent = token.indent ? "  ".repeat(token.indent) : "";

  return (
    <>
      {token.indent && index > 0 && CS_TOKENS[index - 1]?.nl ? indent : ""}
      <motion.span style={{ opacity, color }}>{token.t}</motion.span>
      {token.nl ? "\n" : ""}
    </>
  );
}

interface StagesLaneProps {
  readonly progress: MotionValue<number>;
  readonly reduced: boolean;
}

const STAGE_LABELS = ["parse syntax tree", "analyze symbols", "emit schema"];

function StagesLane({ progress, reduced }: StagesLaneProps) {
  return (
    <LaneFrame label="Roslyn source generator">
      <div className="relative h-full">
        {/* Center rail */}
        <div
          aria-hidden
          className="absolute top-2 bottom-2 left-1/2 w-px -translate-x-1/2"
          style={{ background: "rgba(94,234,212,0.18)" }}
        />
        <div className="relative flex h-full flex-col justify-between gap-4">
          {STAGE_LABELS.map((label, i) => (
            <StageBadge
              key={label}
              index={i}
              label={label}
              progress={progress}
              reduced={reduced}
            />
          ))}
        </div>
        {/* Particles */}
        <ParticleLayer progress={progress} reduced={reduced} />
      </div>
    </LaneFrame>
  );
}

interface StageBadgeProps {
  readonly index: number;
  readonly label: string;
  readonly progress: MotionValue<number>;
  readonly reduced: boolean;
}

function StageBadge({ index, label, progress, reduced }: StageBadgeProps) {
  // Each stage activates when progress passes its threshold.
  const threshold = (index + 1) * 0.25;
  const active = useTransform(progress, (v) =>
    reduced || v >= threshold ? 1 : 0,
  );
  const borderColor = useTransform(
    active,
    (a) => `rgba(94,234,212,${(0.16 + a * 0.54).toFixed(3)})`,
  );
  const dotBg = useTransform(
    active,
    (a) => `rgba(94,234,212,${(0.12 + a * 0.55).toFixed(3)})`,
  );

  return (
    <div className="relative z-10 flex items-center justify-center">
      <motion.div
        className="border-cc-card-border bg-cc-surface flex items-center gap-2 rounded-full border px-3 py-1.5 font-mono text-[10.5px] tracking-widest uppercase"
        style={{ borderColor }}
      >
        <motion.span
          className="inline-flex h-3.5 w-3.5 items-center justify-center rounded-full"
          style={{ background: dotBg }}
          aria-hidden
        >
          <motion.svg
            viewBox="0 0 10 10"
            width="9"
            height="9"
            style={{ opacity: active }}
          >
            <path
              d="M1.5 5.2 L4 7.5 L8.5 2.8"
              stroke="#5eead4"
              strokeWidth="1.5"
              fill="none"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </motion.svg>
        </motion.span>
        <span className="text-cc-ink">{label}</span>
      </motion.div>
    </div>
  );
}

interface ParticleLayerProps {
  readonly progress: MotionValue<number>;
  readonly reduced: boolean;
}

function ParticleLayer({ progress, reduced }: ParticleLayerProps) {
  // Five particles, each released at a slightly later offset; they travel from
  // top to bottom across the three stages.
  const particles = [0, 1, 2, 3, 4];
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0">
      {particles.map((i) => (
        <Particle key={i} index={i} progress={progress} reduced={reduced} />
      ))}
    </div>
  );
}

interface ParticleProps {
  readonly index: number;
  readonly progress: MotionValue<number>;
  readonly reduced: boolean;
}

function Particle({ index, progress, reduced }: ParticleProps) {
  // Stagger five particles across the timeline.
  const start = 0.1 + index * 0.12;
  const end = start + 0.55;

  const y = useTransform(progress, (v) => {
    if (reduced) return 100;
    const local = Math.max(0, Math.min(1, (v - start) / (end - start)));
    return local * 100;
  });
  const opacity = useTransform(progress, (v) => {
    if (reduced) return 0;
    if (v < start) return 0;
    if (v > end + 0.04) return 0;
    return 1;
  });
  const glow = useTransform(progress, (v) => {
    if (reduced) return 0;
    const local = Math.max(0, Math.min(1, (v - start) / (end - start)));
    return Math.pow(local, 2);
  });

  const top = useTransform(y, (yv) => `${yv}%`);
  const boxShadow = useTransform(
    glow,
    (g) =>
      `0 0 ${(6 + g * 18).toFixed(1)}px rgba(94,234,212,${(0.35 + g * 0.5).toFixed(3)})`,
  );

  return (
    <motion.div
      className="absolute left-1/2 h-2 w-2 -translate-x-1/2 rounded-full"
      style={{
        top,
        opacity,
        background: "#5eead4",
        boxShadow,
      }}
    />
  );
}

interface SdlLaneProps {
  readonly progress: MotionValue<number>;
  readonly reduced: boolean;
}

function SdlLane({ progress, reduced }: SdlLaneProps) {
  const totalLines = SDL_LINES.length;
  const visibleLines = useTransform(progress, (v) => {
    if (reduced) return totalLines;
    // SDL writing starts at 0.4 (first particle emits) and finishes near 0.95.
    const local = Math.max(0, Math.min(1, (v - 0.4) / 0.55));
    return Math.round(local * totalLines);
  });
  const underline = useTransform(progress, (v) =>
    reduced ? 1 : Math.max(0, Math.min(1, (v - 0.85) / 0.12)),
  );

  return (
    <LaneFrame label="GraphQL SDL">
      <pre className="font-mono text-[12.5px] leading-6 whitespace-pre">
        {SDL_LINES.map((line, i) => (
          <SdlLine
            key={i}
            index={i}
            line={line}
            visibleLines={visibleLines}
            reduced={reduced}
          />
        ))}
      </pre>
      <motion.div
        className="mt-3 h-px origin-left"
        style={{
          background: "#5eead4",
          scaleX: underline,
        }}
        aria-hidden
      />
    </LaneFrame>
  );
}

interface SdlLineProps {
  readonly index: number;
  readonly line: string;
  readonly visibleLines: MotionValue<number>;
  readonly reduced: boolean;
}

function SdlLine({ index, line, visibleLines, reduced }: SdlLineProps) {
  const opacity = useTransform(visibleLines, (n) =>
    reduced || n > index ? 1 : 0,
  );
  const y = useTransform(visibleLines, (n) => (reduced || n > index ? 0 : 6));
  const colored = colorizeSdl(line);
  return <motion.div style={{ opacity, y }}>{colored}</motion.div>;
}

function colorizeSdl(line: string): ReactNode {
  // Naive token coloring for SDL: type keyword, type names, field names.
  if (line === "") return " ";
  if (line.startsWith("type ")) {
    const name = line.slice(5).replace(" {", "");
    return (
      <>
        <span style={{ color: "#ff7b72" }}>type </span>
        <span style={{ color: "#ffa657" }}>{name}</span>
        <span style={{ color: "#c9d1d9" }}> {"{"}</span>
      </>
    );
  }
  if (line === "}") return <span style={{ color: "#c9d1d9" }}>{"}"}</span>;
  // Field lines like "  productById(" or "  id: UUID!"
  const match = line.match(/^(\s+)([a-zA-Z]+)(\(|: )(.*)$/);
  if (match) {
    const [, indent, field, sep, rest] = match;
    return (
      <>
        <span>{indent}</span>
        <span style={{ color: "#79c0ff" }}>{field}</span>
        <span style={{ color: "#c9d1d9" }}>{sep}</span>
        <span style={{ color: "#ffa657" }}>{rest}</span>
      </>
    );
  }
  if (line.trim() === "): Product") {
    return (
      <>
        <span style={{ color: "#c9d1d9" }}>{"  ): "}</span>
        <span style={{ color: "#ffa657" }}>Product</span>
      </>
    );
  }
  return <span style={{ color: "#c9d1d9" }}>{line}</span>;
}

function Centerpiece() {
  const sectionRef = useRef<HTMLElement>(null);
  const reducedPref = useReducedMotion() ?? false;
  const inView = useInView(sectionRef, { once: true, margin: "-10% 0px" });
  // One-shot progress timeline (0..1) decoupled from scroll. We animate it
  // forward once when the section enters view; reduced-motion users jump to 1.
  const progress = useMotionValue(reducedPref ? 1 : 0);

  useEffect(() => {
    if (reducedPref) {
      progress.set(1);
      return;
    }
    if (!inView) return;
    const controls = animate(progress, 1, {
      duration: 3.2,
      ease: [0.22, 1, 0.36, 1],
    });
    return () => controls.stop();
  }, [inView, reducedPref, progress]);

  return (
    <section
      ref={sectionRef}
      id="centerpiece"
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="mb-10 max-w-3xl">
        <IndexTag value="01" />
        <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
          From C# to schema in one build step.
        </h2>
        <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
          Scroll this section. On the left, a partial class types itself out. In
          the middle, the Roslyn source generator parses the syntax tree,
          analyzes symbols, and emits the schema. On the right, typed GraphQL
          SDL writes itself line by line as each stage completes.
        </p>
      </div>
      <div className="grid gap-4 lg:grid-cols-3">
        <CsLane progress={progress} reduced={reducedPref} />
        <StagesLane progress={progress} reduced={reducedPref} />
        <SdlLane progress={progress} reduced={reducedPref} />
      </div>
      <div className="text-cc-ink-dim mt-6 grid grid-cols-3 gap-4 font-mono text-[10.5px] tracking-widest uppercase">
        <span>source: Catalog/Query.cs</span>
        <span className="text-center">build time: a single dotnet build</span>
        <span className="text-right">output: schema + resolvers + loaders</span>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// TRUST STRIP: v1-grounded factual tiles (no invented numbers)
// -----------------------------------------------------------------------------

interface FactTileProps {
  readonly label: string;
  readonly value: string;
}

function FactTile({ label, value }: FactTileProps) {
  return (
    <li>
      <div className="text-cc-heading font-heading text-xl font-semibold tracking-tight sm:text-2xl">
        {value}
      </div>
      <div className="text-cc-ink-dim mt-1 font-mono text-[10.5px] tracking-widest uppercase">
        {label}
      </div>
    </li>
  );
}

function TrustStrip() {
  return (
    <section
      aria-label="At a glance"
      className="border-cc-card-border border-t py-10 sm:py-12"
    >
      <ul className="grid grid-cols-2 gap-6 sm:grid-cols-3 lg:grid-cols-6">
        <FactTile label="License" value="MIT" />
        <FactTile label="Runtime" value="ASP.NET Core" />
        <FactTile label="Spec" value="GraphQL 2025" />
        <FactTile label="Transports" value="HTTP / WS / SSE" />
        <FactTile label="Federation" value="Fusion + Apollo" />
        <FactTile label="Client" value="Strawberry Shake" />
      </ul>
    </section>
  );
}

// -----------------------------------------------------------------------------
// CAPABILITIES GRID
// -----------------------------------------------------------------------------

interface CapabilityProps {
  readonly icon: ReactNode;
  readonly title: string;
  readonly body: string;
  readonly index: number;
}

function Capability({ icon, title, body, index }: CapabilityProps) {
  const reduced = useReducedMotion() ?? false;
  return (
    <motion.div
      initial={reduced ? false : { opacity: 0, y: 16 }}
      whileInView={reduced ? undefined : { opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.4 }}
      transition={{
        duration: 0.5,
        delay: index * 0.06,
        ease: [0.22, 1, 0.36, 1],
      }}
      className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col gap-3 rounded-xl border p-5 transition-colors"
    >
      <motion.div
        className="text-cc-accent"
        initial={reduced ? false : { scale: 0.7, opacity: 0 }}
        whileInView={reduced ? undefined : { scale: 1, opacity: 1 }}
        viewport={{ once: true, amount: 0.5 }}
        transition={{
          duration: 0.5,
          delay: index * 0.06 + 0.1,
          ease: [0.34, 1.56, 0.64, 1],
        }}
      >
        {icon}
      </motion.div>
      <h3 className="text-cc-heading font-heading text-lg font-semibold tracking-tight">
        {title}
      </h3>
      <p className="text-cc-ink-dim text-sm leading-relaxed">{body}</p>
    </motion.div>
  );
}

function IconBolt() {
  return (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden>
      <path
        d="M11 2 L4 11 H9 L8 18 L16 8 H11 Z"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinejoin="round"
      />
    </svg>
  );
}
function IconLoader() {
  return (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden>
      <path
        d="M3 6 H17 M3 10 H17 M3 14 H17"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
    </svg>
  );
}
function IconSub() {
  return (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden>
      <path
        d="M2 14 C 5 14, 5 6, 8 6 S 11 14, 14 14 S 17 6, 19 6"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
        fill="none"
      />
    </svg>
  );
}
function IconOtel() {
  return (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden>
      <circle cx="10" cy="10" r="6" stroke="currentColor" strokeWidth="1.4" />
      <circle cx="10" cy="10" r="1.6" fill="currentColor" />
    </svg>
  );
}
function IconFusion() {
  return (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden>
      <circle cx="6" cy="6" r="2.2" stroke="currentColor" strokeWidth="1.3" />
      <circle cx="14" cy="6" r="2.2" stroke="currentColor" strokeWidth="1.3" />
      <circle cx="10" cy="14" r="2.2" stroke="currentColor" strokeWidth="1.3" />
      <path
        d="M6 8 L10 12 L14 8"
        stroke="currentColor"
        strokeWidth="1.3"
        fill="none"
      />
    </svg>
  );
}
function IconClient() {
  return (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden>
      <rect
        x="3"
        y="4"
        width="14"
        height="10"
        rx="1.5"
        stroke="currentColor"
        strokeWidth="1.4"
      />
      <path
        d="M8 17 H12"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
      <path d="M10 14 V17" stroke="currentColor" strokeWidth="1.4" />
    </svg>
  );
}

function CapabilitiesGrid() {
  return (
    <section className="border-cc-card-border border-t py-20 sm:py-24">
      <div className="mb-10 max-w-3xl">
        <IndexTag value="02" />
        <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
          What ships in the box.
        </h2>
        <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
          One package, the capabilities a production .NET GraphQL server
          actually needs. No add-on store, no plugin scavenger hunt.
        </p>
      </div>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Capability
          index={0}
          icon={<IconBolt />}
          title="Source-generated resolvers"
          body="Annotate a partial class with [QueryType]. Roslyn emits the schema and resolver pipeline at build time. Pure C#, no reflection, no descriptor boilerplate."
        />
        <Capability
          index={1}
          icon={<IconLoader />}
          title="Green Donut DataLoaders"
          body="Per-request key dedupe and batch dispatch. Annotate a static method with [DataLoader] and the loader, interface, and DI registration are emitted for you."
        />
        <Capability
          index={2}
          icon={<IconSub />}
          title="Realtime subscriptions"
          body="graphql-ws or graphql-sse, plus Redis, NATS, Postgres LISTEN/NOTIFY, or RabbitMQ for production fan-out. Publish from any service via ITopicEventSender."
        />
        <Capability
          index={3}
          icon={<IconOtel />}
          title="OpenTelemetry"
          body="AddInstrumentation lights up spans for the server, the execution pipeline, and DataLoaders. Configure an OTLP exporter and the traces land in your existing backend."
        />
        <Capability
          index={4}
          icon={<IconFusion />}
          title="Fusion-ready"
          body="Run standalone today. Compose into a Fusion gateway at build time tomorrow. Apollo Federation spec compatible via the ApolloFederation package. Resolvers stay the same."
        />
        <Capability
          index={5}
          icon={<IconClient />}
          title="Strawberry Shake client"
          body="A first-party .NET GraphQL client with MSBuild-based codegen, typed operations, and a store. Author once, generate strongly typed clients per project."
        />
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// FEDERATION LANE: animated subgraph merge
// -----------------------------------------------------------------------------

function FederationLane() {
  const reduced = useReducedMotion() ?? false;
  const ref = useRef<SVGSVGElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.4 });

  return (
    <section className="border-cc-card-border border-t py-20 sm:py-24">
      <div className="grid items-center gap-12 lg:grid-cols-12">
        <div className="lg:col-span-5">
          <IndexTag value="03" />
          <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            Three subgraphs, one planned gateway.
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            Fusion plans composition at build time, in CI, against your source
            SDLs. The gateway loads a finished query plan and stays cheap to run
            at the edge. The same Hot Chocolate server can run standalone, as a
            Fusion subgraph, or as an Apollo Federation subgraph.
          </p>
        </div>
        <div className="lg:col-span-7">
          <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5">
            <svg
              ref={ref}
              viewBox="0 0 480 240"
              className="h-auto w-full"
              role="img"
              aria-label="Three subgraph nodes merging into a Fusion gateway"
            >
              {[
                { y: 40, label: "catalog" },
                { y: 110, label: "checkout" },
                { y: 180, label: "reviews" },
              ].map((n, i) => (
                <g key={n.label}>
                  <rect
                    x="12"
                    y={n.y - 16}
                    width="140"
                    height="32"
                    rx="6"
                    fill="rgba(245,241,234,0.04)"
                    stroke="rgba(245,241,234,0.18)"
                  />
                  <text
                    x="82"
                    y={n.y + 4}
                    textAnchor="middle"
                    fontFamily="ui-monospace, monospace"
                    fontSize="11"
                    fill="#a1a3af"
                  >
                    {n.label}.graphql
                  </text>
                  <motion.path
                    d={`M 152 ${n.y} C 220 ${n.y}, 240 110, 320 110`}
                    stroke="#5eead4"
                    strokeOpacity="0.7"
                    strokeWidth="1.5"
                    fill="none"
                    initial={reduced ? false : { pathLength: 0 }}
                    animate={
                      reduced || inView ? { pathLength: 1 } : { pathLength: 0 }
                    }
                    transition={{
                      duration: 0.9,
                      delay: 0.2 + i * 0.18,
                      ease: [0.22, 1, 0.36, 1],
                    }}
                  />
                </g>
              ))}
              <motion.g
                initial={reduced ? false : { opacity: 0, scale: 0.85 }}
                animate={
                  reduced || inView
                    ? { opacity: 1, scale: 1 }
                    : { opacity: 0, scale: 0.85 }
                }
                transition={{
                  duration: 0.5,
                  delay: reduced ? 0 : 0.95,
                  ease: [0.34, 1.56, 0.64, 1],
                }}
                style={{ transformOrigin: "380px 110px" }}
              >
                <rect
                  x="320"
                  y="86"
                  width="140"
                  height="48"
                  rx="10"
                  fill="rgba(94,234,212,0.1)"
                  stroke="rgba(94,234,212,0.6)"
                />
                <text
                  x="390"
                  y="108"
                  textAnchor="middle"
                  fontFamily="var(--font-body)"
                  fontSize="12"
                  fill="#f5f0ea"
                >
                  Fusion gateway
                </text>
                <text
                  x="390"
                  y="124"
                  textAnchor="middle"
                  fontFamily="ui-monospace, monospace"
                  fontSize="10"
                  fill="rgba(245,241,234,0.62)"
                >
                  planned at build time
                </text>
              </motion.g>
            </svg>
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// DATALOADER MICRO-DEMO
// -----------------------------------------------------------------------------

function DataLoaderDemo() {
  const reduced = useReducedMotion() ?? false;
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });

  const requests = [0, 1, 2, 3, 4];

  return (
    <section className="border-cc-card-border border-t py-20 sm:py-24">
      <div className="grid items-center gap-12 lg:grid-cols-12">
        <div className="lg:order-2 lg:col-span-5">
          <IndexTag value="04" />
          <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            N+1 collapses into one batch.
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            Green Donut deduplicates keys per request, the execution engine
            resolves fields in waves, and every batch dispatches together.
            Annotate a static method with [DataLoader] and the loader, the
            interface, and the DI registration are emitted for you.
          </p>
        </div>
        <div className="lg:order-1 lg:col-span-7">
          <div
            ref={ref}
            className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5"
          >
            <div className="relative h-56">
              {requests.map((i) => {
                const yStart = 8 + i * 38;
                return (
                  <div key={i}>
                    <motion.div
                      className="border-cc-card-border bg-cc-surface absolute left-0 flex h-7 items-center rounded-md border px-3 font-mono text-[11px]"
                      style={{ top: yStart, color: "#a1a3af" }}
                      initial={reduced ? false : { opacity: 0, x: -8 }}
                      animate={
                        reduced || inView
                          ? { opacity: 1, x: 0 }
                          : { opacity: 0, x: -8 }
                      }
                      transition={{
                        duration: 0.4,
                        delay: reduced ? 0 : 0.1 + i * 0.08,
                      }}
                    >
                      product(id: {i + 1})
                    </motion.div>
                    <motion.svg
                      className="absolute top-0 left-0 h-full w-full"
                      viewBox="0 0 480 220"
                      preserveAspectRatio="none"
                      aria-hidden
                    >
                      <motion.path
                        d={`M 160 ${yStart + 14} C 240 ${yStart + 14}, 240 110, 300 110`}
                        stroke="#5eead4"
                        strokeOpacity="0.55"
                        strokeWidth="1.4"
                        fill="none"
                        initial={reduced ? false : { pathLength: 0 }}
                        animate={
                          reduced || inView
                            ? { pathLength: 1 }
                            : { pathLength: 0 }
                        }
                        transition={{
                          duration: 0.7,
                          delay: reduced ? 0 : 0.5 + i * 0.08,
                          ease: [0.22, 1, 0.36, 1],
                        }}
                      />
                    </motion.svg>
                  </div>
                );
              })}
              <motion.div
                className="border-cc-accent absolute top-[88px] right-0 w-36 rounded-md border bg-[rgba(94,234,212,0.08)] p-2.5 font-mono text-[11px]"
                initial={reduced ? false : { opacity: 0, scale: 0.85 }}
                animate={
                  reduced || inView
                    ? { opacity: 1, scale: 1 }
                    : { opacity: 0, scale: 0.85 }
                }
                transition={{
                  duration: 0.45,
                  delay: reduced ? 0 : 1.1,
                  ease: [0.34, 1.56, 0.64, 1],
                }}
              >
                <div className="text-cc-accent">LoadAsync(ids)</div>
                <div className="text-cc-ink-dim mt-1">1 batched call</div>
              </motion.div>
            </div>
            <div className="text-cc-ink-dim mt-3 font-mono text-[10.5px] tracking-widest uppercase">
              per-request batching, no N+1
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// CODE + QUOTE
// -----------------------------------------------------------------------------

interface CodeRowProps {
  readonly n: number;
  readonly children: ReactNode;
}

function CodeRow({ n, children }: CodeRowProps) {
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

function CodeAndQuote() {
  const reduced = useReducedMotion() ?? false;
  return (
    <section className="border-cc-card-border border-t py-20 sm:py-24">
      <div className="grid items-stretch gap-8 lg:grid-cols-12">
        <motion.div
          className="lg:col-span-7"
          initial={reduced ? false : { opacity: 0, y: 12 }}
          whileInView={reduced ? undefined : { opacity: 1, y: 0 }}
          viewport={{ once: true, amount: 0.35 }}
          transition={{ duration: 0.5, ease: [0.22, 1, 0.36, 1] }}
        >
          <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-xl border">
            <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-3">
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
                Catalog/Query.cs
              </span>
              <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
                C#
              </span>
            </div>
            <div className="py-4">
              <CodeRow n={1}>
                <span style={{ color: KIND_COLOR.kw }}>using</span>{" "}
                <span style={{ color: KIND_COLOR.plain }}>
                  HotChocolate.Types;
                </span>
              </CodeRow>
              <CodeRow n={2}>&nbsp;</CodeRow>
              <CodeRow n={3}>
                <span style={{ color: KIND_COLOR.punct }}>[</span>
                <span style={{ color: KIND_COLOR.attr }}>QueryType</span>
                <span style={{ color: KIND_COLOR.punct }}>]</span>
              </CodeRow>
              <CodeRow n={4}>
                <span style={{ color: KIND_COLOR.kw }}>
                  public partial class{" "}
                </span>
                <span style={{ color: KIND_COLOR.type }}>Query</span>
              </CodeRow>
              <CodeRow n={5}>
                <span style={{ color: KIND_COLOR.punct }}>{"{"}</span>
              </CodeRow>
              <CodeRow n={6}>
                {"    "}
                <span style={{ color: KIND_COLOR.kw }}>
                  public static async{" "}
                </span>
                <span style={{ color: KIND_COLOR.type }}>Task</span>
                <span style={{ color: KIND_COLOR.punct }}>{"<"}</span>
                <span style={{ color: KIND_COLOR.type }}>Product</span>
                <span style={{ color: KIND_COLOR.punct }}>{"?> "}</span>
                <span style={{ color: KIND_COLOR.fn }}>
                  GetProductByIdAsync
                </span>
                <span style={{ color: KIND_COLOR.punct }}>(</span>
              </CodeRow>
              <CodeRow n={7}>
                {"        "}
                <span style={{ color: KIND_COLOR.type }}>Guid</span>{" "}
                <span style={{ color: KIND_COLOR.param }}>id</span>
                <span style={{ color: KIND_COLOR.punct }}>,</span>
              </CodeRow>
              <CodeRow n={8}>
                {"        "}
                <span style={{ color: KIND_COLOR.type }}>
                  IProductByIdDataLoader
                </span>{" "}
                <span style={{ color: KIND_COLOR.param }}>productById</span>
                <span style={{ color: KIND_COLOR.punct }}>,</span>
              </CodeRow>
              <CodeRow n={9}>
                {"        "}
                <span style={{ color: KIND_COLOR.type }}>
                  CancellationToken
                </span>{" "}
                <span style={{ color: KIND_COLOR.param }}>ct</span>
                <span style={{ color: KIND_COLOR.punct }}>{") =>"}</span>
              </CodeRow>
              <CodeRow n={10}>
                {"        "}
                <span style={{ color: KIND_COLOR.kw }}>await</span>{" "}
                <span style={{ color: KIND_COLOR.param }}>productById</span>
                <span style={{ color: KIND_COLOR.punct }}>.</span>
                <span style={{ color: KIND_COLOR.fn }}>LoadAsync</span>
                <span style={{ color: KIND_COLOR.punct }}>(</span>
                <span style={{ color: KIND_COLOR.param }}>id</span>
                <span style={{ color: KIND_COLOR.punct }}>, </span>
                <span style={{ color: KIND_COLOR.param }}>ct</span>
                <span style={{ color: KIND_COLOR.punct }}>);</span>
              </CodeRow>
              <CodeRow n={11}>
                <span style={{ color: KIND_COLOR.punct }}>{"}"}</span>
              </CodeRow>
            </div>
            <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
              <span>build: schema + resolvers + DataLoader emitted</span>
              <span className="text-cc-accent">Roslyn source generator</span>
            </div>
          </div>
        </motion.div>
        <motion.figure
          className="lg:col-span-5"
          initial={reduced ? false : { opacity: 0, y: 12 }}
          whileInView={reduced ? undefined : { opacity: 1, y: 0 }}
          viewport={{ once: true, amount: 0.35 }}
          transition={{
            duration: 0.5,
            delay: 0.1,
            ease: [0.22, 1, 0.36, 1],
          }}
        >
          <div className="border-cc-card-border bg-cc-card-bg flex h-full flex-col justify-between rounded-xl border p-6">
            <svg
              width="32"
              height="32"
              viewBox="0 0 32 32"
              className="text-cc-accent"
              aria-hidden
            >
              <path
                d="M8 22 V14 a4 4 0 0 1 4 -4 h2 v4 h-2 v8 z M20 22 V14 a4 4 0 0 1 4 -4 h2 v4 h-2 v8 z"
                fill="currentColor"
                opacity="0.85"
              />
            </svg>
            <blockquote className="text-cc-heading font-heading mt-4 text-xl leading-snug">
              We write idiomatic C#. The schema, the loaders, and the resolver
              pipeline are emitted at build time, and the API stays typed end to
              end.
            </blockquote>
            <figcaption className="text-cc-ink-dim mt-4 font-mono text-[11px] tracking-widest uppercase">
              from the Hot Chocolate community
            </figcaption>
          </div>
        </motion.figure>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// CLOSING CTA
// -----------------------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="border-cc-card-border relative border-t py-20 sm:py-28">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-px"
        style={{ background: SPECTRUM }}
      />
      <div className="text-center">
        <Eyebrow>Get started</Eyebrow>
        <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
          Ship your GraphQL server today.
        </h2>
        <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
          A C# project, a partial class, a few attributes. The schema, the
          DataLoaders, and the resolver pipeline are generated for you at build
          time, and the runtime is the ASP.NET Core you already run.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            Star on GitHub
          </OutlineButton>
        </div>
        <ul className="text-cc-ink-dim mx-auto mt-10 flex max-w-2xl flex-wrap items-center justify-center gap-x-6 gap-y-3 font-mono text-[11px] tracking-widest uppercase">
          {["MIT licensed", "ASP.NET Core", "GraphQL 2025", "Fusion ready"].map(
            (label) => (
              <li key={label} className="flex items-center gap-2">
                <span className="text-cc-accent" aria-hidden>
                  <CheckIcon size={11} />
                </span>
                {label}
              </li>
            ),
          )}
        </ul>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// PAGE
// -----------------------------------------------------------------------------

export function ClientPage() {
  return (
    <>
      <Hero />
      <Centerpiece />
      <TrustStrip />
      <CapabilitiesGrid />
      <FederationLane />
      <DataLoaderDemo />
      <CodeAndQuote />
      <ClosingCta />
    </>
  );
}
