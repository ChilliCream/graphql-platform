import type { ComponentType } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { Chemex } from "@/src/icons/Chemex";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

interface Plan {
  readonly brew: string;
  readonly Icon: ComponentType<{ readonly className?: string }>;
  readonly name: string;
  readonly tagline: string;
  readonly price: string;
  readonly priceNote: string;
  readonly features: readonly string[];
  readonly cta: string;
  readonly ctaHref: string;
  readonly popular?: boolean;
}

const PLANS: readonly Plan[] = [
  {
    brew: "French Press",
    Icon: FrenchPress,
    name: "Shared Instance",
    tagline: "Shared resources, fully managed",
    price: "Free",
    priceNote: "pay-as-you-go",
    features: [
      "Multi-tenant cloud region",
      "1 Schema · 3 Environments",
      "Up to 5M ops / month included",
      "Community Slack support",
      "Pay only for what you use after",
    ],
    cta: "Start for Free",
    ctaHref: "/get-started",
  },
  {
    brew: "Pour-Over",
    Icon: PourOver,
    name: "Dedicated Instance",
    tagline: "Dedicated resources, fully managed",
    price: "$400",
    priceNote: "per month",
    features: [
      "Single-tenant cloud region",
      "Unlimited schemas",
      "BYOC region · private networking",
      "99.95% SLA · email + private chat",
      "SSO, audit log, role-based access",
    ],
    cta: "Start for Free",
    ctaHref: "/get-started",
    popular: true,
  },
  {
    brew: "Chemex",
    Icon: Chemex,
    name: "Self-Hosted",
    tagline: "Self managed",
    price: "Custom",
    priceNote: "talk to us",
    features: [
      "Run on your own infrastructure",
      "Air-gapped & on-prem supported",
      "Priority engineering support",
      "Long-term release channel",
      "Custom training & onboarding",
    ],
    cta: "Talk to Us",
    ctaHref: "/services/support/contact",
  },
];

function Dots() {
  return (
    <div
      aria-hidden="true"
      className="my-5 border-t border-dashed border-[rgba(245,241,234,0.16)]"
    />
  );
}

function PlanCard({ plan }: { readonly plan: Plan }) {
  return (
    <div
      className={`relative flex flex-col rounded-3xl border p-6 sm:p-7 ${
        plan.popular
          ? "border-cc-note/50 bg-cc-card-bg"
          : "border-cc-card-border bg-cc-card-bg/60"
      }`}
    >
      {plan.popular && (
        <span className="bg-cc-surface text-cc-nav-label border-cc-card-border absolute -top-3 left-1/2 -translate-x-1/2 rounded-full border px-3 py-1 font-mono text-[0.65rem] tracking-[0.15em] uppercase">
          Most Popular
        </span>
      )}

      <div className="flex flex-col items-center text-center">
        <div
          className="flex size-24 items-center justify-center rounded-2xl"
          style={{ backgroundImage: "linear-gradient(180deg,#6157ec,#5d55f1)" }}
        >
          <plan.Icon className="h-16 w-auto text-white" />
        </div>
        <p className="text-cc-nav-label mt-3 font-mono text-[0.65rem] tracking-[0.15em] uppercase">
          {plan.brew}
        </p>
      </div>

      <Dots />

      <h3 className="font-heading text-cc-ink text-xl font-semibold">
        {plan.name}
      </h3>
      <p className="text-cc-nav-label mt-1 font-mono text-xs">{plan.tagline}</p>

      <div className="mt-5 flex items-baseline gap-2">
        <span className="font-heading text-cc-ink text-h3 font-semibold">
          {plan.price}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {plan.priceNote}
        </span>
      </div>

      <Dots />

      <ul className="flex flex-col gap-3">
        {plan.features.map((feature) => (
          <li key={feature} className="flex items-start gap-3">
            <span className="text-cc-accent mt-1 flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-prose text-sm">{feature}</span>
          </li>
        ))}
      </ul>

      <a
        href={plan.ctaHref}
        className={`mt-7 inline-flex w-full items-center justify-center rounded-xl px-6 py-3 text-sm font-medium no-underline transition-colors ${
          plan.popular
            ? "bg-[#3a7fc0] text-white hover:bg-[#4a8fd0]"
            : "text-cc-ink bg-[#2c3a55] hover:bg-[#37486a]"
        }`}
      >
        {plan.cta}
      </a>
    </div>
  );
}

/**
 * Nitro pricing: three plans framed as coffee brews (French Press, Pour-Over,
 * Chemex). The middle plan is highlighted as the popular pick. Cards stack on
 * small screens and sit side by side from large up.
 */
export function NitroPricing() {
  return (
    <section className="mx-auto max-w-6xl px-5 py-16 sm:px-12 sm:py-24">
      <h2 className="font-heading text-cc-ink text-h4 sm:text-h3 text-center font-semibold">
        Brew it your Way
      </h2>
      <p className="text-cc-prose mx-auto mt-5 max-w-3xl text-center text-base text-pretty sm:text-lg">
        Nitro is the Control Plane and CLI that keeps you in control, whether
        you&rsquo;re deploying a new schema, rolling out a new client, or
        gaining insights into your API environments.
      </p>

      <div className="mt-14 grid items-start gap-6 md:grid-cols-3">
        {PLANS.map((plan) => (
          <PlanCard key={plan.name} plan={plan} />
        ))}
      </div>
    </section>
  );
}
