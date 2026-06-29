"use client";

import { MotionConfig, motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import type {
  Cell,
  ComparisonGroup,
  PricingFaq,
  Tier,
  TierId,
  Unlock,
} from "@/src/components/pricing/pricingData";
import {
  COMPARISON,
  FAQ,
  TIERS,
  UNLOCKS,
  UNLOCKS_NOTE,
} from "@/src/components/pricing/pricingData";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// Concept: Tier Cascade. As each section enters view, the cloud tiers cascade
// in left to right, then their inner content (feature checks, comparison rows,
// unlock steps, FAQ cards) staggers in once and stays. No scroll coupling:
// every reveal uses whileInView with viewport={{ once: true }}. Honors
// prefers-reduced-motion via useReducedMotion to collapse animations to
// opacity-only with zero delay.

// The three shared-cloud tiers (Free, Pay as you go, Dedicated) lead the page;
// Self-Hosted follows as a slim strip. All pricing data is read from the shared
// pricing module so this preview never drifts from the real model.
const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_HOSTED = TIERS.find((tier) => tier.id === "self");

const EASE_OUT_QUART: readonly [number, number, number, number] = [
  0.22, 1, 0.36, 1,
];

const VIEWPORT_ONCE = { once: true, margin: "-10% 0px" } as const;

// Cloud-tier card cascade timing: left to right at 0, 0.08, 0.16s.
const CASCADE_DELAYS: Partial<Record<TierId, number>> = {
  free: 0,
  payg: 0.08,
  dedicated: 0.16,
};

// One label column plus a column per tier (free, payg, dedicated, self). The
// four tier columns make the matrix wide, so it lives in a horizontal scroller.
const MATRIX_COLS =
  "grid grid-cols-[minmax(12rem,1.5fr)_repeat(4,minmax(0,1fr))] gap-x-3 sm:gap-x-4";

export function ClientPage() {
  return (
    <MotionConfig reducedMotion="user">
      <Hero />
      <PlanTierStrip />
      <UnlockStrip />
      <ComparisonMatrix />
      <Faq />
      <ContactBand />
      <ClosingCta />
    </MotionConfig>
  );
}

function Hero() {
  return (
    <section className="relative pt-10 pb-14 sm:pt-16 sm:pb-20">
      {/* Faint cc-accent radial glow anchored top-left */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(60% 50% at 0% 0%, rgba(94, 234, 212, 0.06), transparent 70%)",
        }}
      />
      <div className="grid gap-10 md:grid-cols-[1.4fr_1fr] md:items-start">
        <div>
          <motion.p
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, ease: EASE_OUT_QUART }}
            className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase"
          >
            Nitro GraphQL pricing
          </motion.p>
          <motion.h1
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, ease: EASE_OUT_QUART, delay: 0.06 }}
            className="font-heading text-cc-heading sm:text-h2 mt-5 text-4xl font-semibold"
          >
            Pricing that cascades with your GraphQL platform.
          </motion.h1>
          <motion.p
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, ease: EASE_OUT_QUART, delay: 0.14 }}
            className="text-cc-ink mt-6 max-w-2xl text-base text-pretty sm:text-lg"
          >
            Start free on shared cloud with 1M operations a month. Move to Pay
            as you go at $20 a month as traffic grows. Step up to a dedicated
            single-tenant instance, priced by volume, when you need SSO, private
            networking, and configurable retention. Self-host on your own
            infrastructure when the policy demands it.
          </motion.p>
          <motion.div
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.22 }}
            className="mt-9 flex flex-wrap items-center gap-3"
          >
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/services/support/contact">
              Talk to Sales
            </OutlineButton>
          </motion.div>
        </div>

        <HeroLegend />
      </div>
    </section>
  );
}

