import type { Metadata } from "next";
import NextLink from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Get Help With ChilliCream GraphQL",
  description:
    "Self-serve first. Search the GraphQL docs, ask in Slack, watch the videos, or open a GitHub issue. Escalate to a paid expert session only when you need to.",
  keywords: [
    "GraphQL help",
    "HotChocolate docs",
    "ChilliCream Slack",
    "GraphQL community",
    "GraphQL consultancy",
    "GraphQL support",
    "Nitro help",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    type: "website",
    title: "Get Help With ChilliCream GraphQL",
    description:
      "ChilliCream GraphQL help, self-serve first. Hot Chocolate docs, Slack, YouTube, GitHub, and blog answer most questions. Book a paid expert hour when needed.",
  },
};

// ---------------------------------------------------------------------------
// Data
// ---------------------------------------------------------------------------

interface Channel {
  readonly key: string;
  readonly label: string;
  readonly headline: string;
  readonly description: string;
  readonly href: string;
  readonly external: boolean;
  readonly meta: string;
  readonly Icon: () => ReactNode;
}

const CHANNELS: readonly Channel[] = [
  {
    key: "docs",
    label: "Docs",
    headline: "Read the docs",
    description:
      "Guides, schema cookbook, fetching strategies, source generators, Fusion, Nitro.",
    href: "/docs",
    external: false,
    meta: "Hundreds of guides",
    Icon: DocsIcon,
  },
  {
    key: "slack",
    label: "Slack",
    headline: "Ask the community",
    description:
      "7000+ developers in the ChilliCream Slack answer most questions within hours.",
    href: "https://slack.chillicream.com/",
    external: true,
    meta: "7000+ members",
    Icon: SlackIcon,
  },
  {
    key: "youtube",
    label: "YouTube",
    headline: "Watch deep dives",
    description:
      "Office hours, conference talks, and walkthroughs of every major Hot Chocolate release.",
    href: "https://www.youtube.com/c/ChilliCream",
    external: true,
    meta: "Recorded sessions",
    Icon: YouTubeIcon,
  },
  {
    key: "github",
    label: "GitHub",
    headline: "Search issues",
    description:
      "Browse closed issues and discussions. Reproduce a bug? File one with a minimal repro.",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    meta: "graphql-platform",
    Icon: GitHubIcon,
  },
  {
    key: "blog",
    label: "Blog",
    headline: "Read the deep dives",
    description:
      "Release notes, architecture deep dives, and migration write-ups straight from the maintainers.",
    href: "/blog",
    external: false,
    meta: "Release notes and write-ups",
    Icon: BlogIcon,
  },
];

interface Faq {
  readonly question: string;
  readonly answer: ReactNode;
}

const FAQS: readonly Faq[] = [
  {
    question: "Where should I look first when I'm stuck?",
    answer: (
      <>
        Start in the docs at <Inline href="/docs">/docs</Inline>. They cover the
        majority of recurring questions, including schema design, data loaders,
        Fusion composition, and Nitro. If the answer is not there, search the
        Slack archive and GitHub issues before opening anything new.
      </>
    ),
  },
  {
    question: "How fast does the community Slack respond?",
    answer:
      "There is no SLA on community Slack. In practice, a clear question with a code snippet usually gets a useful reply within a few hours during European or US business hours. Weekends and holidays are slower. If you need a guaranteed response window, that lives behind a paid Support plan.",
  },
  {
    question: "When does it make sense to book a paid session?",
    answer:
      "Book a Consultancy hour when you have a specific decision to unblock (schema design, Fusion rollout, performance triage, upgrade path) and you need a maintainer in the room. One focused 60-minute call is often cheaper than days of internal debate.",
  },
  {
    question: "What is the difference between Consultancy and Support?",
    answer:
      "Consultancy is per-hour expert time on demand, billed at $300 per hour and booked through Calendly. Support is an ongoing plan with a private Slack channel, an account manager, email support, and a defined response window. Pick Consultancy for one-off questions, Support for production systems.",
  },
  {
    question: "Can I report a bug without paying?",
    answer: (
      <>
        Yes. File a reproducible issue on{" "}
        <Inline href="https://github.com/ChilliCream/graphql-platform">
          GitHub
        </Inline>
        . A minimal repro (failing test, schema snippet, version) gets triaged
        faster than a description. Security issues should not be posted
        publicly. Reach out via Slack DM or a Support plan instead.
      </>
    ),
  },
  {
    question: "How do I escalate something urgent in production?",
    answer: (
      <>
        Without a Support plan there is no guaranteed escalation path. Post in
        Slack with the word &ldquo;production&rdquo; and a clear repro, and book
        a Consultancy hour at the next available slot. If incidents like this
        are recurring, a <Inline href="/services/support">Support plan</Inline>{" "}
        with a defined response window is the right shape.
      </>
    ),
  },
];

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function HelpPreviewV2Page() {
  return (
    <div className="pb-24">
      <Hero />
      <SelfServeChannels />
      <SearchPath />
      <HumanEscalation />
      <Faq />
      <ClosingCta />
    </div>
  );
}

