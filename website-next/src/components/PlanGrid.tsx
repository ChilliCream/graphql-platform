import { SolidButton } from "@/src/design-system/Button";

import { CheckIcon } from "./CheckIcon";

export interface Plan {
  title: string;
  price: number | "custom";
  period?: string;
  fromPrice?: boolean;
  description: string;
  features: string[];
  ctaText: string;
  ctaLink: string;
}

export function PlanCard({ plan }: { plan: Plan }) {
  return (
    <div className="flex flex-col rounded-2xl border border-cc-card-border bg-cc-card-bg p-8 backdrop-blur-sm">
      <div className="mb-5 inline-flex w-fit rounded-full border border-cc-card-border px-4 py-1 font-mono text-xs uppercase tracking-widest text-cc-ink-dim">
        {plan.title}
      </div>

      <div className="flex items-baseline gap-2">
        {plan.price === "custom" ? (
          <span className="text-4xl font-semibold text-cc-ink">custom</span>
        ) : (
          <>
            {plan.fromPrice && (
              <span className="text-sm text-cc-ink-dim">from</span>
            )}
            <span className="text-4xl font-semibold text-cc-ink">
              ${plan.price.toLocaleString()}
            </span>
            {plan.period && (
              <span className="text-sm text-cc-ink-dim">/{plan.period}</span>
            )}
          </>
        )}
      </div>

      <p className="mt-3 text-sm text-cc-ink-dim">{plan.description}</p>

      <ul className="mt-6 flex flex-1 flex-col gap-2 text-sm text-cc-ink">
        {plan.features.map((feature) => (
          <li key={feature} className="flex items-start gap-2">
            <span className="flex h-5 flex-none items-center text-cc-accent">
              <CheckIcon />
            </span>
            <span>{feature}</span>
          </li>
        ))}
      </ul>

      <SolidButton href={plan.ctaLink} className="mt-8">
        {plan.ctaText}
      </SolidButton>
    </div>
  );
}

export function PlanGrid({ plans }: { plans: Plan[] }) {
  return (
    <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
      {plans.map((plan) => (
        <PlanCard key={plan.title} plan={plan} />
      ))}
    </div>
  );
}
