"use client";

import { useEffect, useRef } from "react";

import { BRAND } from "../../../tokens";
import { useElementMotion } from "../../../visuals/hooks";
import { hexToRgba } from "./colors";
import {
  createStrand,
  reseedStrand,
  type FilamentPath,
  type Strand,
} from "./filaments";
import { computeLayout, type PlasmaLayout } from "./layout";
import { paintStatic } from "./paint";

const DESKTOP_STRANDS_PER_SPHERE = 26;
/** Fewer strands at 375: the beam and core flare must read through the knot. */
const MOBILE_STRANDS_PER_SPHERE = 14;
/** Offscreen bloom source, a fraction of the live canvas' CSS size. */
const BLOOM_SCALE = 0.22;
const CORE_PULSE_PERIOD_MS = 4800;
const BEAM_SHIMMER_PERIOD_MS = 3200;
const MOTE_COUNT = 14;
const MOTE_LIFE_MS = 2600;

interface Mote {
  readonly angle: number;
  readonly size: number;
  /** Random offset into the shared `MOTE_LIFE_MS` cycle, so motes desync. */
  readonly phase: number;
}

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

function strokePath(
  ctx: CanvasRenderingContext2D,
  path: FilamentPath,
  colorHex: string,
  alphaMul: number,
): void {
  if (path.points.length < 2) {
    return;
  }
  ctx.beginPath();
  ctx.moveTo(path.points[0].x, path.points[0].y);
  for (let i = 1; i < path.points.length; i++) {
    ctx.lineTo(path.points[i].x, path.points[i].y);
  }
  ctx.strokeStyle = hexToRgba(colorHex, path.alpha * alphaMul);
  ctx.lineWidth = path.width;
  ctx.stroke();
}

function strokeWhitePath(
  ctx: CanvasRenderingContext2D,
  path: FilamentPath,
  alphaMul: number,
): void {
  if (path.points.length < 2) {
    return;
  }
  ctx.beginPath();
  ctx.moveTo(path.points[0].x, path.points[0].y);
  for (let i = 1; i < path.points.length; i++) {
    ctx.lineTo(path.points[i].x, path.points[i].y);
  }
  ctx.strokeStyle = `rgba(255,255,255,${path.alpha * alphaMul})`;
  ctx.lineWidth = path.width;
  ctx.stroke();
}

/**
 * v11 - Plasma Fusion: two spheres of branching, lightning-like filaments
 * fusing into a white-hot core with a coral/amber flare and a full-width
 * beam. See `../README.md` for the shared hero contract this follows
 * (palette, lighting/depth, motion gating, technique).
 */
