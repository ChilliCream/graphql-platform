import { PricingCalculator } from "./PricingCalculator";

export function PricingHero() {
  return (
    <div className="cc-pricing-hero">
      <div className="cc-pricing-hero-inner">
        <div className="eyebrow">Pricing</div>
        <h1 className="display">
          Pricing for <span className="accent">humans and agents.</span>
        </h1>
        <p>
          Open source, all the way up. Pay only for the parts you don&apos;t
          want to run.
        </p>

        <PricingCalculator />
      </div>
    </div>
  );
}
