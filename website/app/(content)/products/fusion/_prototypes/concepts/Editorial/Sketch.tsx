import { FOLIO, HAND, PAPER } from "./palette";

/**
 * Drawing parts every Editorial scene shares: the paper ground, the marker
 * wobble filter and the sticky note a subgraph is written on. Not a visual of
 * its own - each scene imports these and composes its own drawing.
 */

interface WobbleProps {
  readonly id: string;
  /** Higher is rougher; 1.4 reads as a steady hand, 3 as a shaky one. */
  readonly scale?: number;
  /** Larger is finer-grained noise. */
  readonly frequency?: number;
  /** Distinguishes one wobble from another so two lines do not match. */
  readonly seed?: number;
}

/**
 * A displacement filter that bends a straight path the way a marker does.
 * Render it inside a `<defs>` and reference it as `filter={`url(#${id})`}`;
 * ids are global in a document, so each scene passes its own prefix.
 */
export function Wobble({
  id,
  scale = 1.6,
  frequency = 0.02,
  seed = 3,
}: WobbleProps) {
  return (
    <filter id={id} x="-6%" y="-6%" width="112%" height="112%">
      <feTurbulence
        type="fractalNoise"
        baseFrequency={frequency}
        numOctaves={2}
        seed={seed}
        result="noise"
      />
      <feDisplacementMap
        in="SourceGraphic"
        in2="noise"
        scale={scale}
        xChannelSelector="R"
        yChannelSelector="G"
      />
    </filter>
  );
}

/** Deterministic paper tooth: no randomness, so SSR and client agree. */
const SPECKS = Array.from({ length: 34 }, (_, i) => ({
  x: 12 + ((i * 271) % 616),
  y: 14 + ((i * 149) % 452),
  r: 0.6 + ((i * 7) % 5) * 0.18,
}));

interface PaperGroundProps {
  readonly width: number;
  readonly height: number;
  /** Draws the faint ruled lines of a notebook page under the drawing. */
  readonly ruled?: boolean;
}

/** The sheet a scene is drawn on: warm stock, a little tooth, a torn edge. */
export function PaperGround({ width, height, ruled }: PaperGroundProps) {
  const rules = ruled
    ? Array.from({ length: Math.floor(height / 34) }, (_, i) => 34 + i * 34)
    : [];

  return (
    <g>
      <rect width={width} height={height} fill={PAPER.sheet} />
      {rules.map((y) => (
        <line
          key={y}
          x1={0}
          y1={y}
          x2={width}
          y2={y}
          stroke={PAPER.ink}
          strokeWidth={1}
          opacity={0.05}
        />
      ))}
      {SPECKS.filter((s) => s.x < width && s.y < height).map((s) => (
        <circle
          key={`${s.x}-${s.y}`}
          cx={s.x}
          cy={s.y}
          r={s.r}
          fill={PAPER.ink}
          opacity={0.07}
        />
      ))}
      <rect
        x={0.5}
        y={0.5}
        width={width - 1}
        height={height - 1}
        fill="none"
        stroke={PAPER.sheetEdge}
        strokeWidth={3}
      />
    </g>
  );
}

interface StickyNoteProps {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
  /** Note stock colour. */
  readonly stock: string;
  /** Degrees of tilt; a hand never sticks one on straight. */
  readonly tilt?: number;
  /** The subgraph name, written large. */
  readonly name: string;
  /** Handwritten under the name, e.g. "Go · GraphQL Fed". */
  readonly caption?: string;
}

/**
 * One subgraph, written on a tilted sticky note with a shadow under it. The
 * note carries its own SVG `transform`, so a scene that animates it wraps it in
 * a group of its own rather than putting a CSS transform on this element.
 */
export function StickyNote({
  x,
  y,
  width,
  height,
  stock,
  tilt = 0,
  name,
  caption,
}: StickyNoteProps) {
  return (
    <g
      transform={`translate(${x} ${y}) rotate(${tilt} ${width / 2} ${height / 2})`}
    >
      <rect
        x={3}
        y={4}
        width={width}
        height={height}
        fill={PAPER.shade}
        rx={2}
      />
      <rect width={width} height={height} fill={stock} />
      <path
        d={`M0 0 H${width} V${height} H0 Z`}
        fill="none"
        stroke={PAPER.ink}
        strokeWidth={1.4}
        opacity={0.5}
      />
      <text
        x={12}
        y={caption ? height / 2 - 2 : height / 2 + 6}
        fill={PAPER.ink}
        fontSize={17}
        style={HAND}
      >
        {name}
      </text>
      {caption ? (
        <text
          x={12}
          y={height / 2 + 20}
          fill={PAPER.inkSoft}
          fontSize={11}
          style={HAND}
        >
          {caption}
        </text>
      ) : null}
    </g>
  );
}

interface FolioProps {
  readonly x: number;
  readonly y: number;
  readonly children: string;
  readonly anchor?: "start" | "middle" | "end";
}

/** Set-in-type caption inside a scene: the printed layer under the marker. */
export function Folio({ x, y, children, anchor = "start" }: FolioProps) {
  return (
    <text
      x={x}
      y={y}
      textAnchor={anchor}
      fill={PAPER.pencil}
      fontSize={10}
      style={FOLIO}
    >
      {children.toUpperCase()}
    </text>
  );
}
