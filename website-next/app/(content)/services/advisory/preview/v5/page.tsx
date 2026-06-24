import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL Advisory by ChilliCream",
  description:
    "GraphQL advisory consulting at $300/hour and scoped contracting from the engineers behind Hot Chocolate, Fusion, and Nitro. Book a 60-minute call today.",
  keywords: [
    "GraphQL advisory consulting",
    "GraphQL consulting",
    "GraphQL contracting",
    "Hot Chocolate consulting",
    "Fusion consulting",
    "Nitro consulting",
    "ChilliCream advisory",
  ],
  openGraph: {
    title: "GraphQL Advisory by ChilliCream",
    description:
      "GraphQL advisory consulting at $300/hour and scoped contracting from the engineers behind Hot Chocolate, Fusion, and Nitro.",
  },
  robots: { index: false, follow: false },
};

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONSULTING_MAILTO = "mailto:contact@chillicream.com?subject=Consulting";
const CONTRACTING_MAILTO = "mailto:contact@chillicream.com?subject=Contracting";

interface Tier {
  readonly id: "consulting" | "contracting";
  readonly eyebrow: string;
  readonly name: string;
  readonly tagline: string;
  readonly priceLine: string;
  readonly priceNote: string;
  readonly bestFor: string;
  readonly perks: readonly string[];
  readonly primaryCta: { readonly label: string; readonly href: string };
  readonly secondaryCta: { readonly label: string; readonly href: string };
}

const TIERS: readonly [Tier, Tier] = [
  {
    id: "consulting",
    eyebrow: "Hourly engagements",
    name: "Consulting",
    tagline:
      "Hourly consulting services to get the help you need at any stage of your project. This is the best way to get started.",
    priceLine: "$300",
    priceNote: "per hour",
    bestFor:
      "Teams that already own the build and need a senior GraphQL engineer on call for design, troubleshooting, and review.",
    perks: [
      "Mentoring and guidance",
      "Architecture",
      "Troubleshooting",
      "Code Review",
      "Best practices education",
    ],
    primaryCta: { label: "Book a 60-min call", href: BOOKING_URL },
    secondaryCta: { label: "Email us", href: CONSULTING_MAILTO },
  },
  {
    id: "contracting",
    eyebrow: "Scoped engagements",
    name: "Contracting",
    tagline:
      "Options for teams who do not have the time, bandwidth, and/or expertise to implement their own GraphQL solutions.",
    priceLine: "Custom",
    priceNote: "scope & timeline",
    bestFor:
      "Teams that want our engineers to deliver a working result, from a proof of concept to a production rollout.",
    perks: ["Proof of concept", "Implementation"],
    primaryCta: { label: "Talk to an Expert", href: CONTRACTING_MAILTO },
    secondaryCta: { label: "Book an intro call", href: BOOKING_URL },
  },
];

interface LedgerRow {
  readonly title: string;
  readonly leftTitle: string;
  readonly leftBody: string;
  readonly rightTitle: string;
  readonly rightBody: string;
}

const HERO_LEDGER_ROWS: readonly LedgerRow[] = [
  {
    title: "Rate",
    leftTitle: "Quoted on request",
    leftBody: "Pricing surfaced after a discovery call and a procurement loop.",
    rightTitle: "$300 per hour, posted",
    rightBody:
      "One published rate for consulting. No retainer minimum to start.",
  },
  {
    title: "Who answers the call",
    leftTitle: "An account manager",
    leftBody: "Notes get relayed to an engineer who joins, maybe, next week.",
    rightTitle: "A senior engineer",
    rightBody: "The person on the call is the person doing the work.",
  },
  {
    title: "Time to start",
    leftTitle: "A month of paperwork",
    leftBody:
      "MSAs, NDAs, and scoping cycles before anyone looks at the schema.",
    rightTitle: "Often the same week",
    rightBody:
      "Consulting hours usually open within days. Mutual NDA on request.",
  },
];

