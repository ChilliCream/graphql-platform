"use client";

import styled from "styled-components";

import { GRID_TOKENS } from "@/components/redesign-system/grid";

// PricingGridRoot owns the page-level dark canvas, surfaces the Grid token
// palette as CSS custom properties, and tunes a small number of body / link
// defaults so deeply nested grid primitives don't have to redeclare them.
//
// The Grid variant is a flat, hairline-bordered translation of the same data
// the Default and Cinematic variants render. Keep the chrome monochrome:
// only the per-page accent thread surfaces the page identity color (in the
// eyebrow, the active-tab underline, and the arrow chevrons).
export const PricingGridRoot = styled.div`
  --cc-grid-bg: ${GRID_TOKENS.bgBase};
  --cc-grid-card-bg: ${GRID_TOKENS.bgCard};
  --cc-grid-card-bg-inverted: ${GRID_TOKENS.bgInverted};
  --cc-grid-card-hover: ${GRID_TOKENS.bgHover};
  --cc-grid-hairline: ${GRID_TOKENS.hairline};
  --cc-grid-hairline-strong: ${GRID_TOKENS.hairlineStrong};
  --cc-grid-ink: ${GRID_TOKENS.inkPrimary};
  --cc-grid-ink-body: ${GRID_TOKENS.inkBody};
  --cc-grid-ink-muted: ${GRID_TOKENS.inkMuted};
  --cc-grid-ink-faint: ${GRID_TOKENS.inkFaint};
  --cc-grid-success: ${GRID_TOKENS.success};
  --cc-grid-warning: ${GRID_TOKENS.warning};
  --cc-grid-danger: ${GRID_TOKENS.danger};
  --cc-pad-x: ${GRID_TOKENS.pageGutter};

  position: relative;
  width: 100%;
  background: ${GRID_TOKENS.bgBase};
  color: ${GRID_TOKENS.inkPrimary};
  font-family: var(--cc-font-sans), system-ui, sans-serif;

  * {
    box-sizing: border-box;
  }
`;
