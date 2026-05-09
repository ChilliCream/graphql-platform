"use client";

import React, { FC } from "react";

interface RoiTile {
  readonly key: string;
  readonly value: string;
  readonly accent: string;
  readonly caption: string;
}

const ROI_TILES: readonly RoiTile[] = [
  {
    key: "consolidated",
    value: "47",
    accent: "BFFs",
    caption:
      "Consolidated into one Fusion mesh by a top-5 European retail bank. One on-call rotation, one schema, one tracing story.",
  },
  {
    key: "rollout",
    value: "9",
    accent: "weeks",
    caption:
      "End-to-end federation rollout for an 18-service FSI group, including audit sign-off and rollback drills.",
  },
  {
    key: "p99",
    value: "480→90ms",
    accent: "p99",
    caption:
      "P99 reduction after replacing a stack of hand-rolled BFFs with one Fusion gateway over a single quarter.",
  },
];

export const PlatformTeamRoi: FC = () => {
  return (
    <section className="cc-ent-section cc-ent-roi">
      <div className="cc-section-label">
        <span className="num">05</span> Customer outcomes
      </div>
      <div className="cc-ent-roi-inner">
        <div className="cc-ent-heading">
          <div className="eyebrow">Customer outcomes</div>
          <h2 className="display">
            Real numbers from production platform teams.
          </h2>
          <p>
            We don't quote headline ROI percentages until we have a third-party
            study to back them up. Here's what we can publish now: the
            consolidation, the rollout time, and the latency move from teams
            running Fusion in production.
          </p>
        </div>
        <div className="cc-ent-roi-grid">
          {ROI_TILES.map((t) => (
            <div key={t.key} className="cc-ent-roi-tile">
              <p className="cc-ent-roi-num">
                {t.value} <span className="accent">{t.accent}</span>
              </p>
              <p className="cc-ent-roi-caption">{t.caption}</p>
            </div>
          ))}
        </div>
        <p className="cc-ent-roi-note">
          Each metric is approved for publication by the customer. Names and
          industry segments are anonymised; the numbers are not.
        </p>
      </div>
    </section>
  );
};
