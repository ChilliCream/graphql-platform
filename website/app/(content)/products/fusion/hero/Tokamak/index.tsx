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
import {
  computeLayout,
  MOBILE_BREAKPOINT,
  type CopyRect,
  type TokamakLayout,
} from "./sceneLayout";
import {
  buildColumnSilhouette,
  paintColumnLayer,
  paintPlasmaLayer,
  paintWall,
  strokeShadedPath,
  strokeShadedPathRgba,
} from "./paint";
import {
  computeFilamentState,
  computeSurge,
  createSparkDefs,
  createStreak,
  isFarSide,
  occludeHelixBehindColumn,
  projectHelix,
  projectSpark,
  projectStreak,
  splitByPredicate,
  surgeWeight,
  type ShadedPoint,
  type SparkDef,
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
// hc-0-540 fix direction 1 ("raise the live streak subset... roughly 40-60%
// live, the rest static"): the counts below are the ticket's ENERGETIC
// build. They only apply when `energetic` is true (see that flag's own doc
// in the mount effect) -- the reduced-motion build always uses the
// `LEGACY_*` counts a few lines down, byte-identical to the starting
// commit, so the parity gate never has to reconcile two different streak
// populations. Raised roughly 3-4x from the legacy live count rather than
// all the way to the 40-60% band-count share the fix direction names: at
// this ticket's per-streak draw cost (each live streak strokes its
// far/near runs twice -- colour, then a white-hot overlay -- plus once more
// into the glow pass), a literal 40-60% SHARE BY COUNT measured against the
// starting commit's frame budget headroom (roughly 2x avg at both required
// widths, see the ticket's own baseline comment) would not fit; the ticket
// itself allows this trade ("if the budget bites, animate fewer but longer
// streaks rather than fewer pixels of glow" -- see `Streak.orbitVariance`
// and `SPEED_ARC_SCALE_*`'s own doc for the "longer" half of that trade).
// Static count is trimmed alongside it so the total population (and
// so the band's own exposure/density) stays close to the pre-ticket total.
const DESKTOP_STATIC_STREAKS = 810;
const DESKTOP_LIVE_STREAKS = 162;
// STACKED's ring is typically closer in absolute size to the desktop ring
// than to the small mobile one (70-80% of a 768-1279px viewport, not a
// 375px one), so it reuses the desktop counts rather than the mobile ones.
const STACKED_STATIC_STREAKS = DESKTOP_STATIC_STREAKS;
const STACKED_LIVE_STREAKS = DESKTOP_LIVE_STREAKS;
// Close to the desktop count: the ring spans most of the mobile viewport,
// so the band needs close to desktop-level streak density to read as a
// continuous torus rather than a sparse scatter.
const MOBILE_STATIC_STREAKS = 650;
const MOBILE_LIVE_STREAKS = 280;

/**
 * The pre-ticket ("legacy") counts, used only when `energetic` is false
 * (the reduced-motion build) so that build reseeds the exact same static
 * cache and live pool the starting commit did -- see `CreateStreakOptions`'
 * own doc on why the shared `rand` stream additionally has to draw the same
 * number of randoms per streak either way.
 */
const LEGACY_DESKTOP_STATIC_STREAKS = 900;
const LEGACY_DESKTOP_LIVE_STREAKS = 72;
const LEGACY_STACKED_STATIC_STREAKS = LEGACY_DESKTOP_STATIC_STREAKS;
const LEGACY_STACKED_LIVE_STREAKS = LEGACY_DESKTOP_LIVE_STREAKS;
const LEGACY_MOBILE_STATIC_STREAKS = 820;
const LEGACY_MOBILE_LIVE_STREAKS = 56;
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

// hc-0-540 fix direction 2 ("2-3x today's angular speed"): the legacy
// period (28s/revolution) divided by this is ~2.8x, inside the ticket's
// range. Per-streak variance around that shared rate comes from
// `Streak.orbitVariance` (0.6-1.6x, drawn only when `energetic`), applied
// in `drawLive` below -- the shared ring no longer rotates as one rigid
// body.
const BAND_ORBIT_PERIOD_S = 9.5;
const TWIST_PERIOD_S = 8;
// hc-0-540 fix direction 5 ("breathe period 2-3s with a visible
// amplitude"): both the period and the amplitude (see `ENERGETIC_BREATHE_*`
// below) are raised from the legacy values.
const BREATHE_PERIOD_S = 2.6;
const ENERGETIC_BREATHE_BASE = 0.85;
const ENERGETIC_BREATHE_AMPLITUDE = 0.15;
/** hc-0-540 fix direction 3: the surge sweep's own target average streak count (within the ticket's 10-30 range), fed to `computeSurge`. */
const SURGE_TARGET_STREAKS = 26;
/** hc-0-540 fix direction 3's "brightens 1.5-2x": `computeSurge`'s own envelope already peaks at 1, so `hot` peaks at `1 + SURGE_BOOST_GAIN` -- 0.5 lands the surge's own peak at 1.5x, the low end of that range, paired with a small `DESKTOP_ALPHA_TRIM` reduction so a surge (which brightens 10-30 neighbouring streaks AT ONCE) still doesn't push the exposure gate's 85%-luminance fraction over its own 15% ceiling at 1440. */
const SURGE_BOOST_GAIN = 0.5;
/**
 * hc-0-540: every live streak's own alpha ceiling (the sharp strokes AND
 * their glow), per mode -- a no-op (1) in the legacy build. The pre-ticket
 * exposure was already right at the 15% ceiling (hc-0-wrc.3's own
 * baseline, "the 375 exposure sits at the 15% bar"); with more live
 * streaks, each fluctuating on its own phase (slow flicker, micro-flicker,
 * the surge sweep), the fraction of the band above 85% luminance at a
 * random instant rises even when every fluctuation is itself
 * mean-preserving (more independent oscillators raises the odds that
 * several land near their OWN peak at once, and `lighter` compositing sums
 * whatever is overlapping right then). Desktop/stacked's own live count is
 * raised far less than a literal 40-60% share would call for (the frame
 * budget's own headroom, see the streak-count constants' own doc), so
 * their exposure sits well under the ceiling and this SPENDS some of that
 * margin back (>1) to still read as energetic; mobile's own live count is
 * raised relatively more (its smaller band needs more streaks to clear the
 * motion-metric floor) and has the least exposure margin of the three
 * required widths, so it trims (<1) instead. Both tuned against
 * `540-exposure.cjs`/`540-motion.cjs` at 375/1440, not guessed.
 */
const DESKTOP_ALPHA_TRIM = 1.25;
const MOBILE_ALPHA_TRIM = 0.58;
/** hc-0-540 fix direction 2's "streak length varies with speed": scales a live streak's drawn arc by its own `orbitVariance` (0.6-1.6) -- a no-op (1x) when `!energetic`, matching the starting commit's own arc exactly. Raised (fewer live streaks than the fix direction's own 40-60%-by-count target fit the frame budget, see the streak-count constants' own doc) so each one covers more of the band, the "longer" half of the ticket's own "fewer but longer" trade. */
const SPEED_ARC_SCALE_BASE = 0.62;
const SPEED_ARC_SCALE_GAIN = 0.32;
/** hc-0-540 fix direction 6: sparks per scene, well under the exposure budget at this size/count. */
const DESKTOP_SPARK_COUNT = 10;
const STACKED_SPARK_COUNT = DESKTOP_SPARK_COUNT;
const MOBILE_SPARK_COUNT = 8;

/** Legacy-only (see the streak-count constants' own doc): the pre-ticket single "hot" streak's period and boost. */
const LEGACY_BAND_ORBIT_PERIOD_S = 28;
const LEGACY_TWIST_PERIOD_S = 20;
const LEGACY_BREATHE_PERIOD_S = 6.2;
const LEGACY_BREATHE_BASE = 0.86;
const LEGACY_BREATHE_AMPLITUDE = 0.14;
const HOT_STREAK_PERIOD_S = 3.4;
const HOT_STREAK_BOOST = 1.4;

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
 * filament at its centre, split into a static baked layer and a live layer
 * for a cheap-bloom effect that stays cheap under the frame budget.
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

    // hc-0-540: whether this mounted instance renders the ticket's
    // energetic plasma (raised live share, faster orbit/twist/breathe,
    // flicker, surges, filament reseed/forks, sparks) or the pre-ticket
    // scene exactly. Read once, directly from `matchMedia` rather than
    // through `useReducedMotionPreference`'s hook (whose own
    // `useSyncExternalStore`/`useReducedMotion` can still report their
    // default value on the very first render, before they settle) -- a
    // plain synchronous DOM read has no such race, and Playwright's own
    // `reducedMotion: "reduce"` context option (the reduced-motion
    // acceptance gate's own mechanism) sets this media feature before the
    // page ever loads, so it is already correct the first time this effect
    // runs. Fixed for this mounted instance's whole lifetime, same as
    // `buildScene`'s own streak population below (`lastMeasureSignature`'s
    // own doc: rebuilding on an incidental re-fire is exactly what this
    // scene avoids) -- a mid-session OS toggle is not reflected, matching
    // that same existing philosophy; the rAF loop's own `running` gate
    // (`useElementMotion`, unchanged by this ticket) is what actually stops
    // `drawLive` from ever being called again once reduced motion is on,
    // which is what the reduced-motion acceptance bar depends on.
    const energetic = !(
      typeof window !== "undefined" &&
      typeof window.matchMedia === "function" &&
      window.matchMedia("(prefers-reduced-motion: reduce)").matches
    );

    const rand = mulberry32(0x746f6b31);
    let layout: TokamakLayout = computeLayout(0, 0, null);
    let dpr = 1;
    let w = 0;
    let h = 0;
    let glowW = 0;
    let glowH = 0;
    let liveStreaks: Streak[] = [];
    let sparks: SparkDef[] = [];
    /** The column's own projected half-width at the plasma's height, in px -- see `occludeHelixBehindColumn`. Recomputed in `buildScene` whenever the layout changes. */
    let columnHalfWidthPx = 0;
    let disposed = false;
    // Development-only escape hatch (`?tokamakDebugColumn` on the URL, and
    // only outside production): skips every plasma draw in `drawLive` (the
    // far/near cached blits, glow, tint, bloom, core, live streaks, the
    // helix) so the live canvas shows only the wall and the opaque column
    // layer, with nothing to bleed through it -- used to sample the
    // column's own silhouette in isolation from a test script. Gated on
    // `NODE_ENV` as well as the query param, and never exposes a window
    // global: a test script locates the column's own bbox from its own
    // alpha scan of this canvas, and unit-checks `isFarSide`/
    // `splitByPredicate` via a direct import of `plasma.ts`, not a global.
    const hidePlasmaForDebug =
      process.env.NODE_ENV !== "production" &&
      typeof window !== "undefined" &&
      new URLSearchParams(window.location.search).has("tokamakDebugColumn");

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
      if (layout.mode === "sideBySide") {
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
      const totalStatic = energetic
        ? layout.mode === "mobile"
          ? MOBILE_STATIC_STREAKS
          : layout.mode === "stacked"
            ? STACKED_STATIC_STREAKS
            : DESKTOP_STATIC_STREAKS
        : layout.mode === "mobile"
          ? LEGACY_MOBILE_STATIC_STREAKS
          : layout.mode === "stacked"
            ? LEGACY_STACKED_STATIC_STREAKS
            : LEGACY_DESKTOP_STATIC_STREAKS;
      const totalLive = energetic
        ? layout.mode === "mobile"
          ? MOBILE_LIVE_STREAKS
          : layout.mode === "stacked"
            ? STACKED_LIVE_STREAKS
            : DESKTOP_LIVE_STREAKS
        : layout.mode === "mobile"
          ? LEGACY_MOBILE_LIVE_STREAKS
          : layout.mode === "stacked"
            ? LEGACY_STACKED_LIVE_STREAKS
            : LEGACY_DESKTOP_LIVE_STREAKS;
      const totalStray = Math.round(totalStatic * STRAY_FRACTION);

      // The column's projected half-width at the plasma's height: the
      // middle column row sits at the torus' own y (see `sceneLayout.ts`'s
      // `buildColumnRows`, waist row at `i=0`, `y=0`), so projecting its
      // edge (world x = radius) and comparing to the on-axis centre
      // (`camera.originX`) gives the span the helix's far-side points have
      // to fall inside to read as behind the column
      // (`occludeHelixBehindColumn`).
      const midColumnRow =
        layout.columnRows[Math.floor(layout.columnRows.length / 2)];
      const columnEdge = project(
        { x: midColumnRow.radius, y: layout.torus.y, z: layout.torus.z },
        layout.camera,
      );
      columnHalfWidthPx = Math.abs(columnEdge.x - layout.camera.originX);

      // Static streaks are frozen (never orbit), so their far/near split is
      // fixed once here and baked straight into two separate cached layers,
      // the far layer desaturated and dimmed. Split PER POINT (`p.theta`,
      // via `splitByPredicate`), not by the streak's own `theta0` alone: a
      // streak's 20-40deg arc can straddle the far/near boundary, and a
      // whole-streak split would draw its head or tail on the wrong side of
      // the column.
      const farPaths: ShadedPoint[][] = [];
      const nearPaths: ShadedPoint[][] = [];
      const pushStatic = (theta0: number, phi: number, stray: boolean) => {
        const pts = projectStreak(
          createStreak(rand, theta0, phi, { stray, energetic }),
          layout.torus,
          layout.camera,
          0,
          false,
        );
        const { far, near } = splitByPredicate(pts, (p) => isFarSide(p.theta));
        farPaths.push(...far);
        nearPaths.push(...near);
      };
      for (const [theta0, phi] of stratifiedAngles(totalStatic, rand)) {
        pushStatic(theta0, phi, false);
      }
      for (const [theta0, phi] of strayAngles(totalStray, rand)) {
        pushStatic(theta0, phi, true);
      }

      const liveAngles = stratifiedAngles(totalLive, rand);
      liveStreaks = liveAngles.map(([theta0, phi]) =>
        createStreak(rand, theta0, phi, { energetic }),
      );

      // hc-0-540 fix direction 6: only the energetic build ever spawns
      // sparks -- the legacy (reduced-motion) build must reproduce the
      // starting commit's frame exactly, which never had any.
      sparks = energetic
        ? createSparkDefs(
            rand,
            layout.mode === "mobile"
              ? MOBILE_SPARK_COUNT
              : layout.mode === "stacked"
                ? STACKED_SPARK_COUNT
                : DESKTOP_SPARK_COUNT,
          )
        : [];

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
      // Checkpoint 3 (mobile/stacked as one continuous scene): away from
      // the band, the column reads as low-alpha structure rather than an
      // opaque black block, so the wall stays visible around/through it
      // and behind the copy band -- `sideBySide` keeps the pre-ticket
      // fully opaque column (floor 1, `bandFadeAt`'s own no-op case).
      const columnBandFadeFloor = layout.mode === "sideBySide" ? 1 : 0.45;
      const columnTiles = buildChamberTiles(
        layout.columnRows,
        layout.columnThetaSegments,
        layout.camera,
        layout.torus,
        "column",
        columnBandFadeFloor,
      );
      const columnBase = buildColumnSilhouette(columnTiles);
      const lights: InstrumentLight[] = buildInstrumentLights(
        layout.wallRows,
        layout.wallLightRowRange,
        layout.camera,
        rand,
        6,
      );
      paintWall(wallCtx!, w, h, wallTiles, lights);
      const bandY = project(
        { x: 0, y: layout.torus.y, z: layout.torus.z },
        layout.camera,
      ).y;
      paintColumnLayer(columnCtx!, w, h, columnBase, columnTiles, bandY);

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

    // The copy block's own rendered rect, relative to the section, used to
    // choose STACKED's `artTop` and SIDE-BY-SIDE's `zoneRight` from the
    // real layout instead of magic widths. `right` reads the teaser
    // paragraph's own rect (`data-hero-teaser` on `FusionHero.tsx`'s `<p>`)
    // -- narrower than the copy block's own box at `xl:max-w-2xl` widths
    // (where the box reserves more room than the current teaser text uses)
    // and wider than the `text-balance` h1 and the left-aligned buttons,
    // so it is the copy's own real rightmost content. `bottom` reads the
    // button row's own bottom (`data-hero-actions` on `FusionHero.tsx`'s
    // wrapper around the `ButtonRow`), not the block's own `py-24` bottom
    // padding. `null` if either element genuinely is not in the DOM (never
    // in practice, since `measure()` runs after mount) -- callers fall
    // back to the previous fixed constants.
    // hc-0-540 housekeeping (planner comment 287, ratified option (b) in
    // ticket comments 370/371): a positional `querySelectorAll("p")[length
    // - 1]` / `lastElementChild` selection here used to be needed because
    // nothing in `FusionHero.tsx` identified the teaser paragraph or the
    // button row directly, and a `Range` over the whole `data-hero-copy`
    // block (tried first) does not reproduce the shipped rect exactly
    // (sub-pixel/line-box rounding moves `zoneRight` enough to fail the
    // header-masked reduced-motion parity gate, `test-results/
    // 540-reduced-diff.cjs`). The two dedicated attributes below replace
    // both the positional selection and that Range attempt: each query
    // resolves to the exact same element the positional selection always
    // picked (the teaser `<p>` and the button row), so the measured rect
    // is unchanged and reduced-motion parity stays exact.
    function measureCopyRect(): CopyRect | null {
      if (!root) {
        return null;
      }
      const teaserEl =
        document.querySelector<HTMLElement>("[data-hero-teaser]");
      const actionsEl = document.querySelector<HTMLElement>(
        "[data-hero-actions]",
      );
      if (!teaserEl || !actionsEl) {
        return null;
      }
      const rootBox = root.getBoundingClientRect();
      return {
        right: teaserEl.getBoundingClientRect().right - rootBox.left,
        bottom: actionsEl.getBoundingClientRect().bottom - rootBox.top,
      };
    }

    /**
     * The values that determine `layout`: a change in any of them is worth
     * a rebuild, a re-fire with the same values is not. Compared as a
     * plain string (cheap for four numbers, and simpler than a deep-equal)
     * so `remeasureIfChanged` -- used by the copy block's own
     * `ResizeObserver` entry and `document.fonts.ready`, both of which are
     * guaranteed to fire at least once even with no real size change --
     * can skip a rebuild that would draw nothing new: `buildScene`'s
     * static cache is drawn from a single shared random generator that
     * keeps advancing across calls (never reseeded, matching the
     * mount call's own single draw), so an unconditional rebuild on a
     * same-values re-fire would silently swap in a different streak
     * pattern for no visible reason. `root`'s own pre-existing
     * `ResizeObserver` entry (below) intentionally keeps calling
     * `measure()` unconditionally, exactly as before this ticket, so a
     * genuine resize is still always honoured.
     */
    let lastMeasureSignature = "";

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
      const copyRect = measureCopyRect();
      const signatureCopyRect = w < MOBILE_BREAKPOINT ? null : copyRect;
      lastMeasureSignature = `${w}x${h}:${signatureCopyRect?.right ?? "n"}:${signatureCopyRect?.bottom ?? "n"}`;
      layout = computeLayout(w, h, copyRect);
      buildScene();
    }

    /**
     * Re-measures only if `root`'s own size, or (at `stacked`/`sideBySide`
     * widths, where `computeLayout` actually reads it) the copy block's
     * own rect, moved since the last measure (see `lastMeasureSignature`).
     * Below the `mobile` breakpoint the copy rect is excluded on purpose:
     * `computeLayout`'s `mobile` branch never reads it, so a font-load
     * reflow that nudges the teaser paragraph by a sub-pixel would
     * otherwise still read as "changed" and force a rebuild -- and thus a
     * fresh reseed of `buildScene`'s random draw -- of a scene that would
     * come out geometrically identical either way (the pixel-parity gate
     * at 375).
     */
    function remeasureIfChanged() {
      const width = root!.clientWidth;
      const height = root!.clientHeight;
      const copyRect = width < MOBILE_BREAKPOINT ? null : measureCopyRect();
      const signature = `${width}x${height}:${copyRect?.right ?? "n"}:${copyRect?.bottom ?? "n"}`;
      if (signature === lastMeasureSignature) {
        return;
      }
      measure();
      drawLive(2);
    }

    function drawLive(timeSec: number) {
      liveCtx!.setTransform(1, 0, 0, 1, 0, 0);
      liveCtx!.clearRect(0, 0, liveCanvas!.width, liveCanvas!.height);
      liveCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      liveCtx!.lineCap = "round";

      // hc-0-540 fix direction 2: the legacy build keeps `BAND_ORBIT_PERIOD_S`
      // replaced by `LEGACY_BAND_ORBIT_PERIOD_S` (28s/rev, the starting
      // commit's own value) so its `orbitPhase` at any `timeSec` matches the
      // starting commit exactly.
      const orbitPeriod = energetic
        ? BAND_ORBIT_PERIOD_S
        : LEGACY_BAND_ORBIT_PERIOD_S;
      const orbitPhase = (timeSec / orbitPeriod) * Math.PI * 2;

      // hc-0-540 fix direction 4: `computeFilamentState` only runs in the
      // energetic build; the legacy build recomputes the starting commit's
      // own `twistPhase` formula directly (period 20s, default `windCount`
      // 3, no fork) so `projectHelix`'s output matches it exactly.
      const filament = energetic
        ? computeFilamentState(timeSec, TWIST_PERIOD_S)
        : {
            twistPhase: (timeSec / LEGACY_TWIST_PERIOD_S) * Math.PI * 2,
            windCount: 3,
            fork: null,
          };
      const helixPts = occludeHelixBehindColumn(
        projectHelix(
          layout.torus,
          layout.camera,
          filament.twistPhase,
          filament.windCount,
        ),
        layout.camera,
        columnHalfWidthPx,
      );
      const { far: helixFar, near: helixNear } = splitByPredicate(
        helixPts,
        (p: ShadedPoint) => isFarSide(p.theta),
      );
      // The filament's brief forking second thread (fix direction 4),
      // non-null only in the reseed window right after its wind count
      // actually changes -- see `computeFilamentState`'s own doc. Always
      // null in the legacy build.
      let helixForkFar: ShadedPoint[][] = [];
      let helixForkNear: ShadedPoint[][] = [];
      if (filament.fork) {
        const forkPts = occludeHelixBehindColumn(
          projectHelix(
            layout.torus,
            layout.camera,
            filament.fork.twistPhase,
            filament.fork.windCount,
          ),
          layout.camera,
          columnHalfWidthPx,
        );
        const forkSplit = splitByPredicate(forkPts, (p: ShadedPoint) =>
          isFarSide(p.theta),
        );
        helixForkFar = forkSplit.far;
        helixForkNear = forkSplit.near;
      }

      // Legacy-only: the pre-ticket single "hot" streak, unchanged formula.
      const hotIndex =
        !energetic && liveStreaks.length
          ? Math.floor(timeSec / HOT_STREAK_PERIOD_S) % liveStreaks.length
          : -1;
      // hc-0-540 fix direction 3: the surge sweep's state for this frame,
      // computed once and applied per streak below via `surgeWeight`. Never
      // computed in the legacy build (which keeps the old single-streak
      // `hotIndex` above instead).
      const surge = energetic
        ? computeSurge(timeSec, liveStreaks.length, SURGE_TARGET_STREAKS)
        : null;

      // hc-0-540 fix direction 2: `streak.orbitVariance` is 1 (a no-op) for
      // every streak in the legacy build (`createStreak`'s own doc), so
      // this reduces to the starting commit's own `theta0 + orbitPhase`
      // there.
      const advanced = liveStreaks.map((streak, index) => ({
        streak,
        index,
        theta0: streak.theta0 + orbitPhase * streak.orbitVariance,
      }));

      // Each live streak is projected once per frame here and its point
      // list split PER POINT (`p.theta`, via `splitByPredicate`), not by
      // the streak's own `theta0` alone: a streak's 20-40deg arc can
      // straddle the far/near boundary, and a whole-streak split would draw
      // its head or tail on the wrong side of the column. Streaks on the
      // torus' far side already project at greater depth (`near` is small
      // from `nearFactor`) and carry a lower `weight`/dimmer colour of
      // their own (see `createStreak`); the far runs below are additionally
      // drawn BEFORE the column layer and the near runs AFTER it, so the
      // column's own tiles occlude whichever far points actually fall
      // behind it -- real occlusion from draw order, not a hand-set
      // dimming factor. Splitting once here (reused by both `strokeGroup`
      // and `drawGlowGroup` below) also means `projectStreak` runs exactly
      // once per live streak per frame, not once per draw pass.
      const liveRuns = advanced.map(({ streak, index, theta0 }) => {
        // hc-0-540 fix direction 2's "streak length varies with speed": a
        // no-op (1x) in the legacy build, so `streak.arc * 1 === streak.arc`
        // and `projectStreak` receives the exact same numeric arc as the
        // starting commit.
        const arcScale = energetic
          ? SPEED_ARC_SCALE_BASE + SPEED_ARC_SCALE_GAIN * streak.orbitVariance
          : 1;
        const pts = projectStreak(
          { ...streak, theta0, arc: streak.arc * arcScale },
          layout.torus,
          layout.camera,
          timeSec,
          true,
          8,
        );
        const { far, near } = splitByPredicate(pts, (p) => isFarSide(p.theta));
        const hot = energetic
          ? 1 + surgeWeight(theta0, surge!) * SURGE_BOOST_GAIN
          : index === hotIndex
            ? HOT_STREAK_BOOST
            : 1;
        return { streak, index, far, near, hot };
      });

      const strokeGroup = (
        side: "far" | "near",
        colorHex: string,
        alphaMul: number,
      ) => {
        for (const { streak, far, near, hot } of liveRuns) {
          const runs = side === "far" ? far : near;
          if (runs.length === 0) {
            continue;
          }
          const slowFlicker =
            0.65 +
            0.35 *
              Math.sin(timeSec * streak.flickerSpeed + streak.flickerPhase);
          // hc-0-540 fix direction 3: a fast (3-8Hz), small-amplitude
          // flicker layered on top of the pre-ticket slow one -- two
          // harmonics rather than a single sine so it reads closer to
          // noise than a clean pulse. A no-op (1) in the legacy build,
          // whose streaks never drew a `microFlickerRate` (always 0
          // there).
          const microFlicker = energetic
            ? 1 +
              0.26 *
                Math.sin(
                  timeSec * streak.microFlickerRate * Math.PI * 2 +
                    streak.microFlickerPhase,
                ) +
              0.13 *
                Math.sin(
                  timeSec * streak.microFlickerRate * Math.PI * 2 * 1.7 +
                    streak.microFlickerPhase * 1.3,
                )
            : 1;
          const flicker = slowFlicker * microFlicker;
          const alphaTrim = energetic
            ? layout.mode === "mobile"
              ? MOBILE_ALPHA_TRIM
              : DESKTOP_ALPHA_TRIM
            : 1;
          for (const run of runs) {
            strokeShadedPath(
              liveCtx!,
              run,
              colorHex,
              2.4 * hot,
              streak.alpha * flicker * 0.4 * alphaMul * hot * alphaTrim,
            );
            strokeShadedPathRgba(
              liveCtx!,
              run,
              whiteToRgba,
              0.9 * hot,
              flicker * 0.3 * alphaMul * hot * alphaTrim,
            );
          }
        }
      };

      const strokeHelixRuns = (
        runs: readonly ShadedPoint[][],
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

      // hc-0-540 fix direction 4: the fork's own brief second thread,
      // brighter than the primary strand and fading with
      // `filament.fork.alpha` -- reads as the filament forking at the
      // reseed moment, one branch dying out. A no-op whenever
      // `filament.fork` is null (always null in the legacy build).
      const strokeHelixForkRuns = (
        runs: readonly ShadedPoint[][],
        colorHex: string,
        alphaMul: number,
      ) => {
        if (!filament.fork || runs.length === 0) {
          return;
        }
        const forkAlpha = filament.fork.alpha;
        for (const run of runs) {
          strokeShadedPath(
            liveCtx!,
            run,
            colorHex,
            5,
            0.5 * alphaMul * forkAlpha,
          );
          strokeShadedPathRgba(
            liveCtx!,
            run,
            warmWhiteToRgba,
            1.6,
            0.7 * alphaMul * forkAlpha,
          );
        }
      };

      // hc-0-540 fix direction 6: each spark's own short trailing run and
      // fade envelope for this frame, projected once and reused by both
      // the far and near passes below (same pattern as `liveRuns`). Always
      // empty in the legacy build (`sparks` is only ever populated when
      // `energetic`, see `buildScene`).
      const sparkRuns = sparks
        .map((spark) => {
          const projected = projectSpark(
            spark,
            layout.torus,
            layout.camera,
            timeSec,
          );
          if (!projected) {
            return null;
          }
          const { far, near } = splitByPredicate(projected.pts, (p) =>
            isFarSide(p.theta),
          );
          return { far, near, envelope: projected.envelope };
        })
        .filter(
          (
            s,
          ): s is {
            far: ShadedPoint[][];
            near: ShadedPoint[][];
            envelope: number;
          } => s !== null,
        );

      const strokeSparks = (
        side: "far" | "near",
        colorHex: string,
        alphaMul: number,
      ) => {
        for (const { far, near, envelope } of sparkRuns) {
          const runs = side === "far" ? far : near;
          for (const run of runs) {
            strokeShadedPath(
              liveCtx!,
              run,
              colorHex,
              2.2,
              0.35 * envelope * alphaMul,
            );
            strokeShadedPathRgba(
              liveCtx!,
              run,
              warmWhiteToRgba,
              1.6,
              0.6 * envelope * alphaMul,
            );
          }
        }
      };

      // Glow halo first (downscaled, blurred by the upscale), sharp cores
      // on top -- every luminous element on this layer carries a halo. Run
      // once per half (its own streak runs + its own helix run) so the far
      // half's bloom is dimmed and desaturated along with its streaks.
      const drawGlowGroup = (
        side: "far" | "near",
        helixRuns: readonly ShadedPoint[][],
        forkRuns: readonly ShadedPoint[][],
        sparkRuns: readonly {
          far: ShadedPoint[][];
          near: ShadedPoint[][];
          envelope: number;
        }[],
        colorHex: string,
        alphaMul: number,
      ) => {
        glowCtx!.setTransform(GLOW_SCALE, 0, 0, GLOW_SCALE, 0, 0);
        glowCtx!.clearRect(0, 0, w, h);
        glowCtx!.globalCompositeOperation = "lighter";
        glowCtx!.lineCap = "round";
        for (const { streak, far, near, hot } of liveRuns) {
          const runs = side === "far" ? far : near;
          // hc-0-540 fix direction 3's "a flare at each surge": boosting
          // the glow pass by the same `hot` the sharp pass uses gives the
          // surging streaks' OWN glow a local flare, not a global bloom
          // bump. Kept at 1 (unboosted, matching the starting commit
          // exactly) in the legacy build, whose `hot` is the old
          // single-streak boost and was never applied to the glow pass
          // before this ticket.
          const glowHot = energetic ? hot : 1;
          const glowAlphaTrim = energetic
            ? layout.mode === "mobile"
              ? MOBILE_ALPHA_TRIM
              : DESKTOP_ALPHA_TRIM
            : 1;
          for (const run of runs) {
            strokeShadedPath(
              glowCtx!,
              run,
              colorHex,
              5.5,
              streak.alpha * 0.5 * alphaMul * glowHot * glowAlphaTrim,
            );
          }
        }
        for (const run of helixRuns) {
          strokeShadedPath(glowCtx!, run, colorHex, 4.5, 0.28 * alphaMul);
        }
        if (filament.fork) {
          for (const run of forkRuns) {
            strokeShadedPath(
              glowCtx!,
              run,
              colorHex,
              5,
              0.3 * alphaMul * filament.fork.alpha,
            );
          }
        }
        // hc-0-540 fix (review 1 major 1): spark motes must carry a halo
        // like every other luminous element on this layer -- mirrors the
        // fork branch just above, scaled by each spark's own envelope so
        // the halo fades with the mote instead of a flat alpha.
        for (const { far, near, envelope } of sparkRuns) {
          const runs = side === "far" ? far : near;
          for (const run of runs) {
            strokeShadedPath(
              glowCtx!,
              run,
              colorHex,
              4.5,
              0.3 * envelope * alphaMul,
            );
          }
        }
        liveCtx!.globalCompositeOperation = "lighter";
        liveCtx!.globalAlpha = 0.9;
        liveCtx!.drawImage(glow, 0, 0, glowW, glowH, 0, 0, w, h);
        liveCtx!.globalAlpha = 1;
      };

      // ----- FAR step: the far half of the band (its cached static
      // majority, its live subset, its own glow and the filament's far
      // run) draws first, desaturated and dimmer, so the column layer
      // (stamped next) reads as standing in front of it. Skipped entirely
      // under `hidePlasmaForDebug` (see its own doc comment).
      if (!hidePlasmaForDebug) {
        liveCtx!.globalCompositeOperation = "lighter";
        blit(liveCtx!, farCanvas);
        drawGlowGroup(
          "far",
          helixFar,
          helixForkFar,
          sparkRuns,
          BRAND.coralSoft,
          FAR_ALPHA_MUL,
        );
        strokeGroup("far", BRAND.coralSoft, FAR_ALPHA_MUL);
        strokeHelixRuns(helixFar, BRAND.coralSoft, FAR_ALPHA_MUL);
        strokeHelixForkRuns(helixForkFar, BRAND.coralSoft, FAR_ALPHA_MUL);
        strokeSparks("far", BRAND.coralSoft, FAR_ALPHA_MUL);
      }

      // ----- COLUMN: the dark tiled column, cached once per `measure()`,
      // stamped fresh every frame so it always paints over the far arc and
      // under the near arc -- real occlusion from draw order, not a
      // dimming factor. Always drawn, debug or not.
      liveCtx!.globalCompositeOperation = "source-over";
      blit(liveCtx!, columnCanvas);

      if (!hidePlasmaForDebug) {
        // The column's own coral tint: a `'lighter'` radial pass drawn
        // right after the column layer so it visibly lands on the column
        // and the nearest wall tiles, not baked into the cache underneath
        // it.
        const torusCenter = bandCenter();
        // hc-0-540 fix direction 5: faster, visibly wider breathing, plus a
        // per-surge flare on top (fix direction 3's "a flare at each
        // surge") -- the legacy build reduces to the starting commit's own
        // `0.86 + 0.14*sin(...)` exactly (period, base and amplitude all
        // fall back to their `LEGACY_*` values, and `surge` is null so the
        // flare term is 0).
        const breathePeriod = energetic
          ? BREATHE_PERIOD_S
          : LEGACY_BREATHE_PERIOD_S;
        const breatheBase = energetic
          ? ENERGETIC_BREATHE_BASE
          : LEGACY_BREATHE_BASE;
        // hc-0-540: `MOBILE_WALL_LIFT_SCALE` (below) already widens the
        // tint/bloom/core reach on mobile, so this ticket's own amplitude
        // raise is halved there -- the exposure gate's own 375 measurement
        // (`540-exposure.cjs`) has the least margin of the three required
        // widths, unlike 768/1440.
        const breatheAmplitude =
          (energetic ? ENERGETIC_BREATHE_AMPLITUDE : LEGACY_BREATHE_AMPLITUDE) *
          (energetic && layout.mode === "mobile" ? 0.5 : 1);
        const surgeFlare = surge?.active ? surge.envelope : 0;
        const breathe =
          breatheBase +
          breatheAmplitude * Math.sin((timeSec / breathePeriod) * Math.PI * 2) +
          surgeFlare * (layout.mode === "mobile" ? 0.03 : 0.08);
        liveCtx!.globalCompositeOperation = "lighter";
        const wallLiftScale =
          layout.mode === "mobile" ? MOBILE_WALL_LIFT_SCALE : 1;
        // hc-0-540: `MOBILE_WALL_LIFT_SCALE` widens the tint/bloom's own
        // reach on mobile (pre-existing, unchanged), so their peak alpha is
        // trimmed here in the energetic build only -- these two gradients
        // change only as fast as `breathe` (a couple of seconds per cycle),
        // so trimming them costs the motion metric almost nothing (a 100ms
        // sample barely sees them move) while mattering a lot to the
        // exposure gate's own 85%-luminance fraction, which the mobile
        // band's smaller bbox is the most sensitive of the three required
        // widths to (tuned against `540-exposure.cjs`, not guessed). 1
        // (a no-op) in the legacy build and at every other width.
        const mobileTintBloomTrim =
          energetic && layout.mode === "mobile" ? 0.4 : 1;
        const tintR = Math.max(1, bandHalfHeightPx() * 0.9 * wallLiftScale);
        const tint = liveCtx!.createRadialGradient(
          torusCenter.x,
          torusCenter.y,
          0,
          torusCenter.x,
          torusCenter.y,
          tintR,
        );
        tint.addColorStop(
          0,
          hexToRgba(BRAND.coral, 0.14 * breathe * mobileTintBloomTrim),
        );
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
        bloom.addColorStop(
          0,
          hexToRgba(BRAND.coral, 0.16 * breathe * mobileTintBloomTrim),
        );
        bloom.addColorStop(
          0.45,
          hexToRgba(BRAND.coral, 0.06 * breathe * mobileTintBloomTrim),
        );
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
        drawGlowGroup(
          "near",
          helixNear,
          helixForkNear,
          sparkRuns,
          BRAND.coral,
          NEAR_ALPHA_MUL,
        );
        strokeGroup("near", BRAND.coral, NEAR_ALPHA_MUL);
        strokeHelixRuns(helixNear, BRAND.coral, NEAR_ALPHA_MUL);
        strokeHelixForkRuns(helixForkNear, BRAND.coral, NEAR_ALPHA_MUL);
        strokeSparks("near", BRAND.coral, NEAR_ALPHA_MUL);

        // `torusCenter` sits at the band's near-side point, not the torus'
        // axis point, so the core radius is derived from its own scale.
        const coreR = torusCenter.scale * layout.torus.a * 0.8 * breathe;
        const coreDensityScale =
          layout.mode === "mobile" ? MOBILE_CORE_SCALE : 1;
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
      }

      liveCtx!.globalCompositeOperation = "source-over";
      featherCopyClearZone(liveCtx!);

      // Checkpoint 3 (mobile/stacked as one continuous scene): the erase
      // above erases the WHOLE live canvas above the band on mobile/
      // stacked (`featherMobileTop`'s own gradient is opaque for every `y`
      // up to `artTop`, not just its own 24px feather strip), including
      // the column this frame just stamped in -- so it never reads as
      // structure continuing behind the copy, only ending abruptly at the
      // band. Re-stamping the column UNDER the erased result
      // (`destination-over`, only inside the erased band-and-above region)
      // brings it back there at its own low `bandFade` alpha (checkpoint
      // 1's `paintColumnLayer`), without touching the far/near plasma
      // split at the band (unaffected, drawn earlier) or reopening the
      // plasma's own copy-clear guarantee (the plasma layers are never
      // re-stamped here, only the column).
      if (layout.mode !== "sideBySide") {
        liveCtx!.save();
        liveCtx!.beginPath();
        liveCtx!.rect(0, 0, w, layout.artTop + 24);
        liveCtx!.clip();
        liveCtx!.globalCompositeOperation = "destination-over";
        blit(liveCtx!, columnCanvas);
        liveCtx!.restore();
        liveCtx!.globalCompositeOperation = "source-over";
      }
    }

    drawLiveRef.current = drawLive;

    measure();
    drawLive(2);

    const RESIZE_DEBOUNCE_MS = 150;
    let resizeTimer: ReturnType<typeof setTimeout> | null = null;
    // `root`'s own resize handling is unchanged from before this ticket --
    // unconditional, so a genuine resize is always honoured -- and stays
    // the only trigger that isn't guarded by `remeasureIfChanged`.
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

    // The copy block's own size (and so `artTop`/`zoneRight`) can change
    // independently of `root`'s own size -- most often when the fonts used
    // for the eyebrow/h1/paragraph finish loading and reflow the copy,
    // which `root`'s own `ResizeObserver` entry never fires for. A
    // separate observer (rather than adding the copy block to `ro` above)
    // keeps `root`'s own entry exactly as it was, and lets this one use
    // `remeasureIfChanged`'s guard: both it and `document.fonts.ready` are
    // guaranteed to fire at least once even with no real size change, and
    // an unconditional rebuild on that harmless re-fire would silently
    // swap the static cache's shared random generator to a different
    // streak pattern for no visible reason.
    const copyEl = document.querySelector<HTMLElement>("[data-hero-copy]");
    let copyResizeTimer: ReturnType<typeof setTimeout> | null = null;
    const copyRo = copyEl
      ? new ResizeObserver(() => {
          if (copyResizeTimer !== null) {
            clearTimeout(copyResizeTimer);
          }
          copyResizeTimer = setTimeout(() => {
            copyResizeTimer = null;
            if (!disposed) {
              remeasureIfChanged();
            }
          }, RESIZE_DEBOUNCE_MS);
        })
      : null;
    if (copyEl && copyRo) {
      copyRo.observe(copyEl);
    }
    let fontsCancelled = false;
    document.fonts?.ready
      .then(() => {
        if (!fontsCancelled && !disposed) {
          remeasureIfChanged();
        }
      })
      .catch(() => {
        // Font-load failures are not this scene's concern; the fallback
        // band/zone constants in `computeLayout` still apply.
      });

    return () => {
      disposed = true;
      fontsCancelled = true;
      drawLiveRef.current = null;
      if (resizeTimer !== null) {
        clearTimeout(resizeTimer);
      }
      if (copyResizeTimer !== null) {
        clearTimeout(copyResizeTimer);
      }
      ro.disconnect();
      copyRo?.disconnect();
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
        SIDE-BY-SIDE copy-column scrim (>= 1280, matching
        `sceneLayout.ts`'s `SIDE_BY_SIDE_BREAKPOINT`): the gradient stops
        are tuned so the paragraph, which extends further right than the
        h1, stays clear of the 7:1 contrast floor -- stops that fade out
        earlier leave the paragraph's tail sitting on too little scrim.
      */}
      <div
        className="absolute inset-0 hidden xl:block"
        style={{
          background: `linear-gradient(90deg, ${hexToRgba(BRAND.navy, 0.72)} 0%, ${hexToRgba(BRAND.navy, 0.52)} 54%, ${hexToRgba(BRAND.navy, 0)} 74%)`,
        }}
      />
      <div
        className="absolute inset-0 hidden xl:block"
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
        `mobile`/STACKED copy-band scrim (< 1280): now that the wall paints
        behind the copy band too, the scrim alpha is raised enough to hold
        the h1/paragraph 7:1 contrast floor against that painted structure.
      */}
      <div
        className="absolute inset-0 xl:hidden"
        style={{
          background: `linear-gradient(180deg, ${hexToRgba(BRAND.navy, 0.6)} 0%, ${hexToRgba(BRAND.navy, 0.6)} 72%, ${hexToRgba(BRAND.navy, 0)} 78%)`,
        }}
      />
      <div
        className="absolute inset-0 xl:hidden"
        style={{
          background: `linear-gradient(180deg, ${hexToRgba(BRAND.navy, 0)} 94%, ${hexToRgba(BRAND.navy, 0.5)} 100%)`,
        }}
      />
    </div>
  );
}
