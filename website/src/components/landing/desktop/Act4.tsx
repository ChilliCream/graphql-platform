"use client";

import React, { useLayoutEffect, useRef } from "react";

import { ScaledCanvas } from "./ScaledCanvas";
import {
  ADAPTER_CX,
  ADAPTER_H,
  ADAPTER_W,
  ADAPTER_X,
  DESKTOP_ADAPTERS,
  adapterExitXs,
} from "./constants";
import { useAnchorContext } from "./AnchorContext";

export const Act4: React.FC = () => {
  const W = 1480;
  const H = 760;

  const PILL_Y = 540;

  const ENTRY_X = 272;
  const PRISM_APEX_X = ENTRY_X;
  const PRISM_APEX_Y = 170;
  const PRISM_BASE_Y = 198;
  const PRISM_HALF_W = 20;

  const sectionRef = useRef<HTMLElement>(null);
  const canvasRef = useRef<HTMLDivElement>(null);
  const { register, unregister } = useAnchorContext();

  // Publish anchors in page-relative pixel coordinates. The canvas applies
  // its own scale; we measure the rendered canvas to convert canvas-coords
  // into page coords.
  useLayoutEffect(() => {
    const measure = () => {
      const canvas = canvasRef.current?.querySelector(
        ".cc-canvas"
      ) as HTMLElement | null;
      const root = sectionRef.current?.closest(
        "[data-cc-landing-root]"
      ) as HTMLElement | null;
      if (!canvas || !root) return;

      const cRect = canvas.getBoundingClientRect();
      const rRect = root.getBoundingClientRect();
      // Translate canvas-coord (cx, cy) to page-coord.
      const toPage = (cx: number, cy: number) => {
        const scale = cRect.width / W;
        return {
          x: cRect.left - rRect.left + cx * scale,
          y: cRect.top - rRect.top + cy * scale,
        };
      };

      const apex = toPage(PRISM_APEX_X, PRISM_APEX_Y);
      register("act4.prism-apex", { ...apex, kind: "prism" });

      const baseL = toPage(PRISM_APEX_X - PRISM_HALF_W, PRISM_BASE_Y);
      register("act4.prism-base-left", { ...baseL, kind: "prism" });

      const baseC = toPage(PRISM_APEX_X, PRISM_BASE_Y);
      register("act4.prism-base-center", { ...baseC, kind: "prism" });

      const baseR = toPage(PRISM_APEX_X + PRISM_HALF_W, PRISM_BASE_Y);
      register("act4.prism-base-right", { ...baseR, kind: "prism" });

      // Top of Act 4 — used to continue the rainbow line from Act 3 down
      // to the prism apex.
      const top = toPage(ENTRY_X, 0);
      register("act4.entry", { ...top, kind: "act-top" });

      DESKTOP_ADAPTERS.forEach((a, i) => {
        const cx = ADAPTER_CX[i];
        const adapterTop = toPage(cx, PILL_Y);
        register(`act4.adapter-${a.key}`, { ...adapterTop, kind: "adapter" });
      });
    };

    measure();
    const ro = new ResizeObserver(measure);
    if (sectionRef.current) ro.observe(sectionRef.current);
    if (canvasRef.current) ro.observe(canvasRef.current);
    window.addEventListener("resize", measure);
    return () => {
      ro.disconnect();
      window.removeEventListener("resize", measure);
      unregister("act4.prism-apex");
      unregister("act4.prism-base-left");
      unregister("act4.prism-base-center");
      unregister("act4.prism-base-right");
      unregister("act4.entry");
      DESKTOP_ADAPTERS.forEach((a) => unregister(`act4.adapter-${a.key}`));
    };
  }, [register, unregister]);

  const exitPaths = (pillIdx: number) => {
    const cx = ADAPTER_CX[pillIdx];
    const yPillBottom = PILL_Y + ADAPTER_H;
    const xs = adapterExitXs(pillIdx);
    return xs.map((x) =>
      [
        `M ${cx} ${yPillBottom}`,
        `C ${cx} ${yPillBottom + 30} ${x} ${yPillBottom + 30} ${x} ${
          yPillBottom + 60
        }`,
        `L ${x} ${H}`,
      ].join(" ")
    );
  };

  return (
    <section
      ref={sectionRef}
      className="cc-act cc-act-adapters cc-act-spills"
      data-screen-label="04 Adapters"
    >
      <div className="cc-act-label">
        <span className="num">04</span> Adapters
      </div>

      <div ref={canvasRef}>
        <ScaledCanvas width={W} height={H}>
          <div
            className="cc-section-headline-fade"
            style={{
              position: "absolute",
              top: 40,
              left: 0,
              width: W,
              textAlign: "center",
              zIndex: 5,
              pointerEvents: "none",
            }}
          >
            <div className="eyebrow">Adapters</div>
            <h2
              className="display"
              style={{
                fontSize: "clamp(36px, 4.4vw, 64px)",
                margin: "8px auto",
                maxWidth: "20ch",
              }}
            >
              The API that speaks any language.
            </h2>
            <p className="cc-explainer">
              One composed graph, many wire formats. Expose the same data as
              GraphQL, REST over OpenAPI, MCP for AI agents, or gRPC for
              service-to-service traffic. The adapter layer translates on the
              fly, no duplicate schemas, no glue services.
            </p>
          </div>

          <svg
            width={W}
            height={H}
            viewBox={`0 0 ${W} ${H}`}
            style={{ position: "absolute", inset: 0, pointerEvents: "none" }}
            aria-hidden
          >
            {DESKTOP_ADAPTERS.map((a, pi) =>
              exitPaths(pi).map((d, ei) => (
                <path
                  key={"exit-" + a.key + "-" + ei}
                  d={d}
                  stroke="var(--cc-ink)"
                  strokeDasharray="3 6"
                  strokeWidth="var(--cc-line-w)"
                  fill="none"
                  strokeLinecap="round"
                  opacity="0.7"
                />
              ))
            )}
          </svg>

          {DESKTOP_ADAPTERS.map((a, i) => (
            <div
              key={a.key}
              className="cc-adapter-pill-d"
              data-key={a.key}
              style={{
                position: "absolute",
                left: ADAPTER_X[i],
                top: PILL_Y,
                width: ADAPTER_W,
                height: ADAPTER_H,
              }}
            >
              {a.label}
            </div>
          ))}
        </ScaledCanvas>
      </div>
    </section>
  );
};
