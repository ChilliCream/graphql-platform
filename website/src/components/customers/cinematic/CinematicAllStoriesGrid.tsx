"use client";

import React, { FC, useMemo, useState } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";
import { INDUSTRIES } from "@/data/customers/industries";
import {
  PRODUCTS,
  STORIES,
  STORY_TYPES,
  type ProductKey,
  type Story,
  type StoryType,
} from "@/data/customers/stories";

import { CaseStudyCard } from "../CaseStudyCard";

type FilterSet<T extends string> = ReadonlySet<T>;

interface ChipRowProps<T extends string> {
  readonly label: string;
  readonly options: readonly { key: T; label: string }[];
  readonly active: FilterSet<T>;
  readonly onToggle: (key: T) => void;
}

function ChipRow<T extends string>({
  label,
  options,
  active,
  onToggle,
}: ChipRowProps<T>): React.ReactElement {
  return (
    <div className="cc-cu-filter-row">
      <span className="cc-cu-filter-label">{label}</span>
      {options.map((opt) => (
        <button
          key={opt.key}
          type="button"
          className={`cc-cu-filter-chip${
            active.has(opt.key) ? " is-active" : ""
          }`}
          onClick={() => onToggle(opt.key)}
        >
          {opt.label}
        </button>
      ))}
    </div>
  );
}

// Section 04 (cinematic): same filterable all-stories grid as the default
// variant, with the gutter eyebrow upgraded to the shared `<ActLabel>` so
// the cinematic chapter run reads `01 CUSTOMERS · 02 FEATURED STORIES ·
// 03 TRUSTED BY · 04 ALL STORIES · 05 RESEARCH CALL`.
export const CinematicAllStoriesGrid: FC = () => {
  const [products, setProducts] = useState<FilterSet<ProductKey>>(
    () => new Set()
  );
  const [industries, setIndustries] = useState<FilterSet<string>>(
    () => new Set()
  );
  const [storyTypes, setStoryTypes] = useState<FilterSet<StoryType>>(
    () => new Set()
  );

  const toggle = <T extends string>(
    setter: React.Dispatch<React.SetStateAction<FilterSet<T>>>,
    key: T
  ): void => {
    setter((prev) => {
      const next = new Set(prev);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  };

  const visible = useMemo<readonly Story[]>(() => {
    return STORIES.filter((story) => {
      if (products.size > 0 && !story.products.some((p) => products.has(p))) {
        return false;
      }
      if (industries.size > 0 && !industries.has(story.industry)) {
        return false;
      }
      if (storyTypes.size > 0 && !storyTypes.has(story.storyType)) {
        return false;
      }
      return true;
    });
  }, [products, industries, storyTypes]);

  const industryOptions = INDUSTRIES.map((i) => ({
    key: i.key,
    label: i.short,
  }));

  return (
    <Band variant="default" ariaLabel="All stories">
      <ActLabel n="04" name="All stories" />
      <div className="cc-cu-grid-inner">
        <div className="cc-cu-heading is-flat">
          <div className="eyebrow">All stories</div>
          <h3 className="cc-cu-grid-section-title">Filter the long tail.</h3>
          <p>
            Eight stories so far, more on the way each quarter. Tag-filterable
            by product, industry, and story type. No search needed at this
            scale.
          </p>
        </div>

        <div className="cc-cu-filters">
          <ChipRow
            label="Product"
            options={
              PRODUCTS as unknown as { key: ProductKey; label: string }[]
            }
            active={products}
            onToggle={(k) => toggle(setProducts, k)}
          />
          <ChipRow
            label="Industry"
            options={industryOptions}
            active={industries}
            onToggle={(k) => toggle(setIndustries, k)}
          />
          <ChipRow
            label="Type"
            options={
              STORY_TYPES as unknown as { key: StoryType; label: string }[]
            }
            active={storyTypes}
            onToggle={(k) => toggle(setStoryTypes, k)}
          />
          <div className="cc-cu-filter-row">
            <span className="cc-cu-filter-count">
              Showing {visible.length} of {STORIES.length}
            </span>
          </div>
        </div>

        <div className="cc-cu-cards-grid">
          {visible.map((story) => (
            <CaseStudyCard key={story.slug} story={story} />
          ))}
        </div>
      </div>
    </Band>
  );
};