function HeroLegend() {
  const items: readonly {
    readonly mono: string;
    readonly label: string;
    readonly note: string;
  }[] = [
    {
      mono: "SR",
      label: "Schema registry",
      note: "History, rollback, CI checks",
    },
    {
      mono: "OT",
      label: "OpenTelemetry-native",
      note: "Traces, metrics, logs",
    },
    {
      mono: "FX",
      label: "Fusion + ASP.NET Core",
      note: "Gateway you run yourself",
    },
  ];

  return (
    <motion.aside
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.6, ease: EASE_OUT_QUART, delay: 0.18 }}
      className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6"
      aria-label="Included on every tier"
    >
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        Included on every tier
      </p>
      <ul className="mt-5 flex flex-col gap-4">
        {items.map((item) => (
          <li key={item.label} className="flex items-start gap-3">
            <span
              aria-hidden="true"
              className="border-cc-card-border text-cc-accent bg-cc-surface flex h-9 w-9 flex-none items-center justify-center rounded-full border font-mono text-[0.65rem] tracking-[0.12em] uppercase"
            >
              {item.mono}
            </span>
            <span className="flex flex-col">
              <span className="text-cc-heading font-heading text-sm font-semibold">
                {item.label}
              </span>
              <span className="text-cc-ink-dim text-xs">{item.note}</span>
            </span>
          </li>
        ))}
      </ul>
    </motion.aside>
  );
}

function PlanTierStrip() {
  return (
    <section
      aria-labelledby="plans-heading"
      className="relative pb-16 sm:pb-24"
    >
      <h2 id="plans-heading" className="sr-only">
        Plans
      </h2>
      <div className="relative">
        {/* Connected baseline rail behind cards, suggesting a tier ladder. */}
        <div
          aria-hidden="true"
          className="bg-cc-card-border pointer-events-none absolute right-8 left-8 hidden h-px lg:block"
          style={{ top: "calc(50% + 1.5rem)" }}
        />
        <div className="relative grid gap-6 lg:grid-cols-3 lg:items-stretch">
          {CLOUD_TIERS.map((tier) => (
            <PlanCard key={tier.id} tier={tier} />
          ))}
        </div>
      </div>

      <SelfHostedStrip />
    </section>
  );
}

