"use client";

import React, { FC } from "react";

// Section 01: gallery hero. Restrained typography only. Vercel's Templates
// hero is functional, not aspirational ("Find your Template."), and ours
// follows suit: the hero exists to label the page, the gallery is the
// actual product. The kicker eyebrow PRE-BREWED keeps the drink-pun running
// without burdening the H1, and the noun "template" picks up the same
// drink-themed gradient (cool blue → violet → warm amber) used by the
// pricing, customers, and enterprise heroes.
export const TemplatesHero: FC = () => {
  return (
    <section className="cc-tp-section cc-tp-hero">
      <div className="cc-section-label">
        <span className="num">01</span> Templates
      </div>
      <div className="cc-tp-hero-inner">
        <div className="kicker">
          <span className="eyebrow">Pre-brewed</span>
        </div>
        <h1 className="display">
          Start with a <span className="accent">template.</span>
        </h1>
        <p>
          Production-ready GraphQL services, federations, and clients. Clone,
          customize, ship.
        </p>
      </div>
    </section>
  );
};
