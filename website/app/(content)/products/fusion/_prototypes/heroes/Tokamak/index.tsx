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
import { project } from "./geometry";
import { computeLayout, type TokamakLayout } from "./layout";
import {
  paintChamber,
  paintPlasmaCache,
  strokeShadedPath,
  strokeShadedPathRgba,
} from "./paint";
import {
  createStreak,
  projectHelix,
  projectSpiralDashes,
  projectStreak,
  type ShadedPoint,
  type SpiralDash,
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
const DESKTOP_STATIC_STREAKS = 420;
const DESKTOP_LIVE_STREAKS = 48;
const MOBILE_STATIC_STREAKS = 210;
const MOBILE_LIVE_STREAKS = 26;
const SPECTRUM_STREAM_COUNT = 5;
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

interface SpiralSeed {
  readonly theta0: number;
  readonly outerRadius: number;
  readonly loops: number;
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
    let spirals: SpiralSeed[] = [];
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

    function buildScene() {
      const totalStatic = layout.mobile
        ? MOBILE_STATIC_STREAKS
        : DESKTOP_STATIC_STREAKS;
      const totalLive = layout.mobile
        ? MOBILE_LIVE_STREAKS
        : DESKTOP_LIVE_STREAKS;

      const staticAngles = stratifiedAngles(totalStatic, rand);
      const staticStreakPaths: ShadedPoint[][] = staticAngles.map(
        ([theta0, phi]) =>
          projectStreak(
            createStreak(rand, theta0, phi),
            layout.torus,
            layout.camera,
            0,
            false,
          ),
      );

      // Starts just outside the column's own flare, not out at the wall's
      // full bulge radius -- the wall sits mostly out of frame or deep in
      // z at this camera distance, so a spiral starting there would spend
      // almost its whole run invisible. Staying close to the ring keeps
      // every turn of the inward spiral on screen.
      const outerRadius = layout.torus.R * 2.35;
      spirals = Array.from({ length: SPECTRUM_STREAM_COUNT }, (_, i) => ({
        theta0: (i / SPECTRUM_STREAM_COUNT) * Math.PI * 2 + rand() * 0.4,
        outerRadius,
        loops: 2.4 + rand() * 1.1,
      }));
      const spiralStreams: SpiralDash[][] = spirals.map((seed) =>
        projectSpiralDashes(
          layout.torus,
          layout.camera,
          seed.theta0,
          seed.outerRadius,
          seed.loops,
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

      paintPlasmaCache(plasmaCtx!, w, h, staticStreakPaths, spiralStreams);
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
      for (const streak of liveStreaks) {
        const advanced: Streak = {
          ...streak,
          theta0: streak.theta0 + orbitPhase,
        };
        const pts = projectStreak(
          advanced,
          layout.torus,
          layout.camera,
          timeSec,
          true,
        );
        strokeShadedPath(glowCtx!, pts, BRAND.coral, 5.5, streak.alpha * 0.5);
      }
      const twistPhase = (timeSec / TWIST_PERIOD_S) * Math.PI * 2;
      const helixPts = projectHelix(layout.torus, layout.camera, twistPhase);
      strokeShadedPath(glowCtx!, helixPts, BRAND.coral, 4.5, 0.28);
    }

    function drawLive(timeSec: number) {
      liveCtx!.setTransform(1, 0, 0, 1, 0, 0);
      liveCtx!.clearRect(0, 0, liveCanvas!.width, liveCanvas!.height);
      liveCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      liveCtx!.lineCap = "round";

      const torusCenter = project(
        { x: 0, y: layout.torus.y, z: layout.torus.z },
        layout.camera,
      );
      const breathe =
        0.86 + 0.14 * Math.sin((timeSec / BREATHE_PERIOD_S) * Math.PI * 2);
      const bloomR =
        (layout.torus.R + layout.torus.a) * torusCenter.scale * 0.62;

      liveCtx!.globalCompositeOperation = "lighter";
      const bloom = liveCtx!.createRadialGradient(
        torusCenter.x,
        torusCenter.y,
        0,
        torusCenter.x,
        torusCenter.y,
        bloomR,
      );
      bloom.addColorStop(0, hexToRgba(BRAND.coral, 0.26 * breathe));
      bloom.addColorStop(0.45, hexToRgba(BRAND.coral, 0.11 * breathe));
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
        // greater depth, so `near` (from `nearFactor`) is already small --
        // they draw dimmer and thinner purely from the projection, the same
        // stand-in for "occluded by the column" every other shaded path in
        // this hero uses, never a hand-set 2D fade.
        const pts = projectStreak(
          advanced,
          layout.torus,
          layout.camera,
          timeSec,
          true,
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

      const twistPhase = (timeSec / TWIST_PERIOD_S) * Math.PI * 2;
      const helixPts = projectHelix(layout.torus, layout.camera, twistPhase);
      strokeShadedPath(liveCtx!, helixPts, BRAND.coral, 2, 0.24);
      strokeShadedPathRgba(liveCtx!, helixPts, [255, 244, 240], 1, 0.85);

      const coreR = torusCenter.scale * layout.torus.a * 0.9 * breathe;
      const core = liveCtx!.createRadialGradient(
        torusCenter.x,
        torusCenter.y,
        0,
        torusCenter.x,
        torusCenter.y,
        Math.max(1, coreR),
      );
      core.addColorStop(0, "rgba(255,255,255,0.85)");
      core.addColorStop(0.5, hexToRgba(BRAND.coral, 0.55 * breathe));
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
