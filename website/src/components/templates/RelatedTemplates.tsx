"use client";

import React, { FC } from "react";

import type { Template } from "@/data/templates/templates";

import { TemplateCard } from "./TemplateCard";

interface RelatedTemplatesProps {
  readonly templates: readonly Template[];
}

// Bottom-of-detail-page strip of related templates. Always 3, computed by
// findRelated() in the data module: same topology first, then product-mix
// overlap, then anything. Same vocabulary as the gallery card so the
// detail page closes the loop back into the index.
export const RelatedTemplates: FC<RelatedTemplatesProps> = ({ templates }) => {
  if (templates.length === 0) {
    return null;
  }
  return (
    <section className="cc-tpd-section cc-tpd-related">
      <div className="cc-tpd-related-inner">
        <div className="cc-tpd-related-heading">
          <div className="eyebrow">More templates</div>
          <h2 className="display">Templates like this one.</h2>
        </div>
        <div className="cc-tp-grid">
          {templates.map((t) => (
            <TemplateCard key={t.slug} template={t} />
          ))}
        </div>
      </div>
    </section>
  );
};
