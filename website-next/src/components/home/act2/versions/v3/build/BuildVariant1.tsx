interface BuildVariant1Props {
  readonly className?: string;
}

/**
 * "Build loop" scene, v3 "Signal & Metrics" take on concept #1, "annotated source
 * to generated artifacts".
 *
 * Leads with the measured result: ONE annotated [QueryType] ProductApi class
 * generates FIVE artifacts, so the hero is a top-left cream delta "1 -> 5" over a
 * lowercase mono caption (layout B, stat-top / signal-row). The single teal signal
 * beneath is an emit fan: one grey source-class chip on the left whose trunk splits
 * into five teal branches, each ending in a teal emit mark that names the real thing
 * the platform writes from that one class, schema.graphql (the SDL contract), the
 * resolver-pipeline registration, the typed DataLoader, the typed client, and the
 * local Nitro tooling. The source chip stays grey so teal is the lone decorative
 * accent, and no status hue appears because nothing here is failing. A dashed-divider
 * footer closes with the honest reading.
 *
 * Static settled frame, server component, no motion, no hooks, no "use client".
 * Strict cc-* dark palette mirrored inline for the SVG. Every svg id is prefixed
 * "v3-build-1-".
 */
export function BuildVariant1({ className }: BuildVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow: identical placement across the v3 set */}
        <p
          className="font-mono text-[0.58rem] tracking-[0.15em] uppercase"
          style={{ color: cc.navLabel }}
        >
          source &rarr; generated
        </p>

        {/* HERO (layout B): top-left "1 -> 5" cream delta, arrow dim */}
        <div className="mt-3 flex items-baseline gap-2.5">
          <span
            className="leading-none font-semibold"
            style={{
              fontFamily: HEADING,
              fontSize: "2rem",
              color: cc.heading,
              fontVariantNumeric: "tabular-nums",
            }}
          >
            1
          </span>
          <span
            className="font-mono"
            style={{ fontSize: "1rem", color: cc.inkDim }}
          >
            &rarr;
          </span>
          <span
            className="leading-none font-semibold"
            style={{
              fontFamily: HEADING,
              fontSize: "2rem",
              color: cc.heading,
              fontVariantNumeric: "tabular-nums",
            }}
          >
            5
          </span>
        </div>
        <p
          className="mt-1.5 font-mono text-[0.7rem] lowercase"
          style={{ color: cc.inkDim }}
        >
          source &rarr; artifacts
        </p>

        {/* the single teal signal: a grey source chip whose trunk fans into five
            teal branches, each naming a real generated artifact. No arrowheads,
            no boxes on the outputs; the fan itself is the lone teal accent. */}
        <svg
          viewBox="0 0 280 92"
          width="100%"
          role="img"
          aria-label="One annotated [QueryType] ProductApi class generating five artifacts: schema.graphql, the resolver pipeline registration, a typed DataLoader, a typed client, and local Nitro tooling"
          className="mt-4"
          style={{ display: "block", fontFamily: MONO }}
        >
          {/* the one teal element: trunk from the source chip fans to five branches */}
          <g
            fill="none"
            stroke={ACCENT}
            strokeWidth="1"
            strokeOpacity="0.8"
            strokeLinejoin="round"
          >
            <path d="M96 46 H124" />
            {ARTIFACTS.map((a) =>
              a.y === 46 ? (
                <path key={`v3-build-1-branch-${a.y}`} d="M124 46 H150" />
              ) : (
                <path
                  key={`v3-build-1-branch-${a.y}`}
                  d={`M124 46 V${a.y} H150`}
                />
              ),
            )}
          </g>

          {/* split junction + five teal emit marks (the reading) */}
          <circle cx="124" cy="46" r="2" fill={ACCENT} />
          {ARTIFACTS.map((a) => (
            <rect
              key={`v3-build-1-mark-${a.y}`}
              x={150}
              y={a.y - 2.5}
              width={5}
              height={5}
              rx={1.5}
              fill={ACCENT}
            />
          ))}

          {/* the source-class chip (grey: teal is reserved for the emit signal) */}
          <rect
            x={1}
            y={30}
            width={95}
            height={32}
            rx={7}
            fill={cc.surface}
            stroke={cc.cardBorder}
            strokeWidth="1"
          />
          <text
            x={11}
            y={45}
            fill={cc.navLabel}
            fontSize="7"
            letterSpacing="0.08em"
            style={{ textTransform: "uppercase" }}
          >
            [QueryType]
          </text>
          <text x={11} y={57} fill={cc.ink} fontSize="9">
            ProductApi
          </text>

          {/* five generated-artifact labels, light (no boxes) */}
          {ARTIFACTS.map((a) => (
            <text
              key={`v3-build-1-label-${a.y}`}
              x={161}
              y={a.y + 3}
              fill={cc.ink}
              fontSize="8.5"
            >
              {a.name}
            </text>
          ))}
        </svg>

        {/* dashed-divider interpretation caption: the honest reading */}
        <div
          className="mt-4 border-t border-dashed pt-3"
          style={{ borderColor: cc.inkFaint }}
        >
          <p
            className="text-center font-mono text-[0.62rem] lowercase"
            style={{ color: cc.inkDim }}
          >
            one class, no hand-written glue
          </p>
        </div>
      </div>
    </div>
  );
}

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const HEADING = '"Josefin Sans", Futura, sans-serif';

const ACCENT = "#5eead4";

/** Strict cc-* dark palette, exact hex mirrored for inline SVG use. */
const cc = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
} as const;

/**
 * The five artifacts the platform generates from the one annotated class, stacked
 * top-down as fan endpoints. `y` is the SVG row center; the teal branch, emit mark,
 * and label all sit on it.
 */
const ARTIFACTS: readonly { readonly y: number; readonly name: string }[] = [
  { y: 8, name: "schema.graphql" },
  { y: 27, name: "resolvers.g.cs" },
  { y: 46, name: "ProductLoader" },
  { y: 65, name: "ProductClient" },
  { y: 84, name: "nitro tooling" },
];
