/**
 * DiagnoseScreen - Tab 3: "From a monitoring error to the root cause, like an error-tracking
 * tool (Sentry / an ELK stack)."
 *
 * This is a multi-PAGE flow (real page transitions, not scrolls), each page a by-eye clone of a
 * Nitro surface:
 *
 *   PAGE 0 - MONITORING OVERVIEW (same visual language as the Observe/TraceScreen overview): the
 *     gateway chrome with **Monitoring** active, a "Production Stage" sub-header, and a tile grid -
 *     Latency (full-width line) · Throughput (area) + Clients (request bars) · Failed Operations
 *     (red line) + **Errors** (a list of recent errors) · Insights (the operations table). The
 *     cursor scans the Errors tile and clicks one ERROR entry (data-testid="error-entry").
 *
 *   PAGE 1 - ERROR SCREEN (Sentry-style; this view does NOT exist in the real app - designed
 *     faithful to Nitro's visual language): a header with the exception type + message + a
 *     createOrder badge + INTERNAL_SERVER_ERROR code; an OCCURRENCES-OVER-TIME bar chart (a spike);
 *     stat tiles (total events / first seen / last seen / affected clients); "WHERE IT OCCURS"
 *     (operation · subgraph · top stack frame); EXAMPLE TRACES (recent trace samples); and the
 *     server STACK TRACE with the root-cause frame highlighted. The cursor clicks "View logs".
 *
 *   PAGE 2 - LOGS VIEW (faithful to the real Nitro Logs view): a **Log Distribution BAR chart** (a
 *     histogram of log volume over time, bars STACKED by severity) above a structured log list
 *     (severity icon · ISO timestamp · message, like the real LogList → DefaultLogEntry). The
 *     cursor DRAG-SELECTS a time range on the distribution chart (press → drag → release a brush
 *     rectangle over the error burst); the list narrows to that window (short load). Then it clicks
 *     the failing error row (data-testid="failing-row") → the Log Detail flyout
 *     (data-testid="reel-flyout") slides in with the request context + GraphQL error + server stack
 *     trace, and we dwell on the root cause.
 *
 * Page transitions are crossfade + slide bridged by a short load spinner (like TraceScreen). All
 * motion derives from a STAGE-BASED timeline (`src/lib/timeline.ts`) via `progress`/useTransform -
 * no internal clocks. At progress=1 (reduced-motion freeze) the frame is the fully-resolved payoff:
 * Logs brushed to the burst, flyout open, stack trace readable, root frame lit. Data new to this
 * screen is LOCAL to this file (nothing added to src/lib/data/tabs.ts).
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
import {
  GatewayChrome,
  GW_DOCTABS_H,
} from "../../primitives/reel/GatewayChrome";
import { TableList } from "../../primitives/reel/TableList";
import { TABREEL_CANVAS } from "../../primitives/reel/TabReel";
import { token } from "../../lib/tokens";
import { ease } from "../../lib/motion";
import { timeline } from "../../lib/timeline";
import {
  smoothLinePath,
  areaFromLine,
  compact,
  type Pt,
} from "../../lib/scale";
import {
  IconMutation,
  IconQuery,
  IconErrorCircle,
  IconWarning,
  IconInfo,
  IconSpinner,
  IconCalendar,
  IconServer,
  IconDatabase,
  IconChevronRight,
} from "../../primitives/icons";

const W = TABREEL_CANVAS.w;
const H = TABREEL_CANVAS.h;
// Exact Nitro github-dark danger (chart-error) = #cf222e.
const DANGER = "#cf222e";
const ORANGE = token.graphEdgeActive;

/* ── geometry / chrome ────────────────────────────────────────────────────────────── */
const RAIL = 50;
const H_VIEWNAV = 36;
const H_SUBHEAD = 34;
// chrome above the page content: GatewayChrome (doc-tab strip + view-nav) + the Production Stage
// sub-header. GW_DOCTABS_H (38) is the added doc-tab-strip height over the old hand-rolled view-nav.
const HEADER_H = GW_DOCTABS_H + H_VIEWNAV + H_SUBHEAD;
const PANEL_PAD_X = 20;
const TILE_GAP = 14;

/* ── STAGE-BASED timeline: each beat owns its ms; the total is DERIVED ──────────────
 * Three pages, two page transitions. Generous moves/dwells: cursor glides are slow, UI
 * reveals/loads are short, clicks 200ms, and each page's payoff gets time to read. */
const TL = timeline([
  // PAGE 0 - Monitoring overview
  { name: "establish", ms: 1000 }, // settle on the Monitoring overview (calm gateway)
  { name: "overviewDwell", ms: 1400 }, // read Latency / Throughput / Clients / Failed Ops
  { name: "moveToError", ms: 1300 }, // glide to the Errors tile, hover the createOrder error
  { name: "errorHover", ms: 700 }, // rest on the error entry before clicking
  { name: "errorClick", ms: 110 }, // click the error entry
  // PAGE 0 → PAGE 1 transition
  { name: "p1Out", ms: 420 }, // overview fades/slides away
  { name: "p1Load", ms: 650 }, // short spinner - loading the error screen
  { name: "p1In", ms: 480 }, // error screen slides/fades in
  // PAGE 1 - Error screen
  { name: "errHeader", ms: 1200 }, // read the exception header + code badge
  { name: "errChart", ms: 1300 }, // the occurrences bar chart + stat tiles read in
  { name: "errWhere", ms: 1500 }, // "Where it occurs" + example traces read in
  { name: "errStack", ms: 1400 }, // the server stack trace + root cause read
  { name: "moveToViewLogs", ms: 1300 }, // glide to the "View logs" action
  { name: "viewLogsHover", ms: 600 }, // rest on the View logs button
  { name: "viewLogsClick", ms: 110 }, // click View logs
  // PAGE 1 → PAGE 2 transition
  { name: "p2Out", ms: 420 }, // error screen fades/slides away
  { name: "p2Load", ms: 650 }, // short spinner - loading the logs view
  { name: "p2In", ms: 480 }, // logs view slides/fades in
  // PAGE 2 - Logs view
  { name: "logsReveal", ms: 900 }, // the distribution bars + log list stream in
  { name: "logsDwell", ms: 800 }, // read the streamed log list
  { name: "moveToDrag", ms: 1100 }, // glide to the left edge of the error burst on the chart
  { name: "dragPress", ms: 260 }, // press down to start the time-range brush
  { name: "dragSelect", ms: 1100 }, // DRAG across the burst - the brush rectangle grows
  { name: "dragRelease", ms: 220 }, // release the brush
  { name: "brushLoad", ms: 650 }, // refetch - the list narrows to the burst window
  { name: "brushDwell", ms: 600 }, // read the narrowed error list
  { name: "moveToRow", ms: 1100 }, // glide to the failing error log row
  { name: "rowHover", ms: 600 }, // rest on the failing row
  { name: "rowClick", ms: 110 }, // click the failing row
  { name: "flyoutLoad", ms: 550 }, // Log Detail flyout slides in (General tab)
  { name: "flyoutDwell", ms: 850 }, // read the severity header + message
  { name: "contextReveal", ms: 1500 }, // request context + GraphQL error fill in
  { name: "stackReveal", ms: 1600 }, // the server .NET stack trace fills in
  { name: "rootCause", ms: 900 }, // OrderService.cs:87 frame lights up
  { name: "dwell", ms: 2400 }, // rest on the root cause
]);

/** DERIVED total duration in ms - feed to SoloScreen / the reel tab. */
export const DIAGNOSE_MS = TL.total;
export const DIAGNOSE_TL = TL;

