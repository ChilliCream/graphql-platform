"use client";

import React, { FC } from "react";

import { PILLARS, PillarKey } from "@/data/enterprise/pillars";

const stroke = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.6,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

// Stroke icons in the Act5 brewer vocabulary. Three pillars, three icons.
const PILLAR_ICONS: Record<PillarKey, React.ReactNode> = {
  federate: (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden {...stroke}>
      <circle cx="12" cy="5" r="2.2" />
      <circle cx="5" cy="18" r="2.2" />
      <circle cx="19" cy="18" r="2.2" />
      <circle cx="12" cy="12" r="2.2" />
      <line x1="12" y1="7.2" x2="12" y2="9.8" />
      <line x1="6.8" y1="16.4" x2="10.4" y2="13.4" />
      <line x1="17.2" y1="16.4" x2="13.6" y2="13.4" />
    </svg>
  ),
  operate: (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden {...stroke}>
      <rect x="4" y="6" width="16" height="4" rx="1.2" />
      <rect x="4" y="14" width="16" height="4" rx="1.2" />
      <circle cx="7" cy="8" r="0.8" />
      <circle cx="7" cy="16" r="0.8" />
      <line x1="11" y1="8" x2="17" y2="8" />
      <line x1="11" y1="16" x2="17" y2="16" />
    </svg>
  ),
  agents: (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden {...stroke}>
      <rect x="5" y="6" width="14" height="11" rx="2" />
      <circle cx="9.5" cy="11.5" r="1.1" />
      <circle cx="14.5" cy="11.5" r="1.1" />
      <line x1="12" y1="3" x2="12" y2="6" />
      <circle cx="12" cy="3" r="0.8" />
      <line x1="8.5" y1="17" x2="8.5" y2="20" />
      <line x1="15.5" y1="17" x2="15.5" y2="20" />
    </svg>
  ),
};

export const PlatformPillars: FC = () => {
  return (
    <section className="cc-ent-section cc-ent-pillars">
      <div className="cc-section-label">
        <span className="num">03</span> Platform pillars
      </div>
      <div className="cc-ent-pillars-inner">
        <div className="cc-ent-heading">
          <div className="eyebrow">What the platform does</div>
          <h2 className="display">
            One stack. Three jobs. No piecemeal vendors.
          </h2>
          <p>
            We don't ship a stitched-together suite. Federate, Operate, and
            Adopt agents are the three things a platform team needs to run
            GraphQL at scale, and they share one engine, one schema model, and
            one control plane.
          </p>
        </div>
        <div className="cc-ent-pillars-grid">
          {PILLARS.map((pillar) => (
            <article key={pillar.key} className="cc-ent-pillar">
              <div className="cc-ent-pillar-icon">
                {PILLAR_ICONS[pillar.key]}
              </div>
              <h3 className="cc-ent-pillar-title">{pillar.title}</h3>
              <p className="cc-ent-pillar-tagline">{pillar.tagline}</p>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
};
