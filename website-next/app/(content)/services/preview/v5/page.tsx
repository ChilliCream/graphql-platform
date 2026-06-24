import type { Metadata } from "next";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "ChilliCream Services: Advisory, Support, Training",
  description:
    "ChilliCream GraphQL services documented as code: Advisory, Support plans from $450 per month, and Corporate Training, from the Hot Chocolate team.",
  robots: { index: false, follow: false },
};

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONTACT_MAILTO = "mailto:contact@chillicream.com";
const ENTERPRISE_MAILTO =
  "mailto:contact@chillicream.com?subject=Enterprise%20Services";
const SUPPORT_CONTACT = "/services/support/contact";

// Shared token classes for the code-walkthrough motif.
const KEYWORD = "text-cc-accent";
const STRING = "text-cc-ink-dim";
const COMMENT = "text-cc-nav-label";
const PUNCT = "text-cc-ink";
const IDENT = "text-cc-heading";

interface CodePanelProps {
  readonly filename: string;
  readonly language?: string;
  readonly children: ReactNode;
  readonly ariaLabel?: string;
}

function CodePanel({
  filename,
  language,
  children,
  ariaLabel,
}: CodePanelProps) {
  return (
    <figure
      aria-label={ariaLabel ?? `${filename} code panel`}
      className="border-cc-card-border bg-cc-code-bg overflow-hidden rounded-2xl border shadow-[0_24px_60px_-30px_rgba(0,0,0,0.6)]"
    >
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-3 border-b px-4 py-2.5">
        <span className="flex items-center gap-1.5" aria-hidden="true">
          <span className="block h-2.5 w-2.5 rounded-full bg-[#ff5f57]" />
          <span className="block h-2.5 w-2.5 rounded-full bg-[#febc2e]" />
          <span className="block h-2.5 w-2.5 rounded-full bg-[#28c840]" />
        </span>
        <span className="text-cc-ink-dim text-caption font-mono">
          {filename}
        </span>
        {language ? (
          <span className="text-cc-nav-label text-caption ml-auto font-mono tracking-[0.18em] uppercase">
            {language}
          </span>
        ) : null}
      </div>
      <pre className="text-cc-ink text-body m-0 overflow-x-auto px-5 py-5 font-mono leading-relaxed">
        <code className="block">{children}</code>
      </pre>
    </figure>
  );
}

interface CodeLineProps {
  readonly n: number;
  readonly children: ReactNode;
  readonly indent?: number;
}

function L({ n, children, indent = 0 }: CodeLineProps) {
  return (
    <span className="flex">
      <span
        aria-hidden="true"
        className="text-cc-nav-label w-8 flex-none pr-3 text-right opacity-60 select-none"
      >
        {n}
      </span>
      <span className="flex-1 whitespace-pre">
        {"  ".repeat(indent)}
        {children}
      </span>
    </span>
  );
}

interface SectionShellProps {
  readonly id?: string;
  readonly marker: string;
  readonly eyebrow: string;
  readonly heading: string;
  readonly headingId: string;
  readonly children: ReactNode;
  readonly code: ReactNode;
}

function SectionShell({
  id,
  marker,
  eyebrow,
  heading,
  headingId,
  children,
  code,
}: SectionShellProps) {
  return (
    <section
      id={id}
      aria-labelledby={headingId}
      className="relative py-16 sm:py-20"
    >
      <span
        aria-hidden="true"
        className="text-cc-accent text-caption absolute top-16 left-0 font-mono tracking-[0.18em] uppercase sm:top-20"
      >
        {marker}
      </span>
      <div className="grid gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-4">
          <div className="lg:sticky lg:top-24">
            <p className="text-cc-accent text-caption font-mono tracking-[0.18em] uppercase">
              {eyebrow}
            </p>
            <h2
              id={headingId}
              className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
            >
              {heading}
            </h2>
            <div className="text-cc-ink text-lead mt-5 space-y-4 text-pretty">
              {children}
            </div>
          </div>
        </div>
        <div className="lg:col-span-8">{code}</div>
      </div>
    </section>
  );
}

export default function ServicesPreviewV5Page() {
  return (
    <div className="relative mx-auto max-w-4xl px-1 sm:px-2">
      <DashedGuideline />
      <Hero />
      <AdvisorySection />
      <SupportSection />
      <TrainingSection />
      <DecisionSection />
      <EnterpriseSection />
      <ClosingSection />
    </div>
  );
}

