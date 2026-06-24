"use client";

import type { ReactNode } from "react";
import { useEffect, useRef, useState } from "react";
import type { Variants } from "motion/react";
import { motion, useInView, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CookieCrumble } from "@/src/icons/CookieCrumble";

// V7 "Mismatch Reel": the failing-snapshot review workflow rendered as a
// three-stage centerpiece that plays once on enter view. Coral is reserved for
// the diff and failure signal inside the reel; the rest of the page stays on
// cc-* tokens.

// Brand spectrum hairline, used exactly once on the closing CTA.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// Semantic coral, only used inside the reel as the diff/failure color.
const CORAL = "#f0786a";

// -----------------------------------------------------------------------------
// Small primitives shared across the page.
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
// Code primitives.
// -----------------------------------------------------------------------------

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

const C = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  comment: { color: "#8b949e", fontStyle: "italic" as const },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
};

interface CodeCardChromeProps {
  readonly filename: string;
  readonly lang: string;
  readonly children: ReactNode;
  readonly footerLeft?: ReactNode;
  readonly footerRight?: ReactNode;
  readonly accent?: boolean;
}

function CodeCardChrome({
  filename,
  lang,
  children,
  footerLeft,
  footerRight,
  accent = false,
}: CodeCardChromeProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl">
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
          {filename}
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {lang}
        </span>
      </div>
      {accent ? (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-70"
          style={{
            background:
              "radial-gradient(420px 180px at 14% 22%, rgba(94, 234, 212, 0.18), transparent 70%), radial-gradient(280px 140px at 8% 16%, rgba(22, 185, 228, 0.16), transparent 70%)",
          }}
        />
      ) : null}
      <div className="relative py-4">{children}</div>
      {(footerLeft || footerRight) && (
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between gap-4 border-t px-4 py-2.5 font-mono text-[11px]">
          <span>{footerLeft}</span>
          <span className="text-cc-accent">{footerRight}</span>
        </div>
      )}
    </div>
  );
}

// -----------------------------------------------------------------------------
// Typewriter: per-character reveal driven by whileInView and stagger.
// Falls back to static text under reduced motion.
// -----------------------------------------------------------------------------

interface TypewriterProps {
  readonly text: string;
  readonly delay?: number;
  readonly perChar?: number;
  readonly style?: React.CSSProperties;
}

function Typewriter({
  text,
  delay = 0,
  perChar = 0.045,
  style,
}: TypewriterProps) {
  const reduce = useReducedMotion();
  if (reduce) {
    return <span style={style}>{text}</span>;
  }
  return (
    <motion.span
      initial="hidden"
      whileInView="show"
      viewport={{ once: true, amount: 0.6 }}
      variants={{
        hidden: {},
        show: {
          transition: { staggerChildren: perChar, delayChildren: delay },
        },
      }}
      aria-label={text}
      style={style}
    >
      {Array.from(text).map((ch, i) => (
        <motion.span
          key={`${ch}-${i}`}
          variants={{
            hidden: { opacity: 0 },
            show: { opacity: 1, transition: { duration: 0.02 } },
          }}
          aria-hidden
        >
          {ch}
        </motion.span>
      ))}
    </motion.span>
  );
}

// -----------------------------------------------------------------------------
// Hero
// -----------------------------------------------------------------------------

function HeroTestCard() {
  const reduce = useReducedMotion();
  return (
    <CodeCardChrome
      filename="Catalog.Tests/ProductQueryTests.cs"
      lang="C#"
      footerLeft="xUnit + Cookie Crumble"
      footerRight="MatchSnapshot()"
      accent
    >
      <CodeLine n={1}>
        <span style={C.kw}>using</span>{" "}
        <span style={C.plain}>CookieCrumble;</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={C.kw}>using</span>{" "}
        <span style={C.plain}>HotChocolate.Execution;</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={C.kw}>public class</span>{" "}
        <span style={C.type}>ProductQueryTests</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={C.punct}>{`{`}</span>
      </CodeLine>
      <CodeLine n={6}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.punct}>[</span>
        <span style={C.attr}>Fact</span>
        <span style={C.punct}>]</span>
      </CodeLine>
      <CodeLine n={7}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.kw}>public async</span> <span style={C.type}>Task</span>{" "}
        <span style={C.fn}>Product_By_Id_Returns_Catalog_Shape</span>
        <span style={C.punct}>()</span>
      </CodeLine>
      <CodeLine n={8}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.punct}>{`{`}</span>
      </CodeLine>
      <CodeLine n={9}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.kw}>await using var</span>{" "}
        <span style={C.plain}>server </span>
        <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
        <span style={C.type}>TestServer</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>CreateAsync</span>
        <span style={C.punct}>();</span>
      </CodeLine>
      <CodeLine n={10}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.type}>IExecutionResult</span>{" "}
        <span style={C.plain}>result </span>
        <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
        <span style={C.plain}>server</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>ExecuteAsync</span>
        <span style={C.punct}>(query);</span>
      </CodeLine>
      <CodeLine n={11}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={12}>
        <span style={C.plain}>{`        `}</span>
        <span style={C.plain}>result</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>
          <Typewriter text="MatchSnapshot" delay={0.2} />
        </span>
        <span style={C.punct}>();</span>
      </CodeLine>
      <CodeLine n={13}>
        <span style={C.plain}>{`        `}</span>
        <motion.span
          aria-hidden
          initial={{ scaleX: 0 }}
          whileInView={{ scaleX: 1 }}
          viewport={{ once: true, amount: 0.6 }}
          transition={{
            duration: reduce ? 0 : 0.6,
            delay: reduce ? 0 : 0.95,
            ease: "easeOut",
          }}
          style={{
            display: "inline-block",
            width: "13ch",
            height: "2px",
            transformOrigin: "left",
            background: CORAL,
            verticalAlign: "middle",
          }}
        />
      </CodeLine>
      <CodeLine n={14}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
      <CodeLine n={15}>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
    </CodeCardChrome>
  );
}

