"use client";

import React, { useEffect, useRef, useState } from "react";

import { useAnchorContext } from "./AnchorContext";
import { LANES } from "./anchorConfig";

// ConnectorLayer renders ONE absolutely-positioned SVG that spans the full
// page, drawing every cross-act path from anchors that the acts publish.
//
// Anchors are stored in page-relative pixel coordinates. The SVG covers the
// entire AnchorProvider wrapper; its size is tracked via ResizeObserver so
// paths recompute as content height changes (e.g. tab panel re-renders in
// Act 2/3).

interface ConnectorLayerProps {
  // Optional pointer to the element whose box defines the SVG size and the
  // coordinate origin used by every published anchor. If omitted (or its
  // current is null at measurement time), the SVG falls back to its own
  // parent element.
  rootRef?: React.RefObject<HTMLElement>;
}

export const ConnectorLayer: React.FC<ConnectorLayerProps> = ({ rootRef }) => {
  const { anchors } = useAnchorContext();
  const [size, setSize] = useState<{ w: number; h: number }>({ w: 0, h: 0 });
  const svgRef = useRef<SVGSVGElement>(null);

  useEffect(() => {
    const resolveRoot = (): HTMLElement | null => {
      if (rootRef?.current) return rootRef.current;
      return (svgRef.current?.parentElement as HTMLElement | null) ?? null;
    };

    const measure = () => {
      const node = resolveRoot();
      if (!node) return;
      const r = node.getBoundingClientRect();
      if (r.width > 0 && r.height > 0) {
        setSize({ w: r.width, h: r.height });
      }
    };
    measure();

    const node = resolveRoot();
    let ro: ResizeObserver | null = null;
    if (node && typeof ResizeObserver !== "undefined") {
      ro = new ResizeObserver(measure);
      ro.observe(node);
    }
    window.addEventListener("resize", measure);
    // Re-measure shortly after mount in case content (e.g. tab panel) is
    // still settling and getBoundingClientRect returned stale numbers.
    const t1 = window.setTimeout(measure, 100);
    const t2 = window.setTimeout(measure, 500);
    return () => {
      ro?.disconnect();
      window.removeEventListener("resize", measure);
      window.clearTimeout(t1);
      window.clearTimeout(t2);
    };
  }, [rootRef]);

  const get = (id: string) => anchors[id];

  // ===== Act 1: hero pour lines =====
  // Act 1 publishes the spout, elbow, elbow-tan, elbow-hold, exit-pre, exit
  // anchors per cup, all pre-scaled to page coordinates. The connector just
  // stitches them into the same Bézier shape the original SVG drew.
  const heroPourPaths: { d: string; key: string }[] = [];
  for (let i = 0; i < 4; i++) {
    const spout = get(`act1.cup-${i}`);
    const elbow = get(`act1.pour-elbow-${i}`);
    const elbowTan = get(`act1.pour-elbow-tan-${i}`);
    const elbowHold = get(`act1.pour-elbow-hold-${i}`);
    const exitPre = get(`act1.pour-exit-pre-${i}`);
    const exit = get(`act1.pour-exit-${i}`);
    if (!spout || !elbow || !elbowTan || !elbowHold || !exitPre || !exit)
      continue;

    const d = [
      `M ${spout.x} ${spout.y}`,
      `C ${elbow.x} ${spout.y} ${elbowTan.x} ${elbowTan.y} ${elbow.x} ${elbow.y}`,
      `C ${elbowHold.x} ${elbowHold.y} ${exitPre.x} ${exitPre.y} ${exit.x} ${exit.y}`,
    ].join(" ");

    heroPourPaths.push({ d, key: `pour-${i}` });
  }

  // ===== Act 1 -> Act 2: continue pour lines into Act 2 entries =====
  // Connect each hero pour exit to its matching Act 2 column entry.
  const heroToAct2Paths: { d: string; key: string }[] = [];
  for (let i = 0; i < 4; i++) {
    const exit = get(`act1.pour-exit-${i}`);
    const entry = get(`act2.entry-${i}`);
    if (!exit || !entry) continue;
    if (exit.x === entry.x) {
      heroToAct2Paths.push({
        d: `M ${exit.x} ${exit.y} L ${entry.x} ${entry.y}`,
        key: `h2a2-${i}`,
      });
    } else {
      const yMid = (exit.y + entry.y) / 2;
      heroToAct2Paths.push({
        d: `M ${exit.x} ${exit.y} C ${exit.x} ${yMid} ${entry.x} ${yMid} ${entry.x} ${entry.y}`,
        key: `h2a2-${i}`,
      });
    }
  }

  // ===== Act 2: column descents + merge curves + catalog exit + 4 service exits =====
  // Entry now lands AT the visible circle (entry.y === bend.y), so the path
  // starts there and bends straight to the column lane without any leading
  // vertical segment.
  const act2ColPaths: { d: string; opacity: number; key: string }[] = [];
  const act2MergePaths: { d: string; opacity: number; key: string }[] = [];
  for (let i = 0; i < 4; i++) {
    const entry = get(`act2.entry-${i}`);
    const bend = get(`act2.bend-${i}`);
    const colAnchor = get(`act2.col-anchor-${i}`);
    const merge = get(`act2.merge`);
    if (!entry || !bend || !colAnchor || !merge) continue;
    const opacity = entry.meta?.opacity ?? 1;

    const R = 8;
    const yBend = bend.y;
    const xCol = bend.x;

    const colD = [
      `M ${entry.x} ${entry.y}`,
      `L ${xCol + R} ${yBend}`,
      `Q ${xCol} ${yBend} ${xCol} ${yBend + R}`,
      `L ${xCol} ${colAnchor.y}`,
    ].join(" ");
    act2ColPaths.push({ d: colD, opacity, key: `a2-col-${i}` });

    const yMid = (colAnchor.y + merge.y) / 2;
    const mergeD = [
      `M ${xCol} ${colAnchor.y}`,
      `C ${xCol} ${yMid} ${merge.x} ${yMid} ${merge.x} ${merge.y}`,
    ].join(" ");
    act2MergePaths.push({ d: mergeD, opacity, key: `a2-merge-${i}` });
  }

  // Catalog line: one continuous path from the merge swatch in Act 2 all the
  // way to the catalog pill in Act 3 — no intermediate stop at the act-bottom
  // boundary so the tangent stays continuous when canvases rescale. The bend
  // position is a fraction of the total vertical run (not a fixed pixel
  // offset) so the curve scales with zoom and viewport.
  let catalogExitPath: string | null = null;
  {
    const merge = get(`act2.merge`);
    const pill = get(`act3.entry-catalog`);
    if (merge && pill) {
      const swatchHalf = 7; // SWATCH = 14
      const x0 = merge.x;
      const x1 = pill.x;
      const y0 = merge.y + swatchHalf;
      const y1 = pill.y;
      const yStraightEnd = y0 + (y1 - y0) * 0.45;
      const yCurveMid = (yStraightEnd + y1) / 2;
      catalogExitPath = [
        `M ${x0} ${y0}`,
        `L ${x0} ${yStraightEnd}`,
        `C ${x0} ${yCurveMid} ${x1} ${yCurveMid} ${x1} ${y1}`,
      ].join(" ");
    }
  }

  // 4 short colored service exit lines from the bottom row of Act 2.
  const act2ServiceExits: {
    d: string;
    color: string;
    key: string;
  }[] = [];
  {
    const swatchHalf = 7;
    (["billing", "ordering", "shipping", "users"] as const).forEach((key) => {
      const stripe = get(`act2.stripe-${key}`);
      const exit = get(`act2.exit-${key}`);
      if (!stripe || !exit) return;
      act2ServiceExits.push({
        d: `M ${stripe.x} ${stripe.y + swatchHalf} L ${exit.x} ${exit.y}`,
        color: LANES[key].color,
        key: `a2-svc-${key}`,
      });
    });
  }

  // ===== Act 2 -> Act 3: service-colored connectors =====
  // Catalog is drawn as one continuous path above (catalogExitPath), so skip
  // it here to avoid a duplicate segment with a tangent break at the boundary.
  const a2ToA3: { d: string; color: string; key: string }[] = [];
  (Object.keys(LANES) as Array<keyof typeof LANES>).forEach((key) => {
    if (key === "catalog") return;
    const exit = get(`act2.exit-${key}`);
    const entry = get(`act3.entry-${key}`);
    if (!exit || !entry) return;
    if (exit.x === entry.x) {
      a2ToA3.push({
        d: `M ${exit.x} ${exit.y} L ${entry.x} ${entry.y}`,
        color: LANES[key].color,
        key: `a2a3-${key}`,
      });
    } else {
      const yMid = (exit.y + entry.y) / 2;
      a2ToA3.push({
        d: `M ${exit.x} ${exit.y} C ${exit.x} ${yMid} ${entry.x} ${yMid} ${entry.x} ${entry.y}`,
        color: LANES[key].color,
        key: `a2a3-${key}`,
      });
    }
  });

  // ===== Act 3 pre-pinch funnel =====
  // Entry now lands AT the visible pill (entry.y === bend.y), so the path
  // bends straight to the column lane without any leading vertical segment,
  // then descends to TWIST_START and funnel-curves into the pinch point.
  const act3PrePinch: { d: string; color: string; key: string }[] = [];
  (Object.keys(LANES) as Array<keyof typeof LANES>).forEach((key, i) => {
    const entry = get(`act3.entry-${key}`);
    const bend = get(`act3.bend-${key}`);
    const twistStart = get(`act3.twist-start-${key}`);
    const pinch = get(`act3.pinch`);
    if (!entry || !bend || !twistStart || !pinch) return;
    const R = 8;
    const yBend = bend.y;
    const xCol = bend.x;
    const yFunnel = (twistStart.y + pinch.y) / 2;
    const d = [
      `M ${entry.x} ${entry.y}`,
      `L ${xCol + R} ${yBend}`,
      `Q ${xCol} ${yBend} ${xCol} ${yBend + R}`,
      `L ${xCol} ${twistStart.y}`,
      `C ${xCol} ${yFunnel} ${pinch.x} ${yFunnel} ${pinch.x} ${pinch.y}`,
    ].join(" ");
    act3PrePinch.push({ d, color: LANES[key].color, key: `a3-pre-${key}` });
    void i;
  });

  // ===== Act 3 post-pinch + Act 4 entry rainbow line (one continuous path) =====
  // Curve directly from pinch into the split point. The straight-descent
  // length is a fraction of the total vertical run (not a fixed pixel
  // offset) so it scales with zoom and viewport.
  let postPinchToPrismPath: string | null = null;
  let postPinchGradientStops: { y0: number; y1: number } | null = null;
  {
    const pinch = get(`act3.pinch`);
    const apex = get(`act4.prism-apex`);
    if (pinch && apex) {
      const x0 = pinch.x;
      const y0 = pinch.y;
      const x1 = apex.x;
      const y1 = apex.y;
      const yStraightEnd = y0 + (y1 - y0) * 0.45;
      const yCurveMid = (yStraightEnd + y1) / 2;
      postPinchToPrismPath = [
        `M ${x0} ${y0}`,
        `L ${x0} ${yStraightEnd}`,
        `C ${x0} ${yCurveMid} ${x1} ${yCurveMid} ${x1} ${y1}`,
      ].join(" ");
      postPinchGradientStops = { y0, y1: apex.y };
    }
  }

  // ===== Act 4 split beams =====
  // All 4 beams emanate from a single split point (the post-pinch path's
  // endpoint = apex), so the rainbow line cleanly diverges into 4 strands.
  const beamPaths: { d: string; key: string }[] = [];
  let beamGradient: { y0: number; y1: number } | null = null;
  {
    const apex = get(`act4.prism-apex`);
    const adapters = (["graphql", "openapi", "mcp", "grpc"] as const).map((k) =>
      get(`act4.adapter-${k}`)
    );
    if (apex && adapters.every((a) => !!a)) {
      const startX = apex.x;
      const startY = apex.y;
      const pillY = adapters[0]!.y;
      beamGradient = { y0: startY, y1: pillY };
      adapters.forEach((a, i) => {
        if (!a) return;
        const endX = a.x;
        const c1y = startY + 80;
        const c2y = pillY - 80;
        const d = `M ${startX} ${startY} C ${startX} ${c1y} ${endX} ${c2y} ${endX} ${pillY}`;
        beamPaths.push({ d, key: `beam-${i}` });
      });
    }
  }

  return (
    <svg
      ref={svgRef}
      className="cc-connector-layer"
      width={size.w}
      height={size.h}
      viewBox={`0 0 ${size.w} ${size.h}`}
      style={{
        position: "absolute",
        top: 0,
        left: 0,
        width: size.w,
        height: size.h,
        pointerEvents: "none",
        zIndex: 0,
        overflow: "visible",
      }}
      aria-hidden
    >
      <defs>
        {/* Pre-split rainbow uses 3 colors; post-split beams use the
            remaining 2. Each of the 5 service colors therefore appears
            exactly once after Fusion Composition. */}
        {postPinchGradientStops && (
          <linearGradient
            id="cc-cl-rainbow-post"
            x1="0"
            y1={postPinchGradientStops.y0}
            x2="0"
            y2={postPinchGradientStops.y1}
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0" stopColor="var(--cc-col-cat)" />
            <stop offset="0.5" stopColor="var(--cc-col-bil)" />
            <stop offset="1" stopColor="var(--cc-col-ord)" />
          </linearGradient>
        )}
        {beamGradient && (
          <linearGradient
            id="cc-cl-rainbow-beam"
            x1="0"
            y1={beamGradient.y0}
            x2="0"
            y2={beamGradient.y1}
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0" stopColor="var(--cc-col-shi)" />
            <stop offset="1" stopColor="var(--cc-col-usr)" />
          </linearGradient>
        )}
      </defs>

      {/* Act 1 hero pour lines */}
      {heroPourPaths.map(({ d, key }) => (
        <path
          key={key}
          d={d}
          stroke="var(--cc-ink)"
          strokeWidth="var(--cc-line-w)"
          vectorEffect="non-scaling-stroke"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          opacity="0.95"
        />
      ))}

      {/* Hero -> Act 2 continuation */}
      {heroToAct2Paths.map(({ d, key }) => (
        <path
          key={key}
          d={d}
          stroke="var(--cc-ink)"
          strokeWidth="var(--cc-line-w)"
          vectorEffect="non-scaling-stroke"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          opacity="0.95"
        />
      ))}

      {/* Act 2: column descents */}
      {act2ColPaths.map(({ d, opacity, key }) => (
        <path
          key={key}
          d={d}
          stroke="var(--cc-ink)"
          strokeWidth="var(--cc-line-w)"
          vectorEffect="non-scaling-stroke"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          opacity={opacity}
        />
      ))}

      {/* Act 2: merge curves */}
      {act2MergePaths.map(({ d, opacity, key }) => (
        <path
          key={key}
          d={d}
          stroke="var(--cc-ink)"
          strokeWidth="var(--cc-line-w)"
          vectorEffect="non-scaling-stroke"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          opacity={opacity}
        />
      ))}

      {/* Catalog exit — red line through Act 2 */}
      {catalogExitPath && (
        <path
          d={catalogExitPath}
          stroke={LANES.catalog.color}
          strokeWidth="var(--cc-line-w)"
          vectorEffect="non-scaling-stroke"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      )}

      {/* Act 2: 4 service exit short lines */}
      {act2ServiceExits.map(({ d, color, key }) => (
        <path
          key={key}
          d={d}
          stroke={color}
          strokeWidth="var(--cc-line-w)"
          vectorEffect="non-scaling-stroke"
          fill="none"
          strokeLinecap="round"
        />
      ))}

      {/* Act 2 -> Act 3: 5 service-colored connectors */}
      {a2ToA3.map(({ d, color, key }) => (
        <path
          key={key}
          d={d}
          stroke={color}
          strokeWidth="var(--cc-line-w)"
          vectorEffect="non-scaling-stroke"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          opacity="0.95"
        />
      ))}

      {/* Act 3: pre-pinch funnel */}
      {act3PrePinch.map(({ d, color, key }) => (
        <path
          key={key}
          d={d}
          stroke={color}
          strokeWidth="var(--cc-line-w)"
          vectorEffect="non-scaling-stroke"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          opacity="0.95"
        />
      ))}

      {/* Act 3 post-pinch + Act 4 entry rainbow */}
      {postPinchToPrismPath && (
        <path
          d={postPinchToPrismPath}
          stroke="url(#cc-cl-rainbow-post)"
          strokeWidth="var(--cc-line-w)"
          vectorEffect="non-scaling-stroke"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          opacity="0.95"
        />
      )}

      {/* Act 4 prism beams */}
      {beamPaths.map(({ d, key }) => (
        <path
          key={key}
          d={d}
          stroke="url(#cc-cl-rainbow-beam)"
          strokeWidth="var(--cc-line-w)"
          vectorEffect="non-scaling-stroke"
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
          opacity="0.95"
        />
      ))}
    </svg>
  );
};
