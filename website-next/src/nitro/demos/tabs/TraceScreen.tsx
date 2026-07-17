/**
 * TraceScreen — Tab 2: "Observe" (the monitoring flagship). Sells "watch your gateway, then
 * root-cause a slow request in seconds." It clones the REAL Nitro monitoring screens as ONE tall
 * Monitoring page the view SCROLLS DOWN through, section by section, as a developer narrows from
 * the gateway dashboard to one slow database span.
 *
 * Built by eye from the two reference screenshots (src/demos/monitoring/image copy*.png) and the
 * real components under cloud/src/App/src/app/api/metrics + .../monitoring/trace-sample.
 *
 * SECTION A — MONITORING OVERVIEW (clone image copy.png): under the gateway chrome (doc tab +
 *   view-nav with Monitoring active + Production stage + "Last 7 days") and a "Production Stage"
 *   sub-header, a tile grid —
 *     · Row 1: Latency, FULL WIDTH — a multi-series (min/avg/max) spiky LINE chart + legend.
 *     · Row 2: Throughput (~62%) blue AREA chart + Checks (~38%) horizontal BAR chart.
 *     · Row 3: Failed Operations (~62%) one red line (GetHomePageQuery) + Errors (~38%) an empty
 *       "No Metrics Available" state.
 *     · Row 4: Insights, FULL WIDTH — the operations table (Operation / Latency / Throughput /
 *       Error Rate / Impact, sorted by impact, mini sparklines + impact bars).
 *   The cursor scans the Insights rows, then clicks the slow outlier GetHomePageQuery
 *   (data-testid="slow-op-row") → a short load.
 *
 * SECTION B — OPERATION SCREEN (clone image copy 2.png):
 *     · Row 1: three charts — Latency (line) / Throughput (area) / Errors (line).
 *     · Row 2: Latency Distribution, FULL WIDTH — a HISTOGRAM (log-ish x; "Click and drag to
 *       select a range") with median/p90/p95/p99 markers.
 *     · Row 3: Trace Sample · 1 of N (Timeline | Logs tabs) → the distributed-trace WATERFALL
 *       builds rank by rank (POST /graphql → parse/validate → parallel subgraph fetches → the slow
 *       db.query span). The cursor drills the slowest span (data-testid="db-span") → the SPAN
 *       DETAILS flyout (data-testid="reel-flyout") slides in with the literal slow SQL, and dwells.
 *
 * All motion derives from a STAGE-BASED timeline (`src/lib/timeline.ts`): each beat owns its OWN
 * duration in ms and the total (`TRACE_MS`) is DERIVED by summing them. The screen consumes a
 * normalized `progress` MotionValue; at progress=1 (reduced motion) it sits in the fully-resolved
 * payoff: flyout open, SQL visible, amber span selected.
 */
import { useState } from "react";
import {
  motion,
  useMotionValueEvent,
  useTransform,
  type MotionValue,
} from "motion/react";
import { Stage } from "../../primitives/reel/Stage";
import { AppFrame } from "../../primitives/reel/AppFrame";
import { Cursor } from "../../primitives/reel/Cursor";
import { Flyout } from "../../primitives/reel/Flyout";
import { TableList } from "../../primitives/reel/TableList";
import { TABREEL_CANVAS } from "../../primitives/reel/TabReel";
import { token } from "../../lib/tokens";
import { ease } from "../../lib/motion";
import { timeline } from "../../lib/timeline";
import { smoothSeries } from "../../lib/data/tabs";
import {
  smoothLinePath,
  areaFromLine,
  logScale,
  compact,
  type Pt,
} from "../../lib/scale";
import { CodeBlock } from "../../primitives/CodeBlock";
import {
  IconDatabase,
  IconChevronDown,
  IconCalendar,
  IconSpinner,
  IconSearch,
  IconServer,
  IconHttp,
  IconGraphql,
  IconInternal,
  IconQuery,
  IconLock,
  IconSettings,
  IconWarning,
  IconErrorCircle,
  IconInfo,
} from "../../primitives/icons";

const W = TABREEL_CANVAS.w;
const H = TABREEL_CANVAS.h;

// Nitro IDE active-accent (tab underlines / rail) is orange.
const ORANGE = token.graphEdgeActive;

// Exact Nitro github-dark chart palette (cloud .../themes: chart-palette-0..3 map to the span
// legend HTTP / GraphQL / Internal / DB and to the metric series colors).
const PAL = {
  http: token.chThroughput, // #0288d1 blue
  graphql: token.chP95, // #c2095a pink/red
  internal: token.chLatency, // #3bceac teal/green
  db: token.chImpact, // #ffc914 amber
};

/* ── LOCAL data (kept out of src/lib/data/tabs.ts per task constraints) ──────────── */

type SpanKind = "server" | "graphql" | "internal" | "http" | "db";

interface Span {
  id: string;
  name: string;
  kind: SpanKind;
  depth: number;
  startMs: number;
  durationMs: number;
  /** 0-based rank used to reveal the waterfall depth by depth */
  rank: number;
  hasChildren?: boolean;
  target?: boolean;
  /** the parent's child glyph sits at this depth's indent */
}

// span colors map to the real Transaction legend (HTTP / GraphQL / Internal / DB palette slots).
const KIND_COLOR: Record<SpanKind, string> = {
  server: PAL.http,
  http: PAL.http,
  graphql: PAL.graphql,
  internal: PAL.internal,
  db: PAL.db,
};

const KIND_ICON: Record<SpanKind, (size: number) => React.ReactNode> = {
  server: (s) => <IconServer size={s} />,
  graphql: (s) => <IconGraphql size={s} />,
  internal: (s) => <IconInternal size={s} />,
  http: (s) => <IconHttp size={s} />,
  db: (s) => <IconDatabase size={s} />,
};

const TRACE = {
  operation: "query GetHomePageQuery",
  totalMs: 842.6,
  timeAgo: "2 minutes ago",
  traceId: "4e2d9f7a6c1b08e3",
  sampleOf: 24,
  // captured waterfall (matches the real OpenTelemetry span tree shape)
  spans: [
    {
      id: "s0",
      name: "POST /graphql",
      kind: "server",
      depth: 0,
      startMs: 0,
      durationMs: 842.6,
      rank: 0,
      hasChildren: true,
    },
    {
      id: "s1",
      name: "query GetHomePageQuery",
      kind: "graphql",
      depth: 1,
      startMs: 12,
      durationMs: 818,
      rank: 1,
      hasChildren: true,
    },
    {
      id: "s2",
      name: "Parse Request",
      kind: "internal",
      depth: 2,
      startMs: 15,
      durationMs: 0.9,
      rank: 2,
    },
    {
      id: "s3",
      name: "Validate Request",
      kind: "internal",
      depth: 2,
      startMs: 18,
      durationMs: 1.4,
      rank: 2,
    },
    {
      id: "s4",
      name: "products: SearchProducts",
      kind: "graphql",
      depth: 2,
      startMs: 60,
      durationMs: 540,
      rank: 2,
      hasChildren: true,
    },
    {
      id: "s5",
      name: "HTTP POST products",
      kind: "http",
      depth: 3,
      startMs: 70,
      durationMs: 360,
      rank: 3,
      hasChildren: true,
    },
    {
      id: "s6",
      name: "db.query SELECT products",
      kind: "db",
      depth: 4,
      startMs: 95,
      durationMs: 312,
      rank: 4,
      target: true,
    },
    {
      id: "s7",
      name: "accounts: viewer",
      kind: "graphql",
      depth: 2,
      startMs: 620,
      durationMs: 180,
      rank: 2,
      hasChildren: true,
    },
    {
      id: "s8",
      name: "HTTP POST accounts",
      kind: "http",
      depth: 3,
      startMs: 625,
      durationMs: 150,
      rank: 3,
    },
  ] as Span[],
  maxRank: 4,
  ticks: [0, 200, 400, 600],
  // log events emitted during the request — markers ABOVE the waterfall. Each marker sits directly
  // above the SPAN it was emitted from (`spanId`); its x is derived from that span's startMs using
  // the SAME time→x scale the waterfall uses, so the indicator lines up with its span. `count` is
  // the number of logs at that point (1 each here). The cursor hovers the WARN one during the dwell.
  logs: [
    {
      id: "l0",
      spanId: "s2",
      count: 1,
      severity: "info",
      text: "GraphQL request parsed · operation GetHomePageQuery",
    },
    {
      id: "l1",
      spanId: "s5",
      count: 1,
      severity: "info",
      text: "products subgraph: dispatching SearchProducts",
    },
    {
      id: "l2",
      spanId: "s6",
      count: 1,
      severity: "warn",
      text: "db.query slow: SELECT products took 312ms (> 250ms budget)",
      hover: true,
    },
    {
      id: "l3",
      spanId: "s4",
      count: 1,
      severity: "info",
      text: "products subgraph: 24 rows returned",
    },
    {
      id: "l4",
      spanId: "s7",
      count: 1,
      severity: "error",
      text: "accounts subgraph: partial cache miss for viewer",
    },
  ] as {
    id: string;
    spanId: string;
    count: number;
    severity: "info" | "warn" | "error";
    text: string;
    hover?: boolean;
  }[],
  dbSpan: {
    name: "db.query SELECT products",
    duration: "312.4 ms",
    kindBadge: "client",
    general: [
      ["ID", "7b3f1a9c2e4d8f60"],
      ["Parent ID", "a1c4e6b80d2f3597"],
      ["Trace ID", "4e2d9f7a6c1b08e3f5a2c7d49b6e0123"],
      ["Status Message", "Ok"],
    ] as [string, string][],
    database: [
      ["Name", "eshop"],
      ["Operation", "SELECT"],
      ["System", "postgresql"],
      ["Instance", "eshop-prod"],
      ["User", "eshop_reader"],
    ] as [string, string][],
    statement:
      "SELECT p.id, p.name, p.price, p.currency, p.rating\nFROM products AS p\nWHERE p.category_id = $1 AND p.in_stock = true\nORDER BY p.rating DESC\nLIMIT 24",
  },
};

