import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRow,
} from "@/src/design-system/Table";

const META_DESCRIPTION =
  "Pick a ChilliCream GraphQL support plan: private Slack, SLAs from next business day to 24 hours, and Hot Chocolate, Fusion, and Nitro experts on the thread.";

export const metadata: Metadata = {
  title: "GraphQL Support Plans and SLAs | ChilliCream",
  description: META_DESCRIPTION,
  keywords: [
    "GraphQL support",
    "Hot Chocolate support",
    "Nitro support",
    "GraphQL SLA",
    "ChilliCream support plans",
    "GraphQL incident response",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    type: "website",
    siteName: "ChilliCream",
    title: "GraphQL Support Plans and SLAs | ChilliCream",
    description: META_DESCRIPTION,
  },
};

// ---------------------------------------------------------------------------
// Canonical Support data (mirrors app/(content)/services/support/page.tsx).
// ---------------------------------------------------------------------------

interface SupportPlan {
  readonly title: string;
  readonly price: string;
  readonly priceNote?: string;
  readonly description: string;
  readonly responseWindow: string;
  readonly responseNote: string;
  readonly perks: readonly string[];
  readonly cta: { readonly title: string; readonly link: string };
  readonly recommended?: boolean;
}

const SUPPORT_PLANS: readonly SupportPlan[] = [
  {
    title: "Community",
    price: "Free",
    description: "For personal or non-commercial projects, to start hacking.",
    responseWindow: "Best effort",
    responseNote: "Public Slack, answered when an expert sees it.",
    perks: ["Public Slack Channel"],
    cta: { title: "Join Slack", link: "https://slack.chillicream.com/" },
  },
  {
    title: "Startup",
    price: "$450",
    priceNote: "per month",
    description:
      "For small teams with moderate bandwidth and projects of low to medium complexity.",
    responseWindow: "Next business day",
    responseNote: "2 critical incidents, private Slack channel.",
    perks: ["Private Slack Channel", "2 critical incidents"],
    cta: {
      title: "Contact Us",
      link: "/services/support/contact?plan=Startup",
    },
  },
  {
    title: "Business",
    price: "$1,300",
    priceNote: "per month",
    description: "For larger teams with business-critical projects.",
    responseWindow: "Next business day",
    responseNote: "5 critical and 2 non-critical incidents, email + Slack.",
    perks: [
      "Private Slack Channel",
      "5 critical incidents",
      "2 non-critical incidents",
      "Email support",
    ],
    cta: {
      title: "Contact Us",
      link: "/services/support/contact?plan=Business",
    },
    recommended: true,
  },
  {
    title: "Enterprise",
    price: "Custom",
    description:
      "For the whole organization, all your teams and business units, and with tailor made SLAs.",
    responseWindow: "24 hours",
    responseNote: "Unlimited critical incidents, phone, dedicated manager.",
    perks: [
      "Private Slack Channel",
      "Unlimited critical incidents",
      "10 non-critical incidents",
      "Phone support",
      "Dedicated account manager",
      "Status reviews",
    ],
    cta: {
      title: "Contact Us",
      link: "/services/support/contact?plan=Enterprise",
    },
  },
];

interface ComparisonRow {
  readonly title: string;
  readonly values: readonly (boolean | string)[];
}

const COMPARISON: readonly ComparisonRow[] = [
  {
    title: "Critical Incidents",
    values: [
      false,
      "2 (next business day)",
      "5 (next business day)",
      "Unlimited (24 hours)",
    ],
  },
  {
    title: "Non-critical Incidents",
    values: [false, false, "5 (3 business days)", "10 (next business day)"],
  },
  { title: "Public Slack Channel", values: [true, true, true, true] },
  { title: "Private Slack Channel", values: [false, true, true, true] },
  {
    title: "Private Issue Tracking Board",
    values: [false, false, true, true],
  },
  { title: "Email Support", values: [false, false, true, true] },
  { title: "Phone Support", values: [false, false, false, true] },
  { title: "Dedicated Account Manager", values: [false, false, false, true] },
  { title: "Status Reviews", values: [false, false, false, true] },
];

