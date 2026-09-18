"use client";

import { useEffect, useRef } from "react";

import { BRAND } from "../../../tokens";
import { useElementMotion } from "../../../visuals/hooks";
import {
  buildChamberTiles,
  buildInstrumentLights,
  type InstrumentLight,
  type Tile,
} from "./chamber";
import { hexToRgba } from "./colors";
import { project, torusPoint } from "./geometry";
import { computeLayout, type TokamakLayout } from "./layout";
import {
  paintChamber,
  paintPlasmaCache,
  strokeShadedPath,
  strokeShadedPathRgba,
  type BandBloomTarget,
} from "./paint";
import {
  createStreak,
  occludeBehindColumn,
  occludeHelixBehindColumn,
  projectHelix,
  projectStreak,
  splitByPredicate,
  type HelixPoint,
  type ShadedPoint,
  type Streak,
} from "./plasma";

/**
 * Streak counts per viewport size. Most of the "hundreds of thin bright
 * streaks" are the static majority, baked once into the cached plasma layer
 * (ticket hc-0-wrc.3 comment 160 / planner ruling 187 item 3: density is
 * the craft bar, hundreds not dozens, 300-600 orbiting the band); a small
 * subset orbits live every frame so the average frame stays well under the
 * 4ms budget.
 */
// Counts and per-streak arc length both raised (hc-0-wrc.3 review 2, F2):
// at the old 420/48 desktop count with short 0.045-0.155 rad arcs the band
// read as a scattered cloud of dashes rather than a continuous glowing
// ring; longer tangential arcs at 2x+ the density stack into a continuous
// band under `lighter` compositing.
const DESKTOP_STATIC_STREAKS = 900;
const DESKTOP_LIVE_STREAKS = 72;
const MOBILE_STATIC_STREAKS = 450;
const MOBILE_LIVE_STREAKS = 39;
/** Offscreen glow source for the live streaks/helix, a fraction of the live canvas' CSS size -- a cheap bloom from downscale + upscale instead of a per-stroke blur filter (same technique as `PlasmaFusion`'s `drawBloomSource`). */
const GLOW_SCALE = 0.25;

const BAND_ORBIT_PERIOD_S = 28;
const TWIST_PERIOD_S = 20;
const BREATHE_PERIOD_S = 6.2;
const HOT_STREAK_PERIOD_S = 3.4;