function HeroSnapshotCard() {
  return (
    <CodeCardChrome
      filename="__snapshots__/ProductQueryTests.Product_By_Id_Returns_Catalog_Shape.snap"
      lang="snapshot"
      footerLeft="GraphQL-aware formatter"
      footerRight="IExecutionResult"
    >
      <CodeLine n={1}>
        <span style={C.punct}>{`{`}</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={C.plain}>{`  `}</span>
        <span style={C.str}>{`"data"`}</span>
        <span style={C.punct}>: {`{`}</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.str}>{`"productById"`}</span>
        <span style={C.punct}>: {`{`}</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={C.plain}>{`      `}</span>
        <span style={C.str}>{`"id"`}</span>
        <span style={C.punct}>: </span>
        <span style={C.str}>{`"p_42"`}</span>
        <span style={C.punct}>,</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={C.plain}>{`      `}</span>
        <span style={C.str}>{`"name"`}</span>
        <span style={C.punct}>: </span>
        <span style={C.str}>{`"Cookie Crumble Tee"`}</span>
        <span style={C.punct}>,</span>
      </CodeLine>
      <CodeLine n={6}>
        <span style={C.plain}>{`      `}</span>
        <span style={C.str}>{`"price"`}</span>
        <span style={C.punct}>: </span>
        <span style={C.plain}>{`24.0`}</span>
      </CodeLine>
      <CodeLine n={7}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
      <CodeLine n={8}>
        <span style={C.plain}>{`  `}</span>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
      <CodeLine n={9}>
        <span style={C.punct}>{`}`}</span>
      </CodeLine>
      <CodeLine n={10}>
        <span style={C.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={11}>
        <span style={C.comment}>{`# Committed alongside the test.`}</span>
      </CodeLine>
      <CodeLine n={12}>
        <span
          style={C.comment}
        >{`# Diffs in PRs read like the API contract.`}</span>
      </CodeLine>
    </CodeCardChrome>
  );
}

// Numeric tick-up for stat tiles. Falls back to static value for non-numeric
// content or reduced-motion users.
interface TickUpProps {
  readonly value: string;
}

function TickUp({ value }: TickUpProps) {
  const reduce = useReducedMotion();
  const ref = useRef<HTMLSpanElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.6 });
  const numeric = /^\d+(\.\d+)?$/.test(value.trim());
  const [shown, setShown] = useState<string>(!numeric || reduce ? value : "0");
  useEffect(() => {
    if (!inView || !numeric || reduce) {
      return;
    }
    const target = Number(value);
    const start = performance.now();
    const dur = 700;
    let raf = 0;
    const step = (t: number) => {
      const k = Math.min(1, (t - start) / dur);
      const eased = 1 - Math.pow(1 - k, 3);
      setShown(String(Math.round(target * eased)));
      if (k < 1) {
        raf = requestAnimationFrame(step);
      } else {
        setShown(value);
      }
    };
    raf = requestAnimationFrame(step);
    return () => cancelAnimationFrame(raf);
  }, [inView, numeric, reduce, value]);
  return <span ref={ref}>{shown}</span>;
}

interface HeroStatTileProps {
  readonly label: string;
  readonly value: string;
}

function HeroStatTile({ label, value }: HeroStatTileProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.6 }}
      transition={{ duration: 0.4, ease: "easeOut" }}
    >
      <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
        {label}
      </dt>
      <dd className="text-cc-ink mt-1 text-sm">
        <TickUp value={value} />
      </dd>
    </motion.div>
  );
}

