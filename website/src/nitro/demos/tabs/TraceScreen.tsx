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
import { PanelTile } from "../../primitives/PanelTile";
import { UnderlineTab } from "../../primitives/UnderlineTab";
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

const ORANGE = token.graphEdgeActive;

const PAL = {
  http: token.chThroughput,
  graphql: token.chP95,
  internal: token.chLatency,
  db: token.chImpact,
};

type SpanKind = "server" | "graphql" | "internal" | "http" | "db";

interface Span {
  id: string;
  name: string;
  kind: SpanKind;
  depth: number;
  startMs: number;
  durationMs: number;
  rank: number;
  hasChildren?: boolean;
  target?: boolean;
}

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
  impact: number;
  slow?: boolean;
  seed: number;
}

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

const CLIENTS = [
  { label: "Web Storefront", value: 128540 },
  { label: "iOS App", value: 86220 },
  { label: "Admin Dashboard", value: 41380 },
  { label: "Mobile Web", value: 27910 },
  { label: "Partner API", value: 9240 },
];
const CLIENTS_MAX = Math.max(...CLIENTS.map((c) => c.value));

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

const OPSERIES = {
  latency: smoothSeries(51, 48, 842, 140),
  errors: smoothSeries(52, 48, 0.04, 0.05),
  throughput: smoothSeries(53, 48, 8400, 1700),
};

