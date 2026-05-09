"use client";

import React from "react";

import { FAQ } from "@/data/pricing/faq";

const Chevron: React.FC = () => (
  <svg
    viewBox="0 0 24 24"
    width="22"
    height="22"
    aria-hidden
    className="cc-faq-chevron"
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
// counted?"). One open accordion is the right starting state for a pricing
// FAQ where the reader is hunting for billing semantics.
const DEFAULT_OPEN_INDEX = 3;

export const PricingFaq: React.FC = () => {
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
    <div className="cc-faq">
      <div className="cc-section-label">
        <span className="num">06</span> FAQ
      </div>
      <div className="cc-faq-inner">
        <div className="cc-faq-heading">
          <div className="eyebrow">FAQ</div>
          <h2 className="display">Honest answers.</h2>
        </div>

        <div className="cc-faq-groups">
          {FAQ_GROUPS.map((group) => (
            <section key={group.title} className="cc-faq-group">
              <h3 className="cc-faq-group-title">{group.title}</h3>
              <div className="cc-faq-list">
                {group.indices.map((index) => {
                  const item = FAQ[index];
                  if (!item) {
                    return null;
                  }
                  return (
                    <details
                      key={item.q}
                      className="cc-faq-item"
                      open={index === DEFAULT_OPEN_INDEX}
                    >
                      <summary>
                        <span className="cc-faq-num">
                          {String(index + 1).padStart(2, "0")}
                        </span>
                        <span className="cc-faq-q">{item.q}</span>
                        <Chevron />
                      </summary>
                      <p className="cc-faq-answer">{item.a}</p>
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
    </div>
  );
};
