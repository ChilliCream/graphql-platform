import type { Metadata } from "next";
import Link from "next/link";
import { Fragment } from "react";
import type { ComponentType, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroMonitoring } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Nitro Pricing: Plans for every team | ChilliCream",
  description:
    "Nitro pricing for the ChilliCream GraphQL platform. Start free on shared cloud, scale to a dedicated instance with SSO and SLA, or self-host anywhere.",
  keywords: [
    "Nitro pricing",
    "ChilliCream plans",
    "GraphQL platform pricing",
    "Nitro plans",
    "GraphQL observability pricing",
    "GraphQL schema registry pricing",
    "self-hosted GraphQL",
    "BYOC GraphQL",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Nitro Pricing: Plans for every team",
    description:
      "Start free. Grow to dedicated. Run anywhere. Pricing for the ChilliCream Nitro GraphQL platform: observability, schema registry, CI checks, and the GraphQL IDE.",
  },
};

interface PlanFeature {
  readonly label: string;
}

interface Plan {
  readonly id: "shared" | "dedicated" | "self-hosted";
  readonly chapter: string;
  readonly name: string;
  readonly subtitle: string;
  readonly price: string;
  readonly priceNote: string;
  readonly story: string;
  readonly forWho: string;
  readonly unlocks: string;
  readonly features: readonly PlanFeature[];
  readonly cta: { readonly label: string; readonly href: string };
  readonly popular?: boolean;
}

const PLANS: readonly Plan[] = [
  {
    id: "shared",
    chapter: "Chapter 01",
    name: "Shared Instance",
    subtitle: "Shared resources, fully managed",
    price: "Free",
    priceNote: "pay-as-you-go",
    story:
      "Start the day you decide. No card, no procurement loop. Push a schema, wire up a client, watch the first traces land. The platform you'll grow into is already the platform you start on.",
    forWho: "Solo builders, early teams, weekend experiments.",
    unlocks:
      "A live schema registry, the GraphQL IDE served from your endpoint, and operation telemetry once Nitro is configured for your service.",
    features: [
      { label: "Multi-tenant cloud region" },
      { label: "1 Schema · 3 Environments" },
      { label: "Up to 5M ops / month included" },
      { label: "Community Slack support" },
      { label: "Pay only for what you use after" },
    ],
    cta: { label: "Start for Free", href: "/get-started" },
  },
  {
    id: "dedicated",
    chapter: "Chapter 02",
    name: "Dedicated Instance",
    subtitle: "Dedicated resources, fully managed",
    price: "$400",
    priceNote: "per month",
    story:
      "When your platform stops being a side quest, the rules change. You need a region that's yours, an SLA you can hand to security, and the kind of access controls auditors recognise. Same product, dedicated metal.",
    forWho: "Scale-ups, platform teams, regulated industries.",
    unlocks:
      "A single-tenant region, unlimited schemas, SSO, audit log, role-based access, and a 99.95% SLA with private chat into the engineering team.",
    features: [
      { label: "Single-tenant cloud region" },
      { label: "Unlimited schemas" },
      { label: "BYOC region · private networking" },
      { label: "99.95% SLA · email + private chat" },
      { label: "SSO, audit log, role-based access" },
    ],
    cta: { label: "Start for Free", href: "/get-started" },
    popular: true,
  },
  {
    id: "self-hosted",
    chapter: "Chapter 03",
    name: "Self-Hosted",
    subtitle: "Self managed",
    price: "Custom",
    priceNote: "talk to us",
    story:
      "Some environments don't get a public endpoint. Air-gapped networks, sovereign clouds, the floor of a factory. Run the entire control plane on infrastructure you own, on a release cadence that fits your change windows.",
    forWho: "Public sector, defense, banks, regulated platforms.",
    unlocks:
      "The full Nitro control plane on your infrastructure, priority engineering support, a long-term release channel, and bespoke training for your teams.",
    features: [
      { label: "Run on your own infrastructure" },
      { label: "Air-gapped & on-prem supported" },
      { label: "Priority engineering support" },
      { label: "Long-term release channel" },
      { label: "Custom training & onboarding" },
    ],
    cta: { label: "Talk to Us", href: "/services/support/contact" },
  },
];