const PLAN_NAMES = ["Community", "Startup", "Business", "Enterprise"] as const;

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function SupportPreviewV3Page() {
  return (
    <>
      <FunnelHero />
      <PlanGrid />
      <IncidentTimeline />
      <ExpectationsBand />
      <ComparisonTable />
      <FaqSection />
      <EnterpriseBand />
      <ClosingCta />
    </>
  );
}

// ---------------------------------------------------------------------------
// Hero: the 3-step "what getting help looks like" path.
// ---------------------------------------------------------------------------

interface FunnelStep {
  readonly num: string;
  readonly label: string;
  readonly title: string;
  readonly copy: string;
  readonly Icon: () => React.ReactElement;
}

const FUNNEL_STEPS: readonly FunnelStep[] = [
  {
    num: "01",
    label: "You are stuck",
    title: "Production hiccup or a tricky design question",
    copy: "A query plan that fell over, a schema decision you don't want to make alone, an upgrade that needs a second pair of eyes.",
    Icon: StuckIcon,
  },
  {
    num: "02",
    label: "You open a channel",
    title: "Private Slack or a ticket on your tracker",
    copy: "One thread. You drop the schema, the trace, the repro. No back and forth on which form to fill out, no support portal maze.",
    Icon: ChannelIcon,
  },
  {
    num: "03",
    label: "An expert responds",
    title: "Within the response window for your plan",
    copy: "A core engineer who works on Hot Chocolate, Fusion or Nitro picks it up and walks the fix with you to resolution.",
    Icon: ExpertIcon,
  },
];

function FunnelHero() {
  return (
    <section className="pt-16 pb-12 sm:pt-24 sm:pb-16">
      <div className="text-cc-nav-label mb-4 text-center font-mono text-xs font-semibold tracking-widest uppercase">
        ChilliCream Support
      </div>
      <h1 className="text-cc-heading mx-auto max-w-3xl text-center text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
        Help that arrives, in writing, with a name attached.
      </h1>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-2xl text-center text-base sm:text-lg">
        Every ChilliCream support plan boils down to the same three steps: you
        get stuck, you open a private channel, an expert who actually works on
        Hot Chocolate, Fusion and Nitro replies within your SLA.
      </p>

      <ol className="mt-14 grid gap-4 md:grid-cols-3 md:gap-0">
        {FUNNEL_STEPS.map((step, index) => (
          <FunnelStepCard
            key={step.num}
            step={step}
            isLast={index === FUNNEL_STEPS.length - 1}
          />
        ))}
      </ol>

      <div className="mt-12 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="#plans">See the four plans</SolidButton>
        <OutlineButton href="#how-it-works">
          How an incident unfolds
        </OutlineButton>
      </div>
      <p className="text-cc-nav-label mt-5 text-center font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        Response windows are measured in business hours.
      </p>
    </section>
  );
}

function FunnelStepCard({
  step,
  isLast,
}: {
  readonly step: FunnelStep;
  readonly isLast: boolean;
}) {
  const { Icon } = step;
  return (
    <li className="border-cc-card-border bg-cc-card-bg/60 relative flex flex-col gap-4 rounded-3xl border p-6 sm:p-7 md:rounded-none md:border-r-0 md:first:rounded-l-3xl md:last:rounded-r-3xl md:last:border-r">
      {!isLast && (
        <span
          aria-hidden="true"
          className="border-cc-card-border bg-cc-surface absolute top-1/2 right-0 z-10 hidden h-8 w-8 translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full border md:flex"
        >
          <ArrowRightIcon />
        </span>
      )}
      <div className="flex items-start justify-between gap-4">
        <span className="text-cc-accent font-mono text-xs font-semibold tracking-[0.2em]">
          {step.num}
        </span>
        <span className="text-cc-ink-dim font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          {step.label}
        </span>
      </div>
      <div className="text-cc-accent">
        <Icon />
      </div>
      <h2 className="font-heading text-cc-heading text-xl font-semibold">
        {step.title}
      </h2>
      <p className="text-cc-ink text-sm leading-relaxed">{step.copy}</p>
    </li>
  );
}

