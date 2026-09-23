"use client";

import { useEffect, useRef } from "react";

import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

import { buildGraph, type GraphModel, type LayoutMode } from "./graph";
import { buildScene, capDpr, type Scene } from "./sceneLayout";
import { paint, type Rect } from "./paint";
import { useInViewAndVisible } from "./useGating";

interface Engine {
  w: number;
  h: number;
  dpr: number;
  mode: LayoutMode | null;
  scene: Scene;
  graph: GraphModel | null;
  copyRect: Rect | null;
  raf: number;
  start: number;
}

export function GraphBackdrop() {
  const [rootRef, active] = useInViewAndVisible<HTMLDivElement>();
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const engineRef = useRef<Engine | null>(null);
  const reduced = useReducedMotionPreference();

  // Mount-once: canvas sizing, graph build and the static-frame draw path
  // all live here, keyed only to the canvas's own measured size, never a
  // separate "is this mobile" state.
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
      dpr: 1,
      mode: null,
      scene: buildScene(1, 1),
      graph: null,
      copyRect: null,
      raf: 0,
      start: 0,
    };
    engineRef.current = engine;

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

    const drawStatic = () => {
      if (!engine.graph || engine.w <= 0 || engine.h <= 0) {
        return;
      }
      paint({
        ctx,
        w: engine.w,
        h: engine.h,
        camera: engine.scene.camera,
        graph: engine.graph,
        time: 0,
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
      engine.dpr = dpr;
      engine.scene = buildScene(w, h);
      if (engine.scene.mode !== engine.mode) {
        engine.mode = engine.scene.mode;
        engine.graph = buildGraph(engine.mode);
      }
      measureCopyRect();
      drawStatic();
    };

    const ro = new ResizeObserver(resize);
    ro.observe(canvas);
    const copyRo = copyEl
      ? new ResizeObserver(() => {
          measureCopyRect();
          drawStatic();
        })
      : null;
    if (copyEl && copyRo) {
      copyRo.observe(copyEl);
    }
    resize();

    return () => {
      cancelAnimationFrame(engine.raf);
      ro.disconnect();
      copyRo?.disconnect();
      engineRef.current = null;
    };
  }, [rootRef]);

  // Runs (or freezes) the RAF loop as the in-view/visible/reduced-motion
  // gate changes, without rebuilding the canvas or the graph model.
  useEffect(() => {
    const engine = engineRef.current;
    const canvas = canvasRef.current;
    if (!engine || !canvas) {
      return;
    }
    const ctx = canvas.getContext("2d");
    if (!ctx) {
      return;
    }

    cancelAnimationFrame(engine.raf);

    if (reduced || !active) {
      if (engine.graph && engine.w > 0 && engine.h > 0) {
        paint({
          ctx,
          w: engine.w,
          h: engine.h,
          camera: engine.scene.camera,
          graph: engine.graph,
          time: 0,
          copyRect: engine.copyRect,
        });
      }
      return;
    }

    engine.start = 0;
    const loop = (t: number) => {
      if (!engine.start) {
        engine.start = t;
      }
      const time = (t - engine.start) / 1000;
      if (engine.graph && engine.w > 0 && engine.h > 0) {
        paint({
          ctx,
          w: engine.w,
          h: engine.h,
          camera: engine.scene.camera,
          graph: engine.graph,
          time,
          copyRect: engine.copyRect,
        });
      }
      engine.raf = requestAnimationFrame(loop);
    };
    engine.raf = requestAnimationFrame(loop);

    return () => {
      cancelAnimationFrame(engine.raf);
    };
  }, [active, reduced]);

  return (
    <div
      ref={rootRef}
      aria-hidden="true"
      className="absolute inset-0 overflow-hidden"
    >
      <canvas
        ref={canvasRef}
        style={{ display: "block", width: "100%", height: "100%" }}
      />
    </div>
  );
}