interface OpRow {
  name: string;
  opm: string;
  p95: string;
  errorRate: string;
  impact: number; // 0..1 bar
  slow?: boolean;
  seed: number;
}

// The Insights table (Monitoring → Insights), ordered by impact (real grid sorts impact desc).
// GetHomePageQuery is the slow outlier the developer is chasing — tall latency + highest impact.
const OPS: OpRow[] = [
  {
    name: "GetHomePageQuery",
    opm: "8,420",
    p95: "842 ms",
    errorRate: "0.02%",
    impact: 1.0,
    slow: true,
    seed: 21,
  },
  {
    name: "SearchProducts",
    opm: "128,540",
    p95: "61 ms",
    errorRate: "0.04%",
    impact: 0.42,
    seed: 22,
  },
  {
    name: "GetOrderHistory",
    opm: "14,772",
    p95: "88 ms",
    errorRate: "0.18%",
    impact: 0.31,
    seed: 23,
  },
  {
    name: "AddToCart",
    opm: "42,910",
    p95: "38 ms",
    errorRate: "0.03%",
    impact: 0.22,
    seed: 24,
  },
  {
    name: "GetProductReviews",
    opm: "96,330",
    p95: "24 ms",
    errorRate: "0.00%",
    impact: 0.14,
    seed: 25,
  },
  {
    name: "UpdateAccountAddress",
    opm: "3,015",
    p95: "19 ms",
    errorRate: "0.00%",
    impact: 0.06,
    seed: 26,
  },
];

// gateway-wide overview time-series. Latency is multi-series (min / avg / max) and spiky; the rest
// are single-series. Seeds chosen for organic, distinct shapes.
// min/avg/max are STACKED (avg = min + positive gap, max = avg + positive gap) so the lines never
// cross — max is always ≥ avg ≥ min at every point.
const _latMin = smoothSeries(63, 64, 18, 9);
const _latAvgGap = smoothSeries(62, 64, 34, 12);
const _latMaxGap = smoothSeries(61, 64, 78, 44);
const _latAvg = _latMin.map((v, i) => v + Math.abs(_latAvgGap[i]));
const _latMax = _latAvg.map((v, i) => v + Math.abs(_latMaxGap[i]));
const OVERVIEW = {
  latMax: _latMax,
  latAvg: _latAvg,
  latMin: _latMin,
  throughput: smoothSeries(41, 64, 4900, 1500),
  failed: smoothSeries(71, 56, 18, 16),
};

// the Clients tile — requests per client, a horizontal bar chart (real MetricsClientsChart:
// category y-axis of client names, value x-axis of request totals, hot→cold color ramp).
const CLIENTS = [
  { label: "Web Storefront", value: 128540 },
  { label: "iOS App", value: 86220 },
  { label: "Admin Dashboard", value: 41380 },
  { label: "Mobile Web", value: 27910 },
  { label: "Partner API", value: 9240 },
];
const CLIENTS_MAX = Math.max(...CLIENTS.map((c) => c.value));

// the Errors tile — a small list of recent errors (timestamp / operation / message).
const ERRORS = [
  {
    time: "14:32:08",
    op: "GetHomePageQuery",
    msg: "upstream timeout fetching products subgraph",
  },
  {
    time: "14:29:51",
    op: "AddToCart",
    msg: "inventory.reserve returned 409 Conflict",
  },
  {
    time: "14:21:14",
    op: "GetOrderHistory",
    msg: "null value in non-null field Order.total",
  },
  {
    time: "14:08:37",
    op: "GetHomePageQuery",
    msg: "db.query exceeded statement_timeout (5s)",
  },
];

// the slow operation's own time-series (Operation screen)
const OPSERIES = {
  latency: smoothSeries(51, 48, 842, 140),
  errors: smoothSeries(52, 48, 0.04, 0.05),
  throughput: smoothSeries(53, 48, 8400, 1700),
};

// latency-distribution histogram — MANY thin bins forming a proper distribution curve. Log
// duration x from ~4ms to ~1.2s; a tall log-normal mass low, plus a long slow tail and a small
// red error mass out near the p99. Built procedurally so the curve reads as ~36 bins.
const DIST_BINS = 36;
const DIST_MS_MIN = 4;
const DIST_MS_MAX = 1200;
function buildDistCounts(): number[] {
  // log-normal-ish in log-ms space: peak around ~28ms, sigma wide, + a secondary slow hump.
  const lo = Math.log10(DIST_MS_MIN);
  const hi = Math.log10(DIST_MS_MAX);
  const out: number[] = [];
  for (let i = 0; i < DIST_BINS; i++) {
    const lm = lo + ((hi - lo) * (i + 0.5)) / DIST_BINS; // bin-center log-ms
    const main = Math.exp(-Math.pow((lm - Math.log10(28)) / 0.34, 2)) * 5200;
    const tail = Math.exp(-Math.pow((lm - Math.log10(360)) / 0.42, 2)) * 520;
    // light deterministic jitter so bars aren't a perfect curve
    const j = 1 + 0.12 * Math.sin(i * 1.7) + 0.06 * Math.cos(i * 3.1);
    out.push(Math.max(1, Math.round((main + tail) * j)));
  }
  return out;
}
const DIST = {
  bins: DIST_BINS,
  msMin: DIST_MS_MIN,
  msMax: DIST_MS_MAX,
  counts: buildDistCounts(),
  // error mass lives in the slow tail (last ~6 bins carry a little red)
  errorFrom: DIST_BINS - 6,
  total: 12936,
  markers: [
    { label: "median", ms: 31, color: token.chLatency },
    { label: "p90", ms: 142, color: token.chThroughput },
    { label: "p95", ms: 312, color: token.chP99 },
    { label: "p99", ms: 842, color: token.chP95 },
  ],
};

/* ── geometry ────────────────────────────────────────────────────────────────────── */

const RAIL = 50;
const H_VIEWNAV = 36;
const H_SUBHEAD = 34;
const PANEL_PAD_X = 20;
const HEADER_H = H_VIEWNAV + H_SUBHEAD; // chrome above the scrolling content
const TILE_GAP = 14;

// usable viewport height below the chrome (footer is 23px = AppFrame.FOOTER_H)
const VIEW_H = H - HEADER_H - 23;

// ── PAGE 1 (Monitoring Overview) — one fixed page; the Insights table scrolls into view at the
// bottom of the overview as the cursor scans it.
const OV_PAD_TOP = 16;
const LAT_TILE_H = 240; // TALL latency tile — its multi-series line chart must read clearly
const TP_TILE_H = 168;
const FAIL_TILE_H = 150;
const INSIGHTS_HEAD_H = 38;
const INSIGHTS_COL_H = 30;
const GRID_ROW_H = 36;
const SEC_OVERVIEW_H =
  OV_PAD_TOP +
  LAT_TILE_H +
  TILE_GAP +
  TP_TILE_H +
  TILE_GAP +
  FAIL_TILE_H +
  TILE_GAP +
  INSIGHTS_HEAD_H +
  INSIGHTS_COL_H +
  6 * GRID_ROW_H +
  16;
// the Insights table top inside the overview page
const INSIGHTS_TOP_IN_OV =
  OV_PAD_TOP +
  LAT_TILE_H +
  TILE_GAP +
  TP_TILE_H +
  TILE_GAP +
  FAIL_TILE_H +
  TILE_GAP;
const gridRowYInOv = (i: number) =>
  INSIGHTS_TOP_IN_OV +
  INSIGHTS_HEAD_H +
  INSIGHTS_COL_H +
  i * GRID_ROW_H +
  GRID_ROW_H / 2;
// measured calibration: real rendered Insights row center is ~50 canvas-px below the estimate.
const OV_ROW_CAL = 50;

// ── PAGE 2 (Operation screen) — a scrolling column: op-charts + latency distribution, then the
// Trace Sample waterfall below. Within page 2 the view scrolls down to reveal the trace.
const SEC_OP_H = 470; // per-operation charts + latency distribution
const SEC_TRACE_H = 600; // trace sample header + log lane + waterfall
const OFF_TRACE = SEC_OP_H;

// Trace waterfall geometry (inside the Trace section, which starts at OFF_TRACE in page 2)
const TRACE_HEADER_H = 62;
const RULER_H = 22;
// section pad-top (OV_PAD_TOP=16) + card border (1) + header + log lane + ruler band. Calibrated
// so the cursor tip lands inside the db.query row rect (see scripts/verify-tabs.mjs).
const WATERFALL_TOP_IN_SEC = OV_PAD_TOP + 1 + TRACE_HEADER_H + RULER_H + 4 + 30;
const SPAN_ROW_H = 40;
// the span BAR sits at the top of each row (top:5, height:12 → its center is ~11px below row top).
// db.query span is index 6 → aim the cursor at that span's BAR center inside the page-2 column.
const DB_ROW_YIN_COL = OFF_TRACE + WATERFALL_TOP_IN_SEC + 6 * SPAN_ROW_H + 11;
// the db.query bar spans canvas x ~[242..752]; aim the cursor near its center so the tip lands on
// the bar (not just the full-width row). Measured against the rendered bar in Playwright.
const DB_SPAN_X = RAIL + PANEL_PAD_X + 430;

