"use client";

import React, { FC } from "react";

export const ContactSalesHero: FC = () => {
  return (
    <section className="cc-cs-section cc-cs-hero">
      <div className="cc-section-label">
        <span className="num">01</span> Contact sales
      </div>
      <div className="cc-cs-hero-inner">
        <div className="eyebrow">Talk to our team</div>
        <h1 className="display">
          Talk to <span className="accent">our team.</span>
        </h1>
        <p>
          Get a custom demo, walk through your deployment fit (cloud,
          self-hosted, or air-gapped), and leave with a written POC plan and
          pricing.
        </p>
      </div>
    </section>
  );
};
