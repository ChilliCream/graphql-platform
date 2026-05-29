"use client";

import React, { useEffect, useRef, useState } from "react";

import { DESKTOP_ADAPTERS, adapterExitXs } from "./constants";
import {
  useAnchorContext,
  useLandingRoot,
  useMeasureEffect,
} from "./AnchorContext";

// Reference geometry — the Adapters act was tuned in a 1480×760 canvas. The
// `.cc-act4-stage` div carries that aspect ratio so all child positions can
// be expressed as fractions of the stage. No transform: scale anywhere.
const REF_W = 1480;
const REF_H = 760;
const PILL_W_REF = 200;
const PILL_H_REF = 64;
const PILL_GAP_REF = 60;
const PILLS_TOTAL_REF =
  DESKTOP_ADAPTERS.length * PILL_W_REF +
  (DESKTOP_ADAPTERS.length - 1) * PILL_GAP_REF;
const PILLS_X0_REF = (REF_W - PILLS_TOTAL_REF) / 2;
const PILL_X_REF = DESKTOP_ADAPTERS.map(
  (_, i) => PILLS_X0_REF + i * (PILL_W_REF + PILL_GAP_REF)
);
const PILL_CX_REF = PILL_X_REF.map((x) => x + PILL_W_REF / 2);
const PILL_Y_REF = 540;
const PRISM_APEX_X_REF = 272;
const PRISM_APEX_Y_REF = 170;
const PRISM_BASE_Y_REF = 198;
const PRISM_HALF_W_REF = 20;

