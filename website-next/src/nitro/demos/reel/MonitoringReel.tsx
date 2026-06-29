/**
 * MonitoringReel: a scripted product "reel" (no narrative text): a camera + cursor tour
 * that demonstrates the monitoring workflow by USING it. The screens are reused as static
 * sets (forced reduced-motion); the reel drives the camera (pan/zoom/scroll), a simulated
 * cursor, the product's own hover popouts + crosshair/row highlight, and a click→view
 * transition into the operation detail. One master clock, loops seamlessly.
 *
 * Story (symptom → investigate → drill in): establish the dashboard → a latency spike,
 * cursor hovers it (crosshair + popout) → scroll to the Subgraphs table → hover the hot
 * row (popout) → click → cross-fade to the operation detail → hover the slow tail (popout).
 *
 * Coordinates are MEASURED canvas coords (see anchors below) in the 1240×780 design canvas.
 * `camFrame(fx,fy,k,vx,vy)` frames canvas point (fx,fy) at viewport (vx,vy) at scale k.
 * The cursor/popouts then sit at predictable viewport positions.
 */
import { useEffect, useMemo } from "react";
import { motion, useTransform, useMotionValue } from "motion/react";
import { MonitoringOverview } from "../monitoring/MonitoringOverview";
import { OperationDetail } from "../operation/OperationDetail";
import { Stage } from "../../primitives/reel/Stage";
import { Cursor } from "../../primitives/reel/Cursor";
import { Popout } from "../../primitives/reel/Popout";
import { Sidebar, SIDEBAR_W } from "../../primitives/reel/Sidebar";
import { TopBar } from "../../primitives/reel/TopBar";
import { StatusBar, STATUSBAR_H } from "../../primitives/reel/StatusBar";
import { AppMotionConfig } from "../../lib/motion";
import { useMasterClock } from "../../lib/useInViewLoop";
import { token } from "../../lib/tokens";
import { ease } from "../../lib/motion";
import { makeMonitoringData, makeTrace } from "../../lib/data";
import { ms, compact } from "../../lib/scale";

const PAD = 12; // dashboard padding inside the content area
const DASH_W = 1240; // content area width (top bar + dashboard)
const W = SIDEBAR_W + DASH_W; // full canvas: sidebar + content
const H = 1004; // sidebar full height: top bar + dashboard + status bar

// MEASURED true canvas anchors WITH the sidebar (scripts/verify-reel.mjs). Re-measure on
// layout change; x-coords include the +SIDEBAR_W shift.
const SPIKE = { x: 1135, y: 139 }; // latency line peak
const LAT_PLOT = { top: 160, bottom: 370 };
const ROW = { top: 745, h: 37, cy: 764, nameX: 347 }; // hot subgraph row (products)
const SPAN = { cx: 1028, y: 868 }; // slow "HTTP POST products" span bar centre
const P95_X = 1026; // operation p95 distribution marker (canvas x)

// frame canvas point (fx,fy) at viewport (vx,vy) at scale k
const camFrame = (
  fx: number,
  fy: number,
  k: number,
  vx: number,
  vy: number,
) => ({
  x: vx - fx * k,
  y: vy - fy * k,
  k,
});

export interface MonitoringReelProps {
  seed?: number;
  durationMs?: number;
  /** freeze the timeline at a fixed progress 0..1 (verification/debug only) */
  staticProgress?: number;
}

