"use client";

import { motion, useInView, useReducedMotion } from "framer-motion";
import NextLink from "next/link";
import {
  createContext,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* ------------------------------------------------------------------ */
/*  Help Console. Help reframed as a live developer console: a docked   */
/*  terminal panel logs each help path as you read about it. One accent: */
/*  cc-accent teal (#5eead4) for prompts, caret, highlighted border and  */
/*  buttons. Brand spectrum (cyan, violet, coral) appears exactly once,  */
/*  as the 1px hairline under the hero h1.                               */
/* ------------------------------------------------------------------ */

const CHAR_COMMAND_MS = 35;
const CHAR_OUTPUT_MS = 15;

/* ===========================  Ground-truth data  ==================== */

interface PathFeature {
  readonly label: string;
}

interface HelpPath {
  readonly index: string;
  readonly name: string;
  readonly price: string;
  readonly priceNote?: string;
  readonly pitch: string;
  readonly responseTime: string;
  readonly bestFor: string;
  readonly features: ReadonlyArray<PathFeature>;
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly highlight?: boolean;
  readonly badge?: string;
}

const PATHS: ReadonlyArray<HelpPath> = [
  {
    index: "01",
    name: "Community",
    price: "Free",
    pitch:
      "Ask in public, learn from thousands of GraphQL practitioners, and pay it forward when you can.",
    responseTime: "Best effort, community paced",
    bestFor: "Learning, sanity checks, sharing a repro",
    features: [
      { label: "Public Slack channel" },
      { label: "7000+ individuals" },
      { label: "Open GitHub discussions" },
      { label: "Searchable history" },
    ],
    ctaLabel: "Join Slack",
    ctaHref: "https://slack.chillicream.com/",
  },
  {
    index: "02",
    name: "Consultancy",
    price: "$300",
    priceNote: "per hour",
    pitch:
      "Book a 60 minute session with a ChilliCream expert. Bring a problem, leave with a direction.",
    responseTime: "Usually within a few business days",
    bestFor: "Urgent unblock, design review, second opinion",
    features: [
      { label: "Dedicated 60 min session" },
      { label: "One on one with an expert" },
      { label: "Architecture and review" },
      { label: "No long term contract" },
    ],
    ctaLabel: "Book a session",
    ctaHref: "https://calendly.com/chillicream/60min",
    highlight: true,
    badge: "Fastest answer",
  },
  {
    index: "03",
    name: "Support",
    price: "Custom",
    pitch:
      "An ongoing relationship for teams that ship GraphQL in production and need a partner on call.",
    responseTime: "Defined in your plan SLA",
    bestFor: "Production systems, regulated industries, scale",
    features: [
      { label: "Dedicated account manager" },
      { label: "Private Slack channel" },
      { label: "Email support" },
      { label: "Plan tailored to your team" },
    ],
    ctaLabel: "Check out plans",
    ctaHref: "/services/support",
  },
];

interface SelfServeChannel {
  readonly title: string;
  readonly description: string;
  readonly href: string;
  readonly external: boolean;
  readonly icon: ReactNode;
}

const SELF_SERVE: ReadonlyArray<SelfServeChannel> = [
  {
    title: "Docs",
    description:
      "Guides, recipes, and the full Hot Chocolate and Fusion reference.",
    href: "/docs",
    external: false,
    icon: <DocsGlyph />,
  },
  {
    title: "Blog",
    description: "Release notes, deep dives, and patterns from the team.",
    href: "/blog",
    external: false,
    icon: <BlogGlyph />,
  },
  {
    title: "Slack",
    description: "Live conversation with maintainers and 7000+ developers.",
    href: "https://slack.chillicream.com/",
    external: true,
    icon: <SlackGlyph />,
  },
  {
    title: "YouTube",
    description:
      "Workshops, talks, and walkthroughs from the ChilliCream team.",
    href: "https://www.youtube.com/c/ChilliCream",
    external: true,
    icon: <PlayGlyph />,
  },
  {
    title: "GitHub",
    description:
      "Source, issues, and discussions for graphql-platform on GitHub.",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    icon: <GitHubGlyph />,
  },
];

interface FaqItem {
  readonly question: string;
  readonly answer: ReactNode;
  readonly echo: string;
}

const FAQ: ReadonlyArray<FaqItem> = [
  {
    question: "Which option gets me unblocked fastest?",
    echo: "fastest path -> consultancy for a deadline, slack for open questions",
    answer: (
      <>
        For a defined problem with a deadline, Consultancy is the most reliable
        path. You book a 60 minute slot with a ChilliCream expert and walk in
        with a question. For lighter or open ended questions, Slack is faster
        than you might expect because the community is large and active.
      </>
    ),
  },
  {
    question: "What response time can I expect on Slack?",
    echo: "slack response -> best effort, quick in EU hours, no guarantee",
    answer: (
      <>
        Slack is best effort. Answers are usually quick during European working
        hours, but there is no guarantee. If your team has a hard deadline, do
        not rely on Slack alone. Book a Consultancy hour or move to a Support
        plan with a contractual SLA.
      </>
    ),
  },
  {
    question: "When should we move from Consultancy to a Support plan?",
    echo: "move to support -> when graphql is on a critical production path",
    answer: (
      <>
        Consultancy is great for one off questions and design reviews. A Support
        plan makes sense once GraphQL is on a critical path in production, you
        need a named contact, a private Slack channel, and a response time you
        can write into an internal runbook.
      </>
    ),
  },
  {
    question: "How do I escalate something urgent in production?",
    echo: "escalate -> support plan account manager, or book next slot + repro",
    answer: (
      <>
        Customers on a Support plan escalate through their dedicated account
        manager and private Slack channel, following the SLA in their plan.
        Without a Support plan, the fastest path is to book the next available
        Consultancy slot and post a clear repro in the public Slack in parallel.
      </>
    ),
  },
  {
    question: "Can ChilliCream help us design a schema or migration?",
    echo: "schema design -> yes, common consultancy topic, advisory for larger",
    answer: (
      <>
        Yes. Design reviews, schema audits, and migration planning are common
        Consultancy topics. For larger engagements, our{" "}
        <NextLink
          href="/services/advisory"
          className="text-cc-accent hover:text-cc-accent-hover underline"
        >
          advisory service
        </NextLink>{" "}
        wraps that work in a structured engagement with deliverables.
      </>
    ),
  },
  {
    question: "Is the community Slack the right place for bug reports?",
    echo: "bug reports -> triage in slack, then file on github for tracking",
    answer: (
      <>
        Slack is good for triage and reproductions. Once a bug is confirmed,
        please file it on{" "}
        <a
          href="https://github.com/ChilliCream/graphql-platform"
          target="_blank"
          rel="noopener noreferrer"
          className="text-cc-accent hover:text-cc-accent-hover underline"
        >
          GitHub
        </a>{" "}
        so it gets a tracking issue, a label, and a place to land the fix.
      </>
    ),
  },
];

/* ===========================  Terminal model  ====================== */

/**
 * A transcript line is either a typed command (rendered with a prompt) or its
 * output. Lines are static text. They are revealed character by character on
 * first enter-view, then frozen.
 */
interface TranscriptLine {
  readonly kind: "command" | "output";
  readonly text: string;
}

/**
 * A transcript block is the unit that a single prose section logs: one or more
 * command/output lines that appear together once that section enters view.
 */
interface TranscriptBlock {
  readonly id: string;
  readonly lines: ReadonlyArray<TranscriptLine>;
}

const BANNER_LINES: ReadonlyArray<TranscriptLine> = [
  { kind: "output", text: "chillicream-help v1.0" },
  { kind: "output", text: "type a path, get a receipt." },
];

const PATH_BLOCKS: ReadonlyArray<TranscriptBlock> = [
  {
    id: "path-01",
    lines: [
      { kind: "command", text: "curl -s https://slack.chillicream.com/join" },
      { kind: "output", text: "200 OK  community: 7000+ members, free" },
    ],
  },
  {
    id: "path-02",
    lines: [
      {
        kind: "command",
        text: 'curl -X POST calendly.com/chillicream/60min -d "slot=next"',
      },
      { kind: "output", text: "201 Created  consultancy hour booked, $300/hr" },
    ],
  },
  {
    id: "path-03",
    lines: [
      { kind: "command", text: "ssh support@chillicream.com" },
      {
        kind: "output",
        text: "welcome. SLA defined in your plan. account manager online.",
      },
    ],
  },
];

const SELF_SERVE_BLOCK: TranscriptBlock = {
  id: "self-serve",
  lines: [
    { kind: "command", text: "man chillicream" },
    {
      kind: "output",
      text: "docs(1)    guides, recipes, hot chocolate + fusion reference",
    },
    { kind: "output", text: "blog(1)    release notes and deep dives" },
    {
      kind: "output",
      text: "slack(1)   live chat with maintainers + 7000 developers",
    },
    { kind: "output", text: "youtube(1) workshops, talks, walkthroughs" },
    { kind: "output", text: "github(1)  source, issues, discussions" },
  ],
};

const FAQ_BLOCK: TranscriptBlock = {
  id: "faq",
  lines: [
    { kind: "command", text: "history | grep faq" },
    ...FAQ.map((item, i) => ({
      kind: "output" as const,
      text: `[${String(i + 1).padStart(2, "0")}:00] ${item.echo}`,
    })),
  ],
};

const CLOSING_BLOCK: TranscriptBlock = {
  id: "closing",
  lines: [{ kind: "output", text: "exit 0  session saved." }],
};

/* ----------  Transcript context: prose sections log into terminal  ---- */

interface TranscriptContextValue {
  /** Register a block as ready; the terminal types it next when its turn comes. */
  readonly activate: (id: string) => void;
  /** Ids that have entered view, in arrival order. */
  readonly active: ReadonlyArray<string>;
}

const TranscriptContext = createContext<TranscriptContextValue | null>(null);

function useTranscript(): TranscriptContextValue {
  const ctx = useContext(TranscriptContext);
  if (ctx === null) {
    throw new Error(
      "useTranscript must be used inside the transcript provider",
    );
  }
  return ctx;
}

/**
 * Sentinel placed inside a prose section. On first enter-view it activates its
 * matching terminal block. Uses useInView({ once: true }) so it is enter-view
 * driven, never coupled to scroll position.
 */
function SectionSentinel({ blockId }: { readonly blockId: string }) {
  const ref = useRef<HTMLSpanElement>(null);
  const inView = useInView(ref, { once: true, margin: "0px 0px -30% 0px" });
  const { activate } = useTranscript();

  useEffect(() => {
    if (inView) {
      activate(blockId);
    }
  }, [inView, blockId, activate]);

  return <span ref={ref} aria-hidden className="block h-px w-px" />;
}

/* ===========================  Page  ================================ */

export function ClientPage() {
  const [active, setActive] = useState<ReadonlyArray<string>>([]);

  function activate(id: string) {
    setActive((prev) => (prev.includes(id) ? prev : [...prev, id]));
  }

  return (
    <TranscriptContext.Provider value={{ activate, active }}>
      <div className="pb-24">
        <div className="mx-auto grid max-w-7xl grid-cols-1 gap-12 px-6 lg:grid-cols-12 lg:gap-10 lg:px-8">
          {/* Prose column */}
          <div className="space-y-20 pt-12 sm:pt-16 lg:col-span-7 lg:space-y-28">
            <HeroBlock />
            <PathsBlock />
            <SelfServeBlock />
            <FaqBlock />
            <ClosingBlock />
          </div>

          {/* Terminal column (desktop) */}
          <div className="hidden lg:col-span-5 lg:block">
            <div className="sticky top-24">
              <DottedBackdrop />
              <Terminal active={active} />
            </div>
          </div>
        </div>
      </div>
    </TranscriptContext.Provider>
  );
}

/* ===========================  Prose sections  ====================== */

function HeroBlock() {
  return (
    <section>
      <p className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
        Help
      </p>
      <h1 className="text-cc-heading font-heading text-hero mt-4">
        GraphQL help, on the record.
      </h1>
      <SpectrumHairline />
      <p className="text-cc-ink-dim lead mt-6 max-w-xl">
        Three honest paths to GraphQL help: a free community of 7000+
        practitioners, expert consultancy by the hour, and tailored support
        plans for production teams. Every path below is logged in the console on
        the right as you read.
      </p>
      <div className="mt-8 flex flex-wrap gap-3">
        <SolidButton href="https://calendly.com/chillicream/60min">
          Book a consultancy hour
        </SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Ask in Slack
        </OutlineButton>
      </div>
    </section>
  );
}

function PathsBlock() {
  return (
    <section aria-labelledby="paths-heading">
      <p className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
        Three paths
      </p>
      <h2
        id="paths-heading"
        className="text-cc-heading font-heading text-h2 mt-3"
      >
        Choose the help that matches the moment.
      </h2>
      <p className="text-cc-ink-dim mt-4 max-w-xl text-base sm:text-lg">
        Community for thinking out loud, Consultancy for getting unstuck this
        week, Support for teams that depend on GraphQL in production.
      </p>

      <div className="mt-12 space-y-px">
        {PATHS.map((path, i) => (
          <PathRow key={path.name} path={path} order={i} />
        ))}
      </div>

      <p className="text-cc-ink-dim mt-8 text-sm">
        Not sure which to pick? Start in{" "}
        <a
          href="https://slack.chillicream.com/"
          target="_blank"
          rel="noopener noreferrer"
          className="text-cc-accent hover:text-cc-accent-hover underline"
        >
          Slack
        </a>
        . If the problem outgrows it, a consultancy hour is one click away.
      </p>
    </section>
  );
}

function PathRow({
  path,
  order,
}: {
  readonly path: HelpPath;
  readonly order: number;
}) {
  const reduce = useReducedMotion();
  const Button = path.highlight ? SolidButton : OutlineButton;
  const rowClass = path.highlight
    ? "relative rounded-2xl border border-cc-accent/40 bg-cc-card-bg p-7 shadow-[0_0_0_1px_rgba(94,234,212,0.15),0_20px_60px_-30px_rgba(94,234,212,0.35)]"
    : "relative rounded-2xl border border-cc-card-border bg-cc-card-bg p-7 transition-colors hover:border-cc-card-border-hover";

  return (
    <motion.article
      className={rowClass}
      initial={reduce ? false : { opacity: 0, y: 8 }}
      whileInView={reduce ? undefined : { opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "0px 0px -15% 0px" }}
      transition={{ duration: 0.4, delay: order * 0.05 }}
    >
      <SectionSentinel blockId={`path-0${order + 1}`} />

      {path.badge ? (
        <span className="bg-cc-accent text-cc-surface absolute -top-3 left-7 rounded-full px-3 py-1 font-mono text-[10px] font-semibold tracking-widest uppercase">
          {path.badge}
        </span>
      ) : null}

      <div className="flex flex-wrap items-baseline gap-x-4 gap-y-1">
        <span className="text-cc-accent/70 font-mono text-sm font-semibold">
          {path.index}
        </span>
        <h3 className="text-cc-heading font-heading text-h4">{path.name}</h3>
        <span className="text-cc-heading ml-auto text-3xl font-semibold tracking-tight">
          {path.price}
        </span>
        {path.priceNote ? (
          <span className="text-cc-ink-dim text-sm">{path.priceNote}</span>
        ) : null}
      </div>

      <p className="text-cc-ink mt-4 text-sm leading-relaxed">{path.pitch}</p>

      <dl className="border-cc-card-border mt-5 grid gap-x-8 gap-y-3 border-y py-4 text-sm sm:grid-cols-2">
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
            Response
          </dt>
          <dd className="text-cc-ink mt-1">{path.responseTime}</dd>
        </div>
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
            Best for
          </dt>
          <dd className="text-cc-ink mt-1">{path.bestFor}</dd>
        </div>
      </dl>

      <ul className="mt-5 grid gap-x-8 gap-y-2 text-sm sm:grid-cols-2">
        {path.features.map((feature) => (
          <li
            key={feature.label}
            className="text-cc-ink flex items-start gap-2.5"
          >
            <span
              className={
                path.highlight ? "text-cc-accent mt-1" : "text-cc-success mt-1"
              }
            >
              <CheckIcon />
            </span>
            <span>{feature.label}</span>
          </li>
        ))}
      </ul>

      <div className="mt-6">
        <Button href={path.ctaHref}>{path.ctaLabel}</Button>
      </div>

      {/* Mobile transcript card for this path */}
      <MobileTranscriptCard block={PATH_BLOCKS[order]} />
    </motion.article>
  );
}

function SelfServeBlock() {
  return (
    <section aria-labelledby="self-serve-heading">
      <SectionSentinel blockId={SELF_SERVE_BLOCK.id} />
      <p className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
        First stop
      </p>
      <h2
        id="self-serve-heading"
        className="text-cc-heading font-heading text-h2 mt-3"
      >
        Try self serve before you raise a hand.
      </h2>
      <p className="text-cc-ink-dim mt-4 max-w-xl text-base sm:text-lg">
        Most questions have already been answered. Five places to look before
        you book a session.
      </p>

      <div className="mt-10 grid gap-4 sm:grid-cols-2">
        {SELF_SERVE.map((channel) => (
          <SelfServeCard key={channel.title} channel={channel} />
        ))}
      </div>

      <MobileTranscriptCard block={SELF_SERVE_BLOCK} />
    </section>
  );
}

function SelfServeCard({ channel }: { readonly channel: SelfServeChannel }) {
  const className =
    "group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full flex-col rounded-xl border p-5 transition-colors";

  const content = (
    <>
      <div className="text-cc-accent border-cc-card-border bg-cc-surface/60 mb-3 inline-flex h-9 w-9 items-center justify-center rounded-lg border">
        {channel.icon}
      </div>
      <h3 className="text-cc-heading font-heading text-h6">{channel.title}</h3>
      <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
        {channel.description}
      </p>
      <span className="text-cc-accent group-hover:text-cc-accent-hover mt-4 inline-flex items-center gap-2 text-sm font-medium">
        Open
        <ArrowGlyph />
      </span>
    </>
  );

  if (channel.external) {
    return (
      <a
        href={channel.href}
        target="_blank"
        rel="noopener noreferrer"
        className={className}
      >
        {content}
      </a>
    );
  }

  return (
    <NextLink href={channel.href} className={className}>
      {content}
    </NextLink>
  );
}

function FaqBlock() {
  return (
    <section aria-labelledby="faq-heading">
      <SectionSentinel blockId={FAQ_BLOCK.id} />
      <p className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
        FAQ
      </p>
      <h2
        id="faq-heading"
        className="text-cc-heading font-heading text-h2 mt-3"
      >
        Honest answers to the obvious questions.
      </h2>

      <div className="border-cc-card-border bg-cc-card-bg divide-cc-card-border mt-10 divide-y overflow-hidden rounded-2xl border">
        {FAQ.map((item, index) => (
          <details
            key={item.question}
            className="group"
            open={index === 0 ? true : undefined}
          >
            <summary className="text-cc-heading hover:bg-cc-surface/40 flex cursor-pointer list-none items-center justify-between gap-6 px-5 py-4 text-base font-medium transition-colors">
              <span>{item.question}</span>
              <span className="text-cc-ink-dim group-open:text-cc-accent shrink-0 transition-transform group-open:rotate-45">
                <PlusGlyph />
              </span>
            </summary>
            <div className="text-cc-ink-dim px-5 pb-5 text-sm leading-relaxed">
              {item.answer}
            </div>
          </details>
        ))}
      </div>

      <MobileTranscriptCard block={FAQ_BLOCK} />
    </section>
  );
}

function ClosingBlock() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-3xl border px-7 py-12 sm:px-10 sm:py-14">
      <SectionSentinel blockId={CLOSING_BLOCK.id} />
      <h2 className="text-cc-heading font-heading text-h2 max-w-md">
        Still not sure where to start?
      </h2>
      <p className="text-cc-ink-dim mt-4 max-w-md text-base sm:text-lg">
        Book a consultancy hour, bring whatever you have, and walk out with a
        direction. If we are not the right help, we will say so.
      </p>
      <div className="mt-7 flex flex-wrap gap-3">
        <SolidButton href="https://calendly.com/chillicream/60min">
          Book a consultancy hour
        </SolidButton>
        <OutlineButton href="/services/support">
          Explore support plans
        </OutlineButton>
      </div>

      <MobileTranscriptCard block={CLOSING_BLOCK} />
    </section>
  );
}

