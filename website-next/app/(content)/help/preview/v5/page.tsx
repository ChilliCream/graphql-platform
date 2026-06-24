import type { Metadata } from "next";
import NextLink from "next/link";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL help, the long read",
  description:
    "GraphQL help on your timeline. Read the full story on community Slack, $300/hr expert consultancy, and tailored support plans from the ChilliCream team.",
  keywords: [
    "GraphQL help",
    "ChilliCream support",
    "Hot Chocolate consultancy",
    "GraphQL Slack community",
    "GraphQL support plan",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "GraphQL help, the long read",
    description:
      "GraphQL help on your timeline. Read the full story on community Slack, $300/hr expert consultancy, and tailored support plans from the ChilliCream team.",
  },
};

interface Tier {
  readonly index: string;
  readonly name: string;
  readonly price: string;
  readonly priceNote?: string;
  readonly pitch: string;
  readonly features: ReadonlyArray<string>;
  readonly bestFor: string;
  readonly response: string;
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly highlight?: boolean;
  readonly badge?: string;
}

const TIERS: ReadonlyArray<Tier> = [
  {
    index: "01",
    name: "Community",
    price: "Free",
    pitch:
      "Ask in public, learn from thousands of GraphQL practitioners, and pay it forward when you can.",
    features: [
      "Public Slack channel",
      "7000+ individuals",
      "Open GitHub discussions",
      "Searchable history",
    ],
    bestFor: "Learning, sanity checks, sharing a repro",
    response: "Best effort, community paced",
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
    features: [
      "Dedicated 60 min session",
      "One on one with an expert",
      "Architecture and review",
      "No long term contract",
    ],
    bestFor: "Urgent unblock, design review, second opinion",
    response: "Usually within a few business days",
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
    features: [
      "Dedicated account manager",
      "Private Slack channel",
      "Email support",
      "Plan tailored to your team",
    ],
    bestFor: "Production systems, regulated industries, scale",
    response: "Defined in your plan SLA",
    ctaLabel: "Check out plans",
    ctaHref: "/services/support",
  },
];

interface SelfServeEntry {
  readonly title: string;
  readonly description: string;
  readonly href: string;
  readonly external: boolean;
  readonly glyph: ReactNode;
}

const SELF_SERVE: ReadonlyArray<SelfServeEntry> = [
  {
    title: "Docs",
    description:
      "Guides, recipes, and the full Hot Chocolate and Fusion reference.",
    href: "/docs",
    external: false,
    glyph: <DocsGlyph />,
  },
  {
    title: "Blog",
    description: "Release notes, deep dives, and patterns from the team.",
    href: "/blog",
    external: false,
    glyph: <BlogGlyph />,
  },
  {
    title: "Slack",
    description: "Live conversation with maintainers and 7000+ developers.",
    href: "https://slack.chillicream.com/",
    external: true,
    glyph: <SlackGlyph />,
  },
  {
    title: "YouTube",
    description:
      "Workshops, talks, and walkthroughs from the ChilliCream team.",
    href: "https://www.youtube.com/c/ChilliCream",
    external: true,
    glyph: <PlayGlyph />,
  },
  {
    title: "GitHub",
    description:
      "Source, issues, and discussions for graphql-platform on GitHub.",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    glyph: <GitHubGlyph />,
  },
];

interface FaqItem {
  readonly index: string;
  readonly question: string;
  readonly answer: ReactNode;
}