/* ── STAGE-BASED timeline: each beat owns its ms; the total is DERIVED ────────────── */
const TL = timeline([
  { name: "establish", ms: 1000 }, // settle on the Monitoring Overview
  { name: "overviewDwell", ms: 1500 }, // read the Latency / Throughput / Clients tiles
  { name: "scrollToInsights", ms: 1300 }, // scroll the overview down to the Insights table
  { name: "scanOps", ms: 1400 }, // cursor scans the operation rows
  { name: "moveToSlowRow", ms: 1400 }, // glide to the slow GetHomePageQuery row
  { name: "opClick", ms: 120 }, // click the slow row
  { name: "pageOut", ms: 420 }, // PAGE TRANSITION out — overview fades/slides away
  { name: "opLoad", ms: 650 }, // short spinner — loading the operation screen
  { name: "pageIn", ms: 480 }, // PAGE TRANSITION in — operation screen slides/fades in
  { name: "opReveal", ms: 1500 }, // read the op charts + latency distribution
  { name: "scrollToTrace", ms: 1300 }, // scroll page 2 down to the Trace Sample
  { name: "traceLoad", ms: 550 }, // short spinner — fetching the trace sample
  { name: "waterfallBuild", ms: 1400 }, // the distributed-trace waterfall builds rank by rank
  { name: "moveToDbSpan", ms: 1500 }, // drill DOWN the call tree to the db span
  { name: "dbClick", ms: 120 }, // click the slow db.query span
  { name: "flyoutLoad", ms: 420 }, // span detail loads
  { name: "detailReveal", ms: 1500 }, // General + Database (SQL) reveal
  { name: "moveToLog", ms: 1300 }, // move up to hover the WARN log marker
  { name: "dwell", ms: 2400 }, // dwell on the root-cause payoff (log popup + SQL)
]);

/** DERIVED total duration in ms — feed to SoloScreen / the reel tab. */
export const TRACE_MS = TL.total;
export const TRACE_TL = TL;

export interface TraceScreenProps {
  progress: MotionValue<number>;
  active?: boolean;
}

// ── the WARN log marker the cursor hovers during the dwell (root-cause callout). Computed in
// canvas px so the cursor can travel to it and the verify probe could be re-pointed if needed.
const HOVER_LOG = TRACE.logs.find((l) => l.hover)!;
const WF_LEFT = RAIL + PANEL_PAD_X + 16; // waterfall inner-area left edge in canvas px
const WF_RIGHT = W - PANEL_PAD_X - 16;
const WF_WIDTH = WF_RIGHT - WF_LEFT;
const logXInArea = (atMs: number) => (atMs / TRACE.totalMs) * WF_WIDTH;
// each log sits over its span on the SAME time→x scale the waterfall uses. Most logs anchor to
// their span's START; "completion" logs (rows returned) anchor near the span's END so they don't
// pile on top of the dispatch markers.
// place each marker in the MIDDLE of its span (never at the very start/end) so the circle clears the
// previous row's left-anchored label and reads as belonging to that span.
const logTimeMs = (log: (typeof TRACE.logs)[number]) => {
  const sp = TRACE.spans.find((s) => s.id === log.spanId)!;
  return sp.startMs + sp.durationMs * 0.5;
};
const logLeftPct = (log: (typeof TRACE.logs)[number]) =>
  (logTimeMs(log) / TRACE.totalMs) * 100;
const HOVER_LOG_X = WF_LEFT + logXInArea(logTimeMs(HOVER_LOG));
// log-marker DOT center inside the trace section (page-2 column coords). The marker's 16px circle
// sits at the TOP of the log lane (top:2 + 8px radius); the lane itself starts below the card
// border + header. Calibrated against the measured on-screen dot center so the cursor tip lands on
// the dot, not the band center.
// the WARN marker sits just above the db.query span's bar, so the cursor hovers it ~16px above the bar
const LOG_DOT_YIN_COL = DB_ROW_YIN_COL - 16;

export function TraceScreen({ progress }: TraceScreenProps) {
  // which PAGE is shown: 0 = the gateway Monitoring Overview, 1 = the Operation screen + Trace.
  // The hand-off is a real PAGE TRANSITION (overview fades/slides out, op screen fades/slides in)
  // bridged by a short load — not a scroll. `view` keeps the inactive page from intercepting.
  const pageMid = TL.at("opLoad", 0.5);
  const viewAt = (p: number) => (p >= pageMid ? 1 : 0);
  const [view, setView] = useState(() => viewAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => setView(viewAt(p)));

  // ── PAGE TRANSITION — crossfade + slide between the two distinct page states.
  const ovOpacity = useTransform(
    progress,
    [TL.start("pageOut"), TL.end("pageOut")],
    [1, 0],
    { ease: ease.inOut, clamp: true },
  );
  const ovX = useTransform(
    progress,
    [TL.start("pageOut"), TL.end("pageOut")],
    [0, -40],
    { ease: ease.inOut, clamp: true },
  );
  const opPageOpacity = useTransform(
    progress,
    [TL.start("pageIn"), TL.end("pageIn")],
    [0, 1],
    { ease: ease.inOut, clamp: true },
  );
  const opPageX = useTransform(
    progress,
    [TL.start("pageIn"), TL.end("pageIn")],
    [44, 0],
    { ease: ease.inOut, clamp: true },
  );
  // load spinner that bridges the two pages (over pageOut→opLoad→pageIn)
  const loadOpacity = useTransform(
    progress,
    [
      TL.start("pageOut"),
      TL.at("opLoad", 0.1),
      TL.at("opLoad", 0.9),
      TL.end("pageIn"),
    ],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const loadRot = useTransform(
    progress,
    [TL.start("pageOut"), TL.end("pageIn")],
    [0, 720],
  );

  // ── PAGE 1 scroll (overview → insights). A small translateY just to bring Insights into view.
  const ovMaxScroll = Math.max(0, SEC_OVERVIEW_H - VIEW_H);
  const ovScrollY = useTransform(
    progress,
    [TL.start("scrollToInsights"), TL.end("scrollToInsights")],
    [0, -Math.min(ovMaxScroll, INSIGHTS_TOP_IN_OV - 90)],
    { ease: ease.inOut, clamp: true },
  );

  // ── PAGE 2 scroll (op charts → trace sample).
  const p2MaxScroll = Math.max(0, OFF_TRACE + SEC_TRACE_H - VIEW_H);
  const p2ScrollY = useTransform(
    progress,
    [TL.start("scrollToTrace"), TL.end("scrollToTrace")],
    [0, -Math.min(p2MaxScroll, OFF_TRACE)],
    { ease: ease.inOut, clamp: true },
  );

  // Cursor targets that live inside scrolling columns track the live scroll. OV_ROW_CAL corrects a
  // small layout drift between the estimated overview geometry and the real rendered row center
  // (measured in Playwright: the slow-op-row center sits ~50 canvas-px below the computed value).
  const opRowY = useTransform(
    ovScrollY,
    (sy) => HEADER_H + gridRowYInOv(0) + OV_ROW_CAL + sy,
  );
  const dbRowY = useTransform(
    p2ScrollY,
    (sy) => HEADER_H + DB_ROW_YIN_COL + sy,
  );
  const logY = useTransform(p2ScrollY, (sy) => HEADER_H + LOG_DOT_YIN_COL + sy);

  // ── CURSOR OPACITY — fade the pointer fully OUT across the page transition (pageOut → opLoad →
  // pageIn) and back IN once it is repositioned on page 2. This is what lets the shared cursor jump
  // from the overview slow-op-row to the operation-screen page WITHOUT any visible instantaneous
  // warp: while it crosses that long distance it is invisible.
  const cursorOpacity = useTransform(
    progress,
    [
      TL.at("pageOut", 0.25),
      TL.at("pageOut", 0.6),
      TL.at("pageIn", 0.55),
      TL.at("pageIn", 0.95),
    ],
    [1, 0, 0, 1],
    { ease: ease.inOut, clamp: true },
  );

  // PAGE-2 neutral rest position the cursor fades back in on (a calm spot over the op charts), held
  // through opReveal / scrollToTrace / traceLoad until it purposefully glides to the db span.
  const P2_REST_X = 460;
  const P2_REST_Y = 300;

  // ── CURSOR X — purposeful, target-to-target only (no idle micro-drifts). The big overview→page-2
  // jump happens entirely while the cursor is faded out (between opClick and pageIn end), so it is
  // never seen warping.
  const cx = useTransform(
    progress,
    [
      TL.start("establish"), // rest
      TL.start("moveToSlowRow"), // still resting (hold — no drift)
      TL.start("opClick"), // on the slow op-row (x within the full-width row)
      TL.start("pageOut"), // hold while fading out
      TL.end("pageIn"), // faded back in at the page-2 rest spot
      TL.start("moveToDbSpan"), // hold rest through reading / scrolling / trace fetch
      TL.start("dbClick"), // glide to the db span and click
      TL.end("detailReveal"), // hold on the db span through the detail reveal
      TL.end("moveToLog"), // glide up to the WARN log marker
      1, // rest on the marker
    ],
    [
      460,
      460,
      440,
      440,
      P2_REST_X,
      P2_REST_X,
      DB_SPAN_X,
      DB_SPAN_X,
      HOVER_LOG_X,
      HOVER_LOG_X,
    ],
    { ease: ease.inOut },
  );

  // ── CURSOR Y — blends a progress-driven rest track with the live, scroll-tracked targets. Each
  // target is HELD still until its own move stage, so the cursor never drifts with the scroll before
  // it is supposed to move. The two real glides (rest→db span, db span→log) are smoothed.
  // smoothstep for the in-stage glides — CLAMPED so progress past a stage's end holds at the target
  // (an unclamped smoothstep overshoots wildly for t>1, flinging the cursor off-screen).
  const smooth = (t: number) => {
    const c = t < 0 ? 0 : t > 1 ? 1 : t;
    return c * c * (3 - 2 * c);
  };
  const cy = useTransform(
    [progress, opRowY, dbRowY, logY] as MotionValue<number>[],
    ([p, orow, drow, lrow]: number[]) => {
      // moveToSlowRow: glide down from the rest spot onto the slow op-row, then hold on it (scroll
      // already settled) through the CLICK.
      if (p >= TL.start("moveToSlowRow") && p < TL.start("pageOut")) {
        if (p >= TL.start("opClick")) return orow;
        const f = smooth(
          (p - TL.start("moveToSlowRow")) /
            Math.max(1e-6, TL.start("opClick") - TL.start("moveToSlowRow")),
        );
        return P2_REST_Y + (orow - P2_REST_Y) * f;
      }
      // moveToLog: glide up from the db span to the WARN log marker.
      if (p >= TL.start("moveToLog")) {
        const f = smooth(
          (p - TL.start("moveToLog")) /
            Math.max(1e-6, TL.end("moveToLog") - TL.start("moveToLog")),
        );
        return drow + (lrow - drow) * f;
      }
      // moveToDbSpan: glide down from the page-2 rest spot to the db span (then held through click
      // and the detail reveal).
      if (p >= TL.start("moveToDbSpan")) {
        if (p >= TL.start("dbClick")) return drow;
        const f = smooth(
          (p - TL.start("moveToDbSpan")) /
            Math.max(1e-6, TL.start("dbClick") - TL.start("moveToDbSpan")),
        );
        return P2_REST_Y + (drow - P2_REST_Y) * f;
      }
      // before any move stage on page 2 (and through the faded page transition): hold the rest spot.
      // establish / overview dwells fall here too — a brief, still rest before the first move.
      return P2_REST_Y;
    },
  );

  return (
    <Stage
      width={W}
      height={H}
      fit="fill"
      chrome={false}
      ariaLabel="Nitro — reading the gateway monitoring overview (latency, throughput, clients, errors, insights), clicking a slow operation to transition to its operation screen, scrolling through its latency distribution into a distributed trace sample, drilling into the slow database span, and hovering a warning log event"
      overlay={
        <Cursor
          x={cx}
          y={cy}
          progress={progress}
          opacity={cursorOpacity}
          clickTimes={[TL.start("opClick"), TL.start("dbClick")]}
          pointerWindows={[
            [TL.start("moveToSlowRow"), TL.start("opClick")],
            [TL.start("moveToDbSpan"), TL.start("dbClick")],
            [TL.start("moveToLog"), 1],
          ]}
        />
      }
    >
      <AppFrame railActive="documents" toolbar={<DocTabStrip />}>
        <div
          style={{
            position: "absolute",
            inset: 0,
            display: "flex",
            flexDirection: "column",
          }}
        >
          {/* gateway chrome: view nav (Monitoring active) + "Production Stage" sub-header */}
          <GatewayViewNav />
          <ProductionStageHeader />

          {/* the page viewport — holds the two stacked, crossfading page states */}
          <div
            style={{
              flex: 1,
              minHeight: 0,
              position: "relative",
              overflow: "hidden",
            }}
          >
            {/* PAGE 1 — Monitoring Overview */}
            <motion.div
              style={{
                position: "absolute",
                inset: 0,
                overflow: "hidden",
                opacity: ovOpacity,
                x: ovX,
                display: view === 0 ? "block" : "none",
              }}
            >
              <motion.div
                style={{
                  position: "absolute",
                  left: 0,
                  right: 0,
                  top: 0,
                  y: ovScrollY,
                }}
              >
                <MonitoringOverview />
              </motion.div>
            </motion.div>

            {/* PAGE 2 — Operation screen + Trace Sample (a scrolling column) */}
            <motion.div
              style={{
                position: "absolute",
                inset: 0,
                overflow: "hidden",
                opacity: opPageOpacity,
                x: opPageX,
                display: view === 1 ? "block" : "none",
              }}
            >
              <motion.div
                style={{
                  position: "absolute",
                  left: 0,
                  right: 0,
                  top: 0,
                  y: p2ScrollY,
                  display: "flex",
                  flexDirection: "column",
                }}
              >
                <OperationScreen />
                <TraceSampleSection progress={progress} />
              </motion.div>
            </motion.div>

            {/* the bridging load spinner during the page transition */}
            <motion.div
              style={{
                position: "absolute",
                inset: 0,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                opacity: loadOpacity,
                pointerEvents: "none",
                background: token.surface,
              }}
            >
              <motion.div style={{ rotate: loadRot, display: "flex" }}>
                <IconSpinner size={30} color={token.accent} />
              </motion.div>
            </motion.div>
          </div>
        </div>

        {/* Span Details flyout — slides in as the RESPONSE to the db-span click */}
        <Flyout
          progress={progress}
          show={TL.at("dbClick", 0.4)}
          hide={1.06}
          title="Span Details"
          counter="7 of 9"
          tabs={["General", "Attributes", "Events", "Errors"]}
          activeTab="General"
          indicatorColor={ORANGE}
        >
          <DbDetail progress={progress} />
        </Flyout>
      </AppFrame>
    </Stage>
  );
}

/* ── gateway chrome ─────────────────────────────────────────────────────────────────── */

/** document tab strip — the open gateway API ("EShops Gateway"). */
function DocTabStrip() {
  return (
    <div style={{ display: "flex", alignItems: "center", height: "100%" }}>
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 7,
          padding: "0 12px",
          height: 28,
          borderRadius: "5px 5px 0 0",
          border: `1px solid ${ORANGE}`,
          background: token.surface,
        }}
      >
        <IconServer size={12} color={token.icQuery} />
        <span
          style={{ fontSize: 12.5, color: token.textStrong, fontWeight: 600 }}
        >
          EShops Gateway
        </span>
      </div>
    </div>
  );
}