/* ===========================  Terminal  ============================ */

/**
 * Sticky terminal panel. Holds one growing transcript: the banner plus every
 * activated block, in arrival order. Each newly activated block types itself
 * out once, then freezes. A blinking caret sits at the current line.
 */
function Terminal({ active }: { readonly active: ReadonlyArray<string> }) {
  const blocksById = new Map<string, TranscriptBlock>(
    [...PATH_BLOCKS, SELF_SERVE_BLOCK, FAQ_BLOCK, CLOSING_BLOCK].map((b) => [
      b.id,
      b,
    ]),
  );
  const orderedBlocks = active
    .map((id) => blocksById.get(id))
    .filter((b): b is TranscriptBlock => b !== undefined);

  return (
    <div className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-xl border shadow-[0_30px_80px_-40px_rgba(94,234,212,0.12)]">
      {/* Title bar */}
      <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-3">
        <span className="block h-2.5 w-2.5 rounded-full bg-[#f0786a]/70" />
        <span className="block h-2.5 w-2.5 rounded-full bg-[#e7c14a]/70" />
        <span className="bg-cc-success/70 block h-2.5 w-2.5 rounded-full" />
        <span className="text-cc-nav-label ml-3 font-mono text-xs">
          chillicream@help ~
        </span>
      </div>

      {/* Body */}
      <div className="max-h-[70vh] overflow-y-auto px-4 py-4 font-mono text-[13px] leading-relaxed">
        {/* Banner: always present, types on mount */}
        <TypedBlock lines={BANNER_LINES} caret={orderedBlocks.length === 0} />

        {orderedBlocks.map((block, i) => (
          <div key={block.id} className="mt-3">
            <TypedBlock
              lines={block.lines}
              caret={i === orderedBlocks.length - 1}
            />
          </div>
        ))}
      </div>
    </div>
  );
}

