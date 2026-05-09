"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic/ActLabel";
import { InsetWindow } from "@/components/redesign-system/cinematic/InsetWindow";
import { productLabel } from "@/data/templates/filters";
import { findFeaturedTemplate } from "@/data/templates/templates";

import { TEMPLATE_THUMBNAILS, templateAccentVars } from "../TemplateCard";

// Cinematic hero: the featured template card is lifted into the homepage's
// `.cc-tabbar-h` chrome via `<InsetWindow>` so it reads as a special exhibit
// instead of an oversized sibling of the gallery cards. The tab strip carries
// the topology pill (FEDERATION, FUSION) and the FEATURED pill, the inset's
// title becomes the template title, and the viz slot hosts the existing
// per-template thumbnail SVG at exhibit scale.
//
// `<ActLabel n="01" name="TEMPLATES" />` sits in the band gutter; the legacy
// `.cc-section-label` is suppressed by TemplatesCinematicRoot.
export const TemplatesCinematicHero: FC = () => {
  const featured = findFeaturedTemplate();
  const Thumb = TEMPLATE_THUMBNAILS[featured.thumbnail];
  const accentStyle = templateAccentVars(featured);

  // Tab strip: the topology label (e.g. "FEDERATION (FUSION)") plus a
  // "FEATURED" pill, mirroring the homepage's product-tab vocabulary.
  const tabs = [
    { id: "featured", label: "Featured" },
    { id: "topology", label: "Federation (Fusion)" },
  ];

  // Bullets: the same product chip strip the gallery card carries at the
  // bottom, surfaced into the inset's footer so the exhibit still names its
  // product mix.
  const bullets = featured.products.slice(0, 4).map((p) => productLabel(p));

  return (
    <Band variant="glow" glowFrom="top-right" className="cc-tp-cinematic-band">
      <ActLabel n="01" name="Templates" />
      <div className="cc-tp-cinematic-hero-inner">
        <div className="cc-tp-cinematic-hero-copy">
          <h1 className="display">
            Start with a <span className="accent">template.</span>
          </h1>
          <p>
            Production-ready GraphQL services, federations, and clients. Clone,
            customize, ship.
          </p>
        </div>
        <InsetWindow
          tabs={tabs}
          activeTabId="featured"
          title={featured.title}
          body={featured.tagline}
          bullets={bullets}
          viz={
            <div
              className="cc-tp-cinematic-featured-viz"
              style={accentStyle}
              aria-hidden
            >
              <Thumb />
            </div>
          }
        />
      </div>
    </Band>
  );
};
