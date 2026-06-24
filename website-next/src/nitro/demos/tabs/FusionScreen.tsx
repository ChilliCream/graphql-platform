/**
 * FusionScreen — Tab 5: the full Fusion story in three sequenced scenes whose CONTENT crossfades,
 * with ONE persistent cursor gliding continuously across all three (so it never disappears at a cut
 * and the seams read as a single session). Timing is fully DERIVED from each scene's stage timeline
 * (`*_TL` / `*_MS`); the scene windows and the shared cursor's keyframe TIMES are computed from
 * those stages — nothing is a hand-tuned global fraction.
 *
 *   1. GATEWAY OVERVIEW   — spot the new Reviews v2.4.0 deployment, click it
 *   2. DEPLOYMENTS        — its additive schema changes recompose the gateway; click "+"
 *   3. QUERY + PLAN       — click into the empty editor, type & Run, then the Operation Plan graph
 *      builds + executes and a CAMERA zooms in and pans ALONG the plan before zooming back out.
 *
 * The camera lives here (it must stay in lock-step with the shared cursor): during the plan's
 * zoom/pan the cursor parks at viewport centre so it "reads along" the plan as it scrolls beneath,
 * then settles on the Products node as the camera zooms back out.
 */
import { useState } from "react";
import {
  motion,
  useMotionValue,
  useMotionValueEvent,
  useTransform,
  type MotionValue,
} from "motion/react";
import { Stage } from "../../primitives/reel/Stage";
import { Cursor } from "../../primitives/reel/Cursor";
import { TABREEL_CANVAS } from "../../primitives/reel/TabReel";
import { ease } from "../../lib/motion";
import {
  FusionOverviewScreen,
  OVERVIEW_MS,
  OVERVIEW_TL,
} from "./fusion/FusionOverviewScreen";
import {
  FusionDeploymentsScreen,
  DEPLOYMENTS_MS,
  DEPLOYMENTS_TL,
} from "./fusion/FusionDeploymentsScreen";
import {
  FusionQueryPlanScreen,
  QUERYPLAN_MS,
  QUERYPLAN_TL,
} from "./fusion/FusionQueryPlanScreen";

const W = TABREEL_CANVAS.w;
const H = TABREEL_CANVAS.h;

// Tab TOTAL = sum of the scenes' derived durations; windows are proportional.
export const FUSION_MS = OVERVIEW_MS + DEPLOYMENTS_MS + QUERYPLAN_MS;
const OV_END = OVERVIEW_MS / FUSION_MS;
const DEP_END = (OVERVIEW_MS + DEPLOYMENTS_MS) / FUSION_MS;
const BAND = 0.014; // content crossfade width at each boundary

// map a scene-local progress (0..1) → GLOBAL progress
const gOv = (l: number) => (l * OVERVIEW_MS) / FUSION_MS;
const gDep = (l: number) => (OVERVIEW_MS + l * DEPLOYMENTS_MS) / FUSION_MS;
const gQp = (l: number) =>
  (OVERVIEW_MS + DEPLOYMENTS_MS + l * QUERYPLAN_MS) / FUSION_MS;
const Q = QUERYPLAN_TL;

// ── plan-camera key instants (global progress) ──
const Z_IN0 = gQp(Q.start("zoomIn"));
const Z_IN1 = gQp(Q.end("zoomIn"));
const Z_PAN1 = gQp(Q.end("panPlan"));
const Z_OUT1 = gQp(Q.end("zoomOut"));
// focus points (canvas px) the camera frames at viewport centre while zoomed (k=1.6):
//   start ≈ the root/Orders area (left), end ≈ the Products fetch (right).
const ZK = 1.5;
const fx0 = 470,
  fy0 = 460; // start-of-plan focus (root/Orders)
const fx1 = 1050,
  fy1 = 460; // end-of-plan focus (deepest fetches)
const camAt = (fx: number, fy: number) => ({
  x: W / 2 - fx * ZK,
  y: H / 2 - fy * ZK,
});
const C0 = camAt(fx0, fy0);
const C1 = camAt(fx1, fy1);

