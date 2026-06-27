/**
 * "Build loop" scene, concept #5 ("Glue tangle collapses to one token"), v3
 * "Signal & Metrics" (strict cc-* dark) take.
 *
 * Lead with the measured result: a tangle of separately hand-maintained,
 * kept-in-sync glue files collapses into ONE [QueryType] ProductApi source
 * token that everything else is generated from. The hero is the two-up cream
 * numeral "12 -> 1" (glue files in, token out) over a lowercase mono caption.
 *
 * Layout B (stat-top / signal-row) with the segment-marks idiom, distinct from
 * concept #2's tick-run collapse. Under the hero, one full-width collapse
 * signal: twelve grey glue-file segment cells sit in two coupled rows (the faint
 * dashed inter-row links are the "kept in sync" web), then funnel through grey
 * 1px connectors into a single junction. The cell's lone teal accent is that
 * junction node plus the one token pill it feeds, a cc-surface chip with a 1px
 * teal border reading "[QueryType] ProductApi". The single filled teal node
 * lands on the surviving "1", the generated source of truth. No status hues,
 * because nothing here is failing.
 *
 * Static settled frame, no motion, no hooks, server component. Every svg id is
 * prefixed "v3-build-5-".
 */
const cc = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  inkDim: "rgba(245,241,234,0.62)",
  ink: "#a1a3af",
  navLabel: "#62748e",
  teal: "#5eead4",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v3-build-5-";

/** Left edge of each of the six columns in the segment cluster (viewBox 288). */
const COLS: readonly number[] = [75.25, 98.75, 122.25, 145.75, 169.25, 192.75];

interface BuildVariant5Props {
  readonly className?: string;
}

export function BuildVariant5({ className }: BuildVariant5Props) {
  // collapse geometry (viewBox 288 x 84): two coupled rows of six glue cells
  // funnel down into one teal junction node, then into the source token pill.
  const cellW = 20;
  const cellH = 9;
  const row1Y = 4;
  const row2Y = 18;
  const nodeX = 144;
  const nodeY = 47;
  const center = (left: number) => left + cellW / 2;

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          glue collapse
        </p>

        {/* HERO: two-up numeral "12 -> 1" + lowercase mono caption */}
        <div className="mt-3 flex items-baseline gap-2.5">
          <span
            className="font-heading text-cc-heading leading-none font-semibold"
            style={{ fontSize: "2rem" }}
          >
            12
          </span>
          <span
            className="text-cc-ink-faint"
            style={{ fontFamily: MONO, fontSize: "1rem" }}
          >
            &rarr;
          </span>
          <span
            className="font-heading text-cc-heading leading-none font-semibold"
            style={{ fontSize: "2rem" }}
          >
            1
          </span>
        </div>
        <p className="text-cc-ink-dim mt-1.5 font-mono text-[0.7rem] lowercase">
          glue files into one token
        </p>

        {/* TEAL SIGNAL: twelve coupled glue-file segment cells funnel down into
            one generated source token. */}
        <svg
          viewBox="0 0 288 84"
          width="100%"
          role="img"
          aria-label="Twelve hand-maintained glue files, kept in sync, collapse into a single generated [QueryType] ProductApi token."
          className="mt-4"
          style={{ display: "block" }}
        >
          {/* the twelve glue-file segment marks: two rows of six */}
          {COLS.map((left, i) => (
            <g key={`${ID}cell-${i}`}>
              <rect
                x={left}
                y={row1Y}
                width={cellW}
                height={cellH}
                rx="3"
                fill={cc.surface}
                stroke={cc.inkFaint}
                strokeWidth="1"
              />
              <rect
                x={left}
                y={row2Y}
                width={cellW}
                height={cellH}
                rx="3"
                fill={cc.surface}
                stroke={cc.inkFaint}
                strokeWidth="1"
              />
              {/* "kept in sync" coupling between the two rows */}
              <line
                x1={center(left)}
                y1={row1Y + cellH}
                x2={center(left)}
                y2={row2Y}
                stroke={cc.inkFaint}
                strokeWidth="1"
                strokeDasharray="2 2"
              />
              {/* funnel: each bottom-row cell collapses toward the junction */}
              <line
                x1={center(left)}
                y1={row2Y + cellH}
                x2={nodeX}
                y2={nodeY - 4}
                stroke={cc.inkFaint}
                strokeWidth="1"
              />
            </g>
          ))}

          {/* junction node: the cell's one filled teal node, on the surviving 1 */}
          <circle cx={nodeX} cy={nodeY} r="5.5" fill={cc.surface} />
          <circle cx={nodeX} cy={nodeY} r="3.2" fill={cc.teal} />

          {/* short teal connector from the node into the token pill */}
          <line
            x1={nodeX}
            y1={nodeY + 5.5}
            x2={nodeX}
            y2="58"
            stroke={cc.teal}
            strokeWidth="1"
          />

          {/* the one generated source token */}
          <rect
            x="72"
            y="58"
            width="144"
            height="22"
            rx="8"
            fill={cc.surface}
            stroke={cc.teal}
            strokeWidth="1"
          />
          <text
            x={nodeX}
            y="72.5"
            textAnchor="middle"
            fontFamily={MONO}
            fontSize="8.5"
            fontWeight="600"
            fill={cc.teal}
          >
            [QueryType] ProductApi
          </text>
        </svg>

        {/* footer caption over a dashed faint divider */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center font-mono text-[0.6rem] lowercase">
            kept in sync by hand, now generated
          </p>
        </div>
      </div>
    </div>
  );
}
