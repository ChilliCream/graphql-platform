"use client";

import React, { CSSProperties, FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { findIndustry } from "@/data/customers/industries";
import { TRUST_WALL_ALL } from "@/data/customers/stories";

// Section 04: trust wall as a single graze-able proof grid on an
// inverted band — the page's one full-bleed dark moment. Industry is a
// chip color, scale drives asymmetric span on desktop, and each tile
// carries a single fact instead of a monogram tile. No card edges; the
// inverted surface and a hairline foot do the separation work.
export const IndustryTrustWall: FC = () => {
  return (
    <Band variant="inverted" ariaLabel="Trust wall">
      <div className="cc-section-label">
        <span className="num">04</span> Trusted by
      </div>
      <div className="cc-cu-trust-inner">
        <div className="cc-cu-heading">
          <div className="eyebrow">Trusted by</div>
          <h2 className="display">Names where allowed. Sectors where not.</h2>
          <p>
            We can't always logo a customer. We can always tell you which sector
            they're in, what scale they run at, and what they replaced. Graze
            the wall — the chip color is the industry, the fact is the proof.
          </p>
        </div>

        <div className="cc-cu-trust-grid">
          {TRUST_WALL_ALL.map((tile) => {
            const industry = findIndustry(tile.industry);
            const tileStyle: CSSProperties = {
              ["--cc-trust-accent" as string]: industry.accentVar,
            };
            const scale = tile.scale ?? "sm";
            return (
              <article
                key={tile.key}
                className={[
                  "cc-cu-trust-tile",
                  `is-${scale}`,
                  tile.named ? "is-named" : "is-anonymous",
                ].join(" ")}
                style={tileStyle}
              >
                <span className="cc-cu-trust-chip">{industry.short}</span>
                <span className="cc-cu-trust-name">{tile.caption}</span>
                {tile.fact ? (
                  <span className="cc-cu-trust-fact">{tile.fact}</span>
                ) : null}
              </article>
            );
          })}
        </div>
      </div>
    </Band>
  );
};