// ---------------------------------------------------------------------------
// Hero
// ---------------------------------------------------------------------------

function Hero() {
  return (
    <section className="pt-12 pb-10 text-center sm:pt-20 sm:pb-14">
      <div className="text-cc-nav-label mb-4 font-mono text-xs font-semibold tracking-[0.18em] uppercase">
        Help, the self-serve way
      </div>
      <h1 className="font-heading text-cc-heading mx-auto max-w-3xl text-5xl leading-[1.05] font-semibold tracking-tight sm:text-6xl lg:text-7xl">
        Most answers are <BrandSweep>one search away</BrandSweep>.
      </h1>
      <p className="text-cc-prose mx-auto mt-6 max-w-2xl text-base sm:text-lg">
        The docs, Slack, YouTube, and GitHub answer the vast majority of
        questions about Hot Chocolate, Fusion, and Nitro. Try them first. When
        you genuinely need a human in the room, the paid paths are right below.
      </p>

      <div className="mx-auto mt-10 max-w-2xl">
        <DocsSearchLink />
      </div>

      <div className="text-cc-ink-dim mt-4 flex flex-wrap items-center justify-center gap-x-4 gap-y-2 font-mono text-xs">
        <span>Try:</span>
        <SuggestedSearch href="/docs/hotchocolate">
          /docs/hotchocolate
        </SuggestedSearch>
        <SuggestedSearch href="/docs/hotchocolate/fetching-data/batching/dataloader">
          dataloader
        </SuggestedSearch>
        <SuggestedSearch href="/docs/fusion">fusion</SuggestedSearch>
        <SuggestedSearch href="/docs/nitro">nitro</SuggestedSearch>
        <SuggestedSearch href="/blog">blog</SuggestedSearch>
      </div>
    </section>
  );
}

function DocsSearchLink() {
  return (
    <NextLink
      href="/docs"
      className="border-cc-card-border hover:border-cc-accent/60 group bg-cc-surface/70 focus-visible:outline-cc-accent flex items-center gap-4 rounded-2xl border px-5 py-4 text-left no-underline shadow-[0_8px_30px_-12px_rgba(0,0,0,0.6)] transition-colors focus-visible:outline focus-visible:outline-offset-2"
    >
      <span className="text-cc-accent flex-none" aria-hidden="true">
        <SearchGlyph />
      </span>
      <span className="flex flex-1 flex-col">
        <span className="text-cc-heading font-heading text-base font-semibold sm:text-lg">
          Open the GraphQL docs
        </span>
        <span className="text-cc-ink-dim text-sm">
          Guides, API reference, migration notes, runnable snippets.
        </span>
      </span>
      <span className="text-cc-nav-label hidden font-mono text-[0.65rem] tracking-[0.18em] uppercase sm:inline">
        /docs
      </span>
      <span
        className="text-cc-ink-dim group-hover:text-cc-accent flex-none transition-colors"
        aria-hidden="true"
      >
        <ArrowGlyph />
      </span>
    </NextLink>
  );
}

function SuggestedSearch({
  href,
  children,
}: {
  readonly href: string;
  readonly children: ReactNode;
}) {
  const className =
    "border-cc-card-border hover:border-cc-accent/60 hover:text-cc-accent rounded-full border px-3 py-1 no-underline transition-colors";
  const external = !href.startsWith("/") && !href.startsWith("#");
  if (external) {
    return (
      <a
        href={href}
        target="_blank"
        rel="noopener noreferrer"
        className={className}
      >
        {children}
      </a>
    );
  }
  return (
    <NextLink href={href} className={className}>
      {children}
    </NextLink>
  );
}

// ---------------------------------------------------------------------------
// Self-serve channels
// ---------------------------------------------------------------------------

function SelfServeChannels() {
  return (
    <section className="py-10 sm:py-14">
      <SectionHeader
        eyebrow="Free channels"
        title="Five places that answer almost everything"
        intro="Big tactile cards, not buried links. Pick the one that fits your question."
      />

      <div className="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {CHANNELS.map((channel, index) => (
          <ChannelCard key={channel.key} channel={channel} index={index} />
        ))}
      </div>
    </section>
  );
}