/* ── LOCAL data (kept out of src/lib/data/tabs.ts) ──────────────────────────────────── */

const FAILING_OP = "createOrder";
const SPAN_NAME = "Orders.createOrder";
const FAILING_TS = "2024-03-18T14:32:07.214Z";

// deterministic smooth series (local - avoids importing tabs.ts data helpers)
function series(seed: number, n: number, base: number, amp: number): number[] {
  const out: number[] = [];
  let s = seed;
  for (let i = 0; i < n; i++) {
    s = (s * 9301 + 49297) % 233280;
    const r = s / 233280;
    out.push(
      Math.max(
        0,
        base + Math.sin(i * 0.7 + seed) * amp * 0.5 + (r - 0.5) * amp,
      ),
    );
  }
  return out;
}

// PAGE 0 overview time-series
const OVERVIEW = {
  latMax: series(61, 60, 120, 80),
  latAvg: series(62, 60, 52, 24),
  latMin: series(63, 60, 18, 9),
  throughput: series(41, 60, 4900, 1400),
  failed: series(71, 52, 16, 18),
};

// PAGE 0 Clients tile
const CLIENTS = [
  { label: "Web Storefront", value: 128540 },
  { label: "iOS App", value: 86220 },
  { label: "Admin Dashboard", value: 41380 },
  { label: "Mobile Web", value: 27910 },
  { label: "Partner API", value: 9240 },
];
const CLIENTS_MAX = Math.max(...CLIENTS.map((c) => c.value));

// PAGE 0 Errors tile - recent errors; the cursor clicks the createOrder one.
interface ErrorEntry {
  time: string;
  op: string;
  type: string;
  msg: string;
  target?: boolean;
}
const ERRORS: ErrorEntry[] = [
  {
    time: "14:32:07",
    op: "createOrder",
    type: "System.InvalidOperationException",
    msg: "Sequence contains no elements",
    target: true,
  },
  {
    time: "14:29:51",
    op: "AddToCart",
    type: "ConflictException",
    msg: "inventory.reserve returned 409 Conflict",
  },
  {
    time: "14:21:14",
    op: "GetOrderHistory",
    type: "GraphQLException",
    msg: "null value in non-null field Order.total",
  },
  {
    time: "14:08:37",
    op: "createOrder",
    type: "TimeoutException",
    msg: "db.query exceeded statement_timeout (5s)",
  },
];

// PAGE 0 Insights table
interface OpRow {
  name: string;
  kind: "query" | "mutation";
  opm: string;
  p95: string;
  errorRate: string;
  failing?: boolean;
}
const OPS: OpRow[] = [
  {
    name: "createOrder",
    kind: "mutation",
    opm: "18,204",
    p95: "142 ms",
    errorRate: "12.4%",
    failing: true,
  },
  {
    name: "SearchProducts",
    kind: "query",
    opm: "128,540",
    p95: "61 ms",
    errorRate: "0.04%",
  },
  {
    name: "GetOrderHistory",
    kind: "query",
    opm: "14,772",
    p95: "88 ms",
    errorRate: "1.80%",
  },
  {
    name: "AddToCart",
    kind: "mutation",
    opm: "42,910",
    p95: "38 ms",
    errorRate: "0.03%",
  },
  {
    name: "GetProductReviews",
    kind: "query",
    opm: "96,330",
    p95: "24 ms",
    errorRate: "0.00%",
  },
];

// PAGE 1 error-screen: occurrences-over-time histogram (calm baseline → a sharp spike → settle).
const OCCUR = [
  2, 1, 3, 2, 1, 2, 3, 2, 1, 2, 1, 2, 3, 1, 2, 2, 4, 9, 22, 41, 58, 47, 28, 14,
  7, 4, 3, 2,
];
const OCCUR_PEAK = 58;

// PAGE 1 example traces
interface TraceSample {
  id: string;
  time: string;
  duration: string;
  status: "error" | "ok";
}
const TRACES: TraceSample[] = [
  {
    id: "4e2d9f7a6c1b08e3",
    time: "14:32:07",
    duration: "142 ms",
    status: "error",
  },
  {
    id: "9b71c0d54f2a3e88",
    time: "14:32:09",
    duration: "138 ms",
    status: "error",
  },
  {
    id: "a1c4e6b80d2f3597",
    time: "14:31:55",
    duration: "129 ms",
    status: "ok",
  },
  {
    id: "7b3f1a9c2e4d8f60",
    time: "14:31:42",
    duration: "151 ms",
    status: "error",
  },
];

// PAGE 2 Logs - the structured log stream.
type Severity = "error" | "warn" | "info";
interface LogRow {
  ts: string;
  severity: Severity;
  message: string;
  burst?: boolean;
}
const LOG_ROWS: LogRow[] = [
  {
    ts: "2024-03-18T14:31:58.002Z",
    severity: "info",
    message: "Executed SearchProducts in 61 ms",
  },
  {
    ts: "2024-03-18T14:32:01.880Z",
    severity: "info",
    message: "Executed AddToCart in 38 ms",
  },
  {
    ts: "2024-03-18T14:32:04.119Z",
    severity: "warn",
    message: "GetOrderHistory resolver timed out, returning cached value",
  },
  {
    ts: "2024-03-18T14:32:06.560Z",
    severity: "info",
    message: "Executed GetProductReviews in 24 ms",
  },
  {
    ts: FAILING_TS,
    severity: "error",
    message:
      "Unhandled exception while executing request - Sequence contains no elements",
    burst: true,
  },
  {
    ts: "2024-03-18T14:32:07.902Z",
    severity: "warn",
    message: "Inventory reservation degraded, falling back to slow path",
    burst: true,
  },
  {
    ts: "2024-03-18T14:32:09.341Z",
    severity: "error",
    message:
      "Unhandled exception while executing request - Sequence contains no elements",
    burst: true,
  },
  {
    ts: "2024-03-18T14:32:10.118Z",
    severity: "error",
    message:
      "Unhandled exception while executing request - Sequence contains no elements",
    burst: true,
  },
  {
    ts: "2024-03-18T14:32:11.006Z",
    severity: "info",
    message: "Executed SearchProducts in 58 ms",
  },
  {
    ts: "2024-03-18T14:32:13.774Z",
    severity: "info",
    message: "Executed GetProductReviews in 31 ms",
  },
];
const BURST_ROWS = LOG_ROWS.filter((r) => r.burst);

// PAGE 2 Log Distribution - stacked-BAR histogram of log volume by severity (info baseline + a
// warn/error burst), 28 buckets.
const DIST = {
  info: [
    4, 5, 4, 6, 5, 4, 5, 6, 5, 4, 6, 5, 4, 5, 6, 5, 4, 6, 5, 4, 5, 6, 5, 4, 5,
    6, 5, 4,
  ],
  warn: [
    0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 3, 5, 6, 4, 3, 2, 1, 1, 0,
    1, 0, 0,
  ],
  error: [
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 6, 12, 18, 14, 9, 5, 3,
    2, 1, 1, 0,
  ],
};
const DIST_N = DIST.error.length;
const DIST_PEAK =
  Math.max(...DIST.info.map((v, i) => v + DIST.warn[i] + DIST.error[i])) * 1.12;
// the brush selection covers the error-burst buckets (where error > 0), as x-fractions [0..1].
const BRUSH_FROM = 15.5 / DIST_N;
const BRUSH_TO = 26.5 / DIST_N;

