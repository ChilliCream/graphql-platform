import type { PricingFaq as FaqEntry } from "@/src/components/pricing/pricingData";
import { FAQ } from "@/src/components/pricing/pricingData";

/**
 * The pricing FAQ as a single-column list of disclosure items. The padding lives
 * on the `<summary>`, so the entire header row (not just the text or the plus)
 * is the click target that toggles the answer.
 */
export function PricingFaq() {
  return (
    <section
      aria-labelledby="faq-heading"
      className="mt-24 scroll-mt-24 sm:mt-28"
      id="faq"
    >
      <div className="text-center">
        <p className="text-cc-ink-dim font-mono text-xs tracking-[0.18em] uppercase">
          FAQ
        </p>
        <h2
          id="faq-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Common questions
        </h2>
      </div>

      <div className="mx-auto mt-10 flex max-w-3xl flex-col gap-4">
        {FAQ.map((faq) => (
          <FaqItem key={faq.question} faq={faq} />
        ))}
      </div>
    </section>
  );
}

function FaqItem({ faq }: { readonly faq: FaqEntry }) {
  return (
    <details className="group border-cc-card-border hover:border-cc-card-border-hover bg-cc-card-bg/60 rounded-2xl border transition-colors">
      <summary className="text-cc-heading font-heading flex cursor-pointer list-none items-start justify-between gap-4 p-5 text-base font-semibold">
        <span>{faq.question}</span>
        <span
          aria-hidden="true"
          className="text-cc-accent mt-1 flex-none font-mono text-sm transition-transform group-open:rotate-45"
        >
          +
        </span>
      </summary>
      <div className="text-cc-ink px-5 pb-5 text-sm leading-relaxed">
        {faq.answer}
      </div>
    </details>
  );
}