function Hero() {
  return (
    <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-5">
          <Eyebrow>Snapshot testing for .NET</Eyebrow>
          <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
            Snapshot testing that understands GraphQL.
          </h1>
          <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
            Cookie Crumble is the open-source snapshot library the ChilliCream
            team writes its own tests with. It ships native formatters for Hot
            Chocolate IExecutionResult and GraphQLHttpResponse, so the snapshot
            file reads like the GraphQL response itself. Inline, file, or
            Markdown. xUnit, NUnit, TUnit, or MSTest. MIT-licensed.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
          <dl className="border-cc-card-border mt-10 grid grid-cols-3 gap-6 border-t pt-6">
            <HeroStatTile label="License" value="MIT" />
            <HeroStatTile label="Runtimes" value=".NET 8 and later" />
            <HeroStatTile
              label="Frameworks"
              value="xUnit, NUnit, TUnit, MSTest"
            />
          </dl>
        </div>
        <div className="lg:col-span-7">
          <motion.div
            initial={{ opacity: 0, y: 14 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, amount: 0.3 }}
            transition={{ duration: 0.55, ease: "easeOut" }}
            className="grid gap-4 lg:grid-cols-2"
          >
            <HeroTestCard />
            <HeroSnapshotCard />
          </motion.div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Capability strip with staggered fade-up and check-icon pop.
// -----------------------------------------------------------------------------

function Capabilities() {
  const items = [
    "GraphQL-aware formatters",
    "Inline + file + Markdown",
    "__mismatch__ workflow",
    "xUnit, NUnit, TUnit, MSTest",
    "Dogfooded by the platform",
  ];
  const list: Variants = {
    hidden: {},
    show: { transition: { staggerChildren: 0.06, delayChildren: 0.05 } },
  };
  const item: Variants = {
    hidden: { opacity: 0, y: 6 },
    show: { opacity: 1, y: 0, transition: { duration: 0.32, ease: "easeOut" } },
  };
  return (
    <section
      aria-label="Capabilities at a glance"
      className="border-cc-card-border border-y py-6"
    >
      <motion.ul
        initial="hidden"
        whileInView="show"
        viewport={{ once: true, amount: 0.3 }}
        variants={list}
        className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-5"
      >
        {items.map((label) => (
          <motion.li
            key={label}
            variants={item}
            className="text-cc-ink flex items-center gap-2 font-mono text-[11.5px] tracking-tight uppercase"
          >
            <motion.span
              className="text-cc-accent"
              aria-hidden
              initial={{ scale: 0 }}
              whileInView={{ scale: 1 }}
              viewport={{ once: true, amount: 0.6 }}
              transition={{ type: "spring", stiffness: 380, damping: 18 }}
            >
              <CheckIcon size={12} />
            </motion.span>
            {label}
          </motion.li>
        ))}
      </motion.ul>
    </section>
  );
}

// -----------------------------------------------------------------------------
// CENTERPIECE: THE MISMATCH REEL
// One-shot three-stage animation: Run, Diff, Accept. Triggered on enter view.
// -----------------------------------------------------------------------------

interface ReelLine {
  readonly key: string;
  readonly text: string;
  readonly changed?: boolean;
}

const OLD_JSON: readonly ReelLine[] = [
  { key: "o1", text: "{" },
  { key: "o2", text: '  "data": {' },
  { key: "o3", text: '    "productById": {' },
  { key: "o4", text: '      "id": "p_42",' },
  { key: "o5", text: '      "name": "Cookie Crumble Tee",' },
  { key: "o6", text: '      "price": 24.0' },
  { key: "o7", text: "    }" },
  { key: "o8", text: "  }" },
  { key: "o9", text: "}" },
];

const NEW_JSON: readonly ReelLine[] = [
  { key: "n1", text: "{" },
  { key: "n2", text: '  "data": {' },
  { key: "n3", text: '    "productById": {' },
  { key: "n4", text: '      "id": "p_42",' },
  {
    key: "n5",
    text: '      "name": "Cookie Crumble Tee Long Sleeve",',
    changed: true,
  },
  { key: "n6", text: '      "price": 28.0', changed: true },
  { key: "n7", text: "    }" },
  { key: "n8", text: "  }" },
  { key: "n9", text: "}" },
];

interface StageRailProps {
  readonly stage: 1 | 2 | 3;
}

function StageRail({ stage }: StageRailProps) {
  const labels: readonly [string, string, string] = ["Run", "Diff", "Accept"];
  return (
    <ol className="flex flex-col gap-6">
      {labels.map((label, i) => {
        const idx = (i + 1) as 1 | 2 | 3;
        const active = stage >= idx;
        return (
          <li
            key={label}
            className="flex items-center gap-3 font-mono text-[11px] tracking-[0.2em] uppercase"
          >
            <span
              className="bg-cc-surface/60 flex h-7 w-7 items-center justify-center rounded-full border tabular-nums transition-colors"
              style={{
                borderColor: active
                  ? "var(--color-cc-accent)"
                  : "var(--color-cc-card-border)",
                color: active
                  ? "var(--color-cc-accent)"
                  : "var(--color-cc-ink-dim)",
              }}
            >
              {idx}
            </span>
            <span
              style={{
                color: active
                  ? "var(--color-cc-heading)"
                  : "var(--color-cc-ink-dim)",
              }}
            >
              {label}
            </span>
          </li>
        );
      })}
    </ol>
  );
}

function MismatchReel() {
  const ref = useRef<HTMLDivElement>(null);
  const reduce = useReducedMotion();
  const inView = useInView(ref, { once: true, amount: 0.35 });

  const [stage, setStage] = useState<1 | 2 | 3>(reduce ? 3 : 1);
  const [runProgress, setRunProgress] = useState(reduce ? 1 : 0);
  const [diffProgress, setDiffProgress] = useState(reduce ? 1 : 0);
  const [acceptProgress, setAcceptProgress] = useState(reduce ? 1 : 0);

  // Drive the three stages with a single time-based progress (0..1) once the
  // section enters the viewport. No scroll coupling.
  useEffect(() => {
    if (!inView || reduce) {
      return;
    }
    const start = performance.now();
    const dur = 4200;
    let raf = 0;
    const step = (t: number) => {
      const v = Math.min(1, (t - start) / dur);
      if (v < 0.33) {
        setStage(1);
        setRunProgress(Math.min(1, v / 0.33));
        setDiffProgress(0);
        setAcceptProgress(0);
      } else if (v < 0.66) {
        setStage(2);
        setRunProgress(1);
        setDiffProgress(Math.min(1, (v - 0.33) / 0.33));
        setAcceptProgress(0);
      } else {
        setStage(3);
        setRunProgress(1);
        setDiffProgress(1);
        setAcceptProgress(Math.min(1, (v - 0.66) / 0.34));
      }
      if (v < 1) {
        raf = requestAnimationFrame(step);
      }
    };
    raf = requestAnimationFrame(step);
    return () => cancelAnimationFrame(raf);
  }, [inView, reduce]);

  const visibleLines = reduce
    ? NEW_JSON.length
    : Math.round(diffProgress * NEW_JSON.length);

  const acceptX = reduce ? 1 : acceptProgress;

  return (
    <section
      ref={ref}
      id="reel"
      aria-label="The Mismatch Reel"
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
    >
      <div className="grid items-start gap-10 lg:grid-cols-12 lg:gap-12">
        {/* Copy + stage rail. */}
        <div className="lg:col-span-4">
          <div className="flex items-center gap-3">
            <IndexTag value="00" />
            <Eyebrow>Update workflow</Eyebrow>
          </div>
          <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            A failing snapshot becomes a code review, not a silent overwrite.
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            When the test runs and the output differs, Cookie Crumble writes the
            actual response into a gitignored __mismatch__ folder. You diff it
            against the committed snapshot, decide whether the change is
            intentional, and only then accept it into __snapshots__.
          </p>
          <div className="border-cc-card-border bg-cc-card-bg mt-8 rounded-xl border px-5 py-5">
            <p className="text-cc-ink-dim font-mono text-[11px] tracking-[0.2em] uppercase">
              Stage progress
            </p>
            <div className="mt-4">
              <StageRail stage={stage} />
            </div>
          </div>
        </div>

        {/* Reel stage. */}
        <div className="lg:col-span-8">
          <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-5 sm:p-7">
            <div className="flex items-center justify-between gap-4">
              <div className="flex items-center gap-2">
                <span
                  aria-hidden
                  className="size-2.5 rounded-full"
                  style={{
                    backgroundColor:
                      stage === 3 ? "var(--color-cc-accent)" : CORAL,
                  }}
                />
                <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.2em] uppercase">
                  Stage {stage} of 3
                </span>
              </div>
              <span className="text-cc-ink-dim font-mono text-[11px]">
                Catalog.Tests / ProductQueryTests
              </span>
            </div>

            {/* Stage 1: Run. */}
            <div className="mt-6">
              <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
                <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]">
                  <span>ProductQueryTests.cs</span>
                  <span style={{ color: CORAL }}>test failed</span>
                </div>
                <div className="py-3">
                  <CodeLine n={1}>
                    <span style={C.plain}>result</span>
                    <span style={C.punct}>.</span>
                    <span style={C.fn}>MatchSnapshot</span>
                    <span style={C.punct}>();</span>
                  </CodeLine>
                  <div className="px-5">
                    <div
                      className="ml-[2.5rem]"
                      style={{
                        height: 2,
                        background: CORAL,
                        width: `${runProgress * 13}ch`,
                        transition: reduce ? "none" : "width 80ms linear",
                        opacity: stage === 1 ? 1 : 0.65,
                      }}
                      aria-hidden
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Stage 2: Diff. */}
            <div className="mt-5 grid gap-4 sm:grid-cols-2">
              <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
                <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]">
                  <span>__snapshots__/ProductQueryTests.snap</span>
                  <span className="text-cc-accent">committed</span>
                </div>
                <div className="py-3 font-mono text-[12px]">
                  {OLD_JSON.map((line) => (
                    <div key={line.key} className="px-5 leading-6">
                      <span style={{ color: "rgba(245,241,234,0.62)" }}>
                        {line.text}
                      </span>
                    </div>
                  ))}
                </div>
              </div>

              <div
                className="bg-cc-code-bg overflow-hidden rounded-lg border"
                style={{
                  borderColor:
                    stage === 3
                      ? "var(--color-cc-accent)"
                      : "rgba(240,120,106,0.55)",
                  transform: reduce
                    ? "translateX(0)"
                    : stage === 3
                      ? `translateX(${-acceptX * 6}%)`
                      : "translateX(0)",
                  transition: reduce ? "none" : "transform 160ms linear",
                }}
              >
                <div
                  className="flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]"
                  style={{
                    borderColor:
                      stage === 3
                        ? "var(--color-cc-accent)"
                        : "rgba(240,120,106,0.35)",
                    color:
                      stage === 3
                        ? "var(--color-cc-accent)"
                        : "var(--color-cc-ink-dim)",
                  }}
                >
                  <span>
                    {stage === 3
                      ? "__snapshots__/ProductQueryTests.snap"
                      : "__mismatch__/ProductQueryTests.snap"}
                  </span>
                  <span
                    style={{
                      color: stage === 3 ? "var(--color-cc-accent)" : CORAL,
                    }}
                  >
                    {stage === 3 ? "accepted" : "actual"}
                  </span>
                </div>
                <div className="relative py-3 font-mono text-[12px]">
                  {NEW_JSON.map((line, i) => {
                    const isVisible = reduce || i < visibleLines;
                    const isChanged = !!line.changed;
                    const dimUnchanged = stage === 2 && !isChanged;
                    return (
                      <div
                        key={line.key}
                        className="px-5 leading-6"
                        style={{
                          opacity: isVisible ? 1 : 0,
                          transform: isVisible
                            ? "translateY(0)"
                            : "translateY(4px)",
                          transition: reduce
                            ? "none"
                            : "opacity 220ms ease-out, transform 220ms ease-out",
                          color: isChanged
                            ? stage === 3
                              ? "var(--color-cc-accent)"
                              : CORAL
                            : dimUnchanged
                              ? "rgba(245,241,234,0.38)"
                              : "rgba(245,241,234,0.78)",
                          background:
                            isChanged && stage === 2
                              ? "rgba(240,120,106,0.07)"
                              : "transparent",
                        }}
                      >
                        {line.text}
                      </div>
                    );
                  })}
                  {stage === 2 && !reduce ? (
                    <div
                      aria-hidden
                      className="pointer-events-none absolute inset-y-0"
                      style={{
                        left: `${diffProgress * 100}%`,
                        width: 2,
                        background: CORAL,
                        opacity: 0.55,
                      }}
                    />
                  ) : null}
                </div>
              </div>
            </div>

            {/* Stage 3: Accept. Arrow draws from __mismatch__ to __snapshots__. */}
            <div className="mt-6 flex flex-wrap items-center justify-between gap-4">
              <div className="text-cc-ink-dim flex items-center gap-3 font-mono text-[11px] tracking-[0.2em] uppercase">
                <span>__mismatch__/</span>
                <svg
                  viewBox="0 0 80 14"
                  width="80"
                  height="14"
                  aria-hidden
                  style={{ overflow: "visible" }}
                >
                  <motion.path
                    d="M 2 7 L 70 7"
                    fill="none"
                    stroke={
                      stage === 3
                        ? "var(--color-cc-accent)"
                        : "rgba(245,241,234,0.28)"
                    }
                    strokeWidth="1.5"
                    strokeLinecap="round"
                    style={{ pathLength: reduce ? 1 : acceptX }}
                  />
                  <motion.polygon
                    points="70,3 78,7 70,11"
                    fill={
                      stage === 3
                        ? "var(--color-cc-accent)"
                        : "rgba(245,241,234,0.28)"
                    }
                    style={{ opacity: reduce ? 1 : acceptX }}
                  />
                </svg>
                <span
                  style={{
                    color:
                      stage === 3
                        ? "var(--color-cc-accent)"
                        : "var(--color-cc-ink-dim)",
                  }}
                >
                  __snapshots__/
                </span>
              </div>
              <div
                className="flex items-center gap-2 font-mono text-[11px] tracking-[0.2em] uppercase"
                style={{
                  color:
                    stage === 3
                      ? "var(--color-cc-accent)"
                      : "var(--color-cc-ink-dim)",
                  opacity: reduce ? 1 : 0.4 + 0.6 * acceptX,
                  transition: reduce ? "none" : "opacity 200ms linear",
                }}
              >
                <motion.span
                  initial={{ scale: 0 }}
                  animate={{ scale: stage === 3 ? 1 : 0 }}
                  transition={{ type: "spring", stiffness: 360, damping: 18 }}
                  aria-hidden
                >
                  <CheckIcon size={14} />
                </motion.span>
                Accepted
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