/**
 * Types a sequence of lines character by character on mount, then freezes.
 * Commands reveal at 35ms/char, output at 15ms/char. When the caret prop is
 * true, a blinking caret trails the last visible character. Respects reduced
 * motion by rendering everything immediately with no blink.
 */
function TypedBlock({
  lines,
  caret,
}: {
  readonly lines: ReadonlyArray<TranscriptLine>;
  readonly caret: boolean;
}) {
  const reduce = useReducedMotion();
  const [lineIdx, setLineIdx] = useState(reduce ? lines.length : 0);
  const [charIdx, setCharIdx] = useState(0);

  useEffect(() => {
    if (reduce) {
      return;
    }
    if (lineIdx >= lines.length) {
      return;
    }
    const line = lines[lineIdx];
    if (charIdx < line.text.length) {
      const step = line.kind === "command" ? CHAR_COMMAND_MS : CHAR_OUTPUT_MS;
      const id = window.setTimeout(() => setCharIdx((c) => c + 1), step);
      return () => window.clearTimeout(id);
    }
    const id = window.setTimeout(() => {
      setLineIdx((l) => l + 1);
      setCharIdx(0);
    }, 120);
    return () => window.clearTimeout(id);
  }, [reduce, lineIdx, charIdx, lines]);

  const done = lineIdx >= lines.length;

  return (
    <div>
      {lines.map((line, i) => {
        if (i > lineIdx) {
          return null;
        }
        const visible =
          reduce || i < lineIdx ? line.text : line.text.slice(0, charIdx);
        const isCurrent = i === lineIdx && !done;
        return (
          <div key={i} className="break-words whitespace-pre-wrap">
            {line.kind === "command" ? (
              <span className="text-cc-accent select-none">$ </span>
            ) : null}
            <span
              className={
                line.kind === "command" ? "text-cc-heading" : "text-cc-ink"
              }
            >
              {visible}
            </span>
            {caret && (isCurrent || (done && i === lines.length - 1)) ? (
              <Caret blink={!reduce} />
            ) : null}
          </div>
        );
      })}
    </div>
  );
}

