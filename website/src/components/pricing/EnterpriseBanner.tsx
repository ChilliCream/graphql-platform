"use client";

import React from "react";

const BULLETS = [
  "Dedicated solution architect",
  "24x7 oncall rotation",
  "Custom uptime SLA (99.99%+)",
  "Federation governance + policies",
  "SOC 2 Type II + ISO 27001",
  "DPA, subprocessor list, security review",
];

const Check: React.FC = () => (
  <svg viewBox="0 0 16 16" width="14" height="14" aria-hidden>
    <path
      d="M3 8.5 L6.5 12 L13 4.5"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </svg>
);

export const EnterpriseBanner: React.FC = () => {
  return (
    <section className="cc-pricing-section cc-enterprise">
      <div className="cc-enterprise-inner">
        <div className="cc-enterprise-card">
          <div className="cc-enterprise-copy">
            <div className="eyebrow">Enterprise + Support</div>
            <h2 className="display">
              Running Fusion in production? Let's talk.
            </h2>
            <p>
              Enterprise wraps Nitro Self-Hosted with a dedicated solution
              architect, 24x7 oncall, custom SLA, and procurement-ready
              compliance evidence. We sign your DPA, answer your questionnaire,
              and stay on the call when something breaks.
            </p>
            <a
              href="mailto:contact@chillicream.com?subject=Enterprise"
              className="cc-btn cc-btn-primary"
            >
              Talk to sales →
            </a>
          </div>
          <ul className="cc-enterprise-bullets">
            {BULLETS.map((b) => (
              <li key={b}>
                <Check />
                <span>{b}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
};
