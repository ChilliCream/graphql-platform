"use client";

import { motion, useReducedMotion } from "motion/react";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// -----------------------------------------------------------------------------
// v8 "Crumb Cards".
//
// Concept: a snapshot is something you reveal. Closed, a Crumb Card is a small
// editor tab (filename, language pill, one-line tagline). On hover it crumbles
// open with a spring transition to expose the real C# snippet, the snapshot
// diff, or the bullet detail, and its accent hairline brightens. Nothing is
// scroll-driven. Cards fade and translate in once on first viewport entry.
//
// The single accent for this page is cc-accent teal (#5eead4). The brand
// spectrum gradient appears exactly once, on the closing CTA hairline.
// -----------------------------------------------------------------------------

const ACCENT = "#5eead4";

// Brand spectrum hairline, used at most once per screen, on the closing CTA.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// GitHub-dark token approximations, scoped to the inline code blocks. The rest
// of the page stays on cc-* tokens.
const C: Record<string, CSSProperties> = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  num: { color: "#79c0ff" },
  comment: { color: "#8b949e", fontStyle: "italic" },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
};

const SPRING = { type: "spring" as const, stiffness: 220, damping: 28 };

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

// A tab-bar header in the editor-tab idiom shared by every Crumb Card.
interface TabBarProps {
  readonly filename: string;
  readonly lang: string;
}

function TabBar({ filename, lang }: TabBarProps) {
  return (
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
      <span className="text-cc-ink-dim ml-3 truncate font-mono text-[11px]">
        {filename}
      </span>
      <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex shrink-0 items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
        {lang}
      </span>
    </div>
  );
}

function ChevronGlyph() {
  return (
    <svg aria-hidden viewBox="0 0 12 12" className="h-2.5 w-2.5" fill="none">
      <path
        d="M2 4 L6 8 L10 4"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

// A compact inline code block used inside the revealed body of a Crumb Card.
interface MiniCodeProps {
  readonly children: ReactNode;
}

function MiniCode({ children }: MiniCodeProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border py-3">
      {children}
    </div>
  );
}

// -----------------------------------------------------------------------------
// Hero Crumb Card: statically open. This is the canonical example, so it needs
// no hover. It pairs the xUnit test with the snapshot it produces. It still
// reveals once on first viewport entry.
// -----------------------------------------------------------------------------

function HeroCrumbCard() {
  const reduceMotion = useReducedMotion();
  return (
    <motion.div
      initial={reduceMotion ? false : { opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.3 }}
      transition={{ duration: 0.5, ease: "easeOut" }}
      className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl"
    >
      <span
        aria-hidden
        className="absolute inset-x-0 top-0 z-10 h-px"
        style={{ background: ACCENT }}
      />
      <TabBar filename="Catalog.Tests/ProductQueryTests.cs" lang="C#" />
      <div className="relative py-4">
        <CodeLine n={1}>
          <span style={C.kw}>using</span>{" "}
          <span style={C.plain}>CookieCrumble;</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>[</span>
          <span style={C.attr}>Fact</span>
          <span style={C.punct}>]</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.kw}>public async</span>{" "}
          <span style={C.type}>Task</span>{" "}
          <span style={C.fn}>Product_By_Id_Returns_Catalog_Shape</span>
          <span style={C.punct}>()</span>
        </CodeLine>
        <CodeLine n={4}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>{`{`}</span>
        </CodeLine>
        <CodeLine n={5}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.type}>IExecutionResult</span>{" "}
          <span style={C.plain}>result </span>
          <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
          <span style={C.plain}>server</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>ExecuteAsync</span>
          <span style={C.punct}>(query);</span>
        </CodeLine>
        <CodeLine n={6}>
          <span style={C.plain}>{`        `}</span>
          <span style={C.plain}>result</span>
          <span style={C.punct}>.</span>
          <span style={C.fn}>MatchSnapshot</span>
          <span style={C.punct}>();</span>
        </CodeLine>
        <CodeLine n={7}>
          <span style={C.plain}>{`    `}</span>
          <span style={C.punct}>{`}`}</span>
        </CodeLine>
      </div>
      <div className="border-cc-card-border bg-cc-code-header/60 border-t">
        <div className="text-cc-ink-dim flex items-center justify-between gap-4 px-4 py-2 font-mono text-[10.5px]">
          <span className="truncate">__snapshots__/ProductQueryTests.snap</span>
          <span className="text-cc-accent shrink-0">IExecutionResult</span>
        </div>
        <div className="py-3">
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
            <span style={C.num}>{`24.0`}</span>
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
        </div>
      </div>
    </motion.div>
  );
}

