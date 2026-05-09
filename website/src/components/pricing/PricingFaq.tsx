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
    <section className="cc-pricing-section cc-faq">
      <div className="cc-section-label">
        <span className="num">07</span> FAQ
      </div>
      <div className="cc-faq-inner">
        <div className="cc-faq-heading">
          <div className="eyebrow">FAQ</div>
          <h2 className="display">Honest answers.</h2>
        </div>

        <div className="cc-faq-list">
          {FAQ.map(({ q, a }, i) => (
            <details key={q} className="cc-faq-item">
              <summary>
                <span className="cc-faq-num">
                  {String(i + 1).padStart(2, "0")}
                </span>
                <span className="cc-faq-q">{q}</span>
                <Chevron />
              </summary>
              <p className="cc-faq-answer">{a}</p>
            </details>
          ))}
        </div>
      </div>

      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
    </section>
  );
};