interface ComparisonRow {
  readonly capability: string;
  readonly shared: string;
  readonly dedicated: string;
  readonly selfHosted: string;
}

interface ComparisonGroup {
  readonly title: string;
  readonly rows: readonly ComparisonRow[];
}

const COMPARISON: readonly ComparisonGroup[] = [
  {
    title: "Hosting & isolation",
    rows: [
      {
        capability: "Deployment model",
        shared: "Multi-tenant cloud",
        dedicated: "Single-tenant cloud or BYOC",
        selfHosted: "Your infrastructure, air-gap supported",
      },
      {
        capability: "Included operations / month",
        shared: "5M, pay-as-you-go after",
        dedicated: "Custom volume",
        selfHosted: "Unmetered on your infra",
      },
      {
        capability: "Schemas · Environments",
        shared: "1 schema · 3 envs",
        dedicated: "Unlimited · branchable",
        selfHosted: "Unlimited · branchable",
      },
    ],
  },
  {
    title: "Schema lifecycle",
    rows: [
      {
        capability: "Schema registry with history & rollback",
        shared: "Included",
        dedicated: "Included",
        selfHosted: "Included",
      },
      {
        capability: "Client registry (persisted operations)",
        shared: "Included",
        dedicated: "Included",
        selfHosted: "Included",
      },
      {
        capability: "Persisted / trusted operations enforcement",
        shared: "Included",
        dedicated: "Included",
        selfHosted: "Included",
      },
    ],
  },
  {
    title: "Developer experience",
    rows: [
      {
        capability: "MCP server endpoint over Streamable HTTP",
        shared: "Included",
        dedicated: "Included",
        selfHosted: "Included",
      },
      {
        capability: "GraphQL IDE",
        shared: "Served from your endpoint",
        dedicated: "Served from your endpoint",
        selfHosted: "Served from your endpoint",
      },
    ],
  },
  {
    title: "Security & access",
    rows: [
      {
        capability: "SSO (SAML / OIDC)",
        shared: "Not included",
        dedicated: "Included",
        selfHosted: "Via your IdP",
      },
      {
        capability: "Audit log for admin actions",
        shared: "Not included",
        dedicated: "Included",
        selfHosted: "Your retention policy",
      },
    ],
  },
  {
    title: "Support & SLAs",
    rows: [
      {
        capability: "Uptime SLA",
        shared: "Best-effort",
        dedicated: "99.95%",
        selfHosted: "You operate it",
      },
      {
        capability: "Support channel",
        shared: "Community Slack",
        dedicated: "Email + private chat",
        selfHosted: "Priority engineering",
      },
      {
        capability: "Release channel",
        shared: "Continuous",
        dedicated: "Continuous",
        selfHosted: "Long-term release channel",
      },
    ],
  },
];

interface Faq {
  readonly q: string;
  readonly a: ReactNode;
}

const FAQS: readonly Faq[] = [
  {
    q: "How does the 5M ops per month limit work on the Shared plan?",
    a: "The first 5 million operations every month are included at no cost. Beyond that you pay only for the operations you actually run; usage rolls over to a new included bucket each month. You'll never get a surprise gate; we'll surface usage in the dashboard before you hit the line.",
  },
  {
    q: "What does the 99.95% SLA on the Dedicated plan cover?",
    a: "The SLA covers availability of your dedicated control plane region. Schema reads, registry writes, CI checks, and the monitoring API are in scope. The GraphQL IDE serves from your endpoint, so its availability follows your service; telemetry requires Nitro to be configured against the operations you care about.",
  },
  {
    q: "Do you charge per seat?",
    a: "No. Plans price the platform, not the people. SSO, audit log, and role-based access are part of the Dedicated plan at the same monthly price no matter how many engineers you put behind the login.",
  },
  {
    q: "What is BYOC and which clouds do you support?",
    a: "Bring Your Own Cloud means we run a dedicated Nitro region inside an account or subscription you own, with private networking back to your services. We support AWS, Azure, and GCP today; talk to us about other targets, including sovereign clouds.",
  },
  {
    q: "Can I self-host the entire platform?",
    a: "Yes. Self-Hosted runs the same control plane on infrastructure you own, including air-gapped and on-prem deployments. You get a long-term release channel, priority engineering support, and onboarding tailored to your environment.",
  },
  {
    q: "What is your refund policy?",
    a: "Cancel at any time and we'll stop billing at the end of the current cycle. We don't pro-rate partial months. If something has genuinely gone wrong, talk to us; we'd rather fix it than keep the money.",
  },
];

