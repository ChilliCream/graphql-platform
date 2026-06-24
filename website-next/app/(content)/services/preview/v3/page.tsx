import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroReel } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Services: book a GraphQL engineer this week",
  description:
    "Embed ChilliCream GraphQL experts with your .NET team. Book a 60-minute consult, pick a support plan, or train your team on Hot Chocolate and Fusion.",
  keywords: [
    "GraphQL consulting",
    "Hot Chocolate support",
    "Fusion advisory",
    "Nitro telemetry",
    ".NET GraphQL training",
    "ChilliCream services",
  ],
  openGraph: {
    title: "Services: book a GraphQL engineer this week",
    description:
      "Embed ChilliCream GraphQL experts with your .NET team. Book a 60-minute consult, pick a support plan, or train your team on Hot Chocolate and Fusion.",
  },
  robots: { index: false, follow: false },
};

const CALENDLY_URL = "https://calendly.com/chillicream/60min";
const CONTACT_MAILTO = "mailto:contact@chillicream.com";

interface OfferCardProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly tagline: string;
  readonly bullets: readonly string[];
  readonly footnote: string;
  readonly primaryHref: string;
  readonly primaryLabel: string;
  readonly secondaryHref?: string;
  readonly secondaryLabel?: string;
  readonly accentClass: string;
}

function OfferCard({
  eyebrow,
  title,
  tagline,
  bullets,
  footnote,
  primaryHref,
  primaryLabel,
  secondaryHref,
  secondaryLabel,
  accentClass,
}: OfferCardProps) {
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex flex-col rounded-2xl border p-7 backdrop-blur-sm transition-colors">
      <div className={`absolute inset-x-7 top-0 h-px ${accentClass}`} />
      <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
        {eyebrow}
      </span>
      <h3 className="text-cc-heading font-heading text-h4 mt-3">{title}</h3>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{tagline}</p>
      <ul className="text-cc-ink mt-5 flex flex-col gap-2.5 text-sm">
        {bullets.map((b) => (
          <li key={b} className="flex items-start gap-2.5">
            <span className="text-cc-accent mt-[3px] shrink-0">
              <CheckIcon />
            </span>
            <span>{b}</span>
          </li>
        ))}
      </ul>
      <p className="text-cc-ink-dim mt-5 text-xs leading-relaxed">{footnote}</p>
      <div className="mt-6 flex flex-wrap items-center gap-3">
        <SolidButton href={primaryHref}>{primaryLabel}</SolidButton>
        {secondaryHref && secondaryLabel ? (
          <a
            href={secondaryHref}
            className="text-cc-accent hover:text-cc-heading text-sm font-medium no-underline transition-colors"
          >
            {secondaryLabel} →
          </a>
        ) : null}
      </div>
    </article>
  );
}

interface StepProps {
  readonly index: string;
  readonly title: string;
  readonly body: string;
  readonly meta: string;
}

function Step({ index, title, body, meta }: StepProps) {
  return (
    <li className="border-cc-card-border bg-cc-card-bg/60 relative flex flex-col rounded-xl border p-6">
      <div className="flex items-baseline gap-3">
        <span className="text-cc-accent font-heading text-h3 leading-none">
          {index}
        </span>
        <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
          {meta}
        </span>
      </div>
      <h3 className="text-cc-heading font-heading text-h5 mt-3">{title}</h3>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{body}</p>
    </li>
  );
}

interface DecisionRowProps {
  readonly situation: string;
  readonly recommendation: ReactNode;
}

function DecisionRow({ situation, recommendation }: DecisionRowProps) {
  return (
    <div className="border-cc-card-border/70 grid gap-2 border-t py-4 first:border-t-0 md:grid-cols-[1fr_1fr] md:items-center md:gap-6">
      <p className="text-cc-ink text-sm leading-relaxed">{situation}</p>
      <p className="text-cc-heading text-sm font-medium">{recommendation}</p>
    </div>
  );
}

