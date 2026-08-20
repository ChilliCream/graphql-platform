"use client";

import { useEffect, useId, useRef, useState, type ReactNode } from "react";

interface BlueprintBackdropProps {
  readonly className?: string;
}

// Deterministic per-cell hash in [0, 1): the scatter is a pure function of
// cell coordinates, so growing the page adds figures without reshuffling the
// ones already on screen.
const hash = (x: number, y: number, k: number): number => {
  let h = Math.imul(x + 1, 374761393) + Math.imul(y + 1, 668265263) + Math.imul(k + 1, 1013904223);
  h = Math.imul(h ^ (h >>> 13), 1274126177);
  return ((h ^ (h >>> 16)) >>> 0) / 4294967296;
};

const CELL_WIDTH = 480;
const CELL_HEIGHT = 420;
/** Share of cells that receive a figure. */
const DENSITY = 0.55;

/**
 * Full-page generative drafting backdrop for /templates: a sheet grid plus
 * scattered technical-drawing figures (subgraph clusters, gateway boxes,
 * detail bubbles, dimension chains, section marks), regenerated to fit the
 * rendered page size. Colored via `currentColor`; the caller layers it behind
 * the content and picks the color.
 */
export function BlueprintBackdrop({ className }: BlueprintBackdropProps) {
  const uid = useId();
  const ref = useRef<HTMLDivElement | null>(null);
  const [size, setSize] = useState<{ readonly width: number; readonly height: number } | null>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) {
      return;
    }
    // Width comes from the document element (viewport minus scrollbar, so a
    // 100vw sheet cannot cause horizontal overflow); height from the wrapper,
    // which tracks the page as filtering grows or shrinks it.
    const measure = () => {
      const width = document.documentElement.clientWidth;
      const height = Math.round(el.getBoundingClientRect().height);
      setSize((prev) => (prev && prev.width === width && prev.height === height ? prev : { width, height }));
    };
    const observer = new ResizeObserver(measure);
    observer.observe(el);
    observer.observe(document.documentElement);
    return () => observer.disconnect();
  }, []);

  const minorId = `bp-minor-${uid}`;
  const majorId = `bp-major-${uid}`;
  const hatchId = `bp-hatch-${uid}`;

  return (
    <div
      ref={ref}
      aria-hidden="true"
      className={`pointer-events-none absolute inset-y-0 left-1/2 -translate-x-1/2 overflow-hidden ${className ?? ""}`}
      style={{ width: size ? `${size.width}px` : "100%" }}
    >
      <svg
        viewBox={size ? `0 0 ${size.width} ${size.height}` : undefined}
        fill="none"
        className={`h-full w-full transition-opacity duration-700 ${size ? "opacity-100" : "opacity-0"}`}
        style={{
          // Clears the sheet behind the hero copy so the headline stays crisp.
          maskImage: "radial-gradient(ellipse 560px 300px at 50% 240px, transparent 30%, rgb(0 0 0) 85%)",
        }}
      >
        <defs>
          <pattern id={minorId} width="24" height="24" patternUnits="userSpaceOnUse">
            <path d="M24 0H0V24" stroke="currentColor" strokeOpacity="0.1" />
          </pattern>
          <pattern id={majorId} width="120" height="120" patternUnits="userSpaceOnUse">
            <rect width="120" height="120" fill={`url(#${minorId})`} />
            <path d="M120 0H0V120" stroke="currentColor" strokeOpacity="0.22" />
          </pattern>
          <pattern id={hatchId} width="7" height="7" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">
            <path d="M0 0V7" stroke="currentColor" strokeOpacity="0.3" />
          </pattern>
        </defs>
        {size && (
          <>
            <rect width={size.width} height={size.height} fill={`url(#${majorId})`} />
            {sheetFigures(size.width, size.height, hatchId)}
          </>
        )}
      </svg>
    </div>
  );
}

function sheetFigures(width: number, height: number, hatchId: string): readonly ReactNode[] {
  const columns = Math.max(1, Math.round(width / CELL_WIDTH));
  const rows = Math.max(1, Math.round(height / CELL_HEIGHT));
  const cellWidth = width / columns;
  const cellHeight = height / rows;
  const figures: ReactNode[] = [];

  for (let row = 0; row < rows; row += 1) {
    for (let column = 0; column < columns; column += 1) {
      if (hash(column, row, 0) > DENSITY) {
        continue;
      }
      const x = column * cellWidth + 30 + hash(column, row, 1) * Math.max(0, cellWidth - 360);
      const y = row * cellHeight + 30 + hash(column, row, 2) * Math.max(0, cellHeight - 300);
      const scale = 0.8 + hash(column, row, 3) * 0.3;
      const index = row * columns + column;
      figures.push(
        <g key={`${column}-${row}`} transform={`translate(${x.toFixed(1)} ${y.toFixed(1)}) scale(${scale.toFixed(2)})`}>
          <Figure kind={Math.floor(hash(column, row, 4) * 5)} index={index} hatchId={hatchId} />
        </g>,
      );
    }
  }
  return figures;
}

