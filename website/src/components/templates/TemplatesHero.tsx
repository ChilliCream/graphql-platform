"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { TypographicMoment } from "@/components/redesign-system/illustrations/TypographicMoment";
import { findFeaturedTemplate } from "@/data/templates/templates";

import { FeaturedTemplateCard } from "./TemplateCard";

// Section 01: gallery hero. Two columns: H1 + sub on the left, an oversized
// real-template card on the right rendered at 1.4x the gallery scale. The
// card teaches the gallery pattern (here's what a template card looks like)
// and seeds the gallery (one click straight into the featured detail page).
//
// The "PRE-BREWED" eyebrow becomes a real typographic moment via
// TypographicMoment with the outline variant, lending the brand pun real
// visual weight instead of disappearing in a 12px pill. The hero sits inside
// a glow Band that picks up the page accent (coffee gradient on /templates).
export const TemplatesHero: FC = () => {
  const featured = findFeaturedTemplate();
  return (
    <Band variant="glow" glowFrom="top-right">
      <div className="cc-section-label">
        <span className="num">01</span> Templates
      </div>
      <div className="cc-tp-hero-inner">
        <div className="cc-tp-hero-copy">
          <TypographicMoment
            text="PRE-BREWED"
            variant="outline"
            size="medium"
            className="cc-tp-hero-stamp"
          />
          <h1 className="display">
            Start with a <span className="accent">template.</span>
          </h1>
          <p>
            Production-ready GraphQL services, federations, and clients. Clone,
            customize, ship.
          </p>
        </div>
        <div className="cc-tp-hero-featured">
          <FeaturedTemplateCard template={featured} />
        </div>
      </div>
    </Band>
  );
};