export default function ServicesPreviewV3() {
  return (
    <>
      {/* Hero */}
      <section className="relative mx-auto w-full max-w-6xl px-6 pt-20 pb-14 md:pt-28 md:pb-20">
        <div className="flex flex-col items-start gap-6 md:items-center md:text-center">
          <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.22em] uppercase">
            ChilliCream Services
          </span>
          <h1 className="text-cc-heading font-heading text-hero max-w-3xl">
            Embed a core engineer on your{" "}
            <span className="bg-gradient-to-r from-[#16b9e4] via-[#7c92c6] to-[#f0786a] bg-clip-text text-transparent">
              GraphQL
            </span>{" "}
            team this week.
          </h1>
          <p className="text-cc-ink lead max-w-2xl">
            Talk to a core engineer about your GraphQL problem this week. We
            work alongside .NET teams shipping Hot Chocolate, Fusion, and Nitro
            in production. Bring the schema, the trace, the deadline. We bring
            the fix.
          </p>
          <div className="mt-2 flex flex-wrap items-center gap-3 md:justify-center">
            <SolidButton href={CALENDLY_URL}>
              Book a 60-minute consult
            </SolidButton>
            <OutlineButton href="/services/support">
              See support plans
            </OutlineButton>
          </div>
          <ul className="text-cc-ink-dim mt-4 flex flex-wrap items-center gap-x-6 gap-y-2 text-xs md:justify-center">
            <li className="flex items-center gap-2">
              <span className="text-cc-success">
                <CheckIcon />
              </span>
              Same engineers who maintain Hot Chocolate
            </li>
            <li className="flex items-center gap-2">
              <span className="text-cc-success">
                <CheckIcon />
              </span>
              Most engagements start within 1-2 weeks
            </li>
            <li className="flex items-center gap-2">
              <span className="text-cc-success">
                <CheckIcon />
              </span>
              Fixed scope or retainer
            </li>
          </ul>
        </div>
      </section>

      {/* Nitro reel anchor */}
      <section className="mx-auto w-full max-w-6xl px-6 pb-16">
        <div className="border-cc-card-border bg-cc-card-bg mx-auto max-w-5xl overflow-hidden rounded-xl border">
          <NitroReel />
        </div>
        <p className="text-cc-ink-dim mt-4 text-center text-xs">
          What we ship with you: authoring, observability, diagnostics, schema
          governance, and Fusion composition. Built by the same team that will
          sit with yours.
        </p>
      </section>

      {/* Three offer cards */}
      <section className="mx-auto w-full max-w-6xl px-6 pb-20">
        <div className="mb-10 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div>
            <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
              What you get
            </span>
            <h2 className="text-cc-heading font-heading text-h2 mt-2">
              Three ways to put us to work.
            </h2>
          </div>
          <p className="text-cc-ink-dim max-w-md text-sm">
            Each engagement is staffed by the engineers who write the libraries
            you depend on. Pick the shape that fits, or start with a consult and
            we will tell you which one.
          </p>
        </div>

        <div className="grid gap-6 md:grid-cols-3">
          <OfferCard
            eyebrow="01 / Advisory"
            title="Advisory"
            tagline="Consulting and contracting from the people who build Hot Chocolate and Fusion."
            bullets={[
              "Hourly consulting on architecture, schema, and performance",
              "Code review on a PR, a service, or a whole gateway",
              "Contracting for proofs of concept and implementations",
              "Mentoring that sticks with your team after we leave",
            ]}
            footnote="Best for: a specific question you want answered well, or a build you want done right."
            primaryHref="/services/advisory"
            primaryLabel="Explore advisory"
            secondaryHref={CALENDLY_URL}
            secondaryLabel="Book a consult"
            accentClass="bg-gradient-to-r from-transparent via-[#16b9e4] to-transparent"
          />
          <OfferCard
            eyebrow="02 / Support"
            title="Support"
            tagline="An SLA-backed line to a core engineer when production is on fire, or before it is."
            bullets={[
              "Community: free public Slack channel",
              "Startup: $450/mo, private Slack, 2 critical incidents",
              "Business: $1,300/mo for business-critical workloads",
              "Enterprise: custom SLAs, phone support, dedicated account manager",
            ]}
            footnote="Best for: production Hot Chocolate, Fusion, or Nitro deployments that cannot wait on a forum thread."
            primaryHref="/services/support"
            primaryLabel="Compare support plans"
            secondaryHref="/services/support/contact"
            secondaryLabel="Talk to sales"
            accentClass="bg-gradient-to-r from-transparent via-[#7c92c6] to-transparent"
          />
          <OfferCard
            eyebrow="03 / Training"
            title="Training"
            tagline="Bring your team to the same page, fast, with curriculum built around your stack."
            bullets={[
              "Corporate Training: tailored to beginner, advanced, or mixed teams",
              "Corporate Workshop: hands-on Hot Chocolate, ASP.NET Core, React, Relay",
              "Real projects, real schemas, real production gotchas",
              "On-site or remote, in your tooling and your repos",
            ]}
            footnote="Best for: a team that needs to ship a GraphQL surface together, not one senior who already gets it."
            primaryHref="/services/training"
            primaryLabel="See training options"
            secondaryHref={`${CONTACT_MAILTO}?subject=Training`}
            secondaryLabel="Request a syllabus"
            accentClass="bg-gradient-to-r from-transparent via-[#f0786a] to-transparent"
          />
        </div>
      </section>

      {/* How an engagement starts */}
      <section className="mx-auto w-full max-w-6xl px-6 pb-20">
        <div className="mb-10">
          <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
            How a service engagement starts
          </span>
          <h2 className="text-cc-heading font-heading text-h2 mt-2 max-w-2xl">
            From inbox to embedded, in three steps.
          </h2>
        </div>
        <ol className="grid gap-5 md:grid-cols-3">
          <Step
            index="01"
            meta="Day 0"
            title="Book the 60-minute consult"
            body="Drop a calendar link, tell us what hurts. We come prepared with questions about your schema, your runtime, and your team. No slideware, no discovery deck."
          />
          <Step
            index="02"
            meta="Day 1 to 3"
            title="We scope the work, in writing"
            body="You get a one-pager: outcomes, timeline, who staffs it, what it costs. Fixed scope for short engagements, weekly retainer for longer ones. You decide."
          />
          <Step
            index="03"
            meta="Day 5 to 7"
            title="We start work, in your repo"
            body="A core engineer joins your Slack and your repository. Reviews, pairing sessions, and shipped PRs. Status is the work, not a report about the work."
          />
        </ol>
      </section>

      {/* Which one is right for me */}
      <section className="mx-auto w-full max-w-6xl px-6 pb-20">
        <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8 md:p-10">
          <div className="mb-6 flex flex-col gap-2 md:flex-row md:items-end md:justify-between">
            <h2 className="text-cc-heading font-heading text-h3">
              Which one is right for me?
            </h2>
            <p className="text-cc-ink-dim text-sm">
              Or skip the matrix and{" "}
              <a
                href={CALENDLY_URL}
                className="text-cc-accent hover:text-cc-heading no-underline"
              >
                book a consult
              </a>
              . We will tell you.
            </p>
          </div>
          <div className="border-cc-card-border/70 grid gap-0 md:grid-cols-[1fr_1fr]">
            <div className="hidden md:contents">
              <div className="text-cc-nav-label border-cc-card-border/70 border-b pb-3 font-mono text-[11px] tracking-[0.18em] uppercase">
                You are here
              </div>
              <div className="text-cc-nav-label border-cc-card-border/70 border-b pb-3 font-mono text-[11px] tracking-[0.18em] uppercase">
                Start with
              </div>
            </div>
            <div className="md:contents">
              <DecisionRow
                situation="One nagging architecture call you cannot get wrong. Federation boundaries, auth, or schema design."
                recommendation={
                  <>
                    <a
                      href="/services/advisory"
                      className="text-cc-accent hover:text-cc-heading no-underline"
                    >
                      Advisory
                    </a>{" "}
                    hourly consulting, or a 60-minute consult.
                  </>
                }
              />
              <DecisionRow
                situation="Production Hot Chocolate or Fusion, and a forum thread is not a plan when latency spikes."
                recommendation={
                  <>
                    <a
                      href="/services/support"
                      className="text-cc-accent hover:text-cc-heading no-underline"
                    >
                      Support
                    </a>{" "}
                    Startup or Business plan, with SLAs in writing.
                  </>
                }
              />
              <DecisionRow
                situation="A team that needs to learn GraphQL the right way, on the codebase they will actually ship."
                recommendation={
                  <>
                    <a
                      href="/services/training"
                      className="text-cc-accent hover:text-cc-heading no-underline"
                    >
                      Training
                    </a>{" "}
                    Corporate Training or a hands-on Workshop.
                  </>
                }
              />
              <DecisionRow
                situation="A new GraphQL service due in weeks, and the team is stretched thin."
                recommendation={
                  <>
                    <a
                      href="/services/advisory"
                      className="text-cc-accent hover:text-cc-heading no-underline"
                    >
                      Advisory
                    </a>{" "}
                    contracting. We build, you own the result.
                  </>
                }
              />
              <DecisionRow
                situation="Multiple BUs, procurement, compliance, named-contact requirements."
                recommendation={
                  <>
                    Enterprise{" "}
                    <a
                      href="/services/support/contact"
                      className="text-cc-accent hover:text-cc-heading no-underline"
                    >
                      Support
                    </a>{" "}
                    with custom SLAs and account management.
                  </>
                }
              />
            </div>
          </div>
        </div>
      </section>

      {/* Enterprise band */}
      <section className="mx-auto w-full max-w-6xl px-6 pb-20">
        <div className="border-cc-card-border bg-cc-surface/70 relative overflow-hidden rounded-2xl border p-8 md:p-12">
          <div
            aria-hidden
            className="pointer-events-none absolute inset-y-0 right-0 hidden w-1/2 opacity-60 md:block"
          >
            <svg
              viewBox="0 0 400 300"
              className="h-full w-full"
              preserveAspectRatio="xMaxYMid slice"
            >
              <defs>
                <radialGradient id="svc-v3-glow" cx="80%" cy="50%" r="60%">
                  <stop offset="0%" stopColor="#5eead4" stopOpacity="0.35" />
                  <stop offset="60%" stopColor="#5eead4" stopOpacity="0.05" />
                  <stop offset="100%" stopColor="#5eead4" stopOpacity="0" />
                </radialGradient>
                <linearGradient id="svc-v3-line" x1="0" y1="0" x2="1" y2="0">
                  <stop offset="0%" stopColor="#5eead4" stopOpacity="0" />
                  <stop offset="100%" stopColor="#5eead4" stopOpacity="0.4" />
                </linearGradient>
              </defs>
              <rect width="400" height="300" fill="url(#svc-v3-glow)" />
              {[80, 120, 160, 200, 240].map((y) => (
                <line
                  key={y}
                  x1="20"
                  y1={y}
                  x2="380"
                  y2={y}
                  stroke="url(#svc-v3-line)"
                  strokeWidth="1"
                />
              ))}
            </svg>
          </div>
          <div className="relative max-w-2xl">
            <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.18em] uppercase">
              Enterprise
            </span>
            <h2 className="text-cc-heading font-heading text-h2 mt-2">
              For platforms that cannot fail quietly.
            </h2>
            <p className="text-cc-ink mt-4 max-w-xl text-sm leading-relaxed">
              Custom SLAs, a dedicated account manager, phone support,
              procurement paperwork done properly. If you run Hot Chocolate,
              Fusion, or Nitro across business units, talk to us before the next
              incident, not during it.
            </p>
            <div className="mt-6 flex flex-wrap items-center gap-3">
              <SolidButton href="/services/support/contact">
                Contact enterprise sales
              </SolidButton>
              <OutlineButton href={CONTACT_MAILTO}>
                Email contact@chillicream.com
              </OutlineButton>
            </div>
          </div>
        </div>
      </section>

      {/* Closing CTA */}
      <section className="mx-auto w-full max-w-4xl px-6 pb-28 text-center">
        <h2 className="text-cc-heading font-heading text-h2">
          Stop debugging in isolation.
        </h2>
        <p className="text-cc-ink lead mx-auto mt-4 max-w-2xl">
          A 60-minute call with a core engineer is the cheapest way to find out
          whether you have a tooling problem, a schema problem, or a runtime
          problem. Most of the time, you leave the call with the answer.
        </p>
        <div className="mt-7 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href={CALENDLY_URL}>
            Book a 60-minute consult
          </SolidButton>
          <OutlineButton href="/services/support">
            See support plans
          </OutlineButton>
        </div>
      </section>
    </>
  );
}