function mulberry32(seed: number): () => number {
  let a = seed >>> 0;
  return () => {
    a += 0x6d2b79f5;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/**
 * A jittered grid over the torus' (theta, phi) domain: `count` streaks
 * spread evenly across the whole band (both around the column and around
 * the tube) instead of a pure random draw, which can leave some radius
 * bands empty while others accumulate enough overlapping short arcs to read
 * as a complete ring.
 */
function stratifiedAngles(
  count: number,
  rand: () => number,
): Array<readonly [number, number]> {
  const cols = Math.max(1, Math.round(Math.sqrt(count * 1.6)));
  const rows = Math.max(1, Math.ceil(count / cols));
  const pairs: Array<readonly [number, number]> = [];
  for (let i = 0; i < count; i++) {
    const c = i % cols;
    const r = Math.floor(i / cols) % rows;
    const theta = ((c + rand()) / cols) * Math.PI * 2;
    const phi = ((r + rand()) / rows) * Math.PI * 2;
    pairs.push([theta, phi]);
  }
  return pairs;
}

/**
 * v12 - Tokamak: seen from inside the vessel, a dark tiled steel column and
 * wall wrapping around the viewer, with a coral/pink plasma torus of
 * hundreds of orbiting streaks and a twisting filament at its centre. See
 * `../README.md` for the shared hero contract (palette, lighting/depth,
 * motion gating, technique) and `../PlasmaFusion` for the static/live split
 * and cheap-bloom technique this follows.
 */
export default function Tokamak() {
  const rootRef = useRef<HTMLDivElement>(null);
  const chamberRef = useRef<HTMLCanvasElement>(null);
  const plasmaCacheRef = useRef<HTMLCanvasElement>(null);
  const liveRef = useRef<HTMLCanvasElement>(null);
  const running = useElementMotion(rootRef);
  const drawLiveRef = useRef<((timeSec: number) => void) | null>(null);

  useEffect(() => {
    const root = rootRef.current;
    const chamberCanvas = chamberRef.current;
    const plasmaCacheCanvas = plasmaCacheRef.current;
    const liveCanvas = liveRef.current;
    if (!root || !chamberCanvas || !plasmaCacheCanvas || !liveCanvas) {
      return;
    }
    const chamberCtx = chamberCanvas.getContext("2d");
    const plasmaCtx = plasmaCacheCanvas.getContext("2d");
    const liveCtx = liveCanvas.getContext("2d");
    const glow = document.createElement("canvas");
    const glowCtx = glow.getContext("2d");
    if (!chamberCtx || !plasmaCtx || !liveCtx || !glowCtx) {
      return;
    }

    const rand = mulberry32(0x746f6b31);
    let layout: TokamakLayout = computeLayout(0, 0);
    let dpr = 1;
    let w = 0;
    let h = 0;
    let glowW = 0;
    let glowH = 0;
    let liveStreaks: Streak[] = [];
    /** The column's own projected half-width at the plasma's height, in px -- see `occludeBehindColumn`. Recomputed in `buildScene` whenever the layout changes. */
    let columnHalfWidthPx = 0;
    let disposed = false;

    function featherEdge(ctx: CanvasRenderingContext2D) {
      ctx.globalCompositeOperation = "destination-out";
      if (!layout.mobile) {
        const left = layout.artLeft;
        const right = layout.artLeft + 24;
        const fade = ctx.createLinearGradient(left, 0, right, 0);
        fade.addColorStop(0, "rgba(0,0,0,1)");
        fade.addColorStop(1, "rgba(0,0,0,0)");
        ctx.fillStyle = fade;
        ctx.fillRect(0, 0, left + 24, h);
      } else {
        const top = layout.artTop;
        const bottom = layout.artTop + 24;
        const fade = ctx.createLinearGradient(0, top, 0, bottom);
        fade.addColorStop(0, "rgba(0,0,0,1)");
        fade.addColorStop(1, "rgba(0,0,0,0)");
        ctx.fillStyle = fade;
        ctx.fillRect(0, 0, w, bottom);
      }
      ctx.globalCompositeOperation = "source-over";
    }

    // The band's own visual centre: the near-side point of the torus tube
    // (theta = -PI/2, the "front-facing" convention `occludeBehindColumn`
    // and `chamber.ts`'s column back-face cull both use, phi = 0 for the
    // tube's own mid-line), NOT the axis point `{x:0,y:torus.y,z:torus.z}`
    // the core/bloom used to project through. The axis point and the band's
    // actual near-side centre project to different screen y (the axis
    // point ignores the camera tilt's effect on the tube's own radius), so
    // the core/bloom used to sit ~70px below the band it was meant to mark
    // (hc-0-wrc.3 review 2, F2: "the core is not at the band's centre").
    function bandCenter() {
      return project(
        torusPoint(
          layout.torus.R,
          layout.torus.a,
          -Math.PI / 2,
          0,
          layout.torus.y,
          layout.torus.z,
        ),
        layout.camera,
      );
    }

    /**
     * The band's own projected vertical half-height in px, from the tube's
     * actual top/bottom points at the near side (`theta = -PI/2`, `phi =
     * +-PI/2`) rather than a flat `a * scale` guess -- the tilt changes
     * each phi's own depth/scale slightly, so projecting the real extremes
     * keeps the coral halo sized to what the band actually renders as.
     */
    function bandHalfHeightPx() {
      const top = project(
        torusPoint(
          layout.torus.R,
          layout.torus.a,
          -Math.PI / 2,
          Math.PI / 2,
          layout.torus.y,
          layout.torus.z,
        ),
        layout.camera,
      );
      const bot = project(
        torusPoint(
          layout.torus.R,
          layout.torus.a,
          -Math.PI / 2,
          -Math.PI / 2,
          layout.torus.y,
          layout.torus.z,
        ),
        layout.camera,
      );
      return Math.abs(bot.y - top.y) / 2;
    }

    function buildScene() {
      const totalStatic = layout.mobile
        ? MOBILE_STATIC_STREAKS
        : DESKTOP_STATIC_STREAKS;
      const totalLive = layout.mobile
        ? MOBILE_LIVE_STREAKS
        : DESKTOP_LIVE_STREAKS;

      // The column's projected half-width at the plasma's height: the
      // middle column row sits at the torus' own y (see `layout.ts`'s
      // `buildTaperedRows`, t=0), so projecting its edge (world x = radius)
      // and comparing to the on-axis centre (`camera.originX`) gives the
      // span a far-side streak has to fall inside to read as behind the
      // column -- planner ruling 187 item 3.
      const midColumnRow =
        layout.columnRows[Math.floor(layout.columnRows.length / 2)];
      const columnEdge = project(
        { x: midColumnRow.radius, y: layout.torus.y, z: layout.torus.z },
        layout.camera,
      );
      columnHalfWidthPx = Math.abs(columnEdge.x - layout.camera.originX);

      const staticAngles = stratifiedAngles(totalStatic, rand);
      const staticStreakPaths: ShadedPoint[][] = staticAngles.map(
        ([theta0, phi]) =>
          occludeBehindColumn(
            projectStreak(
              createStreak(rand, theta0, phi),
              layout.torus,
              layout.camera,
              0,
              false,
            ),
            theta0,
            layout.camera,
            columnHalfWidthPx,
          ),
      );

      const liveAngles = stratifiedAngles(totalLive, rand);
      liveStreaks = liveAngles.map(([theta0, phi]) =>
        createStreak(rand, theta0, phi),
      );

      // Column tiles (the near-constant-radius cylinder) and wall tiles
      // (wide at the plasma's height, narrowing to meet the column above
      // and below it) are built from two separate row families sharing the
      // same axis and camera, then concatenated -- planner ruling
      // hc-0-wrc.3 comment 187 item 1. Wall first, column second: the wall
      // is the far surface wrapping the viewer, the column stands in front
      // of it, so painter's-algorithm order paints far-then-near (fix 1/F1:
      // painting the column first let the far wall's tiles draw back over
      // it every time, which is what made the column read as a pale,
      // doubled-up silhouette instead of a dark object in front).
      const tiles: Tile[] = [
        ...buildChamberTiles(
          layout.wallRows,
          layout.wallThetaSegments,
          layout.camera,
          layout.torus,
          "wall",
        ),
        ...buildChamberTiles(
          layout.columnRows,
          layout.columnThetaSegments,
          layout.camera,
          layout.torus,
          "column",
        ),
      ];
      const lights: InstrumentLight[] = buildInstrumentLights(
        layout.wallRows,
        layout.camera,
        rand,
        6,
      );
      paintChamber(chamberCtx!, w, h, tiles, lights);

      const bc = bandCenter();
      const bloomTarget: BandBloomTarget = {
        x: bc.x,
        y: bc.y,
        // The ring's projected width uses the camera's own base scale (the
        // scale at its aim depth), the same basis the ring's actual
        // left/right on-screen extent falls out of -- not the band
        // centre's own much-larger near-side scale, which previously
        // ~doubled this and produced a blur wide enough to wash out the
        // whole frame instead of just the band (hc-0-wrc.3 review 2, F3).
        ringWidthPx:
          (layout.torus.R + layout.torus.a) * layout.camera.baseScale * 2,
        bandHalfHeightPx: bandHalfHeightPx(),
      };
      paintPlasmaCache(plasmaCtx!, w, h, staticStreakPaths, bloomTarget);
      featherEdge(plasmaCtx!);
    }

    function measure() {
      w = root!.clientWidth;
      h = root!.clientHeight;
      const base = Math.min(window.devicePixelRatio || 1, 2);
      const cap = Math.sqrt(4_000_000 / Math.max(1, w * h));
      dpr = Math.max(0.75, Math.min(base, cap));
      for (const canvas of [chamberCanvas, plasmaCacheCanvas, liveCanvas]) {
        canvas!.width = Math.max(1, Math.round(w * dpr));
        canvas!.height = Math.max(1, Math.round(h * dpr));
        canvas!.getContext("2d")!.setTransform(dpr, 0, 0, dpr, 0, 0);
      }
      glowW = Math.max(1, Math.round(w * GLOW_SCALE));
      glowH = Math.max(1, Math.round(h * GLOW_SCALE));
      glow.width = glowW;
      glow.height = glowH;
      layout = computeLayout(w, h);
      buildScene();
    }

    // A downscaled offscreen pass of the live streaks + helix, composited
    // back at full size with "lighter": a cheap glow halo under their sharp
    // cores without a per-stroke blur filter on every live frame (README
    // section 3, planner ruling 187 item 3; same technique as
    // `PlasmaFusion`'s `drawBloomSource`).
    function drawGlowSource(timeSec: number, orbitPhase: number) {
      glowCtx!.setTransform(GLOW_SCALE, 0, 0, GLOW_SCALE, 0, 0);
      glowCtx!.clearRect(0, 0, w, h);
      glowCtx!.globalCompositeOperation = "lighter";
      glowCtx!.lineCap = "round";
      // 8 samples here (vs the default 16 the static cache bakes once with)
      // -- these `liveStreaks` re-project every frame, so halving their
      // per-streak sample count keeps the longer 0.35-0.7 rad arcs (F2)
      // affordable at the raised live count (72) within the 4ms budget; the
      // cached majority (the visual bulk) still gets the full 16.
      for (const streak of liveStreaks) {
        const advanced: Streak = {
          ...streak,
          theta0: streak.theta0 + orbitPhase,
        };
        const pts = occludeBehindColumn(
          projectStreak(advanced, layout.torus, layout.camera, timeSec, true, 8),
          advanced.theta0,
          layout.camera,
          columnHalfWidthPx,
        );
        strokeShadedPath(glowCtx!, pts, BRAND.coral, 5.5, streak.alpha * 0.5);
      }
      const twistPhase = (timeSec / TWIST_PERIOD_S) * Math.PI * 2;
      const helixPts = occludeHelixBehindColumn(
        projectHelix(layout.torus, layout.camera, twistPhase),
        layout.camera,
        columnHalfWidthPx,
      );
      strokeShadedPath(glowCtx!, helixPts, BRAND.coral, 4.5, 0.28);
    }

    function drawLive(timeSec: number) {
      liveCtx!.setTransform(1, 0, 0, 1, 0, 0);
      liveCtx!.clearRect(0, 0, liveCanvas!.width, liveCanvas!.height);
      liveCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      liveCtx!.lineCap = "round";

      // The near-side band centre (see `bandCenter`), not the axis point:
      // the axis point projects the bloom/core ~70px above where the band
      // actually renders (hc-0-wrc.3 review 2, F2).
      const torusCenter = bandCenter();
      const breathe =
        0.86 + 0.14 * Math.sin((timeSec / BREATHE_PERIOD_S) * Math.PI * 2);
      const bloomR =
        (layout.torus.R + layout.torus.a) * torusCenter.scale * 0.4;

      liveCtx!.globalCompositeOperation = "lighter";
      const bloom = liveCtx!.createRadialGradient(
        torusCenter.x,
        torusCenter.y,
        0,
        torusCenter.x,
        torusCenter.y,
        bloomR,
      );
      bloom.addColorStop(0, hexToRgba(BRAND.coral, 0.16 * breathe));
      bloom.addColorStop(0.45, hexToRgba(BRAND.coral, 0.06 * breathe));
      bloom.addColorStop(1, hexToRgba(BRAND.coral, 0));
      liveCtx!.fillStyle = bloom;
      liveCtx!.beginPath();
      liveCtx!.arc(
        torusCenter.x,
        torusCenter.y,
        Math.max(1, bloomR),
        0,
        Math.PI * 2,
      );
      liveCtx!.fill();

      const orbitPhase = (timeSec / BAND_ORBIT_PERIOD_S) * Math.PI * 2;

      // Glow halo first (downscaled, blurred by the upscale), sharp cores
      // on top -- every luminous element on this layer carries a halo.
      drawGlowSource(timeSec, orbitPhase);
      liveCtx!.globalAlpha = 0.9;
      liveCtx!.drawImage(glow, 0, 0, glowW, glowH, 0, 0, w, h);
      liveCtx!.globalAlpha = 1;

      // The helix's far half (behind the column, per-point occluded) draws
      // BEFORE the front streaks below so they visually cross over it; its
      // near half draws AFTER, on top of the streaks -- "thread partly
      // hidden by the column and crossed by front streaks" (hc-0-wrc.3
      // review 2, F4). A thin, low-contrast core (not the old 1px/alpha
      // 0.85 hard white wire) plus a wider, low-alpha coral pass under it.
      const twistPhase = (timeSec / TWIST_PERIOD_S) * Math.PI * 2;
      const helixPts = occludeHelixBehindColumn(
        projectHelix(layout.torus, layout.camera, twistPhase),
        layout.camera,
        columnHalfWidthPx,
      );
      const { far: helixFar, near: helixNear } = splitByPredicate(
        helixPts,
        (p: HelixPoint) => Math.sin(p.theta) > 0,
      );
      const strokeHelixRuns = (runs: readonly HelixPoint[][]) => {
        for (const run of runs) {
          strokeShadedPath(liveCtx!, run, BRAND.coral, 4.5, 0.35);
          strokeShadedPathRgba(liveCtx!, run, [255, 244, 240], 1.2, 0.5);
        }
      };
      strokeHelixRuns(helixFar);

      const hotIndex = liveStreaks.length
        ? Math.floor(timeSec / HOT_STREAK_PERIOD_S) % liveStreaks.length
        : -1;
      for (let i = 0; i < liveStreaks.length; i++) {
        const streak = liveStreaks[i];
        const advanced: Streak = {
          ...streak,
          theta0: streak.theta0 + orbitPhase,
        };
        // Streaks on the torus' far side (opposite the camera) project at
        // greater depth, so `near` (from `nearFactor`) is already small,
        // drawing them dimmer and thinner purely from the projection; on
        // top of that, `occludeBehindColumn` dims the ones that fall
        // within the column's own projected width so they read as passing
        // behind it, not just further away (planner ruling 187 item 3).
        const pts = occludeBehindColumn(
          projectStreak(advanced, layout.torus, layout.camera, timeSec, true, 8),
          advanced.theta0,
          layout.camera,
          columnHalfWidthPx,
        );
        const flicker =
          0.65 +
          0.35 * Math.sin(timeSec * streak.flickerSpeed + streak.flickerPhase);
        const hot = i === hotIndex ? 1.8 : 1;
        strokeShadedPath(
          liveCtx!,
          pts,
          BRAND.coral,
          2.8 * hot,
          streak.alpha * flicker * 0.55 * hot,
        );
        strokeShadedPathRgba(
          liveCtx!,
          pts,
          [255, 255, 255],
          1 * hot,
          flicker * 0.8 * hot,
        );
      }

      strokeHelixRuns(helixNear);

      // Grown from the old 0.9 coefficient (hc-0-wrc.3 review 2, F3: "the
      // core glow grows 2-3x") -- `torusCenter` itself moved to the band's
      // near-side point (a materially larger `scale` than the old axis
      // point), so this coefficient alone reads as noticeably bigger than
      // 0.9 without needing the full 2.2 the planner measured off the old
      // (smaller-scale) anchor; 2.2 here over-saturated the whole band into
      // a solid white disc with no visible column at all.
      const coreR = torusCenter.scale * layout.torus.a * 1.1 * breathe;
      const core = liveCtx!.createRadialGradient(
        torusCenter.x,
        torusCenter.y,
        0,
        torusCenter.x,
        torusCenter.y,
        Math.max(1, coreR),
      );
      core.addColorStop(0, "rgba(255,255,255,0.7)");
      core.addColorStop(0.5, hexToRgba(BRAND.coral, 0.4 * breathe));
      core.addColorStop(1, hexToRgba(BRAND.coral, 0));
      liveCtx!.fillStyle = core;
      liveCtx!.beginPath();
      liveCtx!.arc(
        torusCenter.x,
        torusCenter.y,
        Math.max(1, coreR),
        0,
        Math.PI * 2,
      );
      liveCtx!.fill();

      liveCtx!.globalCompositeOperation = "source-over";
      featherEdge(liveCtx!);
    }

    drawLiveRef.current = drawLive;

    measure();
    drawLive(2);

    const RESIZE_DEBOUNCE_MS = 150;
    let resizeTimer: ReturnType<typeof setTimeout> | null = null;
    const ro = new ResizeObserver(() => {
      if (resizeTimer !== null) {
        clearTimeout(resizeTimer);
      }
      resizeTimer = setTimeout(() => {
        resizeTimer = null;
        if (disposed) {
          return;
        }
        measure();
        drawLive(2);
      }, RESIZE_DEBOUNCE_MS);
    });
    ro.observe(root);

    return () => {
      disposed = true;
      drawLiveRef.current = null;
      if (resizeTimer !== null) {
        clearTimeout(resizeTimer);
      }
      ro.disconnect();
    };
  }, []);

  useEffect(() => {
    if (!running) {
      return;
    }
    let raf = 0;
    const start = performance.now();
    const loop = (time: number) => {
      drawLiveRef.current?.((time - start) / 1000);
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, [running]);

  return (
    <div ref={rootRef} className="absolute inset-0" aria-hidden="true">
      <canvas ref={chamberRef} className="absolute inset-0 h-full w-full" />
      <canvas ref={plasmaCacheRef} className="absolute inset-0 h-full w-full" />
      <canvas ref={liveRef} className="absolute inset-0 h-full w-full" />
      <div
        className="absolute inset-0"
        style={{
          background: `linear-gradient(90deg, ${hexToRgba(BRAND.navy, 0.92)} 0%, ${hexToRgba(BRAND.navy, 0.62)} 42%, ${hexToRgba(BRAND.navy, 0)} 66%)`,
        }}
      />
      <div
        className="absolute inset-0 hidden md:block"
        style={{
          background: `linear-gradient(180deg, ${hexToRgba(BRAND.navy, 0)} 66%, ${hexToRgba(BRAND.navy, 0.9)} 98%)`,
        }}
      />
      <div
        className="absolute inset-0"
        style={{
          background: `linear-gradient(180deg, ${hexToRgba(BRAND.navy, 0.55)} 0%, ${hexToRgba(BRAND.navy, 0)} 16%)`,
        }}
      />
      <div
        className="absolute inset-0 md:hidden"
        style={{
          background: `linear-gradient(180deg, ${hexToRgba(BRAND.navy, 0.6)} 0%, ${hexToRgba(BRAND.navy, 0.6)} 72%, ${hexToRgba(BRAND.navy, 0)} 78%)`,
        }}
      />
      <div
        className="absolute inset-0 md:hidden"
        style={{
          background: `linear-gradient(180deg, ${hexToRgba(BRAND.navy, 0)} 94%, ${hexToRgba(BRAND.navy, 0.5)} 100%)`,
        }}
      />
    </div>
  );
}
