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
  textZones: Rect[];
}

const TEXT_ZONE_PAD = 24;

function padRect(r: Rect, pad: number): Rect {
  return {
    x: r.x - pad,
    y: r.y - pad,
    width: r.width + pad * 2,
    height: r.height + pad * 2,
  };
}

/**
 * The per-row rects the contrast fix (F1) is measured against: the h1's
 * own text range, each rendered line of the paragraph (not its bounding
 * box, so a two-line paragraph gives two rows, not one tall one) and each
 * button, all relative to the canvas and padded the same 24px the
 * copy-clear probe uses elsewhere on this site.
 */
function measureTextZones(
  canvas: HTMLCanvasElement,
  copyEl: HTMLElement,
): Rect[] {
  const cb = canvas.getBoundingClientRect();
  const rel = (r: DOMRect): Rect => ({
    x: r.x - cb.x,
    y: r.y - cb.y,
    width: r.width,
    height: r.height,
  });
  const zones: Rect[] = [];
  const h1 = copyEl.querySelector("h1");
  if (h1) {
    const range = document.createRange();
    range.selectNodeContents(h1);
    zones.push(padRect(rel(range.getBoundingClientRect()), TEXT_ZONE_PAD));
  }
  const p = copyEl.querySelector("p");
  if (p) {
    // getClientRects() on the <p> itself would return its one block box;
    // a Range over its text gives one rect per wrapped line instead, the
    // real "per glyph row" the contrast fix measures against.
    const range = document.createRange();
    range.selectNodeContents(p);
    for (const line of Array.from(range.getClientRects())) {
      zones.push(padRect(rel(line), TEXT_ZONE_PAD));
    }
  }
  for (const btn of Array.from(copyEl.querySelectorAll("a"))) {
    zones.push(padRect(rel(btn.getBoundingClientRect()), TEXT_ZONE_PAD));
  }
  return zones;
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

    const engine: Engine = {
      w: 0,
      h: 0,
      mode: null,
      graph: null,
      copyRect: null,
      textZones: [],
    };

    const measureCopyRect = () => {
      if (!copyEl) {
        engine.copyRect = null;
        engine.textZones = [];
        return;
      }
      const cb = canvas.getBoundingClientRect();
      const rb = copyEl.getBoundingClientRect();
      engine.copyRect = {
        x: rb.x - cb.x,
        y: rb.y - cb.y,
        width: rb.width,
        height: rb.height,
      };
      engine.textZones = measureTextZones(canvas, copyEl);
    };

    const rebuild = () => {
      if (engine.w <= 0 || engine.h <= 0) {
        return;
      }
      engine.mode = modeForSize(engine.w, engine.h);
      engine.graph = buildGraph(
        engine.w,
        engine.h,
        engine.mode,
        engine.copyRect,
        engine.textZones,
      );
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
          rebuild();
          draw();
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