const FAQ: ReadonlyArray<FaqItem> = [
  {
    index: "01.",
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
    index: "02.",
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
    index: "03.",
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
    index: "04.",
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
    index: "05.",
    question: "Can ChilliCream help us design a schema or migration?",
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
    index: "06.",
    question: "Is the community Slack the right place for bug reports?",
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

export default function HelpPreviewV5Page() {
  return (
    <div className="relative mx-auto w-full max-w-2xl px-6 pb-24 sm:px-8">
      {/* The spine: a 1px vertical hairline running the entire column. */}
      <div
        aria-hidden
        className="bg-cc-card-border pointer-events-none absolute top-0 bottom-0 left-0 w-px"
      />

      <HeroMovement />
      <PrologueMovement />
      <TierStation tier={TIERS[0]} />
      <TierStation tier={TIERS[1]} />
      <TierStation tier={TIERS[2]} />
      <InterludeQuote />
      <SelfServeMovement />
      <FaqMovement />
      <ClosingCoda />
    </div>
  );
}

function HeroMovement() {
  return (
    <section className="relative pt-16 pb-20 sm:pt-24 sm:pb-24">
      <div className="text-cc-nav-label -ml-6 font-mono text-xs font-semibold tracking-widest uppercase">
        Help
      </div>
      <h1 className="text-cc-heading font-heading text-hero mt-6 leading-[0.95]">
        Get unblocked,
        <br />
        <span className="text-cc-accent">on your timeline.</span>
      </h1>
      <p className="lead text-cc-ink-dim mt-8 leading-relaxed">
        Three honest paths to GraphQL help: a free community of 7000+
        practitioners, expert consultancy by the hour, and tailored support
        plans for production teams. Pick the one that matches the urgency.
      </p>
      <p className="text-cc-ink mt-10 text-base leading-relaxed">
        <NextLink
          href="https://calendly.com/chillicream/60min"
          className="text-cc-accent hover:text-cc-accent-hover underline-offset-4 hover:underline"
        >
          Book a consultancy hour
        </NextLink>
        <span className="text-cc-ink-dim mx-3" aria-hidden>
          &middot;
        </span>
        <NextLink
          href="https://slack.chillicream.com/"
          className="text-cc-accent hover:text-cc-accent-hover underline-offset-4 hover:underline"
        >
          Ask in Slack
        </NextLink>
      </p>
    </section>
  );
}

function PrologueMovement() {
  return (
    <section className="relative py-20 sm:py-24">
      <div className="text-cc-nav-label mb-6 -ml-6 font-mono text-xs font-semibold tracking-widest uppercase">
        Prologue
      </div>
      <p className="text-cc-heading font-heading text-h3 leading-snug">
        Three honest paths to GraphQL help, written down so the choice is
        obvious before you ever open a ticket.
      </p>
      <p className="text-cc-ink-dim text-body mt-6 leading-relaxed">
        Path one is Community, free and public. Path two is Consultancy, a
        single billable hour with a ChilliCream expert. Path three is Support,
        an ongoing partnership with an SLA. Read each station below, then pick
        the one that fits the moment.
      </p>
    </section>
  );
}

function TierStation({ tier }: { readonly tier: Tier }) {
  const Button = tier.highlight ? SolidButton : OutlineButton;
  const indexClass = tier.highlight ? "text-cc-accent" : "text-cc-ink-dim";

  return (
    <section
      className="relative py-20 sm:py-24"
      aria-labelledby={`station-${tier.index}`}
    >
      {tier.highlight ? (
        <div
          aria-hidden
          className="bg-cc-accent pointer-events-none absolute top-20 bottom-20 left-0 w-[2px] sm:top-24 sm:bottom-24"
        />
      ) : null}

      <div
        className={`-ml-6 font-mono text-5xl font-semibold tracking-tight sm:text-6xl ${indexClass}`}
      >
        {tier.index}
      </div>

      {tier.badge ? (
        <div className="mt-6">
          <span className="bg-cc-accent text-cc-bg inline-block rounded-full px-3 py-1 font-mono text-[10px] font-semibold tracking-widest uppercase">
            {tier.badge}
          </span>
        </div>
      ) : null}

      <h2
        id={`station-${tier.index}`}
        className="text-cc-heading font-heading text-h1 mt-6 leading-[0.95]"
      >
        {tier.name}
      </h2>
      <div className="mt-4 flex items-baseline gap-3">
        <span className="text-cc-heading text-h3 font-mono">{tier.price}</span>
        {tier.priceNote ? (
          <span className="text-cc-ink-dim text-sm">{tier.priceNote}</span>
        ) : null}
      </div>

      <p className="text-cc-ink lead mt-8 leading-relaxed">{tier.pitch}</p>

      <p className="text-cc-ink-dim text-body mt-6 leading-relaxed">
        {tier.features.join(", ")}.
      </p>

      <p className="text-cc-ink-dim mt-6 text-sm leading-relaxed">
        <span className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
          Best for
        </span>
        <span className="mx-2">&ndash;</span>
        <span>{tier.bestFor}.</span>
        <span className="mx-3" aria-hidden>
          &middot;
        </span>
        <span className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
          Response
        </span>
        <span className="mx-2">&ndash;</span>
        <span>{tier.response}.</span>
      </p>

      <div className="mt-10">
        <Button href={tier.ctaHref}>{tier.ctaLabel}</Button>
      </div>
    </section>
  );
}

function InterludeQuote() {
  return (
    <section className="relative py-24 sm:py-28">
      <div
        aria-hidden
        className="text-cc-accent font-heading absolute top-16 -left-2 text-7xl leading-none select-none sm:text-8xl"
      >
        &ldquo;
      </div>
      <blockquote className="text-cc-heading font-heading text-h3 pl-10 leading-snug italic">
        Start in Slack. If the problem outgrows it, a consultancy hour is one
        click away.
      </blockquote>
    </section>
  );
}

function SelfServeMovement() {
  return (
    <section className="relative py-20 sm:py-24" aria-labelledby="self-serve">
      <div className="text-cc-nav-label mb-6 -ml-6 font-mono text-xs font-semibold tracking-widest uppercase">
        Self serve
      </div>
      <h2
        id="self-serve"
        className="text-cc-heading font-heading text-h2 leading-tight"
      >
        Before you raise a hand.
      </h2>
      <p className="lead text-cc-ink-dim mt-6 leading-relaxed">
        Most questions have already been answered. Five places to look before
        you book a session, in the order we would reach for them ourselves.
      </p>

      <dl className="border-cc-card-border mt-12 space-y-5 border-t pt-8">
        {SELF_SERVE.map((entry) => (
          <SelfServeRow key={entry.title} entry={entry} />
        ))}
      </dl>
    </section>
  );
}

function SelfServeRow({ entry }: { readonly entry: SelfServeEntry }) {
  const titleNode = (
    <span className="text-cc-heading hover:text-cc-accent inline-flex items-center gap-2 font-medium transition-colors">
      <span className="text-cc-accent" aria-hidden>
        {entry.glyph}
      </span>
      {entry.title}
    </span>
  );

  return (
    <div className="text-body leading-relaxed">
      <dt className="inline">
        {entry.external ? (
          <a
            href={entry.href}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex"
          >
            {titleNode}
          </a>
        ) : (
          <NextLink href={entry.href} className="inline-flex">
            {titleNode}
          </NextLink>
        )}
      </dt>
      <span className="text-cc-ink-dim mx-3" aria-hidden>
        &ndash;
      </span>
      <dd className="text-cc-ink-dim inline">{entry.description}</dd>
    </div>
  );
}

function FaqMovement() {
  return (
    <section className="relative py-20 sm:py-24" aria-labelledby="faq">
      <div className="text-cc-nav-label mb-6 -ml-6 font-mono text-xs font-semibold tracking-widest uppercase">
        FAQ
      </div>
      <h2
        id="faq"
        className="text-cc-heading font-heading text-h2 leading-tight"
      >
        Honest answers.
      </h2>
      <p className="lead text-cc-ink-dim mt-6 leading-relaxed">
        Six questions we get most often, answered in the order they tend to come
        up.
      </p>

      <ol className="mt-12 space-y-12">
        {FAQ.map((item) => (
          <li key={item.question} className="text-cc-ink relative">
            <div className="text-cc-accent mb-3 -ml-6 font-mono text-sm tracking-widest">
              {item.index}
            </div>
            <h3 className="text-cc-heading font-heading text-h5 leading-snug">
              {item.question}
            </h3>
            <p className="text-cc-ink-dim text-body mt-4 leading-relaxed">
              {item.answer}
            </p>
          </li>
        ))}
      </ol>
    </section>
  );
}

function ClosingCoda() {
  return (
    <section className="relative pt-20 pb-12 sm:pt-24" aria-labelledby="coda">
      <div className="text-cc-nav-label mb-6 -ml-6 font-mono text-xs font-semibold tracking-widest uppercase">
        Coda
      </div>
      <h2
        id="coda"
        className="text-cc-heading font-heading text-h2 leading-tight"
      >
        Still not sure where to start?
      </h2>
      <p className="lead text-cc-ink-dim mt-6 leading-relaxed">
        Book a consultancy hour, bring whatever you have, and walk out with a
        direction. If we are not the right help, we will say so.
      </p>

      <div className="mt-10 flex flex-wrap items-center gap-4">
        <SolidButton href="https://calendly.com/chillicream/60min">
          Book a consultancy hour
        </SolidButton>
        <OutlineButton href="/services/support">
          Explore support plans
        </OutlineButton>
      </div>

      <p className="text-cc-nav-label mt-20 font-mono text-xs tracking-widest uppercase">
        Built and supported by ChilliCream
        <span className="text-cc-accent mx-2">*</span>
        Maintainers of Hot Chocolate
      </p>
    </section>
  );
}

function DocsGlyph() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="14"
      height="14"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
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
      width="14"
      height="14"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
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
      width="14"
      height="14"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
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
      width="14"
      height="14"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
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
      width="14"
      height="14"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M9 19c-4 1.2-4-2-6-2" />
      <path d="M15 21v-3.2c0-.9-.1-1.3-.6-1.8 2.8-.3 5.6-1.4 5.6-6.1a4.7 4.7 0 0 0-1.3-3.3 4.3 4.3 0 0 0-.1-3.2s-1.1-.3-3.6 1.3a12.4 12.4 0 0 0-6.4 0C5.1 1.1 4 1.4 4 1.4a4.3 4.3 0 0 0-.1 3.2A4.7 4.7 0 0 0 2.6 7.9c0 4.6 2.8 5.7 5.5 6.1-.4.4-.6.9-.6 1.6V21" />
    </svg>
  );
}
