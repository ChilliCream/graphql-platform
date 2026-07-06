export const token = {
  bg: "var(--t-bg)",
  surface: "var(--t-surface)",
  card: "var(--t-card)",
  border: "var(--t-border)",
  borderStrong: "var(--t-border-strong)",
  grid: "var(--t-grid)",
  skeleton: "var(--t-skeleton)",
  shimmer: "var(--t-shimmer)",
  text: "var(--t-text)",
  textSecondary: "var(--t-text-secondary)",
  textStrong: "var(--t-text-strong)",
  accent: "var(--t-accent)",
  accentHover: "var(--t-accent-hover)",
  success: "var(--t-success)",
  error: "var(--t-error)",
  warning: "var(--t-warning)",
  info: "var(--t-info)",
  active: "var(--t-active)",
  successText: "var(--t-success-text)",
  errorText: "var(--t-error-text)",
  cThroughput: "var(--t-c-throughput)",
  cLatency: "var(--t-c-latency)",
  cP95: "var(--t-c-p95)",
  cP99: "var(--t-c-p99)",
  cError: "var(--t-c-error)",
  cSuccess: "var(--t-c-success)",
  chThroughput: "var(--t-ch-throughput)",
  chLatency: "var(--t-ch-latency)",
  chP95: "var(--t-ch-p95)",
  chP99: "var(--t-ch-p99)",
  chImpact: "var(--t-ch-impact)",
  graphCanvas: "var(--t-graph-canvas)",
  graphNode: "var(--t-graph-node)",
  graphNodeSuccess: "var(--t-graph-node-success)",
  graphNodeFailure: "var(--t-graph-node-failure)",
  graphNodeWarning: "var(--t-graph-node-warning)",
  graphEdge: "var(--t-graph-edge)",
  graphEdgeActive: "var(--t-graph-edge-active)",
  graphDots: "var(--t-graph-dots)",
  synKeyword: "var(--t-syn-keyword)",
  synType: "var(--t-syn-type)",
  synField: "var(--t-syn-field)",
  synName: "var(--t-syn-name)",
  synString: "var(--t-syn-string)",
  synPunct: "var(--t-syn-punct)",
  synComment: "var(--t-syn-comment)",
  icQuery: "var(--t-ic-query)",
  icMutation: "var(--t-ic-mutation)",
  icObject: "var(--t-ic-object)",
  icScalar: "var(--t-ic-scalar)",
  icEnum: "var(--t-ic-enum)",
  icField: "var(--t-ic-field)",
  icInput: "var(--t-ic-input)",
  icGraphql: "var(--t-ic-graphql)",
  highlight: "var(--t-highlight)",
  textDim: "var(--t-text-dim)",
  purple: "var(--t-purple)",
  pink: "var(--t-pink)",
  blue: "var(--t-blue)",
  surprise: "var(--t-surprise)",
  severe: "var(--t-severe)",
  radius: "var(--t-radius)",
  shadow: "var(--t-shadow)",
  font: "var(--t-font)",
  fontHeading: "var(--t-font-heading)",
  mono: "var(--t-mono)",
} as const;

export type ThemeName = "dark" | "light";

export const CATEGORICAL = [
  "#349d87",
  "#0969da",
  "#ffc914",
  "#bf3989",
  "#8250df",
  "#ff1e0a",
  "#f2766b",
  "#4d93c8",
  "#bf8700",
  "#1ed994",
] as const;

export const IMPACT_STOPS = ["#1ed994", "#9acd3e", "#ffc914"] as const;

const DARK = {
  "--t-bg": "#0b0f1a",
  "--t-surface": "#0f1626",
  "--t-card": "#131c30",
  "--t-border": "#242d40",
  "--t-border-strong": "#36425c",
  "--t-grid": "#1a2233",
  "--t-skeleton": "#1b2436",
  "--t-shimmer": "rgba(245, 241, 234, 0.06)",
  "--t-text": "#cad5e2",
  "--t-text-secondary": "#8b95a9",
  "--t-text-strong": "#f5f0ea",
  "--t-accent": "#5eead4",
  "--t-accent-hover": "#99f6e4",
  "--t-success": "#2dd4bf",
  "--t-error": "#f0786a",
  "--t-warning": "#ffc914",
  "--t-info": "#58a6ff",
  "--t-active": "#bf3989",
  "--t-success-text": "#5eead4",
  "--t-error-text": "#f2766b",
  "--t-c-throughput": "#4d93c8",
  "--t-c-latency": "#5eead4",
  "--t-c-p95": "#bf3989",
  "--t-c-p99": "#8b8ff0",
  "--t-c-error": "#f0786a",
  "--t-c-success": "#2dd4bf",
  "--t-ch-throughput": "#3a9fe0",
  "--t-ch-latency": "#5eead4",
  "--t-ch-p95": "#d6488f",
  "--t-ch-p99": "#8b8ff0",
  "--t-ch-impact": "#ffc914",
  "--t-graph-canvas": "#0f1626",
  "--t-graph-node": "#131c30",
  "--t-graph-node-success": "#10241f",
  "--t-graph-node-failure": "#241620",
  "--t-graph-node-warning": "#241f10",
  "--t-graph-edge": "#36425c",
  "--t-graph-edge-active": "#f0786a",
  "--t-graph-dots": "#3a465c",
  "--t-syn-keyword": "#f97583",
  "--t-syn-type": "#b392f0",
  "--t-syn-field": "#ffab70",
  "--t-syn-name": "#3fb950",
  "--t-syn-string": "#79b8ff",
  "--t-syn-punct": "#cdd6e3",
  "--t-syn-comment": "#6a737d",
  "--t-ic-query": "#cdd6e3",
  "--t-ic-mutation": "#f0786a",
  "--t-ic-object": "#8250df",
  "--t-ic-scalar": "#58a6ff",
  "--t-ic-enum": "#d4a72c",
  "--t-ic-field": "#bf3989",
  "--t-ic-input": "#bc7a4c",
  "--t-ic-graphql": "#e10098",
  "--t-highlight": "#36425c",
  "--t-text-dim": "#62748e",
  "--t-purple": "#8250df",
  "--t-pink": "#bf3989",
  "--t-blue": "#58a6ff",
  "--t-surprise": "#5eead4",
  "--t-severe": "#f0a050",
} as const;