export default function PricingV3Page() {
  return (
    <div className="-mx-5 sm:-mx-12">
      <div className="mx-auto max-w-5xl px-5 sm:px-12">
        <Hero />
      </div>

      <div className="mx-auto mt-20 max-w-5xl px-5 sm:mt-28 sm:px-12">
        <ProductBand />
      </div>

      <div className="mx-auto mt-24 max-w-5xl px-5 sm:mt-36 sm:px-12">
        <PlansSection />
      </div>

      <div className="mx-auto mt-24 max-w-5xl px-5 sm:mt-32 sm:px-12">
        <HonestyBlock />
      </div>

      <div className="mx-auto mt-24 max-w-5xl px-5 sm:mt-32 sm:px-12">
        <ComparisonTable />
      </div>

      <div className="mx-auto mt-24 max-w-5xl px-5 sm:mt-32 sm:px-12">
        <FaqSection />
      </div>

      <div className="mx-auto mt-24 max-w-5xl px-5 sm:mt-32 sm:px-12">
        <EnterpriseBand />
      </div>

      <div className="mx-auto mt-20 mb-8 max-w-5xl px-5 sm:mt-28 sm:px-12">
        <ClosingCta />
      </div>
    </div>
  );
}

/* ---------------------------------------------------------------------- */
/* Sections                                                                */
/* ---------------------------------------------------------------------- */

function Hero() {
  return (
    <section className="relative pt-6 sm:pt-10">
      <Eyebrow>Pricing · Three acts, one platform</Eyebrow>
      <h1 className="font-heading text-cc-heading text-h2 sm:text-hero mt-6 font-semibold tracking-tight">
        Start free.
        <br />
        Grow to dedicated.
        <br />
        <span
          className="bg-clip-text text-transparent"
          style={{
            backgroundImage:
              "linear-gradient(90deg, #16b9e4 0%, #7c92c6 55%, #f0786a 100%)",
          }}
        >
          Run anywhere.
        </span>
      </h1>
      <p className="lead text-cc-ink-dim mt-8 max-w-3xl text-pretty">
        Nitro is the control plane behind your GraphQL APIs: schema registry, CI
        checks, deployments, observability, and the GraphQL IDE. One platform,
        three ways to operate it. Pick the one that fits where you are today,
        switch when you&rsquo;re ready.
      </p>

      <div className="mt-10 flex flex-wrap items-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
        <Link
          href="#plans"
          className="text-cc-nav-label hover:text-cc-ink ml-2 font-mono text-xs tracking-[0.18em] uppercase"
        >
          Jump to plans &rarr;
        </Link>
      </div>

      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-10 right-0 -z-10 h-72 w-72 rounded-full opacity-30 blur-3xl sm:h-96 sm:w-96"
        style={{
          background:
            "radial-gradient(circle, #16b9e4 0%, rgba(124, 146, 198, 0.4) 45%, transparent 70%)",
        }}
      />
    </section>
  );
}

function ProductBand() {
  return (
    <section aria-labelledby="product-band-heading">
      <div className="flex items-end justify-between gap-6">
        <div>
          <Eyebrow>Before the price</Eyebrow>
          <h2
            id="product-band-heading"
            className="font-heading text-cc-heading text-h3 mt-4 font-semibold"
          >
            What every plan gets you
          </h2>
        </div>
        <p className="text-cc-ink-dim hidden max-w-sm text-sm sm:block">
          Same control plane, same dashboards. The plan changes where it runs
          and how it scales, not what you see when you open it.
        </p>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-8 max-w-5xl overflow-hidden rounded-xl border">
        <NitroMonitoring />
      </div>

      <p className="text-cc-ink-dim mt-4 text-center font-mono text-xs tracking-wide sm:text-left">
        Live monitoring overview · the same dashboard ships in every plan
      </p>
    </section>
  );
}