// -----------------------------------------------------------------------------
// Crumb Card: the hover-expand centerpiece primitive.
//
// Collapsed: tab-bar + index + one-line tagline at a uniform collapsed height.
// Hovered: the inner panel springs to a uniform expanded height and the
// revealed body (code + bullets) fades and slides in, while the accent hairline
// brightens from 30 percent to 100 percent. The "closed"/"open" variant state
// is held on a single inner motion.div (initial/animate "closed", whileHover
// "open") and inherited by the hairline, the height panel, and the revealed
// body via framer-motion variant propagation; those children declare variants
// only so they do not control their own state. Animation is hover-only and
// enter-once; there is no scroll coupling.
// -----------------------------------------------------------------------------

interface CrumbCardProps {
  readonly index: number;
  readonly indexLabel: string;
  readonly filename: string;
  readonly lang: string;
  readonly tagline: string;
  readonly body: ReactNode;
  readonly bullets: readonly string[];
}

// Inner panel heights. The tab-bar (49px tall) sits outside the animated panel,
// so the panel animates the remaining height to keep the card heights uniform.
const PANEL_COLLAPSED = 131;
const PANEL_EXPANDED = 391;

function CrumbCard({
  index,
  indexLabel,
  filename,
  lang,
  tagline,
  body,
  bullets,
}: CrumbCardProps) {
  const reduceMotion = useReducedMotion();

  return (
    <motion.article
      initial={reduceMotion ? false : "hidden"}
      whileInView="shown"
      viewport={{ once: true, amount: 0.2 }}
      transition={{ duration: 0.45, ease: "easeOut", delay: index * 0.06 }}
      variants={{
        hidden: { opacity: 0, y: 18 },
        shown: { opacity: 1, y: 0 },
      }}
      className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover group relative overflow-hidden rounded-xl border"
    >
      {/*
        Hover sub-tree. This inner motion.div owns the "closed"/"open" variant
        state and is the only element with whileHover here, so its children (the
        hairline, the height panel, and the revealed body) inherit "open" on
        hover via variant propagation. The children declare variants only; they
        must NOT set their own animate, or motion treats them as controlling
        their own variants and excludes them from this parent's propagation.
      */}
      <motion.div
        initial="closed"
        animate="closed"
        whileHover={reduceMotion ? undefined : "open"}
      >
        {/* Accent hairline: 30% at rest, brightens to 100% on hover. */}
        <motion.span
          aria-hidden
          className="absolute inset-x-0 top-0 z-10 h-px"
          variants={{ closed: { opacity: 0.3 }, open: { opacity: 1 } }}
          transition={{ duration: 0.25 }}
          style={{ background: ACCENT }}
        />
        <TabBar filename={filename} lang={lang} />
        <motion.div
          className="overflow-hidden"
          variants={{
            closed: { height: PANEL_COLLAPSED },
            open: { height: PANEL_EXPANDED },
          }}
          transition={SPRING}
        >
          <div className="flex h-full flex-col px-5 pt-5 pb-5">
            {/* Always-visible collapsed face: index + tagline. */}
            <div className="flex items-start gap-3">
              <span className="border-cc-card-border text-cc-ink-dim inline-flex h-6 shrink-0 items-center justify-center rounded-full border px-2 font-mono text-[11px] tabular-nums">
                {indexLabel}
              </span>
              <p className="text-cc-ink text-sm leading-relaxed">{tagline}</p>
            </div>
            <span className="text-cc-ink-dim mt-3 flex items-center gap-1.5 font-mono text-[10.5px] tracking-widest uppercase group-hover:hidden">
              <ChevronGlyph />
              Hover to open
            </span>
            {/* Revealed body: fades and slides in on hover. */}
            <motion.div
              className="mt-4 flex min-h-0 flex-1 flex-col"
              variants={{
                closed: { opacity: 0, y: 8 },
                open: { opacity: 1, y: 0 },
              }}
              transition={{ duration: 0.3, ease: "easeOut" }}
            >
              {body}
              <ul className="mt-4 flex flex-col gap-2">
                {bullets.map((b) => (
                  <li
                    key={b}
                    className="text-cc-ink flex items-start gap-2.5 text-[12.5px] leading-relaxed"
                  >
                    <span className="text-cc-accent mt-0.5 shrink-0">
                      <CheckIcon size={13} />
                    </span>
                    <span>{b}</span>
                  </li>
                ))}
              </ul>
            </motion.div>
          </div>
        </motion.div>
      </motion.div>
    </motion.article>
  );
}