function DashedGuideline() {
  return (
    <span
      aria-hidden="true"
      className="border-cc-ink-faint pointer-events-none absolute top-32 bottom-32 left-3 hidden border-l border-dashed sm:left-5 lg:block"
    />
  );
}

function Hero() {
  return (
    <section className="pt-12 pb-10 sm:pt-16 sm:pb-12">
      <div className="grid gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-5">
          <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
            ChilliCream Services
          </p>
          <h1
            className="font-heading text-cc-heading text-hero mt-5 font-semibold tracking-tight"
            style={{
              backgroundImage:
                "linear-gradient(120deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
              WebkitBackgroundClip: "text",
              backgroundClip: "text",
              color: "transparent",
            }}
          >
            Pick the right level of help for your GraphQL stack.
          </h1>
          <p className="text-cc-ink text-lead mt-6 max-w-xl text-pretty">
            Three ways to work with the team behind Hot Chocolate, Fusion, and
            Nitro: hands-on Advisory, ongoing Support plans, or Corporate
            Training. Read the manifest, then pick a track.
          </p>
          <div className="mt-8 flex flex-wrap items-center gap-3">
            <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
            <OutlineButton href="#decide">Help me choose</OutlineButton>
          </div>
        </div>
        <div className="lg:col-span-7">
          <CodePanel
            filename="hero.graphql"
            language="graphql"
            ariaLabel="Hero GraphQL query"
          >
            <L n={1}>
              <span
                className={COMMENT}
              >{`# ChilliCream GraphQL services`}</span>
            </L>
            <L n={2}>
              <span
                className={COMMENT}
              >{`# Pick the right level of help.`}</span>
            </L>
            <L n={3}>{` `}</L>
            <L n={4}>
              <span className={KEYWORD}>query</span>{" "}
              <span className={IDENT}>Services</span>
              <span className={PUNCT}>{` {`}</span>
            </L>
            <L n={5} indent={1}>
              <span className={IDENT}>chillicream</span>
              <span className={PUNCT}>{` {`}</span>
            </L>
            <L n={6} indent={2}>
              <span className={IDENT}>advisory</span>
              <span className={PUNCT}>{` {`}</span>{" "}
              <span className={IDENT}>hourly</span>{" "}
              <span className={IDENT}>contracting</span>{" "}
              <span className={PUNCT}>{`}`}</span>
            </L>
            <L n={7} indent={2}>
              <span className={IDENT}>support</span>
              <span className={PUNCT}>{` {`}</span>{" "}
              <span className={IDENT}>community</span>{" "}
              <span className={IDENT}>business</span>{" "}
              <span className={IDENT}>enterprise</span>{" "}
              <span className={PUNCT}>{`}`}</span>
            </L>
            <L n={8} indent={2}>
              <span className={IDENT}>training</span>
              <span className={PUNCT}>{` {`}</span>{" "}
              <span className={IDENT}>corporate</span>{" "}
              <span className={IDENT}>workshop</span>{" "}
              <span className={PUNCT}>{`}`}</span>
            </L>
            <L n={9} indent={1}>
              <span className={PUNCT}>{`}`}</span>
            </L>
            <L n={10}>
              <span className={PUNCT}>{`}`}</span>
            </L>
          </CodePanel>
        </div>
      </div>
    </section>
  );
}