function PlansSection() {
  return (
    <section id="plans" aria-labelledby="plans-heading">
      <Eyebrow>The plans</Eyebrow>
      <h2
        id="plans-heading"
        className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 font-semibold"
      >
        Three chapters. Same story.
      </h2>
      <p className="text-cc-ink-dim mt-5 max-w-2xl text-pretty">
        Every plan ships the full Nitro control plane. The difference is where
        it lives and what&rsquo;s around it.
      </p>

      <div className="mt-16 flex flex-col gap-20 sm:gap-28">
        {PLANS.map((plan, index) => (
          <PlanStory key={plan.id} plan={plan} index={index} />
        ))}
      </div>
    </section>
  );
}

interface PlanStoryProps {
  readonly plan: Plan;
  readonly index: number;
}

function PlanStory({ plan, index }: PlanStoryProps) {
  const reversed = index % 2 === 1;
  return (
    <article
      className={`grid items-center gap-10 lg:grid-cols-12 lg:gap-12 ${
        reversed ? "lg:[&>*:first-child]:order-2" : ""
      }`}
    >
      <div className="lg:col-span-5">
        <p className="text-cc-accent font-mono text-xs tracking-[0.22em] uppercase">
          {plan.chapter}
        </p>
        <h3 className="font-heading text-cc-heading text-h3 mt-4 font-semibold">
          {plan.name}
        </h3>
        <p className="text-cc-nav-label mt-2 font-mono text-xs">
          {plan.subtitle}
        </p>
        <p className="text-cc-ink mt-6 text-pretty">{plan.story}</p>

        <dl className="mt-8 grid gap-4">
          <div>
            <dt className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              For
            </dt>
            <dd className="text-cc-ink mt-1 text-sm">{plan.forWho}</dd>
          </div>
          <div>
            <dt className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              Unlocks
            </dt>
            <dd className="text-cc-ink mt-1 text-sm">{plan.unlocks}</dd>
          </div>
        </dl>
      </div>

      <div className="lg:col-span-7">
        <PlanCard plan={plan} />
      </div>
    </article>
  );
}

function PlanCard({ plan }: { readonly plan: Plan }) {
  const Cta = plan.popular ? SolidButton : OutlineButton;
  return (
    <div
      className={`relative overflow-hidden rounded-3xl border p-7 sm:p-9 ${
        plan.popular
          ? "border-cc-accent bg-cc-card-bg"
          : "border-cc-card-border bg-cc-card-bg/60"
      }`}
    >
      {plan.popular && (
        <div className="absolute top-5 right-5">
          <span className="border-cc-accent text-cc-accent rounded-full border px-3 py-1 font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            Most Popular
          </span>
        </div>
      )}

      {plan.popular && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -top-24 -right-24 h-64 w-64 rounded-full opacity-25 blur-3xl"
          style={{
            background: "radial-gradient(circle, #5eead4 0%, transparent 70%)",
          }}
        />
      )}

      <div className="relative">
        <div className="flex items-baseline gap-3">
          <span className="font-heading text-cc-heading text-hero font-semibold">
            {plan.price}
          </span>
          <span className="text-cc-nav-label font-mono text-xs">
            {plan.priceNote}
          </span>
        </div>

        <div className="border-cc-ink-faint my-7 border-t border-dashed" />

        <ul className="grid gap-3 sm:grid-cols-2">
          {plan.features.map((feature) => (
            <li key={feature.label} className="flex items-start gap-3">
              <span className="text-cc-accent mt-1 flex-none">
                <CheckIcon />
              </span>
              <span className="text-cc-ink text-sm">{feature.label}</span>
            </li>
          ))}
        </ul>

        <div className="mt-8 flex flex-wrap items-center gap-4">
          <Cta href={plan.cta.href} className="min-w-[10rem]">
            {plan.cta.label}
          </Cta>
          {plan.id !== "self-hosted" && (
            <Link
              href="/docs"
              className="text-cc-nav-label hover:text-cc-ink font-mono text-xs tracking-[0.18em] uppercase"
            >
              Read the docs &rarr;
            </Link>
          )}
        </div>
      </div>
    </div>
  );
}

