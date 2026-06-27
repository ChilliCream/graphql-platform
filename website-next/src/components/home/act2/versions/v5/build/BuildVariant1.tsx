interface BuildVariant1Props {
  readonly className?: string;
}

/**
 * "Build loop" scene, v5 "Schematic Lines", concept #1: annotated source to
 * generated artifacts.
 *
 * A reductive monoline radial fan-out. One hub ring is the annotated `[QueryType]`
 * partial class `ProductApi` the Hot Chocolate source generator reads; three open
 * artifact rings on the right are what it emits: the `schema.graphql` SDL contract,
 * the `ProductApi.g.cs` resolver-pipeline registration, and the typed
 * `ProductDataLoader`. One source, many generated artifacts.
 *
 * The single teal thread is the only accent: it leaves the hollow teal source ring,
 * traces the one route the headline names (source -> generated `schema.graphql`
 * contract) and terminates on the focal ring (stroked teal) with a teal chevron and
 * a solid teal landing dot. Strip the teal and the whole schematic reads as a quiet
 * grey wiring map; every other ring, connector, and the lone footer numeral stay
 * neutral.
 *
 * React Server Component: no hooks, no client APIs, settled final frame only. All
 * literal artifact strings are borrowed verbatim from the v1 / v2 siblings. Every
 * svg id is prefixed "v5-build-1-".
 */
export function BuildVariant1({ className }: BuildVariant1Props) {
  const ID = "v5-build-1-";

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          source &rarr; generated
        </p>

        {/* radial fan-out: one annotated source ring emits three artifact rings */}
        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="One annotated C# source class fanning out into three generated artifacts"
          className="mt-4"
          style={{ display: "block", fontFamily: MONO }}
        >
          <defs>
            <marker
              id={`${ID}arrow-grey`}
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={C.inkFaint}
                strokeWidth="1"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </marker>
            <marker
              id={`${ID}arrow-teal`}
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={C.accent}
                strokeWidth="1"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </marker>
          </defs>

          {/* grey connectors: source hub -> resolver registration, -> DataLoader */}
          <path
            d="M60 75 L167 75"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
            markerEnd={`url(#${ID}arrow-grey)`}
          />
          <path
            d="M59 78.5 L168.4 113.6"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
            markerEnd={`url(#${ID}arrow-grey)`}
          />

          {/* teal thread: source -> generated schema.graphql contract */}
          <path
            d="M59 71.5 L168.4 36.4"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
            markerEnd={`url(#${ID}arrow-teal)`}
          />

          {/* hollow teal source ring: the annotated [QueryType] class */}
          <circle
            cx="48"
            cy="75"
            r="11"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* grey artifact rings */}
          <circle
            cx="176"
            cy="75"
            r="7"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle
            cx="176"
            cy="116"
            r="7"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* focal artifact ring (teal) + solid teal landing dot */}
          <circle
            cx="176"
            cy="34"
            r="7"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="176" cy="34" r="2.5" fill={C.accent} />

          {/* sparse micro-labels: one source value + three artifact names */}
          <text x="48" y="101" textAnchor="middle" fontSize="8" fill={C.ink}>
            [QueryType]
          </text>
          <text x="189" y="37" fontSize="8" fill={C.ink}>
            schema.graphql
          </text>
          <text x="189" y="78" fontSize="8" fill={C.ink}>
            ProductApi.g.cs
          </text>
          <text x="189" y="119" fontSize="8" fill={C.ink}>
            ProductDataLoader
          </text>
        </svg>

        {/* lone footer numeral: the generation span */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            3
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            artifacts from one class
          </p>
        </div>
      </div>
    </div>
  );
}

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked v5 monoline palette: grey schematic ink + the single teal accent. */
const C = {
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  accent: "#5eead4",
} as const;