// -----------------------------------------------------------------------------
// Feature row (with whileInView reveals).
// -----------------------------------------------------------------------------

interface FeatureRowProps {
  readonly id: string;
  readonly index: string;
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
  eyebrow,
  title,
  body,
  bullets,
  visual,
  reverse = false,
}: FeatureRowProps) {
  return (
    <motion.section
      id={id}
      className="border-cc-card-border scroll-mt-24 border-t py-20 sm:py-24"
      initial={{ opacity: 0, y: 18 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.2 }}
      transition={{ duration: 0.55, ease: "easeOut" }}
    >
      <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-16">
        <div
          className={[
            "lg:col-span-5",
            reverse ? "lg:order-2" : "lg:order-1",
          ].join(" ")}
        >
          <div className="flex items-center gap-3">
            <IndexTag value={index} />
            <Eyebrow>{eyebrow}</Eyebrow>
          </div>
          <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            {title}
          </h2>
          <p className="text-cc-prose mt-4 text-base leading-relaxed sm:text-lg">
            {body}
          </p>
          <motion.ul
            initial="hidden"
            whileInView="show"
            viewport={{ once: true, amount: 0.3 }}
            variants={{
              hidden: {},
              show: { transition: { staggerChildren: 0.07 } },
            }}
            className="mt-6 flex flex-col gap-2.5"
          >
            {bullets.map((b) => (
              <motion.li
                key={b}
                variants={{
                  hidden: { opacity: 0, x: -6 },
                  show: {
                    opacity: 1,
                    x: 0,
                    transition: { duration: 0.32, ease: "easeOut" },
                  },
                }}
                className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
              >
                <span className="text-cc-accent mt-1 shrink-0">
                  <CheckIcon size={14} />
                </span>
                <span>{b}</span>
              </motion.li>
            ))}
          </motion.ul>
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
    </motion.section>
  );
}