function PlanCard({ tier }: { readonly tier: Tier }) {
  const delay = CASCADE_DELAYS[tier.id] ?? 0;
  const cardInitial = { opacity: 0, y: 12 };
  const cardAnimate = { opacity: 1, y: 0 };
  const cardTransition = {
    duration: 0.55,
    ease: EASE_OUT_QUART,
    delay,
  } as const;

  if (tier.popular) {
    return (
      <motion.div
        initial={cardInitial}
        whileInView={cardAnimate}
        viewport={VIEWPORT_ONCE}
        transition={cardTransition}
        className="relative rounded-3xl p-[1.5px] lg:-my-2"
        style={{
          background:
            "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
      >
        <PopularPill />
        <div className="bg-cc-surface flex h-full flex-col rounded-[calc(1.5rem-1.5px)] p-7 sm:p-8">
          <PlanCardBody tier={tier} cascadeDelay={delay} />
        </div>
      </motion.div>
    );
  }

  return (
    <motion.div
      initial={cardInitial}
      whileInView={cardAnimate}
      viewport={VIEWPORT_ONCE}
      transition={cardTransition}
      className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-8"
    >
      <PlanCardBody tier={tier} cascadeDelay={delay} />
    </motion.div>
  );
}

function PlanCardBody({
  tier,
  cascadeDelay,
}: {
  readonly tier: Tier;
  readonly cascadeDelay: number;
}) {
  const CallToAction = tier.popular ? SolidButton : OutlineButton;
  // Inner stagger starts shortly after the card has landed.
  const innerDelay = cascadeDelay + 0.25;

  return (
    <>
      <h3 className="font-heading text-cc-heading text-h5 font-semibold">
        {tier.name}
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm">{tier.tagline}</p>
      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h3 font-semibold">
          {tier.price}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {tier.priceNote}
        </span>
      </div>
      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-6 border-t border-dashed"
      />
      <motion.ul
        initial="hidden"
        whileInView="show"
        viewport={VIEWPORT_ONCE}
        variants={{
          hidden: {},
          show: {
            transition: {
              staggerChildren: 0.04,
              delayChildren: innerDelay,
            },
          },
        }}
        className="flex flex-1 flex-col gap-3"
      >
        {tier.features.map((feature) => (
          <motion.li
            key={feature}
            variants={{
              hidden: { opacity: 0, y: 4 },
              show: {
                opacity: 1,
                y: 0,
                transition: { duration: 0.3, ease: EASE_OUT_QUART },
              },
            }}
            className="flex items-start gap-3"
          >
            <span className="text-cc-accent mt-[5px] flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{feature}</span>
          </motion.li>
        ))}
      </motion.ul>
      <CallToAction href={tier.ctaHref} className="mt-8 w-full">
        {tier.cta}
      </CallToAction>
    </>
  );
}

function PopularPill() {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-1/2 z-10 -translate-x-1/2 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      Most popular
    </span>
  );
}

function SelfHostedStrip() {
  if (!SELF_HOSTED) {
    return null;
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={VIEWPORT_ONCE}
      transition={{ duration: 0.55, ease: EASE_OUT_QUART, delay: 0.24 }}
      className="border-cc-card-border bg-cc-card-bg/60 mt-6 flex flex-col gap-5 rounded-3xl border p-6 sm:flex-row sm:items-center sm:justify-between sm:p-8"
    >
      <div>
        <div className="flex items-baseline gap-3">
          <h3 className="font-heading text-cc-heading text-h6 font-semibold">
            {SELF_HOSTED.name}
          </h3>
          <span className="text-cc-nav-label font-mono text-xs">
            {SELF_HOSTED.price} · {SELF_HOSTED.priceNote}
          </span>
        </div>
        <p className="text-cc-ink mt-2 max-w-2xl text-sm text-pretty">
          {SELF_HOSTED.tagline} Run on your own infrastructure, air-gapped or
          on-prem, with configurable retention, priority engineering support,
          and a long-term release channel.
        </p>
      </div>
      <OutlineButton href={SELF_HOSTED.ctaHref} className="shrink-0 sm:w-auto">
        {SELF_HOSTED.cta}
      </OutlineButton>
    </motion.div>
  );
}

function UnlockStrip() {
  return (
    <section aria-labelledby="unlock-heading" className="pb-16 sm:pb-24">
      <div className="mx-auto max-w-2xl text-center">
        <h2
          id="unlock-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold"
        >
          Unlock more as you grow
        </h2>
        <p className="text-cc-ink mt-4 text-base text-pretty">
          Commit to a minimum monthly spend to unlock more, up to your spend.
        </p>
      </div>

      <motion.ul
        initial="hidden"
        whileInView="show"
        viewport={VIEWPORT_ONCE}
        variants={{
          hidden: {},
          show: { transition: { staggerChildren: 0.08, delayChildren: 0.05 } },
        }}
        className="mx-auto mt-10 flex max-w-3xl flex-col gap-3"
      >
        {UNLOCKS.map((unlock, index) => (
          <UnlockRow key={unlock.title} unlock={unlock} index={index} />
        ))}
      </motion.ul>
      <p className="text-cc-nav-label mt-5 text-center font-mono text-[0.7rem]">
        {UNLOCKS_NOTE}
      </p>
    </section>
  );
}

function UnlockRow({
  unlock,
  index,
}: {
  readonly unlock: Unlock;
  readonly index: number;
}) {
  const Glyph = UNLOCK_ICONS[index] ?? SupportGlyph;
  return (
    <motion.li
      variants={{
        hidden: { opacity: 0, y: 10 },
        show: {
          opacity: 1,
          y: 0,
          transition: { duration: 0.45, ease: EASE_OUT_QUART },
        },
      }}
      className="border-cc-card-border bg-cc-card-bg/60 flex items-center gap-4 rounded-2xl border p-5 sm:gap-5 sm:p-6"
    >
      <span
        aria-hidden="true"
        className="border-cc-card-border bg-cc-surface text-cc-accent flex size-11 flex-none items-center justify-center rounded-xl border"
      >
        <Glyph className="size-5" />
      </span>
      <div className="min-w-0 flex-1">
        <h3 className="font-heading text-cc-heading text-base font-semibold">
          {unlock.title}
        </h3>
        <p className="text-cc-ink-dim mt-1 text-sm text-pretty">
          {unlock.description}
        </p>
      </div>
      <span className="text-cc-accent flex-none font-mono text-sm font-semibold tabular-nums sm:text-base">
        {unlock.spend}
      </span>
    </motion.li>
  );
}

interface GlyphProps {
  readonly className?: string;
}

/** Lifebuoy, for the Business Support unlock. */
function SupportGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      aria-hidden="true"
      className={className}
    >
      <circle cx="12" cy="12" r="9" />
      <circle cx="12" cy="12" r="3.4" />
      <path d="M5.2 5.2l4.4 4.4M18.8 5.2l-4.4 4.4M5.2 18.8l4.4-4.4M18.8 18.8l-4.4-4.4" />
    </svg>
  );
}

/** Shield with a check, for the second support unlock tier. */
function ShieldGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M12 3l7 3v5c0 4-3 6.6-7 8-4-1.4-7-4-7-8V6z" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  );
}

