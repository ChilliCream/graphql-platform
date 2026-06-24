import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Help: Find the Right Path Fast",
  description:
    "Stuck on GraphQL? Pick your path: Slack for quick questions, an hourly expert session for incidents, or a support plan with an SLA. Slack, Calendly, or custom.",
  keywords: [
    "GraphQL help",
    "GraphQL support",
    "Hot Chocolate help",
    "Hot Chocolate support",
    "GraphQL incident",
    "GraphQL Slack",
    "GraphQL consultancy",
  ],
  openGraph: {
    title: "Help: Find the Right Path Fast",
    description:
      "Stuck on GraphQL? Pick your path: Slack for quick questions, an hourly expert session for incidents, or a support plan with an SLA.",
  },
  robots: { index: false, follow: false },
};

/* ------------------------------------------------------------------ */
/*  Inline icons                                                       */
/* ------------------------------------------------------------------ */

interface IconProps {
  readonly className?: string;
}

function MessageIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={20}
      height={20}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M4 5h16v11H8l-4 4z" />
      <path d="M8 9h8M8 12h5" />
    </svg>
  );
}

function AlertIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={20}
      height={20}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M12 3 1.8 20h20.4z" />
      <path d="M12 10v5" />
      <circle cx={12} cy={18} r={0.6} fill="currentColor" stroke="none" />
    </svg>
  );
}

function BlueprintIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={20}
      height={20}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <rect x={3.5} y={4.5} width={17} height={15} rx={1.5} />
      <path d="M8 9h8M8 12h5M8 15h6" />
    </svg>
  );
}

function BookIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={20}
      height={20}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M4 5.5C4 4.7 4.7 4 5.5 4H11v15H5.5C4.7 19 4 18.3 4 17.5z" />
      <path d="M20 5.5C20 4.7 19.3 4 18.5 4H13v15h5.5c.8 0 1.5-.7 1.5-1.5z" />
    </svg>
  );
}

function CompassIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={20}
      height={20}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <circle cx={12} cy={12} r={9} />
      <path d="M14.5 9.5 11 11l-1.5 3.5L13 13z" />
    </svg>
  );
}

function ArrowRightIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={16}
      height={16}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M5 12h14" />
      <path d="m13 6 6 6-6 6" />
    </svg>
  );
}

function DocsIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={18}
      height={18}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M6 3h9l4 4v14H6z" />
      <path d="M14 3v5h5" />
      <path d="M9 13h7M9 16h5" />
    </svg>
  );
}

function BlogIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={18}
      height={18}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M4 5h12a4 4 0 0 1 4 4v10H8a4 4 0 0 1-4-4z" />
      <path d="M8 10h8M8 14h6" />
    </svg>
  );
}

function YouTubeIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={18}
      height={18}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <rect x={2.5} y={6} width={19} height={12} rx={3} />
      <path d="m10.5 9.5 5 2.5-5 2.5z" fill="currentColor" stroke="none" />
    </svg>
  );
}

function GitHubIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={18}
      height={18}
      fill="currentColor"
      className={className}
      aria-hidden="true"
    >
      <path d="M12 .5C5.65.5.5 5.65.5 12c0 5.1 3.3 9.4 7.9 10.9.6.1.8-.25.8-.55v-2c-3.2.7-3.9-1.5-3.9-1.5-.5-1.35-1.3-1.7-1.3-1.7-1.05-.7.1-.7.1-.7 1.15.1 1.75 1.2 1.75 1.2 1.05 1.8 2.75 1.3 3.4 1 .1-.75.4-1.3.75-1.6-2.55-.3-5.25-1.3-5.25-5.7 0-1.25.45-2.3 1.2-3.1-.15-.3-.55-1.5.1-3.1 0 0 1-.3 3.25 1.2.95-.25 1.95-.4 2.95-.4s2 .15 2.95.4c2.25-1.5 3.25-1.2 3.25-1.2.65 1.6.25 2.8.1 3.1.75.8 1.2 1.85 1.2 3.1 0 4.4-2.7 5.4-5.3 5.7.4.35.8 1.05.8 2.15v3.2c0 .3.2.65.8.55C20.2 21.4 23.5 17.1 23.5 12 23.5 5.65 18.35.5 12 .5z" />
    </svg>
  );
}

function SlackIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      width={18}
      height={18}
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <rect x={4} y={10} width={10} height={4} rx={2} />
      <rect x={10} y={4} width={4} height={10} rx={2} />
      <rect
        x={10}
        y={14}
        width={10}
        height={4}
        rx={2}
        transform="rotate(180 15 16)"
      />
      <rect
        x={14}
        y={10}
        width={4}
        height={10}
        rx={2}
        transform="rotate(180 16 15)"
      />
    </svg>
  );
}

/* ------------------------------------------------------------------ */
/*  Hero: 3-step routing                                              */
/* ------------------------------------------------------------------ */

interface StepCardProps {
  readonly index: number;
  readonly title: string;
  readonly body: string;
  readonly tone: "cyan" | "violet" | "coral";
}

const STEP_TONE: Record<StepCardProps["tone"], string> = {
  cyan: "text-[#16b9e4]",
  violet: "text-[#7c92c6]",
  coral: "text-[#f0786a]",
};

function StepCard({ index, title, body, tone }: StepCardProps) {
  return (
    <li className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex flex-col gap-3 rounded-2xl border p-6 backdrop-blur-sm transition-colors">
      <div className="flex items-center gap-3">
        <span
          className={`font-heading text-h4 leading-none font-bold ${STEP_TONE[tone]}`}
          aria-hidden="true"
        >
          {String(index).padStart(2, "0")}
        </span>
        <span className="caption text-cc-nav-label font-mono tracking-[0.18em] uppercase">
          Step {index}
        </span>
      </div>
      <h2 className="font-heading text-h6 text-cc-heading font-semibold">
        {title}
      </h2>
      <p className="text-body text-cc-prose">{body}</p>
    </li>
  );
}

