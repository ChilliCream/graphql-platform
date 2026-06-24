import type { Metadata } from "next";
import NextLink from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Get GraphQL help fast",
  description:
    "Stuck on GraphQL? Get help from the ChilliCream community, book an expert consultancy hour, or pick a support plan. Pick the path that fits the urgency.",
  keywords: [
    "GraphQL help",
    "ChilliCream support",
    "Hot Chocolate consultancy",
    "GraphQL Slack community",
    "GraphQL support plan",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Get GraphQL help fast",
    description:
      "Stuck on GraphQL? Get help from the ChilliCream community, book an expert consultancy hour, or pick a support plan. Pick the path that fits the urgency.",
  },
};

interface TierFeature {
  readonly label: string;
}

interface Tier {
  readonly name: string;
  readonly price: string;
  readonly priceNote?: string;
  readonly pitch: string;
  readonly responseTime: string;
  readonly bestFor: string;
  readonly features: ReadonlyArray<TierFeature>;
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly highlight?: boolean;
  readonly badge?: string;
}

const TIERS: ReadonlyArray<Tier> = [
  {
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

export default function HelpPreviewV1Page() {
  return (
    <div className="space-y-24 pb-24">
      <HelpHero />
      <TiersSection />
      <SelfServeSection />
      <FaqSection />
      <ClosingCta />
    </div>
  );
}

function HelpHero() {
  return (
    <section className="pt-12 pb-4 text-center sm:pt-20">
      <div className="text-cc-nav-label mb-4 font-mono text-xs font-semibold tracking-widest uppercase">
        Help
      </div>
      <h1 className="text-cc-heading font-heading text-hero mx-auto max-w-4xl">
        Get unblocked, <span className="text-cc-accent">on your timeline</span>.
      </h1>
      <p className="text-cc-ink-dim lead mx-auto mt-6 max-w-2xl">
        Three honest paths to GraphQL help: a free community of 7000+
        practitioners, expert consultancy by the hour, and tailored support
        plans for production teams. Pick the one that matches the urgency.
      </p>
      <div className="mt-10 flex flex-wrap justify-center gap-3">
        <SolidButton href="https://calendly.com/chillicream/60min">
          Book a consultancy hour
        </SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Ask in Slack
        </OutlineButton>
      </div>
      <div className="text-cc-ink-dim mx-auto mt-6 flex max-w-md flex-wrap items-center justify-center gap-x-6 gap-y-2 text-xs">
        <span className="inline-flex items-center gap-2">
          <Dot className="text-cc-success" /> Built and supported by ChilliCream
        </span>
        <span className="inline-flex items-center gap-2">
          <Dot className="text-cc-accent" /> Maintainers of Hot Chocolate
        </span>
      </div>
    </section>
  );
}

function TiersSection() {
  return (
    <section aria-labelledby="tiers-heading">
      <div className="mx-auto max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Three paths
        </div>
        <h2 id="tiers-heading" className="text-cc-heading font-heading text-h2">
          Choose the help that matches the moment.
        </h2>
        <p className="text-cc-ink-dim mt-4 text-base sm:text-lg">
          Community for thinking out loud, Consultancy for getting unstuck this
          week, Support for teams that depend on GraphQL in production.
        </p>
      </div>

      <div className="mt-12 grid gap-6 md:grid-cols-3">
        {TIERS.map((tier) => (
          <TierCard key={tier.name} tier={tier} />
        ))}
      </div>

      <p className="text-cc-ink-dim mt-8 text-center text-sm">
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

function TierCard({ tier }: { readonly tier: Tier }) {
  const Button = tier.highlight ? SolidButton : OutlineButton;
  const cardClass = tier.highlight
    ? "relative flex flex-col rounded-2xl border border-cc-accent/40 bg-cc-card-bg p-8 shadow-[0_0_0_1px_rgba(94,234,212,0.15),0_20px_60px_-20px_rgba(94,234,212,0.35)]"
    : "relative flex flex-col rounded-2xl border border-cc-card-border bg-cc-card-bg p-8 transition-colors hover:border-cc-card-border-hover";

  return (
    <article className={cardClass}>
      {tier.badge ? (
        <div className="absolute -top-3 left-1/2 -translate-x-1/2">
          <span className="bg-cc-accent text-cc-surface rounded-full px-3 py-1 font-mono text-[10px] font-semibold tracking-widest uppercase">
            {tier.badge}
          </span>
        </div>
      ) : null}

      <header>
        <h3 className="text-cc-heading font-heading text-h4">{tier.name}</h3>
        <div className="mt-3 flex items-baseline gap-2">
          <span className="text-cc-heading text-4xl font-semibold tracking-tight">
            {tier.price}
          </span>
          {tier.priceNote ? (
            <span className="text-cc-ink-dim text-sm">{tier.priceNote}</span>
          ) : null}
        </div>
        <p className="text-cc-ink mt-4 text-sm leading-relaxed">{tier.pitch}</p>
      </header>

      <dl className="border-cc-card-border mt-6 grid gap-3 border-y py-5 text-sm">
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
            Response
          </dt>
          <dd className="text-cc-ink mt-1">{tier.responseTime}</dd>
        </div>
        <div>
          <dt className="text-cc-nav-label font-mono text-[10px] tracking-widest uppercase">
            Best for
          </dt>
          <dd className="text-cc-ink mt-1">{tier.bestFor}</dd>
        </div>
      </dl>

      <ul className="mt-6 space-y-3 text-sm">
        {tier.features.map((feature) => (
          <li
            key={feature.label}
            className="text-cc-ink flex items-start gap-3"
          >
            <span
              className={
                tier.highlight ? "text-cc-accent mt-1" : "text-cc-success mt-1"
              }
            >
              <CheckIcon />
            </span>
            <span>{feature.label}</span>
          </li>
        ))}
      </ul>

      <div className="mt-8 pt-2">
        <Button href={tier.ctaHref} className="w-full">
          {tier.ctaLabel}
        </Button>
      </div>
    </article>
  );
}

function SelfServeSection() {
  return (
    <section aria-labelledby="self-serve-heading">
      <div className="mx-auto max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          First stop
        </div>
        <h2
          id="self-serve-heading"
          className="text-cc-heading font-heading text-h2"
        >
          Try self serve before you raise a hand.
        </h2>
        <p className="text-cc-ink-dim mt-4 text-base sm:text-lg">
          Most questions have already been answered. Five places to look before
          you book a session.
        </p>
      </div>

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
      <div className="text-cc-accent border-cc-card-border bg-cc-surface/60 mb-4 inline-flex h-10 w-10 items-center justify-center rounded-lg border">
        {channel.icon}
      </div>
      <h3 className="text-cc-heading font-heading text-h6">{channel.title}</h3>
      <p className="text-cc-ink-dim mt-2 text-sm leading-relaxed">
        {channel.description}
      </p>
      <span className="text-cc-accent group-hover:text-cc-accent-hover mt-6 inline-flex items-center gap-2 text-sm font-medium">
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

function FaqSection() {
  return (
    <section aria-labelledby="faq-heading">
      <div className="mx-auto max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          FAQ
        </div>
        <h2 id="faq-heading" className="text-cc-heading font-heading text-h2">
          Honest answers to the obvious questions.
        </h2>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg divide-cc-card-border mx-auto mt-12 max-w-3xl divide-y overflow-hidden rounded-2xl border">
        {FAQ.map((item, index) => (
          <details
            key={item.question}
            className="group"
            open={index === 0 ? true : undefined}
          >
            <summary className="text-cc-heading hover:bg-cc-surface/40 flex cursor-pointer list-none items-center justify-between gap-6 px-6 py-5 text-base font-medium transition-colors sm:text-lg">
              <span>{item.question}</span>
              <span className="text-cc-ink-dim group-open:text-cc-accent shrink-0 transition-transform group-open:rotate-45">
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
    <section className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-3xl border px-8 py-16 text-center sm:px-12 sm:py-20">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-60"
      >
        <BrandSpectrum />
      </div>
      <div className="relative">
        <h2 className="text-cc-heading font-heading text-h2 mx-auto max-w-2xl">
          Still not sure where to start?
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl text-base sm:text-lg">
          Book a consultancy hour, bring whatever you have, and walk out with a
          direction. If we are not the right help, we will say so.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <SolidButton href="https://calendly.com/chillicream/60min">
            Book a consultancy hour
          </SolidButton>
          <OutlineButton href="/services/support">
            Explore support plans
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

function Dot({ className = "" }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 8 8"
      width="8"
      height="8"
      aria-hidden
      className={className}
    >
      <circle cx="4" cy="4" r="4" fill="currentColor" />
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

function BrandSpectrum() {
  return (
    <svg
      viewBox="0 0 1200 400"
      preserveAspectRatio="none"
      width="100%"
      height="100%"
    >
      <defs>
        <linearGradient
          id="cc-help-v1-spectrum"
          x1="0%"
          y1="50%"
          x2="100%"
          y2="50%"
        >
          <stop offset="0%" stopColor="#16b9e4" stopOpacity="0" />
          <stop offset="20%" stopColor="#16b9e4" stopOpacity="0.35" />
          <stop offset="50%" stopColor="#7c92c6" stopOpacity="0.45" />
          <stop offset="80%" stopColor="#f0786a" stopOpacity="0.35" />
          <stop offset="100%" stopColor="#f0786a" stopOpacity="0" />
        </linearGradient>
      </defs>
      <rect
        x="0"
        y="160"
        width="1200"
        height="80"
        fill="url(#cc-help-v1-spectrum)"
      />
    </svg>
  );
}