/** gateway view nav — Overview / Monitoring (active) / … + Production stage selector + Last 7 days. */
function GatewayViewNav() {
  const views = [
    "Overview",
    "Monitoring",
    "Logs",
    "Schema",
    "Deployments",
    "Operations",
    "Clients",
  ];
  return (
    <div
      style={{
        height: H_VIEWNAV,
        flex: "0 0 auto",
        display: "flex",
        alignItems: "center",
        gap: 18,
        padding: "0 14px",
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      {views.map((v) => {
        const on = v === "Monitoring";
        return (
          <span
            key={v}
            style={{
              position: "relative",
              fontSize: 13,
              height: "100%",
              display: "flex",
              alignItems: "center",
              color: on ? token.textStrong : token.textSecondary,
            }}
          >
            {v}
            {on && (
              <span
                style={{
                  position: "absolute",
                  left: 0,
                  right: 0,
                  bottom: 0,
                  height: 2,
                  background: ORANGE,
                }}
              />
            )}
          </span>
        );
      })}
      <span
        style={{
          marginLeft: "auto",
          display: "flex",
          alignItems: "center",
          gap: 6,
          fontSize: 12,
          color: token.text,
          border: `1px solid ${token.border}`,
          borderRadius: 5,
          padding: "4px 8px",
        }}
      >
        Production <IconChevronDown size={12} color={token.textSecondary} />
      </span>
      <span
        style={{
          display: "flex",
          alignItems: "center",
          gap: 5,
          fontSize: 12,
          color: token.text,
        }}
      >
        <IconLock size={12} color={token.textSecondary} />
        eshops.fusion.cloud
      </span>
      <span style={{ color: token.textSecondary, display: "flex" }}>
        <IconSettings size={14} color="currentColor" />
      </span>
    </div>
  );
}

/** "Production Stage" sub-header + a "Last 7 days" range selector (matches the screenshot). */
function ProductionStageHeader() {
  return (
    <div
      style={{
        height: H_SUBHEAD,
        flex: "0 0 auto",
        display: "flex",
        alignItems: "center",
        gap: 10,
        padding: `0 ${PANEL_PAD_X}px`,
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      <span style={{ fontSize: 14, fontWeight: 600, color: token.textStrong }}>
        Production Stage
      </span>
      <span
        style={{
          marginLeft: "auto",
          display: "flex",
          alignItems: "center",
          gap: 6,
          fontSize: 12,
          color: token.text,
          border: `1px solid ${token.border}`,
          borderRadius: 5,
          padding: "4px 8px",
        }}
      >
        <IconCalendar size={13} color={token.textSecondary} /> Last 7 days
      </span>
    </div>
  );
}

/* ── SECTION A — Monitoring Overview ───────────────────────────────────────────────── */

function MonitoringOverview() {
  return (
    <div
      style={{
        minHeight: SEC_OVERVIEW_H,
        padding: `${OV_PAD_TOP}px ${PANEL_PAD_X}px 0`,
        display: "flex",
        flexDirection: "column",
        gap: TILE_GAP,
      }}
    >
      {/* Row 1 — Latency, FULL WIDTH + TALL (multi-series spiky line) */}
      <Tile
        title="Latency"
        height={LAT_TILE_H}
        headerExtra={<MetricBadge value="128 ms" sub="p95" />}
      >
        <MultiLineChart
          series={[
            { values: OVERVIEW.latMax, color: token.chP95, label: "max" },
            { values: OVERVIEW.latAvg, color: token.chLatency, label: "avg" },
            {
              values: OVERVIEW.latMin,
              color: token.chThroughput,
              label: "min",
            },
          ]}
          legend
          jagged
        />
      </Tile>

      {/* Row 2 — Throughput (area) + Clients (horizontal request bars) */}
      <div style={{ display: "flex", gap: TILE_GAP, height: TP_TILE_H }}>
        <div style={{ flex: "0 0 61%", minWidth: 0, display: "flex" }}>
          <Tile
            title="Throughput"
            headerExtra={<MetricBadge value="4.9K" sub="opm" />}
          >
            <AreaLineChart
              values={OVERVIEW.throughput}
              color={token.chThroughput}
            />
          </Tile>
        </div>
        <div style={{ flex: 1, minWidth: 0, display: "flex" }}>
          <Tile
            title="Clients"
            headerExtra={
              <span style={{ fontSize: 11, color: token.textSecondary }}>
                requests
              </span>
            }
          >
            <ClientsBars />
          </Tile>
        </div>
      </div>

      {/* Row 3 — Failed Operations (red line) + Errors (recent error list) */}
      <div style={{ display: "flex", gap: TILE_GAP, height: FAIL_TILE_H }}>
        <div style={{ flex: "0 0 61%", minWidth: 0, display: "flex" }}>
          <Tile title="Failed Operations">
            <AreaLineChart
              values={OVERVIEW.failed}
              color={token.chP95}
              legendLabel="GetHomePageQuery"
            />
          </Tile>
        </div>
        <div style={{ flex: 1, minWidth: 0, display: "flex" }}>
          <Tile
            title="Errors"
            headerExtra={
              <span style={{ fontSize: 11, color: token.errorText }}>
                {ERRORS.length} recent
              </span>
            }
          >
            <ErrorList />
          </Tile>
        </div>
      </div>

      {/* Row 4 — Insights, FULL WIDTH (operations table) */}
      <InsightsTable />
    </div>
  );
}

/* ── tiles + charts ─────────────────────────────────────────────────────────────────── */

function Tile({
  title,
  height,
  headerExtra,
  children,
}: {
  title: string;
  height?: number;
  headerExtra?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <div
      style={{
        flex: 1,
        minWidth: 0,
        height,
        background: token.card,
        border: `1px solid ${token.border}`,
        borderRadius: 8,
        display: "flex",
        flexDirection: "column",
      }}
    >
      <div
        style={{
          height: 36,
          flex: "0 0 auto",
          display: "flex",
          alignItems: "center",
          gap: 10,
          padding: "0 14px",
          borderBottom: `1px solid ${token.border}`,
        }}
      >
        <span
          style={{ fontSize: 13, fontWeight: 600, color: token.textStrong }}
        >
          {title}
        </span>
        {headerExtra && (
          <span style={{ marginLeft: "auto", display: "flex" }}>
            {headerExtra}
          </span>
        )}
      </div>
      <div
        style={{
          flex: 1,
          minHeight: 0,
          padding: "12px 14px 10px",
          position: "relative",
        }}
      >
        {children}
      </div>
    </div>
  );
}

function MetricBadge({ value, sub }: { value: string; sub: string }) {
  return (
    <span style={{ display: "flex", alignItems: "baseline", gap: 4 }}>
      <span
        style={{
          fontSize: 16,
          fontWeight: 700,
          fontFamily: token.mono,
          color: token.textStrong,
        }}
      >
        {value}
      </span>
      <span style={{ fontSize: 10.5, color: token.textSecondary }}>{sub}</span>
    </span>
  );
}

/** A multi-series line chart with an optional legend; `jagged` adds spiky highs (latency).
 *  STATIC — the chart is rendered fully present at rest (no draw-in animation). */
function MultiLineChart({
  series,
  legend,
  jagged,
}: {
  series: { values: number[]; color: string; label: string }[];
  legend?: boolean;
  jagged?: boolean;
}) {
  const all = series.flatMap((s) => s.values);
  const max = Math.max(...all) * 1.16 || 1;
  const GRID = [0.25, 0.5, 0.75];
  return (
    <div
      style={{
        position: "absolute",
        inset: "12px 14px 10px",
        display: "flex",
        flexDirection: "column",
      }}
    >
      {legend && (
        <div style={{ display: "flex", gap: 14, marginBottom: 4 }}>
          {series.map((s) => (
            <span
              key={s.label}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 5,
                fontSize: 10.5,
                color: token.textSecondary,
              }}
            >
              <span
                style={{
                  width: 10,
                  height: 2.5,
                  borderRadius: 2,
                  background: s.color,
                }}
              />{" "}
              {s.label}
            </span>
          ))}
        </div>
      )}
      <div style={{ position: "relative", flex: 1, minHeight: 0 }}>
        <svg
          viewBox="0 0 100 100"
          preserveAspectRatio="none"
          style={{
            position: "absolute",
            inset: 0,
            width: "100%",
            height: "100%",
            overflow: "visible",
          }}
        >
          {GRID.map((f) => (
            <line
              key={f}
              x1={0}
              x2={100}
              y1={f * 100}
              y2={f * 100}
              stroke={token.grid}
              strokeWidth={1}
              vectorEffect="non-scaling-stroke"
            />
          ))}
          {series.map((s, i) => (
            <LinePath
              key={i}
              values={s.values}
              color={s.color}
              max={max}
              jagged={jagged}
            />
          ))}
        </svg>
      </div>
    </div>
  );
}

function LinePath({
  values,
  color,
  max,
  jagged,
}: {
  values: number[];
  color: string;
  max: number;
  jagged?: boolean;
}) {
  const pts: Pt[] = values.map((v, i) => [
    (i / (values.length - 1)) * 100,
    100 - (v / max) * 90 - 5,
  ]);
  const line = smoothLinePath(pts, jagged ? 0.85 : 0.5);
  return (
    <path
      d={line}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
      vectorEffect="non-scaling-stroke"
      stroke={color}
      strokeWidth={jagged ? 1.4 : 1.8}
    />
  );
}

/** A single-series area+line chart (Throughput / Failed Operations). STATIC — fully present. */
function AreaLineChart({
  values,
  color,
  legendLabel,
}: {
  values: number[];
  color: string;
  legendLabel?: string;
}) {
  const max = Math.max(...values) * 1.2 || 1;
  const pts: Pt[] = values.map((v, i) => [
    (i / (values.length - 1)) * 100,
    100 - (v / max) * 88 - 6,
  ]);
  const line = smoothLinePath(pts, 0.55);
  const areaD = areaFromLine(line, pts, 100);
  const GRID = [0.3, 0.6];
  return (
    <div
      style={{
        position: "absolute",
        inset: "12px 14px 10px",
        display: "flex",
        flexDirection: "column",
      }}
    >
      {legendLabel && (
        <div style={{ display: "flex", marginBottom: 4 }}>
          <span
            style={{
              display: "flex",
              alignItems: "center",
              gap: 5,
              fontSize: 10.5,
              color: token.textSecondary,
            }}
          >
            <span
              style={{
                width: 10,
                height: 2.5,
                borderRadius: 2,
                background: color,
              }}
            />{" "}
            {legendLabel}
          </span>
        </div>
      )}
      <div style={{ position: "relative", flex: 1, minHeight: 0 }}>
        <svg
          viewBox="0 0 100 100"
          preserveAspectRatio="none"
          style={{
            position: "absolute",
            inset: 0,
            width: "100%",
            height: "100%",
            overflow: "visible",
          }}
        >
          {GRID.map((f) => (
            <line
              key={f}
              x1={0}
              x2={100}
              y1={f * 100}
              y2={f * 100}
              stroke={token.grid}
              strokeWidth={1}
              vectorEffect="non-scaling-stroke"
            />
          ))}
          <path d={areaD} fill={color} opacity={0.24} />
          <path
            d={line}
            fill="none"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
            stroke={color}
            strokeWidth={1.8}
          />
        </svg>
      </div>
    </div>
  );
}

/** The Clients tile — a horizontal bar chart of requests-per-client (real MetricsClientsChart:
 *  category rows, value bars, hot→cold ramp). STATIC — bars are fully present at rest. */
function ClientsBars() {
  return (
    <div
      style={{
        position: "absolute",
        inset: "12px 14px 10px",
        display: "flex",
        flexDirection: "column",
        justifyContent: "space-around",
      }}
    >
      {CLIENTS.map((c, i) => {
        const frac = c.value / CLIENTS_MAX;
        // hot (top, most requests) → cold ramp, matching the real visualMap. (token.ch* are CSS
        // vars, not hex, so pick from a discrete real-color ramp instead of lerping.)
        const CLIENT_RAMP = [
          token.chImpact,
          token.chP95,
          token.chLatency,
          token.chThroughput,
          token.accent,
        ];
        const color = CLIENT_RAMP[Math.min(CLIENT_RAMP.length - 1, i)];
        return (
          <div
            key={c.label}
            style={{ display: "flex", alignItems: "center", gap: 10 }}
          >
            <span
              style={{
                flex: "0 0 104px",
                fontSize: 11.5,
                color: token.textSecondary,
                whiteSpace: "nowrap",
                overflow: "hidden",
                textOverflow: "ellipsis",
              }}
            >
              {c.label}
            </span>
            <span
              style={{
                flex: 1,
                height: 11,
                borderRadius: 2,
                background: token.grid,
                overflow: "hidden",
              }}
            >
              <span
                style={{
                  display: "block",
                  height: "100%",
                  width: `${frac * 100}%`,
                  borderRadius: "0 2px 2px 0",
                  background: color,
                }}
              />
            </span>
            <span
              style={{
                flex: "0 0 46px",
                textAlign: "right",
                fontSize: 11,
                fontFamily: token.mono,
                color: token.text,
              }}
            >
              {compact(c.value)}
            </span>
          </div>
        );
      })}
    </div>
  );
}

/** The Errors tile — a small list of recent errors (timestamp / operation / message). */
function ErrorList() {
  return (
    <div
      style={{
        position: "absolute",
        inset: "6px 6px 6px",
        display: "flex",
        flexDirection: "column",
        overflow: "hidden",
      }}
    >
      {ERRORS.map((e, i) => (
        <div
          key={i}
          style={{
            display: "flex",
            alignItems: "flex-start",
            gap: 8,
            padding: "6px 8px",
            borderBottom:
              i < ERRORS.length - 1 ? `1px solid ${token.grid}` : "none",
          }}
        >
          <span
            style={{
              display: "flex",
              flex: "0 0 auto",
              marginTop: 1,
              color: token.error,
            }}
          >
            <IconErrorCircle size={13} />
          </span>
          <span
            style={{
              flex: 1,
              minWidth: 0,
              display: "flex",
              flexDirection: "column",
              gap: 1,
            }}
          >
            <span style={{ display: "flex", alignItems: "baseline", gap: 7 }}>
              <span
                style={{
                  fontSize: 11.5,
                  fontWeight: 600,
                  color: token.blue,
                  whiteSpace: "nowrap",
                }}
              >
                {e.op}
              </span>
              <span
                style={{
                  fontSize: 10,
                  fontFamily: token.mono,
                  color: token.textDim,
                  marginLeft: "auto",
                  whiteSpace: "nowrap",
                }}
              >
                {e.time}
              </span>
            </span>
            <span
              style={{
                fontSize: 11,
                color: token.textSecondary,
                whiteSpace: "nowrap",
                overflow: "hidden",
                textOverflow: "ellipsis",
              }}
            >
              {e.msg}
            </span>
          </span>
        </div>
      ))}
    </div>
  );
}

/* ── Insights table (Monitoring → Insights) ─────────────────────────────────────────── */

function InsightsTable() {
  return (
    <div
      style={{
        background: token.card,
        border: `1px solid ${token.border}`,
        borderRadius: 8,
        display: "flex",
        flexDirection: "column",
      }}
    >
      {/* tile header */}
      <div
        style={{
          height: INSIGHTS_HEAD_H,
          flex: "0 0 auto",
          display: "flex",
          alignItems: "center",
          gap: 10,
          padding: "0 14px",
          borderBottom: `1px solid ${token.border}`,
        }}
      >
        <span
          style={{ fontSize: 13, fontWeight: 600, color: token.textStrong }}
        >
          Insights
        </span>
        <span style={{ fontSize: 11.5, color: token.textSecondary }}>
          operations · sorted by impact
        </span>
        <span
          style={{
            marginLeft: "auto",
            display: "flex",
            alignItems: "center",
            gap: 6,
            height: 24,
            padding: "0 8px",
            borderRadius: 4,
            border: `1px solid ${token.border}`,
            background: token.bg,
            fontSize: 11.5,
            color: token.textSecondary,
          }}
        >
          <IconSearch size={12} color="currentColor" /> Search operations…
        </span>
      </div>
      {/* column headers */}
      <div
        style={{
          height: INSIGHTS_COL_H,
          flex: "0 0 auto",
          display: "flex",
          alignItems: "center",
          padding: "0 14px",
          borderBottom: `1px solid ${token.border}`,
          fontSize: 10.5,
          fontWeight: 600,
          letterSpacing: 0.4,
          textTransform: "uppercase",
          color: token.textSecondary,
        }}
      >
        <GridCol flex="1">Operation</GridCol>
        <GridCol w={150} right>
          Latency
        </GridCol>
        <GridCol w={130} right>
          Throughput
        </GridCol>
        <GridCol w={96} right>
          Error Rate
        </GridCol>
        <GridCol w={130} right>
          Impact
        </GridCol>
      </div>
      <div>
        {OPS.map((o) => (
          <GridRow key={o.name} op={o} />
        ))}
      </div>
    </div>
  );
}

function GridRow({ op }: { op: OpRow }) {
  return (
    <div
      data-testid={op.slow ? "slow-op-row" : undefined}
      style={{
        height: GRID_ROW_H,
        display: "flex",
        alignItems: "center",
        padding: "0 14px",
        borderBottom: `1px solid ${token.grid}`,
        background: op.slow ? token.highlight : "transparent",
      }}
    >
      <GridCol flex="1">
        <span style={{ display: "flex", alignItems: "center", gap: 9 }}>
          <OpBadge color={token.icQuery} />
          <span
            style={{
              fontSize: 13,
              color: op.slow ? token.textStrong : token.blue,
              fontWeight: op.slow ? 600 : 400,
              whiteSpace: "nowrap",
            }}
          >
            {op.name}
          </span>
        </span>
      </GridCol>
      <GridCol w={150} right>
        <span
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "flex-end",
            gap: 8,
          }}
        >
          <span
            style={{
              fontSize: 12.5,
              fontFamily: token.mono,
              fontWeight: op.slow ? 700 : 400,
              color: op.slow ? token.chImpact : token.text,
            }}
          >
            {op.p95}
          </span>
          <MiniSpark
            seed={op.seed}
            color={op.slow ? token.chImpact : token.chLatency}
          />
        </span>
      </GridCol>
      <GridCol w={130} right>
        <span
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "flex-end",
            gap: 8,
          }}
        >
          <span
            style={{
              fontSize: 12.5,
              fontFamily: token.mono,
              color: token.text,
            }}
          >
            {op.opm}
          </span>
          <MiniSpark seed={op.seed + 40} color={token.chThroughput} />
        </span>
      </GridCol>
      <GridCol w={96} right>
        <span
          style={{ fontSize: 12.5, fontFamily: token.mono, color: token.text }}
        >
          {op.errorRate}
        </span>
      </GridCol>
      <GridCol w={130} right>
        <ImpactBar value={op.impact} hot={op.slow} />
      </GridCol>
    </div>
  );
}