function ChannelCard({
  channel,
  index,
}: {
  readonly channel: Channel;
  readonly index: number;
}) {
  const { label, headline, description, href, external, meta, Icon } = channel;
  const className =
    "border-cc-card-border hover:border-cc-card-border-hover group bg-cc-card-bg hover:bg-cc-surface/80 relative flex h-full flex-col overflow-hidden rounded-3xl border p-6 no-underline transition-colors sm:p-8";

  const body = (
    <>
      <span
        aria-hidden="true"
        className="text-cc-nav-label absolute top-5 right-6 font-mono text-[0.65rem] tracking-[0.18em] uppercase"
      >
        {String(index + 1).padStart(2, "0")}
      </span>

      <span className="text-cc-accent mb-6 inline-flex h-12 w-12 items-center justify-center rounded-2xl bg-white/[0.04] ring-1 ring-white/[0.06]">
        <Icon />
      </span>

      <div className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        {label}
      </div>
      <h3 className="font-heading text-cc-heading mt-2 text-2xl font-semibold">
        {headline}
      </h3>
      <p className="text-cc-prose mt-3 text-sm leading-relaxed">
        {description}
      </p>

      <div className="border-cc-card-border mt-6 flex items-center justify-between border-t border-dashed pt-4">
        <span className="text-cc-nav-label font-mono text-xs">{meta}</span>
        <span className="text-cc-ink-dim group-hover:text-cc-accent inline-flex items-center gap-2 text-sm transition-colors">
          Open
          <ArrowGlyph />
        </span>
      </div>
    </>
  );

  if (external) {
    return (
      <a
        href={href}
        target="_blank"
        rel="noopener noreferrer"
        className={className}
      >
        {body}
      </a>
    );
  }

  return (
    <NextLink href={href} className={className}>
      {body}
    </NextLink>
  );
}

// ---------------------------------------------------------------------------
// Search path (lightweight rail of high-traffic destinations)
// ---------------------------------------------------------------------------

const SEARCH_HOTSPOTS: readonly {
  href: string;
  label: string;
  note: string;
}[] = [
  {
    href: "/docs/hotchocolate",
    label: "Hot Chocolate",
    note: "GraphQL server",
  },
  { href: "/docs/fusion", label: "Fusion", note: "Compose" },
  { href: "/docs/nitro", label: "Nitro", note: "Cockpit and CLI" },
  {
    href: "/docs/strawberryshake",
    label: "Strawberry Shake",
    note: ".NET client",
  },
  { href: "/blog", label: "Blog", note: "Release notes, deep dives" },
];

