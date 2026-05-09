"use client";

import React, { FC } from "react";

// Section 03: bottom CTA. Vercel keeps submission off the index entirely
// (it's a docs flow). We mirror that: no loud "Submit a template" surface,
// just an issue link for the rare reader who'd otherwise email us.
export const TemplatesCtaStrip: FC = () => {
  return (
    <section className="cc-tp-section cc-tp-ctastrip">
      <div className="cc-tp-ctastrip-inner">
        <div className="cc-tp-ctastrip-text">
          <span className="eyebrow">Missing a shape?</span>
          <h2 className="display">Have a template idea?</h2>
        </div>
        <div className="cc-cta-row">
          <a
            href="https://github.com/ChilliCream/templates/issues/new"
            className="cc-btn cc-btn-ghost"
            rel="noopener"
          >
            Open an issue →
          </a>
          <a
            href="https://chillicream.com/docs"
            className="cc-btn cc-btn-primary"
          >
            Read the docs →
          </a>
        </div>
      </div>
    </section>
  );
};
