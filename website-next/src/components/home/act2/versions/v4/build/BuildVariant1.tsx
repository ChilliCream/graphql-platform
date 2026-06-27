interface BuildVariant1Props {
  readonly className?: string;
}

/**
 * "Build loop" scene, v4 "Generated Artifacts", concept #1: annotated source to
 * generated artifacts.
 *
 * Pattern B (two stacked tiles + one teal single-elbow connector). The top tile is
 * the implementation-first C# the Hot Chocolate source generator reads: the
 * annotated `[QueryType]` partial class `ProductApi` with its `GetProduct(int id)`
 * resolver (`QueryType` is the one cream strong source token). The bottom tile is
 * the emitted `schema.graphql`, whose generated `product` field is the load-bearing
 * teal token.
 *
 * The single teal callout is the only teal in the cell and doubles as the Pattern B
 * connector: a 2.5px anchor dot on the source resolver, a 1px curved leader with a
 * chevron arrowhead landing on the generated `product` token, a 2px underline tick
 * beneath it, and a "GENERATED" micro-label. Strip the teal and both tiles read as
 * neutral monochrome code; tile borders, dividers, and title bars stay grey.
 *
 * The "one source, many artifacts" thesis stays explicit through the Stat duo (1
 * annotated source / 3 generated artifacts: schema, resolver pipeline, typed
 * DataLoader) even though only the contract tile is drawn.
 *
 * React Server Component: no hooks, no client APIs, settled final frame only. All
 * literal artifact strings are borrowed verbatim from the v1 / v2 siblings. Every
 * svg id is prefixed "v4-build-1-".
 */
export function BuildVariant1({ className }: BuildVariant1Props) {
  const ID = "v4-build-1-";

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

        {/* source tile -> emitted schema.graphql tile, one teal connector callout */}
        <svg
          viewBox="0 0 320 168"
          width="100%"
          role="img"
          aria-label="An annotated C# QueryType source class generating a schema.graphql artifact whose product field is marked generated."
          className="mt-4"
          style={{ display: "block", fontFamily: MONO }}
        >
          <defs>
            <marker
              id={`${ID}arrow`}
              markerWidth="6"
              markerHeight="6"
              refX="4.6"
              refY="3"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={C.accent}
                strokeWidth="1"
              />
            </marker>
          </defs>

          {/* source tile (grey): the annotated [QueryType] partial class */}
          <rect
            x={10}
            y={4}
            width={300}
            height={72}
            rx={8}
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth="1"
          />
          <text x={22} y={18} fill={C.inkDim} fontSize="9" fontWeight={600}>
            ProductApi
          </text>
          <text
            x={298}
            y={18}
            textAnchor="end"
            fill={C.navLabel}
            fontSize="7.5"
            letterSpacing="0.05em"
          >
            .cs
          </text>
          <line
            x1={10}
            y1={27}
            x2={310}
            y2={27}
            stroke={C.cardBorder}
            strokeWidth="1"
          />
          <text x={22} y={44} fontSize="9.5">
            <tspan fill={C.navLabel}>[</tspan>
            <tspan fill={C.heading}>QueryType</tspan>
            <tspan fill={C.navLabel}>]</tspan>
          </text>
          <text x={22} y={58} fontSize="9.5">
            <tspan fill={C.navLabel}>public partial class </tspan>
            <tspan fill={C.ink}>ProductApi</tspan>
          </text>
          <text x={22} y={72} fontSize="9.5">
            <tspan fill={C.ink}>Product GetProduct</tspan>
            <tspan fill={C.navLabel}>(int id)</tspan>
          </text>

          {/* artifact tile (grey): the emitted schema.graphql */}
          <rect
            x={10}
            y={104}
            width={300}
            height={58}
            rx={8}
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth="1"
          />
          <text x={22} y={118} fill={C.inkDim} fontSize="9" fontWeight={600}>
            schema
          </text>
          <text
            x={298}
            y={118}
            textAnchor="end"
            fill={C.navLabel}
            fontSize="7.5"
            letterSpacing="0.05em"
          >
            .graphql
          </text>
          <line
            x1={10}
            y1={127}
            x2={310}
            y2={127}
            stroke={C.cardBorder}
            strokeWidth="1"
          />
          <text x={22} y={150} fontSize="9">
            <tspan fill={C.navLabel}>type </tspan>
            <tspan fill={C.ink}>Query </tspan>
            <tspan fill={C.navLabel}>{"{ "}</tspan>
            <tspan fill={C.accent}>product</tspan>
            <tspan fill={C.navLabel}>(id: </tspan>
            <tspan fill={C.ink}>Int!</tspan>
            <tspan fill={C.navLabel}>): </tspan>
            <tspan fill={C.ink}>Product </tspan>
            <tspan fill={C.navLabel}>{"}"}</tspan>
          </text>

          {/* single teal callout (only teal): source resolver dot -> 1px curved
              leader -> the generated `product` token, with underline tick + label */}
          <circle cx={123} cy={68} r="2.5" fill={C.accent} />
          <path
            d="M123 68 C 123 102, 112 124, 111 142"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow)`}
          />
          <line
            x1={92}
            y1={155}
            x2={130}
            y2={155}
            stroke={C.accent}
            strokeWidth="2"
          />
          <text
            x={74}
            y={98}
            fill={C.accent}
            fontSize="7"
            letterSpacing="0.1em"
          >
            GENERATED
          </text>
        </svg>

        {/* Stat duo: one annotated source, three generated artifacts */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="1" label="annotated source" />
          <Stat figure="3" label="generated artifacts" />
        </div>
      </div>
    </div>
  );
}

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked v4 cc-* palette: surfaces, ink ramp, the one cream token, single teal. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  heading: "#f5f0ea",
  navLabel: "#62748e",
  accent: "#5eead4",
} as const;

interface StatProps {
  readonly figure: string;
  readonly label: string;
}

/** The ScrollScenes Stat: a display numeral over a small dim caption. */
function Stat({ figure, label }: StatProps) {
  return (
    <div>
      <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-1.5 text-xs">{label}</p>
    </div>
  );
}