function HonestyBlock() {
  return (
    <section aria-labelledby="honesty-heading">
      <Eyebrow>How we price</Eyebrow>
      <h2
        id="honesty-heading"
        className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 font-semibold"
      >
        The honest version.
      </h2>
      <p className="text-cc-ink-dim mt-5 max-w-2xl text-pretty">
        Pricing pages are usually written to make the cheap plan unusable and
        the expensive plan inevitable. We try not to do that. Here is
        what&rsquo;s actually true.
      </p>

      <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        <HonestyCard
          title="No per-seat tax"
          body="Plans price the platform, not the people behind it. Add the whole engineering org to your workspace; the price stays the same."
        />
        <HonestyCard
          title="Pay-as-you-go after 5M"
          body="The Shared plan includes 5 million operations a month. After that you pay only for the operations you actually run, billed against the same dashboard you'd use anyway."
        />
        <HonestyCard
          title="BYOC, explained plainly"
          body="Bring Your Own Cloud puts a dedicated Nitro region inside your AWS, Azure, or GCP account, with private networking back to your services. Your data, your perimeter, our control plane."
        />
        <HonestyCard
          title="The free plan is not a trial"
          body="Shared isn't a 14-day countdown. Stay there for as long as it fits. The day you outgrow it, your schemas, environments, and history move with you."
        />
        <HonestyCard
          title="SLA covers the platform"
          body="The 99.95% SLA on Dedicated covers the Nitro control plane region. Telemetry requires Nitro to be configured against the operations you want to see; the GraphQL IDE serves from your endpoint."
        />
        <HonestyCard
          title="Self-hosting is a first-class plan"
          body="Self-Hosted runs the same product as Dedicated, on your infrastructure, on a long-term release channel, for environments where public endpoints aren't an option."
        />
      </div>
    </section>
  );
}

function HonestyCard({
  title,
  body,
}: {
  readonly title: string;
  readonly body: string;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover h-full rounded-2xl border p-6 transition-colors">
      <h3 className="font-heading text-cc-heading text-h6 font-semibold">
        {title}
      </h3>
      <p className="text-cc-ink mt-3 text-sm text-pretty">{body}</p>
    </div>
  );
}

