import type { Metadata } from "next";
import NextLink from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

export const metadata: Metadata = {
  title: "Get GraphQL help fast",
  description:
    "GraphQL help, served three ways at the ChilliCream bar: free community Slack, a $300 expert consultancy hour, and tailored support plans for production teams.",
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
      "GraphQL help, served three ways at the ChilliCream bar: free community Slack, a $300 expert consultancy hour, and tailored support plans for production teams.",
  },
};

interface TierFeature {
  readonly label: string;
}

interface Tier {
  readonly name: string;
  readonly price: string;
  readonly priceNote?: string;
  readonly brewLabel: string;
  readonly brewLine: string;
  readonly todaysPour: string;
  readonly pitch: string;
  readonly responseTime: string;
  readonly bestFor: string;
  readonly features: ReadonlyArray<TierFeature>;
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly highlight?: boolean;
  readonly badge?: string;
  readonly icon: ReactNode;
}

const TIERS: ReadonlyArray<Tier> = [
  {
    name: "Community",
    price: "Free",
    brewLabel: "Pour-over",
    brewLine: "Slow and shared",
    todaysPour: "Today's pour",
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
    icon: <PourOver className="h-10 w-10" />,
  },
  {
    name: "Consultancy",
    price: "$300",
    priceNote: "per hour",
    brewLabel: "Espresso",
    brewLine: "One focused shot",
    todaysPour: "Today's pour",
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
    icon: <EspressoCupGlyph className="h-10 w-10" />,
  },
  {
    name: "Support",
    price: "Custom",
    brewLabel: "French press",
    brewLine: "Steeped relationship",
    todaysPour: "Today's pour",
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
    icon: <FrenchPress className="h-10 w-10" />,
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

export default function HelpPreviewV6Page() {
  return (
    <div className="space-y-24 pb-24">
      <HelpHero />
      <MenuSection />
      <SelfServeSection />
      <HouseBlendSection />
      <FaqSection />
      <ClosingCta />
    </div>
  );
}

function HelpHero() {
  return (
    <section className="pt-12 pb-4 text-center sm:pt-20">
      <div className="text-cc-nav-label mb-4 font-mono text-xs font-semibold tracking-widest uppercase">
        On the menu today
      </div>
      <h1 className="text-cc-heading font-heading text-hero mx-auto flex max-w-4xl flex-wrap items-center justify-center gap-x-4 gap-y-2">
        <SteamingCupMark className="text-cc-accent inline-block h-12 w-12 sm:h-14 sm:w-14" />
        <span>
          Get unblocked,{" "}
          <span className="text-cc-accent">on your timeline</span>.
        </span>
      </h1>
      <p className="text-cc-ink-dim lead mx-auto mt-6 max-w-2xl">
        Three brews of GraphQL help on the bar today: a pour-over community in
        Slack, an espresso shot of expert consultancy by the hour, and a
        french-press support plan steeped for production teams.
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
          <Dot className="text-cc-accent" /> Maintainers of Hot Chocolate
        </span>
        <span className="inline-flex items-center gap-2">
          <Dot className="text-cc-success" /> Built and supported by ChilliCream
        </span>
      </div>
    </section>
  );
}

function MenuSection() {
  return (
    <section aria-labelledby="menu-heading">
      <div className="mx-auto max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Today&apos;s menu
        </div>
        <h2 id="menu-heading" className="text-cc-heading font-heading text-h2">
          Three brews, one bar.
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
        Not sure which to pour? Start in{" "}
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
        <div className="flex items-center gap-3">
          <span
            className={
              tier.highlight
                ? "text-cc-accent shrink-0"
                : "text-cc-ink-dim shrink-0"
            }
          >
            {tier.icon}
          </span>
          <div className="min-w-0">
            <div className="text-cc-nav-label font-mono text-[10px] font-semibold tracking-widest uppercase">
              {tier.brewLabel}
            </div>
            <div className="text-cc-ink-dim mt-0.5 text-xs">
              {tier.brewLine}
            </div>
          </div>
        </div>

        <h3 className="text-cc-heading font-heading text-h4 mt-6">
          {tier.name}
        </h3>

        <div className="text-cc-nav-label mt-3 font-mono text-[10px] tracking-widest uppercase">
          {tier.todaysPour}
        </div>
        <div className="mt-1 flex items-baseline gap-2">
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
          Behind the bar
        </div>
        <h2
          id="self-serve-heading"
          className="text-cc-heading font-heading text-h2"
        >
          Try the self-serve counter first.
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

function HouseBlendSection() {
  return (
    <section aria-labelledby="house-blend-heading">
      <div className="mx-auto max-w-3xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          House blend
        </div>
        <h2
          id="house-blend-heading"
          className="text-cc-heading font-heading text-h3"
        >
          7000+ practitioners in Slack, maintainers of Hot Chocolate, every
          answer comes from someone who ships it.
        </h2>
      </div>

      <div className="mx-auto mt-10 grid max-w-3xl gap-4 sm:grid-cols-3">
        <StatTile value="7000+" label="In Slack" />
        <StatTile value="60 min" label="Consultancy slot" />
        <StatTile value="SLA" label="Backed support" />
      </div>
    </section>
  );
}

function StatTile({
  value,
  label,
}: {
  readonly value: string;
  readonly label: string;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6 text-center">
      <div className="text-cc-accent font-heading text-3xl font-semibold tracking-tight">
        {value}
      </div>
      <div className="text-cc-nav-label mt-2 font-mono text-[10px] tracking-widest uppercase">
        {label}
      </div>
    </div>
  );
}

function FaqSection() {
  return (
    <section aria-labelledby="faq-heading">
      <div className="mx-auto max-w-2xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Order questions
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
      <div
        aria-hidden
        className="text-cc-accent pointer-events-none absolute inset-0 flex justify-center opacity-25"
      >
        <SteamingLineMotif className="h-full w-40" />
      </div>
      <div className="relative">
        <h2 className="text-cc-heading font-heading text-h2 mx-auto max-w-2xl">
          Step up to the bar.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl text-base sm:text-lg">
          Book a 60 minute consultancy hour, bring whatever you have, and walk
          out with a direction. If we are not the right help, we will say so.
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

function SteamingCupMark({ className = "" }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 64 64"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="2.2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M14 14c1 3 -1 5 0 8" opacity="0.7" />
      <path d="M22 10c1 3 -1 5 0 8" opacity="0.85" />
      <path d="M30 14c1 3 -1 5 0 8" opacity="0.7" />
      <path d="M10 28h30v10a10 10 0 0 1 -10 10h-10a10 10 0 0 1 -10 -10z" />
      <path d="M40 32h6a5 5 0 0 1 0 10h-6" />
      <path d="M8 54h36" opacity="0.6" />
    </svg>
  );
}

function EspressoCupGlyph({ className = "" }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 64 64"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M20 10c1 3 -1 5 0 8" opacity="0.85" />
      <path d="M32 8c1 3 -1 5 0 8" opacity="0.85" />
      <path d="M14 28h30v8a10 10 0 0 1 -10 10h-10a10 10 0 0 1 -10 -10z" />
      <path d="M44 32h4a4 4 0 0 1 0 8h-4" />
      <path d="M12 52h34" opacity="0.6" />
    </svg>
  );
}

function SteamingLineMotif({
  className = "",
}: {
  readonly className?: string;
}) {
  return (
    <svg
      viewBox="0 0 80 400"
      preserveAspectRatio="none"
      aria-hidden
      fill="none"
      stroke="currentColor"
      strokeWidth="1.2"
      strokeLinecap="round"
      className={className}
    >
      <path d="M40 380 C 20 320, 60 280, 40 220 C 20 160, 60 120, 40 60 C 30 30, 50 10, 40 0" />
      <path
        d="M55 380 C 35 320, 75 280, 55 220 C 35 160, 75 120, 55 60"
        opacity="0.5"
      />
      <path
        d="M25 380 C 5 320, 45 280, 25 220 C 5 160, 45 120, 25 60"
        opacity="0.5"
      />
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
          id="cc-help-v6-spectrum"
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
        fill="url(#cc-help-v6-spectrum)"
      />
    </svg>
  );
}