// PAGE 2 flyout payoff - request context + GraphQL error + the .NET exception stack trace.
const GQL_ERROR = {
  message: "Unexpected Execution Error",
  code: "INTERNAL_SERVER_ERROR",
  path: "createOrder",
};
const EXCEPTION = {
  type: "System.InvalidOperationException",
  message: "Sequence contains no elements",
  topFrame: "OrderService.cs:line 87",
  subgraph: "orders",
  stack: [
    "at System.Linq.ThrowHelper.ThrowNoElementsException()",
    "at System.Linq.Enumerable.First[TSource](IEnumerable`1 source)",
    "at EShops.Orders.OrderService.ReserveInventoryAsync(CreateOrderInput input)",
    "    in /src/Orders/OrderService.cs:line 87",
    "at EShops.Orders.Mutations.CreateOrderAsync(CreateOrderInput input, CancellationToken ct)",
    "    in /src/Orders/Mutations.cs:line 42",
    "at HotChocolate.Resolvers.Expressions.ExpressionHelper.AwaitTaskHelper[T](Task`1 task)",
  ],
};

/* ── cursor targets (canvas px) ─────────────────────────────────────────────────────── */
// PAGE 0 Errors tile is bottom-right; the target error entry is its first row.
const ERR_TILE_LEFT_FRAC = 0.61; // Failed Operations spans 61%, Errors fills the rest
const ERRORS_TILE_X =
  RAIL +
  PANEL_PAD_X +
  (W - RAIL - PANEL_PAD_X * 2) *
    (ERR_TILE_LEFT_FRAC + (1 - ERR_TILE_LEFT_FRAC) / 2);
// vertical position of the Errors tile first row (see overview layout heights)
const OV_PAD_TOP = 16;
const LAT_TILE_H = 220;
const TP_ROW_H = 158;
const FAIL_ROW_H = 150;
const ERRORS_ROW0_Y =
  HEADER_H + OV_PAD_TOP + LAT_TILE_H + TILE_GAP + TP_ROW_H + TILE_GAP + 36 + 15;

// PAGE 1 "View logs" button - top-right of the error header.
const VIEW_LOGS_X = W - PANEL_PAD_X - 60;
const VIEW_LOGS_Y = HEADER_H + OV_PAD_TOP + 30;

// PAGE 2 distribution chart plot rect (canvas px) for the drag brush.
const CHART_CARD_TOP = HEADER_H + 16;
const CHART_PLOT_TOP = CHART_CARD_TOP + 14 + 22;
const CHART_PLOT_H = 170 - 14 - 22 - 14 - 14; // minus x-axis label band
const CHART_LEFT = RAIL + 16 + 14 + 30; // card pad + y-label gutter
const CHART_RIGHT = W - 16 - 14;
const CHART_W = CHART_RIGHT - CHART_LEFT;
const DRAG_FROM_X = CHART_LEFT + BRUSH_FROM * CHART_W;
const DRAG_TO_X = CHART_LEFT + BRUSH_TO * CHART_W;
const DRAG_Y = CHART_PLOT_TOP + CHART_PLOT_H * 0.5;

// PAGE 2 log list: pad 16 + chart card 170 + gap 12 = list card top. Failing row is index 0 once
// narrowed to the burst.
const LIST_TOP = HEADER_H + 16 + 170 + 12;
const ROW_H = 26;
const ROW_X = 380;
const failingRowY = LIST_TOP + ROW_H / 2;

const SeverityGlyph = ({ s, size = 13 }: { s: Severity; size?: number }) =>
  s === "error" ? (
    <IconErrorCircle size={size} color={DANGER} />
  ) : s === "warn" ? (
    <IconWarning size={size} color={token.warning} />
  ) : (
    <IconInfo size={size} color={token.info} />
  );

export interface DiagnoseScreenProps {
  progress: MotionValue<number>;
  active?: boolean;
}

