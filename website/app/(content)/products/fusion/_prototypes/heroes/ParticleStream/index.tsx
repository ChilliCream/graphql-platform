"use client";

import { useEffect, useRef } from "react";

import { CC } from "../../../tokens";
import { useElementMotion } from "../../../visuals/hooks";
import { SERVICE_SPECTRUM } from "../../spectrum";
import {
  type Layout,
  type Particle,
  type Rgb,
  computeLayout,
  createParticles,
  hexToRgb,
  mixRgb,
  particlePosition,
} from "./particles";

/**
 * Hero art: a canvas field of particles in the five service colours
 * streaming in from the right edge toward one attractor behind the copy,
 * turning white there and continuing out past the left edge as a single
 * river. The rest frame freezes every particle at its own evenly spread
 * starting point, which already traces the full shape of the flow.
 */

const PARTICLE_SEED = 0x9e3779b9;

interface Simulation {
  readonly particles: readonly Particle[];
  readonly laneRgb: readonly Rgb[];
  readonly whiteRgb: Rgb;
  layout: Layout;
}

/** Resolves a token's colour (e.g. `CC.white`, a CSS `var()` string) to an RGB triple, for canvas fillStyle. */
function resolveRgb(colorToken: string): Rgb {
  const probe = document.createElement("span");
  probe.style.position = "absolute";
  probe.style.visibility = "hidden";
  probe.style.color = colorToken;
  document.body.appendChild(probe);
  const computed = getComputedStyle(probe).color;
  document.body.removeChild(probe);
  const parts = computed.match(/\d+(?:\.\d+)?/g);
  if (!parts || parts.length < 3) {
    return [255, 255, 255];
  }
  return [Number(parts[0]), Number(parts[1]), Number(parts[2])];
}

function drawFrame(
  ctx: CanvasRenderingContext2D,
  sim: Simulation,
  time: number,
) {
  const { width, height, attractor } = sim.layout;
  ctx.clearRect(0, 0, width, height);

  const glowRadius = Math.max(width, height) * 0.22;
  const glow = ctx.createRadialGradient(
    attractor.x,
    attractor.y,
    0,
    attractor.x,
    attractor.y,
    glowRadius,
  );
  glow.addColorStop(0, `rgba(${sim.whiteRgb.join(",")},0.18)`);
  glow.addColorStop(1, `rgba(${sim.whiteRgb.join(",")},0)`);
  ctx.fillStyle = glow;
  ctx.beginPath();
  ctx.arc(attractor.x, attractor.y, glowRadius, 0, Math.PI * 2);
  ctx.fill();

  ctx.globalCompositeOperation = "lighter";
  for (const particle of sim.particles) {
    const frame = particlePosition(particle, time, sim.layout);
    if (frame.alpha <= 0.02) {
      continue;
    }
    const rgb =
      frame.colorMix >= 1
        ? sim.whiteRgb
        : mixRgb(sim.laneRgb[particle.lane], sim.whiteRgb, frame.colorMix);
    ctx.fillStyle = `rgba(${rgb.join(",")},${frame.alpha})`;
    ctx.beginPath();
    ctx.arc(frame.x, frame.y, particle.radius, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.globalCompositeOperation = "source-over";
}

export default function ParticleStream() {
  const rootRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const ctxRef = useRef<CanvasRenderingContext2D | null>(null);
  const simRef = useRef<Simulation | null>(null);
  const rafRef = useRef(0);
  const elapsedRef = useRef(0);
  const lastRef = useRef(0);
  const running = useElementMotion(rootRef);

  // One-time setup: build the particle field, size the canvas to the hero
  // and keep it sized as the hero resizes. Always draws at least the rest
  // frame, independent of whether motion is running.
  useEffect(() => {
    const root = rootRef.current;
    const canvas = canvasRef.current;
    if (!root || !canvas) {
      return;
    }
    const ctx = canvas.getContext("2d");
    if (!ctx) {
      return;
    }
    ctxRef.current = ctx;

    const sim: Simulation = {
      particles: createParticles(PARTICLE_SEED),
      laneRgb: SERVICE_SPECTRUM.map((stop) => hexToRgb(stop.color)),
      whiteRgb: resolveRgb(CC.white),
      layout: computeLayout(root.clientWidth, root.clientHeight),
    };
    simRef.current = sim;

    function resize() {
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      const width = root!.clientWidth;
      const height = root!.clientHeight;
      canvas!.width = Math.round(width * dpr);
      canvas!.height = Math.round(height * dpr);
      ctx!.setTransform(dpr, 0, 0, dpr, 0, 0);
      sim.layout = computeLayout(width, height);
      drawFrame(ctx!, sim, elapsedRef.current);
    }

    resize();

    const ro = new ResizeObserver(() => resize());
    ro.observe(root);

    return () => {
      ro.disconnect();
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current);
        rafRef.current = 0;
      }
    };
  }, []);

  // Motion gate: run the particle loop only while `useElementMotion` says
  // to. Stopping always parks on the deterministic time-zero rest frame.
  useEffect(() => {
    const ctx = ctxRef.current;
    const sim = simRef.current;
    if (!ctx || !sim) {
      return;
    }

    if (!running) {
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current);
        rafRef.current = 0;
      }
      elapsedRef.current = 0;
      lastRef.current = 0;
      drawFrame(ctx, sim, 0);
      return;
    }

    function loop(now: number) {
      const dt =
        lastRef.current > 0
          ? Math.min((now - lastRef.current) / 1000, 0.05)
          : 0;
      lastRef.current = now;
      elapsedRef.current += dt;
      drawFrame(ctx!, sim!, elapsedRef.current);
      rafRef.current = requestAnimationFrame(loop);
    }
    rafRef.current = requestAnimationFrame(loop);

    return () => {
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current);
        rafRef.current = 0;
      }
    };
  }, [running]);

  return (
    <div ref={rootRef} className="absolute inset-0" aria-hidden="true">
      <canvas ref={canvasRef} className="absolute inset-0 h-full w-full" />

      {/* Scrim so the hero copy stays readable over the stream and river. */}
      <div className="from-cc-bg via-cc-bg/85 pointer-events-none absolute inset-y-0 left-0 w-full max-w-4xl bg-gradient-to-r from-0% via-70% to-transparent" />
    </div>
  );
}