// -----------------------------------------------------------------------------
// Row visuals.
// -----------------------------------------------------------------------------

function FormattersVisual() {
  const cards: Variants = {
    hidden: {},
    show: { transition: { staggerChildren: 0.12 } },
  };
  const card: Variants = {
    hidden: { opacity: 0, y: 10 },
    show: { opacity: 1, y: 0, transition: { duration: 0.4, ease: "easeOut" } },
  };
  return (
    <motion.div
      initial="hidden"
      whileInView="show"
      viewport={{ once: true, amount: 0.3 }}
      variants={cards}
      className="flex flex-col gap-4"
    >
      <motion.div
        variants={card}
        className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border"
      >
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]">
          <span>IExecutionResult</span>
          <span className="text-cc-accent">in-process execution</span>
        </div>
        <div className="py-3">
          <CodeLine n={1}>
            <span style={C.type}>IExecutionResult</span>{" "}
            <span style={C.plain}>result </span>
            <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
            <span style={C.plain}>server</span>
            <span style={C.punct}>.</span>
            <span style={C.fn}>ExecuteAsync</span>
            <span style={C.punct}>(query);</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={C.plain}>result</span>
            <span style={C.punct}>.</span>
            <span style={C.fn}>MatchSnapshot</span>
            <span style={C.punct}>();</span>
          </CodeLine>
        </div>
      </motion.div>
      <motion.div
        variants={card}
        className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border"
      >
        <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]">
          <span>GraphQLHttpResponse</span>
          <span className="text-cc-accent">over HTTP</span>
        </div>
        <div className="py-3">
          <CodeLine n={1}>
            <span style={C.type}>GraphQLHttpResponse</span>{" "}
            <span style={C.plain}>response </span>
            <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
            <span style={C.plain}>client</span>
            <span style={C.punct}>.</span>
            <span style={C.fn}>PostAsync</span>
            <span style={C.punct}>(request);</span>
          </CodeLine>
          <CodeLine n={2}>
            <span style={C.plain}>response</span>
            <span style={C.punct}>.</span>
            <span style={C.fn}>MatchSnapshot</span>
            <span style={C.punct}>();</span>
          </CodeLine>
        </div>
      </motion.div>
      <motion.p
        variants={card}
        className="text-cc-ink-dim text-[12px] leading-relaxed"
      >
        Both types ship with native formatters, so the snapshot reads like the
        GraphQL response itself, not like a serialized object graph.
      </motion.p>
    </motion.div>
  );
}

