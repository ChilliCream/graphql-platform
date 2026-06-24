/**
 * Nitro telemetry palette, RE-THEMED to harmonize with the ChilliCream website
 * (cc-* palette). The Act 2 scene illustrations are styled as cropped panels of
 * the Nitro app, but they live on the marketing page, so the surfaces are the
 * site's deep navy (#0b0f1a / #0c1322), headings are cream-white (#f5f0ea), body
 * text is cool-light, and the accent is the brand teal (#5eead4) — rather than
 * the GitHub-dark grey/teal of the standalone product. Data-viz series stay
 * distinguishable but the teal/coral families are pulled toward the brand.
 *
 * Mirrors the `--t-*` DARK theme in src/nitro/lib/tokens.ts, so the embedded
 * product screens and these illustrations share one palette. Keys are stable;
 * only values changed. Applied via inline `style={{}}`, scoped to these
 * illustration components; the rest of the site uses the cc-* Tailwind tokens.
 */
export const nitro = {
  // surfaces
  bg: "#0b0f1a",
  surface: "#0f1626",
  card: "#131c30",
  border: "#242d40",
  borderStrong: "#36425c",
  grid: "#1a2233",
  // text
  text: "#cad5e2",
  textSecondary: "#8b95a9",
  textStrong: "#f5f0ea",
  textDim: "#62748e",
  // intent
  accent: "#5eead4",
  accentHover: "#99f6e4",
  success: "#2dd4bf",
  successText: "#5eead4",
  error: "#f0786a",
  errorText: "#f2766b",
  warning: "#ffc914",
  info: "#58a6ff",
  active: "#bf3989",
  blue: "#58a6ff",
  purple: "#8250df",
  pink: "#bf3989",
  // semantic chart series (areas / lines)
  cThroughput: "#4d93c8",
  cLatency: "#5eead4",
  cP95: "#bf3989",
  cP99: "#8b8ff0",
  cError: "#f0786a",
  cSuccess: "#2dd4bf",
  // precise chart colors
  chThroughput: "#3a9fe0",
  chLatency: "#5eead4",
  chP95: "#d6488f",
  chP99: "#8b8ff0",
  chImpact: "#ffc914",
  // graph / topology (Fusion plan, Mocha topology)
  graphCanvas: "#0f1626",
  graphNode: "#131c30",
  graphNodeSuccess: "#10241f",
  graphNodeFailure: "#241620",
  graphNodeWarning: "#241f10",
  graphEdge: "#36425c",
  graphEdgeActive: "#f0786a",
  graphDots: "#3a465c",
  // GitHub-dark editor syntax tokens (read fine on the navy code surface)
  synKeyword: "#f97583",
  synType: "#b392f0",
  synField: "#ffab70",
  synName: "#3fb950",
  synString: "#79b8ff",
  synPunct: "#cdd6e3",
  synComment: "#6a737d",
  // schema type-kind icon colors
  icQuery: "#cdd6e3",
  icMutation: "#f0786a",
  icObject: "#8250df",
  icScalar: "#58a6ff",
  icEnum: "#d4a72c",
  icField: "#bf3989",
  icGraphql: "#e10098",
  // misc
  radius: "6px",
  font: "-apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif",
  mono: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
} as const;

/** Low -> high impact gradient stops (brand teal -> yellow), matching the Clients panel. */
export const NITRO_IMPACT_STOPS = ["#5eead4", "#9acd3e", "#ffc914"] as const;
