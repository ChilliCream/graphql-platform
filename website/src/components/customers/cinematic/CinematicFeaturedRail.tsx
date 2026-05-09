"use client";

import Link from "next/link";
import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import {
  ActLabel,
  VibrantTile,
  type VibrantVariant,
} from "@/components/redesign-system/cinematic";
import { findIndustry } from "@/data/customers/industries";
import { STORIES } from "@/data/customers/stories";

import { CaseStudyCard } from "../CaseStudyCard";

// Section 03 (cinematic): the featured rail's top three slots are reframed
// as zine-style vibrant tiles — orange / yellow-rays / pink — turning the
// social-proof moment into the page's marketing peak. The remaining three
// featured stories keep the existing dark monogram CaseStudyCard treatment
// so the band still reads as a case-study exhibit, not a blog spread.
const FEATURED_TILE_VARIANTS: readonly VibrantVariant[] = [
  "orange",
  "yellow-rays",
  "pink",
];

export const CinematicFeaturedRail: FC = () => {
  const featured = STORIES.filter((s) => s.featured).slice(0, 6);
  const featuredTop = featured.slice(0, 3);
  const featuredRest = featured.slice(3);

  return (
    <Band variant="default" ariaLabel="Featured stories">
      <ActLabel n="02" name="Featured stories" />
      <div className="cc-cu-featured-inner">
        <div className="cc-cu-heading">
          <div className="eyebrow">Featured stories</div>
          <h2 className="display">The win first. The brand second.</h2>
          <p>
            Six platform teams. Six different stacks. Six metrics that paid for
            the rollout in the first quarter. Some named, some not — every one
            real and verified.
          </p>
        </div>
        <div className="cc-cu-cards-grid">
          {featuredTop.map((story, index) => {
            const industry = findIndustry(story.industry);
            const variant = FEATURED_TILE_VARIANTS[index];
            return (
              <Link
                key={story.slug}
                href={`/customers/${story.slug}`}
                className="cc-cu-vibrant-link"
                aria-label={`${story.displayName} — ${industry.label}`}
              >
                <VibrantTile
                  variant={variant}
                  tag={industry.label}
                  title={story.displayName}
                />
              </Link>
            );
          })}
          {featuredRest.map((story) => (
            <CaseStudyCard key={story.slug} story={story} />
          ))}
        </div>
      </div>
    </Band>
  );
};