interface FlavorCardProps {
  readonly title: string;
  readonly api: string;
  readonly children: ReactNode;
}

function FlavorCard({ title, api, children }: FlavorCardProps) {
  return (
    <motion.div
      variants={{
        hidden: { opacity: 0, y: 12 },
        show: {
          opacity: 1,
          y: 0,
          transition: { duration: 0.4, ease: "easeOut" },
        },
      }}
      className="bg-cc-code-bg overflow-hidden rounded-lg border"
      style={{ borderColor: "var(--color-cc-card-border)" }}
    >
      <motion.div
        initial={{ boxShadow: "0 0 0 0 rgba(94,234,212,0)" }}
        whileInView={{
          boxShadow: [
            "0 0 0 0 rgba(94,234,212,0)",
            "0 0 0 1px rgba(94,234,212,0.55)",
            "0 0 0 0 rgba(94,234,212,0)",
          ],
        }}
        viewport={{ once: true, amount: 0.6 }}
        transition={{ duration: 1.1, ease: "easeInOut" }}
        className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]"
      >
        <span>{title}</span>
        <span className="text-cc-accent">{api}</span>
      </motion.div>
      <div className="py-3">{children}</div>
    </motion.div>
  );
}

function SnapshotFlavorsVisual() {
  return (
    <motion.div
      initial="hidden"
      whileInView="show"
      viewport={{ once: true, amount: 0.3 }}
      variants={{
        hidden: {},
        show: { transition: { staggerChildren: 0.12 } },
      }}
      className="flex flex-col gap-3"
    >
      <FlavorCard title="Inline" api="MatchInlineSnapshot">
        <CodeLine n={1}>
          <span style={C.plain}>result</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchInlineSnapshot</span>
          <span style={C.punct}>(</span>
          <span style={C.str}>{`"""`}</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.str}>{`{ "data": { "ping": "pong" } }`}</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.str}>{`"""`}</span>
          <span style={C.punct}>);</span>
        </CodeLine>
      </FlavorCard>
      <FlavorCard title="File" api="MatchSnapshot">
        <CodeLine n={1}>
          <span style={C.plain}>result</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchSnapshot</span>
          <span style={C.punct}>();</span>{" "}
          <span style={C.comment}>{`// __snapshots__/<test>.snap`}</span>
        </CodeLine>
      </FlavorCard>
      <FlavorCard title="Markdown" api="MatchMarkdownSnapshot">
        <CodeLine n={1}>
          <span style={C.type}>Snapshot</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Create</span>
          <span style={C.punct}>()</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(request, </span>
          <span style={C.str}>{`"Request"`}</span>
          <span style={C.punct}>)</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(result, </span>
          <span style={C.str}>{`"Result"`}</span>
          <span style={C.punct}>)</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>Add</span>
          <span style={C.punct}>(events, </span>
          <span style={C.str}>{`"Audit"`}</span>
          <span style={C.punct}>)</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchMarkdownSnapshot</span>
          <span style={C.punct}>();</span>
        </CodeLine>
      </FlavorCard>
    </motion.div>
  );
}

