"use client";

import React, { FC, forwardRef } from "react";

import { SalesFormFields } from "./SalesFormFields";

const Check: FC = () => (
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

const PROMISES: readonly string[] = [
  "Reply within one business day",
  "30-min discovery with a solution architect",
  "Tailored demo + POC plan",
  "DPA, SOC 2 report, security questionnaire",
];

export const InlineSalesForm = forwardRef<HTMLElement>(function InlineSalesForm(
  _,
  ref
) {
  return (
    <section ref={ref} id="contact-form" className="cc-ent-section cc-ent-form">
      <div className="cc-section-label">
        <span className="num">13</span> Talk to sales
      </div>
      <div className="cc-ent-form-inner">
        <div className="cc-ent-form-card">
          <div className="cc-ent-form-copy">
            <div className="eyebrow">Talk to a solution architect</div>
            <h2 className="display">
              Tell us about your stack. We'll come back with a plan.
            </h2>
            <p>
              Four fields, one engineer on the other end of the line. We'll
              reply with a deployment plan, a POC scope, and pricing — within
              one business day.
            </p>
            <ul className="cc-ent-form-bullets">
              {PROMISES.map((p) => (
                <li key={p}>
                  <Check />
                  <span>{p}</span>
                </li>
              ))}
            </ul>
          </div>
          <div>
            <SalesFormFields variant="inline" />
          </div>
        </div>
      </div>
    </section>
  );
});
