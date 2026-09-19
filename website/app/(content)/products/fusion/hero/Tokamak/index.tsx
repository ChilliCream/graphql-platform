"use client";

import { useEffect, useRef } from "react";

import { BRAND } from "../../tokens";
import { useElementMotion } from "../../visuals/hooks";
import {
  buildChamberTiles,
  buildInstrumentLights,
  type InstrumentLight,
} from "./chamber";
import { hexToRgba, warmWhiteToRgba, whiteToRgba } from "./colors";
import { project, torusPoint } from "./geometry";
import { computeLayout, type TokamakLayout } from "./sceneLayout";
import {
  paintColumnLayer,
  paintPlasmaLayer,
  paintWall,
  strokeShadedPath,
  strokeShadedPathRgba,
} from "./paint";
import {
  createStreak,
  isFarSide,
  occludeHelixBehindColumn,
  projectHelix,
  projectStreak,
  splitByPredicate,
  type HelixPoint,
  type ShadedPoint,
  type Streak,
} from "./plasma";

/**
 * Streak counts per viewport size. Most of the hundreds of thin bright
 * streaks are the static majority, baked once into the cached plasma layer;
 * a small subset orbits live every frame so the average frame stays well
 * under the frame budget. Counts and per-streak arc length are tuned
 * together: longer tangential arcs at high density stack into a continuous
 * band under `lighter` compositing instead of reading as a scattered cloud
 * of dashes.
 */
const DESKTOP_STATIC_STREAKS = 900;
const DESKTOP_LIVE_STREAKS = 72;
// Close to the desktop count: the ring spans most of the mobile viewport,
// so the band needs close to desktop-level streak density to read as a
// continuous torus rather than a sparse scatter.
const MOBILE_STATIC_STREAKS = 820;
const MOBILE_LIVE_STREAKS = 56;
/** ~10% of the static majority, added on top as loose, further-dimmed streaks off the tube's own radius -- the reference's sparse strays thinning out above/below the band. */
const STRAY_FRACTION = 0.1;
/** Extra dampening on the white-hot core specifically: even at the enlarged mobile band, the core gradient concentrates into a larger share of the band than at desktop scale, so it stays damped independently. */
const MOBILE_CORE_SCALE = 0.3;
/**
 * Scales the column-tint and bloom radii (below) on mobile only: at the
 * enlarged mobile ring these radii, unscaled, reach only a small fraction
 * of the ring's own radius, so the wall would read as flat dark navy right
 * next to a bright ring instead of a lit chamber. Widening the radii (not
 * raising the per-tile wall fill, which stays untouched) lets the same
 * additive coral wash reach the visible wall above/below the band.
 */
const MOBILE_WALL_LIFT_SCALE = 2.2;
/** Alpha multiplier for the far half of the band (its own streaks, glow and helix run) on top of `BRAND.coralSoft`'s own desaturation, so the far half reads reduced in alpha and desaturated relative to the near half. */
const FAR_ALPHA_MUL = 0.5;
/** Alpha multiplier for the near half (on top of the base per-pass alphas in `paintPlasmaLayer`/`strokeGroup`) -- the outer-limb weight bias trims the band's own mean luminance, so the near half's own exposure is nudged back up rather than raising bloom to compensate. */
const NEAR_ALPHA_MUL = 1.35;
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
 * The sparse "stray" population: fully random (not stratified/weighted)
 * theta/phi so they scatter loosely across the whole band instead of
 * filling out its grid -- `createStreak`'s `stray` option then pushes them
 * off the tube's own radius and further dims them, thinning out above and
 * below the band.
 */
function strayAngles(
  count: number,
  rand: () => number,
): Array<readonly [number, number]> {
  const pairs: Array<readonly [number, number]> = [];
  for (let i = 0; i < count; i++) {
    pairs.push([rand() * Math.PI * 2, rand() * Math.PI * 2]);
  }
  return pairs;
}

/**
 * Tokamak: the Fusion hero's plasma scene, seen from inside the vessel -- a
 * dark tiled steel column and wall wrapping around the viewer, with a
 * coral/pink plasma torus of hundreds of orbiting streaks and a twisting
 * filament at its centre. See `../../_prototypes/heroes/README.md` for the
 * shared hero contract (palette, lighting/depth, motion gating, technique)
 * and `../../_prototypes/heroes/PlasmaFusion` for the static/live split and
 * cheap-bloom technique this follows.
 */