function FrameworkMatrixVisual() {
  const frameworks = [
    { name: "xUnit", note: "[Fact] / [Theory]" },
    { name: "NUnit", note: "[Test]" },
    { name: "TUnit", note: "[Test]" },
    { name: "MSTest", note: "[TestMethod]" },
  ];
  return (
    <div className="grid grid-cols-2 gap-3">
      {frameworks.map((f, i) => (
        <motion.div
          key={f.name}
          initial={{ opacity: 0, rotateX: -25, y: 8 }}
          whileInView={{ opacity: 1, rotateX: 0, y: 0 }}
          viewport={{ once: true, amount: 0.4 }}
          transition={{ duration: 0.45, delay: i * 0.06, ease: "easeOut" }}
          style={{ transformPerspective: 800 }}
          className="border-cc-card-border bg-cc-surface/40 flex flex-col gap-1 rounded-lg border px-4 py-4"
        >
          <div className="flex items-center justify-between">
            <span className="text-cc-heading font-heading text-base font-semibold">
              {f.name}
            </span>
            <motion.span
              className="text-cc-accent"
              aria-hidden
              initial={{ scale: 0 }}
              whileInView={{ scale: 1 }}
              viewport={{ once: true, amount: 0.6 }}
              transition={{
                type: "spring",
                stiffness: 360,
                damping: 18,
                delay: 0.15 + i * 0.06,
              }}
            >
              <CheckIcon size={14} />
            </motion.span>
          </div>
          <span className="text-cc-ink-dim font-mono text-[11px]">
            {f.note}
          </span>
        </motion.div>
      ))}
      <div className="border-cc-card-border bg-cc-card-bg col-span-2 rounded-lg border px-4 py-3">
        <p className="text-cc-ink text-[12.5px] leading-relaxed">
          The assertion API is the same in every framework. Add the Cookie
          Crumble package, call MatchSnapshot, and the failure message points at
          the diff your runner of choice already knows how to surface.
        </p>
      </div>
    </div>
  );
}

function DogfoodedVisual() {
  const reduce = useReducedMotion();
  const products = [
    { name: "Hot Chocolate", role: "GraphQL server" },
    { name: "Fusion", role: "Federation gateway" },
    { name: "Mocha", role: "Distributed messaging" },
  ];
  return (
    <div className="grid items-center gap-6 sm:grid-cols-[auto_1fr]">
      <motion.div
        animate={reduce ? { y: 0 } : { y: [0, -4, 0, 4, 0] }}
        transition={
          reduce
            ? undefined
            : { duration: 6, repeat: Infinity, ease: "easeInOut" }
        }
        className="bg-cc-surface/40 border-cc-card-border flex h-32 w-32 items-center justify-center rounded-xl border"
      >
        <CookieCrumble className="h-24 w-auto" />
      </motion.div>
      <motion.ul
        initial="hidden"
        whileInView="show"
        viewport={{ once: true, amount: 0.3 }}
        variants={{
          hidden: {},
          show: { transition: { staggerChildren: 0.1, delayChildren: 0.1 } },
        }}
        className="flex flex-col gap-2.5"
      >
        {products.map((p) => (
          <motion.li
            key={p.name}
            variants={{
              hidden: { opacity: 0, x: 14 },
              show: {
                opacity: 1,
                x: 0,
                transition: { duration: 0.4, ease: "easeOut" },
              },
            }}
            className="border-cc-card-border bg-cc-surface/40 flex items-center justify-between rounded-lg border px-4 py-3"
          >
            <span className="text-cc-heading font-heading text-base font-semibold">
              {p.name}
            </span>
            <span className="text-cc-ink-dim font-mono text-[11px] tracking-wider uppercase">
              {p.role}
            </span>
          </motion.li>
        ))}
        <li className="text-cc-ink-dim text-[12.5px] leading-relaxed">
          Every product on the ChilliCream platform writes its assertions with
          Cookie Crumble. The library evolves under real production pressure.
        </li>
      </motion.ul>
    </div>
  );
}

// -----------------------------------------------------------------------------
// MIT band proof tile with tick-up.
// -----------------------------------------------------------------------------

interface ProofItemProps {
  readonly label: string;
  readonly value: string;
}

function ProofItem({ label, value }: ProofItemProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.5 }}
      transition={{ duration: 0.4, ease: "easeOut" }}
      className="flex flex-col gap-1"
    >
      <span className="text-cc-heading font-heading text-2xl font-semibold tracking-tight">
        <TickUp value={value} />
      </span>
      <span className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
        {label}
      </span>
    </motion.div>
  );
}

// -----------------------------------------------------------------------------
// Page view (default export).
// -----------------------------------------------------------------------------

