"use client";

import React, { FC, Suspense } from "react";
import styled from "styled-components";

import { GRID_TOKENS } from "@/components/redesign-system/grid";

import { TemplatesCtaStrip } from "../TemplatesCtaStrip";

import { TemplatesGridGallery } from "./TemplatesGridGallery";
import { TemplatesGridHero } from "./TemplatesGridHero";

// Top-level shell for the Grid variant of /templates. Wraps the page in the
// dark-navy background tokens already used by Default + Cinematic so chrome
// and per-page accent thread continue to read, but flips the section system
// to the strict square-bordered Grid primitives.
//
// The CTA strip at the bottom is shared with Default + Cinematic; the Grid
// variant only overrides the hero + gallery archetypes.
//
// Note: named `TemplatesGridVariant` to avoid colliding with the existing
// `TemplatesGrid` (the Default variant's gallery component).
export const TemplatesGridVariant: FC = () => {
  return (
    <Shell>
      <TemplatesGridHero />
      <Suspense fallback={null}>
        <TemplatesGridGallery />
      </Suspense>
      <TemplatesCtaStrip />
    </Shell>
  );
};

const Shell = styled.div`
  --cc-ink: #f5f1ea;
  --cc-ink-dim: rgba(245, 241, 234, 0.62);
  --cc-ink-faint: rgba(245, 241, 234, 0.16);
  --cc-grid-hairline: ${GRID_TOKENS.hairline};
  --cc-grid-hairline-strong: ${GRID_TOKENS.hairlineStrong};

  position: relative;
  width: 100%;
  color: var(--cc-ink);
  font-family: var(--cc-font-sans), system-ui, sans-serif;
  background: ${GRID_TOKENS.bgBase};

  * {
    box-sizing: border-box;
  }
`;
