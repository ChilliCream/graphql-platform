"use client";

import React, { FC, useState } from "react";

import { findIndustry, INDUSTRIES } from "@/data/customers/industries";
import { TRUST_WALL } from "@/data/customers/stories";

import { AnonymousMonogram } from "./AnonymousMonogram";

// Section 04: tabbed industry trust wall. The active tab is underlined in
// --cc-ink. Each tab shows ~12 monogram tiles, with named brands rendered
// as cream-on-glass and anonymous tiles using the industry-accented stroke
// monogram. The honesty of the mix is the point.
export const IndustryTrustWall: FC = () => {
  const [activeKey, setActiveKey] = useState(INDUSTRIES[0].key);
  const activeIndustry = findIndustry(activeKey);
  const tiles = TRUST_WALL[activeKey] ?? [];

  return (
    <section className="cc-cu-section cc-cu-trust">
      <div className="cc-section-label">
        <span className="num">04</span> Trusted by
      </div>
      <div className="cc-cu-trust-inner">
        <div className="cc-cu-heading">
          <div className="eyebrow">Trusted by</div>
          <h2 className="display">Names where allowed. Sectors where not.</h2>
          <p>
            We can't always logo a customer. We can always tell you which sector
            they're in, what scale they run at, and what they replaced.
          </p>
        </div>

        <div
          className="cc-cu-trust-tabs"
          role="tablist"
          aria-label="Industries"
        >
          {INDUSTRIES.map((industry) => (
            <button
              key={industry.key}
              type="button"
              role="tab"
              aria-selected={activeKey === industry.key}
              className={`cc-cu-trust-tab${
                activeKey === industry.key ? " is-active" : ""
              }`}
              onClick={() => setActiveKey(industry.key)}
            >
              {industry.label}
            </button>
          ))}
        </div>

        <div className="cc-cu-trust-grid" role="tabpanel">
          {tiles.map((tile) => (
            <div
              key={tile.key}
              className={`cc-cu-trust-tile${tile.named ? " is-named" : ""}`}
            >
              <div className="cc-cu-trust-mono">
                {tile.named ? (
                  tile.monogram
                ) : (
                  <AnonymousMonogram
                    industry={activeIndustry}
                    size={42}
                    title={tile.caption}
                  />
                )}
              </div>
              <div className="cc-cu-trust-caption">{tile.caption}</div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};