function MiniSpark({ seed, color }: { seed: number; color: string }) {
  const vals = smoothSeries(seed, 22, 40, 18);
  const max = Math.max(...vals) * 1.18 || 1;
  const pts: Pt[] = vals.map((v, i) => [
    (i / (vals.length - 1)) * 100,
    100 - (v / max) * 80 - 10,
  ]);
  const line = smoothLinePath(pts, 0.5);
  const areaD = areaFromLine(line, pts, 100);
  return (
    <svg
      width={58}
      height={18}
      viewBox="0 0 100 100"
      preserveAspectRatio="none"
      style={{ display: "block", flex: "0 0 auto" }}
    >
      <path d={areaD} fill={color} opacity={0.16} />
      <path
        d={line}
        fill="none"
        stroke={color}
        strokeWidth={2}
        vectorEffect="non-scaling-stroke"
      />
    </svg>
  );
}

function ImpactBar({ value, hot }: { value: number; hot?: boolean }) {
  return (
    <span
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "flex-end",
        gap: 8,
      }}
    >
      <span
        style={{
          width: 80,
          height: 4,
          borderRadius: 2,
          background: token.grid,
          overflow: "hidden",
        }}
      >
        <span
          style={{
            display: "block",
            height: "100%",
            width: `${value * 100}%`,
            background: hot ? token.chImpact : token.accent,
            borderRadius: 2,
          }}
        />
      </span>
      <span
        style={{
          fontSize: 11.5,
          fontFamily: token.mono,
          color: token.textSecondary,
          width: 28,
          textAlign: "right",
        }}
      >
        {Math.round(value * 100)}
      </span>
    </span>
  );
}

