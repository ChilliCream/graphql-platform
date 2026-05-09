"use client";

import React, { FC } from "react";

import {
  DashboardComposite,
  Hemisphere,
} from "@/components/redesign-system/illustrations";

// Hero composite: full-bleed glow band, copy left, layered DashboardComposite
// bleeding off the right edge. The Hemisphere casts cyan ambient light from
// the right corner under the dashboard. NO card frame anywhere on the hero,
// the dashboard *is* the page background for the first viewport.

export const ObservabilityHero: FC = () => {
  return (
    <div className="cc-obs-hero-shell" aria-label="Observability hero">
      <div className="cc-obs-hero-ambient" aria-hidden>
        <Hemisphere side="right" />
      </div>
      <div className="cc-obs-hero-grid">
        <div className="cc-obs-hero-copy">
          <div className="cc-section-label">
            <span className="num">01</span> Observability
          </div>
          <div className="eyebrow">Nitro · Observability</div>
          <h1 className="display">
            Operator's <span className="accent">Window.</span>
          </h1>
          <p>
            One trace spans the gateway and every owning service. One control
            surface for staging and prod. The federation, finally legible.
          </p>
          <div className="cc-obs-hero-cta">
            <a href="/pricing" className="cc-btn cc-btn-primary">
              Start free →
            </a>
            <a
              href="mailto:contact@chillicream.com?subject=Nitro%20demo"
              className="cc-btn cc-btn-ghost"
            >
              Book a demo
            </a>
          </div>
        </div>
        <div className="cc-obs-hero-dashboard" aria-hidden>
          <DashboardComposite
            panels={["trace", "chart", "log-stream"]}
            bleedDirection="right"
          />
        </div>
      </div>
    </div>
  );
};