const WHY_ROWS: readonly LedgerRow[] = [
  {
    title: "Schema review",
    leftTitle: "Slide deck full of TODOs",
    leftBody:
      "Surface-level notes from someone who skimmed the schema once before the call.",
    rightTitle: "Line-level review with rationale",
    rightBody:
      "Written comments on the schema itself, with the reasoning behind every change.",
  },
  {
    title: "Fusion rollout",
    leftTitle: "Trial-and-error subgraph wiring",
    leftBody:
      "Generic federation advice that ignores how composition actually plans your fields.",
    rightTitle: "Composition designed by the people who wrote Fusion",
    rightBody:
      "Composition at planning time, with rollout sequenced around the published clients affected.",
  },
  {
    title: "Production incident",
    leftTitle: "Stack-Overflow archaeology",
    leftBody:
      "An account manager forwarding screenshots while the on-call engineer waits.",
    rightTitle: "Direct line to the maintainers",
    rightBody:
      "A shared channel with the engineers who write and ship the framework code.",
  },
  {
    title: "Performance",
    leftTitle: "Generic .NET tuning",
    leftBody:
      "Profiler screenshots and CPU graphs without a single resolver in sight.",
    rightTitle: "Resolver and DataLoader tuning by the framework authors",
    rightBody:
      "Targeted fixes in the resolvers, batching layer, and Hot Chocolate execution path.",
  },
  {
    title: "Hand-off",
    leftTitle: "Slideware",
    leftBody:
      "A PDF, a recorded call, and a forwarding address. You rebuild the work yourself.",
    rightTitle: "Working code in your repo",
    rightBody:
      "Pull requests, an ADR, and the engineers who wrote it still on the channel.",
  },
];

interface EngagementStep {
  readonly index: string;
  readonly title: string;
  readonly description: string;
  readonly bullets: readonly string[];
}

const ENGAGEMENT_STEPS: readonly EngagementStep[] = [
  {
    index: "01",
    title: "Introductory call",
    description:
      "A 60-minute working call. You walk us through the system, the goal, and the constraints. We ask the hard questions and tell you whether we are the right fit.",
    bullets: [
      "Senior engineer, no sales handoff",
      "Mutual NDA available on request",
      "Written recap with next steps",
    ],
  },
  {
    index: "02",
    title: "Proposal",
    description:
      "A written proposal that matches your need: hourly retainer for consulting, or a scoped statement of work for contracting with deliverables, milestones, and a target timeline.",
    bullets: [
      "Hourly retainer or fixed scope",
      "Clear deliverables and milestones",
      "Written, scoped, and signable",
    ],
  },
  {
    index: "03",
    title: "Kickoff",
    description:
      "Contract signed, channel opened, work starts. You get a direct line to the engineers doing the work, a shared backlog, and a weekly checkpoint.",
    bullets: [
      "Shared Slack or Teams channel",
      "Weekly checkpoint and written status",
      "Direct access to the engineers doing the work",
    ],
  },
];

const VENDOR_STEPS: readonly {
  readonly title: string;
  readonly body: string;
}[] = [
  {
    title: "Sales qualification call",
    body: "A discovery call with someone who will not work on the project, scoring your budget instead of your problem.",
  },
  {
    title: "Statement of work after a month",
    body: "Weeks of back-and-forth before any engineer looks at the schema or the federation plan.",
  },
  {
    title: "Account manager intermediary",
    body: "Every question routed through a project manager who paraphrases the engineers back to you.",
  },
];

interface FaqItem {
  readonly question: string;
  readonly answer: string;
}

const FAQ: readonly FaqItem[] = [
  {
    question: "What is the hourly rate for consulting?",
    answer:
      "Consulting is billed at $300 per hour. We bill in blocks of time agreed up front, on a retainer or a small purchase order, so you never get a surprise invoice. Contracting engagements are scoped separately as a statement of work.",
  },
  {
    question: "How small is too small for an engagement?",
    answer:
      "A single 60-minute call is fine. Many teams start with one or two hours to unblock a specific decision (schema shape, Fusion composition, auth model) and only return when the next question lands. There is no minimum retainer to talk to us.",
  },
  {
    question: "Do you sign an NDA?",
    answer:
      "Yes. We will sign a mutual NDA before the introductory call when you ask, and we are comfortable with most standard agreements. Customer code, schemas, and traces never leave the engagement.",
  },
  {
    question: "How quickly can you start?",
    answer:
      "Consulting hours usually start within the same week, sometimes the same day. Contracting engagements depend on scope and current bandwidth, and we will tell you the realistic start date on the introductory call rather than promise a slot we cannot honor.",
  },
  {
    question: "What outcomes can I expect?",
    answer:
      "Concrete, written deliverables tied to your goal: an architecture decision record, a schema review with line-level comments, a working proof of concept, or a production-ready implementation. We do not bill for slideware.",
  },
  {
    question: "Who actually does the work?",
    answer:
      "The engineers who build Hot Chocolate, Fusion, and Nitro. The same people who write the framework code, review the pull requests, and answer the hard issues on GitHub are the people on your call.",
  },
];