export function DiagnoseScreen({ progress }: DiagnoseScreenProps) {
  // which PAGE is shown: 0 = Monitoring overview, 1 = Error screen, 2 = Logs view.
  const p1Mid = TL.at("p1Load", 0.5);
  const p2Mid = TL.at("p2Load", 0.5);
  const pageAt = (p: number) => (p >= p2Mid ? 2 : p >= p1Mid ? 1 : 0);
  const [page, setPage] = useState(() => pageAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => setPage(pageAt(p)));

  // ── PAGE TRANSITIONS - crossfade + slide, bridged by a short load spinner.
  const p0Opacity = useTransform(
    progress,
    [TL.start("p1Out"), TL.end("p1Out")],
    [1, 0],
    { ease: ease.inOut, clamp: true },
  );
  const p0X = useTransform(
    progress,
    [TL.start("p1Out"), TL.end("p1Out")],
    [0, -40],
    { ease: ease.inOut, clamp: true },
  );
  const p1Opacity = useTransform(
    progress,
    [TL.start("p1In"), TL.end("p1In"), TL.start("p2Out"), TL.end("p2Out")],
    [0, 1, 1, 0],
    { ease: ease.inOut, clamp: true },
  );
  const p1X = useTransform(
    progress,
    [TL.start("p1In"), TL.end("p1In"), TL.start("p2Out"), TL.end("p2Out")],
    [44, 0, 0, -40],
    { ease: ease.inOut, clamp: true },
  );
  const p2Opacity = useTransform(
    progress,
    [TL.start("p2In"), TL.end("p2In")],
    [0, 1],
    { ease: ease.inOut, clamp: true },
  );
  const p2X = useTransform(
    progress,
    [TL.start("p2In"), TL.end("p2In")],
    [44, 0],
    { ease: ease.inOut, clamp: true },
  );

  // the two bridging load spinners
  const load1Opacity = useTransform(
    progress,
    [
      TL.start("p1Out"),
      TL.at("p1Load", 0.1),
      TL.at("p1Load", 0.9),
      TL.end("p1In"),
    ],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const load1Rot = useTransform(
    progress,
    [TL.start("p1Out"), TL.end("p1In")],
    [0, 720],
  );
  const load2Opacity = useTransform(
    progress,
    [
      TL.start("p2Out"),
      TL.at("p2Load", 0.1),
      TL.at("p2Load", 0.9),
      TL.end("p2In"),
    ],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const load2Rot = useTransform(
    progress,
    [TL.start("p2Out"), TL.end("p2In")],
    [0, 720],
  );

  // ── CURSOR path across the three pages. The drag is a press → glide (DRAG_FROM → DRAG_TO).
  const cx = useTransform(
    progress,
    [
      TL.start("establish"),
      TL.start("moveToError"),
      TL.end("moveToError"),
      TL.start("p1Out"), // hold on the error entry through the click + transition out
      TL.end("p1In"), // arrive on the error screen
      TL.start("moveToViewLogs"),
      TL.end("moveToViewLogs"),
      TL.start("p2In"), // arrive on the logs view
      TL.start("moveToDrag"),
      TL.start("dragPress"),
      TL.end("dragSelect"), // brush drag end (left → right)
      TL.start("moveToRow"),
      TL.end("moveToRow"),
      1,
    ],
    [
      640,
      640,
      ERRORS_TILE_X,
      ERRORS_TILE_X,
      ERRORS_TILE_X,
      ERRORS_TILE_X,
      VIEW_LOGS_X,
      VIEW_LOGS_X,
      VIEW_LOGS_X,
      DRAG_FROM_X,
      DRAG_TO_X,
      DRAG_TO_X,
      ROW_X,
      ROW_X,
    ],
    { ease: ease.glide },
  );
  const cy = useTransform(
    progress,
    [
      TL.start("establish"),
      TL.start("moveToError"),
      TL.end("moveToError"),
      TL.start("p1Out"),
      TL.end("p1In"),
      TL.start("moveToViewLogs"),
      TL.end("moveToViewLogs"),
      TL.start("p2In"),
      TL.start("moveToDrag"),
      TL.start("dragPress"),
      TL.end("dragSelect"),
      TL.start("moveToRow"),
      TL.end("moveToRow"),
      1,
    ],
    [
      360,
      360,
      ERRORS_ROW0_Y,
      ERRORS_ROW0_Y,
      VIEW_LOGS_Y,
      VIEW_LOGS_Y,
      VIEW_LOGS_Y,
      VIEW_LOGS_Y,
      DRAG_Y,
      DRAG_Y,
      DRAG_Y,
      DRAG_Y,
      failingRowY,
      failingRowY,
    ],
    { ease: ease.glide },
  );

  return (
    <Stage
      width={W}
      height={H}
      fit="fill"
      chrome={false}
      ariaLabel="Nitro - from the gateway monitoring overview, clicking a production error to open an error-tracking screen with its occurrences, where-it-occurs and server stack trace, then opening the logs view, drag-selecting the error burst on the log distribution chart, and drilling the failing log into a root-cause stack trace"
      overlay={
        <Cursor
          x={cx}
          y={cy}
          progress={progress}
          clickTimes={[
            TL.start("errorClick"),
            TL.start("viewLogsClick"),
            TL.start("rowClick"),
          ]}
          pointerWindows={[
            [TL.start("errorHover"), TL.start("errorClick") + 0.02],
            [TL.start("viewLogsHover"), TL.start("viewLogsClick") + 0.02],
            // hand stays pressed through the drag brush
            [TL.start("dragPress"), TL.end("dragSelect")],
            [TL.start("rowHover"), TL.start("rowClick") + 0.02],
          ]}
        />
      }
    >
      <AppFrame railActive="documents" footerCounts={[1, 0, 2]}>
        <div
          style={{
            position: "absolute",
            inset: 0,
            display: "flex",
            flexDirection: "column",
          }}
        >
          {/* gateway chrome - the "EShops Gateway" doc-tab strip + view-nav (Monitoring/Logs active,
              matching the other tabs), then the "Production Stage" sub-header. */}
          <GatewayChrome activeView={page === 2 ? "Logs" : "Monitoring"} />
          <ProductionStageHeader />

          {/* the page viewport - holds the three crossfading pages */}
          <div
            style={{
              flex: 1,
              minHeight: 0,
              position: "relative",
              overflow: "hidden",
            }}
          >
            {/* PAGE 0 - Monitoring overview */}
            <motion.div
              style={{
                position: "absolute",
                inset: 0,
                overflow: "hidden",
                opacity: p0Opacity,
                x: p0X,
                display: page === 0 ? "block" : "none",
              }}
            >
              <MonitoringOverview progress={progress} />
            </motion.div>

            {/* PAGE 1 - Error screen */}
            <motion.div
              style={{
                position: "absolute",
                inset: 0,
                overflow: "hidden",
                opacity: p1Opacity,
                x: p1X,
                display: page === 1 ? "block" : "none",
              }}
            >
              <ErrorScreen progress={progress} />
            </motion.div>

            {/* PAGE 2 - Logs view */}
            <motion.div
              style={{
                position: "absolute",
                inset: 0,
                overflow: "hidden",
                opacity: p2Opacity,
                x: p2X,
                display: page === 2 ? "block" : "none",
              }}
            >
              <LogsView progress={progress} />
            </motion.div>

            {/* bridging load spinners */}
            <BridgeSpinner opacity={load1Opacity} rot={load1Rot} />
            <BridgeSpinner opacity={load2Opacity} rot={load2Rot} />
          </div>
        </div>

        {/* Log Detail flyout - slides in on the failing-row click; General tab carries the payoff. */}
        <Flyout
          progress={progress}
          show={TL.start("flyoutLoad")}
          hide={2}
          title="Log Detail"
          tabs={["General", "Attributes"]}
          activeTab="General"
          indicatorColor={ORANGE}
        >
          <LogDetail progress={progress} />
        </Flyout>
      </AppFrame>
    </Stage>
  );
}

function BridgeSpinner({
  opacity,
  rot,
}: {
  opacity: MotionValue<number>;
  rot: MotionValue<number>;
}) {
  return (
    <motion.div
      style={{
        position: "absolute",
        inset: 0,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        opacity,
        pointerEvents: "none",
        background: token.surface,
      }}
    >
      <motion.div style={{ rotate: rot, display: "flex" }}>
        <IconSpinner size={30} color={token.accent} />
      </motion.div>
    </motion.div>
  );
}

/* ── shared gateway chrome ──────────────────────────────────────────────────────────── */

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

/* ── PAGE 0 - Monitoring overview ───────────────────────────────────────────────────── */

function MonitoringOverview({ progress }: { progress: MotionValue<number> }) {
  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        padding: `${OV_PAD_TOP}px ${PANEL_PAD_X}px 0`,
        display: "flex",
        flexDirection: "column",
        gap: TILE_GAP,
        overflow: "hidden",
      }}
    >
      {/* Row 1 - Latency, FULL WIDTH */}
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
        />
      </Tile>

      {/* Row 2 - Throughput (area) + Clients (request bars) */}
      <div style={{ display: "flex", gap: TILE_GAP, height: TP_ROW_H }}>
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

      {/* Row 3 - Failed Operations (red line) + Errors (recent error list) */}
      <div style={{ display: "flex", gap: TILE_GAP, height: FAIL_ROW_H }}>
        <div style={{ flex: "0 0 61%", minWidth: 0, display: "flex" }}>
          <Tile title="Failed Operations">
            <AreaLineChart
              values={OVERVIEW.failed}
              color={token.chP95}
              legendLabel="createOrder"
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
            <ErrorList progress={progress} />
          </Tile>
        </div>
      </div>

      {/* Row 4 - Insights, FULL WIDTH */}
      <InsightsTable />
    </div>
  );
}

function MultiLineChart({
  series,
}: {
  series: { values: number[]; color: string; label: string }[];
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
          {series.map((s, i) => {
            const pts: Pt[] = s.values.map((v, j) => [
              (j / (s.values.length - 1)) * 100,
              100 - (v / max) * 90 - 5,
            ]);
            return (
              <path
                key={i}
                d={smoothLinePath(pts, 0.7)}
                fill="none"
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
                stroke={s.color}
                strokeWidth={1.4}
              />
            );
          })}
        </svg>
      </div>
    </div>
  );
}

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

// hot→cold request-volume ramp (amber → blue). Token values are CSS vars (not parseable hex), so
// the bar color is picked from real tokens per rank rather than via lerpColor on a var() string.
const CLIENT_RAMP = [
  token.chImpact,
  token.chP99,
  token.chLatency,
  token.chThroughput,
  token.accent,
];

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
        const color = CLIENT_RAMP[Math.min(i, CLIENT_RAMP.length - 1)];
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

/** The Errors tile - recent errors. The cursor clicks the createOrder error (data-testid). */
function ErrorList({ progress }: { progress: MotionValue<number> }) {
  return (
    <div
      style={{
        position: "absolute",
        inset: "4px 6px 6px",
        display: "flex",
        flexDirection: "column",
        overflow: "hidden",
      }}
    >
      {ERRORS.map((e, i) => (
        <ErrorRow
          key={i}
          e={e}
          last={i === ERRORS.length - 1}
          progress={progress}
        />
      ))}
    </div>
  );
}