const DIST_BINS = 36;
const DIST_MS_MIN = 4;
const DIST_MS_MAX = 1200;
function buildDistCounts(): number[] {
  const lo = Math.log10(DIST_MS_MIN);
  const hi = Math.log10(DIST_MS_MAX);
  const out: number[] = [];
  for (let i = 0; i < DIST_BINS; i++) {
    const lm = lo + ((hi - lo) * (i + 0.5)) / DIST_BINS;
    const main = Math.exp(-Math.pow((lm - Math.log10(28)) / 0.34, 2)) * 5200;
    const tail = Math.exp(-Math.pow((lm - Math.log10(360)) / 0.42, 2)) * 520;
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
  errorFrom: DIST_BINS - 6,
  total: 12936,
  markers: [
    { label: "median", ms: 31, color: token.chLatency },
    { label: "p90", ms: 142, color: token.chThroughput },
    { label: "p95", ms: 312, color: token.chP99 },
    { label: "p99", ms: 842, color: token.chP95 },
  ],
};

const RAIL = 50;
const H_VIEWNAV = 36;
const H_SUBHEAD = 34;
const PANEL_PAD_X = 20;
const HEADER_H = H_VIEWNAV + H_SUBHEAD;
const TILE_GAP = 14;

const VIEW_H = H - HEADER_H - 23;

const OV_PAD_TOP = 16;
const LAT_TILE_H = 240;
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
const OV_ROW_CAL = 50;

const SEC_OP_H = 470;
const SEC_TRACE_H = 600;
const OFF_TRACE = SEC_OP_H;

const TRACE_HEADER_H = 62;
const RULER_H = 22;
const WATERFALL_TOP_IN_SEC = OV_PAD_TOP + 1 + TRACE_HEADER_H + RULER_H + 4 + 30;
const SPAN_ROW_H = 40;
const DB_ROW_YIN_COL = OFF_TRACE + WATERFALL_TOP_IN_SEC + 6 * SPAN_ROW_H + 11;
const DB_SPAN_X = RAIL + PANEL_PAD_X + 430;

const TL = timeline([
  { name: "establish", ms: 1000 },
  { name: "overviewDwell", ms: 1500 },
  { name: "scrollToInsights", ms: 1300 },
  { name: "scanOps", ms: 1400 },
  { name: "moveToSlowRow", ms: 1400 },
  { name: "opClick", ms: 120 },
  { name: "pageOut", ms: 420 },
  { name: "opLoad", ms: 650 },
  { name: "pageIn", ms: 480 },
  { name: "opReveal", ms: 1500 },
  { name: "scrollToTrace", ms: 1300 },
  { name: "traceLoad", ms: 550 },
  { name: "waterfallBuild", ms: 1400 },
  { name: "moveToDbSpan", ms: 1500 },
  { name: "dbClick", ms: 120 },
  { name: "flyoutLoad", ms: 420 },
  { name: "detailReveal", ms: 1500 },
  { name: "moveToLog", ms: 1300 },
  { name: "dwell", ms: 2400 },
]);

export const TRACE_MS = TL.total;
export const TRACE_TL = TL;

export interface TraceScreenProps {
  progress: MotionValue<number>;
  active?: boolean;
}

const HOVER_LOG = TRACE.logs.find((l) => l.hover)!;
const WF_LEFT = RAIL + PANEL_PAD_X + 16;
const WF_RIGHT = W - PANEL_PAD_X - 16;
const WF_WIDTH = WF_RIGHT - WF_LEFT;
const logXInArea = (atMs: number) => (atMs / TRACE.totalMs) * WF_WIDTH;
const logTimeMs = (log: (typeof TRACE.logs)[number]) => {
  const sp = TRACE.spans.find((s) => s.id === log.spanId)!;
  return sp.startMs + sp.durationMs * 0.5;
};
const logLeftPct = (log: (typeof TRACE.logs)[number]) =>
  (logTimeMs(log) / TRACE.totalMs) * 100;
const HOVER_LOG_X = WF_LEFT + logXInArea(logTimeMs(HOVER_LOG));
const LOG_DOT_YIN_COL = DB_ROW_YIN_COL - 16;

export function TraceScreen({ progress }: TraceScreenProps) {
  const pageMid = TL.at("opLoad", 0.5);
  const viewAt = (p: number) => (p >= pageMid ? 1 : 0);
  const [view, setView] = useState(() => viewAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => setView(viewAt(p)));

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

  const ovMaxScroll = Math.max(0, SEC_OVERVIEW_H - VIEW_H);
  const ovScrollY = useTransform(
    progress,
    [TL.start("scrollToInsights"), TL.end("scrollToInsights")],
    [0, -Math.min(ovMaxScroll, INSIGHTS_TOP_IN_OV - 90)],
    { ease: ease.inOut, clamp: true },
  );

  const p2MaxScroll = Math.max(0, OFF_TRACE + SEC_TRACE_H - VIEW_H);
  const p2ScrollY = useTransform(
    progress,
    [TL.start("scrollToTrace"), TL.end("scrollToTrace")],
    [0, -Math.min(p2MaxScroll, OFF_TRACE)],
    { ease: ease.inOut, clamp: true },
  );

  const opRowY = useTransform(
    ovScrollY,
    (sy) => HEADER_H + gridRowYInOv(0) + OV_ROW_CAL + sy,
  );
  const dbRowY = useTransform(
    p2ScrollY,
    (sy) => HEADER_H + DB_ROW_YIN_COL + sy,
  );
  const logY = useTransform(p2ScrollY, (sy) => HEADER_H + LOG_DOT_YIN_COL + sy);

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

  const P2_REST_X = 460;
  const P2_REST_Y = 300;

  const cx = useTransform(
    progress,
    [
      TL.start("establish"),
      TL.start("moveToSlowRow"),
      TL.start("opClick"),
      TL.start("pageOut"),
      TL.end("pageIn"),
      TL.start("moveToDbSpan"),
      TL.start("dbClick"),
      TL.end("detailReveal"),
      TL.end("moveToLog"),
      1,
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

  const smooth = (t: number) => {
    const c = t < 0 ? 0 : t > 1 ? 1 : t;
    return c * c * (3 - 2 * c);
  };
  const cy = useTransform(
    [progress, opRowY, dbRowY, logY] as MotionValue<number>[],
    ([p, orow, drow, lrow]: number[]) => {
      if (p >= TL.start("moveToSlowRow") && p < TL.start("pageOut")) {
        if (p >= TL.start("opClick")) return orow;
        const f = smooth(
          (p - TL.start("moveToSlowRow")) /
            Math.max(1e-6, TL.start("opClick") - TL.start("moveToSlowRow")),
        );
        return P2_REST_Y + (orow - P2_REST_Y) * f;
      }
      if (p >= TL.start("moveToLog")) {
        const f = smooth(
          (p - TL.start("moveToLog")) /
            Math.max(1e-6, TL.end("moveToLog") - TL.start("moveToLog")),
        );
        return drow + (lrow - drow) * f;
      }
      if (p >= TL.start("moveToDbSpan")) {
        if (p >= TL.start("dbClick")) return drow;
        const f = smooth(
          (p - TL.start("moveToDbSpan")) /
            Math.max(1e-6, TL.start("dbClick") - TL.start("moveToDbSpan")),
        );
        return P2_REST_Y + (drow - P2_REST_Y) * f;
      }
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
          <GatewayViewNav />
          <ProductionStageHeader />

          <div
            style={{
              flex: 1,
              minHeight: 0,
              position: "relative",
              overflow: "hidden",
            }}
          >
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
          <UnderlineTab
            key={v}
            label={v}
            active={on}
            height="100%"
            color={ORANGE}
          />
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
      <PanelTile
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
      </PanelTile>

      <div style={{ display: "flex", gap: TILE_GAP, height: TP_TILE_H }}>
        <div style={{ flex: "0 0 61%", minWidth: 0, display: "flex" }}>
          <PanelTile
            title="Throughput"
            headerExtra={<MetricBadge value="4.9K" sub="opm" />}
          >
            <AreaLineChart
              values={OVERVIEW.throughput}
              color={token.chThroughput}
            />
          </PanelTile>
        </div>
        <div style={{ flex: 1, minWidth: 0, display: "flex" }}>
          <PanelTile
            title="Clients"
            headerExtra={
              <span style={{ fontSize: 11, color: token.textSecondary }}>
                requests
              </span>
            }
          >
            <ClientsBars />
          </PanelTile>
        </div>
      </div>

      <div style={{ display: "flex", gap: TILE_GAP, height: FAIL_TILE_H }}>
        <div style={{ flex: "0 0 61%", minWidth: 0, display: "flex" }}>
          <PanelTile title="Failed Operations">
            <AreaLineChart
              values={OVERVIEW.failed}
              color={token.chP95}
              legendLabel="GetHomePageQuery"
            />
          </PanelTile>
        </div>
        <div style={{ flex: 1, minWidth: 0, display: "flex" }}>
          <PanelTile
            title="Errors"
            headerExtra={
              <span style={{ fontSize: 11, color: token.errorText }}>
                {ERRORS.length} recent
              </span>
            }
          >
            <ErrorList />
          </PanelTile>
        </div>
      </div>

      <InsightsTable />
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
        <div
          style={{
            display: "flex",
            gap: TILE_GAP,
            height: 150,
            flex: "0 0 auto",
          }}
        >
          <PanelTile
            title="Latency"
            headerExtra={<MetricBadge value="842 ms" sub="p95" />}
          >
            <AreaLineChart values={OPSERIES.latency} color={token.chP95} />
          </PanelTile>
          <PanelTile
            title="Throughput"
            headerExtra={<MetricBadge value="8.4K" sub="opm" />}
          >
            <AreaLineChart
              values={OPSERIES.throughput}
              color={token.chThroughput}
            />
          </PanelTile>
          <PanelTile
            title="Errors"
            headerExtra={<MetricBadge value="0.04" sub="%" />}
          >
            <AreaLineChart values={OPSERIES.errors} color={token.chP95} />
          </PanelTile>
        </div>
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
              Total operations: {DIST.total.toLocaleString("en-US")}
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

function LatencyDistribution() {
  const maxCount = Math.max(...DIST.counts);
  const yScale = logScale(1, maxCount * 1.15, 100, 4);
  const n = DIST.counts.length;
  const xMin = DIST.msMin;
  const xMax = DIST.msMax;
  const xScale = logScale(xMin, xMax, 0, 100);
  const lo = Math.log10(xMin);
  const hi = Math.log10(xMax);
  const binLeftMs = (i: number) => Math.pow(10, lo + ((hi - lo) * i) / n);
  const yTicks = [1, 10, 100, 1000, 10000].filter((t) => t <= maxCount * 1.15);
  const markPct = (msVal: number) =>
    xScale(Math.min(xMax, Math.max(xMin, msVal)));

  return (
    <div style={{ position: "relative", width: "100%", height: "100%" }}>
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
            const bw = Math.max(0.5, x1 - x0 - 0.4);
            const topY = yScale(Math.max(1, c));
            const hasError = i >= DIST.errorFrom;
            const errH = hasError
              ? Math.min(100 - topY, 4 + (i - DIST.errorFrom) * 2.5)
              : 0;
            return (
              <g key={i}>
                <rect
                  x={x0 + 0.2}
                  y={topY + errH}
                  width={bw}
                  height={Math.max(0, 100 - topY - errH)}
                  rx={0.4}
                  style={{ fill: token.cLatency }}
                />
                {hasError && (
                  <rect
                    x={x0 + 0.2}
                    y={topY}
                    width={bw}
                    height={errH}
                    style={{ fill: token.cError }}
                  />
                )}
              </g>
            );
          })}
        </svg>
        {DIST.markers.map((m) => (
          <DistMarker
            key={m.label}
            label={m.label}
            leftPct={markPct(m.ms)}
            color={m.color}
          />
        ))}
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

function TraceSampleSection({ progress }: { progress: MotionValue<number> }) {
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

        <div
          style={{
            flex: 1,
            minHeight: 0,
            display: "flex",
            flexDirection: "column",
          }}
        >
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

  const built = 1;

  const glow = useTransform(progress, [0, TL.start("dbClick")], [3, 4], {
    clamp: true,
  });
  const targetGlow = useTransform(
    glow,
    (r) => `0 0 ${r}px 0 ${token.chImpact}`,
  );

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

function DbDetail({ progress }: { progress: MotionValue<number> }) {
  const w0 = TL.start("detailReveal");
  return (
    <div style={{ display: "flex", flexDirection: "column", height: "100%" }}>
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
        <SqlStatement progress={progress} />
      </div>
    </div>
  );
}

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
