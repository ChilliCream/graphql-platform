import type { Metadata } from "next";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL Advisory Consulting | ChilliCream",
  description:
    "GraphQL advisory consulting at $300 per hour, or scoped contracting from the engineers behind Hot Chocolate, Fusion, and Nitro. Book a 60-minute working call.",
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
    title: "GraphQL Advisory Consulting | ChilliCream",
    description:
      "GraphQL advisory consulting at $300 per hour, or scoped contracting from the engineers behind Hot Chocolate, Fusion, and Nitro.",
  },
  robots: { index: false, follow: false },
};

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONSULTING_MAILTO = "mailto:contact@chillicream.com?subject=Consulting";
const CONTRACTING_MAILTO = "mailto:contact@chillicream.com?subject=Contracting";

interface SpecRow {
  readonly label: string;
  readonly value: ReactNode;
}

const HERO_SPECS: readonly SpecRow[] = [
  { label: "Rate", value: "$300 / hour" },
  { label: "Intro", value: "60 min, working call" },
  { label: "Tiers", value: "Consulting, Contracting" },
  { label: "Start", value: "Often same week" },
  { label: "NDA", value: "Mutual, on request" },
  { label: "Team", value: "Core maintainers" },
];

interface Tier {
  readonly id: "consulting" | "contracting";
  readonly eyebrow: string;
  readonly name: string;
  readonly price: string;
  readonly priceNote: string;
  readonly bestFor: string;
  readonly perks: readonly string[];
  readonly primaryCta: { readonly label: string; readonly href: string };
  readonly secondaryCta: { readonly label: string; readonly href: string };
}

const TIERS: readonly Tier[] = [
  {
    id: "consulting",
    eyebrow: "Hourly engagements",
    name: "Consulting",
    price: "$300",
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
    price: "Custom",
    priceNote: "scope & timeline",
    bestFor:
      "Teams that want our engineers to deliver a working result, from a proof of concept to a production rollout.",
    perks: ["Proof of concept", "Implementation"],
    primaryCta: { label: "Talk to an Expert", href: CONTRACTING_MAILTO },
    secondaryCta: { label: "Book an intro call", href: BOOKING_URL },
  },
];

interface EngagementStep {
  readonly index: string;
  readonly title: string;
  readonly description: string;
  readonly micro: string;
}

const ENGAGEMENT_STEPS: readonly EngagementStep[] = [
  {
    index: "01",
    title: "Introductory call",
    description:
      "A 60-minute working call. You walk us through the system, the goal, and the constraints. We ask the hard questions and tell you whether we are the right fit.",
    micro: "60 min",
  },
  {
    index: "02",
    title: "Proposal",
    description:
      "A written proposal that matches your need: hourly retainer for consulting, or a scoped statement of work for contracting with deliverables, milestones, and a target timeline.",
    micro: "written",
  },
  {
    index: "03",
    title: "Kickoff",
    description:
      "Contract signed, channel opened, work starts. You get a direct line to the engineers doing the work, a shared backlog, and a weekly checkpoint.",
    micro: "weekly",
  },
];

interface CredentialColumn {
  readonly key: string;
  readonly title: string;
  readonly entries: readonly { readonly term: string; readonly def: string }[];
}

const CREDENTIALS: readonly CredentialColumn[] = [
  {
    key: "maintainers",
    title: "Maintainers",
    entries: [
      {
        term: "Who",
        def: "Senior engineers from the core team that maintains Hot Chocolate, designs Fusion, and ships Nitro.",
      },
      {
        term: "Access",
        def: "Direct line to the maintainers and the pull-request authors on the core code.",
      },
      {
        term: "Continuity",
        def: "Same team across consulting and contracting, no sales handoff.",
      },
    ],
  },
  {
    key: "scope",
    title: "Scope",
    entries: [
      {
        term: "Stack",
        def: "Schema design, federation with Fusion, ASP.NET Core integration, resolver and performance tuning.",
      },
      {
        term: "Adjacent",
        def: "MCP, Nitro observability, persisted operations, CI pipelines.",
      },
      {
        term: "Out of scope",
        def: "Generalist consulting. We stay inside the GraphQL stack we ship.",
      },
    ],
  },
  {
    key: "cadence",
    title: "Cadence",
    entries: [
      {
        term: "Channel",
        def: "Shared Slack or Teams channel, in your repo, in your timezone window.",
      },
      {
        term: "Reporting",
        def: "Written weekly status, written recap after the intro call.",
      },
      {
        term: "Honesty",
        def: "We will say no when no is the right answer, even on a smaller engagement.",
      },
    ],
  },
];