function ErrorRow({
  e,
  last,
  progress,
}: {
  e: ErrorEntry;
  last: boolean;
  progress: MotionValue<number>;
}) {
  // the target error highlights as the cursor hovers it.
  const bg = useTransform(progress, (p): string =>
    e.target && p >= TL.start("errorHover") ? token.highlight : "transparent",
  );
  return (
    <motion.div
      data-testid={e.target ? "error-entry" : undefined}
      style={{
        display: "flex",
        alignItems: "flex-start",
        gap: 8,
        padding: "6px 8px",
        borderRadius: 4,
        background: bg,
        borderBottom: last ? "none" : `1px solid ${token.grid}`,
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
          <span style={{ fontFamily: token.mono, color: token.errorText }}>
            {e.type}
          </span>
          : {e.msg}
        </span>
      </span>
    </motion.div>
  );
}

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
          Insights
        </span>
        <span style={{ fontSize: 11.5, color: token.textSecondary }}>
          operations · sorted by error rate
        </span>
      </div>
      <div
        style={{
          height: 28,
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
        <span style={{ flex: 1 }}>Operation</span>
        <span style={{ flex: "0 0 120px", textAlign: "right" }}>
          Throughput
        </span>
        <span style={{ flex: "0 0 110px", textAlign: "right" }}>p95</span>
        <span style={{ flex: "0 0 110px", textAlign: "right" }}>
          Error Rate
        </span>
      </div>
      {OPS.map((o) => (
        <div
          key={o.name}
          style={{
            height: 34,
            display: "flex",
            alignItems: "center",
            padding: "0 14px",
            borderBottom: `1px solid ${token.grid}`,
            background: o.failing ? token.highlight : "transparent",
          }}
        >
          <span
            style={{ flex: 1, display: "flex", alignItems: "center", gap: 9 }}
          >
            {o.kind === "mutation" ? (
              <IconMutation size={14} />
            ) : (
              <IconQuery size={14} />
            )}
            <span
              style={{
                fontSize: 13,
                color: o.failing ? token.textStrong : token.blue,
                fontWeight: o.failing ? 600 : 400,
              }}
            >
              {o.name}
            </span>
          </span>
          <span
            style={{
              flex: "0 0 120px",
              textAlign: "right",
              fontSize: 12.5,
              fontFamily: token.mono,
              color: token.text,
            }}
          >
            {o.opm}
          </span>
          <span
            style={{
              flex: "0 0 110px",
              textAlign: "right",
              fontSize: 12.5,
              fontFamily: token.mono,
              color: token.text,
            }}
          >
            {o.p95}
          </span>
          <span
            style={{
              flex: "0 0 110px",
              textAlign: "right",
              fontSize: 12.5,
              fontFamily: token.mono,
              fontWeight: o.failing ? 700 : 400,
              color: o.failing ? token.errorText : token.text,
            }}
          >
            {o.errorRate}
          </span>
        </div>
      ))}
    </div>
  );
}

/* ── PAGE 1 - Error screen (Sentry / ELK style) ─────────────────────────────────────── */

function ErrorScreen({ progress }: { progress: MotionValue<number> }) {
  const chartOp = useTransform(
    progress,
    [TL.start("errChart"), TL.at("errChart", 0.3)],
    [0, 1],
    { clamp: true },
  );
  const whereOp = useTransform(
    progress,
    [TL.start("errWhere"), TL.at("errWhere", 0.3)],
    [0, 1],
    { clamp: true },
  );
  const stackOp = useTransform(
    progress,
    [TL.start("errStack"), TL.at("errStack", 0.3)],
    [0, 1],
    { clamp: true },
  );

  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        padding: `${OV_PAD_TOP}px ${PANEL_PAD_X}px 16px`,
        display: "flex",
        flexDirection: "column",
        gap: TILE_GAP,
        overflow: "hidden",
      }}
    >
      {/* header - exception type + message + createOrder badge + INTERNAL_SERVER_ERROR code + View logs */}
      <div
        style={{
          flex: "0 0 auto",
          display: "flex",
          alignItems: "flex-start",
          gap: 12,
        }}
      >
        <span style={{ display: "flex", color: token.error, marginTop: 3 }}>
          <IconErrorCircle size={22} />
        </span>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 10,
              flexWrap: "wrap",
            }}
          >
            <span
              style={{
                fontSize: 18,
                fontWeight: 700,
                color: token.textStrong,
                fontFamily: token.mono,
              }}
            >
              {EXCEPTION.type}
            </span>
            <Pill text={FAILING_OP} icon={<IconMutation size={11} />} />
            <Pill text={GQL_ERROR.code} danger />
          </div>
          <div style={{ fontSize: 13.5, color: token.text, marginTop: 4 }}>
            {EXCEPTION.message}
          </div>
        </div>
        {/* View logs action - the cursor clicks this */}
        <ViewLogsButton progress={progress} />
      </div>

      {/* occurrences bar chart + stat tiles */}
      <motion.div
        style={{
          flex: "0 0 auto",
          display: "flex",
          gap: TILE_GAP,
          height: 168,
          opacity: chartOp,
        }}
      >
        <div style={{ flex: "0 0 58%", minWidth: 0, display: "flex" }}>
          <Tile
            title="Occurrences"
            headerExtra={
              <span style={{ fontSize: 11, color: token.textSecondary }}>
                events / hour · last 24h
              </span>
            }
          >
            <OccurrenceBars progress={progress} />
          </Tile>
        </div>
        <div
          style={{
            flex: 1,
            minWidth: 0,
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gridTemplateRows: "1fr 1fr",
            gap: TILE_GAP,
          }}
        >
          <StatTile label="Total events" value="1,204" emphasize />
          <StatTile label="Affected clients" value="38" />
          <StatTile label="First seen" value="6h ago" />
          <StatTile label="Last seen" value="just now" />
        </div>
      </motion.div>

      {/* where it occurs + example traces */}
      <motion.div
        style={{
          flex: "0 0 auto",
          display: "flex",
          gap: TILE_GAP,
          height: 168,
          opacity: whereOp,
        }}
      >
        <div style={{ flex: "0 0 38%", minWidth: 0, display: "flex" }}>
          <Tile title="Where it occurs">
            <WhereItOccurs />
          </Tile>
        </div>
        <div style={{ flex: 1, minWidth: 0, display: "flex" }}>
          <Tile
            title="Example traces"
            headerExtra={
              <span style={{ fontSize: 11, color: token.textSecondary }}>
                recent samples
              </span>
            }
          >
            <ExampleTraces />
          </Tile>
        </div>
      </motion.div>

      {/* server stack trace */}
      <motion.div
        style={{ flex: 1, minHeight: 0, display: "flex", opacity: stackOp }}
      >
        <Tile
          title="Server Stack Trace"
          headerExtra={
            <span
              style={{
                fontSize: 11,
                fontFamily: token.mono,
                color: token.errorText,
              }}
            >
              {EXCEPTION.type}: {EXCEPTION.message}
            </span>
          }
        >
          <ErrorStackTrace progress={progress} />
        </Tile>
      </motion.div>
    </div>
  );
}

function ViewLogsButton({ progress }: { progress: MotionValue<number> }) {
  const press = useTransform(
    progress,
    [
      TL.start("viewLogsClick"),
      TL.at("viewLogsClick", 0.5),
      TL.end("viewLogsClick"),
    ],
    [1, 0.94, 1],
    { clamp: true },
  );
  const bg = useTransform(progress, (p): string =>
    p >= TL.start("viewLogsHover") ? token.accent : "transparent",
  );
  const fg = useTransform(progress, (p): string =>
    p >= TL.start("viewLogsHover") ? "#fff" : token.text,
  );
  return (
    <motion.span
      data-testid="view-logs"
      style={{
        flex: "0 0 auto",
        display: "flex",
        alignItems: "center",
        gap: 7,
        height: 30,
        padding: "0 12px",
        borderRadius: 5,
        border: `1px solid ${token.borderStrong}`,
        background: bg,
        color: fg,
        fontSize: 13,
        fontWeight: 600,
        scale: press,
      }}
    >
      View logs <IconChevronRight size={13} color="currentColor" />
    </motion.span>
  );
}

