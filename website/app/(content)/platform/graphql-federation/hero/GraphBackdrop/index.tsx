"use client";

import { useEffect, useRef } from "react";

import { rgba } from "./color";
import { buildGraph, type GraphModel, type LayoutMode } from "./graph";
import { capDpr, modeForSize } from "./sceneLayout";
import { paint, type Rect } from "./paint";
import { NAVY } from "../palette";

// A CSS-only vignette so that with JS disabled (no canvas paint at all)
// the hero still reads as an intentional dark scene -- the page's own navy
// background plus a soft radial darkening at the corners -- instead of a
// flat, broken-looking rectangle.
const FALLBACK_VIGNETTE = `radial-gradient(ellipse at 50% 50%, ${rgba(NAVY, 0)} 45%, ${rgba(NAVY, 0.55)} 100%)`;

interface Engine {
  w: number;
  h: number;
  mode: LayoutMode | null;
  graph: GraphModel | null;
  copyRect: Rect | null;
  paragraphRect: Rect | null;
  ctaRect: Rect | null;
}

interface BuildInputs {
  readonly w: number;
  readonly h: number;
  readonly mode: LayoutMode;
  readonly copyRect: Rect | null;
}

function sameRect(a: Rect | null, b: Rect | null): boolean {
  if (a === b) {
    return true;
  }
  if (!a || !b) {
    return false;
  }
  return (
    a.x === b.x && a.y === b.y && a.width === b.width && a.height === b.height
  );
}

function sameInputs(a: BuildInputs | null, b: BuildInputs): boolean {
  return (
    !!a &&
    a.w === b.w &&
    a.h === b.h &&
    a.mode === b.mode &&
    sameRect(a.copyRect, b.copyRect)
  );
}

// A static constellation backdrop: the whole scene is a still frame,
// painted once on mount and again on resize (ResizeObserver), never from a
// requestAnimationFrame loop. The layout is always a pure function of the
// canvas's own measured size and the copy block's own measured rect,
// computed in this draw path, never a separate "is this mobile" state that
// could ship a wrong first paint.
export function GraphBackdrop() {
  const rootRef = useRef<HTMLDivElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

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

    const section = root.parentElement;
    const copyEl =
      section?.querySelector<HTMLElement>("[data-hero-copy]") ?? null;
    const paragraphEl =
      section?.querySelector<HTMLElement>("[data-hero-paragraph]") ?? null;
    const ctaEl =
      section?.querySelector<HTMLElement>("[data-hero-cta]") ?? null;

    const engine: Engine = {
      w: 0,
      h: 0,
      mode: null,
      graph: null,
      copyRect: null,
      paragraphRect: null,
      ctaRect: null,
    };
    let lastBuild: BuildInputs | null = null;

    const relRect = (rb: DOMRect, cb: DOMRect): Rect => ({
      x: rb.x - cb.x,
      y: rb.y - cb.y,
      width: rb.width,
      height: rb.height,
    });

    const measureCopyRect = () => {
      const cb = canvas.getBoundingClientRect();
      engine.copyRect = copyEl
        ? relRect(copyEl.getBoundingClientRect(), cb)
        : null;
      engine.paragraphRect = paragraphEl
        ? relRect(paragraphEl.getBoundingClientRect(), cb)
        : null;
      engine.ctaRect = ctaEl
        ? relRect(ctaEl.getBoundingClientRect(), cb)
        : null;
    };

    // ResizeObserver fires once on observe() even with no real change (both
    // the canvas's own and the copy block's), so a plain mount without this
    // guard rebuilds the same graph up to three times. Skipping the build
    // when w, h, mode and the copy rect all match the last build keeps it
    // to once per actually-distinct input.
    const rebuild = (): boolean => {
      if (engine.w <= 0 || engine.h <= 0) {
        return false;
      }
      const mode = modeForSize(engine.w, engine.h);
      const inputs: BuildInputs = {
        w: engine.w,
        h: engine.h,
        mode,
        copyRect: engine.copyRect,
      };
      engine.mode = mode;
      if (sameInputs(lastBuild, inputs)) {
        return false;
      }
      engine.graph = buildGraph(engine.w, engine.h, mode, engine.copyRect);
      lastBuild = inputs;
      return true;
    };

    const draw = () => {
      if (!engine.graph || engine.w <= 0 || engine.h <= 0) {
        return;
      }
      paint({
        ctx,
        w: engine.w,
        h: engine.h,
        graph: engine.graph,
        copyRect: engine.copyRect,
        paragraphRect: engine.paragraphRect,
        ctaRect: engine.ctaRect,
      });
    };

    const resize = () => {
      const w = canvas.clientWidth;
      const h = canvas.clientHeight;
      if (w <= 0 || h <= 0) {
        return;
      }
      const dpr = capDpr(w, h, window.devicePixelRatio || 1);
      canvas.width = Math.max(1, Math.round(w * dpr));
      canvas.height = Math.max(1, Math.round(h * dpr));
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      engine.w = w;
      engine.h = h;
      measureCopyRect();
      rebuild();
      draw();
    };

    const ro = new ResizeObserver(resize);
    ro.observe(canvas);
    const copyRo = copyEl
      ? new ResizeObserver(() => {
          measureCopyRect();
          // Unlike resize(), this never resets the canvas backing store, so
          // a skipped rebuild has nothing new to paint.
          if (rebuild()) {
            draw();
          }
        })
      : null;
    if (copyEl && copyRo) {
      copyRo.observe(copyEl);
    }
    resize();

    return () => {
      ro.disconnect();
      copyRo?.disconnect();
    };
  }, []);

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="absolute inset-0 overflow-hidden"
      style={{ backgroundImage: FALLBACK_VIGNETTE }}
    >
      <canvas
        ref={canvasRef}
        style={{ display: "block", width: "100%", height: "100%" }}
      />
    </div>
  );
}
