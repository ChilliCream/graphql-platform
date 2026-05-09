"use client";

import React, { useLayoutEffect, useRef, useState } from "react";

// Two-anchor connector primitive distilled from the homepage's
// `landing/desktop/ConnectorLayer.tsx`. Reads two `<Anchor>` endpoints by
// `data-cc-anchor` id, computes their bounding boxes, translates them into a
// containing element's coordinate space, and draws a single SVG path between
// them. Curves recompute on mount, on viewport resize, and on document-level
// layout changes via a `ResizeObserver`.
//
// Unlike the homepage's full anchor-config system, this primitive is band-
// scoped: it does NOT use a context, does NOT publish anchors page-wide, and
// only handles a single from/to pair. Use it for diagrams or small narrative
// threads inside one `<Band>`.

export type ConnectorCurve = "bezier" | "straight" | "right-angle";
export type ConnectorWeight = "hairline" | "thin";
export type ConnectorTone =
  | "ink-faint"
  | "accent-shi"
  | "accent-usr"
  | "accent-cat"
  | (string & {});

export interface ConnectorLineProps {
  /** Anchor id of the start point. */
  from: string;
  /** Anchor id of the end point. */
  to: string;
  /** Curve shape. Defaults to `bezier`. */
  curve?: ConnectorCurve;
  /** Stroke width. `hairline` = 1px, `thin` = 1.5px. Defaults to `hairline`. */
  weight?: ConnectorWeight;
  /** Stroke color. Tone tokens map to CSS variables; raw colors pass through. */
  tone?: ConnectorTone;
  /**
   * Optional CSS selector for the ancestor whose box defines the connector's
   * coordinate space. Defaults to the closest `[data-cc-connector-layer]`,
   * `form`, `article`, or `section` ancestor of the `from` anchor.
   */
  containerSelector?: string;
  className?: string;
}

interface Endpoint {
  x: number;
  y: number;
}

interface Geometry {
  fromPoint: Endpoint;
  toPoint: Endpoint;
  containerLeft: number;
  containerTop: number;
  width: number;
  height: number;
}

const TONE_MAP: Record<string, string> = {
  "ink-faint": "var(--cc-ink-faint)",
  "accent-shi": "var(--cc-col-shi)",
  "accent-usr": "var(--cc-col-usr)",
  "accent-cat": "var(--cc-col-cat)",
};

const PATH_MARGIN = 12;

const resolveTone = (tone: ConnectorTone): string => {
  return TONE_MAP[tone as string] ?? (tone as string);
};

const findAnchor = (id: string): HTMLElement | null => {
  if (typeof document === "undefined") {
    return null;
  }
  return document.querySelector<HTMLElement>(`[data-cc-anchor="${id}"]`);
};

const findContainer = (
  fromEl: HTMLElement,
  selector?: string
): HTMLElement | null => {
  if (selector) {
    const explicit = document.querySelector<HTMLElement>(selector);
    if (explicit) {
      return explicit;
    }
  }
  const closest = fromEl.closest<HTMLElement>(
    "[data-cc-connector-layer], form, article, section"
  );
  if (closest) {
    return closest;
  }
  // Fall back to the nearest positioned ancestor.
  let node: HTMLElement | null = fromEl.parentElement;
  while (node) {
    const position = window.getComputedStyle(node).position;
    if (position !== "static") {
      return node;
    }
    node = node.parentElement;
  }
  return null;
};

const centerOf = (rect: DOMRect): Endpoint => ({
  x: rect.left + rect.width / 2,
  y: rect.top + rect.height / 2,
});

const buildPath = (
  curve: ConnectorCurve,
  from: Endpoint,
  to: Endpoint
): string => {
  if (curve === "straight") {
    return `M ${from.x} ${from.y} L ${to.x} ${to.y}`;
  }
  if (curve === "right-angle") {
    return `M ${from.x} ${from.y} L ${from.x} ${to.y} L ${to.x} ${to.y}`;
  }
  // Bezier: control points offset perpendicular to the line direction at ~45%
  // of the distance, producing a flowing S-curve rather than a U-turn.
  const dx = to.x - from.x;
  const dy = to.y - from.y;
  const length = Math.sqrt(dx * dx + dy * dy) || 1;
  const offset = length * 0.45;
  const nx = -dy / length;
  const ny = dx / length;
  const c1x = from.x + dx * 0.33 + nx * offset * 0.35;
  const c1y = from.y + dy * 0.33 + ny * offset * 0.35;
  const c2x = from.x + dx * 0.67 - nx * offset * 0.35;
  const c2y = from.y + dy * 0.67 - ny * offset * 0.35;
  return `M ${from.x} ${from.y} C ${c1x} ${c1y} ${c2x} ${c2y} ${to.x} ${to.y}`;
};

