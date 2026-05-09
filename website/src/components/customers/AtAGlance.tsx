"use client";

import React, { FC } from "react";

import {
  productLabel,
  type AtAGlance as AtAGlanceData,
  type StoryMetric,
} from "@/data/customers/stories";

interface AtAGlanceProps {
  readonly data: AtAGlanceData;
  readonly keyMetrics: readonly StoryMetric[];
}

// Sidebar block on the detail page. Architects vet stories partly by
// similarity — same industry, similar scale, recognizable stack neighbors.
// The sidebar is the architect's vetting tool. Sticky on desktop, stacked
// on mobile. The 3 key metrics get the gradient accent treatment.
export const AtAGlance: FC<AtAGlanceProps> = ({ data, keyMetrics }) => {
  return (
    <aside className="cc-csd-sidebar" aria-label="At a glance">
      <div className="cc-csd-sidebar-title">At a glance</div>

      <div className="cc-csd-sidebar-row">
        <span className="label">Industry</span>
        <span className="value">{data.industry}</span>
      </div>
      <div className="cc-csd-sidebar-row">
        <span className="label">Scale</span>
        <span className="value">{data.scale}</span>
      </div>
      <div className="cc-csd-sidebar-row">
        <span className="label">Region</span>
        <span className="value">{data.region}</span>
      </div>
      <div className="cc-csd-sidebar-row">
        <span className="label">Products used</span>
        <div className="cc-csd-sidebar-chips">
          {data.products.map((p) => (
            <span key={p} className="cc-csd-sidebar-chip">
              {productLabel(p)}
            </span>
          ))}
        </div>
      </div>
      <div className="cc-csd-sidebar-row">
        <span className="label">Stack neighbors</span>
        <div className="cc-csd-sidebar-chips">
          {data.stack.map((s) => (
            <span key={s} className="cc-csd-sidebar-chip">
              {s}
            </span>
          ))}
        </div>
      </div>
      <div className="cc-csd-sidebar-row">
        <span className="label">Live since</span>
        <span className="value">{data.liveSince}</span>
      </div>

      <div className="cc-csd-sidebar-divider" aria-hidden />

      <div className="cc-csd-sidebar-title">3 key metrics</div>
      <div className="cc-csd-sidebar-metrics">
        {keyMetrics.map((m, i) => (
          <div key={i} className="cc-csd-sidebar-metric">
            <div className="value">{m.value}</div>
            <div className="label">{m.label}</div>
          </div>
        ))}
      </div>

      <a
        href="/contact/sales?interest=reference"
        className="cc-csd-sidebar-cta"
      >
        Book a reference call →
      </a>
    </aside>
  );
};
