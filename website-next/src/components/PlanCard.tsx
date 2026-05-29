import Link from "next/link";

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
  const isExternal = plan.ctaLink.startsWith("http");
  return (
    <div className="flex flex-col rounded-2xl border border-[var(--cc-card-border)] bg-[var(--cc-card-bg)] p-8 backdrop-blur-sm">
      <div className="mb-5 inline-flex w-fit rounded-full border border-[var(--cc-card-border)] px-4 py-1 font-mono text-xs uppercase tracking-widest text-[var(--cc-ink-dim)]">
        {plan.title}
      </div>
      <div className="flex items-baseline gap-2">
        {plan.price === "custom" ? (
          <span className="text-4xl font-semibold text-[var(--cc-ink)]">
            custom
          </span>
        ) : (
          <>
            {plan.fromPrice && (
              <span className="text-sm text-[var(--cc-ink-dim)]">from</span>
            )}
            <span className="text-4xl font-semibold text-[var(--cc-ink)]">
              ${plan.price.toLocaleString()}
            </span>
            {plan.period && (
              <span className="text-sm text-[var(--cc-ink-dim)]">
                /{plan.period}
              </span>
            )}
          </>
        )}
      </div>
      <p className="mt-3 text-sm text-[var(--cc-ink-dim)]">
        {plan.description}
      </p>
      <ul className="mt-6 flex flex-1 flex-col gap-2 text-sm text-[var(--cc-ink)]">
        {plan.features.map((feature) => (
          <li key={feature} className="flex items-start gap-2">
            <span aria-hidden className="mt-1 text-[var(--cc-accent)]">
              ✓
            </span>
            <span>{feature}</span>
          </li>
        ))}
      </ul>
      <Link
        href={plan.ctaLink}
        {...(isExternal
          ? { target: "_blank", rel: "noopener noreferrer" }
          : {})}
        className="mt-8 inline-flex items-center justify-center rounded-full bg-[var(--cc-ink)] px-6 py-2.5 text-sm font-medium text-[#0c1322] no-underline transition-colors hover:bg-white"
      >
        {plan.ctaText}
      </Link>
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