/** Cloud, for the BYOC unlock. */
function CloudGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M7 18h10a4 4 0 0 0 .5-7.97A5.5 5.5 0 0 0 6.5 9 4 4 0 0 0 7 18z" />
    </svg>
  );
}

const UNLOCK_ICONS = [SupportGlyph, ShieldGlyph, CloudGlyph];

function ComparisonMatrix() {
  return (
    <section
      aria-labelledby="compare-heading"
      className="border-cc-card-border bg-cc-card-bg/40 relative rounded-3xl border p-6 sm:p-10"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Compare plans
        </p>
        <h2
          id="compare-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Every capability, side by side.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          The same Nitro platform on every plan. What changes is where it runs,
          who you share it with, how you are billed, and what support you get
          from us.
        </p>
      </div>

      <MatrixGrid />
    </section>
  );
}

function MatrixGrid() {
  return (
    <div className="-mx-6 mt-10 overflow-x-auto px-6 sm:-mx-10 sm:px-10">
      <div className="min-w-[60rem]">
        {/* Column header row */}
        <div className={`${MATRIX_COLS} items-end pb-4`}>
          <div className="sr-only">Capability</div>
          {TIERS.map((tier) => (
            <div key={tier.id} className="px-2 text-center">
              <div
                className={`font-heading text-cc-heading text-sm font-semibold sm:text-base ${
                  tier.popular ? "text-cc-accent" : ""
                }`}
              >
                {tier.name}
              </div>
              <div className="text-cc-nav-label mt-1 font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                {tier.price} · {tier.priceNote}
              </div>
            </div>
          ))}
        </div>

        <div className="flex flex-col">
          {COMPARISON.map((group) => (
            <MatrixGroup key={group.title} group={group} />
          ))}
        </div>
      </div>
    </div>
  );
}

function MatrixGroup({ group }: { readonly group: ComparisonGroup }) {
  return (
    <motion.section
      initial="hidden"
      whileInView="show"
      viewport={VIEWPORT_ONCE}
      variants={{
        hidden: {},
        show: { transition: { staggerChildren: 0.03, delayChildren: 0.18 } },
      }}
      aria-label={group.title}
      className="border-cc-ink-faint border-t"
    >
      <motion.header
        variants={{
          hidden: { opacity: 0, x: -8 },
          show: {
            opacity: 1,
            x: 0,
            transition: { duration: 0.4, ease: EASE_OUT_QUART },
          },
        }}
        className={`${MATRIX_COLS} items-center pt-6 pb-3`}
      >
        <span className="text-cc-nav-label inline-flex items-center pl-2 font-mono text-xs tracking-[0.15em] uppercase">
          <span
            aria-hidden="true"
            className="bg-cc-accent mr-3 inline-block h-1 w-3 rounded-full"
          />
          {group.title}
        </span>
        {/* Pay as you go soft tint as a column hint */}
        <span aria-hidden="true" />
        <span aria-hidden="true" className="bg-cc-accent/5 h-full rounded-sm" />
        <span aria-hidden="true" />
        <span aria-hidden="true" />
      </motion.header>

      <div role="list" className="flex flex-col">
        {group.rows.map((row) => (
          <motion.div
            key={row.label}
            role="listitem"
            variants={{
              hidden: { opacity: 0, y: 4 },
              show: {
                opacity: 1,
                y: 0,
                transition: { duration: 0.32, ease: EASE_OUT_QUART },
              },
            }}
            className={`${MATRIX_COLS} border-cc-ink-faint/50 items-start border-t py-3`}
          >
            <div className="text-cc-ink pl-2 text-sm">{row.label}</div>
            <MatrixCell value={row.free} />
            <MatrixCell value={row.payg} highlight />
            <MatrixCell value={row.dedicated} />
            <MatrixCell value={row.self} />
          </motion.div>
        ))}
      </div>
    </motion.section>
  );
}

function MatrixCell({
  value,
  highlight = false,
}: {
  readonly value: Cell;
  readonly highlight?: boolean;
}) {
  return (
    <div
      className={`flex min-h-[1.5rem] items-start justify-center px-2 text-center text-sm ${
        highlight ? "bg-cc-accent/5 rounded-sm" : ""
      }`}
    >
      {typeof value === "boolean" ? (
        value ? (
          <span
            className="text-cc-accent inline-flex pt-[2px]"
            aria-label="Included"
          >
            <CheckIcon size={16} />
          </span>
        ) : (
          <span
            className="text-cc-ink-faint inline-block"
            aria-label="Not included"
          >
            &ndash;
          </span>
        )
      ) : (
        <span className="text-cc-ink">{value}</span>
      )}
    </div>
  );
}

