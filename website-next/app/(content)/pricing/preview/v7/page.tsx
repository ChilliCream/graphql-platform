"use client";

import {
  MotionConfig,
  animate,
  motion,
  useInView,
  useMotionValue,
  useReducedMotion,
  useTransform,
} from "motion/react";
import type { ReactNode } from "react";
import { useEffect, useRef } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import type {
  Cell,
  ComparisonGroup,
  PricingFaq,
  Tier,
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

// Note: This page is a "use client" file because the centerpiece animation
// relies on motion hooks (useInView, useMotionValue, useTransform, animate,
// useReducedMotion). Next.js does not allow `export const metadata` from a
// client component, so robots/no-index for this preview path is enforced via
// the parent route configuration. The page is at /pricing/preview/v7/ which
// is an internal preview path and not surfaced in production navigation.
// Primary keyword: Nitro GraphQL pricing.
//
// All pricing data (tiers, comparison rows, unlocks, FAQ) is imported from the
// shared module in src/components/pricing/pricingData. This page never inlines
// pricing numbers; it only renders that single source of truth.

// The three cloud tiers (Free, Pay as you go, Dedicated) are shown as cards;
// Self-Hosted is presented as a slim strip below them.
const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_HOSTED = TIERS.find((tier) => tier.id === "self");

const EASE_OUT_QUART: readonly [number, number, number, number] = [
  0.22, 1, 0.36, 1,
];

/** Parse a clean "$<number>" price for the animated counter on the popular card. */
function parsePrice(price: string): number | undefined {
  const match = price.match(/^\$(\d+)$/);
  return match ? Number(match[1]) : undefined;
}

export default function PricingPreviewV7Page() {
  return (
    <MotionConfig reducedMotion="user">
      <Hero />
      <PlanGrid />
      <UpgradePathStrip />
      <ComparisonMatrix />
      <UnlockBand />
      <Faq />
      <CustomBand />
      <ClosingCta />
    </MotionConfig>
  );
}

function Hero() {
  return (
    <section className="pt-10 pb-14 text-center sm:pt-16 sm:pb-20">
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
        transition={{ duration: 0.6, ease: EASE_OUT_QUART, delay: 0.08 }}
        className="font-heading text-cc-heading sm:text-h2 mt-5 text-4xl font-semibold"
      >
        Pricing that scales with your GraphQL platform.
      </motion.h1>
      <motion.p
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6, ease: EASE_OUT_QUART, delay: 0.16 }}
        className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg"
      >
        Start free on shared cloud with 1M operations a month. Move to Pay as
        you go at $20 a month when you need more, then to a dedicated,
        single-tenant instance priced by volume. Self-host on your own
        infrastructure when the workload, or the policy, demands it.
      </motion.p>
      <motion.div
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.24 }}
        className="mt-9 flex flex-wrap items-center justify-center gap-3"
      >
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
      </motion.div>
    </section>
  );
}

function PlanGrid() {
  const containerRef = useRef<HTMLDivElement>(null);
  const inView = useInView(containerRef, { once: true, amount: 0.35 });

  return (
    <section aria-labelledby="plans-heading" className="pb-16 sm:pb-24">
      <h2 id="plans-heading" className="sr-only">
        Plans
      </h2>
      <motion.div
        ref={containerRef}
        variants={{
          hidden: {},
          show: { transition: { staggerChildren: 0.12, delayChildren: 0.05 } },
        }}
        initial="hidden"
        animate={inView ? "show" : "hidden"}
        className="grid gap-6 lg:grid-cols-3 lg:items-stretch"
      >
        {CLOUD_TIERS.map((tier) => (
          <PlanCard key={tier.id} plan={tier} parentInView={inView} />
        ))}
      </motion.div>

      {SELF_HOSTED && <SelfHostedStrip tier={SELF_HOSTED} />}
    </section>
  );
}