function SearchPath() {
  return (
    <section className="py-10 sm:py-14">
      <SectionHeader eyebrow="Jump in" title="Doc sections people open most" />
      <ul className="border-cc-card-border bg-cc-surface/60 mt-8 divide-y divide-white/[0.05] overflow-hidden rounded-2xl border">
        {SEARCH_HOTSPOTS.map((item, index) => (
          <li key={item.href}>
            <NextLink
              href={item.href}
              className="hover:bg-cc-hover group flex items-center gap-4 px-5 py-4 no-underline transition-colors sm:px-7"
            >
              <span className="text-cc-nav-label font-mono text-xs tracking-[0.18em] tabular-nums">
                {String(index + 1).padStart(2, "0")}
              </span>
              <span className="text-cc-heading font-heading flex-1 text-base font-semibold sm:text-lg">
                {item.label}
              </span>
              <span className="text-cc-ink-dim hidden font-mono text-xs sm:inline">
                {item.note}
              </span>
              <span className="text-cc-ink-dim group-hover:text-cc-accent transition-colors">
                <ArrowGlyph />
              </span>
            </NextLink>
          </li>
        ))}
      </ul>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Human escalation (Consultancy + Support)
// ---------------------------------------------------------------------------

function HumanEscalation() {
  return (
    <section className="py-14 sm:py-20">
      <SectionHeader
        eyebrow="When you need a human"
        title="Two paid paths, picked for the shape of the problem"
        intro="Free channels handle most things. These are the escalations when they do not."
      />

      <div className="mt-12 grid gap-6 lg:grid-cols-2">
        <EscalationCard
          tier="Consultancy"
          headline="One focused hour with a maintainer"
          price="$300"
          priceNote="per hour"
          summary="Bring a specific question. Schema design, Fusion rollout, performance triage, upgrade path. Walk out with a decision."
          perks={[
            "60-minute dedicated session",
            "Bring a maintainer on the call",
            "Async follow-up notes",
            "Book the next open slot in Calendly",
          ]}
          ctaLabel="Book a session"
          ctaHref="https://calendly.com/chillicream/60min"
          ctaStyle="solid"
        />

        <EscalationCard
          tier="Support"
          headline="An ongoing plan for production systems"
          price="Custom"
          priceNote="contact for pricing"
          summary="A private channel, a named account manager, email support, and a defined response window. The shape that fits a team behind a real product."
          perks={[
            "Dedicated account manager",
            "Private Slack channel",
            "Email support",
            "Defined response window",
          ]}
          ctaLabel="Check support plans"
          ctaHref="/services/support"
          ctaStyle="outline"
        />
      </div>

      <p className="text-cc-ink-dim mt-8 text-center text-sm">
        Not sure which fits? Start with a Consultancy hour. If the pattern
        repeats, a Support plan is the natural next step.
      </p>
    </section>
  );
}

interface EscalationCardProps {
  readonly tier: string;
  readonly headline: string;
  readonly price: string;
  readonly priceNote: string;
  readonly summary: string;
  readonly perks: readonly string[];
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly ctaStyle: "solid" | "outline";
}

function EscalationCard({
  tier,
  headline,
  price,
  priceNote,
  summary,
  perks,
  ctaLabel,
  ctaHref,
  ctaStyle,
}: EscalationCardProps) {
  const Cta = ctaStyle === "solid" ? SolidButton : OutlineButton;

  return (
    <article className="border-cc-card-border bg-cc-card-bg relative flex h-full flex-col overflow-hidden rounded-3xl border p-7 sm:p-9">
      <div className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        {tier}
      </div>
      <h3 className="font-heading text-cc-heading mt-3 text-2xl font-semibold sm:text-3xl">
        {headline}
      </h3>

      <div className="mt-5 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-4xl font-semibold sm:text-5xl">
          {price}
        </span>
        {priceNote && (
          <span className="text-cc-nav-label font-mono text-xs">
            {priceNote}
          </span>
        )}
      </div>

      <p className="text-cc-prose mt-5 text-sm leading-relaxed">{summary}</p>

      <ul className="mt-6 space-y-3">
        {perks.map((perk) => (
          <li key={perk} className="flex items-start gap-3">
            <span className="text-cc-accent mt-1 flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{perk}</span>
          </li>
        ))}
      </ul>

      <div className="mt-8">
        <Cta href={ctaHref} className="w-full">
          {ctaLabel}
        </Cta>
      </div>
    </article>
  );
}

// ---------------------------------------------------------------------------
// FAQ
// ---------------------------------------------------------------------------

function Faq() {
  return (
    <section className="py-14 sm:py-20">
      <SectionHeader
        eyebrow="Straight answers"
        title="Frequently asked, honestly answered"
      />

      <div className="border-cc-card-border bg-cc-surface/60 mt-10 divide-y divide-white/[0.06] overflow-hidden rounded-2xl border">
        {FAQS.map((faq) => (
          <details
            key={faq.question}
            className="group open:bg-cc-hover px-5 py-5 sm:px-8 sm:py-6"
          >
            <summary className="text-cc-heading font-heading flex cursor-pointer list-none items-start justify-between gap-4 text-base font-semibold sm:text-lg [&::-webkit-details-marker]:hidden">
              <span>{faq.question}</span>
              <span
                aria-hidden="true"
                className="text-cc-ink-dim group-open:text-cc-accent mt-1 flex-none transition-transform group-open:rotate-45"
              >
                <PlusGlyph />
              </span>
            </summary>
            <div className="text-cc-prose mt-3 text-sm leading-relaxed sm:text-base">
              {faq.answer}
            </div>
          </details>
        ))}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Closing CTA
// ---------------------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="py-14 sm:py-20">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-3xl border p-8 text-center sm:p-12">
        <div
          aria-hidden="true"
          className="from-cc-accent/10 pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r via-white/20 to-transparent"
        />
        <div className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Still stuck?
        </div>
        <h2 className="font-heading text-cc-heading mx-auto mt-3 max-w-2xl text-3xl font-semibold sm:text-4xl">
          Try Slack first. Book an hour if you need to.
        </h2>
        <p className="text-cc-prose mx-auto mt-4 max-w-xl text-sm sm:text-base">
          A clear question with a code snippet usually gets a useful reply from
          the community in hours. If it does not, a Consultancy hour gets a
          maintainer on the call.
        </p>
        <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
          <SolidButton href="https://slack.chillicream.com/">
            Join the Slack
          </SolidButton>
          <OutlineButton href="https://calendly.com/chillicream/60min">
            Book a Consultancy hour
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Shared bits
// ---------------------------------------------------------------------------

function SectionHeader({
  eyebrow,
  title,
  intro,
}: {
  readonly eyebrow: string;
  readonly title: string;
  readonly intro?: string;
}) {
  return (
    <div className="text-center">
      <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-[0.18em] uppercase">
        {eyebrow}
      </div>
      <h2 className="font-heading text-cc-heading mx-auto mt-3 max-w-3xl text-3xl font-semibold tracking-tight sm:text-4xl">
        {title}
      </h2>
      {intro && (
        <p className="text-cc-ink-dim mx-auto mt-3 max-w-2xl text-sm sm:text-base">
          {intro}
        </p>
      )}
    </div>
  );
}

function Inline({
  href,
  children,
}: {
  readonly href: string;
  readonly children: ReactNode;
}) {
  const external = !href.startsWith("/") && !href.startsWith("#");
  const linkProps = external
    ? { target: "_blank" as const, rel: "noopener noreferrer" }
    : {};
  return (
    <a
      href={href}
      {...linkProps}
      className="text-cc-accent hover:text-cc-accent-hover underline underline-offset-4"
    >
      {children}
    </a>
  );
}

function BrandSweep({ children }: { readonly children: ReactNode }) {
  // Single, rationed use of the brand spectrum on the page.
  return (
    <span
      className="bg-clip-text text-transparent"
      style={{
        backgroundImage:
          "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
      }}
    >
      {children}
    </span>
  );
}

// ---------------------------------------------------------------------------
// Inline SVG glyphs
// ---------------------------------------------------------------------------

function SearchGlyph() {
  return (
    <svg
      width="22"
      height="22"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <circle cx="10.5" cy="10.5" r="6.5" />
      <path d="m20 20-4.6-4.6" />
    </svg>
  );
}

function ArrowGlyph() {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 16 16"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M3 8h10" />
      <path d="m9 4 4 4-4 4" />
    </svg>
  );
}

function PlusGlyph() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 16 16"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      aria-hidden="true"
    >
      <path d="M8 3v10" />
      <path d="M3 8h10" />
    </svg>
  );
}

function DocsIcon() {
  return (
    <svg
      width="22"
      height="22"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M5 4h9l5 5v11a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1Z" />
      <path d="M14 4v5h5" />
      <path d="M8 13h8" />
      <path d="M8 17h6" />
    </svg>
  );
}

function SlackIcon() {
  return (
    <svg
      width="22"
      height="22"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <rect x="3" y="10" width="8" height="3" rx="1.5" />
      <rect x="13" y="10" width="3" height="8" rx="1.5" />
      <rect x="10" y="3" width="3" height="8" rx="1.5" />
      <rect x="13" y="13" width="8" height="3" rx="1.5" />
    </svg>
  );
}

function YouTubeIcon() {
  return (
    <svg
      width="22"
      height="22"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <rect x="3" y="6" width="18" height="12" rx="3" />
      <path d="m10 9 5 3-5 3z" fill="currentColor" stroke="none" />
    </svg>
  );
}

function BlogIcon() {
  return (
    <svg
      width="22"
      height="22"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M4 5h12a3 3 0 0 1 3 3v9a3 3 0 0 1-3 3H7a3 3 0 0 1-3-3V5Z" />
      <path d="M8 9h7" />
      <path d="M8 13h7" />
      <path d="M8 17h4" />
    </svg>
  );
}

function GitHubIcon() {
  return (
    <svg
      width="22"
      height="22"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M12 3a9 9 0 0 0-2.85 17.54c.45.08.62-.2.62-.43v-1.5c-2.5.55-3.03-1.2-3.03-1.2-.41-1.04-1-1.32-1-1.32-.82-.56.06-.55.06-.55.9.06 1.38.93 1.38.93.8 1.37 2.1.98 2.61.75.08-.58.31-.98.57-1.2-2-.23-4.1-1-4.1-4.45 0-.98.35-1.78.93-2.41-.09-.23-.4-1.16.09-2.41 0 0 .76-.24 2.49.92a8.6 8.6 0 0 1 4.53 0c1.73-1.16 2.49-.92 2.49-.92.5 1.25.18 2.18.09 2.41.58.63.93 1.43.93 2.41 0 3.46-2.1 4.22-4.1 4.44.32.28.61.83.61 1.67v2.48c0 .24.16.52.62.43A9 9 0 0 0 12 3Z" />
    </svg>
  );
}