function AdvisorySection() {
  return (
    <SectionShell
      id="advisory"
      marker="// 01"
      eyebrow="// 01 Advisory"
      heading="Hourly consulting, scoped contracting."
      headingId="advisory-heading"
      code={
        <CodePanel filename="advisory.cs" language="csharp">
          <L n={1}>
            <span
              className={COMMENT}
            >{`// Bring a question, a design, or a deadline.`}</span>
          </L>
          <L n={2}>
            <span className={KEYWORD}>using</span>{" "}
            <span className={IDENT}>ChilliCream</span>
            <span className={PUNCT}>.</span>
            <span className={IDENT}>Advisory</span>
            <span className={PUNCT}>;</span>
          </L>
          <L n={3}>{` `}</L>
          <L n={4}>
            <span className={PUNCT}>[</span>
            <span className={IDENT}>Service</span>
            <span className={PUNCT}>(</span>
            <span className={STRING}>{`"advisory"`}</span>
            <span className={PUNCT}>)]</span>
          </L>
          <L n={5}>
            <span className={KEYWORD}>public sealed class</span>{" "}
            <span className={IDENT}>Advisory</span>
          </L>
          <L n={6}>
            <span className={PUNCT}>{`{`}</span>
          </L>
          <L n={7} indent={1}>
            <span className={KEYWORD}>public</span>{" "}
            <span className={IDENT}>Engagement</span>{" "}
            <span className={IDENT}>ArchitectureReview</span>
            <span className={PUNCT}>();</span>
          </L>
          <L n={8} indent={1}>
            <span className={KEYWORD}>public</span>{" "}
            <span className={IDENT}>Engagement</span>{" "}
            <span className={IDENT}>SchemaAudit</span>
            <span className={PUNCT}>();</span>
          </L>
          <L n={9} indent={1}>
            <span className={KEYWORD}>public</span>{" "}
            <span className={IDENT}>Engagement</span>{" "}
            <span className={IDENT}>ContractDelivery</span>
            <span className={PUNCT}>();</span>
          </L>
          <L n={10} indent={1}>
            <span
              className={COMMENT}
            >{`// Start with a single 60-minute call.`}</span>
          </L>
          <L n={11} indent={1}>
            <span className={KEYWORD}>public</span>{" "}
            <span className={IDENT}>Call</span>{" "}
            <span className={IDENT}>SixtyMinuteIntro</span>
            <span className={PUNCT}>();</span>
          </L>
          <L n={12}>
            <span className={PUNCT}>{`}`}</span>
          </L>
        </CodePanel>
      }
    >
      <p>
        Hourly consulting or scoped contracting from senior engineers on the
        core team. Direct access, no account-management layer in between.
      </p>
      <p className="text-cc-ink-dim">
        Architecture reviews, schema audits, troubleshooting, proof of concept,
        or full implementation. Start with a single 60-minute call and decide
        what shape the engagement should take.
      </p>
      <div className="pt-2">
        <SolidButton href="/services/advisory">Explore Advisory</SolidButton>
      </div>
    </SectionShell>
  );
}