function PlanCard({
  plan,
  parentInView,
}: {
  readonly plan: Tier;
  readonly parentInView: boolean;
}) {
  const cardVariants = {
    hidden: { opacity: 0, y: 24 },
    show: {
      opacity: 1,
      y: 0,
      transition: { duration: 0.5, ease: EASE_OUT_QUART },
    },
  } as const;

  if (plan.popular) {
    return (
      <motion.div
        variants={cardVariants}
        className="relative rounded-3xl p-[1.5px] lg:-my-2"
        style={{
          background:
            "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
      >
        <PopularPill />
        <PopularGradientFrame active={parentInView} />
        <div className="bg-cc-surface relative flex h-full flex-col rounded-[calc(1.5rem-1.5px)] p-7 sm:p-8">
          <PlanCardBody plan={plan} active={parentInView} />
        </div>
      </motion.div>
    );
  }

  return (
    <motion.div
      variants={cardVariants}
      className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-8"
    >
      <PlanCardBody plan={plan} active={parentInView} />
    </motion.div>
  );
}

function PopularGradientFrame({ active }: { readonly active: boolean }) {
  const reduce = useReducedMotion();
  return (
    <svg
      aria-hidden="true"
      className="pointer-events-none absolute inset-0 h-full w-full"
      preserveAspectRatio="none"
      viewBox="0 0 100 100"
    >
      <defs>
        <linearGradient id="popularGradient-v7" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#16b9e4" />
          <stop offset="50%" stopColor="#7c92c6" />
          <stop offset="100%" stopColor="#f0786a" />
        </linearGradient>
      </defs>
      <motion.rect
        x="1"
        y="1"
        width="98"
        height="98"
        rx="5"
        ry="5"
        fill="none"
        stroke="url(#popularGradient-v7)"
        strokeWidth="0.6"
        vectorEffect="non-scaling-stroke"
        initial={reduce ? { pathLength: 1 } : { pathLength: 0 }}
        animate={active ? { pathLength: 1 } : { pathLength: 0 }}
        transition={{ duration: 1.2, ease: "easeInOut", delay: 0.4 }}
      />
    </svg>
  );
}

function PlanCardBody({
  plan,
  active,
}: {
  readonly plan: Tier;
  readonly active: boolean;
}) {
  const CallToAction = plan.popular ? SolidButton : OutlineButton;
  const numeric = plan.popular ? parsePrice(plan.price) : undefined;
  return (
    <>
      <h3 className="font-heading text-cc-heading text-h5 font-semibold">
        {plan.name}
      </h3>
      <p className="text-cc-ink-dim mt-2 text-sm">{plan.tagline}</p>
      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h3 font-semibold">
          {numeric !== undefined ? (
            <AnimatedPrice target={numeric} active={active} />
          ) : (
            plan.price
          )}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {plan.priceNote}
        </span>
      </div>
      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-6 border-t border-dashed"
      />
      <motion.ul
        variants={{
          hidden: {},
          show: { transition: { staggerChildren: 0.06, delayChildren: 0.3 } },
        }}
        className="flex flex-1 flex-col gap-3"
      >
        {plan.features.map((feature) => (
          <motion.li
            key={feature}
            variants={{
              hidden: { opacity: 0, x: -6 },
              show: {
                opacity: 1,
                x: 0,
                transition: { duration: 0.35, ease: EASE_OUT_QUART },
              },
            }}
            className="flex items-start gap-3"
          >
            <motion.span
              variants={{
                hidden: { scale: 0 },
                show: {
                  scale: 1,
                  transition: { duration: 0.3, ease: EASE_OUT_QUART },
                },
              }}
              className="text-cc-accent mt-[5px] flex-none"
            >
              <CheckIcon />
            </motion.span>
            <span className="text-cc-ink text-sm">{feature}</span>
          </motion.li>
        ))}
      </motion.ul>
      <CallToAction href={plan.ctaHref} className="mt-8 w-full">
        {plan.cta}
      </CallToAction>
    </>
  );
}

function SelfHostedStrip({ tier }: { readonly tier: Tier }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.4 }}
      transition={{ duration: 0.5, ease: EASE_OUT_QUART }}
      className="border-cc-card-border bg-cc-card-bg/60 mt-6 flex flex-col gap-5 rounded-3xl border p-6 sm:flex-row sm:items-center sm:justify-between sm:p-8"
    >
      <div>
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          {tier.price}
        </p>
        <h3 className="font-heading text-cc-heading text-h6 mt-2 font-semibold">
          {tier.name}
        </h3>
        <p className="text-cc-ink mt-2 max-w-2xl text-sm text-pretty">
          {tier.tagline} Run on your own infrastructure, air-gapped or on-prem,
          with configurable retention, priority engineering support, and a
          long-term release channel.
        </p>
      </div>
      <OutlineButton href={tier.ctaHref} className="shrink-0 sm:w-auto">
        {tier.cta}
      </OutlineButton>
    </motion.div>
  );
}

