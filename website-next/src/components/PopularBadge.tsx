// Cyan-to-blue slice of the card's border gradient (the tab sits where the
// border is in that range), used for the hexagon stroke.
const STROKE_FROM = "#16b9e4";
const STROKE_TO = "#5b94cf";

// Hexagon outline in a stretched viewBox: the tips touch the left/right edges at
// mid-height and the slants inset from the top/bottom corners. preserveAspect
// "none" stretches it to the badge box (sized by the text), and a non-scaling
// stroke keeps a uniform 1.5px through that stretch, matching the card border.
const HEX_POINTS = "0,20 11,0 89,0 100,20 89,40 11,40";

/**
 * The "Most Popular" hexagon tab that sits on the top border of a popular
 * `Offering` card. Drawn as a single SVG polygon stroked with the cyan-to-blue
 * slice of the border gradient so it reads as part of the border, with a solid
 * fill behind the label.
 */
export function PopularBadge() {
  return (
    <span className="absolute -top-[0.75px] left-1/2 -translate-x-1/2 -translate-y-1/2">
      <svg
        aria-hidden="true"
        viewBox="0 0 100 40"
        preserveAspectRatio="none"
        className="absolute inset-0 h-full w-full overflow-visible"
      >
        <defs>
          <linearGradient id="popular-badge-stroke" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0" stopColor={STROKE_FROM} />
            <stop offset="1" stopColor={STROKE_TO} />
          </linearGradient>
        </defs>
        <polygon
          points={HEX_POINTS}
          fill="var(--color-cc-surface)"
          stroke="url(#popular-badge-stroke)"
          strokeWidth="1.5"
          vectorEffect="non-scaling-stroke"
        />
      </svg>
      <span className="text-cc-heading relative block px-7 py-3 indent-[0.15em] font-mono text-[0.65rem] leading-none tracking-[0.15em] whitespace-nowrap uppercase">
        Most Popular
      </span>
    </span>
  );
}
