interface ActionLinesProps {
  /** Rendered width/height in px (the burst is square). */
  readonly size: number;
  readonly color: string;
  /** Number of rays. */
  readonly count?: number;
  /** Inner radius (0-100 viewBox units) where the rays start. */
  readonly innerRadius?: number;
}

/**
 * Manga-style action lines radiating from the center, drawn as an inline SVG so
 * Satori (next/og) can render it. The inner radius leaves a gap so the rays
 * burst out from behind a centered subject.
 */
export function ActionLines({
  size,
  color,
  count = 22,
  innerRadius = 64,
}: ActionLinesProps) {
  const center = 100;
  const outer = 99;

  return (
    <svg width={size} height={size} viewBox="0 0 200 200" fill="none">
      {Array.from({ length: count }, (_, i) => {
        const angle = (i / count) * Math.PI * 2;
        const inner = innerRadius + (i % 2 === 0 ? 0 : 7);
        return (
          <line
            key={i}
            x1={center + Math.cos(angle) * inner}
            y1={center + Math.sin(angle) * inner}
            x2={center + Math.cos(angle) * outer}
            y2={center + Math.sin(angle) * outer}
            stroke={color}
            strokeWidth={2.4}
            strokeLinecap="round"
          />
        );
      })}
    </svg>
  );
}
