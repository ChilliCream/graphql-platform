"use client";

import React, { CSSProperties, FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel, DottedGridBg } from "@/components/redesign-system/cinematic";
import { findIndustry } from "@/data/customers/industries";
import { TRUST_WALL_ALL } from "@/data/customers/stories";

// Section 03 (cinematic): trust wall grounded on a dotted-grid background,
// turning the long logo wall into a directory surface. The grid sits at
// `position: absolute; inset: 0; z-index: 0;` and the inner content lifts
// to z-index: 1 so the tiles render above. No connector lines through the
// wall — that would read as a Gantt chart instead of a directory.
export const CinematicIndustryTrustWall: FC = () => {
  return (
    <Band variant="inverted" ariaLabel="Trust wall">
      <ActLabel n="03" name="Trusted by" />
      <DottedGridBg density="lg" fade="both" />
      <div className="cc-cu-trust-inner cc-cu-trust-inner-cinematic">
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
