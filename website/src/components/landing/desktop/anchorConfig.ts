// Shared per-lane styling for service-colored connectors.
//
// Geometry constants (CANVAS_W, HERO_W/H, HERO_LANES, PINCH_X/Y, ACT3_FULL_H,
// LANES.x) have been removed. Acts now publish fully-projected page-pixel
// anchors via the AnchorContext, and ConnectorLayer renders against the
// landing root's measured pixel viewBox. Only color metadata + the LaneKey
// type need to live here.
export const LANES = {
  catalog: { color: "var(--cc-col-cat)" },
  billing: { color: "var(--cc-col-bil)" },
  ordering: { color: "var(--cc-col-ord)" },
  shipping: { color: "var(--cc-col-shi)" },
  users: { color: "var(--cc-col-usr)" },
} as const;

export type LaneKey = keyof typeof LANES;