export const Act4: React.FC = () => {
  const sectionRef = useRef<HTMLElement>(null);
  const stageRef = useRef<HTMLDivElement>(null);
  const pillRefs = useRef<Record<string, HTMLDivElement | null>>({});
  const { register, unregister } = useAnchorContext();
  const root = useLandingRoot();

  // Per-pill measured center in stage-local pixels — drives the dashed exit
  // fan paths so they align with the rendered pill regardless of viewport.
  const [pillCenters, setPillCenters] = useState<
    Array<{ cx: number; bottom: number } | null>
  >(() => DESKTOP_ADAPTERS.map(() => null));
  // Measured stage dimensions in CSS pixels — drives the local SVG viewBox.
  const [stageDims, setStageDims] = useState<{ w: number; h: number }>({
    w: REF_W,
    h: REF_H,
  });

  useMeasureEffect(
    () => {
      const stage = stageRef.current;
      if (!stage || !root) {
        return;
      }
      const sRect = stage.getBoundingClientRect();
      const rRect = root.getBoundingClientRect();
      const stageW = sRect.width;
      const stageH = sRect.height;
      setStageDims((prev) =>
        prev.w === stageW && prev.h === stageH ? prev : { w: stageW, h: stageH }
      );
      const toRoot = (refX: number, refY: number) => ({
        x: sRect.left - rRect.left + (refX / REF_W) * stageW,
        y: sRect.top - rRect.top + (refY / REF_H) * stageH,
      });

      // Prism apex + base — derived from stage rect.
      register("act4.prism-apex", {
        ...toRoot(PRISM_APEX_X_REF, PRISM_APEX_Y_REF),
        kind: "prism",
      });
      register("act4.prism-base-left", {
        ...toRoot(PRISM_APEX_X_REF - PRISM_HALF_W_REF, PRISM_BASE_Y_REF),
        kind: "prism",
      });
      register("act4.prism-base-center", {
        ...toRoot(PRISM_APEX_X_REF, PRISM_BASE_Y_REF),
        kind: "prism",
      });
      register("act4.prism-base-right", {
        ...toRoot(PRISM_APEX_X_REF + PRISM_HALF_W_REF, PRISM_BASE_Y_REF),
        kind: "prism",
      });
      register("act4.entry", {
        ...toRoot(PRISM_APEX_X_REF, 0),
        kind: "act-top",
      });

      // Adapter pills — prefer measured DOM top-center. Falls back to the
      // reference geometry on first paint before pill refs settle.
      const newPillCenters: Array<{ cx: number; bottom: number }> = [];
      DESKTOP_ADAPTERS.forEach((a, i) => {
        const pillEl = pillRefs.current[a.key];
        let topCenter: { x: number; y: number };
        let stageCxBottom: { cx: number; bottom: number };
        if (pillEl) {
          const pRect = pillEl.getBoundingClientRect();
          topCenter = {
            x: pRect.left - rRect.left + pRect.width / 2,
            y: pRect.top - rRect.top,
          };
          stageCxBottom = {
            cx: pRect.left - sRect.left + pRect.width / 2,
            bottom: pRect.top - sRect.top + pRect.height,
          };
        } else {
          topCenter = toRoot(PILL_CX_REF[i], PILL_Y_REF);
          stageCxBottom = {
            cx: (PILL_CX_REF[i] / REF_W) * stageW,
            bottom: ((PILL_Y_REF + PILL_H_REF) / REF_H) * stageH,
          };
        }
        register(`act4.adapter-${a.key}`, { ...topCenter, kind: "adapter" });
        newPillCenters.push(stageCxBottom);
      });

      // Only push state when pill centers actually shifted to avoid render
      // loops when the measure callback fires multiple times in a frame.
      setPillCenters((prev) => {
        if (prev.length !== newPillCenters.length) {
          return newPillCenters;
        }
        for (let i = 0; i < prev.length; i++) {
          const a = prev[i];
          const b = newPillCenters[i];
          if (!a || a.cx !== b.cx || a.bottom !== b.bottom) {
            return newPillCenters;
          }
        }
        return prev;
      });
    },
    [stageRef, sectionRef],
    [register, root]
  );

  useEffect(
    () => () => {
      unregister("act4.prism-apex");
      unregister("act4.prism-base-left");
      unregister("act4.prism-base-center");
      unregister("act4.prism-base-right");
      unregister("act4.entry");
      DESKTOP_ADAPTERS.forEach((a) => unregister(`act4.adapter-${a.key}`));
    },
    [unregister]
  );

  const xPct = (refX: number) => `${(refX / REF_W) * 100}%`;
  const yPct = (refY: number) => `${(refY / REF_H) * 100}%`;
  const wPct = (sizeRef: number) => `${(sizeRef / REF_W) * 100}%`;
  const hPct = (sizeRef: number) => `${(sizeRef / REF_H) * 100}%`;

  // The dashed fan-out paths from each pill bottom into the section bottom.
  // Drawn into a stage-sized SVG using pixel coordinates pulled from the
  // measured pill rects (preferred) or fallback to reference geometry until
  // measurement completes on first paint.
  const { w: stageW, h: stageH } = stageDims;

  const fanPaths: { d: string; key: string }[] = [];
  DESKTOP_ADAPTERS.forEach((a, pi) => {
    const center = pillCenters[pi];
    const cx = center?.cx ?? (PILL_CX_REF[pi] / REF_W) * stageW;
    const yPillBottom =
      center?.bottom ?? ((PILL_Y_REF + PILL_H_REF) / REF_H) * stageH;
    const xs = adapterExitXs(pi, cx);
    const bottomY = stageH;
    xs.forEach((x, ei) => {
      const d = [
        `M ${cx} ${yPillBottom}`,
        `C ${cx} ${yPillBottom + 30} ${x} ${yPillBottom + 30} ${x} ${
          yPillBottom + 60
        }`,
        `L ${x} ${bottomY}`,
      ].join(" ");
      fanPaths.push({ d, key: `exit-${a.key}-${ei}` });
    });
  });
  return (
    <section
      ref={sectionRef}
      className="cc-act cc-act-adapters cc-act-spills"
      data-screen-label="04 Adapters"
    >
      <div className="cc-act-label">
        <span className="num">04</span> Adapters
      </div>

      <div className="cc-act4-stage" ref={stageRef}>
        <div
          className="cc-section-headline-fade"
          style={{
            position: "absolute",
            top: yPct(40),
            left: 0,
            width: "100%",
            textAlign: "center",
            zIndex: 5,
            pointerEvents: "none",
          }}
        >
          <div className="eyebrow">Adapters</div>
          <h2
            className="display cc-act4-headline"
            style={{ margin: "8px auto", maxWidth: "20ch" }}
          >
            The API that speaks any language.
          </h2>
          <p className="cc-explainer">
            One composed graph, many wire formats. Expose the same data as
            GraphQL, REST over OpenAPI, MCP for AI agents, or gRPC for
            service-to-service traffic. The adapter layer translates on the fly,
            no duplicate schemas, no glue services.
          </p>
        </div>

        {/* Adapter pills */}
        {DESKTOP_ADAPTERS.map((a, i) => (
          <div
            key={a.key}
            ref={(el) => {
              pillRefs.current[a.key] = el;
            }}
            className="cc-adapter-pill-d cc-act4-pill"
            data-key={a.key}
            style={{
              position: "absolute",
              left: xPct(PILL_X_REF[i]),
              top: yPct(PILL_Y_REF),
              width: wPct(PILL_W_REF),
              height: hPct(PILL_H_REF),
            }}
          >
            {a.label}
          </div>
        ))}

        {/* Dashed fan-out lines from each pill into the section bottom. The
            SVG is sized to the stage in stage-local pixels; the viewBox is
            also in pixels so dashed-array spacing is honoured 1:1. */}
        <svg
          width={stageW}
          height={stageH}
          viewBox={`0 0 ${stageW} ${stageH}`}
          style={{
            position: "absolute",
            inset: 0,
            width: "100%",
            height: "100%",
            pointerEvents: "none",
            overflow: "visible",
          }}
          aria-hidden
        >
          {fanPaths.map(({ d, key }) => (
            <path
              key={key}
              d={d}
              stroke="var(--cc-ink)"
              strokeDasharray="3 6"
              strokeWidth="var(--cc-line-w)"
              fill="none"
              strokeLinecap="round"
              opacity="0.7"
            />
          ))}
        </svg>
      </div>
    </section>
  );
};