// ── ONE continuous cursor path (canvas px). Times derived from each scene's stages; positions
// chosen so boundaries line up. In scene 3 it clicks into the empty editor, types, Runs, opens the
// plan, drifts onto the plan as it execs, then PARKS at viewport centre (752,470) through the
// zoom+pan, and settles on Products as the camera zooms out.
const CURSOR_T = [
  0,
  gOv(OVERVIEW_TL.start("moveToDeployment")),
  gOv(OVERVIEW_TL.end("moveToDeployment")),
  gDep(DEPLOYMENTS_TL.start("moveToPlus")),
  gDep(DEPLOYMENTS_TL.start("plusClick")),
  gQp(Q.start("establish")),
  gQp(Q.start("editorClick")),
  gQp(Q.start("moveToRun")),
  gQp(Q.start("runClick")),
  gQp(Q.start("moveToPlanTab")),
  gQp(Q.start("planTabClick")),
  Z_IN0, // drifted onto the plan (as it execs), at viewport centre
  Z_PAN1, // hold centre through the pan
  gQp(Q.start("viewClick")), // → the Products node's "View Raw Data"
  1,
];
const CURSOR_X = [
  1082, 1082, 1122, 1122, 243, 243, 300, 320, 620, 690, 830, 752, 752, 710, 710,
];
const CURSOR_Y = [
  393, 393, 543, 543, 19, 19, 210, 230, 64, 95, 64, 470, 470, 453, 453,
];
const CURSOR_CLICKS = [
  gOv(OVERVIEW_TL.start("click")),
  gDep(DEPLOYMENTS_TL.start("plusClick")),
  gQp(Q.start("editorClick")),
  gQp(Q.start("runClick")),
  gQp(Q.start("planTabClick")),
  gQp(Q.start("viewClick")),
];

const liveAt = (p: number) => ({
  ov: p < OV_END + 0.0005,
  dep: p > OV_END - BAND && p < DEP_END + 0.0005,
  qp: p >= DEP_END - BAND,
});

export interface FusionScreenProps {
  progress: MotionValue<number>;
  active?: boolean;
}

export function FusionScreen({ progress }: FusionScreenProps) {
  const ovLocal = useTransform(progress, [0, OV_END], [0, 1], { clamp: true });
  const depLocal = useTransform(progress, [OV_END, DEP_END], [0, 1], {
    clamp: true,
  });
  const qpLocal = useTransform(progress, [DEP_END, 1], [0, 1], { clamp: true });

  const ovOp = useTransform(progress, [OV_END - BAND, OV_END], [1, 0], {
    clamp: true,
  });
  const depOp = useTransform(
    progress,
    [OV_END - BAND, OV_END, DEP_END - BAND, DEP_END],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const qpOp = useTransform(progress, [DEP_END - BAND, DEP_END], [0, 1], {
    clamp: true,
  });

  // plan camera: identity until zoomIn, frames the start, pans to Products, zooms back to identity.
  const camScale = useTransform(
    progress,
    [Z_IN0, Z_IN1, Z_PAN1, Z_OUT1],
    [1, ZK, ZK, 1],
    { clamp: true, ease: ease.glide },
  );
  const camX = useTransform(
    progress,
    [Z_IN0, Z_IN1, Z_PAN1, Z_OUT1],
    [0, C0.x, C1.x, 0],
    { clamp: true, ease: ease.glide },
  );
  const camY = useTransform(
    progress,
    [Z_IN0, Z_IN1, Z_PAN1, Z_OUT1],
    [0, C0.y, C1.y, 0],
    { clamp: true, ease: ease.glide },
  );
  const planCamera = { x: camX, y: camY, scale: camScale };

  // the single shared cursor (global progress)
  const cx = useTransform(progress, CURSOR_T, CURSOR_X, { ease: ease.glide });
  const cy = useTransform(progress, CURSOR_T, CURSOR_Y, { ease: ease.glide });

  // identity camera for the non-zooming scenes (stable refs)
  const z0 = useMotionValue(0);
  const k1 = useMotionValue(1);

  const [live, setLive] = useState(() => liveAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => {
    const n = liveAt(p);
    setLive((prev) =>
      prev.ov === n.ov && prev.dep === n.dep && prev.qp === n.qp ? prev : n,
    );
  });

  return (
    <div style={{ position: "absolute", inset: 0 }}>
      {live.ov && (
        <motion.div style={{ position: "absolute", inset: 0, opacity: ovOp }}>
          <FusionOverviewScreen progress={ovLocal} showCursor={false} />
        </motion.div>
      )}
      {live.dep && (
        <motion.div style={{ position: "absolute", inset: 0, opacity: depOp }}>
          <FusionDeploymentsScreen progress={depLocal} showCursor={false} />
        </motion.div>
      )}
      {live.qp && (
        <motion.div style={{ position: "absolute", inset: 0, opacity: qpOp }}>
          <FusionQueryPlanScreen
            progress={qpLocal}
            showCursor={false}
            camera={planCamera}
          />
        </motion.div>
      )}

      {/* ONE persistent cursor on top of the crossfading content — never fades or jumps. It is NOT
          camera-transformed, so during the plan zoom/pan it sits at viewport centre and the plan
          scrolls beneath it. */}
      <div style={{ position: "absolute", inset: 0, pointerEvents: "none" }}>
        <Stage
          width={W}
          height={H}
          fit="fill"
          chrome={false}
          camera={{ x: z0, y: z0, scale: k1 }}
          style={{ background: "transparent" }}
          overlay={
            <Cursor
              x={cx}
              y={cy}
              progress={progress}
              clickTimes={CURSOR_CLICKS}
              pointerWindows={[[gQp(Q.start("planDwell")), 1]]}
            />
          }
        >
          <></>
        </Stage>
      </div>
    </div>
  );
}
