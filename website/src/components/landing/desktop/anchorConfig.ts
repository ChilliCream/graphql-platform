// Shared geometry constants used across acts and the ConnectorLayer.
// All x positions are in canvas-coordinate space (W = 1480) so anchors
// from different acts can be combined into a single SVG.
export const CANVAS_W = 1480;

// Service lane x positions in canvas coords. These match the pill centers
// computed in Act 2 / Act 3 (PILL_W = 220, GAP = 14, centered).
export const LANES = {
  catalog: { x: 272, color: "var(--cc-col-cat)" },
  billing: { x: 506, color: "var(--cc-col-bil)" },
  ordering: { x: 740, color: "var(--cc-col-ord)" },
  shipping: { x: 974, color: "var(--cc-col-shi)" },
  users: { x: 1208, color: "var(--cc-col-usr)" },
} as const;

export type LaneKey = keyof typeof LANES;

// Hero pour exit lanes — fractions of HERO_W (1000). Used by Act 1 to
// compute the bottom-of-canvas exit x for each cup pour.
export const HERO_LANES = [0.22, 0.4, 0.58, 0.76] as const;

// Hero canvas dimensions — Act 1 renders into a 1000x760 viewBox, but the
// canvas-coord container is 1480 wide. Hero exits land at HERO_LANES * 1480.
export const HERO_W = 1000;
export const HERO_H = 760;

// Act 3 pinch geometry — used by both Act 3 (for the halo + dot) and the
// ConnectorLayer (for funnel and post-pinch path).
export const PINCH_X = LANES.catalog.x; // catalog lane
export const PINCH_Y_REL = 648; // y inside Act 3's local SVG
export const ACT3_FULL_H = 1000;