/* ── SECTION B.1 — Operation screen (per-operation metrics + distribution) ───────────── */

function OperationScreen() {
  return (
    <div
      style={{
        height: SEC_OP_H,
        padding: `${OV_PAD_TOP}px ${PANEL_PAD_X}px 0`,
        position: "relative",
      }}
    >
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          gap: TILE_GAP,
          height: "100%",
        }}
      >
        {/* operation breadcrumb */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 9,
            flex: "0 0 auto",
          }}
        >
          <span style={{ display: "flex", color: token.textSecondary }}>
            <IconQuery size={15} />
          </span>
          <span
            style={{ fontSize: 15, fontWeight: 600, color: token.textStrong }}
          >
            GetHomePageQuery
          </span>
          <span style={{ fontSize: 12, color: token.textSecondary }}>
            operation metrics
          </span>
        </div>
        {/* Row 1 — three charts side by side */}
        <div
          style={{
            display: "flex",
            gap: TILE_GAP,
            height: 150,
            flex: "0 0 auto",
          }}
        >
          <Tile
            title="Latency"
            headerExtra={<MetricBadge value="842 ms" sub="p95" />}
          >
            <AreaLineChart values={OPSERIES.latency} color={token.chP95} />
          </Tile>
          <Tile
            title="Throughput"
            headerExtra={<MetricBadge value="8.4K" sub="opm" />}
          >
            <AreaLineChart
              values={OPSERIES.throughput}
              color={token.chThroughput}
            />
          </Tile>
          <Tile
            title="Errors"
            headerExtra={<MetricBadge value="0.04" sub="%" />}
          >
            <AreaLineChart values={OPSERIES.errors} color={token.chP95} />
          </Tile>
        </div>
        {/* Row 2 — Latency Distribution, FULL WIDTH */}
        <div
          style={{
            flex: 1,
            minHeight: 0,
            background: token.card,
            border: `1px solid ${token.border}`,
            borderRadius: 8,
            display: "flex",
            flexDirection: "column",
          }}
        >
          <div
            style={{
              height: 36,
              flex: "0 0 auto",
              display: "flex",
              alignItems: "center",
              gap: 10,
              padding: "0 14px",
              borderBottom: `1px solid ${token.border}`,
            }}
          >
            <span
              style={{ fontSize: 13, fontWeight: 600, color: token.textStrong }}
            >
              Latency Distribution
            </span>
            <span style={{ fontSize: 11.5, color: token.textSecondary }}>
              Total operations: {DIST.total.toLocaleString()}
            </span>
            <span
              style={{ marginLeft: "auto", fontSize: 11, color: token.textDim }}
            >
              Click and drag to select a range
            </span>
          </div>
          <div style={{ flex: 1, minHeight: 0, padding: "16px 16px 12px" }}>
            <LatencyDistribution />
          </div>
        </div>
      </div>
    </div>
  );
}

/** Latency-distribution histogram — MANY thin bins (a proper distribution curve) on a log
 *  duration x-axis, log count y-axis, with median/p90/p95/p99 markers + a red error tail.
 *  STATIC — all bars + markers are fully present at rest (no draw-in). */
