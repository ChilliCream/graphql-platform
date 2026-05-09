"use client";

import React, { FC, forwardRef } from "react";

import { Band } from "@/components/redesign-system/Band";
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

interface NextStep {
  readonly key: string;
  readonly num: string;
  readonly title: string;
  readonly body: string;
}

const NEXT_STEPS: readonly NextStep[] = [
  {
    key: "discovery",
    num: "01",
    title: "Discovery call",
    body: "30 minutes with a solution architect, no decks. Map your stack, your blockers, and the metrics you'd need to greenlight a pilot.",
  },
  {
    key: "architecture",
    num: "02",
    title: "Architecture review",
    body: "We review your current federation/BFF/gateway design and return a written deployment plan tailored to your infra and compliance posture.",
  },
  {
    key: "pilot",
    num: "03",
    title: "Pilot SOW",
    body: "A scoped two-week paid pilot: schema audit, working Fusion mesh on one slice, rollout plan with rollback drill. One named architect, no handoff.",
  },
];

const PROMISES: readonly string[] = [
  "Reply within one business day",
  "30-min discovery with a solution architect",
  "Tailored demo + POC plan",
  "DPA, SOC 2 report, security questionnaire",
];

// Accent-band sales section. The "What happens next" 3-step strip sits
// directly above the form so the buyer knows what they're signing up for —
// reframes a transactional 4-field form as a consultative entry point.
export const InlineSalesForm = forwardRef<HTMLElement>(function InlineSalesForm(
  _,
  ref
) {
  return (
    <Band variant="accent" ariaLabel="Talk to sales">
      <section ref={ref} id="contact-form">
        <div className="cc-section-label">
          <span className="num">13</span> Talk to sales
        </div>

        <div className="cc-ent-next-head">
          <div className="eyebrow">What happens next</div>
          <h2 className="display">
            From form submission to pilot, in three steps.
          </h2>
        </div>
        <ol className="cc-ent-next-strip" aria-label="Next steps">
          {NEXT_STEPS.map((step) => (
            <li key={step.key} className="cc-ent-next-step">
              <span className="cc-ent-next-num">{step.num}</span>
              <h3 className="cc-ent-next-title">{step.title}</h3>
              <p className="cc-ent-next-body">{step.body}</p>
            </li>
          ))}
        </ol>

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
    </Band>
  );
});