function SupportSection() {
  return (
    <section
      id="support"
      aria-labelledby="support-heading"
      className="relative py-16 sm:py-20"
    >
      <span
        aria-hidden="true"
        className="text-cc-accent text-caption absolute top-16 left-0 font-mono tracking-[0.18em] uppercase sm:top-20"
      >
        {"// 02"}
      </span>
      <div className="border-cc-accent border-l-2 pl-6 sm:pl-8">
        <p className="text-cc-accent text-caption font-mono tracking-[0.18em] uppercase">
          Most teams start here
        </p>
      </div>
      <div className="mt-4 grid gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-4">
          <div className="lg:sticky lg:top-24">
            <p className="text-cc-accent text-caption font-mono tracking-[0.18em] uppercase">
              {"// 02 Support"}
            </p>
            <h2
              id="support-heading"
              className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
            >
              Tiered plans, defined response times.
            </h2>
            <div className="text-cc-ink text-lead mt-5 space-y-4 text-pretty">
              <p>
                A free Community plan, paid tiers from $450 per month, Business
                at $1,300, and Enterprise with phone support and a dedicated
                account manager.
              </p>
              <p className="text-cc-ink-dim">
                Same engineers who ship Hot Chocolate, Fusion, and Nitro. Each
                plan declares its own channels and response times so escalation
                is never a guessing game.
              </p>
            </div>
            <div className="mt-6 flex flex-wrap gap-3">
              <SolidButton href="/services/support">
                Compare Support
              </SolidButton>
            </div>
          </div>
        </div>
        <div className="lg:col-span-8">
          <CodePanel filename="support.yaml" language="yaml">
            <L n={1}>
              <span
                className={COMMENT}
              >{`# Tiered support plans with SLAs.`}</span>
            </L>
            <L n={2}>
              <span className={IDENT}>plans</span>
              <span className={PUNCT}>:</span>
            </L>
            <L n={3} indent={1}>
              <span className={IDENT}>community</span>
              <span className={PUNCT}>:</span>
            </L>
            <L n={4} indent={2}>
              <span className={IDENT}>price</span>
              <span className={PUNCT}>:</span>{" "}
              <span className={STRING}>{`"$0"`}</span>
            </L>
            <L n={5} indent={2}>
              <span className={IDENT}>channels</span>
              <span className={PUNCT}>:</span> <span className={PUNCT}>[</span>
              <span className={STRING}>{`"slack"`}</span>
              <span className={PUNCT}>,</span>{" "}
              <span className={STRING}>{`"github"`}</span>
              <span className={PUNCT}>]</span>
            </L>
            <L n={6} indent={1}>
              <span className={IDENT}>business</span>
              <span className={PUNCT}>:</span>{" "}
              <span className={COMMENT}>{`# most teams start here`}</span>
            </L>
            <L n={7} indent={2}>
              <span className={IDENT}>price</span>
              <span className={PUNCT}>:</span>{" "}
              <span className={STRING}>{`"$1,300 / month"`}</span>
            </L>
            <L n={8} indent={2}>
              <span className={IDENT}>floor</span>
              <span className={PUNCT}>:</span>{" "}
              <span className={STRING}>{`"$450 / month"`}</span>
            </L>
            <L n={9} indent={2}>
              <span className={IDENT}>channels</span>
              <span className={PUNCT}>:</span> <span className={PUNCT}>[</span>
              <span className={STRING}>{`"email"`}</span>
              <span className={PUNCT}>,</span>{" "}
              <span className={STRING}>{`"incident"`}</span>
              <span className={PUNCT}>]</span>
            </L>
            <L n={10} indent={2}>
              <span className={IDENT}>response_time</span>
              <span className={PUNCT}>:</span>{" "}
              <span className={STRING}>{`"defined per severity"`}</span>
            </L>
            <L n={11} indent={1}>
              <span className={IDENT}>enterprise</span>
              <span className={PUNCT}>:</span>
            </L>
            <L n={12} indent={2}>
              <span className={IDENT}>price</span>
              <span className={PUNCT}>:</span>{" "}
              <span className={STRING}>{`"custom"`}</span>
            </L>
            <L n={13} indent={2}>
              <span className={IDENT}>channels</span>
              <span className={PUNCT}>:</span> <span className={PUNCT}>[</span>
              <span className={STRING}>{`"phone"`}</span>
              <span className={PUNCT}>,</span>{" "}
              <span className={STRING}>{`"email"`}</span>
              <span className={PUNCT}>,</span>{" "}
              <span className={STRING}>{`"incident"`}</span>
              <span className={PUNCT}>]</span>
            </L>
            <L n={14} indent={2}>
              <span className={IDENT}>account_manager</span>
              <span className={PUNCT}>:</span>{" "}
              <span className={KEYWORD}>true</span>
            </L>
            <L n={15} indent={2}>
              <span className={IDENT}>response_time</span>
              <span className={PUNCT}>:</span>{" "}
              <span className={STRING}>{`"negotiated in contract"`}</span>
            </L>
          </CodePanel>
        </div>
      </div>
    </section>
  );
}