function LatencyDistribution() {
  const r3 = (v: number) => Math.round(v * 1000) / 1000;
  const maxCount = Math.max(...DIST.counts);
  const yScale = logScale(1, maxCount * 1.15, 100, 4);
  const n = DIST.counts.length;
  const xMin = DIST.msMin;
  const xMax = DIST.msMax;
  const xScale = logScale(xMin, xMax, 0, 100);
  const lo = Math.log10(xMin);
  const hi = Math.log10(xMax);
  // bin [i] spans equal log-ms slices over [xMin, xMax]
  const binLeftMs = (i: number) => Math.pow(10, lo + ((hi - lo) * i) / n);
  const yTicks = [1, 10, 100, 1000, 10000].filter((t) => t <= maxCount * 1.15);
  const markPct = (msVal: number) =>
    xScale(Math.min(xMax, Math.max(xMin, msVal)));

  return (
    <div style={{ position: "relative", width: "100%", height: "100%" }}>
      {/* y labels */}
      <div
        style={{ position: "absolute", left: 0, top: 0, bottom: 18, width: 30 }}
      >
        {yTicks.map((t) => (
          <span
            key={t}
            style={{
              position: "absolute",
              right: 4,
              top: `${yScale(t)}%`,
              transform: "translateY(-50%)",
              fontSize: 9,
              color: token.textDim,
              fontFamily: token.mono,
            }}
          >
            {t >= 1000 ? `${t / 1000}k` : t}
          </span>
        ))}
      </div>
      <div
        style={{ position: "absolute", left: 32, right: 0, top: 0, bottom: 18 }}
      >
        {yTicks.map((t) => (
          <div
            key={t}
            style={{
              position: "absolute",
              left: 0,
              right: 0,
              top: `${yScale(t)}%`,
              borderTop: `1px solid ${token.grid}`,
            }}
          />
        ))}
        <svg
          viewBox="0 0 100 100"
          preserveAspectRatio="none"
          style={{
            position: "absolute",
            inset: 0,
            width: "100%",
            height: "100%",
            overflow: "visible",
          }}
        >
          {DIST.counts.map((c, i) => {
            const x0 = xScale(binLeftMs(i));
            const x1 = xScale(binLeftMs(i + 1));
            const bw = Math.max(0.5, x1 - x0 - 0.4); // thin bars
            const topY = yScale(Math.max(1, c));
            const hasError = i >= DIST.errorFrom;
            // taller red error cap further out in the tail
            const errH = hasError
              ? Math.min(100 - topY, 4 + (i - DIST.errorFrom) * 2.5)
              : 0;
            return (
              <g key={i}>
                <rect
                  x={r3(x0 + 0.2)}
                  y={r3(topY + errH)}
                  width={r3(bw)}
                  height={r3(Math.max(0, 100 - topY - errH))}
                  rx={0.4}
                  style={{ fill: token.cLatency }}
                />
                {hasError && (
                  <rect
                    x={r3(x0 + 0.2)}
                    y={r3(topY)}
                    width={r3(bw)}
                    height={r3(errH)}
                    style={{ fill: token.cError }}
                  />
                )}
              </g>
            );
          })}
        </svg>
        {/* median / p90 / p95 / p99 markers */}
        {DIST.markers.map((m) => (
          <DistMarker
            key={m.label}
            label={m.label}
            leftPct={markPct(m.ms)}
            color={m.color}
          />
        ))}
        {/* x ticks */}
        <div
          style={{
            position: "absolute",
            left: 0,
            right: 0,
            bottom: -16,
            display: "flex",
            justifyContent: "space-between",
            fontSize: 9,
            color: token.textDim,
            fontFamily: token.mono,
          }}
        >
          <span>4ms</span>
          <span>32ms</span>
          <span>256ms</span>
          <span>1s</span>
        </div>
      </div>
    </div>
  );
}

/** A static median/p90/p95/p99 marker — dashed vertical line + label pill. */
function DistMarker({
  label,
  leftPct,
  color,
}: {
  label: string;
  leftPct: number;
  color: string;
}) {
  return (
    <div
      style={{
        position: "absolute",
        top: 0,
        bottom: 0,
        left: `${leftPct}%`,
        pointerEvents: "none",
      }}
    >
      <div
        style={{
          position: "absolute",
          top: 16,
          bottom: 0,
          left: 0,
          width: 0,
          borderLeft: `1.5px dashed ${color}`,
        }}
      />
      <span
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          transform: "translateX(-50%)",
          fontSize: 9.5,
          fontWeight: 600,
          color: "#fff",
          background: color,
          borderRadius: 3,
          padding: "1px 6px",
          whiteSpace: "nowrap",
        }}
      >
        {label}
      </span>
    </div>
  );
}

/* ── SECTION B.2 — Trace Sample (waterfall → drill the slow span) ───────────────────── */