// ---------------------------------------------------------------------------
// Compact plan grid with response window front and centre.
// ---------------------------------------------------------------------------

function PlanGrid() {
  return (
    <section id="plans" className="py-16 sm:py-20">
      <div className="mb-10 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Four plans
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Pick the response window your team can live with.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base">
          Each tier widens what counts as a covered incident and shortens how
          long you wait. Everything else, the people and the workflow, is the
          same.
        </p>
      </div>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {SUPPORT_PLANS.map((plan) => (
          <PlanCard key={plan.title} plan={plan} />
        ))}
      </div>
    </section>
  );
}

function PlanCard({ plan }: { readonly plan: SupportPlan }) {
  const Cta = plan.recommended ? SolidButton : OutlineButton;
  return (
    <article
      className={`relative flex h-full flex-col gap-5 rounded-3xl border p-6 ${
        plan.recommended
          ? "border-cc-accent bg-cc-card-bg"
          : "border-cc-card-border bg-cc-card-bg/60"
      }`}
    >
      {plan.recommended && (
        <span className="bg-cc-accent text-cc-surface absolute -top-3 left-6 rounded-full px-3 py-1 font-mono text-[0.6rem] font-semibold tracking-[0.18em] uppercase">
          Most teams pick this
        </span>
      )}
      <header className="flex flex-col gap-1">
        <h3 className="font-heading text-cc-heading text-xl font-semibold">
          {plan.title}
        </h3>
        <p className="text-cc-ink-dim text-sm leading-relaxed">
          {plan.description}
        </p>
      </header>

      <div className="flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h4 font-semibold">
          {plan.price}
        </span>
        {plan.priceNote && (
          <span className="text-cc-nav-label font-mono text-xs">
            {plan.priceNote}
          </span>
        )}
      </div>

      <div className="border-cc-card-border bg-cc-surface/60 rounded-2xl border p-4">
        <div className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          Response window
        </div>
        <div className="text-cc-heading mt-1 text-base font-semibold">
          {plan.responseWindow}
        </div>
        <p className="text-cc-ink-dim mt-2 text-xs leading-relaxed">
          {plan.responseNote}
        </p>
      </div>

      <ul className="flex flex-1 flex-col gap-2">
        {plan.perks.map((perk) => (
          <li key={perk} className="text-cc-ink flex items-start gap-2 text-sm">
            <span className="text-cc-accent mt-1 flex-none">
              <CheckIcon />
            </span>
            <span>{perk}</span>
          </li>
        ))}
      </ul>

      <Cta href={plan.cta.link} className="w-full">
        {plan.cta.title}
      </Cta>
    </article>
  );
}

// ---------------------------------------------------------------------------
// "How an incident unfolds" timeline.
// ---------------------------------------------------------------------------

interface TimelineStop {
  readonly t: string;
  readonly title: string;
  readonly copy: string;
  readonly Icon: () => React.ReactElement;
}

const TIMELINE: readonly TimelineStop[] = [
  {
    t: "T+0",
    title: "Open",
    copy: "You post in your private Slack channel or file a ticket on your private board with the schema, repro, and logs you already have.",
    Icon: OpenIcon,
  },
  {
    t: "Within SLA",
    title: "Triage",
    copy: "We acknowledge, classify severity together (critical vs. non-critical), and confirm the response window that applies.",
    Icon: TriageIcon,
  },
  {
    t: "Engaged",
    title: "Expert engaged",
    copy: "A core engineer on Hot Chocolate, Fusion or Nitro owns the thread, asks follow-up questions, and walks the fix with you.",
    Icon: EngagedIcon,
  },
  {
    t: "Closed",
    title: "Resolved + write-up",
    copy: "We confirm the fix works in your environment. For Enterprise we close with a short post-mortem and add it to the next status review.",
    Icon: ResolvedIcon,
  },
];