export default function PlasmaFusion() {
  const rootRef = useRef<HTMLDivElement>(null);
  const staticRef = useRef<HTMLCanvasElement>(null);
  const liveRef = useRef<HTMLCanvasElement>(null);
  const running = useElementMotion(rootRef);
  const drawLiveRef = useRef<((time: number) => void) | null>(null);
  const stepRef = useRef<((time: number) => void) | null>(null);

  // Mount once: size both canvases, prerender the static layer, draw one
  // rich rest frame immediately, and keep it in sync on resize. The RAF
  // loop below only ever paints on top of what this effect sets up.
  useEffect(() => {
    const root = rootRef.current;
    const staticCanvas = staticRef.current;
    const liveCanvas = liveRef.current;
    if (!root || !staticCanvas || !liveCanvas) {
      return;
    }
    const staticCtx = staticCanvas.getContext("2d");
    const liveCtx = liveCanvas.getContext("2d");
    const bloom = document.createElement("canvas");
    const bloomCtx = bloom.getContext("2d");
    if (!staticCtx || !liveCtx || !bloomCtx) {
      return;
    }

    const rand = mulberry32(0x9a1e2f04);
    let layout: PlasmaLayout = computeLayout(0, 0);
    let dpr = 1;
    let w = 0;
    let h = 0;
    let bloomW = 0;
    let bloomH = 0;
    let strandsA: Strand[] = [];
    let strandsB: Strand[] = [];
    let motes: Mote[] = [];
    let disposed = false;
    // Tracks the rAF timestamp of the previous step() call, so the first
    // call (and any call after a long gap, e.g. resuming from off-screen)
    // can shift every strand's absolute `nextReseedAt` into the current
    // timeline instead of reseeding all of them in the same frame.
    let lastStepTime = -1;

    function buildScene() {
      const sphereA = {
        x: layout.sphereA.x,
        y: layout.sphereA.y,
        radius: layout.radius,
      };
      const sphereB = {
        x: layout.sphereB.x,
        y: layout.sphereB.y,
        radius: layout.radius,
      };
      const strandsPerSphere = layout.mobile
        ? MOBILE_STRANDS_PER_SPHERE
        : DESKTOP_STRANDS_PER_SPHERE;
      strandsA = Array.from({ length: strandsPerSphere }, () =>
        createStrand(sphereA, layout.core, rand),
      );
      strandsB = Array.from({ length: strandsPerSphere }, () =>
        createStrand(sphereB, layout.core, rand),
      );
      motes = Array.from({ length: MOTE_COUNT }, () => ({
        angle: rand() * Math.PI * 2,
        size: 0.8 + rand() * 1.4,
        phase: rand() * MOTE_LIFE_MS,
      }));
    }

    function measure() {
      w = root!.clientWidth;
      h = root!.clientHeight;
      const base = Math.min(window.devicePixelRatio || 1, 2);
      const cap = Math.sqrt(4_000_000 / Math.max(1, w * h));
      dpr = Math.max(0.75, Math.min(base, cap));
      staticCanvas!.width = Math.max(1, Math.round(w * dpr));
      staticCanvas!.height = Math.max(1, Math.round(h * dpr));
      liveCanvas!.width = staticCanvas!.width;
      liveCanvas!.height = staticCanvas!.height;
      staticCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      liveCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      bloomW = Math.max(1, Math.round(w * BLOOM_SCALE));
      bloomH = Math.max(1, Math.round(h * BLOOM_SCALE));
      bloom.width = bloomW;
      bloom.height = bloomH;
      layout = computeLayout(w, h);
      buildScene();
      paintStatic(staticCtx!, w, h, layout);
    }

    // Re-seed just the strands whose independent timer has elapsed, in
    // small, staggered groups, never all at once.
    function step(time: number) {
      if (lastStepTime < 0 || time - lastStepTime > 300) {
        const shift = lastStepTime < 0 ? time : time - lastStepTime;
        for (const strand of strandsA) {
          strand.nextReseedAt += shift;
        }
        for (const strand of strandsB) {
          strand.nextReseedAt += shift;
        }
      }
      for (const strand of strandsA) {
        if (time >= strand.nextReseedAt) {
          reseedStrand(
            strand,
            { x: layout.sphereA.x, y: layout.sphereA.y, radius: layout.radius },
            layout.core,
            rand,
          );
          strand.nextReseedAt = time + strand.reseedInterval;
        }
      }
      for (const strand of strandsB) {
        if (time >= strand.nextReseedAt) {
          reseedStrand(
            strand,
            { x: layout.sphereB.x, y: layout.sphereB.y, radius: layout.radius },
            layout.core,
            rand,
          );
          strand.nextReseedAt = time + strand.reseedInterval;
        }
      }
      lastStepTime = time;
    }

    // A downscaled offscreen pass of the filaments + core, composited back
    // with "lighter" at full size: a cheap bloom without per-stroke blur.
    function drawBloomSource(time: number) {
      bloomCtx!.setTransform(BLOOM_SCALE, 0, 0, BLOOM_SCALE, 0, 0);
      bloomCtx!.clearRect(0, 0, w, h);
      bloomCtx!.globalCompositeOperation = "lighter";
      bloomCtx!.lineCap = "round";
      for (const strand of strandsA) {
        const flicker =
          0.82 +
          0.18 *
            Math.sin((time / 1000) * strand.flickerSpeed + strand.flickerPhase);
        for (const path of strand.paths) {
          strokePath(bloomCtx!, path, BRAND.cyan, flicker * 1.7);
        }
      }
      for (const strand of strandsB) {
        const flicker =
          0.82 +
          0.18 *
            Math.sin((time / 1000) * strand.flickerSpeed + strand.flickerPhase);
        for (const path of strand.paths) {
          strokePath(bloomCtx!, path, BRAND.cyan, flicker * 1.7);
        }
      }

      const pulse =
        0.85 + 0.15 * Math.sin((time / CORE_PULSE_PERIOD_MS) * Math.PI * 2);
      const coreRadius = layout.radius * 0.9 * pulse;
      const glow = bloomCtx!.createRadialGradient(
        layout.core.x,
        layout.core.y,
        0,
        layout.core.x,
        layout.core.y,
        coreRadius,
      );
      glow.addColorStop(0, "rgba(255,255,255,0.9)");
      glow.addColorStop(0.25, hexToRgba(BRAND.coral, 0.55 * pulse));
      glow.addColorStop(1, hexToRgba(BRAND.coral, 0));
      bloomCtx!.fillStyle = glow;
      bloomCtx!.beginPath();
      bloomCtx!.arc(layout.core.x, layout.core.y, coreRadius, 0, Math.PI * 2);
      bloomCtx!.fill();
    }

    function drawLive(time: number) {
      liveCtx!.setTransform(1, 0, 0, 1, 0, 0);
      liveCtx!.clearRect(0, 0, liveCanvas!.width, liveCanvas!.height);
      liveCtx!.setTransform(dpr, 0, 0, dpr, 0, 0);

      drawBloomSource(time);
      liveCtx!.globalCompositeOperation = "lighter";
      liveCtx!.globalAlpha = 0.85;
      liveCtx!.drawImage(bloom, 0, 0, bloomW, bloomH, 0, 0, w, h);
      liveCtx!.globalAlpha = 1;

      // Crisp near-white filament centrelines on top of the bloom.
      liveCtx!.lineCap = "round";
      for (const strand of strandsA) {
        const flicker =
          0.75 +
          0.25 *
            Math.sin(
              (time / 1000) * strand.flickerSpeed + strand.flickerPhase + 1.3,
            );
        for (const path of strand.paths) {
          strokeWhitePath(liveCtx!, path, flicker);
        }
      }
      for (const strand of strandsB) {
        const flicker =
          0.75 +
          0.25 *
            Math.sin(
              (time / 1000) * strand.flickerSpeed + strand.flickerPhase + 1.3,
            );
        for (const path of strand.paths) {
          strokeWhitePath(liveCtx!, path, flicker);
        }
      }

      // A few motes drifting outward from the core and fading; distance
      // scales with the sphere radius so they stay in proportion at every
      // width instead of a fixed pixel drift.
      liveCtx!.shadowBlur = 6;
      liveCtx!.shadowColor = hexToRgba(BRAND.cyan, 0.8);
      for (const m of motes) {
        const cycle =
          (((time + m.phase) % MOTE_LIFE_MS) + MOTE_LIFE_MS) % MOTE_LIFE_MS;
        const age = cycle / MOTE_LIFE_MS;
        const dist = age * layout.radius * 0.9;
        const mx = layout.core.x + Math.cos(m.angle) * dist;
        const my = layout.core.y + Math.sin(m.angle) * dist;
        const edge = Math.min(age / 0.15, 1) * Math.min((1 - age) / 0.35, 1);
        const alpha = Math.max(0, edge) * 0.85;
        if (alpha <= 0.02) {
          continue;
        }
        liveCtx!.fillStyle = `rgba(255,255,255,${alpha})`;
        liveCtx!.beginPath();
        liveCtx!.arc(mx, my, m.size, 0, Math.PI * 2);
        liveCtx!.fill();
      }
      liveCtx!.shadowBlur = 0;

      // Feather the filaments + bloom + motes out of the copy-clear zone:
      // this only cleans up stray pixels at the zone edge
      // (`artLeft`/`artTop`) -- the geometry itself already keeps the
      // spheres out of the zone, so this is never a full destination-out cut
      // over geometry that still overlaps the copy (planner ruling 2,
      // ticket hc-0-wrc.2 comment 152). The beam shimmer and the core flare
      // are drawn after this, so they still cross the full width/height.
      liveCtx!.globalCompositeOperation = "destination-out";
      if (!layout.mobile) {
        const featherLeft = layout.artLeft;
        const featherRight = layout.artLeft + 24;
        const fade = liveCtx!.createLinearGradient(
          featherLeft,
          0,
          featherRight,
          0,
        );
        fade.addColorStop(0, "rgba(0,0,0,1)");
        fade.addColorStop(1, "rgba(0,0,0,0)");
        liveCtx!.fillStyle = fade;
        liveCtx!.fillRect(0, 0, w, h);
      } else {
        const featherTop = layout.artTop;
        const featherBottom = layout.artTop + 24;
        const fade = liveCtx!.createLinearGradient(
          0,
          featherTop,
          0,
          featherBottom,
        );
        fade.addColorStop(0, "rgba(0,0,0,1)");
        fade.addColorStop(1, "rgba(0,0,0,0)");
        liveCtx!.fillStyle = fade;
        liveCtx!.fillRect(0, 0, w, h);
      }
      liveCtx!.globalCompositeOperation = "source-over";

      // Beam shimmer over the static base, brighter on mobile so it reads
      // through the denser mobile knot.
      const shimmer =
        0.55 + 0.45 * Math.sin((time / BEAM_SHIMMER_PERIOD_MS) * Math.PI * 2);
      const span = Math.max(w, 1);
      const cx = layout.core.x / span;
      const beamSideAlpha = layout.mobile ? 0.22 : 0.1;
      const beamFarAlpha = layout.mobile ? 0.25 : 0.12;
      const beamCenterMul = layout.mobile ? 0.75 : 0.5;
      const beam = liveCtx!.createLinearGradient(0, 0, span, 0);
      beam.addColorStop(0, hexToRgba(BRAND.cyan, 0));
      beam.addColorStop(
        Math.max(0, cx - 0.3),
        hexToRgba(BRAND.cyan, beamSideAlpha),
      );
      beam.addColorStop(cx, `rgba(255,255,255,${beamCenterMul * shimmer})`);
      beam.addColorStop(
        Math.min(1, cx + 0.3),
        hexToRgba(BRAND.cyan, beamFarAlpha),
      );
      beam.addColorStop(1, hexToRgba(BRAND.cyan, 0));
      liveCtx!.fillStyle = beam;
      liveCtx!.fillRect(0, layout.beamY - 1.5, w, 3);

      // White-hot core with a coral-then-amber flare, crisp on top. The
      // flare radius scales with the sphere radius instead of a fixed 26px,
      // so it reads at 375 (about 15px) as well as at 1440 (about 32px).
      const pulse =
        0.85 + 0.15 * Math.sin((time / CORE_PULSE_PERIOD_MS) * Math.PI * 2);
      const coreR = layout.radius * 0.2 * pulse;
      const core = liveCtx!.createRadialGradient(
        layout.core.x,
        layout.core.y,
        0,
        layout.core.x,
        layout.core.y,
        coreR,
      );
      core.addColorStop(0, "rgba(255,255,255,0.95)");
      core.addColorStop(0.4, hexToRgba(BRAND.amber, 0.6));
      core.addColorStop(1, hexToRgba(BRAND.coral, 0));
      liveCtx!.fillStyle = core;
      liveCtx!.beginPath();
      liveCtx!.arc(layout.core.x, layout.core.y, coreR, 0, Math.PI * 2);
      liveCtx!.fill();
    }

    drawLiveRef.current = drawLive;
    stepRef.current = step;

    measure();
    drawLive(0);

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
        drawLive(0);
      }, RESIZE_DEBOUNCE_MS);
    });
    ro.observe(root);

    return () => {
      disposed = true;
      drawLiveRef.current = null;
      stepRef.current = null;
      if (resizeTimer !== null) {
        clearTimeout(resizeTimer);
      }
      ro.disconnect();
    };
  }, []);

  // The animation loop runs only while in view, the tab is visible and
  // motion is allowed; otherwise the rest frame the effect above already
  // painted stays on screen untouched.
  useEffect(() => {
    if (!running) {
      return;
    }
    let raf = 0;
    const loop = (time: number) => {
      stepRef.current?.(time);
      drawLiveRef.current?.(time);
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, [running]);

  return (
    <div ref={rootRef} className="absolute inset-0" aria-hidden="true">
      <canvas ref={staticRef} className="absolute inset-0 h-full w-full" />
      <canvas ref={liveRef} className="absolute inset-0 h-full w-full" />
      <div
        className="absolute inset-0"
        style={{
          background: `linear-gradient(90deg, ${hexToRgba(BRAND.navy, 0.92)} 0%, ${hexToRgba(BRAND.navy, 0.6)} 40%, ${hexToRgba(BRAND.navy, 0)} 66%)`,
        }}
      />
      <div
        className="absolute inset-0 hidden md:block"
        style={{
          background: `linear-gradient(180deg, ${hexToRgba(BRAND.navy, 0)} 68%, ${hexToRgba(BRAND.navy, 0.92)} 98%)`,
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
          background: `linear-gradient(180deg, ${hexToRgba(BRAND.navy, 0)} 94%, ${hexToRgba(BRAND.navy, 0.92)} 100%)`,
        }}
      />
      <div
        className="absolute inset-0 md:hidden"
        style={{
          background: `linear-gradient(180deg, ${hexToRgba(BRAND.navy, 0.55)} 0%, ${hexToRgba(BRAND.navy, 0.55)} 72%, ${hexToRgba(BRAND.navy, 0)} 77%)`,
        }}
      />
    </div>
  );
}