interface ProductRow {
  readonly product: string;
  readonly role: string;
}

const PRODUCT_ROSTER: readonly ProductRow[] = [
  { product: "Hot Chocolate", role: ".NET GraphQL server we maintain" },
  { product: "Fusion", role: "Federation and composition for subgraphs" },
  { product: "Nitro", role: "Observability and CI platform for the stack" },
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

const CONTACT_SPECS: readonly SpecRow[] = [
  { label: "Rate", value: "$300 / hour for consulting" },
  { label: "Contracting", value: "Scoped statement of work" },
  { label: "NDA", value: "Mutual NDA on request" },
  { label: "Start", value: "Often the same week" },
  { label: "Book", value: "60-min working call" },
  { label: "Email", value: "contact@chillicream.com" },
];

export default function AdvisoryPreviewV4Page() {
  return (
    <div className="mx-auto w-full max-w-[960px]">
      <DocHeader />
      <SpecHero />
      <TierMatrix />
      <EngagementProcedure />
      <CredentialsIndex />
      <FaqIndex />
      <ContactSpec />
    </div>
  );
}

function RouteLabel({ children }: { readonly children: ReactNode }) {
  return (
    <div className="border-cc-card-border border-b pb-3">
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        {children}
      </p>
    </div>
  );
}

function DocHeader() {
  return (
    <header className="border-cc-card-border bg-cc-bg/80 sticky top-0 z-10 -mx-4 border-b px-4 py-3 backdrop-blur sm:-mx-6 sm:px-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          chillicream.com / services / advisory
        </p>
        <p className="text-cc-ink-dim font-mono text-xs tracking-[0.18em] uppercase">
          rev. 2026.06
        </p>
      </div>
    </header>
  );
}

function SpecHero() {
  return (
    <section aria-labelledby="advisory-heading" className="pt-10 pb-12">
      <RouteLabel>/advisory/00 / overview</RouteLabel>
      <h1
        id="advisory-heading"
        className="font-heading text-cc-heading text-hero mt-8 font-semibold tracking-tight"
      >
        GraphQL advisory.
      </h1>
      <p className="text-cc-ink-dim text-lead mt-6 max-w-2xl">
        GraphQL advisory consulting at $300 per hour, or scoped contracting,
        delivered by the engineers behind Hot Chocolate, Fusion, and Nitro.
        Bring a question, a design, or a deadline.
      </p>

      <dl className="divide-cc-card-border border-cc-card-border mt-10 grid divide-y border-t border-b sm:grid-cols-2 sm:divide-y-0">
        {HERO_SPECS.map((spec, i) => (
          <HairlineRow
            key={spec.label}
            label={spec.label}
            value={spec.value}
            bordered={i >= 2}
          />
        ))}
      </dl>

      <div className="mt-10 flex flex-wrap gap-3">
        <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
        <OutlineButton href={CONSULTING_MAILTO}>Email us</OutlineButton>
      </div>
    </section>
  );
}

function HairlineRow({
  label,
  value,
  bordered,
}: {
  readonly label: string;
  readonly value: ReactNode;
  readonly bordered?: boolean;
}) {
  return (
    <div
      className={`grid grid-cols-[10rem_1fr] items-baseline gap-4 py-3 ${
        bordered ? "sm:border-cc-card-border sm:border-t" : ""
      }`}
    >
      <dt className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        {label}
      </dt>
      <dd className="text-cc-ink font-sans text-sm tabular-nums">{value}</dd>
    </div>
  );
}

function SectionTitle({
  id,
  children,
}: {
  readonly id: string;
  readonly children: ReactNode;
}) {
  return (
    <h2
      id={id}
      className="font-heading text-cc-heading text-h4 mt-6 mb-6 font-semibold"
    >
      {children}
    </h2>
  );
}

function TierMatrix() {
  return (
    <section aria-labelledby="tiers-heading" className="pt-8 pb-12">
      <RouteLabel>/advisory/01 / tiers</RouteLabel>
      <SectionTitle id="tiers-heading">Tier matrix</SectionTitle>

      <div className="border-cc-card-border overflow-x-auto border">
        <table className="w-full table-fixed border-collapse text-left">
          <colgroup>
            <col className="w-[9rem] sm:w-[11rem]" />
            <col />
            <col />
          </colgroup>
          <thead>
            <tr className="border-cc-card-border border-b">
              <th className="text-cc-nav-label px-3 py-3 font-mono text-xs tracking-[0.18em] uppercase">
                Spec
              </th>
              {TIERS.map((tier) => (
                <th
                  key={tier.id}
                  className="border-cc-card-border text-cc-nav-label border-l px-3 py-3 font-mono text-xs tracking-[0.18em] uppercase"
                >
                  {tier.name}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="align-top">
            <tr className="border-cc-card-border border-b">
              <th
                scope="row"
                className="text-cc-nav-label px-3 py-3 text-left font-mono text-xs tracking-[0.18em] uppercase"
              >
                Eyebrow
              </th>
              {TIERS.map((tier) => (
                <td
                  key={tier.id}
                  className="border-cc-card-border text-cc-ink-dim border-l px-3 py-3 font-mono text-xs tracking-[0.18em] uppercase"
                >
                  {tier.eyebrow}
                </td>
              ))}
            </tr>
            <tr className="border-cc-card-border border-b">
              <th
                scope="row"
                className="text-cc-nav-label px-3 py-3 text-left font-mono text-xs tracking-[0.18em] uppercase"
              >
                Price
              </th>
              {TIERS.map((tier) => (
                <td
                  key={tier.id}
                  className="border-cc-card-border border-l px-3 py-3"
                >
                  <span className="font-heading text-cc-heading text-h5 font-semibold tabular-nums">
                    {tier.price}
                  </span>
                  <span className="text-cc-nav-label ml-2 font-mono text-xs">
                    {tier.priceNote}
                  </span>
                </td>
              ))}
            </tr>
            <tr className="border-cc-card-border border-b">
              <th
                scope="row"
                className="text-cc-nav-label px-3 py-3 text-left font-mono text-xs tracking-[0.18em] uppercase"
              >
                Best for
              </th>
              {TIERS.map((tier) => (
                <td
                  key={tier.id}
                  className="border-cc-card-border text-cc-ink border-l px-3 py-3 text-sm leading-relaxed"
                >
                  {tier.bestFor}
                </td>
              ))}
            </tr>
            <tr className="border-cc-card-border border-b">
              <th
                scope="row"
                className="text-cc-nav-label px-3 py-3 text-left font-mono text-xs tracking-[0.18em] uppercase"
              >
                Included
              </th>
              {TIERS.map((tier) => (
                <td
                  key={tier.id}
                  className="border-cc-card-border text-cc-ink border-l px-3 py-3 text-sm leading-relaxed"
                >
                  {tier.perks.join(", ")}
                </td>
              ))}
            </tr>
            <tr>
              <th
                scope="row"
                className="text-cc-nav-label px-3 py-4 text-left font-mono text-xs tracking-[0.18em] uppercase"
              >
                CTA
              </th>
              {TIERS.map((tier) => (
                <td
                  key={tier.id}
                  className="border-cc-card-border border-l px-3 py-4"
                >
                  <div className="flex flex-col gap-2">
                    <SolidButton href={tier.primaryCta.href}>
                      {tier.primaryCta.label}
                    </SolidButton>
                    <OutlineButton href={tier.secondaryCta.href}>
                      {tier.secondaryCta.label}
                    </OutlineButton>
                  </div>
                </td>
              ))}
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  );
}

function EngagementProcedure() {
  return (
    <section aria-labelledby="engagement-heading" className="pt-8 pb-12">
      <RouteLabel>/advisory/02 / engagement</RouteLabel>
      <SectionTitle id="engagement-heading">Engagement procedure</SectionTitle>
      <ol className="divide-cc-card-border border-cc-card-border divide-y border-t border-b">
        {ENGAGEMENT_STEPS.map((step) => (
          <li
            key={step.index}
            className="grid grid-cols-[3ch_1fr_auto] items-baseline gap-4 py-4"
          >
            <span className="text-cc-accent font-mono text-sm tabular-nums">
              {step.index}.
            </span>
            <div>
              <p className="font-heading text-cc-heading text-base font-semibold">
                {step.title}
              </p>
              <p className="text-cc-ink mt-2 text-sm leading-relaxed">
                {step.description}
              </p>
            </div>
            <span className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase tabular-nums">
              {step.micro}
            </span>
          </li>
        ))}
      </ol>
    </section>
  );
}

function CredentialsIndex() {
  return (
    <section aria-labelledby="team-heading" className="pt-8 pb-12">
      <RouteLabel>/advisory/03 / team</RouteLabel>
      <SectionTitle id="team-heading">Team and credentials</SectionTitle>

      <div className="border-cc-card-border md:divide-cc-card-border grid border-t border-b md:grid-cols-3 md:divide-x">
        {CREDENTIALS.map((column) => (
          <div
            key={column.key}
            className="py-4 md:px-4 md:first:pl-0 md:last:pr-0"
          >
            <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
              {column.key}
            </p>
            <h3 className="font-heading text-cc-heading mt-1 text-base font-semibold">
              {column.title}
            </h3>
            <dl className="divide-cc-card-border mt-4 divide-y">
              {column.entries.map((entry) => (
                <div key={entry.term} className="py-3 first:pt-0 last:pb-0">
                  <dt className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
                    {entry.term}
                  </dt>
                  <dd className="text-cc-ink mt-1 text-sm leading-relaxed">
                    {entry.def}
                  </dd>
                </div>
              ))}
            </dl>
          </div>
        ))}
      </div>

      <div className="border-cc-card-border sm:divide-cc-card-border mt-6 grid border-b sm:grid-cols-3 sm:divide-x">
        {PRODUCT_ROSTER.map((row, i) => (
          <div
            key={row.product}
            className={`grid grid-cols-[5rem_1fr] gap-3 py-3 sm:px-4 ${
              i === 0 ? "sm:pl-0" : ""
            } ${i === PRODUCT_ROSTER.length - 1 ? "sm:pr-0" : ""}`}
          >
            <div>
              <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
                Product
              </p>
              <p className="font-heading text-cc-heading mt-1 text-sm font-semibold">
                {row.product}
              </p>
            </div>
            <div>
              <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
                Role
              </p>
              <p className="text-cc-ink mt-1 text-sm leading-relaxed">
                {row.role}
              </p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function FaqIndex() {
  return (
    <section aria-labelledby="faq-heading" className="pt-8 pb-12">
      <RouteLabel>/advisory/04 / faq</RouteLabel>
      <SectionTitle id="faq-heading">FAQ index</SectionTitle>
      <div className="divide-cc-card-border border-cc-card-border divide-y border-t border-b">
        {FAQ.map((item, i) => (
          <details key={item.question} className="group py-3">
            <summary className="grid cursor-pointer list-none grid-cols-[3ch_1fr_auto] items-baseline gap-4">
              <span className="text-cc-accent font-mono text-xs tabular-nums">
                {String(i + 1).padStart(2, "0")}
              </span>
              <span className="font-heading text-cc-heading text-h6 font-semibold">
                <span className="text-cc-nav-label mr-2 font-mono text-xs tracking-[0.18em] uppercase">
                  Q.
                </span>
                {item.question}
              </span>
              <span
                aria-hidden="true"
                className="text-cc-ink-dim font-mono text-xs transition-transform group-open:rotate-45"
              >
                +
              </span>
            </summary>
            <div className="mt-3 grid grid-cols-[3ch_1fr_auto] gap-4">
              <span aria-hidden="true" />
              <p className="text-cc-ink text-body leading-relaxed">
                {item.answer}
              </p>
              <span aria-hidden="true" />
            </div>
          </details>
        ))}
      </div>
    </section>
  );
}

function ContactSpec() {
  return (
    <section aria-labelledby="contact-heading" className="pt-8 pb-16">
      <RouteLabel>/advisory/05 / contact</RouteLabel>
      <SectionTitle id="contact-heading">Contact spec</SectionTitle>
      <p className="text-cc-ink-dim mt-2 max-w-2xl text-sm leading-relaxed">
        Book a 60-minute call. You walk us through what you are building, we ask
        the questions, and you leave with a clear next step. No commitment
        beyond the hour. If we are not the right fit, we will tell you on the
        call.
      </p>

      <dl className="divide-cc-card-border border-cc-card-border mt-8 grid divide-y border-t border-b sm:grid-cols-2 sm:divide-y-0">
        {CONTACT_SPECS.map((spec, i) => (
          <HairlineRow
            key={spec.label}
            label={spec.label}
            value={spec.value}
            bordered={i >= 2}
          />
        ))}
      </dl>

      <div className="border-cc-card-border flex flex-wrap justify-end gap-3 border-b py-4">
        <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
        <OutlineButton href={CONSULTING_MAILTO}>Email us</OutlineButton>
      </div>
    </section>
  );
}
