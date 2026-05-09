"use client";

import styled from "styled-components";

import { GRID_TOKENS } from "@/components/redesign-system/grid";

// Page-level shell for the Grid variant of /integrations. Same shape as
// PricingGridRoot: surface the GRID_TOKENS palette as CSS custom properties so
// nested Grid primitives inherit a single set of values, set a flat dark-navy
// canvas (no gradients, no chrome), and lock the box-sizing default.
//
// The Grid variant is the third sibling of Default and Cinematic, a strict
// hairline-bordered translation of the same data the other two render. Keep
// the chrome monochrome: only the `<AccentThread page="integrations">`
// cascade surfaces the page identity color (in the eyebrow, the active type
// chip, and the trailing arrow chevrons).
export const IntegrationsGridRoot = styled.div`
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