// -----------------------------------------------------------------------------
// Revealed bodies, one per Crumb Card.
// -----------------------------------------------------------------------------

function FormattersBody() {
  return (
    <MiniCode>
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
      <CodeLine n={3}>
        <span style={C.type}>GraphQLHttpResponse</span>{" "}
        <span style={C.plain}>res </span>
        <span style={C.punct}>=</span> <span style={C.kw}>await</span>{" "}
        <span style={C.plain}>client</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>PostAsync</span>
        <span style={C.punct}>(req);</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={C.plain}>res</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>MatchSnapshot</span>
        <span style={C.punct}>();</span>
      </CodeLine>
    </MiniCode>
  );
}

function FlavorsBody() {
  return (
    <MiniCode>
      <CodeLine n={1}>
        <span style={C.plain}>result</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>MatchInlineSnapshot</span>
        <span style={C.punct}>(</span>
        <span style={C.str}>{`"..."`}</span>
        <span style={C.punct}>);</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={C.plain}>result</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>MatchSnapshot</span>
        <span style={C.punct}>();</span>{" "}
        <span style={C.comment}>{`// __snapshots__`}</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={C.type}>Snapshot</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>Create</span>
        <span style={C.punct}>()</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>Add</span>
        <span style={C.punct}>(result)</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={C.plain}>{`    `}</span>
        <span style={C.punct}>.</span>
        <span style={C.fn}>MatchMarkdownSnapshot</span>
        <span style={C.punct}>();</span>
      </CodeLine>
    </MiniCode>
  );
}

function MismatchBody() {
  return (
    <MiniCode>
      <CodeLine n={1}>
        <span style={C.comment}>{`# test run differs from committed`}</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={C.plain}>__mismatch__/</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={C.plain}>{`  `}</span>
        <span style={C.str}>ProductQueryTests.snap</span>{" "}
        <span style={C.comment}>{`# gitignored`}</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={C.comment}>{`# review the diff, then move into`}</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={C.plain}>__snapshots__/</span>{" "}
        <span style={C.comment}>{`# accepted on purpose`}</span>
      </CodeLine>
    </MiniCode>
  );
}

function FrameworksBody() {
  const frameworks = [
    { name: "xUnit", note: "[Fact] / [Theory]" },
    { name: "NUnit", note: "[Test]" },
    { name: "TUnit", note: "[Test]" },
    { name: "MSTest", note: "[TestMethod]" },
  ];
  return (
    <div className="grid grid-cols-2 gap-2">
      {frameworks.map((f) => (
        <div
          key={f.name}
          className="border-cc-card-border bg-cc-surface/40 flex items-center justify-between gap-2 rounded-lg border px-3 py-2"
        >
          <span className="text-cc-heading font-heading text-sm font-semibold">
            {f.name}
          </span>
          <span className="text-cc-ink-dim font-mono text-[10px]">
            {f.note}
          </span>
        </div>
      ))}
    </div>
  );
}