export default function Tokamak() {
  const rootRef = useRef<HTMLDivElement>(null);
  const wallRef = useRef<HTMLCanvasElement>(null);
  const liveRef = useRef<HTMLCanvasElement>(null);
  const running = useElementMotion(rootRef);
  const drawLiveRef = useRef<((timeSec: number) => void) | null>(null);

  useEffect(() => {
    const root = rootRef.current;
    const wallCanvas = wallRef.current;
    const liveCanvas = liveRef.current;
    if (!root || !wallCanvas || !liveCanvas) {
      return;
    }
    const wallCtx = wallCanvas.getContext("2d");
    const liveCtx = liveCanvas.getContext("2d");
    // The column and the plasma's far/near static caches are never added to
    // the DOM: they exist only as `drawImage` sources the live canvas
    // stamps every frame, in the order wall -> far arc -> column -> near
    // arc, so the column's own tiles can sit BETWEEN the two halves of the
    // orbiting band -- real occlusion from draw order, not a fourth
    // stacked canvas or a destination-out mask.
    const columnCanvas = document.createElement("canvas");
    const farCanvas = document.createElement("canvas");
    const nearCanvas = document.createElement("canvas");
    const columnCtx = columnCanvas.getContext("2d");
    const farCtx = farCanvas.getContext("2d");
    const nearCtx = nearCanvas.getContext("2d");
    const glow = document.createElement("canvas");
    const glowCtx = glow.getContext("2d");
    if (!wallCtx || !liveCtx || !columnCtx || !farCtx || !nearCtx || !glowCtx) {
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
    /** The column's own projected half-width at the plasma's height, in px -- see `occludeHelixBehindColumn`. Recomputed in `buildScene` whenever the layout changes. */
    let columnHalfWidthPx = 0;
    let disposed = false;

    // Erases the LIVE (plasma) canvas's own copy-clear zone -- the
    // ring/streaks/filament/bloom must never render under the copy, so
    // every live frame gets this destination-out fade from `artLeft` back
    // to 0 on desktop, a hard guarantee on top of the geometry already
    // keeping the band at 0.775w. On mobile it instead feathers the whole
    // scene's top edge (`featherMobileTop` below) so the plasma never
    // shows above the copy/button row. The wall canvas is never erased
    // this way: it keeps its structure under the copy at both widths
    // (low-alpha, under the DOM scrims); only the plasma canvas clears the
    // zone by erasure.
    function featherCopyClearZone(ctx: CanvasRenderingContext2D) {
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
        featherMobileTop(ctx);
      }
      ctx.globalCompositeOperation = "source-over";
    }

    // Feathers the top edge of the mobile band (the copy/button row sits
    // above `artTop`) so the plasma reads as ending there instead of being
    // cut off -- called only against the live/plasma canvas, via
    // `featherCopyClearZone` above. The wall canvas is never erased at
    // this edge: it paints behind the mobile copy band the same way it
    // paints behind the desktop copy column, at low alpha under the DOM
    // scrim.
    function featherMobileTop(ctx: CanvasRenderingContext2D) {
      const top = layout.artTop;
      const bottom = layout.artTop + 24;
      const fade = ctx.createLinearGradient(0, top, 0, bottom);
      fade.addColorStop(0, "rgba(0,0,0,1)");
      fade.addColorStop(1, "rgba(0,0,0,0)");
      ctx.fillStyle = fade;
      ctx.fillRect(0, 0, w, bottom);
    }

    // Blits an already-dpr-scaled offscreen cache (the column, or a plasma
    // half) onto the live canvas 1:1 in raw pixels -- `liveCtx`'s transform
    // is the CSS-unit dpr scale the path draws around it use, so this
    // briefly swaps to the identity transform (matching the cache's own raw
    // pixel buffer) and restores it, without touching the caller's current
    // `globalCompositeOperation`.
    function blit(ctx: CanvasRenderingContext2D, img: CanvasImageSource) {
      ctx.save();
      ctx.setTransform(1, 0, 0, 1, 0, 0);
      ctx.drawImage(img, 0, 0);
      ctx.restore();
    }

    // The band's own visual centre: the near-side point of the torus tube
    // (theta = -PI/2, the "front-facing" convention `isFarSide` and
    // `chamber.ts`'s column back-face cull both use, phi = 0 for the
    // tube's own mid-line), NOT the axis point `{x:0,y:torus.y,z:torus.z}`
    // the core/bloom used to project through. The axis point and the band's
    // actual near-side centre project to different screen y (the axis
    // point ignores the camera tilt's effect on the tube's own radius), so
    // projecting the axis point instead would put the core/bloom well
    // below the band it is meant to mark.
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
     * keeps the coral tint/bloom sized to what the band actually renders
     * as.
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
      const totalStray = Math.round(totalStatic * STRAY_FRACTION);

      // The column's projected half-width at the plasma's height: the
      // middle column row sits at the torus' own y (see `layout.ts`'s
      // `buildTaperedRows`, t=0), so projecting its edge (world x = radius)
      // and comparing to the on-axis centre (`camera.originX`) gives the
      // span the helix's far-side points have to fall inside to read as
      // behind the column (`occludeHelixBehindColumn`).
      const midColumnRow =
        layout.columnRows[Math.floor(layout.columnRows.length / 2)];
      const columnEdge = project(
        { x: midColumnRow.radius, y: layout.torus.y, z: layout.torus.z },
        layout.camera,
      );
      columnHalfWidthPx = Math.abs(columnEdge.x - layout.camera.originX);

      // Static streaks are frozen (never orbit), so their far/near split is
      // fixed once here -- `isFarSide` on each streak's own `theta0` -- and
      // baked straight into two separate cached layers, the far layer
      // desaturated and dimmed.
      const farPaths: ShadedPoint[][] = [];
      const nearPaths: ShadedPoint[][] = [];
      const pushStatic = (theta0: number, phi: number, stray: boolean) => {
        const pts = projectStreak(
          createStreak(rand, theta0, phi, { stray }),
          layout.torus,
          layout.camera,
          0,
          false,
        );
        (isFarSide(theta0) ? farPaths : nearPaths).push(pts);
      };
      for (const [theta0, phi] of stratifiedAngles(totalStatic, rand)) {
        pushStatic(theta0, phi, false);
      }
      for (const [theta0, phi] of strayAngles(totalStray, rand)) {
        pushStatic(theta0, phi, true);
      }

      const liveAngles = stratifiedAngles(totalLive, rand);
      liveStreaks = liveAngles.map(([theta0, phi]) =>
        createStreak(rand, theta0, phi),
      );

      // Wall tiles (wide at the plasma's height, narrowing to meet the
      // column above and below it) and column tiles (the near-constant-
      // radius cylinder) are built from two separate row families sharing
      // the same axis and camera, then painted into their own layers so
      // the plasma can be stamped between the two cached canvases every
      // frame.
      const wallTiles = buildChamberTiles(
        layout.wallRows,
        layout.wallThetaSegments,
        layout.camera,
        layout.torus,
        "wall",
      );
      const columnTiles = buildChamberTiles(
        layout.columnRows,
        layout.columnThetaSegments,
        layout.camera,
        layout.torus,
        "column",
      );
      const lights: InstrumentLight[] = buildInstrumentLights(
        layout.wallRows,
        layout.camera,
        rand,
        6,
      );
      paintWall(wallCtx!, w, h, wallTiles, lights);
      paintColumnLayer(columnCtx!, w, h, columnTiles);

      // The ring's projected width uses the camera's own base scale (the
      // scale at its aim depth), the same basis the ring's actual
      // left/right on-screen extent falls out of -- not the band centre's
      // own much-larger near-side scale.
      const ringWidthPx =
        (layout.torus.R + layout.torus.a) * layout.camera.baseScale * 2;
      paintPlasmaLayer(farCtx!, w, h, farPaths, ringWidthPx, {
        colorHex: BRAND.coralSoft,
        alphaMul: FAR_ALPHA_MUL,
      });
      paintPlasmaLayer(nearCtx!, w, h, nearPaths, ringWidthPx, {
        colorHex: BRAND.coral,
        alphaMul: NEAR_ALPHA_MUL,
      });
    }

    function measure() {
      w = root!.clientWidth;
      h = root!.clientHeight;
      const base = Math.min(window.devicePixelRatio || 1, 2);
      const cap = Math.sqrt(4_000_000 / Math.max(1, w * h));
      dpr = Math.max(0.75, Math.min(base, cap));
      for (const canvas of [
        wallCanvas,
        liveCanvas,
        columnCanvas,
        farCanvas,
        nearCanvas,
      ]) {
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

    function drawLive(timeSec: number) {
      liveCtx!.setTransform(1, 0, 0, 1, 0, 0);
      liveCtx!.clearRect(0, 0, liveCanvas!.width, liveCanvas!.height);
      liveCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      liveCtx!.lineCap = "round";

      const orbitPhase = (timeSec / BAND_ORBIT_PERIOD_S) * Math.PI * 2;
      const twistPhase = (timeSec / TWIST_PERIOD_S) * Math.PI * 2;
      const helixPts = occludeHelixBehindColumn(
        projectHelix(layout.torus, layout.camera, twistPhase),
        layout.camera,
        columnHalfWidthPx,
      );
      const { far: helixFar, near: helixNear } = splitByPredicate(
        helixPts,
        (p: HelixPoint) => isFarSide(p.theta),
      );

      const hotIndex = liveStreaks.length
        ? Math.floor(timeSec / HOT_STREAK_PERIOD_S) % liveStreaks.length
        : -1;
      const advanced = liveStreaks.map((streak, index) => ({
        streak,
        index,
        theta0: streak.theta0 + orbitPhase,
      }));
      const farLive = advanced.filter((a) => isFarSide(a.theta0));
      const nearLive = advanced.filter((a) => !isFarSide(a.theta0));

      // Streaks on the torus' far side (opposite the camera) already
      // project at greater depth (`near` is small from `nearFactor`) and
      // carry a lower `weight`/dimmer colour of their own (see
      // `createStreak`); the far/near split below additionally draws them
      // BEFORE the column layer and the near group AFTER it, so the
      // column's own tiles occlude whichever far streaks actually fall
      // behind it -- real occlusion from draw order, not a hand-set
      // dimming factor.
      const strokeGroup = (
        group: readonly { streak: Streak; index: number; theta0: number }[],
        colorHex: string,
        alphaMul: number,
      ) => {
        for (const { streak, index, theta0 } of group) {
          const pts = projectStreak(
            { ...streak, theta0 },
            layout.torus,
            layout.camera,
            timeSec,
            true,
            8,
          );
          const flicker =
            0.65 +
            0.35 *
              Math.sin(timeSec * streak.flickerSpeed + streak.flickerPhase);
          const hot = index === hotIndex ? 1.4 : 1;
          strokeShadedPath(
            liveCtx!,
            pts,
            colorHex,
            2.4 * hot,
            streak.alpha * flicker * 0.4 * alphaMul * hot,
          );
          strokeShadedPathRgba(
            liveCtx!,
            pts,
            whiteToRgba,
            0.9 * hot,
            flicker * 0.3 * alphaMul * hot,
          );
        }
      };

      const strokeHelixRuns = (
        runs: readonly HelixPoint[][],
        colorHex: string,
        alphaMul: number,
      ) => {
        for (const run of runs) {
          strokeShadedPath(liveCtx!, run, colorHex, 4.5, 0.35 * alphaMul);
          strokeShadedPathRgba(
            liveCtx!,
            run,
            warmWhiteToRgba,
            1.2,
            0.5 * alphaMul,
          );
        }
      };

      // Glow halo first (downscaled, blurred by the upscale), sharp cores
      // on top -- every luminous element on this layer carries a halo. Run
      // once per half (its own streaks + its own helix run) so the far
      // half's bloom is dimmed and desaturated along with its streaks.
      const drawGlowGroup = (
        group: readonly { streak: Streak; theta0: number }[],
        helixRuns: readonly HelixPoint[][],
        colorHex: string,
        alphaMul: number,
      ) => {
        glowCtx!.setTransform(GLOW_SCALE, 0, 0, GLOW_SCALE, 0, 0);
        glowCtx!.clearRect(0, 0, w, h);
        glowCtx!.globalCompositeOperation = "lighter";
        glowCtx!.lineCap = "round";
        // 8 samples here (vs the default 16 the static cache bakes once
        // with) -- these `liveStreaks` re-project every frame, so halving
        // their per-streak sample count keeps the longer 0.35-0.7 rad arcs
        // affordable within the 4ms budget; the cached majority (the
        // visual bulk) still gets the full 16.
        for (const { streak, theta0 } of group) {
          const pts = projectStreak(
            { ...streak, theta0 },
            layout.torus,
            layout.camera,
            timeSec,
            true,
            8,
          );
          strokeShadedPath(
            glowCtx!,
            pts,
            colorHex,
            5.5,
            streak.alpha * 0.5 * alphaMul,
          );
        }
        for (const run of helixRuns) {
          strokeShadedPath(glowCtx!, run, colorHex, 4.5, 0.28 * alphaMul);
        }
        liveCtx!.globalCompositeOperation = "lighter";
        liveCtx!.globalAlpha = 0.9;
        liveCtx!.drawImage(glow, 0, 0, glowW, glowH, 0, 0, w, h);
        liveCtx!.globalAlpha = 1;
      };

      // ----- FAR step: the far half of the band (its cached static
      // majority, its live subset, its own glow and the filament's far
      // run) draws first, desaturated and dimmer, so the column layer
      // (stamped next) reads as standing in front of it.
      liveCtx!.globalCompositeOperation = "lighter";
      blit(liveCtx!, farCanvas);
      drawGlowGroup(farLive, helixFar, BRAND.coralSoft, FAR_ALPHA_MUL);
      strokeGroup(farLive, BRAND.coralSoft, FAR_ALPHA_MUL);
      strokeHelixRuns(helixFar, BRAND.coralSoft, FAR_ALPHA_MUL);

      // ----- COLUMN: the dark tiled column, cached once per `measure()`,
      // stamped fresh every frame so it always paints over the far arc and
      // under the near arc -- real occlusion from draw order, not a
      // dimming factor.
      liveCtx!.globalCompositeOperation = "source-over";
      blit(liveCtx!, columnCanvas);

      // The column's own coral tint: a `'lighter'` radial pass drawn right
      // after the column layer so it visibly lands on the column and the
      // nearest wall tiles, not baked into the cache underneath it.
      const torusCenter = bandCenter();
      const breathe =
        0.86 + 0.14 * Math.sin((timeSec / BREATHE_PERIOD_S) * Math.PI * 2);
      liveCtx!.globalCompositeOperation = "lighter";
      const wallLiftScale = layout.mobile ? MOBILE_WALL_LIFT_SCALE : 1;
      const tintR = Math.max(1, bandHalfHeightPx() * 0.9 * wallLiftScale);
      const tint = liveCtx!.createRadialGradient(
        torusCenter.x,
        torusCenter.y,
        0,
        torusCenter.x,
        torusCenter.y,
        tintR,
      );
      tint.addColorStop(0, hexToRgba(BRAND.coral, 0.14 * breathe));
      tint.addColorStop(1, hexToRgba(BRAND.coral, 0));
      liveCtx!.fillStyle = tint;
      liveCtx!.beginPath();
      liveCtx!.arc(torusCenter.x, torusCenter.y, tintR, 0, Math.PI * 2);
      liveCtx!.fill();

      // ----- NEAR step: the breathing bloom, the near half of the band
      // (cached majority + live subset + glow), the filament's near run
      // and the white-hot core all draw on top of the column, full
      // brightness.
      const bloomR =
        (layout.torus.R + layout.torus.a) *
        torusCenter.scale *
        0.4 *
        wallLiftScale;
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

      blit(liveCtx!, nearCanvas);
      drawGlowGroup(nearLive, helixNear, BRAND.coral, NEAR_ALPHA_MUL);
      strokeGroup(nearLive, BRAND.coral, NEAR_ALPHA_MUL);
      strokeHelixRuns(helixNear, BRAND.coral, NEAR_ALPHA_MUL);

      // `torusCenter` sits at the band's near-side point, not the torus'
      // axis point, so the core radius is derived from its own scale.
      const coreR = torusCenter.scale * layout.torus.a * 0.8 * breathe;
      const coreDensityScale = layout.mobile ? MOBILE_CORE_SCALE : 1;
      const core = liveCtx!.createRadialGradient(
        torusCenter.x,
        torusCenter.y,
        0,
        torusCenter.x,
        torusCenter.y,
        Math.max(1, coreR),
      );
      core.addColorStop(0, whiteToRgba(0.35 * coreDensityScale));
      core.addColorStop(
        0.5,
        hexToRgba(BRAND.coral, 0.25 * breathe * coreDensityScale),
      );
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
      featherCopyClearZone(liveCtx!);
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
      <canvas ref={wallRef} className="absolute inset-0 h-full w-full" />
      <canvas ref={liveRef} className="absolute inset-0 h-full w-full" />
      {/*
        Desktop copy-column scrim: the gradient stops are tuned so the
        paragraph, which extends further right than the h1, stays clear of
        the 7:1 contrast floor -- stops that fade out earlier leave the
        paragraph's tail sitting on too little scrim.
      */}
      <div
        className="absolute inset-0 hidden md:block"
        style={{
          background: `linear-gradient(90deg, ${hexToRgba(BRAND.navy, 0.72)} 0%, ${hexToRgba(BRAND.navy, 0.52)} 54%, ${hexToRgba(BRAND.navy, 0)} 74%)`,
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
      {/*
        Mobile copy-band scrim: now that the wall paints behind the mobile
        copy band too, the scrim alpha is raised enough to hold the h1/
        paragraph 7:1 contrast floor against that painted structure.
      */}
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
