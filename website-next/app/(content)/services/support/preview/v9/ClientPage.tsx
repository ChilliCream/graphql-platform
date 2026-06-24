"use client";

import { animate, motion, useInView, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";
import { useEffect, useRef } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// The one accent for this page: ChilliCream brand cyan. Used only where a rule
// needs the exact brand hue. Everything else stays on cc-* tokens.
const CYAN = "#16b9e4";

type PlanName = "Community" | "Startup" | "Business" | "Enterprise";

interface Plan {
  readonly name: PlanName;
  readonly price: string;
  readonly priceNote?: string;
  readonly tagline: string;
  readonly description: string;
  readonly perks: readonly string[];
  readonly cta: { readonly label: string; readonly href: string };
  readonly highlight?: boolean;
}

const PLANS: readonly Plan[] = [
  {
    name: "Community",
    price: "Free",
    tagline: "For hackers and side projects",
    description: "For personal or non-commercial projects, to start hacking.",
    perks: ["Public Slack Channel"],
    cta: { label: "Join Slack", href: "https://slack.chillicream.com/" },
  },
  {
    name: "Startup",
    price: "$450",
    priceNote: "per month",
    tagline: "Small teams, steady cadence",
    description:
      "For small teams with moderate bandwidth and projects of low to medium complexity.",
    perks: ["Private Slack Channel", "2 critical incidents"],
    cta: {
      label: "Contact Us",
      href: "/services/support/contact?plan=Startup",
    },
  },
  {
    name: "Business",
    price: "$1,300",
    priceNote: "per month",
    tagline: "Most popular",
    description: "For larger teams with business-critical projects.",
    perks: [
      "Private Slack Channel",
      "5 critical incidents",
      "2 non-critical incidents",
      "Email support",
    ],
    cta: {
      label: "Contact Us",
      href: "/services/support/contact?plan=Business",
    },
    highlight: true,
  },
  {
    name: "Enterprise",
    price: "Custom",
    tagline: "Whole-org coverage with SLAs",
    description:
      "For the whole organization, all your teams and business units, and with tailor made SLAs.",
    perks: [
      "Private Slack Channel",
      "Unlimited critical incidents",
      "10 non-critical incidents",
      "Phone support",
      "Dedicated account manager",
      "Status reviews",
    ],
    cta: {
      label: "Contact Us",
      href: "/services/support/contact?plan=Enterprise",
    },
  },
];

type CellValue = boolean | string;

interface ComparisonRow {
  readonly title: string;
  readonly hint?: string;
  readonly values: readonly [CellValue, CellValue, CellValue, CellValue];
}

interface ComparisonGroup {
  readonly title: string;
  readonly rows: readonly ComparisonRow[];
}

const PLAN_NAMES: readonly PlanName[] = [
  "Community",
  "Startup",
  "Business",
  "Enterprise",
];

const COMPARISON: readonly ComparisonGroup[] = [
  {
    title: "Response & incidents",
    rows: [
      {
        title: "Critical Incidents",
        hint: "Production is impacted: down, data loss, or hard outage.",
        values: [
          false,
          "2 (next business day)",
          "5 (next business day)",
          "Unlimited (24 hours)",
        ],
      },
      {
        title: "Non-critical Incidents",
        hint: "Bugs and questions that block work but not production.",
        values: [false, false, "5 (3 business days)", "10 (next business day)"],
      },
    ],
  },
  {
    title: "Channels",
    rows: [
      {
        title: "Public Slack Channel",
        values: [true, true, true, true],
      },
      {
        title: "Private Slack Channel",
        values: [false, true, true, true],
      },
      {
        title: "Private Issue Tracking Board",
        values: [false, false, true, true],
      },
      {
        title: "Email Support",
        values: [false, false, true, true],
      },
      {
        title: "Phone Support",
        values: [false, false, false, true],
      },
    ],
  },
  {
    title: "Strategic",
    rows: [
      {
        title: "Dedicated Account Manager",
        values: [false, false, false, true],
      },
      {
        title: "Status Reviews",
        hint: "Recurring check-ins on roadmap, upgrades, and posture.",
        values: [false, false, false, true],
      },
    ],
  },
];

interface FaqItem {
  readonly q: string;
  readonly a: string;
}

const FAQ: readonly FaqItem[] = [
  {
    q: "What counts as a critical incident?",
    a: "An incident is critical when a production system you run on Hot Chocolate, Fusion, or Nitro is down, returning wrong data, or otherwise hard-blocked. Anything that degrades a live user experience qualifies. Local dev issues and questions are non-critical.",
  },
  {
    q: "How fast do you respond?",
    a: "Startup and Business respond to critical incidents the next business day. Enterprise responds to critical incidents within 24 hours, any day. Non-critical incidents are 3 business days on Business and next business day on Enterprise. The Community plan is best-effort in public Slack with no guarantee.",
  },
  {
    q: "How is an incident opened and tracked?",
    a: "Paid plans get a private Slack channel staffed by ChilliCream engineers. Business and Enterprise additionally get a private issue tracking board so every incident has a ticket, an owner, and a written history you can audit.",
  },
  {
    q: "What happens when I use up my incidents in a month?",
    a: "We do not cut you off mid-fire. We keep working the incident and reach out to discuss either a one-time top-up or moving to the next plan. Incidents do not roll over month to month.",
  },
  {
    q: "Do you support self-hosted Nitro and on-prem deployments?",
    a: "Yes. Business and Enterprise both support self-hosted Nitro, Fusion gateways, and Hot Chocolate services running in your own cloud or on-prem. Enterprise adds tailored SLAs and a named account manager who knows your topology.",
  },
  {
    q: "Can we change plans later?",
    a: "Yes. You can upgrade at any time and the new SLA takes effect immediately. Downgrades take effect at the start of the next billing month so an in-flight incident never falls between plans.",
  },
];

// Response targets per tier, derived verbatim from the comparison matrix above.
interface ResponseColumn {
  readonly name: PlanName;
  readonly critical: string;
  readonly nonCritical: string;
  readonly highlight?: boolean;
}

const RESPONSE_COLUMNS: readonly ResponseColumn[] = [
  { name: "Community", critical: "Best-effort", nonCritical: "Best-effort" },
  {
    name: "Startup",
    critical: "Next business day",
    nonCritical: "Not included",
  },
  {
    name: "Business",
    critical: "Next business day",
    nonCritical: "3 business days",
    highlight: true,
  },
  {
    name: "Enterprise",
    critical: "24 hours, any day",
    nonCritical: "Next business day",
  },
];

interface EnterpriseFactData {
  readonly title: string;
  readonly detail: string;
}

const ENTERPRISE_FACTS: readonly EnterpriseFactData[] = [
  {
    title: "24-hour critical SLA",
    detail: "Any day. Unlimited critical incidents under one ceiling.",
  },
  {
    title: "Dedicated account manager",
    detail: "One named contact who knows your stack and your roadmap.",
  },
  {
    title: "Phone support",
    detail: "For the calls that should not wait for a ticket.",
  },
  {
    title: "Status reviews",
    detail: "Recurring check-ins on roadmap, upgrades, and posture.",
  },
];

export function ClientPage() {
  return (
    <div className="relative">
      <LedgerPaper />
      <div className="mx-auto max-w-6xl px-6">
        <IntroSection />
        <PlansSection />
        <ResponseSection />
        <ComparisonSection />
        <FaqSection />
        <EnterpriseSection />
        <CloseSection />
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/* Background: faint evenly-spaced ledger hairlines.                          */
/* -------------------------------------------------------------------------- */

function LedgerPaper() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute inset-0 -z-10 overflow-hidden"
      style={{
        maskImage:
          "linear-gradient(to bottom, transparent, black 12%, black 88%, transparent)",
        WebkitMaskImage:
          "linear-gradient(to bottom, transparent, black 12%, black 88%, transparent)",
      }}
    >
      <svg
        className="h-full w-full"
        preserveAspectRatio="none"
        aria-hidden
        focusable="false"
      >
        <defs>
          <pattern
            id="cc-support-v9-ledger"
            width="100%"
            height="96"
            patternUnits="userSpaceOnUse"
          >
            <line
              x1="0"
              y1="0.5"
              x2="100%"
              y2="0.5"
              stroke="var(--color-cc-card-border)"
              strokeWidth="1"
              opacity="0.18"
            />
          </pattern>
        </defs>
        <rect width="100%" height="100%" fill="url(#cc-support-v9-ledger)" />
      </svg>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/* Ledger primitives: the cyan section rule, eyebrow, and ruled cells.        */
/* -------------------------------------------------------------------------- */

interface SectionRuleProps {
  readonly index?: string;
}

// A 1px cyan rule that draws itself from scaleX(0) to scaleX(1) once on view.
function SectionRule({ index }: SectionRuleProps) {
  const reduce = useReducedMotion();

  return (
    <div className="flex items-end justify-between gap-4">
      <motion.span
        className="block h-px flex-1 origin-left"
        style={{ backgroundColor: CYAN }}
        initial={reduce ? false : { scaleX: 0 }}
        whileInView={reduce ? undefined : { scaleX: 1 }}
        viewport={{ once: true, margin: "-10%" }}
        transition={{ duration: 0.6, ease: "easeOut" }}
      />
      {index && (
        <span className="text-cc-nav-label shrink-0 font-mono text-xs font-semibold tracking-widest tabular-nums">
          {index}
        </span>
      )}
    </div>
  );
}

interface EyebrowRowProps {
  readonly label: string;
  readonly meta?: string;
}

// The "ledger entry" line: mono uppercase label left, small meta right. Fades
// and rises shortly after its rule.
function EyebrowRow({ label, meta }: EyebrowRowProps) {
  const reduce = useReducedMotion();

  return (
    <motion.div
      className="mt-4 flex items-baseline justify-between gap-4"
      initial={reduce ? false : { opacity: 0, y: 4 }}
      whileInView={reduce ? undefined : { opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-10%" }}
      transition={{ duration: 0.4, ease: "easeOut", delay: 0.08 }}
    >
      <span
        className="font-mono text-xs font-semibold tracking-widest uppercase"
        style={{ color: CYAN }}
      >
        {label}
      </span>
      {meta && (
        <span className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
          {meta}
        </span>
      )}
    </motion.div>
  );
}

interface SectionHeadProps {
  readonly heading: string;
  readonly lead: string;
}

// Layer 3: heading left, lead paragraph right on lg.
function SectionHead({ heading, lead }: SectionHeadProps) {
  return (
    <div className="mt-8 grid gap-6 lg:grid-cols-[1.1fr_1fr] lg:items-start lg:gap-12">
      <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
        {heading}
      </h2>
      <p className="text-cc-prose lead text-base leading-relaxed sm:text-lg">
        {lead}
      </p>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/* 01 INTRO                                                                   */
/* -------------------------------------------------------------------------- */

function IntroSection() {
  const reduce = useReducedMotion();

  return (
    <section className="py-20 sm:py-24">
      <SectionRule index="01 / 06" />
      <motion.div
        className="mt-4"
        initial={reduce ? false : { opacity: 0, y: 4 }}
        whileInView={reduce ? undefined : { opacity: 1, y: 0 }}
        viewport={{ once: true, margin: "-10%" }}
        transition={{ duration: 0.4, ease: "easeOut", delay: 0.08 }}
      >
        <span
          className="font-mono text-xs font-semibold tracking-widest uppercase"
          style={{ color: CYAN }}
        >
          Support / GraphQL Plans
        </span>
      </motion.div>

      <div className="mt-8 grid gap-6 lg:grid-cols-[1.1fr_1fr] lg:items-start lg:gap-12">
        <h1 className="font-heading text-cc-heading text-4xl leading-tight font-semibold tracking-tight sm:text-5xl lg:text-6xl">
          GraphQL support plans on a schedule you can plan around.
        </h1>
        <div>
          <p className="text-cc-prose lead text-base leading-relaxed sm:text-lg">
            Pick the tier that matches how your team ships Hot Chocolate,
            Fusion, and Nitro. Clear response times, named channels, and
            engineers who wrote the code.
          </p>
          <div className="mt-7 flex flex-col gap-3 sm:flex-row sm:gap-4">
            <SolidButton href="#plans">Compare plans</SolidButton>
            <OutlineButton href="/services/support/contact">
              Talk to us
            </OutlineButton>
          </div>
        </div>
      </div>

      <ul className="border-cc-card-border mt-12 grid gap-x-8 gap-y-3 border-t pt-6 sm:grid-cols-3">
        <IntroFact>Engineers, not a queue</IntroFact>
        <IntroFact>Private Slack on every paid plan</IntroFact>
        <IntroFact>Written SLAs from Business up</IntroFact>
      </ul>
    </section>
  );
}

function IntroFact({ children }: { readonly children: ReactNode }) {
  return (
    <li className="text-cc-ink flex items-center gap-3 text-sm">
      <span
        className="inline-flex shrink-0"
        style={{ color: CYAN }}
        aria-hidden
      >
        <CheckIcon />
      </span>
      <span>{children}</span>
    </li>
  );
}

/* -------------------------------------------------------------------------- */
/* 02 PLANS                                                                   */
/* -------------------------------------------------------------------------- */

function PlansSection() {
  return (
    <section id="plans" className="py-20 sm:py-24">
      <SectionRule index="02 / 06" />
      <EyebrowRow label="The Ledger / Four Tiers" meta="4 plans" />
      <SectionHead
        heading="Pick the tier that matches how you ship."
        lead="Every paid plan is staffed by ChilliCream engineers, with flat monthly pricing. No card chrome, just one line per plan."
      />

      <div className="mt-12">
        {PLANS.map((plan) => (
          <PlanRow key={plan.name} plan={plan} />
        ))}
      </div>

      <p className="text-cc-ink-dim mt-6 font-mono text-xs tracking-wide">
        Prices in USD. Excludes applicable taxes.
      </p>
    </section>
  );
}

function PlanRow({ plan }: { readonly plan: Plan }) {
  // The highlighted Business row swaps the 1px top rule for a 2px cyan rule and
  // adds a faint cyan tint, flagging the popular plan with no card outline.
  const reduce = useReducedMotion();
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-10%" });

  return (
    <div
      ref={ref}
      className={`relative ${plan.highlight ? "bg-[#16b9e4]/[0.03]" : ""}`}
    >
      {plan.highlight ? (
        <HighlightRule active={inView && !reduce} />
      ) : (
        <div
          className="h-px w-full"
          style={{ backgroundColor: CYAN, opacity: 0.85 }}
        />
      )}

      <div className="grid grid-cols-1 gap-x-6 gap-y-5 px-1 py-7 lg:grid-cols-12 lg:items-center lg:py-8">
        {/* name + tagline */}
        <div className="lg:col-span-3">
          <h3 className="font-heading text-cc-heading text-2xl font-semibold tracking-tight">
            {plan.name}
          </h3>
          <p className="text-cc-ink-dim mt-1 text-sm">{plan.tagline}</p>
        </div>

        {/* price */}
        <div className="flex items-baseline gap-2 lg:col-span-2 lg:flex-col lg:items-start lg:gap-0">
          <span className="font-heading text-cc-heading text-3xl font-semibold tracking-tight">
            {plan.price}
          </span>
          {plan.priceNote && (
            <span className="text-cc-ink-dim font-mono text-xs tracking-wide">
              {plan.priceNote}
            </span>
          )}
        </div>

        {/* perks as inline check pills */}
        <ul className="flex flex-wrap gap-x-4 gap-y-2 lg:col-span-5">
          {plan.perks.map((perk) => (
            <li
              key={perk}
              className="text-cc-prose inline-flex items-center gap-2 text-sm"
            >
              <span
                className="inline-flex shrink-0"
                style={{ color: CYAN }}
                aria-hidden
              >
                <CheckIcon size={13} />
              </span>
              <span>{perk}</span>
            </li>
          ))}
        </ul>

        {/* CTA */}
        <div className="lg:col-span-2 lg:justify-self-end">
          {plan.highlight ? (
            <SolidButton href={plan.cta.href} className="w-full lg:w-auto">
              {plan.cta.label}
            </SolidButton>
          ) : (
            <OutlineButton href={plan.cta.href} className="w-full lg:w-auto">
              {plan.cta.label}
            </OutlineButton>
          )}
        </div>
      </div>

      {plan.highlight && (
        <div
          className="h-0.5 w-full"
          style={{ backgroundColor: CYAN }}
          aria-hidden
        />
      )}
    </div>
  );
}

// The Business tier's 2px top rule with a one-shot color sweep on first view.
function HighlightRule({ active }: { readonly active: boolean }) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const node = ref.current;
    if (!node || !active) {
      return;
    }
    const controls = animate(
      node,
      { backgroundColor: [CYAN, "#7bdcf5", CYAN] },
      { duration: 1.2, ease: "easeInOut" },
    );
    return () => controls.stop();
  }, [active]);

  return (
    <div
      ref={ref}
      className="h-0.5 w-full"
      style={{ backgroundColor: CYAN }}
      aria-hidden
    />
  );
}

/* -------------------------------------------------------------------------- */
/* 03 RESPONSE TIMES                                                          */
/* -------------------------------------------------------------------------- */

function ResponseSection() {
  return (
    <section className="py-20 sm:py-24">
      <SectionRule index="03 / 06" />
      <EyebrowRow label="SLA / Response" meta="per tier" />
      <SectionHead
        heading="Response times, written down."
        lead="What each tier commits to when an incident opens, side by side. Critical means production is impacted; non-critical blocks work but not production."
      />

      <div className="mt-12 grid grid-cols-2 lg:grid-cols-4">
        {RESPONSE_COLUMNS.map((column, index) => (
          <ResponseColumnCell
            key={column.name}
            column={column}
            withLeftRule={index > 0}
          />
        ))}
      </div>
    </section>
  );
}

function ResponseColumnCell({
  column,
  withLeftRule,
}: {
  readonly column: ResponseColumn;
  readonly withLeftRule: boolean;
}) {
  return (
    <div
      className={`px-5 py-5 ${
        withLeftRule
          ? column.highlight
            ? "border-l-2 border-l-[#16b9e4]"
            : "border-l-cc-card-border border-l"
          : ""
      } ${column.highlight ? "bg-[#16b9e4]/[0.03]" : ""}`}
    >
      <div
        className={`font-mono text-xs font-semibold tracking-widest uppercase ${
          column.highlight ? "text-[#16b9e4]" : "text-cc-nav-label"
        }`}
      >
        {column.name}
      </div>
      <dl className="mt-4 space-y-4">
        <div>
          <dt className="text-cc-ink-dim text-xs">Critical</dt>
          <dd className="text-cc-heading mt-1 text-sm font-medium">
            {column.critical}
          </dd>
        </div>
        <div>
          <dt className="text-cc-ink-dim text-xs">Non-critical</dt>
          <dd className="text-cc-heading mt-1 text-sm font-medium">
            {column.nonCritical}
          </dd>
        </div>
      </dl>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/* 04 COMPARISON                                                              */
/* -------------------------------------------------------------------------- */

function ComparisonSection() {
  return (
    <section className="py-20 sm:py-24">
      <SectionRule index="04 / 06" />
      <EyebrowRow label="Matrix / Every Line" meta="feature by feature" />
      <SectionHead
        heading="Compare every plan, line by line."
        lead="Response times, channels, and the strategic perks Enterprise teams ask for. Only rules separate the rows."
      />

      <div className="mt-12 overflow-x-auto">
        <table className="w-full border-collapse text-sm">
          <caption className="sr-only">
            Support plan comparison by feature
          </caption>
          <thead>
            <tr>
              <th
                scope="col"
                className="text-cc-nav-label w-[34%] py-4 pr-5 text-left align-bottom font-mono text-xs font-semibold tracking-widest uppercase"
              >
                Feature
              </th>
              {PLAN_NAMES.map((name) => {
                const isHighlight = name === "Business";
                return (
                  <th
                    key={name}
                    scope="col"
                    className={`px-4 py-4 text-center align-bottom font-mono text-xs font-semibold tracking-widest uppercase ${
                      isHighlight
                        ? "border-x-2 border-x-[#16b9e4] text-[#16b9e4]"
                        : "text-cc-heading"
                    }`}
                  >
                    {name}
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {COMPARISON.map((group) => (
              <ComparisonGroupRows key={group.title} group={group} />
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function ComparisonGroupRows({ group }: { readonly group: ComparisonGroup }) {
  return (
    <>
      <tr>
        <th
          scope="colgroup"
          colSpan={1}
          className="text-cc-nav-label border-cc-card-border border-y py-2 pr-5 text-left font-mono text-[11px] font-semibold tracking-widest uppercase"
        >
          {group.title}
        </th>
        <td
          colSpan={4}
          className="border-cc-card-border border-y"
          aria-hidden
        />
      </tr>
      {group.rows.map((row) => (
        <tr key={row.title} className="border-cc-card-border border-b">
          <th scope="row" className="py-4 pr-5 text-left align-top">
            <div className="text-cc-heading font-medium">{row.title}</div>
            {row.hint && (
              <div className="text-cc-ink-dim mt-1 text-xs leading-relaxed">
                {row.hint}
              </div>
            )}
          </th>
          {row.values.map((value, index) => {
            const planName = PLAN_NAMES[index];
            const isHighlight = planName === "Business";
            return (
              <td
                key={planName}
                className={`px-4 py-4 text-center align-top ${
                  isHighlight
                    ? "border-x-2 border-x-[#16b9e4] bg-[#16b9e4]/[0.03]"
                    : ""
                }`}
              >
                <ComparisonValue value={value} />
              </td>
            );
          })}
        </tr>
      ))}
    </>
  );
}

function ComparisonValue({ value }: { readonly value: CellValue }) {
  if (value === true) {
    return (
      <span
        className="inline-flex items-center justify-center"
        style={{ color: CYAN }}
        aria-label="Included"
      >
        <CheckIcon size={16} />
      </span>
    );
  }
  if (value === false) {
    return (
      <span
        className="text-cc-ink-faint inline-flex items-center justify-center"
        aria-label="Not included"
      >
        <svg viewBox="0 0 16 16" width={12} height={2} aria-hidden>
          <line
            x1="2"
            y1="1"
            x2="14"
            y2="1"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeLinecap="round"
          />
        </svg>
      </span>
    );
  }
  return <span className="text-cc-prose">{value}</span>;
}

/* -------------------------------------------------------------------------- */
/* 05 FAQ                                                                     */
/* -------------------------------------------------------------------------- */

function FaqSection() {
  return (
    <section className="py-20 sm:py-24">
      <SectionRule index="05 / 06" />
      <EyebrowRow label="Questions / Before You Sign" meta="6 answers" />
      <SectionHead
        heading="The questions buyers ask, answered straight."
        lead="What an incident is, how fast we respond, and how plans change. No surrounding container, one ruled line per answer."
      />

      <div className="mt-12">
        {FAQ.map((item, index) => (
          <details
            key={item.q}
            className="group border-cc-card-border border-t py-5"
            name="support-faq-v9"
          >
            <summary className="flex cursor-pointer list-none items-start justify-between gap-6">
              <span className="flex items-start gap-4">
                <span className="text-cc-nav-label mt-1 font-mono text-xs tracking-widest tabular-nums">
                  {String(index + 1).padStart(2, "0")}
                </span>
                <span className="text-cc-heading text-base font-medium sm:text-lg">
                  {item.q}
                </span>
              </span>
              <span
                className="mt-1 inline-flex shrink-0 transition-transform group-open:rotate-45"
                style={{ color: CYAN }}
                aria-hidden
              >
                <PlusGlyph />
              </span>
            </summary>
            <p className="text-cc-prose mt-3 pr-10 pl-9 text-sm leading-relaxed sm:text-base">
              {item.a}
            </p>
          </details>
        ))}
        <div className="border-cc-card-border border-t" />
      </div>
    </section>
  );
}

function PlusGlyph() {
  return (
    <svg viewBox="0 0 16 16" width={16} height={16} aria-hidden>
      <path
        d="M8 3v10M3 8h10"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

/* -------------------------------------------------------------------------- */
/* 06 ENTERPRISE                                                              */
/* -------------------------------------------------------------------------- */

function EnterpriseSection() {
  const reduce = useReducedMotion();

  return (
    <section className="relative py-20 sm:py-24">
      <EnterpriseWash />

      {/* The single brand-spectrum gradient rule on the whole page. */}
      <div className="flex items-end justify-between gap-4">
        <motion.span
          className="block h-px flex-1 origin-left"
          style={{
            backgroundImage:
              "linear-gradient(to right, #16b9e4, #7c92c6, #f0786a)",
          }}
          initial={reduce ? false : { scaleX: 0 }}
          whileInView={reduce ? undefined : { scaleX: 1 }}
          viewport={{ once: true, margin: "-10%" }}
          transition={{ duration: 0.6, ease: "easeOut" }}
        />
        <span className="text-cc-nav-label shrink-0 font-mono text-xs font-semibold tracking-widest tabular-nums">
          06 / 06
        </span>
      </div>
      <EyebrowRow label="Enterprise / Whole-Org" meta="tailored SLAs" />

      <div className="relative mt-8 grid gap-10 lg:grid-cols-[1.1fr_1fr] lg:gap-12">
        <div>
          <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
            Whole-org coverage, tailored SLAs, one named contact.
          </h2>
          <p className="text-cc-prose mt-4 max-w-xl text-base leading-relaxed sm:text-lg">
            For organizations running ChilliCream across multiple teams and
            business units. We shape the SLA around how you ship, and a
            dedicated account manager owns the relationship end to end.
          </p>
          <div className="mt-7 flex flex-col gap-3 sm:flex-row sm:gap-4">
            <SolidButton href="/services/support/contact?plan=Enterprise">
              Contact Us
            </SolidButton>
            <OutlineButton href="/services/advisory">
              Pair with Advisory
            </OutlineButton>
          </div>
        </div>

        <ul>
          {ENTERPRISE_FACTS.map((fact, index) => (
            <li
              key={fact.title}
              className={`flex items-start gap-4 py-4 ${
                index === 0
                  ? "border-cc-card-border border-y"
                  : "border-cc-card-border border-b"
              }`}
            >
              <span
                className="mt-1 inline-flex shrink-0"
                style={{ color: CYAN }}
                aria-hidden
              >
                <CheckIcon size={16} />
              </span>
              <div>
                <div className="text-cc-heading font-medium">{fact.title}</div>
                <p className="text-cc-ink-dim mt-1 text-sm leading-relaxed">
                  {fact.detail}
                </p>
              </div>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

function EnterpriseWash() {
  return (
    <div
      aria-hidden
      className="pointer-events-none absolute inset-0 -z-10"
      style={{
        backgroundImage:
          "radial-gradient(60% 60% at 80% 10%, rgba(22, 185, 228, 0.10), transparent 70%)",
      }}
    />
  );
}

/* -------------------------------------------------------------------------- */
/* 07 CLOSE                                                                   */
/* -------------------------------------------------------------------------- */

function CloseSection() {
  return (
    <section className="py-20 sm:py-24">
      <SectionRule index="07 / 06" />
      <EyebrowRow label="Next / Talk To Us" />

      <div className="mt-8 grid gap-6 lg:grid-cols-[1.1fr_1fr] lg:items-center lg:gap-12">
        <h2 className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl">
          Ready when you are.
        </h2>
        <div>
          <p className="text-cc-prose text-base leading-relaxed sm:text-lg">
            Join the community Slack to talk to other ChilliCream users, or get
            in touch to size a paid plan for your team.
          </p>
          <div className="mt-7 flex flex-col gap-3 sm:flex-row sm:gap-4">
            <SolidButton href="/services/support/contact">
              Contact sales
            </SolidButton>
            <OutlineButton href="https://slack.chillicream.com/">
              Join community Slack
            </OutlineButton>
          </div>
        </div>
      </div>

      {/* The trailing rule that closes the ledger. */}
      <div className="mt-12">
        <SectionRule />
      </div>
    </section>
  );
}