export function MonitoringReel({
  seed = 7,
  durationMs = 22000,
  staticProgress,
}: MonitoringReelProps = {}) {
  const data = useMemo(() => makeMonitoringData(seed), [seed]);
  const trace = useMemo(() => makeTrace(seed), [seed]);
  const slowSpan = trace.spans.find((s) => s.name === "HTTP POST products")!;
  const slowPct = Math.round((slowSpan.durationMs / trace.totalMs) * 100);
  const {
    ref,
    progress: clockProgress,
    reduced,
  } = useMasterClock({ durationMs, amount: 0.2 });
  const frozen = useMotionValue(staticProgress ?? 0);
  useEffect(() => {
    if (staticProgress != null) frozen.set(staticProgress);
  }, [staticProgress, frozen]);
  const progress = staticProgress != null ? frozen : clockProgress;

  // ---- camera timeline ----
  // Each hover beat frames its target at a viewport point; the cursor goes to that SAME
  // point (see VP below), so cursor and target coincide by construction.
  const VP = {
    spike: { x: 620, y: 220 },
    row: { x: 320, y: 470 },
    dist: { x: 760, y: 360 },
    span: { x: 700, y: 520 },
  };
  // Hover beats zoom in on their target (pushing the sidebar off); establish/opEstablish
  // sit at scale 1 so the sidebar shows ("after you click, the sidebar shows").
  const C = {
    establish: { x: 0, y: 0, k: 1 },
    spike: camFrame(SPIKE.x, SPIKE.y, 1.4, VP.spike.x, VP.spike.y),
    table: camFrame(ROW.nameX, ROW.cy, 1.2, VP.row.x, VP.row.y),
    opEstablish: { x: 0, y: -8, k: 1 },
    opDist: camFrame(P95_X, 455, 1.4, VP.dist.x, VP.dist.y),
    opTrace: camFrame(SPAN.cx, SPAN.y, 1.3, VP.span.x, VP.span.y),
  };
  const BEATS = [
    0, 0.06, 0.15, 0.25, 0.31, 0.42, 0.52, 0.575, 0.64, 0.72, 0.8, 0.9, 1,
  ] as const;
  const camKeys = [
    C.establish,
    C.establish,
    C.spike,
    C.spike,
    C.table,
    C.table,
    C.table,
    C.opEstablish,
    C.opDist,
    C.opDist,
    C.opTrace,
    C.opTrace,
    C.establish,
  ];
  const camOpts = { ease: ease.inOut };
  const camX = useTransform(
    progress,
    [...BEATS],
    camKeys.map((c) => c.x),
    camOpts,
  );
  const camY = useTransform(
    progress,
    [...BEATS],
    camKeys.map((c) => c.y),
    camOpts,
  );
  const camScale = useTransform(
    progress,
    [...BEATS],
    camKeys.map((c) => c.k),
    camOpts,
  );

  // ---- cursor timeline (viewport coords; lands where the camera frames each target) ----
  const cuBeats = [
    0, 0.05, 0.14, 0.25, 0.31, 0.42, 0.5, 0.52, 0.575, 0.68, 0.8, 0.88, 1,
  ];
  const cursorX = useTransform(
    progress,
    cuBeats,
    [
      1380,
      880,
      VP.spike.x,
      VP.spike.x,
      520,
      VP.row.x,
      VP.row.x,
      VP.row.x,
      760,
      VP.dist.x,
      VP.span.x,
      VP.span.x,
      1380,
    ],
    camOpts,
  );
  const cursorY = useTransform(
    progress,
    cuBeats,
    [
      940,
      420,
      VP.spike.y,
      VP.spike.y,
      360,
      VP.row.y,
      VP.row.y,
      VP.row.y,
      360,
      VP.dist.y,
      VP.span.y,
      VP.span.y,
      940,
    ],
    camOpts,
  );

  // ---- screen transition: fade THROUGH dark (no messy overlap of the two screens) ----
  const monOpacity = useTransform(
    progress,
    [0, 0.52, 0.555, 0.93, 0.965, 1],
    [1, 1, 0, 0, 1, 1],
  );
  const opOpacity = useTransform(
    progress,
    [0.555, 0.59, 0.9, 0.935],
    [0, 1, 1, 0],
  );

  // ---- screen-space overlays (canvas coords) ----
  const crosshair = useTransform(
    progress,
    [0.13, 0.17, 0.27, 0.31],
    [0, 1, 1, 0],
  );
  const rowHi = useTransform(progress, [0.35, 0.39, 0.52, 0.55], [0, 1, 1, 0]);
  const rowHiOpacity = useTransform(rowHi, [0, 1], [0, 0.12]);

  // whole-reel seam envelope (frozen to 1 under reduced motion)
  const envelope = useTransform(progress, [0, 0.02, 0.98, 1], [0, 1, 1, 0]);

  const hot = data.insights[1]; // hot subgraph (products), matches the slow trace span

  const screens = (
    <>
      {/* full-width status bar pinned at the very bottom */}
      <StatusBar
        style={{
          position: "absolute",
          left: 0,
          top: H - STATUSBAR_H,
          width: W,
        }}
      />
      {/* left nav sidebar (above the status bar) */}
      <Sidebar
        selected="createOrder"
        style={{
          position: "absolute",
          left: 0,
          top: 0,
          width: SIDEBAR_W,
          height: H - STATUSBAR_H,
        }}
      />

      {/* monitoring content: top bar + dashboard (overlays positioned in content space) */}
      <motion.div
        style={{
          position: "absolute",
          left: SIDEBAR_W,
          top: 0,
          width: DASH_W,
          opacity: monOpacity,
        }}
      >
        <TopBar active="Monitoring" />
        <div style={{ padding: PAD }}>
          <AppMotionConfig reducedMotion="always">
            <MonitoringOverview seed={seed} />
          </AppMotionConfig>
        </div>
        {/* spike crosshair (canvas coords → content-local = x − SIDEBAR_W) */}
        <motion.div
          style={{
            position: "absolute",
            left: SPIKE.x - SIDEBAR_W,
            top: LAT_PLOT.top,
            height: LAT_PLOT.bottom - LAT_PLOT.top,
            width: 0,
            borderLeft: `1px dashed ${token.cP99}`,
            opacity: crosshair,
            pointerEvents: "none",
          }}
        />
        <motion.div
          data-testid="reel-xhair-dot"
          style={{
            position: "absolute",
            left: SPIKE.x - SIDEBAR_W - 4.5,
            top: SPIKE.y - 4.5,
            width: 9,
            height: 9,
            borderRadius: "50%",
            background: token.cP99,
            border: `2px solid ${token.bg}`,
            opacity: crosshair,
          }}
        />
        {/* row highlight */}
        <motion.div
          style={{
            position: "absolute",
            left: PAD + 2,
            right: PAD + 2,
            top: ROW.top,
            height: ROW.h,
            borderRadius: 4,
            background: token.accent,
            opacity: rowHiOpacity,
            pointerEvents: "none",
          }}
        />
      </motion.div>

      {/* operation content */}
      <motion.div
        style={{
          position: "absolute",
          left: SIDEBAR_W,
          top: 0,
          width: DASH_W,
          opacity: opOpacity,
        }}
      >
        <TopBar active="Operations" />
        <div style={{ padding: PAD }}>
          <AppMotionConfig reducedMotion="always">
            <OperationDetail seed={seed} />
          </AppMotionConfig>
        </div>
      </motion.div>
    </>
  );

  const overlay = (
    <>
      <Popout
        x={730}
        y={92}
        progress={progress}
        show={0.17}
        hide={0.27}
        title={fmtTime(
          data.throughput[Math.round(data.throughput.length * 0.7)].epoch,
        )}
        rows={[
          { label: "mean", color: token.cLatency, value: ms(110) },
          { label: "p95", color: token.cP95, value: ms(data.totals.p95) },
          { label: "p99", color: token.cP99, value: ms(358) },
        ]}
      />
      <Popout
        x={352}
        y={384}
        progress={progress}
        show={0.41}
        hide={0.51}
        title={hot.name}
        rows={[
          {
            label: "latency",
            color: token.cLatency,
            value: ms(hot.averageLatency),
          },
          {
            label: "throughput",
            color: token.cThroughput,
            value: `${compact(hot.opm)}/m`,
          },
          {
            label: "errors",
            color: token.cError,
            value: `${(hot.errorRate * 100).toFixed(1)}%`,
          },
        ]}
      />
      <Popout
        x={832}
        y={176}
        progress={progress}
        show={0.66}
        hide={0.73}
        title="distribution"
        rows={[
          { label: "p95", color: token.cP95, value: ms(260) },
          { label: "current", color: token.cP99, value: ms(42) },
        ]}
      />
      <Popout
        x={792}
        y={332}
        progress={progress}
        show={0.82}
        hide={0.9}
        title="HTTP POST"
        rows={[
          {
            label: "duration",
            color: token.cThroughput,
            value: ms(slowSpan.durationMs),
          },
          {
            label: "of trace",
            color: token.textSecondary,
            value: `${slowPct}%`,
          },
        ]}
      />
      <Cursor
        x={cursorX}
        y={cursorY}
        progress={progress}
        clickTimes={[0.515]}
      />
    </>
  );

  return (
    <motion.div
      ref={ref}
      style={{ width: "100%", opacity: reduced ? 1 : envelope }}
    >
      <Stage
        width={W}
        height={H}
        camera={{ x: camX, y: camY, scale: camScale }}
        overlay={overlay}
        ariaLabel="Nitro monitoring, guided product tour"
      >
        {screens}
      </Stage>
    </motion.div>
  );
}

const fmtTime = (epoch: number) => {
  const d = new Date(epoch);
  return `${String(d.getUTCHours()).padStart(2, "0")}:${String(d.getUTCMinutes()).padStart(2, "0")}`;
};