function AnimatedPrice({
  target,
  active,
}: {
  readonly target: number;
  readonly active: boolean;
}) {
  const reduce = useReducedMotion();
  const count = useMotionValue(reduce ? target : 0);
  const display = useTransform(count, (v) => `$${Math.round(v)}`);

  useEffect(() => {
    if (!active) {
      return;
    }
    if (reduce) {
      count.set(target);
      return;
    }
    const controls = animate(count, target, {
      duration: 1.1,
      ease: "easeOut",
      delay: 0.4,
    });
    return () => controls.stop();
  }, [active, count, reduce, target]);

  return <motion.span aria-label={`$${target}`}>{display}</motion.span>;
}

function PopularPill() {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-1/2 z-10 -translate-x-1/2 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      Most popular
    </span>
  );
}

function UpgradePathStrip() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.5 });
  const reduce = useReducedMotion();

  const stops: readonly {
    readonly label: string;
    readonly position: string;
  }[] = [
    { label: "Free", position: "0%" },
    { label: "Pay as you go", position: "33%" },
    { label: "Dedicated", position: "66%" },
    { label: "Self-Hosted", position: "100%" },
  ];

  return (
    <section
      aria-label="Upgrade path"
      className="mb-16 hidden sm:mb-24 lg:block"
    >
      <div ref={ref} className="relative mx-auto max-w-4xl px-4 py-2">
        <p className="text-cc-nav-label mb-4 text-center font-mono text-xs tracking-[0.18em] uppercase">
          Upgrade path
        </p>
        <div className="relative h-10">
          <motion.div
            initial={reduce ? { scaleX: 1 } : { scaleX: 0 }}
            animate={inView ? { scaleX: 1 } : { scaleX: 0 }}
            transition={{ duration: 1, ease: EASE_OUT_QUART }}
            style={{ transformOrigin: "left" }}
            className="bg-cc-ink-faint absolute top-1/2 right-0 left-0 h-px -translate-y-1/2"
          />
          <motion.div
            initial={reduce ? { left: "100%" } : { left: "0%" }}
            animate={inView ? { left: "100%" } : { left: "0%" }}
            transition={{ duration: 1.4, ease: EASE_OUT_QUART, delay: 0.1 }}
            className="bg-cc-accent absolute top-1/2 h-2 w-2 -translate-x-1/2 -translate-y-1/2 rounded-full shadow-[0_0_12px_rgba(94,234,212,0.7)]"
          />
          {stops.map((stop, i) => (
            <motion.div
              key={stop.label}
              initial={reduce ? { opacity: 1 } : { opacity: 0 }}
              animate={inView ? { opacity: 1 } : { opacity: 0 }}
              transition={{
                duration: 0.4,
                ease: EASE_OUT_QUART,
                delay: 0.4 + i * 0.15,
              }}
              className="absolute top-1/2 flex -translate-y-1/2 flex-col items-center"
              style={{
                left: stop.position,
                transform: `translate(-50%, -50%)`,
              }}
            >
              <span className="bg-cc-bg border-cc-card-border h-3 w-3 rounded-full border" />
              <span className="text-cc-ink-dim font-heading mt-3 text-xs whitespace-nowrap">
                {stop.label}
              </span>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
}

function ComparisonMatrix() {
  return (
    <section
      aria-labelledby="compare-heading"
      className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-10"
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
          The same Nitro platform across every plan. What changes is where it
          runs, who you share it with, and what you get from us.
        </p>
      </div>

      <div className="mt-10 overflow-x-auto">
        <table className="w-full min-w-[52rem] border-separate border-spacing-0 text-left text-sm">
          <thead>
            <tr>
              <th scope="col" className="w-1/3 pb-4 pl-2">
                <span className="sr-only">Capability</span>
              </th>
              {TIERS.map((tier) => (
                <th
                  key={tier.id}
                  scope="col"
                  className="pb-4 text-center align-bottom"
                >
                  <div
                    className={`font-heading text-cc-heading text-base font-semibold ${
                      tier.popular ? "text-cc-accent" : ""
                    }`}
                  >
                    {tier.name}
                  </div>
                  <div className="text-cc-nav-label mt-1 font-mono text-[0.65rem] tracking-[0.15em] uppercase">
                    {tier.price} · {tier.priceNote}
                  </div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {COMPARISON.map((group, groupIndex) => (
              <ComparisonGroupRows
                key={group.title}
                group={group}
                groupIndex={groupIndex}
              />
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function ComparisonGroupRows({
  group,
  groupIndex,
}: {
  readonly group: ComparisonGroup;
  readonly groupIndex: number;
}) {
  return (
    <>
      <motion.tr
        initial={{ opacity: 0, y: 8 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, amount: 0.3 }}
        transition={{ duration: 0.45, ease: EASE_OUT_QUART }}
      >
        <th
          scope="colgroup"
          colSpan={5}
          className="border-cc-ink-faint text-cc-nav-label border-t pt-6 pb-3 pl-2 text-left font-mono text-xs tracking-[0.15em] uppercase"
        >
          {group.title}
        </th>
      </motion.tr>
      {group.rows.map((row, rowIndex) => (
        <motion.tr
          key={row.label}
          initial={{ opacity: 0, y: 6 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, amount: 0.2 }}
          transition={{
            duration: 0.35,
            ease: EASE_OUT_QUART,
            delay: Math.min(rowIndex * 0.04, 0.24),
          }}
        >
          <th
            scope="row"
            className="text-cc-ink py-3 pl-2 text-left align-top text-sm font-normal"
          >
            {row.label}
          </th>
          <ComparisonCell value={row.free} />
          <ComparisonCell
            value={row.payg}
            highlight
            pulse={rowIndex === 0 && groupIndex === 0}
          />
          <ComparisonCell value={row.dedicated} />
          <ComparisonCell value={row.self} />
        </motion.tr>
      ))}
    </>
  );
}

function ComparisonCell({
  value,
  highlight = false,
  pulse = false,
}: {
  readonly value: Cell;
  readonly highlight?: boolean;
  readonly pulse?: boolean;
}) {
  const inner =
    typeof value === "boolean" ? (
      value ? (
        <span className="text-cc-accent inline-flex" aria-label="Included">
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
    );

  if (highlight && pulse) {
    return (
      <motion.td
        className="bg-cc-accent/5 py-3 text-center align-top text-sm"
        initial={{ backgroundColor: "rgba(94, 234, 212, 0.05)" }}
        whileInView={{
          backgroundColor: [
            "rgba(94, 234, 212, 0.05)",
            "rgba(94, 234, 212, 0.18)",
            "rgba(94, 234, 212, 0.05)",
          ],
        }}
        viewport={{ once: true, amount: 0.6 }}
        transition={{ duration: 1.2, ease: "easeInOut" }}
      >
        {inner}
      </motion.td>
    );
  }

  return (
    <td
      className={`py-3 text-center align-top text-sm ${
        highlight ? "bg-cc-accent/5" : ""
      }`}
    >
      {inner}
    </td>
  );
}

function UnlockBand() {
  const ref = useRef<HTMLOListElement>(null);
  const inView = useInView(ref, { once: true, amount: 0.3 });

  return (
    <section aria-labelledby="unlock-heading" className="mt-20 sm:mt-28">
      <div className="mx-auto max-w-2xl text-center">
        <h2
          id="unlock-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold"
        >
          Unlock more as you grow
        </h2>
        <p className="text-cc-ink mx-auto mt-4 text-base text-pretty">
          Commit to a minimum monthly spend to unlock more, up to your spend.
        </p>
      </div>

      <motion.ol
        ref={ref}
        variants={{
          hidden: {},
          show: { transition: { staggerChildren: 0.12, delayChildren: 0.05 } },
        }}
        initial="hidden"
        animate={inView ? "show" : "hidden"}
        className="mx-auto mt-10 flex max-w-3xl flex-col gap-3"
      >
        {UNLOCKS.map((unlock, index) => (
          <UnlockRow key={unlock.title} unlock={unlock} index={index} />
        ))}
      </motion.ol>
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
        hidden: { opacity: 0, y: 18 },
        show: {
          opacity: 1,
          y: 0,
          transition: { duration: 0.45, ease: EASE_OUT_QUART },
        },
      }}
      className="border-cc-card-border bg-cc-card-bg flex items-center gap-4 rounded-2xl border p-5 sm:gap-5 sm:p-6"
    >
      <span className="border-cc-card-border bg-cc-surface text-cc-accent flex size-11 shrink-0 items-center justify-center rounded-xl border">
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
      <span className="text-cc-accent shrink-0 font-mono text-base font-semibold sm:text-lg">
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

/** Shield with a check, for the priority support unlock. */
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

      <dl className="mt-10 grid gap-4 md:grid-cols-2">
        {FAQ.map((item, i) => (
          <FaqEntry key={item.question} item={item} index={i} />
        ))}
      </dl>
    </section>
  );
}

function FaqEntry({
  item,
  index,
}: {
  readonly item: PricingFaq;
  readonly index: number;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.35 }}
      transition={{
        duration: 0.45,
        ease: EASE_OUT_QUART,
        delay: Math.min(index * 0.05, 0.2),
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

function CustomBand() {
  return (
    <Section className="mt-20 sm:mt-28">
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, amount: 0.3 }}
        transition={{ duration: 0.55, ease: EASE_OUT_QUART }}
        className="border-cc-card-border bg-cc-card-bg/70 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center"
      >
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Custom
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
            <CustomCheck>Dedicated solution architect</CustomCheck>
            <CustomCheck>Annual contracts &amp; POs</CustomCheck>
            <CustomCheck>Security &amp; DPA review</CustomCheck>
            <CustomCheck>Migration playbooks</CustomCheck>
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

function CustomCheck({ children }: { readonly children: ReactNode }) {
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
        viewport={{ once: true, amount: 0.4 }}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART }}
        className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold"
      >
        Ship your GraphQL platform with Nitro.
      </motion.h2>
      <motion.p
        initial={{ opacity: 0, y: 10 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, amount: 0.4 }}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.08 }}
        className="text-cc-ink mx-auto mt-5 max-w-2xl text-base"
      >
        Start free in minutes. Move to Pay as you go when you outgrow it, or to
        a dedicated, single-tenant instance with your own region, SSO, and
        configurable retention. The docs walk you through every step.
      </motion.p>
      <motion.div
        initial={{ opacity: 0, scale: 0.96 }}
        whileInView={{ opacity: 1, scale: 1 }}
        viewport={{ once: true, amount: 0.4 }}
        transition={{ duration: 0.5, ease: EASE_OUT_QUART, delay: 0.18 }}
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