function Faq() {
  return (
    <section aria-labelledby="faq-heading" className="mt-20 sm:mt-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Frequently asked
        </p>
        <h2
          id="faq-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Honest answers about pricing.
        </h2>
      </div>

      <motion.dl
        initial="hidden"
        whileInView="show"
        viewport={VIEWPORT_ONCE}
        variants={{
          hidden: {},
          // Pairwise stagger: two cards per row, each pair starts 0.08s later.
          show: { transition: { staggerChildren: 0.04, delayChildren: 0.05 } },
        }}
        className="mt-10 grid gap-4 md:grid-cols-2"
      >
        {FAQ.map((item) => (
          <FaqEntry key={item.question} item={item} />
        ))}
      </motion.dl>
    </section>
  );
}

function FaqEntry({ item }: { readonly item: PricingFaq }) {
  const reduce = useReducedMotion();
  return (
    <motion.div
      variants={{
        hidden: { opacity: 0, y: reduce ? 0 : 8 },
        show: {
          opacity: 1,
          y: 0,
          transition: { duration: 0.4, ease: EASE_OUT_QUART },
        },
      }}
      className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6"
    >
      <dt className="font-heading text-cc-heading text-base font-semibold">
        {item.question}
      </dt>
      <dd className="text-cc-ink mt-3 text-sm leading-relaxed">
        {item.answer}
      </dd>
    </motion.div>
  );
}

function ContactBand() {
  return (
    <Section className="mt-20 sm:mt-28">
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={VIEWPORT_ONCE}
        transition={{ duration: 0.55, ease: EASE_OUT_QUART }}
        className="border-cc-card-border bg-cc-card-bg/70 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center"
      >
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Custom terms
          </p>
          <h2 className="font-heading text-cc-heading text-h4 mt-3 font-semibold">
            Custom volume, procurement, or air-gapped?
          </h2>
          <p className="text-cc-ink mt-4 text-base">
            We work directly with platform and security teams on bespoke
            commercial terms, on-prem rollouts, and migrations from existing
            GraphQL gateways. Engineers, not gatekeepers, run the call.
          </p>
          <ul className="text-cc-ink mt-6 grid gap-3 text-sm sm:grid-cols-2">
            <ContactCheck>Dedicated solution architect</ContactCheck>
            <ContactCheck>Annual contracts &amp; POs</ContactCheck>
            <ContactCheck>Security &amp; DPA review</ContactCheck>
          </ul>
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <SolidButton href="/services/support/contact">
            Contact Sales
          </SolidButton>
          <OutlineButton href="/platform">Explore the platform</OutlineButton>
        </div>
      </motion.div>
    </Section>
  );
}

function ContactCheck({ children }: { readonly children: ReactNode }) {
  return (
    <li className="flex items-start gap-3">
      <span className="text-cc-accent mt-[5px] flex-none">
        <CheckIcon />
      </span>
      <span>{children}</span>
    </li>
  );
}

function ClosingCta() {
  return (
    <section className="mt-20 mb-8 text-center sm:mt-28">
      <motion.h2
        initial={{ opacity: 0, y: 10 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={VIEWPORT_ONCE}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART }}
        className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold"
      >
        Ship your GraphQL platform with Nitro.
      </motion.h2>
      <motion.p
        initial={{ opacity: 0, y: 10 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={VIEWPORT_ONCE}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.08 }}
        className="text-cc-ink mx-auto mt-5 max-w-2xl text-base"
      >
        Start free on shared cloud in minutes. Move to Pay as you go as traffic
        grows, or a dedicated instance when you need SSO, private networking,
        and configurable retention. The docs walk you through every step.
      </motion.p>
      <motion.div
        initial={{ opacity: 0, y: 8 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={VIEWPORT_ONCE}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.16 }}
        className="mt-8 flex flex-wrap items-center justify-center gap-3"
      >
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the docs</OutlineButton>
      </motion.div>
    </section>
  );
}

function Section({
  className = "",
  children,
}: {
  readonly className?: string;
  readonly children: ReactNode;
}) {
  return <section className={className}>{children}</section>;
}