export function CookieCrumbleV7View() {
  return (
    <>
      <Hero />
      <Capabilities />

      <MismatchReel />

      <FeatureRow
        id="formatters"
        index="01"
        eyebrow="GraphQL-aware formatters"
        title="The snapshot reads like the GraphQL response, not a dump."
        body="Cookie Crumble ships first-class formatters for Hot Chocolate's IExecutionResult and for GraphQLHttpResponse. Pass either type to MatchSnapshot and the snapshot file comes out as the request and the response, in a shape your reviewers can read. No custom serializers, no opt-in attributes."
        bullets={[
          "Native formatter for IExecutionResult covers data, errors, and extensions.",
          "Native formatter for GraphQLHttpResponse keeps status, headers, and body together.",
          "Falls back to a structural formatter for any other .NET object you assert on.",
        ]}
        visual={<FormattersVisual />}
      />

      <FeatureRow
        id="flavors"
        index="02"
        eyebrow="Inline, file, Markdown"
        title="Three snapshot shapes, one assertion API."
        body="Small assertions go inline so the expected output sits beside the test. Larger payloads land in a snapshot file next to the test. When a single test exercises several layers (request, response, projected events, audit log), MatchMarkdownSnapshot composes them into a single readable document instead of a bag of unrelated assertions."
        bullets={[
          "MatchInlineSnapshot keeps tiny assertions self-contained.",
          "MatchSnapshot writes to a snapshot file next to your test.",
          "MatchMarkdownSnapshot captures several shapes of state in one document.",
        ]}
        visual={<SnapshotFlavorsVisual />}
        reverse
      />

      <FeatureRow
        id="frameworks"
        index="03"
        eyebrow="Test framework"
        title="Drops into the .NET test runner you already use."
        body="The same MatchSnapshot, MatchInlineSnapshot, and MatchMarkdownSnapshot APIs work on top of xUnit, NUnit, TUnit, and MSTest. Cookie Crumble figures out the current test's name and namespace from the runner, names the snapshot file accordingly, and surfaces failures through the runner's normal channel."
        bullets={[
          "Same assertion API across xUnit, NUnit, TUnit, and MSTest.",
          "Snapshot file names are derived from the test method and class.",
          "Failures show up as ordinary test failures in your runner, IDE, and CI logs.",
        ]}
        visual={<FrameworkMatrixVisual />}
      />

      <FeatureRow
        id="dogfood"
        index="04"
        eyebrow="Dogfooded by the platform"
        title="Built so the team can test Hot Chocolate, Fusion, and Mocha."
        body="Cookie Crumble exists because the ChilliCream platform needed snapshot assertions that understand GraphQL. It backs the test suites for Hot Chocolate, Fusion, and Mocha, so every commit through those products exercises Cookie Crumble itself. Pick it up for your own .NET tests and you inherit that pressure."
        bullets={[
          "Used end-to-end across the ChilliCream platform's own test suites.",
          "Every Hot Chocolate, Fusion, and Mocha commit re-exercises Cookie Crumble.",
          "Equally useful for any .NET test that benefits from snapshots.",
        ]}
        visual={<DogfoodedVisual />}
        reverse
      />

      {/* MIT / open source band. */}
      <section
        aria-label="Open source"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="grid items-center gap-10 lg:grid-cols-12">
          <motion.div
            initial={{ opacity: 0, y: 16 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, amount: 0.3 }}
            transition={{ duration: 0.5, ease: "easeOut" }}
            className="lg:col-span-7"
          >
            <Eyebrow>MIT licensed</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Open source, dogfooded, free to use.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Cookie Crumble is released under the MIT license and developed in
              the open alongside the rest of the ChilliCream platform. Use it in
              commercial work, fork it, vendor it, audit it. The package, the
              issue tracker, and the release notes all live on GitHub.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </SolidButton>
              <OutlineButton href="/docs/cookiecrumble">
                Read the docs
              </OutlineButton>
            </div>
          </motion.div>
          <div className="lg:col-span-5">
            <div className="border-cc-card-border bg-cc-card-bg grid grid-cols-2 gap-6 rounded-xl border p-6">
              <ProofItem label="License" value="MIT" />
              <ProofItem label="Package" value="CookieCrumble" />
              <ProofItem label="Runtimes" value=".NET 8 and later" />
              <ProofItem
                label="Frameworks"
                value="xUnit + NUnit + TUnit + MSTest"
              />
              <ProofItem label="Formatters" value="GraphQL-aware" />
              <ProofItem label="Workflow" value="__mismatch__/" />
            </div>
          </div>
        </div>
      </section>

      {/* Closing CTA with the single brand-spectrum hairline. */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Get started</Eyebrow>
          <motion.h2
            initial={{ opacity: 0, y: 14 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, amount: 0.5 }}
            transition={{ duration: 0.55, ease: "easeOut" }}
            className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl"
          >
            Write the assertion. Read the GraphQL.
          </motion.h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            Add the Cookie Crumble package to your test project, call
            MatchSnapshot on an IExecutionResult or a GraphQLHttpResponse, and
            the next pull request diff reads like the API contract instead of a
            wall of property assertions.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <motion.div whileHover={{ scale: 1.02 }}>
              <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
            </motion.div>
            <motion.div whileHover={{ scale: 1.02 }}>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </motion.div>
          </div>
        </div>
      </section>
    </>
  );
}