function Figure({ kind, index, hatchId }: { readonly kind: number; readonly index: number; readonly hatchId: string }) {
  switch (kind) {
    case 0:
      return <SubgraphCluster index={index} />;
    case 1:
      return <GatewayBox index={index} hatchId={hatchId} />;
    case 2:
      return <DetailBubble index={index} />;
    case 3:
      return <DimensionChain index={index} />;
    default:
      return <SectionMark />;
  }
}

function Callout({ x, y, n }: { readonly x: number; readonly y: number; readonly n: number }) {
  return (
    <g>
      <circle cx={x} cy={y} r="8" stroke="currentColor" strokeOpacity="0.45" />
      <text
        x={x}
        y={y + 3.5}
        fill="currentColor"
        fillOpacity="0.55"
        fontSize="10"
        fontFamily="monospace"
        textAnchor="middle"
      >
        {n}
      </text>
    </g>
  );
}

// Two subgraph circles with centerlines, dashed links converging right.
function SubgraphCluster({ index }: { readonly index: number }) {
  return (
    <>
      {[40, 140].map((y, i) => (
        <g key={y}>
          <path d={`M8 ${y}h64M40 ${y - 32}v64`} stroke="currentColor" strokeOpacity="0.18" strokeDasharray="9 4 2 4" />
          <circle cx="40" cy={y} r="19" stroke="currentColor" strokeOpacity="0.5" />
          <circle cx="40" cy={y} r="4" fill="currentColor" fillOpacity="0.5" />
          <Callout x={0} y={y - 26} n={((index + i) % 4) + 1} />
        </g>
      ))}
      <g stroke="currentColor" strokeOpacity="0.35" strokeDasharray="7 5">
        <path d="M59 40 180 84M59 140 180 96" />
      </g>
      <circle cx="196" cy="90" r="9" stroke="currentColor" strokeOpacity="0.4" />
    </>
  );
}

// Gateway rectangle with a hatched header band and an egress arrow.
function GatewayBox({ index, hatchId }: { readonly index: number; readonly hatchId: string }) {
  return (
    <>
      <rect x="0" y="20" width="130" height="76" rx="9" stroke="currentColor" strokeOpacity="0.55" />
      <path d="M0 42v-13a9 9 0 0 1 9 -9h112a9 9 0 0 1 9 9v13z" fill={`url(#${hatchId})`} />
      <path d="M0 42h130" stroke="currentColor" strokeOpacity="0.45" />
      <Callout x={146} y={8} n={(index % 4) + 1} />
      <path d="M130 58h74" stroke="currentColor" strokeOpacity="0.4" />
      <path d="M204 58l-9 -3v6z" fill="currentColor" fillOpacity="0.4" />
      <circle cx="219" cy="58" r="9" stroke="currentColor" strokeOpacity="0.4" />
    </>
  );
}

// Zoomed detail circle with stacked blocks, dashed inner ring, and a label.
function DetailBubble({ index }: { readonly index: number }) {
  const letter = String.fromCharCode(65 + (index % 4));
  return (
    <>
      <circle cx="72" cy="72" r="72" stroke="currentColor" strokeOpacity="0.45" />
      <circle cx="72" cy="72" r="60" stroke="currentColor" strokeOpacity="0.2" strokeDasharray="4 6" />
      <g stroke="currentColor" strokeOpacity="0.45">
        <rect x="34" y="39" width="46" height="20" rx="4" />
        <rect x="57" y="67" width="46" height="20" rx="4" />
        <rect x="34" y="95" width="46" height="20" rx="4" />
        <path d="M57 49h18M80 77h-18M57 105h18" strokeDasharray="3 3" />
      </g>
      <text
        x="72"
        y="169"
        fill="currentColor"
        fillOpacity="0.55"
        fontSize="11"
        fontFamily="monospace"
        letterSpacing="1.5"
        textAnchor="middle"
      >
        {`DETAIL ${letter} · QUERY PLAN`}
      </text>
    </>
  );
}

// Horizontal dimension chain with end ticks, arrowheads, and a figure label.
function DimensionChain({ index }: { readonly index: number }) {
  return (
    <>
      <g stroke="currentColor" strokeOpacity="0.4">
        <path d="M0 20h240M0 15v10M240 15v10" />
      </g>
      <path d="M0 20l7 -2.5v5zM240 20l-7 -2.5v5z" fill="currentColor" fillOpacity="0.4" />
      <text
        x="120"
        y="12"
        fill="currentColor"
        fillOpacity="0.55"
        fontSize="11"
        fontFamily="monospace"
        letterSpacing="1.5"
        textAnchor="middle"
      >
        {`SUPERGRAPH ASSEMBLY — FIG ${(index % 9) + 1}`}
      </text>
    </>
  );
}

// Drafting section mark: roof triangle plus a north arrow.
function SectionMark() {
  return (
    <>
      <g stroke="currentColor" strokeOpacity="0.3">
        <path d="m0 66 58 -58 58 58Zm29 -29h58" />
        <path d="M147 66v-66m-6 9 6 -9 6 9" />
      </g>
      <text
        x="0"
        y="88"
        fill="currentColor"
        fillOpacity="0.45"
        fontSize="11"
        fontFamily="monospace"
        letterSpacing="1.5"
      >
        SECTION A–A
      </text>
    </>
  );
}