function DogfoodedBody() {
  const products = [
    { name: "Hot Chocolate", role: "GraphQL server" },
    { name: "Fusion", role: "Federation gateway" },
    { name: "Mocha", role: "Distributed messaging" },
  ];
  return (
    <ul className="flex flex-col gap-2">
      {products.map((p) => (
        <li
          key={p.name}
          className="border-cc-card-border bg-cc-surface/40 flex items-center justify-between rounded-lg border px-3 py-2"
        >
          <span className="text-cc-heading font-heading text-sm font-semibold">
            {p.name}
          </span>
          <span className="text-cc-ink-dim font-mono text-[10px] tracking-wider uppercase">
            {p.role}
          </span>
        </li>
      ))}
    </ul>
  );
}

// -----------------------------------------------------------------------------
// Inline assertion strip: three compact cards, one per snapshot flavor.
// -----------------------------------------------------------------------------

interface InlineFlavorCardProps {
  readonly label: string;
  readonly api: string;
  readonly children: ReactNode;
}

function InlineFlavorCard({ label, api, children }: InlineFlavorCardProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
      <div className="border-cc-card-border text-cc-ink-dim flex items-center justify-between border-b px-4 py-2 font-mono text-[11px]">
        <span>{label}</span>
        <span className="text-cc-accent">{api}</span>
      </div>
      <div className="py-3">{children}</div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Proof tile for the MIT band.
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

// The faint dotted snapshot-folder texture behind the Crumb Cards grid only.
// 16px dot grid, masked with a radial fade so the center reads darkest.
function SnapshotFolderTexture() {
  return (
    <svg
      aria-hidden
      className="pointer-events-none absolute inset-0 h-full w-full"
      preserveAspectRatio="none"
    >
      <defs>
        <pattern
          id="cc-v8-dots"
          width="16"
          height="16"
          patternUnits="userSpaceOnUse"
        >
          <circle cx="1" cy="1" r="1" fill="rgba(98, 116, 142, 0.7)" />
        </pattern>
        <radialGradient id="cc-v8-fade" cx="50%" cy="50%" r="62%">
          <stop offset="0%" stopColor="#fff" stopOpacity="1" />
          <stop offset="100%" stopColor="#fff" stopOpacity="0" />
        </radialGradient>
        <mask id="cc-v8-mask">
          <rect width="100%" height="100%" fill="url(#cc-v8-fade)" />
        </mask>
      </defs>
      <rect
        width="100%"
        height="100%"
        fill="url(#cc-v8-dots)"
        mask="url(#cc-v8-mask)"
        opacity="0.04"
      />
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export function ClientPage() {
  const crumbCards: readonly CrumbCardProps[] = [
    {
      index: 0,
      indexLabel: "01",
      filename: "Formatters.cs",
      lang: "C#",
      tagline:
        "The snapshot reads like the GraphQL response, not a serialized object graph.",
      body: <FormattersBody />,
      bullets: [
        "Native formatter for IExecutionResult covers data, errors, and extensions.",
        "Native formatter for GraphQLHttpResponse keeps status, headers, and body together.",
        "Falls back to a structural formatter for any other .NET object you assert on.",
      ],
    },
    {
      index: 1,
      indexLabel: "02",
      filename: "SnapshotFlavors.cs",
      lang: "C#",
      tagline: "Inline, file, and Markdown snapshots behind one assertion API.",
      body: <FlavorsBody />,
      bullets: [
        "MatchInlineSnapshot keeps tiny assertions self-contained.",
        "MatchSnapshot writes to a snapshot file next to your test.",
        "MatchMarkdownSnapshot captures several shapes of state in one document.",
      ],
    },
    {
      index: 2,
      indexLabel: "03",
      filename: "MismatchWorkflow.gitignore",
      lang: "workflow",
      tagline:
        "A __mismatch__ folder turns a failing snapshot into a code review.",
      body: <MismatchBody />,
      bullets: [
        "Failing snapshots land in __mismatch__/, never on top of the committed file.",
        "The folder is meant to be gitignored, so nothing accidental gets checked in.",
        "Updates become a deliberate review step, not a silent overwrite.",
      ],
    },
    {
      index: 3,
      indexLabel: "04",
      filename: "TestRunners.cs",
      lang: "C#",
      tagline: "The same assertion API drops into the runner you already use.",
      body: <FrameworksBody />,
      bullets: [
        "Same assertion API across xUnit, NUnit, TUnit, and MSTest.",
        "Snapshot file names are derived from the test method and class.",
        "Failures show up as ordinary test failures in your runner, IDE, and CI logs.",
      ],
    },
    {
      index: 4,
      indexLabel: "05",
      filename: "Dogfooded.md",
      lang: "platform",
      tagline:
        "Built so the team can test Hot Chocolate, Fusion, and Mocha with it.",
      body: <DogfoodedBody />,
      bullets: [
        "Used end-to-end across the ChilliCream platform's own test suites.",
        "Every Hot Chocolate, Fusion, and Mocha commit re-exercises Cookie Crumble.",
        "Equally useful for any .NET test that benefits from snapshots.",
      ],
    },
  ];

  return (
    <>
      {/* HERO: copy left, statically open Hero Crumb Card right. */}
      <section className="pt-12 pb-10 sm:pt-20 sm:pb-16">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-5">
            <Eyebrow>Snapshot testing for .NET</Eyebrow>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              Snapshots that open when you look at them.
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
              Cookie Crumble is the open-source snapshot library the ChilliCream
              team writes its own tests with. It ships native formatters for Hot
              Chocolate IExecutionResult and GraphQLHttpResponse, so the
              snapshot file reads like the GraphQL response itself. Inline,
              file, or Markdown. xUnit, NUnit, TUnit, or MSTest. MIT-licensed.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
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
                  Runtimes
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">.NET 8 and later</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  Frameworks
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">
                  xUnit, NUnit, TUnit, MSTest
                </dd>
              </div>
            </dl>
          </div>
          <div className="lg:col-span-7">
            <HeroCrumbCard />
          </div>
        </div>
      </section>

      {/* Capabilities ribbon. */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
        <ul className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm sm:grid-cols-3 lg:grid-cols-5">
          {[
            "GraphQL-aware formatters",
            "Inline + file + Markdown",
            "__mismatch__ workflow",
            "xUnit, NUnit, TUnit, MSTest",
            "Dogfooded by the platform",
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
      </section>

      {/* Crumb Cards grid: the centerpiece. */}
      <section className="relative py-20 sm:py-24">
        <SnapshotFolderTexture />
        <div className="relative">
          <Eyebrow>Hover to crumble open</Eyebrow>
          <h2 className="text-cc-heading font-heading mt-5 max-w-2xl text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            Five surfaces, one assertion API.
          </h2>
          <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
            Each card is a snapshot file at rest: a tab, a filename, a single
            line. Hover one and it crumbles open to show the real snippet, the
            workflow, or the runner notes underneath.
          </p>
          <div className="mt-10 grid items-start gap-5 lg:grid-cols-2">
            {crumbCards.map((card) => (
              <CrumbCard key={card.indexLabel} {...card} />
            ))}
          </div>
        </div>
      </section>

      {/* Inline assertion strip. */}
      <section
        aria-label="Snapshot flavors side by side"
        className="border-cc-card-border border-t py-16 sm:py-20"
      >
        <div className="grid gap-4 lg:grid-cols-3">
          <InlineFlavorCard label="Inline" api="MatchInlineSnapshot">
            <CodeLine n={1}>
              <span style={C.plain}>result</span>
              <span style={C.punct}>.</span>
              <span style={C.fn}>MatchInlineSnapshot</span>
              <span style={C.punct}>(</span>
            </CodeLine>
            <CodeLine n={2}>
              <span style={C.plain}>{`  `}</span>
              <span style={C.str}>{`"""`}</span>
            </CodeLine>
            <CodeLine n={3}>
              <span style={C.plain}>{`  `}</span>
              <span style={C.str}>{`{ "data": { "ping": "pong" } }`}</span>
            </CodeLine>
            <CodeLine n={4}>
              <span style={C.plain}>{`  `}</span>
              <span style={C.str}>{`"""`}</span>
              <span style={C.punct}>);</span>
            </CodeLine>
          </InlineFlavorCard>
          <InlineFlavorCard label="File" api="MatchSnapshot">
            <CodeLine n={1}>
              <span style={C.plain}>result</span>
              <span style={C.punct}>.</span>
              <span style={C.fn}>MatchSnapshot</span>
              <span style={C.punct}>();</span>
            </CodeLine>
            <CodeLine n={2}>
              <span style={C.comment}>{`// writes to`}</span>
            </CodeLine>
            <CodeLine n={3}>
              <span style={C.comment}>{`// __snapshots__/<test>.snap`}</span>
            </CodeLine>
            <CodeLine n={4}>
              <span style={C.plain}>&nbsp;</span>
            </CodeLine>
          </InlineFlavorCard>
          <InlineFlavorCard label="Markdown" api="MatchMarkdownSnapshot">
            <CodeLine n={1}>
              <span style={C.type}>Snapshot</span>
              <span style={C.punct}>.</span>
              <span style={C.fn}>Create</span>
              <span style={C.punct}>()</span>
            </CodeLine>
            <CodeLine n={2}>
              <span style={C.plain}>{`  `}</span>
              <span style={C.punct}>.</span>
              <span style={C.fn}>Add</span>
              <span style={C.punct}>(request, </span>
              <span style={C.str}>{`"Request"`}</span>
              <span style={C.punct}>)</span>
            </CodeLine>
            <CodeLine n={3}>
              <span style={C.plain}>{`  `}</span>
              <span style={C.punct}>.</span>
              <span style={C.fn}>Add</span>
              <span style={C.punct}>(result, </span>
              <span style={C.str}>{`"Result"`}</span>
              <span style={C.punct}>)</span>
            </CodeLine>
            <CodeLine n={4}>
              <span style={C.plain}>{`  `}</span>
              <span style={C.punct}>.</span>
              <span style={C.fn}>MatchMarkdownSnapshot</span>
              <span style={C.punct}>();</span>
            </CodeLine>
          </InlineFlavorCard>
        </div>
        <p className="text-cc-ink-dim mt-4 text-[12.5px] leading-relaxed">
          Same library, same call site, three shapes of output. Pick inline for
          tiny assertions, a file for larger payloads, and Markdown when one
          test exercises several layers at once.
        </p>
      </section>

      {/* MIT / open-source band as a wide horizontal card. */}
      <section
        aria-label="Open source"
        className="border-cc-card-border border-t py-20 sm:py-24"
      >
        <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8 sm:p-10">
          <div className="grid items-center gap-10 lg:grid-cols-12">
            <div className="lg:col-span-7">
              <Eyebrow>MIT licensed</Eyebrow>
              <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
                Open source, dogfooded, free to use.
              </h2>
              <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
                Cookie Crumble is released under the MIT license and developed
                in the open alongside the rest of the ChilliCream platform. Use
                it in commercial work, fork it, vendor it, audit it. The
                package, the issue tracker, and the release notes all live on
                GitHub.
              </p>
              <div className="mt-8 flex flex-wrap gap-3">
                <SolidButton href="https://github.com/ChilliCream/graphql-platform">
                  View on GitHub
                </SolidButton>
                <OutlineButton href="/docs/cookiecrumble">
                  Read the docs
                </OutlineButton>
              </div>
            </div>
            <div className="lg:col-span-5">
              <div className="border-cc-card-border bg-cc-surface/40 grid grid-cols-2 gap-6 rounded-xl border p-6">
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
        </div>
      </section>

      {/* Closing CTA. The single brand-spectrum hairline lives here. */}
      <section className="border-cc-card-border relative border-t py-20 sm:py-28">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="text-center">
          <Eyebrow>Get started</Eyebrow>
          <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
            Write the assertion. Read the GraphQL.
          </h2>
          <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            Add the Cookie Crumble package to your test project, call
            MatchSnapshot on an IExecutionResult or a GraphQLHttpResponse, and
            the next pull request diff reads like the API contract instead of a
            wall of property assertions.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <SolidButton href="/docs/cookiecrumble">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
        </div>
      </section>
    </>
  );
}
