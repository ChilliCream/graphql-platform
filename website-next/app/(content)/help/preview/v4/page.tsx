import type { Metadata } from "next";
import NextLink from "next/link";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL help, pointed in the right direction",
  description:
    "GraphQL help, pointed in the right direction. Ask the ChilliCream community in Slack, book a consultancy hour, or pick a tailored support plan.",
  keywords: [
    "GraphQL help",
    "ChilliCream support",
    "Hot Chocolate consultancy",
    "GraphQL Slack community",
    "GraphQL support plan",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "GraphQL help, pointed in the right direction",
    description:
      "GraphQL help, pointed in the right direction. Ask the ChilliCream community in Slack, book a consultancy hour, or pick a tailored support plan.",
  },
};

const ACCENT = "#16b9e4";

interface Path {
  readonly name: string;
  readonly price: string;
  readonly priceNote?: string;
  readonly response: string;
  readonly bestFor: string;
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly dash: "solid" | "dashed" | "long";
  readonly highlight?: boolean;
  readonly badge?: string;
}

const PATHS: ReadonlyArray<Path> = [
  {
    name: "Community",
    price: "Free",
    response: "Best effort, community paced",
    bestFor: "Learning, sanity checks, sharing a repro",
    ctaLabel: "Join Slack",
    ctaHref: "https://slack.chillicream.com/",
    dash: "dashed",
  },
  {
    name: "Consultancy",
    price: "$300",
    priceNote: "per hour",
    response: "Usually within a few business days",
    bestFor: "Urgent unblock, design review, second opinion",
    ctaLabel: "Book a session",
    ctaHref: "https://calendly.com/chillicream/60min",
    dash: "solid",
    highlight: true,
    badge: "Fastest answer",
  },
  {
    name: "Support",
    price: "Custom",
    response: "Defined in your plan SLA",
    bestFor: "Production systems, regulated industries, scale",
    ctaLabel: "Check out plans",
    ctaHref: "/services/support",
    dash: "long",
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

interface UrgencyBullet {
  readonly situation: string;
  readonly path: "Community" | "Consultancy" | "Support";
}

const PICK_BY_URGENCY: ReadonlyArray<UrgencyBullet> = [
  { situation: "Production is on fire and you have an SLA.", path: "Support" },
  {
    situation: "You need a direction this week, not next month.",
    path: "Consultancy",
  },
  {
    situation: "You are exploring an idea or sanity checking a pattern.",
    path: "Community",
  },
];

const PICK_BY_STAGE: ReadonlyArray<UrgencyBullet> = [
  {
    situation: "Learning Hot Chocolate or writing your first resolver.",
    path: "Community",
  },
  {
    situation: "Reviewing a schema or planning a migration.",
    path: "Consultancy",
  },
  {
    situation: "Operating GraphQL in production with a named team.",
    path: "Support",
  },
];

interface FaqItem {
  readonly question: string;
  readonly answer: ReactNode;
}

const FAQ: ReadonlyArray<FaqItem> = [
  {
    question: "Which option gets me unblocked fastest?",
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
    answer: (
      <>
        Yes. Design reviews, schema audits, and migration planning are common
        Consultancy topics. For larger engagements, our{" "}
        <NextLink
          href="/services/advisory"
          className="underline"
          style={{ color: ACCENT }}
        >
          advisory service
        </NextLink>{" "}
        wraps that work in a structured engagement with deliverables.
      </>
    ),
  },
  {
    question: "Is the community Slack the right place for bug reports?",
    answer: (
      <>
        Slack is good for triage and reproductions. Once a bug is confirmed,
        please file it on{" "}
        <a
          href="https://github.com/ChilliCream/graphql-platform"
          target="_blank"
          rel="noopener noreferrer"
          className="underline"
          style={{ color: ACCENT }}
        >
          GitHub
        </a>{" "}
        so it gets a tracking issue, a label, and a place to land the fix.
      </>
    ),
  },
];

export default function HelpPreviewV4Page() {
  return (
    <div className="space-y-24 pb-24">
      <HelpHero />
      <div className="mx-auto max-w-6xl space-y-24 px-4 sm:px-6">
        <PathComparator />
        <SelfServeSection />
        <UrgencyGuide />
        <FaqSection />
        <ClosingCta />
      </div>
    </div>
  );
}

function HelpHero() {
  return (
    <section className="relative pt-12 sm:pt-20">
      <div className="mx-auto grid max-w-7xl items-center gap-12 px-4 sm:px-6 lg:grid-cols-12 lg:gap-8">
        <div className="lg:col-span-6">
          <div className="text-cc-nav-label mb-5 font-mono text-xs font-semibold tracking-widest uppercase">
            Help
          </div>
          <h1 className="text-cc-heading font-heading text-hero max-w-xl">
            GraphQL help, pointed in the{" "}
            <span style={{ color: ACCENT }}>right direction.</span>
          </h1>
          <p className="text-cc-ink-dim lead mt-6 max-w-xl">
            Three honest paths to an answer: a free Slack community, an hour
            with a Hot Chocolate maintainer, or a tailored support plan. Pick
            the one that matches the urgency, not the budget.
          </p>
          <div className="mt-10 flex flex-wrap gap-3">
            <SolidButton href="https://calendly.com/chillicream/60min">
              Book a consultancy hour
            </SolidButton>
            <OutlineButton href="https://slack.chillicream.com/">
              Ask in Slack
            </OutlineButton>
          </div>
          <div className="text-cc-ink-dim mt-8 flex flex-wrap items-center gap-x-6 gap-y-2 text-xs">
            <span className="inline-flex items-center gap-2">
              <span className="text-cc-status-healthy">
                <Dot color="currentColor" />
              </span>{" "}
              7000+ in Slack
            </span>
            <span className="inline-flex items-center gap-2">
              <Dot color={ACCENT} /> Hot Chocolate maintainers
            </span>
          </div>
        </div>
        <div className="lg:col-span-6">
          <div className="mx-auto w-full max-w-[520px]">
            <HelpCompass />
          </div>
        </div>
      </div>
    </section>
  );
}

function HelpCompass() {
  const ringIds = ["r1", "r2", "r3", "r4"];
  return (
    <svg
      viewBox="0 0 520 520"
      width="100%"
      height="auto"
      role="img"
      aria-label="GraphQL help compass with three spokes leading to Community, Consultancy, and Support."
      className="block"
    >
      <defs>
        <linearGradient id="cc-help-v4-hex" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor={ACCENT} stopOpacity="0.35" />
          <stop offset="100%" stopColor={ACCENT} stopOpacity="0.08" />
        </linearGradient>
      </defs>

      {/* concentric radar rings */}
      {ringIds.map((id, i) => (
        <circle
          key={id}
          cx="260"
          cy="260"
          r={70 + i * 50}
          fill="none"
          stroke="rgba(245,241,234,0.10)"
          strokeWidth="1"
        />
      ))}

      {/* faint cross hairs */}
      <line
        x1="260"
        y1="40"
        x2="260"
        y2="480"
        stroke="rgba(245,241,234,0.06)"
        strokeWidth="1"
      />
      <line
        x1="40"
        y1="260"
        x2="480"
        y2="260"
        stroke="rgba(245,241,234,0.06)"
        strokeWidth="1"
      />

      {/* three spokes, each a different dash pattern */}
      {/* Consultancy: solid (fastest, top right) */}
      <line
        x1="260"
        y1="260"
        x2="430"
        y2="120"
        stroke={ACCENT}
        strokeWidth="2"
        strokeLinecap="round"
      />
      {/* Community: dashed (left) */}
      <line
        x1="260"
        y1="260"
        x2="90"
        y2="260"
        stroke={ACCENT}
        strokeWidth="2"
        strokeLinecap="round"
        strokeDasharray="6 8"
        opacity="0.85"
      />
      {/* Support: long dash (bottom right) */}
      <line
        x1="260"
        y1="260"
        x2="420"
        y2="410"
        stroke={ACCENT}
        strokeWidth="2"
        strokeLinecap="round"
        strokeDasharray="18 10"
        opacity="0.85"
      />

      {/* central hexagon node (GraphQL-style) */}
      <Hex
        cx={260}
        cy={260}
        r={42}
        fill="url(#cc-help-v4-hex)"
        stroke={ACCENT}
        strokeWidth={1.5}
      />
      <text
        x="260"
        y="266"
        textAnchor="middle"
        className="font-mono"
        fontSize="11"
        letterSpacing="3"
        fill="#f5f0ea"
      >
        HELP
      </text>

      {/* orbit nodes */}
      <OrbitNode cx={90} cy={260} label="Community" sub="Slack" />
      <OrbitNode cx={430} cy={120} label="Consultancy" sub="60 min" />
      <OrbitNode cx={420} cy={410} label="Support" sub="SLA" />
    </svg>
  );
}

interface OrbitNodeProps {
  readonly cx: number;
  readonly cy: number;
  readonly label: string;
  readonly sub: string;
}

function OrbitNode({ cx, cy, label, sub }: OrbitNodeProps) {
  return (
    <g>
      <Hex
        cx={cx}
        cy={cy}
        r={22}
        fill="#0c1322"
        stroke={ACCENT}
        strokeWidth={1.5}
      />
      <text
        x={cx}
        y={cy - 36}
        textAnchor="middle"
        className="font-mono"
        fontSize="11"
        letterSpacing="2"
        fill="#f5f0ea"
      >
        {label.toUpperCase()}
      </text>
      <text
        x={cx}
        y={cy + 44}
        textAnchor="middle"
        className="font-mono"
        fontSize="10"
        letterSpacing="1.5"
        fill="rgba(245,241,234,0.62)"
      >
        {sub}
      </text>
    </g>
  );
}

interface HexProps {
  readonly cx: number;
  readonly cy: number;
  readonly r: number;
  readonly fill?: string;
  readonly stroke?: string;
  readonly strokeWidth?: number;
}

function Hex({ cx, cy, r, fill = "none", stroke, strokeWidth = 1 }: HexProps) {
  const pts: Array<string> = [];
  for (let i = 0; i < 6; i++) {
    const angle = (Math.PI / 3) * i - Math.PI / 2;
    const x = cx + r * Math.cos(angle);
    const y = cy + r * Math.sin(angle);
    pts.push(`${x.toFixed(2)},${y.toFixed(2)}`);
  }
  return (
    <polygon
      points={pts.join(" ")}
      fill={fill}
      stroke={stroke}
      strokeWidth={strokeWidth}
    />
  );
}

function HexGlyph({ dash }: { readonly dash: "solid" | "dashed" | "long" }) {
  const dashArray =
    dash === "solid" ? undefined : dash === "dashed" ? "2 3" : "5 3";
  return (
    <svg viewBox="0 0 20 20" width="16" height="16" aria-hidden>
      <Hex cx={10} cy={10} r={7} stroke={ACCENT} strokeWidth={1.25} />
      <line
        x1="10"
        y1="10"
        x2="17"
        y2="10"
        stroke={ACCENT}
        strokeWidth={1.25}
        strokeDasharray={dashArray}
        strokeLinecap="round"
      />
    </svg>
  );
}

function PathComparator() {
  return (
    <section aria-labelledby="paths-heading">
      <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
        Three paths
      </div>
      <h2
        id="paths-heading"
        className="text-cc-heading font-heading text-h2 max-w-2xl"
      >
        One row per path. No comparison gymnastics.
      </h2>
      <p className="text-cc-ink-dim mt-4 max-w-2xl text-base sm:text-lg">
        Community for thinking out loud, Consultancy for getting unstuck this
        week, Support for teams that depend on GraphQL in production.
      </p>

      {/* column headers, hidden on mobile */}
      <div className="border-cc-card-border text-cc-nav-label mt-12 hidden grid-cols-12 gap-4 border-b px-6 pb-3 font-mono text-[10px] font-semibold tracking-widest uppercase md:grid">
        <div className="col-span-3">Path</div>
        <div className="col-span-2">Price</div>
        <div className="col-span-3">Response</div>
        <div className="col-span-2">Best for</div>
        <div className="col-span-2 text-right">Action</div>
      </div>

      <ul className="border-cc-card-border divide-cc-card-border mt-0 divide-y border-b">
        {PATHS.map((path) => (
          <PathRow key={path.name} path={path} />
        ))}
      </ul>
    </section>
  );
}

function PathRow({ path }: { readonly path: Path }) {
  const Button = path.highlight ? SolidButton : OutlineButton;

  return (
    <li
      className={
        "bg-cc-card-bg/40 relative grid grid-cols-1 gap-4 border-l-2 px-6 py-6 md:grid-cols-12 md:items-center " +
        (path.highlight ? "border-[color:#16b9e4]" : "border-transparent")
      }
    >
      {/* Path label + glyph + badge */}
      <div className="md:col-span-3">
        <div className="flex items-center gap-3">
          <HexGlyph dash={path.dash} />
          <h3 className="text-cc-heading font-heading text-h6">{path.name}</h3>
        </div>
        {path.badge ? (
          <span
            className="mt-2 inline-block rounded-full px-2 py-0.5 font-mono text-[10px] font-semibold tracking-widest uppercase"
            style={{ backgroundColor: ACCENT, color: "#0c1322" }}
          >
            {path.badge}
          </span>
        ) : null}
      </div>

      {/* Price */}
      <div className="md:col-span-2">
        <div className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase md:hidden">
          Price
        </div>
        <div className="mt-1 flex items-baseline gap-1 md:mt-0">
          <span className="text-cc-heading text-2xl font-semibold tracking-tight">
            {path.price}
          </span>
          {path.priceNote ? (
            <span className="text-cc-ink-dim text-sm">{path.priceNote}</span>
          ) : null}
        </div>
      </div>

      {/* Response */}
      <div className="md:col-span-3">
        <div className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase md:hidden">
          Response
        </div>
        <p className="text-cc-ink mt-1 text-sm leading-relaxed md:mt-0">
          {path.response}
        </p>
      </div>

      {/* Best for */}
      <div className="md:col-span-2">
        <div className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase md:hidden">
          Best for
        </div>
        <p className="text-cc-ink mt-1 text-sm leading-relaxed md:mt-0">
          {path.bestFor}
        </p>
      </div>

      {/* CTA */}
      <div className="md:col-span-2 md:text-right">
        <Button href={path.ctaHref}>{path.ctaLabel}</Button>
      </div>
    </li>
  );
}

function SelfServeSection() {
  return (
    <section aria-labelledby="self-serve-heading">
      <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
        First stop
      </div>
      <h2
        id="self-serve-heading"
        className="text-cc-heading font-heading text-h2 max-w-2xl"
      >
        Try self serve first.
      </h2>
      <p className="text-cc-ink-dim mt-4 max-w-2xl text-base sm:text-lg">
        Most questions have already been answered. Five places to look before
        you book a session.
      </p>

      <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {SELF_SERVE.map((channel) => (
          <SelfServeCard key={channel.title} channel={channel} />
        ))}
      </div>
    </section>
  );
}

function SelfServeCard({ channel }: { readonly channel: SelfServeChannel }) {
  const className =
    "group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full flex-col rounded-xl border p-6 transition-colors";

  const content = (
    <>
      <div
        className="border-cc-card-border bg-cc-surface/60 mb-4 inline-flex h-10 w-10 items-center justify-center rounded-full border"
        style={{ color: ACCENT }}
      >
        {channel.icon}
      </div>
      <h3 className="text-cc-heading font-heading text-h6">{channel.title}</h3>
      <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
        {channel.description}
      </p>
      <span
        className="mt-6 inline-flex items-center gap-2 text-sm font-medium"
        style={{ color: ACCENT }}
      >
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

function UrgencyGuide() {
  return (
    <section aria-labelledby="urgency-heading">
      <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
        Compass legend
      </div>
      <h2
        id="urgency-heading"
        className="text-cc-heading font-heading text-h2 max-w-2xl"
      >
        Pick by urgency. Or pick by stage.
      </h2>
      <p className="text-cc-ink-dim mt-4 max-w-2xl text-base sm:text-lg">
        Two short maps from your situation back to the right spoke on the
        compass.
      </p>

      <div className="mt-12 grid gap-6 md:grid-cols-2">
        <UrgencyPanel title="Pick by urgency" items={PICK_BY_URGENCY} />
        <UrgencyPanel title="Pick by stage" items={PICK_BY_STAGE} />
      </div>
    </section>
  );
}

function UrgencyPanel({
  title,
  items,
}: {
  readonly title: string;
  readonly items: ReadonlyArray<UrgencyBullet>;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8">
      <h3 className="text-cc-heading font-heading text-h6">{title}</h3>
      <ul className="mt-6 space-y-5">
        {items.map((item) => (
          <li key={item.situation} className="flex items-start gap-4">
            <span className="mt-1 shrink-0" aria-hidden>
              <HexGlyph dash={dashForPath(item.path)} />
            </span>
            <div className="min-w-0">
              <p className="text-cc-ink text-sm leading-relaxed">
                {item.situation}
              </p>
              <p
                className="mt-1 font-mono text-[10px] font-semibold tracking-widest uppercase"
                style={{ color: ACCENT }}
              >
                {item.path}
              </p>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}

function dashForPath(name: UrgencyBullet["path"]): "solid" | "dashed" | "long" {
  if (name === "Consultancy") return "solid";
  if (name === "Community") return "dashed";
  return "long";
}

function FaqSection() {
  return (
    <section aria-labelledby="faq-heading">
      <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
        FAQ
      </div>
      <h2
        id="faq-heading"
        className="text-cc-heading font-heading text-h2 max-w-2xl"
      >
        Honest answers to the obvious questions.
      </h2>

      <div className="border-cc-card-border bg-cc-card-bg divide-cc-card-border mt-12 divide-y overflow-hidden rounded-2xl border">
        {FAQ.map((item, index) => (
          <details
            key={item.question}
            className="group"
            open={index === 0 ? true : undefined}
          >
            <summary className="text-cc-heading hover:bg-cc-surface/40 flex cursor-pointer list-none items-center justify-between gap-6 px-6 py-5 text-base font-medium transition-colors sm:text-lg">
              <span>{item.question}</span>
              <span className="text-cc-ink-dim shrink-0 transition-transform group-open:rotate-45 group-open:text-[color:#16b9e4]">
                <PlusGlyph />
              </span>
            </summary>
            <div className="text-cc-ink-dim px-6 pb-6 text-sm leading-relaxed sm:text-base">
              {item.answer}
            </div>
          </details>
        ))}
      </div>
    </section>
  );
}

function ClosingCta() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg rounded-2xl border px-8 py-16 sm:px-12 sm:py-20">
      <h2 className="text-cc-heading font-heading text-h2 max-w-2xl">
        Still not sure where to point?
      </h2>
      <p className="text-cc-ink-dim lead mt-4 max-w-xl">
        Book a consultancy hour, bring whatever you have, and walk out with a
        direction. If we are not the right help, we will say so.
      </p>
      <div className="mt-8 flex flex-wrap gap-3">
        <SolidButton href="https://calendly.com/chillicream/60min">
          Book a consultancy hour
        </SolidButton>
        <OutlineButton href="/services/support">
          Explore support plans
        </OutlineButton>
      </div>
    </section>
  );
}

function Dot({ color }: { readonly color: string }) {
  return (
    <svg viewBox="0 0 8 8" width="8" height="8" aria-hidden>
      <circle cx="4" cy="4" r="4" fill={color} />
    </svg>
  );
}

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
      width="20"
      height="20"
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
      width="20"
      height="20"
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
      width="20"
      height="20"
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
      width="20"
      height="20"
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
      width="20"
      height="20"
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