/**
 * Draws a single SVG path between two `<Anchor>` endpoints. The path
 * recomputes on layout changes; the connector is purely decorative and is
 * hidden from assistive tech.
 */
export const ConnectorLine: React.FC<ConnectorLineProps> = ({
  from,
  to,
  curve = "bezier",
  weight = "hairline",
  tone = "ink-faint",
  containerSelector,
  className,
}) => {
  const [geom, setGeom] = useState<Geometry | null>(null);
  const frameRef = useRef<number | null>(null);

  useLayoutEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    const measure = () => {
      const fromEl = findAnchor(from);
      const toEl = findAnchor(to);
      if (!fromEl || !toEl) {
        setGeom(null);
        return;
      }
      const container = findContainer(fromEl, containerSelector);
      if (!container) {
        setGeom(null);
        return;
      }
      const fromRect = fromEl.getBoundingClientRect();
      const toRect = toEl.getBoundingClientRect();
      const containerRect = container.getBoundingClientRect();
      const fromCenter = centerOf(fromRect);
      const toCenter = centerOf(toRect);
      setGeom({
        fromPoint: {
          x: fromCenter.x - containerRect.left,
          y: fromCenter.y - containerRect.top,
        },
        toPoint: {
          x: toCenter.x - containerRect.left,
          y: toCenter.y - containerRect.top,
        },
        containerLeft: 0,
        containerTop: 0,
        width: containerRect.width,
        height: containerRect.height,
      });
    };

    const schedule = () => {
      if (frameRef.current !== null) {
        cancelAnimationFrame(frameRef.current);
      }
      frameRef.current = requestAnimationFrame(() => {
        frameRef.current = null;
        measure();
      });
    };

    schedule();

    let resizeObserver: ResizeObserver | null = null;
    if (typeof ResizeObserver !== "undefined") {
      resizeObserver = new ResizeObserver(schedule);
      resizeObserver.observe(document.documentElement);
    }
    window.addEventListener("resize", schedule);
    window.addEventListener("scroll", schedule, true);

    return () => {
      if (frameRef.current !== null) {
        cancelAnimationFrame(frameRef.current);
        frameRef.current = null;
      }
      resizeObserver?.disconnect();
      window.removeEventListener("resize", schedule);
      window.removeEventListener("scroll", schedule, true);
    };
  }, [from, to, containerSelector]);

  if (!geom) {
    return null;
  }

  const minX = Math.min(geom.fromPoint.x, geom.toPoint.x) - PATH_MARGIN;
  const minY = Math.min(geom.fromPoint.y, geom.toPoint.y) - PATH_MARGIN;
  const maxX = Math.max(geom.fromPoint.x, geom.toPoint.x) + PATH_MARGIN;
  const maxY = Math.max(geom.fromPoint.y, geom.toPoint.y) + PATH_MARGIN;
  const width = Math.max(maxX - minX, 1);
  const height = Math.max(maxY - minY, 1);

  const fromLocal: Endpoint = {
    x: geom.fromPoint.x - minX,
    y: geom.fromPoint.y - minY,
  };
  const toLocal: Endpoint = {
    x: geom.toPoint.x - minX,
    y: geom.toPoint.y - minY,
  };

  const d = buildPath(curve, fromLocal, toLocal);
  const strokeWidth = weight === "thin" ? 1.5 : 1;
  const strokeColor = resolveTone(tone);

  return (
    <svg
      className={className}
      width={width}
      height={height}
      viewBox={`0 0 ${width} ${height}`}
      style={{
        position: "absolute",
        left: minX,
        top: minY,
        width,
        height,
        pointerEvents: "none",
        overflow: "visible",
      }}
      aria-hidden="true"
    >
      <path
        d={d}
        stroke={strokeColor}
        strokeWidth={strokeWidth}
        fill="none"
        strokeLinecap="round"
        strokeLinejoin="round"
        vectorEffect="non-scaling-stroke"
      />
    </svg>
  );
};