function TrainingSection() {
  return (
    <SectionShell
      id="training"
      marker="// 03"
      eyebrow="// 03 Training"
      heading="Lift the whole team at once."
      headingId="training-heading"
      code={
        <CodePanel filename="training.json" language="json">
          <L n={1}>
            <span className={PUNCT}>{`{`}</span>
          </L>
          <L n={2} indent={1}>
            <span className={STRING}>{`"corporate_training"`}</span>
            <span className={PUNCT}>:</span>{" "}
            <span className={PUNCT}>{`{`}</span>
          </L>
          <L n={3} indent={2}>
            <span className={STRING}>{`"audience"`}</span>
            <span className={PUNCT}>:</span>{" "}
            <span
              className={STRING}
            >{`"beginner" | "advanced" | "mixed"`}</span>
            <span className={PUNCT}>,</span>
          </L>
          <L n={4} indent={2}>
            <span className={STRING}>{`"pacing"`}</span>
            <span className={PUNCT}>:</span>{" "}
            <span
              className={STRING}
            >{`"tuned to where engineers are today"`}</span>
          </L>
          <L n={5} indent={1}>
            <span className={PUNCT}>{`},`}</span>
          </L>
          <L n={6} indent={1}>
            <span className={STRING}>{`"corporate_workshop"`}</span>
            <span className={PUNCT}>:</span>{" "}
            <span className={PUNCT}>{`{`}</span>
          </L>
          <L n={7} indent={2}>
            <span className={STRING}>{`"topics"`}</span>
            <span className={PUNCT}>:</span> <span className={PUNCT}>[</span>
            <span className={STRING}>{`"Hot Chocolate"`}</span>
            <span className={PUNCT}>,</span>{" "}
            <span className={STRING}>{`"ASP.NET Core"`}</span>
            <span className={PUNCT}>,</span>{" "}
            <span className={STRING}>{`"React"`}</span>
            <span className={PUNCT}>,</span>{" "}
            <span className={STRING}>{`"Relay"`}</span>
            <span className={PUNCT}>]</span>
            <span className={PUNCT}>,</span>
          </L>
          <L n={8} indent={2}>
            <span className={STRING}>{`"format"`}</span>
            <span className={PUNCT}>:</span>{" "}
            <span className={STRING}>{`"on-site" | "remote"`}</span>
            <span className={PUNCT}>,</span>
          </L>
          <L n={9} indent={2}>
            <span className={STRING}>{`"exercises"`}</span>
            <span className={PUNCT}>:</span>{" "}
            <span
              className={STRING}
            >{`"real-project, production quirks"`}</span>
          </L>
          <L n={10} indent={1}>
            <span className={PUNCT}>{`}`}</span>
          </L>
          <L n={11}>
            <span className={PUNCT}>{`}`}</span>
          </L>
        </CodePanel>
      }
    >
      <p>
        Corporate Training and hands-on Workshops, sized to your team and your
        stack. Beginner, advanced, or mixed groups, on-site or remote.
      </p>
      <p className="text-cc-ink-dim">
        Real-project exercises and the production quirks our engineers actually
        hit. Designed so the whole team levels up at the same time, instead of
        one senior reading docs in a corner.
      </p>
      <div className="pt-2">
        <SolidButton href="/services/training">Explore Training</SolidButton>
      </div>
    </SectionShell>
  );
}

interface DecisionRoute {
  readonly id: string;
  readonly caseName: string;
  readonly summary: string;
  readonly destinations: readonly {
    readonly label: string;
    readonly href: string;
  }[];
}

const DECISION_ROUTES: readonly DecisionRoute[] = [
  {
    id: "right-now",
    caseName: '"right-now"',
    summary: "Need help right now.",
    destinations: [
      { label: "Advisory consult", href: "/services/advisory" },
      { label: "Support plans", href: "/services/support" },
    ],
  },
  {
    id: "expert-delivery",
    caseName: '"expert-delivery"',
    summary: "Need expert delivery.",
    destinations: [
      { label: "Advisory: Contracting", href: "/services/advisory" },
    ],
  },
  {
    id: "team-trained",
    caseName: '"team-trained"',
    summary: "Need your team trained.",
    destinations: [{ label: "Training", href: "/services/training" }],
  },
];