function Caret({ blink }: { readonly blink: boolean }) {
  return (
    <motion.span
      aria-hidden
      className="bg-cc-accent ml-0.5 inline-block h-[1em] w-[0.55ch] translate-y-[0.12em]"
      animate={blink ? { opacity: [1, 1, 0, 0] } : { opacity: 1 }}
      transition={
        blink
          ? {
              duration: 1,
              repeat: Infinity,
              ease: "linear",
              times: [0, 0.5, 0.5, 1],
            }
          : { duration: 0 }
      }
    />
  );
}

/* ----------  Mobile transcript card (no sticky, no scroll-lock)  ----- */

function MobileTranscriptCard({ block }: { readonly block: TranscriptBlock }) {
  return (
    <div className="border-cc-card-border bg-cc-surface mt-6 overflow-hidden rounded-lg border lg:hidden">
      <div className="border-cc-card-border flex items-center gap-2 border-b px-3 py-2">
        <span className="block h-2 w-2 rounded-full bg-[#f0786a]/70" />
        <span className="block h-2 w-2 rounded-full bg-[#e7c14a]/70" />
        <span className="bg-cc-success/70 block h-2 w-2 rounded-full" />
        <span className="text-cc-nav-label ml-2 font-mono text-[11px]">
          chillicream@help ~
        </span>
      </div>
      <div className="px-3 py-3 font-mono text-[12px] leading-relaxed">
        {block.lines.map((line, i) => (
          <div key={i} className="break-words whitespace-pre-wrap">
            {line.kind === "command" ? (
              <span className="text-cc-accent select-none">$ </span>
            ) : null}
            <span
              className={
                line.kind === "command" ? "text-cc-heading" : "text-cc-ink"
              }
            >
              {line.text}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}

/* ===========================  Decoration  ========================== */

/** The single allowed brand-spectrum usage: a hairline under the hero h1. */
function SpectrumHairline() {
  return (
    <div
      aria-hidden
      className="mt-5 h-px w-40 opacity-50"
      style={{
        background:
          "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
      }}
    />
  );
}

/** Dotted grid behind the terminal column, faded top and bottom. */
function DottedBackdrop() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute -inset-x-6 -inset-y-8 -z-10"
      style={{
        backgroundImage:
          "radial-gradient(circle, rgba(94,234,212,0.04) 1px, transparent 1px)",
        backgroundSize: "24px 24px",
        maskImage:
          "linear-gradient(to bottom, transparent, black 18%, black 82%, transparent)",
        WebkitMaskImage:
          "linear-gradient(to bottom, transparent, black 18%, black 82%, transparent)",
      }}
    />
  );
}

/* ===========================  Glyphs  ============================== */

function PlusGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      width="16"
      height="16"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
    >
      <path d="M3 8h10" />
      <path d="M8 3v10" />
    </svg>
  );
}