const LIGHT = {
  "--t-bg": "#f6f8fa",
  "--t-surface": "#ffffff",
  "--t-card": "#ffffff",
  "--t-border": "#d0d7de",
  "--t-border-strong": "#afb8c1",
  "--t-grid": "#eaeef2",
  "--t-shimmer": "rgba(17, 24, 39, 0.05)",
  "--t-text": "#24292f",
  "--t-text-secondary": "#57606a",
  "--t-text-strong": "#1f2328",
  "--t-accent": "#1a7f6b",
  "--t-accent-hover": "#0f766e",
  "--t-success": "#1a7f6b",
  "--t-error": "#cf222e",
  "--t-warning": "#9a6700",
  "--t-info": "#0969da",
  "--t-active": "#a01b6b",
  "--t-success-text": "#1a7f37",
  "--t-error-text": "#cf222e",
  "--t-c-throughput": "#2563a8",
  "--t-c-latency": "#1a7f6b",
  "--t-c-p95": "#a01b6b",
  "--t-c-p99": "#6e40c9",
  "--t-c-error": "#cf222e",
  "--t-c-success": "#1a7f6b",
  "--t-ch-throughput": "#0969da",
  "--t-ch-latency": "#1a7f6b",
  "--t-ch-p95": "#a01b6b",
  "--t-ch-p99": "#6e40c9",
  "--t-ch-impact": "#9a6700",
  "--t-graph-canvas": "#ffffff",
  "--t-graph-node": "#ffffff",
  "--t-graph-node-success": "#e6f4ea",
  "--t-graph-node-failure": "#fbe9e9",
  "--t-graph-node-warning": "#fdf2e0",
  "--t-graph-edge": "#afb8c1",
  "--t-graph-edge-active": "#d4663f",
  "--t-graph-dots": "#d0d7de",
  "--t-syn-keyword": "#cf222e",
  "--t-syn-type": "#6e40c9",
  "--t-syn-field": "#953800",
  "--t-syn-name": "#1a7f37",
  "--t-syn-string": "#0a3069",
  "--t-syn-punct": "#24292f",
  "--t-syn-comment": "#6e7781",
  "--t-ic-query": "#24292f",
  "--t-ic-mutation": "#cf222e",
  "--t-ic-object": "#8250df",
  "--t-ic-scalar": "#0969da",
  "--t-ic-enum": "#9a6700",
  "--t-ic-field": "#a01b6b",
  "--t-ic-input": "#bc4c00",
  "--t-ic-graphql": "#e10098",
  "--t-highlight": "#eaeef2",
  "--t-text-dim": "#8a94a0",
  "--t-purple": "#8250df",
  "--t-pink": "#bf3989",
  "--t-blue": "#0969da",
  "--t-surprise": "#1b7c83",
  "--t-severe": "#bc4c00",
} as const;

const SHARED = {
  "--t-radius": "6px",
  "--t-font":
    "-apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif",
  "--t-font-heading":
    "-apple-system, BlinkMacSystemFont, 'Inter', 'Segoe UI', Roboto, Helvetica, Arial, sans-serif",
  "--t-mono":
    "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
} as const;

const vars = (o: Record<string, string>) =>
  Object.entries(o)
    .map(([k, v]) => `  ${k}: ${v};`)
    .join("\n");

export const THEME_CSS = `
.nt-root {
${vars(SHARED)}
  --t-shadow: 0 1px 4px rgba(1, 4, 9, 0.5);
  color: var(--t-text);
  background: var(--t-bg);
  font-family: var(--t-font);
  font-size: 12px;
  line-height: 1.4;
  -webkit-font-smoothing: antialiased;
}
.nt-root[data-theme='dark'] {
${vars(DARK)}
  --t-shadow: 0 1px 3px rgba(2, 6, 16, 0.6);
}
.nt-root[data-theme='light'] {
${vars(LIGHT)}
  --t-shadow: 0 1px 3px rgba(17, 24, 39, 0.08);
}
.nt-root *,
.nt-root *::before,
.nt-root *::after {
  box-sizing: border-box;
}
`;