function DecisionSection() {
  return (
    <SectionShell
      id="decide"
      marker="// 04"
      eyebrow="// 04 Decision tree"
      heading="Which one is right for me?"
      headingId="decision-heading"
      code={
        <div className="space-y-4">
          <CodePanel filename="decision-tree.ts" language="typescript">
            <L n={1}>
              <span
                className={COMMENT}
              >{`// Three common starting points.`}</span>
            </L>
            <L n={2}>
              <span className={KEYWORD}>function</span>{" "}
              <span className={IDENT}>route</span>
              <span className={PUNCT}>(</span>
              <span className={IDENT}>need</span>
              <span className={PUNCT}>:</span>{" "}
              <span className={IDENT}>Need</span>
              <span className={PUNCT}>) {`{`}</span>
            </L>
            <L n={3} indent={1}>
              <span className={KEYWORD}>switch</span>
              <span className={PUNCT}>(</span>
              <span className={IDENT}>need</span>
              <span className={PUNCT}>) {`{`}</span>
            </L>
            <L n={4} indent={2}>
              <span className={KEYWORD}>case</span>{" "}
              <span className={STRING}>{`"right-now"`}</span>
              <span className={PUNCT}>:</span>
            </L>
            <L n={5} indent={3}>
              <span className={KEYWORD}>return</span>{" "}
              <span className={PUNCT}>[</span>
              <span className={STRING}>{`"advisory"`}</span>
              <span className={PUNCT}>,</span>{" "}
              <span className={STRING}>{`"support"`}</span>
              <span className={PUNCT}>];</span>
            </L>
            <L n={6} indent={2}>
              <span className={KEYWORD}>case</span>{" "}
              <span className={STRING}>{`"expert-delivery"`}</span>
              <span className={PUNCT}>:</span>
            </L>
            <L n={7} indent={3}>
              <span className={KEYWORD}>return</span>{" "}
              <span className={PUNCT}>[</span>
              <span className={STRING}>{`"advisory.contracting"`}</span>
              <span className={PUNCT}>];</span>
            </L>
            <L n={8} indent={2}>
              <span className={KEYWORD}>case</span>{" "}
              <span className={STRING}>{`"team-trained"`}</span>
              <span className={PUNCT}>:</span>
            </L>
            <L n={9} indent={3}>
              <span className={KEYWORD}>return</span>{" "}
              <span className={PUNCT}>[</span>
              <span className={STRING}>{`"training"`}</span>
              <span className={PUNCT}>];</span>
            </L>
            <L n={10} indent={1}>
              <span className={PUNCT}>{`}`}</span>
            </L>
            <L n={11}>
              <span className={PUNCT}>{`}`}</span>
            </L>
          </CodePanel>
          <DecisionRoutes />
        </div>
      }
    >
      <p>
        Pick the case that sounds like you and follow the chips, or book a call
        and we will sort it out together.
      </p>
      <p className="text-cc-ink-dim">
        Three starting points cover almost every conversation we have: an
        immediate fire, a scoped delivery with our engineers, or a team that
        needs to come up to speed.
      </p>
    </SectionShell>
  );
}

function DecisionRoutes() {
  return (
    <ul className="space-y-3">
      {DECISION_ROUTES.map((route) => (
        <li
          key={route.id}
          className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-5"
        >
          <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
            <span className="text-cc-accent text-caption font-mono">
              case {route.caseName}:
            </span>
            <span className="text-cc-ink text-body">{route.summary}</span>
          </div>
          <div className="mt-4 flex flex-wrap gap-2">
            {route.destinations.map((destination) => (
              <OutlineButton key={destination.href} href={destination.href}>
                {destination.label}
              </OutlineButton>
            ))}
          </div>
        </li>
      ))}
    </ul>
  );
}

function EnterpriseSection() {
  return (
    <SectionShell
      id="enterprise"
      marker="// 05"
      eyebrow="// 05 Enterprise"
      heading="One contract, every team, an SLA you wrote together."
      headingId="enterprise-heading"
      code={
        <CodePanel filename="enterprise.contract.md" language="markdown">
          <L n={1}>
            <span className={PUNCT}>---</span>
          </L>
          <L n={2}>
            <span className={IDENT}>coverage</span>
            <span className={PUNCT}>:</span>{" "}
            <span className={STRING}>
              Phone support and unlimited critical incidents
            </span>
          </L>
          <L n={3}>
            <span className={IDENT}>account</span>
            <span className={PUNCT}>:</span>{" "}
            <span className={STRING}>
              Dedicated account manager and status reviews
            </span>
          </L>
          <L n={4}>
            <span className={IDENT}>delivery</span>
            <span className={PUNCT}>:</span>{" "}
            <span className={STRING}>
              Embedded engineers across teams and units
            </span>
          </L>
          <L n={5}>
            <span className={IDENT}>contract</span>
            <span className={PUNCT}>:</span>{" "}
            <span className={STRING}>
              Custom SLAs and procurement-ready paperwork
            </span>
          </L>
          <L n={6}>
            <span className={PUNCT}>---</span>
          </L>
          <L n={7}>{` `}</L>
          <L n={8}>
            <span className={KEYWORD}># Enterprise services</span>
          </L>
          <L n={9}>{` `}</L>
          <L n={10}>
            <span className={STRING}>
              For organizations standardizing on Hot Chocolate, Fusion,
            </span>
          </L>
          <L n={11}>
            <span className={STRING}>and Nitro across business units.</span>
          </L>
          <L n={12}>{` `}</L>
          <L n={13}>
            <span className={KEYWORD}>- </span>
            <span className={STRING}>
              Advisory hours bundled into one agreement
            </span>
          </L>
          <L n={14}>
            <span className={KEYWORD}>- </span>
            <span className={STRING}>
              Enterprise Support plan with phone access
            </span>
          </L>
          <L n={15}>
            <span className={KEYWORD}>- </span>
            <span className={STRING}>
              On-site training scheduled across teams
            </span>
          </L>
          <L n={16}>
            <span className={KEYWORD}>- </span>
            <span className={STRING}>Procurement-ready paperwork</span>
          </L>
        </CodePanel>
      }
    >
      <p>
        For organizations standardizing on Hot Chocolate, Fusion, and Nitro
        across business units. Bundle Advisory hours, an Enterprise Support
        plan, and on-site training into one agreement.
      </p>
      <p className="text-cc-ink-dim">
        Coverage, account, delivery, and contract all land in one document, in
        the shape procurement actually buys.
      </p>
      <div className="flex flex-wrap gap-3 pt-2">
        <SolidButton href={ENTERPRISE_MAILTO}>Talk to sales</SolidButton>
        <OutlineButton href={SUPPORT_CONTACT}>
          Enterprise Support details
        </OutlineButton>
      </div>
    </SectionShell>
  );
}