function ArrowGlyph() {
  return (
    <svg
      viewBox="0 0 16 16"
      width="14"
      height="14"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M3 8h10" />
      <path d="M9 4l4 4-4 4" />
    </svg>
  );
}

function DocsGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="18"
      height="18"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M6 3h8l4 4v14H6z" />
      <path d="M14 3v4h4" />
      <path d="M9 12h6" />
      <path d="M9 16h6" />
    </svg>
  );
}

function BlogGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="18"
      height="18"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="4" y="4" width="16" height="16" rx="2" />
      <path d="M8 9h8" />
      <path d="M8 13h8" />
      <path d="M8 17h5" />
    </svg>
  );
}

function SlackGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="18"
      height="18"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="3" y="10" width="8" height="4" rx="2" />
      <rect x="13" y="10" width="8" height="4" rx="2" />
      <rect x="10" y="3" width="4" height="8" rx="2" />
      <rect x="10" y="13" width="4" height="8" rx="2" />
    </svg>
  );
}

function PlayGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="18"
      height="18"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="3" y="5" width="18" height="14" rx="3" />
      <path d="M11 9.5v5l4-2.5z" fill="currentColor" stroke="none" />
    </svg>
  );
}

function GitHubGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="18"
      height="18"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M9 19c-4 1.2-4-2-6-2" />
      <path d="M15 21v-3.2c0-.9-.1-1.3-.6-1.8 2.8-.3 5.6-1.4 5.6-6.1a4.7 4.7 0 0 0-1.3-3.3 4.3 4.3 0 0 0-.1-3.2s-1.1-.3-3.6 1.3a12.4 12.4 0 0 0-6.4 0C5.1 1.1 4 1.4 4 1.4a4.3 4.3 0 0 0-.1 3.2A4.7 4.7 0 0 0 2.6 7.9c0 4.6 2.8 5.7 5.5 6.1-.4.4-.6.9-.6 1.6V21" />
    </svg>
  );
}
