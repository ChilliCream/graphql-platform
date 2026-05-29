import { FAQ } from "@/src/data/pricing/faq";

// Three intent buckets over the flat FAQ data file. Indices reference the
// canonical ordering in `data/pricing/faq.ts`, so the grouping survives a
// single pass without rewriting the source list.
interface FaqGroup {
  readonly title: string;
  readonly indices: readonly number[];
}

const FAQ_GROUPS: readonly FaqGroup[] = [
  { title: "Billing & limits", indices: [3, 2, 4, 8] },
  { title: "Migration & flexibility", indices: [0, 1, 5, 6, 7] },
  { title: "Procurement", indices: [9] },
];

// Default-open the single most-asked question (index 3, "How is a request
// counted?").
const DEFAULT_OPEN_INDEX = 3;

function Chevron() {
  return (
    <svg
      viewBox="0 0 24 24"
      width="22"
      height="22"
      aria-hidden
      className="mt-1 size-[22px] shrink-0 text-[var(--cc-ink-dim)] transition-transform duration-200 group-open:rotate-180 group-open:text-fuchsia-400"
    >
      <path
        d="M6 9 L12 15 L18 9"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export function PricingFaq() {
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    mainEntity: FAQ.map(({ q, a }) => ({
      "@type": "Question",
      name: q,
      acceptedAnswer: {
        "@type": "Answer",
        text: a,
      },
    })),
  };

  return (
    <section className="py-16">
      <div className="mx-auto max-w-3xl">
        <div className="mb-12 text-center">
          <div className="mb-2 font-mono text-xs font-semibold uppercase tracking-widest text-[var(--cc-ink-dim)]">
            FAQ
          </div>
          <h2 className="text-3xl font-semibold tracking-tight text-[var(--cc-ink)] sm:text-4xl">
            Honest answers.
          </h2>
        </div>

        <div className="flex flex-col gap-9">
          {FAQ_GROUPS.map((group) => (
            <section key={group.title}>
              <h3 className="mb-3 font-mono text-[11px] font-medium uppercase tracking-[0.18em] text-fuchsia-400">
                {group.title}
              </h3>
              <div className="flex flex-col border-t border-[var(--cc-card-border)]">
                {group.indices.map((index) => {
                  const item = FAQ[index];
                  if (!item) {
                    return null;
                  }
                  return (
                    <details
                      key={item.q}
                      open={index === DEFAULT_OPEN_INDEX}
                      className="group border-b border-[var(--cc-card-border)]"
                    >
                      <summary className="flex cursor-pointer list-none items-start justify-between gap-6 px-1 py-6 text-base font-medium text-[var(--cc-ink)] transition-colors hover:text-fuchsia-400 group-open:text-fuchsia-400 sm:text-lg [&::-webkit-details-marker]:hidden">
                        <span className="w-9 shrink-0 pt-1 font-mono text-xs tracking-[0.14em] text-[var(--cc-ink-dim)]">
                          {String(index + 1).padStart(2, "0")}
                        </span>
                        <span className="flex-1 text-left">{item.q}</span>
                        <Chevron />
                      </summary>
                      <p className="mb-7 max-w-[70ch] pl-10 pr-1 text-base leading-relaxed text-[var(--cc-ink-dim)]">
                        {item.a}
                      </p>
                    </details>
                  );
                })}
              </div>
            </section>
          ))}
        </div>
      </div>

      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
    </section>
  );
}