function Pill({
  text,
  icon,
  danger,
}: {
  text: string;
  icon?: React.ReactNode;
  danger?: boolean;
}) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 5,
        fontSize: 11,
        fontWeight: 600,
        padding: "2px 8px",
        borderRadius: 4,
        fontFamily: token.mono,
        background: danger ? "rgba(207,34,46,0.14)" : token.bg,
        border: `1px solid ${danger ? DANGER : token.border}`,
        color: danger ? token.errorText : token.text,
      }}
    >
      {icon}
      {text}
    </span>
  );
}

function StatTile({
  label,
  value,
  emphasize,
}: {
  label: string;
  value: string;
  emphasize?: boolean;
}) {
  return (
    <div
      style={{
        background: token.card,
        border: `1px solid ${token.border}`,
        borderRadius: 8,
        padding: "10px 14px",
        display: "flex",
        flexDirection: "column",
        justifyContent: "center",
        minWidth: 0,
      }}
    >
      <span
        style={{
          fontSize: 11,
          color: token.textSecondary,
          whiteSpace: "nowrap",
        }}
      >
        {label}
      </span>
      <span
        style={{
          fontSize: 20,
          fontWeight: 700,
          fontFamily: token.mono,
          color: emphasize ? token.errorText : token.textStrong,
          marginTop: 2,
        }}
      >
        {value}
      </span>
    </div>
  );
}

/** Occurrences-over-time histogram - calm baseline then a sharp spike, bars sweeping in. */
function OccurrenceBars({ progress }: { progress: MotionValue<number> }) {
  const grow = useTransform(
    progress,
    [TL.at("errChart", 0.1), TL.at("errChart", 0.6)],
    [0, 1],
    { clamp: true },
  );
  return (
    <div
      style={{
        position: "absolute",
        inset: "12px 14px 10px",
        display: "flex",
        alignItems: "flex-end",
        gap: 2,
      }}
    >
      {OCCUR.map((v, i) => {
        const h = (v / OCCUR_PEAK) * 100;
        const isSpike = v > 12;
        return (
          <motion.span
            key={i}
            style={{
              flex: 1,
              height: `${h}%`,
              transformOrigin: "bottom",
              scaleY: grow,
              background: isSpike ? DANGER : token.chThroughput,
              opacity: isSpike ? 0.92 : 0.55,
              borderRadius: "2px 2px 0 0",
            }}
          />
        );
      })}
    </div>
  );
}

function WhereItOccurs() {
  const rows: [React.ReactNode, string, string][] = [
    [<IconMutation key="op" size={13} />, "Operation", SPAN_NAME],
    [
      <IconServer key="sub" size={13} />,
      "Subgraph",
      `${EXCEPTION.subgraph} service`,
    ],
    [<IconDatabase key="frame" size={13} />, "Top frame", EXCEPTION.topFrame],
  ];
  return (
    <div
      style={{
        position: "absolute",
        inset: "8px 14px 10px",
        display: "flex",
        flexDirection: "column",
        justifyContent: "space-around",
      }}
    >
      {rows.map(([icon, label, value], i) => (
        <div
          key={i}
          style={{
            display: "flex",
            alignItems: "center",
            gap: 9,
            padding: "4px 0",
            borderBottom:
              i < rows.length - 1 ? `1px solid ${token.grid}` : "none",
          }}
        >
          <span
            style={{
              display: "flex",
              color: token.textSecondary,
              flex: "0 0 auto",
            }}
          >
            {icon}
          </span>
          <span
            style={{
              flex: "0 0 72px",
              fontSize: 11.5,
              color: token.textSecondary,
            }}
          >
            {label}
          </span>
          <span
            style={{
              flex: 1,
              minWidth: 0,
              fontSize: 12.5,
              fontFamily: token.mono,
              color: token.textStrong,
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
              textAlign: "right",
            }}
          >
            {value}
          </span>
        </div>
      ))}
    </div>
  );
}

function ExampleTraces() {
  return (
    <div
      style={{
        position: "absolute",
        inset: "2px 6px 6px",
        display: "flex",
        flexDirection: "column",
        overflow: "hidden",
      }}
    >
      {TRACES.map((t, i) => (
        <div
          key={t.id}
          style={{
            display: "flex",
            alignItems: "center",
            gap: 10,
            height: 30,
            padding: "0 8px",
            borderRadius: 4,
            borderBottom:
              i < TRACES.length - 1 ? `1px solid ${token.grid}` : "none",
          }}
        >
          <span
            style={{
              display: "flex",
              flex: "0 0 auto",
              color: t.status === "error" ? token.error : token.successText,
            }}
          >
            {t.status === "error" ? (
              <IconErrorCircle size={12} />
            ) : (
              <IconInfo size={12} />
            )}
          </span>
          <span
            style={{
              flex: 1,
              minWidth: 0,
              fontSize: 12,
              fontFamily: token.mono,
              color: token.blue,
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
            }}
          >
            {t.id}
          </span>
          <span
            style={{
              flex: "0 0 auto",
              fontSize: 11.5,
              fontFamily: token.mono,
              color: token.textSecondary,
            }}
          >
            {t.time}
          </span>
          <span
            style={{
              flex: "0 0 56px",
              textAlign: "right",
              fontSize: 11.5,
              fontFamily: token.mono,
              color: token.text,
            }}
          >
            {t.duration}
          </span>
          <span
            style={{ display: "flex", flex: "0 0 auto", color: token.textDim }}
          >
            <IconChevronRight size={13} />
          </span>
        </div>
      ))}
    </div>
  );
}

function ErrorStackTrace({ progress }: { progress: MotionValue<number> }) {
  const frames = EXCEPTION.stack;
  const failingIdx = frames.findIndex((s) => s.includes("OrderService.cs"));
  const fileLine = failingIdx >= 0 ? failingIdx + 1 : -1;
  const frameBg = useTransform(
    progress,
    [TL.at("errStack", 0.5), TL.end("errStack")],
    ["rgba(0,0,0,0)", "rgba(207,34,46,0.12)"],
    { clamp: true },
  );
  const frameBorder = useTransform(
    progress,
    [TL.at("errStack", 0.5), TL.end("errStack")],
    ["rgba(0,0,0,0)", DANGER],
    { clamp: true },
  );
  return (
    <div
      style={{
        position: "absolute",
        inset: "12px 14px 10px",
        overflow: "hidden",
        fontFamily: token.mono,
        fontSize: 11.5,
        lineHeight: "18px",
        color: token.text,
        whiteSpace: "pre",
      }}
    >
      {frames.map((f, i) => {
        const emphasized = i === failingIdx || i === fileLine;
        if (emphasized) {
          return (
            <motion.div
              key={i}
              style={{
                background: frameBg,
                borderLeft: "2px solid",
                borderColor: frameBorder,
                margin: "0 -14px",
                padding: "0 14px 0 12px",
                color: token.textStrong,
              }}
            >
              {f}
            </motion.div>
          );
        }
        return <div key={i}>{f}</div>;
      })}
    </div>
  );
}

/* ── PAGE 2 - Logs view (real Nitro Logs view) ──────────────────────────────────────── */

