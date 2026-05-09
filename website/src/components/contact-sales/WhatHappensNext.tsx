"use client";

import React, { FC } from "react";

interface Step {
  readonly key: string;
  readonly num: string;
  readonly title: string;
  readonly body: string;
}

const STEPS: readonly Step[] = [
  {
    key: "reply",
    num: "01",
    title: "We reply within one business day.",
    body: "An engineer reads your submission and responds with concrete next steps. No SDR triage, no calendar widget.",
  },
  {
    key: "discovery",
    num: "02",
    title: "30-minute discovery with a solution architect.",
    body: "Your stack, your scale, your regulatory constraints. We come prepared with a tailored agenda based on your interest.",
  },
  {
    key: "poc",
    num: "03",
    title: "Tailored demo + POC plan.",
    body: "A written deployment plan, a 30-day POC scope on a slice of your stack, and a price quote you can take to procurement.",
  },
];

export const WhatHappensNext: FC = () => {
  return (
    <section className="cc-cs-section cc-cs-next">
      <div className="cc-cs-next-inner">
        <div className="cc-cs-next-heading">
          <div className="eyebrow">What happens next</div>
          <h2 className="display">From submit to signed POC plan.</h2>
          <p>
            No phone tree. No SDR sequence. Three steps, one engineer, and a
            written plan you can take to your platform team.
          </p>
        </div>
        <div className="cc-cs-next-grid">
          {STEPS.map((step) => (
            <article key={step.key} className="cc-cs-step">
              <span className="cc-cs-step-num">{step.num}</span>
              <h3 className="cc-cs-step-title">{step.title}</h3>
              <p className="cc-cs-step-body">{step.body}</p>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
};