function ComparisonTable() {
  return (
    <section aria-labelledby="compare-heading">
      <Eyebrow>Compare</Eyebrow>
      <h2
        id="compare-heading"
        className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 font-semibold"
      >
        The capabilities, side by side.
      </h2>
      <p className="text-cc-ink-dim mt-5 max-w-2xl text-pretty">
        Same control plane in every column. The columns differ in where it runs
        and what wraps around it.
      </p>

      <div className="border-cc-card-border bg-cc-card-bg/60 mt-10 overflow-hidden rounded-2xl border">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[640px] border-collapse text-left">
            <thead>
              <tr className="border-cc-card-border border-b">
                <th
                  scope="col"
                  className="text-cc-nav-label px-5 py-4 font-mono text-[0.65rem] tracking-[0.18em] uppercase"
                >
                  Capability
                </th>
                <th
                  scope="col"
                  className="text-cc-nav-label px-5 py-4 font-mono text-[0.65rem] tracking-[0.18em] uppercase"
                >
                  Shared
                </th>
                <th
                  scope="col"
                  className="text-cc-accent px-5 py-4 font-mono text-[0.65rem] tracking-[0.18em] uppercase"
                >
                  Dedicated
                </th>
                <th
                  scope="col"
                  className="text-cc-nav-label px-5 py-4 font-mono text-[0.65rem] tracking-[0.18em] uppercase"
                >
                  Self-Hosted
                </th>
              </tr>
            </thead>
            <tbody>
              {COMPARISON.map((group, gi) => {
                const isLastGroup = gi === COMPARISON.length - 1;
                return (
                  <Fragment key={group.title}>
                    <tr className="border-cc-card-border bg-cc-card-bg/40 border-b">
                      <th
                        scope="colgroup"
                        colSpan={4}
                        className="text-cc-nav-label px-5 py-3 font-mono text-[0.65rem] tracking-[0.18em] uppercase"
                      >
                        {group.title}
                      </th>
                    </tr>
                    {group.rows.map((row, ri) => {
                      const isLastRow =
                        isLastGroup && ri === group.rows.length - 1;
                      return (
                        <tr
                          key={row.capability}
                          className={
                            isLastRow ? "" : "border-cc-card-border border-b"
                          }
                        >
                          <th
                            scope="row"
                            className="text-cc-heading px-5 py-4 text-sm font-medium"
                          >
                            {row.capability}
                          </th>
                          <td className="text-cc-ink px-5 py-4 text-sm">
                            {row.shared}
                          </td>
                          <td className="text-cc-heading px-5 py-4 text-sm">
                            {row.dedicated}
                          </td>
                          <td className="text-cc-ink px-5 py-4 text-sm">
                            {row.selfHosted}
                          </td>
                        </tr>
                      );
                    })}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}

function FaqSection() {
  return (
    <section aria-labelledby="faq-heading">
      <Eyebrow>Questions</Eyebrow>
      <h2
        id="faq-heading"
        className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 font-semibold"
      >
        Frequently asked.
      </h2>
      <p className="text-cc-ink-dim mt-5 max-w-2xl text-pretty">
        Six answers to the questions we hear most often. Have a seventh?
        <Link
          href="/services/support/contact"
          className="text-cc-accent hover:text-cc-accent-hover ml-1"
        >
          Ask us directly
        </Link>
        .
      </p>

      <dl className="mt-10 grid gap-4">
        {FAQS.map((faq) => (
          <FaqItem key={faq.q} q={faq.q} a={faq.a} />
        ))}
      </dl>
    </section>
  );
}

function FaqItem({ q, a }: Faq) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-card-border-hover rounded-2xl border p-6 transition-colors sm:p-7">
      <dt className="font-heading text-cc-heading text-h6 font-semibold">
        {q}
      </dt>
      <dd className="text-cc-ink mt-3 text-sm text-pretty">{a}</dd>
    </div>
  );
}

function EnterpriseBand() {
  return (
    <section
      aria-labelledby="enterprise-heading"
      className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-3xl border p-8 sm:p-12"
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -top-32 -right-32 h-96 w-96 rounded-full opacity-25 blur-3xl"
        style={{
          background:
            "radial-gradient(circle, #f0786a 0%, rgba(240, 120, 106, 0.2) 45%, transparent 75%)",
        }}
      />
      <div className="relative grid items-center gap-10 lg:grid-cols-[3fr_2fr]">
        <div>
          <Eyebrow>Enterprise</Eyebrow>
          <h2
            id="enterprise-heading"
            className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 font-semibold"
          >
            Bigger footprint? Let&rsquo;s design it together.
          </h2>
          <p className="text-cc-ink mt-5 max-w-xl text-pretty">
            BYOC across multiple regions, custom procurement, security review,
            vendor onboarding, training for a hundred engineers, something
            stranger. The engineering team takes these conversations directly,
            no SDR layer in between.
          </p>
        </div>
        <div className="flex flex-col gap-4 lg:items-end">
          <SolidButton href="/services/support/contact">
            Talk to Engineering
          </SolidButton>
          <Link
            href="/platform"
            className="text-cc-nav-label hover:text-cc-ink font-mono text-xs tracking-[0.18em] uppercase"
          >
            Tour the platform &rarr;
          </Link>
        </div>
      </div>
    </section>
  );
}

function ClosingCta() {
  return (
    <section
      aria-labelledby="closing-heading"
      className="py-12 text-center sm:py-16"
    >
      <h2
        id="closing-heading"
        className="font-heading text-cc-heading text-h2 sm:text-hero font-semibold tracking-tight"
      >
        Pick a chapter.
        <br />
        <span
          className="bg-clip-text text-transparent"
          style={{
            backgroundImage:
              "linear-gradient(90deg, #16b9e4 0%, #7c92c6 55%, #f0786a 100%)",
          }}
        >
          Start writing.
        </span>
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-6 max-w-xl text-pretty">
        Free forever on Shared. Move to Dedicated when scale, SLA, or SSO ask
        for it. Run on your own infrastructure when the world asks for that.
      </p>
      <div className="mt-10 flex flex-wrap justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
      </div>
    </section>
  );
}

/* ---------------------------------------------------------------------- */
/* Primitives                                                              */
/* ---------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-xs tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

// Asserts that the explicit component shapes above match React's expected
// component type, so refactors that change the prop interfaces fail at the
// type level instead of silently at runtime.
type _PlanStoryGuard = ComponentType<PlanStoryProps>;
const _planStoryGuard: _PlanStoryGuard = PlanStory;
void _planStoryGuard;