function LogsView({ progress }: { progress: MotionValue<number> }) {
  const contentOpacity = useTransform(
    progress,
    [TL.start("logsReveal"), TL.at("logsReveal", 0.6)],
    [0, 1],
    { clamp: true },
  );

  // narrowed to the error burst once the brush refetch lands
  const narrowedAt = (p: number) => p >= TL.end("brushLoad");
  const [narrowed, setNarrowed] = useState(() => narrowedAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => setNarrowed(narrowedAt(p)));
  const rows = narrowed ? BURST_ROWS : LOG_ROWS;

  // the list dims during the refetch
  const listDim = useTransform(progress, (p): number =>
    p >= TL.start("dragRelease") && p <= TL.end("brushLoad") ? 0.4 : 1,
  );

  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        padding: 16,
        display: "flex",
        flexDirection: "column",
        gap: 12,
      }}
    >
      <motion.div
        style={{
          display: "flex",
          flexDirection: "column",
          gap: 12,
          height: "100%",
          opacity: contentOpacity,
        }}
      >
        {/* Log Distribution BAR chart card */}
        <div
          style={{
            flex: "0 0 auto",
            height: 170,
            background: token.card,
            border: `1px solid ${token.borderStrong}`,
            borderRadius: 8,
            padding: 14,
            display: "flex",
            flexDirection: "column",
          }}
        >
          <div
            style={{ display: "flex", alignItems: "center", marginBottom: 8 }}
          >
            <span
              style={{ fontSize: 14, fontWeight: 600, color: token.textStrong }}
            >
              Log Distribution
            </span>
            <div
              style={{
                marginLeft: "auto",
                display: "flex",
                gap: 14,
                fontSize: 11,
                color: token.textSecondary,
              }}
            >
              <Legend color={token.info} label="Info" />
              <Legend color={token.warning} label="Warn" />
              <Legend color={DANGER} label="Error" />
            </div>
          </div>
          <DistributionBars progress={progress} />
        </div>

        {/* log list card - real structure: no toolbar, no headers, just the rows */}
        <div
          style={{
            flex: 1,
            minHeight: 0,
            background: token.card,
            border: `1px solid ${token.borderStrong}`,
            borderRadius: 8,
            overflow: "hidden",
            display: "flex",
            flexDirection: "column",
          }}
        >
          <div
            style={{
              flex: 1,
              minHeight: 0,
              position: "relative",
              overflow: "hidden",
            }}
          >
            <ListLoadingBar
              progress={progress}
              window={[TL.start("brushLoad"), TL.end("brushLoad")]}
            />
            <motion.div style={{ opacity: listDim }}>
              {rows.map((r) => (
                <LogListRow key={r.ts} row={r} progress={progress} />
              ))}
            </motion.div>
          </div>
        </div>
      </motion.div>
    </div>
  );
}

function Legend({ color, label }: { color: string; label: string }) {
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 5 }}>
      <span
        style={{ width: 9, height: 9, borderRadius: 2, background: color }}
      />
      {label}
    </span>
  );
}

/**
 * Log Distribution - a stacked-BAR histogram of log volume over time (info baseline + warn + the
 * error burst), like the real LogDistributionChart but rendered as discrete bars per the brief. A
 * brush rectangle is DRAWN by the cursor's drag-select: its left edge sits at DRAG_FROM and its
 * width grows over the dragSelect stage to DRAG_TO (then it stays as the active time range).
 */
function DistributionBars({ progress }: { progress: MotionValue<number> }) {
  // bars sweep up over logsReveal
  const grow = useTransform(
    progress,
    [TL.start("logsReveal"), TL.at("logsReveal", 0.7)],
    [0, 1],
    { clamp: true },
  );

  // brush rectangle - left fixed at BRUSH_FROM; width grows 0 → full over dragSelect, then holds.
  const brushLeftPct = BRUSH_FROM * 100;
  const brushFullPct = (BRUSH_TO - BRUSH_FROM) * 100;
  const brushOpacity = useTransform(
    progress,
    [TL.start("dragPress"), TL.at("dragPress", 0.6)],
    [0, 1],
    { clamp: true },
  );
  const brushWidthPct = useTransform(
    progress,
    [TL.start("dragSelect"), TL.end("dragSelect")],
    [0, brushFullPct],
    { ease: ease.inOut, clamp: true },
  );
  const brushWidth = useTransform(brushWidthPct, (w) => `${w}%`);

  return (
    <div
      style={{
        position: "relative",
        flex: 1,
        minHeight: 0,
        display: "flex",
        flexDirection: "column",
      }}
    >
      <div style={{ position: "relative", flex: 1, minHeight: 0 }}>
        {/* y baseline grid */}
        {[0.5, 1].map((fr) => (
          <div
            key={fr}
            style={{
              position: "absolute",
              left: 30,
              right: 0,
              top: `${(1 - fr) * 100}%`,
              borderTop: `1px solid ${token.grid}`,
            }}
          />
        ))}
        {/* the plot area (bars + brush) sits right of the 30px y-gutter */}
        <div
          style={{
            position: "absolute",
            left: 30,
            right: 0,
            top: 0,
            bottom: 0,
            display: "flex",
            alignItems: "flex-end",
            gap: 3,
          }}
        >
          {DIST.info.map((_, i) => {
            const tot = DIST.info[i] + DIST.warn[i] + DIST.error[i];
            const totH = (tot / DIST_PEAK) * 100;
            const infoH = tot ? (DIST.info[i] / tot) * 100 : 0;
            const warnH = tot ? (DIST.warn[i] / tot) * 100 : 0;
            const errH = tot ? (DIST.error[i] / tot) * 100 : 0;
            return (
              <motion.span
                key={i}
                style={{
                  flex: 1,
                  height: `${totH}%`,
                  transformOrigin: "bottom",
                  scaleY: grow,
                  display: "flex",
                  flexDirection: "column",
                  borderRadius: "2px 2px 0 0",
                  overflow: "hidden",
                }}
              >
                {errH > 0 && (
                  <span style={{ height: `${errH}%`, background: DANGER }} />
                )}
                {warnH > 0 && (
                  <span
                    style={{ height: `${warnH}%`, background: token.warning }}
                  />
                )}
                <span
                  style={{
                    height: `${infoH}%`,
                    background: token.info,
                    opacity: 0.85,
                  }}
                />
              </motion.span>
            );
          })}
          {/* the drag-select brush rectangle (left fixed, width driven by the dragSelect stage) */}
          <motion.div
            style={{
              position: "absolute",
              top: 0,
              bottom: 0,
              left: `${brushLeftPct}%`,
              width: brushWidth,
              opacity: brushOpacity,
              pointerEvents: "none",
            }}
          >
            <span
              style={{
                position: "absolute",
                inset: 0,
                background: ORANGE,
                opacity: 0.16,
              }}
            />
            <span
              style={{
                position: "absolute",
                top: 0,
                bottom: 0,
                left: 0,
                width: 1.5,
                background: ORANGE,
              }}
            />
            <span
              style={{
                position: "absolute",
                top: 0,
                bottom: 0,
                right: 0,
                width: 1.5,
                background: ORANGE,
              }}
            />
          </motion.div>
        </div>
      </div>
      {/* x-axis time labels */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          paddingLeft: 30,
          marginTop: 4,
          fontSize: 9,
          fontFamily: token.mono,
          color: token.textDim,
        }}
      >
        <span>14:31:50</span>
        <span>14:32:00</span>
        <span>14:32:10</span>
        <span>14:32:20</span>
      </div>
    </div>
  );
}