function IncidentTimeline() {
  return (
    <section id="how-it-works" className="py-16 sm:py-20">
      <div className="mb-12 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          How an incident unfolds
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          From the first message to a confirmed fix.
        </h2>
      </div>

      <ol className="border-cc-card-border bg-cc-card-bg/60 relative grid gap-0 rounded-3xl border md:grid-cols-4">
        {TIMELINE.map((stop, index) => (
          <TimelineCell key={stop.title} stop={stop} index={index} />
        ))}
      </ol>
    </section>
  );
}

function TimelineCell({
  stop,
  index,
}: {
  readonly stop: TimelineStop;
  readonly index: number;
}) {
  const { Icon } = stop;
  const isLast = index === TIMELINE.length - 1;
  return (
    <li
      className={`relative flex flex-col gap-3 p-6 ${
        !isLast
          ? "border-cc-card-border border-b md:border-r md:border-b-0"
          : ""
      }`}
    >
      <div className="flex items-center gap-3">
        <span className="border-cc-accent/40 bg-cc-accent/10 text-cc-accent inline-flex h-8 w-8 items-center justify-center rounded-full border">
          <Icon />
        </span>
        <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          {stop.t}
        </span>
      </div>
      <h3 className="font-heading text-cc-heading text-lg font-semibold">
        {stop.title}
      </h3>
      <p className="text-cc-ink text-sm leading-relaxed">{stop.copy}</p>
    </li>
  );
}

// ---------------------------------------------------------------------------
// "What you can expect from us" honesty band.
// ---------------------------------------------------------------------------

const EXPECTATIONS_DO = [
  "Reply within the response window for your plan, in business hours, in your private channel.",
  "Loop in the engineer who knows the relevant subsystem instead of routing through a generic queue.",
  "Say so plainly when something is a bug, a missing feature, or a design choice you should reconsider.",
  "Stay on the thread until the fix is confirmed in your environment, not just merged on our side.",
];

const EXPECTATIONS_DONT = [
  "Promise 24/7 phone coverage on plans that don't include it. Phone is Enterprise only.",
  "Count an incident against your quota if it turns out to be a bug on our side.",
  "Send a sales rep when you ask an engineering question.",
  "Ship workarounds and walk away. We track the underlying issue to a real fix.",
];

function ExpectationsBand() {
  return (
    <section className="py-16 sm:py-20">
      <div className="mb-10 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          The honest version
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          What you can expect from us.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base">
          Support contracts are easy to over-promise. Here is what is actually
          in the box, and what isn&apos;t.
        </p>
      </div>
      <div className="grid gap-4 md:grid-cols-2">
        <ExpectationsColumn
          tone="do"
          heading="We will"
          items={EXPECTATIONS_DO}
        />
        <ExpectationsColumn
          tone="dont"
          heading="We won't"
          items={EXPECTATIONS_DONT}
        />
      </div>
    </section>
  );
}