const TEAM_LEFT_BULLETS: readonly string[] = [
  "Learned GraphQL last quarter",
  "Reads the same docs you do",
  "Sends a different engineer each week",
];

const TEAM_RIGHT_BULLETS: readonly string[] = [
  "Direct line to the maintainers",
  "Pull-request authors on the core code",
  "Same team across consulting and contracting",
];

export default function AdvisoryPreviewV5Page() {
  return (
    <>
      <Hero />
      <ChoiceLedger />
      <WhyLedger />
      <HowLedger />
      <TeamLedger />
      <FaqLedger />
      <ContactLedger />
    </>
  );
}

function Hero() {
  return (
    <section className="mx-auto max-w-6xl pt-10 pb-12 sm:pt-16 sm:pb-16">
      <p className="text-cc-nav-label text-center font-mono text-xs tracking-[0.18em] uppercase">
        ChilliCream Advisory
      </p>
      <h1 className="font-heading text-cc-heading text-hero mx-auto mt-5 max-w-4xl text-center font-semibold tracking-tight text-balance">
        Talk to the engineers who built your GraphQL stack.
      </h1>
      <p className="text-cc-ink text-lead mx-auto mt-6 max-w-2xl text-center text-pretty">
        GraphQL advisory consulting at $300 per hour, or scoped contracting,
        from the team behind Hot Chocolate, Fusion, and Nitro.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
        <OutlineButton href={CONSULTING_MAILTO}>Email us</OutlineButton>
      </div>

      <div className="mt-12">
        <ComparePanel
          leftLabel="Without ChilliCream"
          rightLabel="With ChilliCream"
          ariaLabel="Hero comparison"
          rows={HERO_LEDGER_ROWS}
        />
      </div>
    </section>
  );
}

function ChoiceLedger() {
  const [consulting, contracting] = TIERS;
  return (
    <section
      aria-labelledby="choice-heading"
      className="mx-auto mt-16 max-w-6xl sm:mt-24"
    >
      <LedgerHeader
        eyebrow="Two ways to engage"
        title="Consulting vs Contracting."
        headingId="choice-heading"
        intro="Pick the engagement that matches the question. Both sides are the same engineers."
      />

      <div className="border-cc-card-border bg-cc-card-bg mt-10 overflow-hidden rounded-3xl border">
        <div className="grid lg:grid-cols-[1fr_1px_1fr]">
          <ChoiceColumn tier={consulting} alignedLabel="Hourly Consulting" />
          <VerticalRule />
          <ChoiceColumn tier={contracting} alignedLabel="Scoped Contracting" />
        </div>
      </div>
    </section>
  );
}

function ChoiceColumn({
  tier,
  alignedLabel,
}: {
  readonly tier: Tier;
  readonly alignedLabel: string;
}) {
  return (
    <div className="border-cc-card-border flex flex-col border-b p-7 last:border-b-0 sm:p-9 lg:border-b-0">
      <ColumnLabel>{alignedLabel}</ColumnLabel>
      <p className="text-cc-nav-label mt-6 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {tier.eyebrow}
      </p>
      <h3 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
        {tier.name}
      </h3>
      <div className="mt-3 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h4 font-semibold">
          {tier.priceLine}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {tier.priceNote}
        </span>
      </div>
      <p className="text-cc-ink-dim mt-4 text-sm leading-relaxed sm:text-base">
        {tier.tagline}
      </p>

      <p className="text-cc-nav-label mt-6 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        Best for
      </p>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{tier.bestFor}</p>

      <p className="text-cc-nav-label mt-6 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        What is included
      </p>
      <ul className="mt-3 flex flex-1 flex-col gap-3">
        {tier.perks.map((perk) => (
          <li key={perk} className="flex items-start gap-3">
            <span className="text-cc-accent mt-[5px] flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{perk}</span>
          </li>
        ))}
      </ul>

      <div className="mt-8 flex flex-col gap-3 sm:flex-row">
        <SolidButton href={tier.primaryCta.href} className="w-full sm:flex-1">
          {tier.primaryCta.label}
        </SolidButton>
        <OutlineButton
          href={tier.secondaryCta.href}
          className="w-full sm:flex-1"
        >
          {tier.secondaryCta.label}
        </OutlineButton>
      </div>
    </div>
  );
}