function Hero() {
  return (
    <section className="relative pt-10 pb-16 sm:pt-16 sm:pb-20">
      <div className="flex flex-col gap-6">
        <span className="caption text-cc-accent font-mono tracking-[0.22em] uppercase">
          Help, fast
        </span>
        <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 font-bold">
          You are stuck.
          <br />
          <span className="text-cc-accent">Let us route you.</span>
        </h1>
        <p className="lead text-cc-prose max-w-3xl">
          Three steps. Pick the path that matches what you are facing right now,
          from a five-minute Slack question to a production incident on a
          Saturday night.
        </p>
        <div className="mt-2 flex flex-wrap gap-3">
          <SolidButton href="#pick-your-path">
            Pick my path
            <ArrowRightIcon className="ml-2" />
          </SolidButton>
          <OutlineButton href="https://slack.chillicream.com/">
            Ask in Slack first
          </OutlineButton>
        </div>
      </div>

      <ol className="mt-12 grid gap-4 sm:grid-cols-3">
        <StepCard
          index={1}
          tone="cyan"
          title="Describe your problem"
          body="A one-line question, an error message, a screenshot of a trace, a failing schema diff. Have it ready before you start the next step."
        />
        <StepCard
          index={2}
          tone="violet"
          title="Check the free channel"
          body="Docs, GitHub Issues, and the community Slack cover most questions. Most answers arrive within hours, often minutes during EU and US business hours."
        />
        <StepCard
          index={3}
          tone="coral"
          title="Escalate if urgent"
          body="Production down or revenue blocked? Book a paid expert session, or, if you already have a support plan, page your account contact."
        />
      </ol>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/*  Pick your path: 4-card decision grid                              */
/* ------------------------------------------------------------------ */

interface PathCardProps {
  readonly tag: string;
  readonly scenario: string;
  readonly action: string;
  readonly description: string;
  readonly meta: string;
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly tone: "calm" | "danger" | "design" | "learn";
  readonly Icon: (props: IconProps) => React.JSX.Element;
}

const TONE_RING: Record<PathCardProps["tone"], string> = {
  calm: "before:bg-cc-accent/40",
  danger: "before:bg-cc-status-firing/50",
  design: "before:bg-cc-info/40",
  learn: "before:bg-cc-status-investigating/40",
};

const TONE_TAG: Record<PathCardProps["tone"], string> = {
  calm: "text-cc-accent",
  danger: "text-cc-status-firing",
  design: "text-cc-info",
  learn: "text-cc-status-investigating",
};

function PathCard({
  tag,
  scenario,
  action,
  description,
  meta,
  ctaLabel,
  ctaHref,
  tone,
  Icon,
}: PathCardProps) {
  const isExternal = !ctaHref.startsWith("/") && !ctaHref.startsWith("#");
  return (
    <article
      className={`border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex h-full flex-col gap-4 overflow-hidden rounded-2xl border p-6 backdrop-blur-sm transition-colors before:absolute before:inset-x-0 before:top-0 before:h-px ${TONE_RING[tone]}`}
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-2">
          <Icon className={TONE_TAG[tone]} />
          <span
            className={`caption font-mono tracking-[0.18em] uppercase ${TONE_TAG[tone]}`}
          >
            {tag}
          </span>
        </div>
      </div>
      <div className="flex flex-col gap-2">
        <p className="caption text-cc-nav-label font-mono tracking-[0.18em] uppercase">
          Scenario
        </p>
        <p className="font-heading text-h6 text-cc-heading font-semibold">
          {scenario}
        </p>
      </div>
      <div className="flex flex-col gap-2">
        <p className="caption text-cc-nav-label font-mono tracking-[0.18em] uppercase">
          Best path
        </p>
        <p className="font-heading text-h5 text-cc-heading leading-tight font-bold">
          {action}
        </p>
        <p className="text-body text-cc-prose">{description}</p>
      </div>
      <p className="caption text-cc-ink-dim">{meta}</p>
      <div className="mt-auto pt-2">
        <a
          href={ctaHref}
          {...(isExternal
            ? { target: "_blank", rel: "noopener noreferrer" }
            : {})}
          className="text-cc-accent hover:text-cc-accent-hover inline-flex items-center gap-1.5 text-sm font-medium"
        >
          {ctaLabel}
          <ArrowRightIcon />
        </a>
      </div>
    </article>
  );
}

function PickYourPath() {
  return (
    <section
      id="pick-your-path"
      className="border-cc-card-border scroll-mt-24 border-t py-16 sm:py-20"
    >
      <div className="flex flex-col gap-4">
        <span className="caption text-cc-nav-label font-mono tracking-[0.22em] uppercase">
          Pick your path
        </span>
        <h2 className="font-heading text-h3 text-cc-heading font-bold">
          What does &ldquo;stuck&rdquo; look like for you?
        </h2>
        <p className="lead text-cc-prose max-w-3xl">
          Four common shapes a request takes. Match the closest one and click
          through.
        </p>
      </div>

      <div className="mt-10 grid gap-4 md:grid-cols-2">
        <PathCard
          tag="Quick question"
          scenario="A 5-minute question about Hot Chocolate, Strawberry Shake, or Fusion."
          action="Ask in community Slack"
          description="Free, 7000+ members, and most threads see a response from a maintainer or community member the same day."
          meta="Best for: how-do-I questions, config doubts, version checks."
          ctaLabel="Join community Slack"
          ctaHref="https://slack.chillicream.com/"
          tone="calm"
          Icon={MessageIcon}
        />
        <PathCard
          tag="Production incident"
          scenario="Something is on fire right now: a gateway is degraded, a schema rollout broke clients, latency spiked."
          action="Book a 60-min expert session"
          description="Direct call with a ChilliCream engineer at $300 per hour. If you already have a support plan, page your account contact instead."
          meta="Best for: live incidents, rollback decisions, hotfix triage."
          ctaLabel="Book on Calendly"
          ctaHref="https://calendly.com/chillicream/60min"
          tone="danger"
          Icon={AlertIcon}
        />
        <PathCard
          tag="Schema design review"
          scenario="You are about to commit to a schema shape, a Fusion composition strategy, or an authZ model and want a second pair of eyes."
          action="Engage advisory"
          description="Scoped review with a ChilliCream architect. Best done before the code lands, not after."
          meta="Best for: schema design, Fusion topology, performance plans."
          ctaLabel="See advisory services"
          ctaHref="/services/advisory"
          tone="design"
          Icon={BlueprintIcon}
        />
        <PathCard
          tag="I just want to learn"
          scenario="No incident, no deadline. You want to get good at GraphQL on .NET on your own time."
          action="Start with docs, blog, and YouTube"
          description="The docs cover the platform end to end. The blog and YouTube channel walk through real patterns. Bookmark this section."
          meta="Best for: onboarding, ramp-up, evaluating the stack."
          ctaLabel="Open the docs"
          ctaHref="/docs"
          tone="learn"
          Icon={CompassIcon}
        />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/*  First stop: self-serve channels                                   */
/* ------------------------------------------------------------------ */

interface FirstStopLinkProps {
  readonly href: string;
  readonly label: string;
  readonly note: string;
  readonly Icon: (props: IconProps) => React.JSX.Element;
}

function FirstStopLink({ href, label, note, Icon }: FirstStopLinkProps) {
  const isExternal = !href.startsWith("/");
  const className =
    "group flex items-center gap-4 rounded-xl border border-cc-card-border bg-cc-card-bg p-4 backdrop-blur-sm transition-colors hover:border-cc-card-border-hover";
  const inner = (
    <>
      <span className="border-cc-card-border text-cc-accent flex h-10 w-10 flex-none items-center justify-center rounded-lg border">
        <Icon />
      </span>
      <span className="flex flex-1 flex-col">
        <span className="font-heading text-h6 text-cc-heading font-semibold">
          {label}
        </span>
        <span className="caption text-cc-ink-dim">{note}</span>
      </span>
      <ArrowRightIcon className="text-cc-ink-dim group-hover:text-cc-accent transition-colors" />
    </>
  );

  if (isExternal) {
    return (
      <a
        href={href}
        target="_blank"
        rel="noopener noreferrer"
        className={className}
      >
        {inner}
      </a>
    );
  }
  return (
    <a href={href} className={className}>
      {inner}
    </a>
  );
}

function FirstStop() {
  return (
    <section className="border-cc-card-border border-t py-16 sm:py-20">
      <div className="flex flex-col gap-4">
        <span className="caption text-cc-nav-label font-mono tracking-[0.22em] uppercase">
          First stop, free
        </span>
        <h2 className="font-heading text-h3 text-cc-heading font-bold">
          Before you escalate, try these.
        </h2>
        <p className="lead text-cc-prose max-w-3xl">
          Most questions never need a paid channel. Search the docs, scan recent
          blog posts, or open an issue.
        </p>
      </div>

      <div className="mt-10 grid gap-3 sm:grid-cols-2">
        <FirstStopLink
          href="/docs"
          label="Documentation"
          note="API reference, guides, migration notes, and recipes."
          Icon={DocsIcon}
        />
        <FirstStopLink
          href="https://slack.chillicream.com/"
          label="Community Slack"
          note="7000+ members. Maintainers read every channel."
          Icon={SlackIcon}
        />
        <FirstStopLink
          href="https://github.com/ChilliCream/graphql-platform"
          label="GitHub Issues"
          note="Search existing issues, file a reproducible bug report."
          Icon={GitHubIcon}
        />
        <FirstStopLink
          href="/blog"
          label="Engineering blog"
          note="Release notes, deep dives, and design rationale."
          Icon={BlogIcon}
        />
        <FirstStopLink
          href="https://www.youtube.com/c/ChilliCream"
          label="YouTube channel"
          note="Walkthroughs, livestreams, and conference talks."
          Icon={YouTubeIcon}
        />
        <FirstStopLink
          href="/services/advisory"
          label="Advisory services"
          note="When a design choice deserves more than a Slack thread."
          Icon={BookIcon}
        />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/*  Compact tier strip: Community / Consultancy / Support             */
/* ------------------------------------------------------------------ */

interface TierProps {
  readonly name: string;
  readonly price: string;
  readonly priceNote?: string;
  readonly tagline: string;
  readonly perks: readonly string[];
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly highlight?: boolean;
  readonly badgeLabel?: string;
}

function Tier({
  name,
  price,
  priceNote,
  tagline,
  perks,
  ctaLabel,
  ctaHref,
  highlight,
  badgeLabel,
}: TierProps) {
  const borderCls = highlight
    ? "border-cc-accent/60 ring-1 ring-cc-accent/30"
    : "border-cc-card-border hover:border-cc-card-border-hover";
  return (
    <article
      className={`bg-cc-card-bg relative flex h-full flex-col gap-4 rounded-2xl border p-6 backdrop-blur-sm transition-colors ${borderCls}`}
    >
      {badgeLabel ? (
        <span className="bg-cc-accent text-cc-surface absolute -top-2.5 left-6 inline-flex items-center rounded-full px-3 py-0.5 text-xs font-semibold">
          {badgeLabel}
        </span>
      ) : null}
      <div className="flex items-baseline justify-between gap-2">
        <h3 className="font-heading text-h5 text-cc-heading font-bold">
          {name}
        </h3>
        <div className="flex items-baseline gap-1.5">
          <span className="font-heading text-h5 text-cc-heading font-bold">
            {price}
          </span>
          {priceNote ? (
            <span className="caption text-cc-ink-dim">{priceNote}</span>
          ) : null}
        </div>
      </div>
      <p className="text-body text-cc-prose">{tagline}</p>
      <ul className="flex flex-col gap-2">
        {perks.map((perk) => (
          <li
            key={perk}
            className="text-body text-cc-ink flex items-start gap-2"
          >
            <span className="text-cc-accent mt-1 flex-none">
              <CheckIcon />
            </span>
            <span>{perk}</span>
          </li>
        ))}
      </ul>
      <div className="mt-auto pt-2">
        <SolidButton href={ctaHref} className="w-full">
          {ctaLabel}
        </SolidButton>
      </div>
    </article>
  );
}

function Tiers() {
  return (
    <section className="border-cc-card-border border-t py-16 sm:py-20">
      <div className="flex flex-col gap-4">
        <span className="caption text-cc-nav-label font-mono tracking-[0.22em] uppercase">
          The three tiers
        </span>
        <h2 className="font-heading text-h3 text-cc-heading font-bold">
          Free, hourly, or on a plan.
        </h2>
        <p className="lead text-cc-prose max-w-3xl">
          These are the only channels. Everything above routes into one of them.
        </p>
      </div>

      <div className="mt-10 grid gap-4 lg:grid-cols-3">
        <Tier
          name="Community"
          price="Free"
          tagline="Be part of the community, get help, and help others. Best-effort response from maintainers and members."
          perks={[
            "Public Slack channel",
            "7000+ individuals",
            "Best-effort response time",
          ]}
          ctaLabel="Join Slack"
          ctaHref="https://slack.chillicream.com/"
        />
        <Tier
          name="Consultancy"
          price="$300"
          priceNote="per hour"
          tagline="You need immediate help and want to talk to an expert, no plan required."
          perks={[
            "Dedicated 60-min session",
            "Dedicated expert",
            "Booked via Calendly",
          ]}
          ctaLabel="Book a session"
          ctaHref="https://calendly.com/chillicream/60min"
          highlight
          badgeLabel="Hourly, no contract"
        />
        <Tier
          name="Support"
          price="Custom"
          tagline="Ongoing relationship with a defined response time, named contacts, and a private channel."
          perks={[
            "Dedicated account manager",
            "Private Slack channel",
            "E-mail support",
            "And more",
          ]}
          ctaLabel="Check out plans"
          ctaHref="/services/support"
        />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/*  FAQ                                                                */
/* ------------------------------------------------------------------ */

interface FaqItemProps {
  readonly question: string;
  readonly answer: string;
}

function FaqItem({ question, answer }: FaqItemProps) {
  return (
    <details className="group border-cc-card-border bg-cc-card-bg open:border-cc-card-border-hover hover:border-cc-card-border-hover rounded-xl border p-5 backdrop-blur-sm transition-colors">
      <summary className="text-cc-heading flex cursor-pointer list-none items-start justify-between gap-4">
        <span className="font-heading text-h6 font-semibold">{question}</span>
        <span
          className="border-cc-card-border text-cc-ink-dim mt-1 flex h-6 w-6 flex-none items-center justify-center rounded-full border transition-transform group-open:rotate-45"
          aria-hidden="true"
        >
          <svg
            viewBox="0 0 16 16"
            width={12}
            height={12}
            fill="none"
            stroke="currentColor"
            strokeWidth={1.6}
            strokeLinecap="round"
          >
            <path d="M8 3v10M3 8h10" />
          </svg>
        </span>
      </summary>
      <p className="text-body text-cc-prose mt-3">{answer}</p>
    </details>
  );
}

function Faq() {
  return (
    <section className="border-cc-card-border border-t py-16 sm:py-20">
      <div className="flex flex-col gap-4">
        <span className="caption text-cc-nav-label font-mono tracking-[0.22em] uppercase">
          Honest FAQ
        </span>
        <h2 className="font-heading text-h3 text-cc-heading font-bold">
          Before you panic, read this.
        </h2>
      </div>

      <div className="mt-10 flex flex-col gap-3">
        <FaqItem
          question="How fast do I get a reply on community Slack?"
          answer="Best effort. Many threads get a maintainer response within hours during EU and US business hours, but there is no guarantee. If you need a guaranteed response time, use Consultancy or a Support plan."
        />
        <FaqItem
          question="My production is down right now. What do I do?"
          answer="If you have a Support plan, contact your account manager through the channels in your contract. Otherwise, book the next available Calendly slot for a Consultancy session and post a Slack thread in parallel so the community can help while you wait."
        />
        <FaqItem
          question="What is the difference between Consultancy and Support?"
          answer="Consultancy is an ad-hoc, hourly session: one problem, one call, no ongoing relationship. Support is a contract: a named account manager, a private Slack channel, e-mail support, and a defined response time. Pick Consultancy for a one-off, Support for recurring needs."
        />
        <FaqItem
          question="When should I use Advisory instead of Consultancy?"
          answer="Advisory is for design reviews and architecture work that happens before code lands: schema shape, Fusion topology, auth model, performance plan. Consultancy is for getting an answer to a specific problem right now. They overlap, the difference is mostly timing."
        />
        <FaqItem
          question="Can I get free help with a commercial project?"
          answer="Yes. The community Slack and GitHub issues are open to everyone, commercial or not. We ask that you make questions reproducible and search before posting. If your team needs guaranteed turnaround, that is what the paid tiers are for."
        />
        <FaqItem
          question="How do I file a good bug report?"
          answer="Open an issue on GitHub with the package versions, a minimal repro (a small repo or a gist), the actual error, and what you expected. Bug reports with a reproducible minimal example get triaged fastest."
        />
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/*  Closing CTA                                                        */
/* ------------------------------------------------------------------ */

function ClosingCta() {
  return (
    <section className="border-cc-card-border border-t py-16 sm:py-20">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 backdrop-blur-sm sm:p-12">
        <div
          className="pointer-events-none absolute -top-24 -right-24 h-64 w-64 rounded-full opacity-30 blur-3xl"
          style={{
            background:
              "radial-gradient(circle, rgba(94,234,212,0.5), transparent 70%)",
          }}
          aria-hidden="true"
        />
        <div className="relative flex flex-col gap-6">
          <span className="caption text-cc-accent font-mono tracking-[0.22em] uppercase">
            Still stuck
          </span>
          <h2 className="font-heading text-h3 text-cc-heading font-bold">
            Take the next step.
          </h2>
          <p className="lead text-cc-prose max-w-3xl">
            Slack for a quick question, Calendly for an hourly session, Support
            for an ongoing plan. Pick the smallest one that fits.
          </p>
          <div className="mt-2 flex flex-wrap gap-3">
            <SolidButton href="https://calendly.com/chillicream/60min">
              Book an expert session
            </SolidButton>
            <OutlineButton href="https://slack.chillicream.com/">
              Ask in Slack
            </OutlineButton>
            <OutlineButton href="/services/support">
              See support plans
            </OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/*  Page                                                               */
/* ------------------------------------------------------------------ */

export default function HelpV3Page() {
  return (
    <div>
      <Hero />
      <PickYourPath />
      <FirstStop />
      <Tiers />
      <Faq />
      <ClosingCta />
    </div>
  );
}