function ExpectationsColumn({
  tone,
  heading,
  items,
}: {
  readonly tone: "do" | "dont";
  readonly heading: string;
  readonly items: readonly string[];
}) {
  const ringClass = tone === "do" ? "ring-cc-success/30" : "ring-cc-warning/30";
  const iconColor = tone === "do" ? "text-cc-success" : "text-cc-warning";
  return (
    <div
      className={`border-cc-card-border bg-cc-card-bg/60 rounded-3xl border p-6 ring-1 ${ringClass}`}
    >
      <h3 className="text-cc-heading mb-4 text-lg font-semibold">{heading}</h3>
      <ul className="flex flex-col gap-3">
        {items.map((item) => (
          <li key={item} className="flex items-start gap-3">
            <span className={`mt-0.5 flex-none ${iconColor}`}>
              {tone === "do" ? (
                <CheckIcon size={16} />
              ) : (
                <MinusIcon size={16} />
              )}
            </span>
            <span className="text-cc-ink text-sm leading-relaxed">{item}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Comparison table.
// ---------------------------------------------------------------------------

function ComparisonTable() {
  return (
    <section className="py-16 sm:py-20">
      <div className="mb-10 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Side by side
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Compare the four plans.
        </h2>
      </div>
      <Table alternating>
        <TableHead>
          <TableRow>
            <TableHeaderCell>Coverage</TableHeaderCell>
            {PLAN_NAMES.map((name) => (
              <TableHeaderCell key={name} align="center">
                {name}
              </TableHeaderCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {COMPARISON.map((row) => (
            <TableRow key={row.title}>
              <TableCell>{row.title}</TableCell>
              {row.values.map((value, i) => (
                <TableCell key={i} align="center">
                  {value === true ? (
                    <span className="text-cc-accent inline-flex items-center justify-center">
                      <CheckIcon />
                    </span>
                  ) : value === false ? (
                    <span className="text-cc-ink-faint">·</span>
                  ) : (
                    <span className="text-cc-ink">{value}</span>
                  )}
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </section>
  );
}

// ---------------------------------------------------------------------------
// FAQ
// ---------------------------------------------------------------------------

interface FaqItem {
  readonly q: string;
  readonly a: string;
}

const FAQS: readonly FaqItem[] = [
  {
    q: "What counts as a critical incident?",
    a: "Production is down or materially degraded for end users, a security issue with no safe workaround, or data integrity is at risk. If you are not sure, file it as critical. We classify together at triage and never count a downgrade against your quota.",
  },
  {
    q: "What is the actual response time per plan?",
    a: "Startup and Business: next business day for critical incidents, three business days for non-critical on Business. Enterprise: 24 hours for critical, next business day for non-critical. Community is best effort on public Slack with no SLA.",
  },
  {
    q: "Are response times 24/7 or business hours?",
    a: "Response windows are measured in business hours. The Enterprise tier has a 24 hour SLA on critical incidents and phone support; anything beyond that is scoped in your Enterprise contract.",
  },
  {
    q: "What happens if I run out of incidents in a month?",
    a: "If you are close to your monthly quota we will flag it before doing further work and either upgrade your plan or scope the next steps with you. We do not silently bill overages.",
  },
  {
    q: "Who actually replies in my private channel?",
    a: "ChilliCream core engineers who work on Hot Chocolate, Fusion and Nitro. There is no first-line script tier in front of them. For Enterprise you also get a named account manager who owns the relationship.",
  },
  {
    q: "Can I switch plans later or pause support?",
    a: "Yes. You can upgrade, downgrade, or pause your plan by contacting us, and Enterprise terms are agreed directly in your contract.",
  },
];

function FaqSection() {
  return (
    <section className="py-16 sm:py-20">
      <div className="mb-10 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Common questions
        </div>
        <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Before you decide.
        </h2>
      </div>
      <div className="mx-auto max-w-3xl">
        <ul className="border-cc-card-border bg-cc-card-bg/60 divide-cc-card-border divide-y rounded-3xl border">
          {FAQS.map((item) => (
            <li key={item.q} className="p-6">
              <h3 className="text-cc-heading text-base font-semibold">
                {item.q}
              </h3>
              <p className="text-cc-ink mt-2 text-sm leading-relaxed">
                {item.a}
              </p>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Enterprise contact band.
// ---------------------------------------------------------------------------

function EnterpriseBand() {
  return (
    <section className="py-16 sm:py-20">
      <div className="border-cc-accent/40 bg-cc-card-bg/70 relative overflow-hidden rounded-3xl border p-8 sm:p-12">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-0"
          style={{
            background:
              "radial-gradient(60% 80% at 100% 0%, rgba(94,234,212,0.12), transparent 60%), radial-gradient(50% 70% at 0% 100%, rgba(124,146,198,0.10), transparent 60%)",
          }}
        />
        <div className="relative grid gap-8 lg:grid-cols-[1.4fr_1fr] lg:items-center">
          <div>
            <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
              Enterprise
            </div>
            <h2 className="text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
              Your SLA, your teams, your platform.
            </h2>
            <p className="text-cc-ink mt-4 max-w-xl text-base leading-relaxed">
              Multiple business units on Hot Chocolate, Fusion or Nitro?
              Regulated workload that needs a 24 hour critical SLA, phone cover,
              and status reviews? We will tailor the contract, the response
              windows, and the named contacts to fit.
            </p>
            <ul className="text-cc-ink mt-6 grid gap-2 text-sm sm:grid-cols-2">
              {[
                "Tailored SLA and response windows",
                "Dedicated account manager",
                "Phone support",
                "Status reviews",
                "Unlimited critical incidents",
                "Private issue tracking board",
              ].map((item) => (
                <li key={item} className="flex items-start gap-2">
                  <span className="text-cc-accent mt-1 flex-none">
                    <CheckIcon />
                  </span>
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </div>
          <div className="flex flex-col gap-3">
            <SolidButton
              href="/services/support/contact?plan=Enterprise"
              className="w-full"
            >
              Talk to us about Enterprise
            </SolidButton>
            <OutlineButton href="/services/advisory" className="w-full">
              See advisory engagements
            </OutlineButton>
            <p className="text-cc-ink-dim mt-2 font-mono text-xs">
              Typical response on enterprise enquiries: one business day.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Closing CTA pair.
// ---------------------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="py-20 text-center sm:py-24">
      <h2 className="text-cc-heading mx-auto max-w-2xl text-3xl font-semibold tracking-tight sm:text-4xl">
        Ready to put a name on the other side of the channel?
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl text-base">
        Start in public Slack to see how we work, or pick a paid plan when you
        need a private channel and a response window you can plan against.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/services/support/contact?plan=Business">
          Contact ChilliCream Support
        </SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Join the community Slack
        </OutlineButton>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Inline SVG icons (decorative, currentColor).
// ---------------------------------------------------------------------------

function StuckIcon() {
  return (
    <svg
      width="44"
      height="44"
      viewBox="0 0 44 44"
      fill="none"
      aria-hidden="true"
    >
      <circle
        cx="22"
        cy="22"
        r="14"
        stroke="currentColor"
        strokeWidth="1.5"
        opacity="0.4"
      />
      <path
        d="M22 13v10"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
      />
      <circle cx="22" cy="29" r="1.4" fill="currentColor" />
      <path
        d="M11 11l3 3M33 11l-3 3M11 33l3-3M33 33l-3-3"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        opacity="0.6"
      />
    </svg>
  );
}

function ChannelIcon() {
  return (
    <svg
      width="44"
      height="44"
      viewBox="0 0 44 44"
      fill="none"
      aria-hidden="true"
    >
      <rect
        x="6"
        y="10"
        width="32"
        height="22"
        rx="4"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path
        d="M6 16h32"
        stroke="currentColor"
        strokeWidth="1.5"
        opacity="0.6"
      />
      <circle cx="10" cy="13" r="0.9" fill="currentColor" />
      <circle cx="13" cy="13" r="0.9" fill="currentColor" />
      <circle cx="16" cy="13" r="0.9" fill="currentColor" />
      <path
        d="M11 22h14M11 26h10"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

function ExpertIcon() {
  return (
    <svg
      width="44"
      height="44"
      viewBox="0 0 44 44"
      fill="none"
      aria-hidden="true"
    >
      <circle cx="22" cy="17" r="6" stroke="currentColor" strokeWidth="1.5" />
      <path
        d="M10 35c1.5-6 6.5-9 12-9s10.5 3 12 9"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <path
        d="M28 11l2 2 4-4"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function ArrowRightIcon() {
  return (
    <svg
      width="14"
      height="14"
      viewBox="0 0 14 14"
      fill="none"
      aria-hidden="true"
      className="text-cc-accent"
    >
      <path
        d="M3 7h8M8 4l3 3-3 3"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function OpenIcon() {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M8 3v10M3 8h10"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
      />
    </svg>
  );
}

function TriageIcon() {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M2 4h12M4 8h8M6 12h4"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
      />
    </svg>
  );
}

function EngagedIcon() {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
    >
      <circle cx="5" cy="8" r="2" stroke="currentColor" strokeWidth="1.4" />
      <circle cx="11" cy="8" r="2" stroke="currentColor" strokeWidth="1.4" />
      <path
        d="M7 8h2"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
    </svg>
  );
}

function ResolvedIcon() {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M3 8.5L6.5 12 13 4.5"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function MinusIcon({ size = 14 }: { readonly size?: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M3 8h10"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}