function WhyLedger() {
  return (
    <section
      aria-labelledby="why-heading"
      className="mx-auto mt-16 max-w-6xl sm:mt-24"
    >
      <LedgerHeader
        eyebrow="Without vs With"
        title="Why teams call us."
        headingId="why-heading"
        intro="Same five questions teams arrive with. Two very different ways they get answered."
      />

      <div className="mt-10">
        <ComparePanel
          leftLabel="Without ChilliCream"
          rightLabel="With ChilliCream"
          ariaLabel="Why teams call us comparison"
          rows={WHY_ROWS}
        />
      </div>
    </section>
  );
}

function HowLedger() {
  return (
    <section
      aria-labelledby="how-heading"
      className="mx-auto mt-16 max-w-6xl sm:mt-24"
    >
      <LedgerHeader
        eyebrow="From first call to first commit"
        title="How an engagement starts."
        headingId="how-heading"
        intro="No long sales cycle. You speak to an engineer, you get a written proposal, you kick off."
      />

      <div className="border-cc-card-border bg-cc-card-bg mt-10 overflow-hidden rounded-3xl border">
        <ColumnHeaders left="Other vendors" right="ChilliCream Advisory" />

        {ENGAGEMENT_STEPS.map((step, index) => {
          const vendor = VENDOR_STEPS[index];
          return (
            <div
              key={step.index}
              className="border-cc-card-border grid border-t lg:grid-cols-[1fr_1px_1fr]"
            >
              <div className="px-7 py-7 sm:px-9 sm:py-8">
                <RowChip variant="cross" />
                <p className="text-cc-nav-label mt-4 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                  Step {step.index}
                </p>
                <h3 className="font-heading text-cc-heading mt-2 text-base font-semibold">
                  {vendor.title}
                </h3>
                <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">
                  {vendor.body}
                </p>
              </div>
              <VerticalRule />
              <div className="border-cc-card-border border-t px-7 py-7 sm:px-9 sm:py-8 lg:border-t-0">
                <RowChip variant="check" />
                <p className="text-cc-accent mt-4 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                  Step {step.index}
                </p>
                <h3 className="font-heading text-cc-heading mt-2 text-base font-semibold">
                  {step.title}
                </h3>
                <p className="text-cc-ink mt-3 text-sm leading-relaxed">
                  {step.description}
                </p>
                <ul className="mt-4 flex flex-col gap-2">
                  {step.bullets.map((bullet) => (
                    <li
                      key={bullet}
                      className="text-cc-ink flex items-start gap-2 text-xs"
                    >
                      <span className="text-cc-accent mt-[4px] flex-none">
                        <CheckIcon size={12} />
                      </span>
                      <span>{bullet}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          );
        })}
      </div>
    </section>
  );
}

function TeamLedger() {
  return (
    <section
      aria-labelledby="team-heading"
      className="mx-auto mt-16 max-w-6xl sm:mt-24"
    >
      <LedgerHeader
        eyebrow="The team"
        title="Who you work with."
        headingId="team-heading"
        intro="ChilliCream advisory is not a generalist consultancy that learned GraphQL last quarter."
      />

      <div className="border-cc-card-border bg-cc-card-bg mt-10 overflow-hidden rounded-3xl border">
        <div className="grid lg:grid-cols-[1fr_1px_1fr]">
          <div className="border-cc-card-border border-b p-7 sm:p-9 lg:border-b-0">
            <ColumnLabel>A generalist consultancy</ColumnLabel>
            <p className="text-cc-ink-dim mt-6 text-sm leading-relaxed">
              Three engineers deep in a rotation, none of whom have shipped
              GraphQL beyond a tutorial. Different face every sprint.
            </p>
            <ul className="mt-6 flex flex-col gap-3">
              {TEAM_LEFT_BULLETS.map((bullet) => (
                <li key={bullet} className="flex items-start gap-3">
                  <RowChip variant="cross" inline />
                  <span className="text-cc-ink-dim text-sm leading-relaxed">
                    {bullet}
                  </span>
                </li>
              ))}
            </ul>
          </div>
          <VerticalRule />
          <div className="p-7 sm:p-9">
            <ColumnLabel>ChilliCream Advisory</ColumnLabel>
            <p className="text-cc-ink mt-6 text-sm leading-relaxed">
              Senior engineers from the core team. The same people who maintain
              Hot Chocolate, design Fusion, and ship Nitro.
            </p>
            <ul className="mt-6 flex flex-col gap-3">
              {TEAM_RIGHT_BULLETS.map((bullet) => (
                <li key={bullet} className="flex items-start gap-3">
                  <RowChip variant="check" inline />
                  <span className="text-cc-ink text-sm leading-relaxed">
                    {bullet}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        </div>

        <div className="border-cc-card-border grid border-t sm:grid-cols-3">
          <ProductBadge
            name="Hot Chocolate"
            tagline="The .NET GraphQL server we maintain."
            position="first"
          />
          <ProductBadge
            name="Fusion"
            tagline="Federation and composition for GraphQL subgraphs."
            position="middle"
          />
          <ProductBadge
            name="Nitro"
            tagline="The observability and CI platform for the stack."
            position="last"
          />
        </div>
      </div>
    </section>
  );
}

function ProductBadge({
  name,
  tagline,
  position,
}: {
  readonly name: string;
  readonly tagline: string;
  readonly position: "first" | "middle" | "last";
}) {
  const sep =
    position === "last"
      ? ""
      : "border-cc-card-border border-b sm:border-r sm:border-b-0";
  return (
    <div className={`flex flex-col p-6 sm:p-7 ${sep}`}>
      <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        Product
      </span>
      <span className="font-heading text-cc-heading mt-2 text-lg font-semibold">
        {name}
      </span>
      <span className="text-cc-ink-dim mt-1 text-sm">{tagline}</span>
    </div>
  );
}

function FaqLedger() {
  return (
    <section
      aria-labelledby="faq-heading"
      className="mx-auto mt-16 max-w-6xl sm:mt-24"
    >
      <LedgerHeader
        eyebrow="FAQ"
        title="Frequently asked."
        headingId="faq-heading"
        intro="Honest answers before you book a call."
      />

      <div className="border-cc-card-border bg-cc-card-bg mt-10 overflow-hidden rounded-3xl border">
        <ColumnHeaders left="Question" right="Answer" />

        <dl>
          {FAQ.map((item) => (
            <div
              key={item.question}
              className="border-cc-card-border grid border-t lg:grid-cols-[1fr_1px_1fr]"
            >
              <dt className="px-7 py-6 sm:px-9 sm:py-7">
                <span className="font-heading text-cc-heading text-base font-semibold">
                  {item.question}
                </span>
              </dt>
              <VerticalRule />
              <dd className="border-cc-card-border text-cc-ink border-t px-7 py-6 text-sm leading-relaxed sm:px-9 sm:py-7 lg:border-t-0">
                {item.answer}
              </dd>
            </div>
          ))}
        </dl>
      </div>
    </section>
  );
}

function ContactLedger() {
  return (
    <section
      aria-labelledby="contact-heading"
      className="mx-auto mt-16 mb-8 max-w-6xl sm:mt-24"
    >
      <LedgerHeader
        eyebrow="Ready when you are"
        title="One call is usually enough to know."
        headingId="contact-heading"
        intro="Book a 60-minute call. You walk us through what you are building, we ask the questions, and you leave with a clear next step."
      />

      <div className="border-cc-card-border bg-cc-card-bg mt-10 overflow-hidden rounded-3xl border">
        <div className="grid lg:grid-cols-[1fr_1px_1fr]">
          <div className="border-cc-card-border border-b p-7 sm:p-9 lg:border-b-0">
            <ColumnLabel>The pitch</ColumnLabel>
            <p className="text-cc-ink mt-6 text-sm leading-relaxed sm:text-base">
              No commitment beyond the hour. If we are not the right fit, we
              will tell you on the call. If we are, you leave with a written
              recap and a proposal.
            </p>
            <ContactSpec />
          </div>
          <VerticalRule />
          <div className="flex flex-col p-7 sm:p-9">
            <ColumnLabel>Book it</ColumnLabel>
            <div className="mt-6 flex flex-1 flex-col justify-center gap-3">
              <SolidButton href={BOOKING_URL} className="w-full">
                Book a 60-min call
              </SolidButton>
              <OutlineButton href={CONSULTING_MAILTO} className="w-full">
                Email us
              </OutlineButton>
            </div>
            <div className="border-cc-card-border mt-8 border-t pt-6">
              <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                Calendly
              </p>
              <p className="text-cc-ink text-caption mt-2 font-mono break-all">
                {BOOKING_URL}
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function ContactSpec() {
  const items: readonly {
    readonly label: string;
    readonly value: ReactNode;
  }[] = [
    { label: "Rate", value: "$300 / hour for consulting" },
    { label: "Contracting", value: "Scoped statement of work" },
    { label: "NDA", value: "Mutual NDA on request" },
    { label: "Start", value: "Often the same week" },
  ];
  return (
    <ul className="mt-6 grid gap-3 sm:grid-cols-2">
      {items.map((item) => (
        <li key={item.label} className="flex items-start gap-3">
          <span className="text-cc-accent mt-[5px] flex-none">
            <CheckIcon />
          </span>
          <span className="text-cc-ink text-sm">
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              {item.label}
            </span>
            <br />
            {item.value}
          </span>
        </li>
      ))}
    </ul>
  );
}

interface LedgerHeaderProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly headingId: string;
  readonly intro?: string;
}

function LedgerHeader({ eyebrow, title, headingId, intro }: LedgerHeaderProps) {
  return (
    <div className="text-center">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        {eyebrow}
      </p>
      <h2
        id={headingId}
        className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
      >
        {title}
      </h2>
      {intro ? (
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">{intro}</p>
      ) : null}
    </div>
  );
}

interface ComparePanelProps {
  readonly leftLabel: string;
  readonly rightLabel: string;
  readonly rows: readonly LedgerRow[];
  readonly ariaLabel: string;
}

function ComparePanel({
  leftLabel,
  rightLabel,
  rows,
  ariaLabel,
}: ComparePanelProps) {
  return (
    <div
      role="table"
      aria-label={ariaLabel}
      className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-3xl border"
    >
      <ColumnHeaders left={leftLabel} right={rightLabel} />

      {rows.map((row) => (
        <LedgerCompareRow key={row.title} row={row} />
      ))}
    </div>
  );
}

function ColumnHeaders({
  left,
  right,
}: {
  readonly left: string;
  readonly right: string;
}) {
  return (
    <div className="grid lg:grid-cols-[1fr_1px_1fr]">
      <div className="px-7 pt-6 pb-2 sm:px-9">
        <ColumnLabel>{left}</ColumnLabel>
      </div>
      <div className="bg-cc-card-border hidden w-px lg:block" />
      <div className="border-cc-card-border border-t px-7 pt-6 pb-2 sm:px-9 lg:border-t-0">
        <ColumnLabel>{right}</ColumnLabel>
      </div>
    </div>
  );
}

function LedgerCompareRow({ row }: { readonly row: LedgerRow }) {
  return (
    <div
      role="row"
      className="border-cc-card-border grid border-t lg:grid-cols-[1fr_1px_1fr]"
    >
      <div className="px-7 py-6 sm:px-9 sm:py-7">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          {row.title}
        </p>
        <div className="mt-3 flex items-start gap-3">
          <RowChip variant="cross" />
          <div>
            <p className="text-cc-heading text-sm font-semibold">
              {row.leftTitle}
            </p>
            <p className="text-cc-ink-dim mt-1 text-sm leading-relaxed">
              {row.leftBody}
            </p>
          </div>
        </div>
      </div>
      <VerticalRule />
      <div className="border-cc-card-border border-t px-7 py-6 sm:px-9 sm:py-7 lg:border-t-0">
        <p className="text-cc-accent font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          {row.title}
        </p>
        <div className="mt-3 flex items-start gap-3">
          <RowChip variant="check" />
          <div>
            <p className="text-cc-heading text-sm font-semibold">
              {row.rightTitle}
            </p>
            <p className="text-cc-ink mt-1 text-sm leading-relaxed">
              {row.rightBody}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

function VerticalRule() {
  return (
    <div
      aria-hidden="true"
      className="bg-cc-card-border hidden w-px lg:block"
    />
  );
}

function ColumnLabel({ children }: { readonly children: ReactNode }) {
  return (
    <span className="text-cc-nav-label inline-block font-mono text-[0.65rem] tracking-[0.18em] uppercase">
      {children}
    </span>
  );
}

function RowChip({
  variant,
  inline = false,
}: {
  readonly variant: "check" | "cross";
  readonly inline?: boolean;
}) {
  const base =
    "border-cc-card-border bg-cc-surface inline-flex h-4 w-4 flex-none items-center justify-center rounded-full border";
  const margin = inline ? "mt-[2px]" : "";
  const cls = [base, margin].filter(Boolean).join(" ");
  if (variant === "check") {
    return (
      <span aria-hidden="true" className={`${cls} text-cc-accent`}>
        <CheckIcon size={10} />
      </span>
    );
  }
  return (
    <span aria-hidden="true" className={`${cls} text-cc-ink-dim`}>
      <CrossGlyph />
    </span>
  );
}

function CrossGlyph() {
  return (
    <svg viewBox="0 0 16 16" width={10} height={10} aria-hidden>
      <path
        d="M4 4 L12 12 M12 4 L4 12"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}
