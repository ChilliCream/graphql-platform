"use client";

import { useEffect, useRef } from "react";

import { rgba } from "./color";
import { buildGraph, type GraphModel, type LayoutMode } from "./graph";
import { buildScene, capDpr, type Scene } from "./sceneLayout";
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
  scene: Scene;
  graph: GraphModel | null;
  copyRect: Rect | null;
}

// A static 3D graph backdrop: the whole scene is a still frame, painted
// once on mount and again on resize (ResizeObserver), never from a
// requestAnimationFrame loop. The layout is always a pure function of the
// canvas's own measured size, computed in this draw path, never a
// separate "is this mobile" state that could ship a wrong first paint.
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
      scene: buildScene(1, 1),
      graph: null,
      copyRect: null,
    };

    const measureCopyRect = () => {
      if (!copyEl) {
        engine.copyRect = null;
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
    };

    const draw = () => {
      if (!engine.graph || engine.w <= 0 || engine.h <= 0) {
        return;
      }
      paint({
        ctx,
        w: engine.w,
        h: engine.h,
        camera: engine.scene.camera,
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
      engine.scene = buildScene(w, h);
      // Rebuilt on every resize, not only when the landscape/portrait mode
      // flips: a portrait cluster's own placement depends continuously on
      // the viewport's aspect (see graph.ts), not just the discrete mode,
      // and the graph build is cheap enough (well inside the render-cost
      // budget) to redo on each ResizeObserver callback.
      engine.mode = engine.scene.mode;
      engine.graph = buildGraph(engine.mode, w / h);
      measureCopyRect();
      draw();
    };

    const ro = new ResizeObserver(resize);
    ro.observe(canvas);
    const copyRo = copyEl
      ? new ResizeObserver(() => {
          measureCopyRect();
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