function TraceSampleSection({ progress }: { progress: MotionValue<number> }) {
  // The trace sample is present from the start of the operation screen — no load spinner.
  return (
    <div
      style={{
        height: SEC_TRACE_H,
        padding: `${OV_PAD_TOP}px ${PANEL_PAD_X}px 0`,
        position: "relative",
      }}
    >
      <div
        style={{
          height: "100%",
          background: token.card,
          border: `1px solid ${token.border}`,
          borderRadius: 8,
          display: "flex",
          flexDirection: "column",
          position: "relative",
          overflow: "visible",
        }}
      >
        {/* Trace Sample header — title + "1 of N" + Timeline | Logs tabs */}
        <div
          style={{
            height: TRACE_HEADER_H,
            flex: "0 0 auto",
            padding: "0 16px",
            display: "flex",
            flexDirection: "column",
            justifyContent: "center",
            borderBottom: `1px solid ${token.border}`,
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <span
              style={{ fontSize: 14, fontWeight: 600, color: token.textStrong }}
            >
              Trace Sample
            </span>
            <span style={{ fontSize: 12, color: token.textSecondary }}>
              · 1 of {TRACE.sampleOf}
            </span>
            <span
              style={{
                marginLeft: "auto",
                display: "flex",
                alignItems: "center",
                gap: 8,
                fontSize: 12.5,
                color: token.textSecondary,
              }}
            >
              <span
                style={{
                  fontFamily: token.mono,
                  color: token.chImpact,
                  fontWeight: 600,
                }}
              >
                {TRACE.totalMs} ms
              </span>
              <span>· {TRACE.timeAgo}</span>
            </span>
          </div>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 18,
              marginTop: 8,
              fontSize: 12.5,
            }}
          >
            <TraceTab label="Timeline" active />
            <TraceTab label="Logs" />
            <span
              style={{
                marginLeft: "auto",
                display: "flex",
                alignItems: "center",
                gap: 14,
              }}
            >
              {(["HTTP", "GraphQL", "Internal", "DB"] as const).map((l, i) => (
                <span
                  key={l}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 5,
                    fontSize: 10.5,
                    color: token.textSecondary,
                  }}
                >
                  <span
                    style={{
                      width: 9,
                      height: 9,
                      borderRadius: 2,
                      background: [PAL.http, PAL.graphql, PAL.internal, PAL.db][
                        i
                      ],
                    }}
                  />{" "}
                  {l}
                </span>
              ))}
            </span>
          </div>
        </div>

        {/* the waterfall — present from the start (no load reveal) */}
        <div
          style={{
            flex: 1,
            minHeight: 0,
            display: "flex",
            flexDirection: "column",
          }}
        >
          {/* time ruler */}
          <div
            style={{
              position: "relative",
              height: RULER_H,
              flex: "0 0 auto",
              margin: "0 16px",
            }}
          >
            {TRACE.ticks.map((t) => (
              <span
                key={t}
                style={{
                  position: "absolute",
                  left: `${(t / TRACE.totalMs) * 100}%`,
                  top: 2,
                  transform: t === 0 ? "none" : "translateX(-50%)",
                  fontSize: 10.5,
                  color: token.textSecondary,
                }}
              >
                {t === 0 ? "0" : `${t}ms`}
              </span>
            ))}
            <span
              style={{
                position: "absolute",
                right: 0,
                top: 0,
                fontSize: 12.5,
                fontWeight: 600,
                color: token.text,
              }}
            >
              {TRACE.totalMs} ms
            </span>
          </div>
          {/* waterfall rows */}
          <div
            style={{
              position: "relative",
              flex: 1,
              minHeight: 0,
              margin: "0 16px",
            }}
          >
            {TRACE.ticks.map((t) => (
              <div
                key={t}
                style={{
                  position: "absolute",
                  left: `${(t / TRACE.totalMs) * 100}%`,
                  top: 0,
                  bottom: 0,
                  width: 0,
                  borderLeft: `1px dashed ${token.grid}`,
                }}
              />
            ))}
            {TRACE.spans.map((s, i) => (
              <SpanRow key={s.id} span={s} index={i} progress={progress} />
            ))}
            {/* log markers — each just ABOVE its own span's bar, with a short flag-pole */}
            {TRACE.logs.map((lg) => (
              <LogMarker key={lg.id} log={lg} progress={progress} />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function TraceTab({ label, active }: { label: string; active?: boolean }) {
  return (
    <span
      style={{
        position: "relative",
        height: 22,
        display: "flex",
        alignItems: "center",
        color: active ? token.textStrong : token.textSecondary,
        fontWeight: active ? 600 : 400,
      }}
    >
      {label}
      {active && (
        <span
          style={{
            position: "absolute",
            left: 0,
            right: 0,
            bottom: -2,
            height: 2,
            background: ORANGE,
          }}
        />
      )}
    </span>
  );
}

const LOG_SEVERITY = {
  info: {
    color: token.blue,
    label: "INFO",
    icon: (s: number) => <IconInfo size={s} />,
  },
  warn: {
    color: token.warning,
    label: "WARN",
    icon: (s: number) => <IconWarning size={s} />,
  },
  error: {
    color: token.error,
    label: "ERROR",
    icon: (s: number) => <IconErrorCircle size={s} />,
  },
} as const;

/** A log-event marker sitting JUST ABOVE its own span's bar: a small severity circle badged with
 *  the log COUNT, on a short "flag-pole" stem that drops onto the span's bar (so it's clearly tied
 *  to that span — not floating in a top lane). x uses the same time→% scale as the bar. The WARN
 *  marker the cursor settles on shows a tooltip POPUP (the log message) above it during the dwell. */
const CIRCLE = 15;
const STEM = 7;
function LogMarker({
  log,
  progress,
}: {
  log: (typeof TRACE.logs)[number];
  progress: MotionValue<number>;
}) {
  const sev = LOG_SEVERITY[log.severity];
  const leftPct = logLeftPct(log);
  const idx = TRACE.spans.findIndex((s) => s.id === log.spanId);
  // the WARN one pops its tooltip as the cursor reaches it.
  const popOpacity = useTransform(
    progress,
    [TL.at("moveToLog", 0.6), TL.end("moveToLog")],
    [0, 1],
    { clamp: true },
  );
  return (
    <div
      style={{
        position: "absolute",
        left: `${leftPct}%`,
        top: idx * SPAN_ROW_H + 5 - STEM - CIRCLE,
        transform: "translateX(-50%)",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        zIndex: log.hover ? 60 : 45,
      }}
    >
      <span
        style={{
          width: CIRCLE,
          height: CIRCLE,
          borderRadius: "50%",
          background: token.surface,
          border: `1.5px solid ${sev.color}`,
          color: sev.color,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          fontSize: 9,
          fontWeight: 700,
          fontFamily: token.mono,
        }}
      >
        {log.count}
      </span>
      {/* flag-pole down to the span's bar */}
      <span
        style={{ width: 1, height: STEM, background: sev.color, opacity: 0.7 }}
      />
      {log.hover && (
        <motion.div
          style={{
            position: "absolute",
            bottom: "100%",
            marginBottom: 4,
            left: "50%",
            transform: "translateX(-50%)",
            width: 300,
            opacity: popOpacity,
            background: token.surface,
            border: `1px solid ${token.border}`,
            borderRadius: 6,
            boxShadow: "0 8px 24px rgba(1,4,9,0.5)",
            zIndex: 1000,
            pointerEvents: "none",
            overflow: "hidden",
          }}
        >
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 6,
              padding: "6px 10px",
              borderBottom: `1px solid ${token.border}`,
            }}
          >
            <span style={{ display: "flex", color: sev.color }}>
              {sev.icon(13)}
            </span>
            <span
              style={{
                fontSize: 11,
                fontFamily: token.mono,
                fontWeight: 600,
                color: sev.color,
              }}
            >
              {sev.label}
            </span>
            <span
              style={{
                marginLeft: "auto",
                fontSize: 10,
                fontFamily: token.mono,
                color: token.textDim,
              }}
            >
              +{Math.round(logTimeMs(log))} ms
            </span>
          </div>
          <div
            style={{
              padding: "8px 10px",
              fontSize: 11.5,
              fontFamily: token.mono,
              color: token.text,
              whiteSpace: "normal",
              lineHeight: 1.45,
            }}
          >
            {log.text}
          </div>
        </motion.div>
      )}
    </div>
  );
}

function SpanRow({
  span,
  index,
  progress,
}: {
  span: Span;
  index: number;
  progress: MotionValue<number>;
}) {
  const color = KIND_COLOR[span.kind];
  const left = (span.startMs / TRACE.totalMs) * 100;
  const width = Math.max(0.6, (span.durationMs / TRACE.totalMs) * 100);
  const rightAnchor = left > 55;

  // The waterfall is rendered STATICALLY present — the full span tree is already there the instant
  // the operation screen is shown (no rank-by-rank build-in). Bars and labels are at full from the
  // start; the only "load" is the brief trace-fetch spinner gating the whole lane (see
  // `waterfallOpacity`). `built` is kept as a constant 1 so the bar geometry stays unchanged.
  const built = 1;

  // the amber DB span carries a SUBTLE bottleneck highlight (no heavy glow), nudging up slightly as
  // it is about to be clicked.
  const glow = useTransform(progress, [0, TL.start("dbClick")], [3, 4], {
    clamp: true,
  });
  const targetGlow = useTransform(
    glow,
    (r) => `0 0 ${r}px 0 ${token.chImpact}`,
  );

  // the DB span row latches to the active (selected) highlight the instant it is clicked
  const select = TL.start("dbClick");
  const rowBg = useTransform(progress, (p) =>
    span.target && p >= select ? token.highlight : "transparent",
  );

  return (
    <motion.div
      data-testid={span.target ? "db-span" : undefined}
      style={{
        position: "absolute",
        left: 0,
        right: 0,
        top: index * SPAN_ROW_H,
        height: SPAN_ROW_H,
        background: rowBg,
        borderBottom: `1px solid ${token.grid}`,
      }}
    >
      {span.hasChildren && (
        <span
          style={{
            position: "absolute",
            left: 4 + span.depth * 12,
            top: 5,
            color: token.textSecondary,
            display: "flex",
          }}
        >
          <IconChevronDown size={13} />
        </span>
      )}
      <motion.div
        style={{
          position: "absolute",
          left: `${left}%`,
          top: 5,
          width: `${width}%`,
          height: 12,
          borderRadius: 2,
          background: color,
          transformOrigin: "left center",
          scaleX: built,
          boxShadow: span.target ? targetGlow : undefined,
          // (scaleX held at 1 — the bar is statically full-width from the start)
        }}
      />
      <motion.div
        style={{
          position: "absolute",
          top: 22,
          [rightAnchor ? "right" : "left"]: rightAnchor
            ? `${100 - left}%`
            : `${left}%`,
          display: "flex",
          alignItems: "center",
          gap: 7,
          opacity: 1,
          whiteSpace: "nowrap",
        }}
      >
        <span style={{ display: "flex", color, flex: "0 0 auto" }}>
          {KIND_ICON[span.kind](12)}
        </span>
        <span
          style={{
            fontSize: 12,
            color: span.target ? token.textStrong : token.text,
            fontWeight: span.target ? 600 : 400,
          }}
        >
          {span.name}
        </span>
        <span
          style={{
            fontSize: 11,
            color: token.textSecondary,
            fontFamily: token.mono,
          }}
        >
          {fmtDur(span.durationMs)}
        </span>
      </motion.div>
    </motion.div>
  );
}

const fmtDur = (d: number) =>
  d >= 1 ? `${d.toFixed(d < 10 ? 1 : 0)} ms` : `${Math.round(d * 1000)} µs`;

/* ── Span Details flyout body ───────────────────────────────────────────────────────── */

function DbDetail({ progress }: { progress: MotionValue<number> }) {
  // header settles WITH the flyout; the General + Database (SQL) reveals are the climax, then hold.
  const w0 = TL.start("detailReveal");
  return (
    <div style={{ display: "flex", flexDirection: "column", height: "100%" }}>
      {/* span header — span name + duration + client kind */}
      <div style={{ marginBottom: 14, flex: "0 0 auto" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span style={{ display: "flex", color: token.chImpact }}>
            <IconDatabase size={16} />
          </span>
          <span
            style={{ fontSize: 15, fontWeight: 600, color: token.textStrong }}
          >
            {TRACE.dbSpan.name}
          </span>
        </div>
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            marginTop: 6,
          }}
        >
          <span
            style={{
              fontSize: 12,
              fontFamily: token.mono,
              color: token.chImpact,
              fontWeight: 600,
            }}
          >
            {TRACE.dbSpan.duration}
          </span>
          <Badge text={TRACE.dbSpan.kindBadge} bg={token.blue} dark />
        </div>
      </div>

      <div style={{ flex: 1, minHeight: 0, overflow: "hidden" }}>
        {/* General Information settles first … */}
        <TableList
          title="General Information"
          rows={TRACE.dbSpan.general.map(([label, value]) => ({
            label,
            value,
            mono: true,
          }))}
          progress={progress}
          playWindow={[w0, TL.at("detailReveal", 0.4)]}
        />
        {/* Database attributes — System / Instance / etc. */}
        <TableList
          title="Database"
          rows={TRACE.dbSpan.database.map(([label, value]) => ({
            label,
            value,
            mono: label === "System" || label === "Instance",
          }))}
          progress={progress}
          playWindow={[TL.at("detailReveal", 0.4), TL.at("detailReveal", 0.7)]}
        />
        {/* … THE CLIMAX: the literal slow SQL statement, properly syntax-highlighted. */}
        <SqlStatement progress={progress} />
      </div>
    </div>
  );
}

/** The Span Details "Statement" block — the slow SQL, syntax-highlighted via CodeBlock. The SQL
 *  types in over the back half of detailReveal as the span's attributes resolve. */
function SqlStatement({ progress }: { progress: MotionValue<number> }) {
  return (
    <div style={{ marginTop: 14 }}>
      <div
        style={{
          fontSize: 12,
          fontWeight: 600,
          color: token.textStrong,
          marginBottom: 6,
        }}
      >
        Statement
      </div>
      <div
        style={{
          background: token.bg,
          border: `1px solid ${token.border}`,
          borderRadius: 6,
          overflow: "hidden",
          padding: "0 10px",
        }}
      >
        <CodeBlock
          code={TRACE.dbSpan.statement}
          lang="sql"
          gutter={false}
          caret={false}
          fontSize={11.5}
          lineHeight={18}
          padding={10}
          progress={progress}
          playWindow={[TL.at("detailReveal", 0.55), TL.end("detailReveal")]}
          ariaLabel="slow SQL statement"
        />
      </div>
    </div>
  );
}

/* ── small helpers ─────────────────────────────────────────────────────────────────── */

function GridCol({
  w,
  flex,
  right,
  children,
}: {
  w?: number;
  flex?: string;
  right?: boolean;
  children: React.ReactNode;
}) {
  return (
    <span
      style={{
        flex: flex ?? `0 0 ${w}px`,
        minWidth: 0,
        textAlign: right ? "right" : "left",
        display: "flex",
        alignItems: "center",
        justifyContent: right ? "flex-end" : "flex-start",
      }}
    >
      {children}
    </span>
  );
}

function OpBadge({ color }: { color: string }) {
  return (
    <span
      style={{
        flex: "0 0 auto",
        width: 18,
        height: 18,
        borderRadius: 3,
        border: `1px solid ${color}`,
        color,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      }}
    >
      <IconQuery size={11} />
    </span>
  );
}

function Badge({
  text,
  bg,
  dark,
}: {
  text: string;
  bg: string;
  dark?: boolean;
}) {
  return (
    <span
      style={{
        fontSize: 11,
        fontWeight: 500,
        padding: "2px 7px",
        borderRadius: 4,
        background: bg,
        color: dark ? token.surface : "#fff",
        textTransform: "lowercase",
      }}
    >
      {text}
    </span>
  );
}