function ListLoadingBar({
  progress,
  window,
}: {
  progress: MotionValue<number>;
  window: [number, number];
}) {
  const [a, b] = window;
  const opacity = useTransform(progress, (p): number =>
    p >= a && p <= b ? 1 : 0,
  );
  const left = useTransform(progress, (p): string =>
    p >= a && p <= b ? `${((p - a) / (b - a)) * 130 - 30}%` : "-30%",
  );
  return (
    <motion.div
      style={{
        position: "absolute",
        top: 0,
        left: 0,
        right: 0,
        height: 2,
        overflow: "hidden",
        background: token.grid,
        opacity,
        zIndex: 2,
      }}
    >
      <motion.div
        style={{
          position: "absolute",
          top: 0,
          bottom: 0,
          width: "34%",
          left,
          background: token.accentHover,
        }}
      />
    </motion.div>
  );
}

/** A real Nitro log row: [severity icon][ISO timestamp][body], 26px tall, Fira Code 13px/20px. */
function LogListRow({
  row,
  progress,
}: {
  row: LogRow;
  progress: MotionValue<number>;
}) {
  const target = row.ts === FAILING_TS;
  const bg = useTransform(progress, (p): string =>
    target && p >= TL.start("rowHover") ? token.highlight : "transparent",
  );
  return (
    <motion.div
      data-testid={target ? "failing-row" : undefined}
      style={{
        display: "flex",
        alignItems: "center",
        height: ROW_H,
        padding: "0 8px",
        background: bg,
        fontFamily: token.mono,
        fontSize: 13,
        fontWeight: 500,
        lineHeight: "20px",
        color: token.text,
      }}
    >
      <span
        style={{
          flex: "0 0 20px",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <SeverityGlyph s={row.severity} size={13} />
      </span>
      <span
        style={{
          flex: "0 0 175px",
          padding: "0 6px",
          textAlign: "right",
          whiteSpace: "nowrap",
          color: token.textSecondary,
        }}
      >
        {row.ts}
      </span>
      <span
        style={{
          flex: "1 1 auto",
          padding: "0 6px",
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis",
        }}
      >
        {row.message}
      </span>
    </motion.div>
  );
}

/* ── Log Detail flyout body ─────────────────────────────────────────────────────────── */

function LogDetail({ progress }: { progress: MotionValue<number> }) {
  const bodyOpacity = useTransform(
    progress,
    [TL.start("contextReveal") - 0.01, TL.start("contextReveal")],
    [1, 0],
    { clamp: true },
  );
  const richOpacity = useTransform(
    progress,
    [TL.start("contextReveal"), TL.at("contextReveal", 0.2)],
    [0, 1],
    { clamp: true },
  );

  return (
    <div style={{ display: "flex", flexDirection: "column", height: "100%" }}>
      {/* FlyoutContentHeader - severity icon + severity text + full datetime */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          paddingBottom: 12,
          marginBottom: 12,
          borderBottom: `1px solid ${token.border}`,
          color: token.textSecondary,
        }}
      >
        <IconErrorCircle size={15} color={DANGER} />
        <span style={{ fontSize: 15, fontWeight: 600, color: token.errorText }}>
          Error
        </span>
        <span
          style={{
            marginLeft: "auto",
            fontSize: 12,
            fontFamily: token.mono,
            color: token.textSecondary,
          }}
        >
          {FAILING_TS}
        </span>
      </div>

      {/* operation identity */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 7,
          marginBottom: 12,
        }}
      >
        <IconMutation size={14} />
        <span style={{ fontSize: 13, color: token.textStrong }}>
          {SPAN_NAME}
        </span>
        <Badge text="142 ms" />
      </div>

      <motion.div style={{ position: "relative", flex: 1, minHeight: 0 }}>
        {/* raw log body - visible until the rich context reveals */}
        <motion.div
          style={{ position: "absolute", inset: 0, opacity: bodyOpacity }}
        >
          <div
            style={{
              fontSize: 12,
              color: token.textSecondary,
              marginBottom: 6,
            }}
          >
            Message
          </div>
          <div
            style={{
              background: token.bg,
              border: `1px solid ${token.border}`,
              borderRadius: 4,
              padding: "8px 10px",
              fontFamily: token.mono,
              fontSize: 11.5,
              lineHeight: "17px",
              color: token.text,
              whiteSpace: "pre-wrap",
            }}
          >
            Unhandled exception while executing request - Sequence contains no
            elements
          </div>
        </motion.div>

        {/* request context + GraphQL error + server stack trace (the payoff) */}
        <motion.div
          style={{
            position: "absolute",
            inset: 0,
            overflow: "hidden",
            opacity: richOpacity,
          }}
        >
          <TableList
            title="Request"
            rows={[{ label: "Operation", value: SPAN_NAME, mono: true }]}
            progress={progress}
            playWindow={[
              TL.start("contextReveal"),
              TL.at("contextReveal", 0.5),
            ]}
          />
          <TableList
            title="GraphQL Error"
            rows={[
              { label: "Message", value: GQL_ERROR.message },
              { label: "Path", value: GQL_ERROR.path, mono: true },
              {
                label: "Code",
                value: GQL_ERROR.code,
                mono: true,
                color: token.errorText,
              },
            ]}
            progress={progress}
            playWindow={[TL.at("contextReveal", 0.5), TL.end("contextReveal")]}
          />
          <TableList
            title={`Exception: ${EXCEPTION.type}`}
            titleColor={token.errorText}
            rows={[{ label: "Message", value: EXCEPTION.message, mono: true }]}
            progress={progress}
            playWindow={[TL.start("stackReveal"), TL.at("stackReveal", 0.4)]}
          />
          <StackTrace progress={progress} />
        </motion.div>
      </motion.div>
    </div>
  );
}

function StackTrace({ progress }: { progress: MotionValue<number> }) {
  const frames = EXCEPTION.stack;
  const failingIdx = frames.findIndex((s) => s.includes("OrderService.cs"));
  const fileLine = failingIdx >= 0 ? failingIdx + 1 : -1;
  const opacity = useTransform(
    progress,
    [TL.at("stackReveal", 0.4), TL.end("stackReveal")],
    [0, 1],
    { clamp: true },
  );
  const frameBg = useTransform(
    progress,
    TL.span("rootCause"),
    ["rgba(0,0,0,0)", "rgba(207,34,46,0.12)"],
    { clamp: true },
  );
  const frameBorder = useTransform(
    progress,
    TL.span("rootCause"),
    ["rgba(0,0,0,0)", DANGER],
    { clamp: true },
  );

  return (
    <motion.div style={{ marginBottom: 14, opacity }}>
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          fontSize: 13,
          fontWeight: 600,
          color: token.textStrong,
          marginBottom: 6,
        }}
      >
        Server Stack Trace
      </div>
      <div
        style={{
          background: token.bg,
          border: `1px solid ${token.border}`,
          borderRadius: 4,
          padding: "8px 10px",
          fontFamily: token.mono,
          fontSize: 11.5,
          lineHeight: "17px",
          color: token.text,
          whiteSpace: "pre",
          overflow: "hidden",
        }}
      >
        {frames.map((f, i) => {
          const emphasized = i === failingIdx || i === fileLine;
          if (emphasized) {
            return (
              <motion.div
                key={i}
                style={{
                  background: frameBg,
                  borderLeft: "2px solid",
                  borderColor: frameBorder,
                  margin: "0 -10px",
                  padding: "0 10px 0 8px",
                  color: token.textStrong,
                }}
              >
                {f}
              </motion.div>
            );
          }
          return <div key={i}>{f}</div>;
        })}
      </div>
    </motion.div>
  );
}

function Badge({ text }: { text: string }) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        fontSize: 11,
        fontWeight: 500,
        padding: "2px 7px",
        borderRadius: 4,
        background: token.bg,
        border: `1px solid ${token.border}`,
        color: token.textSecondary,
        fontFamily: token.mono,
      }}
    >
      {text}
    </span>
  );
}