function ClosingSection() {
  return (
    <SectionShell
      id="closing"
      marker="// 06"
      eyebrow="// 06 Book the call"
      heading="One call is usually enough to know."
      headingId="closing-heading"
      code={
        <div className="space-y-4">
          <CodePanel filename="book-call.sh" language="zsh">
            <L n={1}>
              <span
                className={COMMENT}
              >{`# 60 minutes, an engineer, a clear next step.`}</span>
            </L>
            <L n={2}>
              <span className={KEYWORD}>$</span>{" "}
              <span className={IDENT}>curl</span>{" "}
              <span className={STRING}>-X POST</span>{" "}
              <span className={STRING}>{BOOKING_URL}</span>
            </L>
            <L n={3}>{` `}</L>
            <L n={4}>
              <span className={COMMENT}>HTTP/1.1 200 OK</span>
            </L>
            <L n={5}>
              <span className={COMMENT}>content-type: application/json</span>
            </L>
            <L n={6}>{` `}</L>
            <L n={7}>
              <span className={PUNCT}>{`{`}</span>
            </L>
            <L n={8} indent={1}>
              <span className={STRING}>{`"outcomes"`}</span>
              <span className={PUNCT}>:</span> <span className={PUNCT}>[</span>
            </L>
            <L n={9} indent={2}>
              <span
                className={STRING}
              >{`"Advisory: hourly or scoped contracting"`}</span>
              <span className={PUNCT}>,</span>
            </L>
            <L n={10} indent={2}>
              <span
                className={STRING}
              >{`"Support: a plan from $450 / month"`}</span>
              <span className={PUNCT}>,</span>
            </L>
            <L n={11} indent={2}>
              <span
                className={STRING}
              >{`"Training: tailored to your team"`}</span>
              <span className={PUNCT}>,</span>
            </L>
            <L n={12} indent={2}>
              <span
                className={STRING}
              >{`"A candid no when we are not the right fit"`}</span>
            </L>
            <L n={13} indent={1}>
              <span className={PUNCT}>]</span>
            </L>
            <L n={14}>
              <span className={PUNCT}>{`}`}</span>
            </L>
            <L n={15}>{` `}</L>
            <L n={16}>
              <span className={KEYWORD}>$</span>{" "}
              <span className={IDENT}>_</span>
            </L>
          </CodePanel>
          <div className="flex flex-wrap items-center gap-3">
            <span className="text-cc-accent text-caption font-mono">$</span>
            <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
            <OutlineButton href={CONTACT_MAILTO}>Email us</OutlineButton>
          </div>
        </div>
      }
    >
      <p>
        Book a 60-minute call with an engineer. Walk us through the project, and
        you will leave with a clear next step.
      </p>
      <p className="text-cc-ink-dim">
        Advisory, a Support plan, Training, or a candid no when we are not the
        right fit. Either way, you stop guessing.
      </p>
    </SectionShell>
  );
}
