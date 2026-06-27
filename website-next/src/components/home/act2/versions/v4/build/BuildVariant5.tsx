interface BuildVariant5Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration, v4 "Generated Artifacts" take on concept #5,
 * "glue tangle collapses to one token".
 *
 * Pattern D (convergence fan). The top registry tile is the tangle: four glue
 * files (schema.graphql, types.cs, client.cs, dtos.cs) that today are kept in
 * sync by hand. A grey 1px funnel collapses all four into a single convergence
 * node, which drops into the single source tile, `ProductApi.cs`. Inside it the
 * cream `[QueryType]` annotation marks the teal `ProductApi` source token
 * everything is generated from. The lone teal callout pins that
 * token to a "ONE SOURCE" micro-label dropped into the clear band below the
 * tile, kept distinct from the grey funnel above it. A Stat duo counts the
 * 4 -> 1 collapse.
 *
 * Literals (glue file set, the `[QueryType] ProductApi` source token, the Stat
 * captions) are borrowed verbatim from the v2 build sibling so the artifact is
 * accurate. React Server Component: no hooks, no client APIs, settled final
 * frame only. Every svg id is prefixed "v4-build-5-".
 */

const CC = {
  surface: "#0c1322",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  heading: "#f5f0ea",
  inkFaint: "rgba(245,241,234,0.16)",
  cardBorder: "rgba(245,241,234,0.12)",
  navLabel: "#62748e",
  accent: "#5eead4",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v4-build-5-";

/** The hand-maintained glue files, verbatim from the v2 build sibling. */
const GLUE: readonly { readonly name: string; readonly ext: string }[] = [
  { name: "schema", ext: ".graphql" },
  { name: "types", ext: ".cs" },
  { name: "client", ext: ".cs" },
  { name: "dtos", ext: ".cs" },
];

/** Funnel origins along the tangle tile's bottom edge, converging inward. */
const FUNNEL_X: readonly number[] = [72, 124, 176, 228];

export function BuildVariant5({ className }: BuildVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          glue collapse
        </p>

        {/* four hand-synced glue files funnel into one generated source token */}
        <svg
          viewBox="0 0 300 170"
          width="100%"
          role="img"
          aria-label="Four glue files kept in sync by hand collapsing into one generated ProductApi source token."
          className="mt-3"
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
                stroke={CC.ink}
                strokeOpacity="0.7"
                strokeWidth="1"
              />
            </marker>
          </defs>

          {/* TILE 1: the kept-in-sync glue file registry */}
          <rect
            x={2}
            y={2}
            width={296}
            height={82}
            rx={8}
            fill={CC.surface}
            stroke={CC.cardBorder}
            strokeWidth="1"
          />
          <text
            x={14}
            y={16}
            fill={CC.navLabel}
            fontSize="7.5"
            letterSpacing="0.12em"
            style={{ textTransform: "uppercase" }}
          >
            kept in sync
          </text>
          <text
            x={286}
            y={16}
            textAnchor="end"
            fill={CC.navLabel}
            fontSize="7.5"
            letterSpacing="0.12em"
            style={{ textTransform: "uppercase" }}
          >
            by hand
          </text>
          <line
            x1={2}
            y1={23}
            x2={298}
            y2={23}
            stroke={CC.cardBorder}
            strokeWidth="1"
          />

          {GLUE.map((file, i) => {
            const cy = 23 + 15.25 * (i + 0.5);
            return (
              <g key={file.name}>
                {i > 0 ? (
                  <line
                    x1={2}
                    y1={23 + 15.25 * i}
                    x2={298}
                    y2={23 + 15.25 * i}
                    stroke={CC.cardBorder}
                    strokeWidth="1"
                  />
                ) : null}
                <FileGlyph x={14} y={cy - 5} />
                <text x={28} y={cy + 3.2} fill={CC.ink} fontSize="9.5">
                  {file.name}
                </text>
                <text
                  x={286}
                  y={cy + 3.2}
                  textAnchor="end"
                  fill={CC.navLabel}
                  fontSize="8.5"
                >
                  {file.ext}
                </text>
              </g>
            );
          })}

          {/* funnel: the four files collapse toward one convergence node (grey) */}
          {FUNNEL_X.map((fx) => (
            <line
              key={fx}
              x1={fx}
              y1={84}
              x2={150}
              y2={100}
              stroke={CC.inkFaint}
              strokeWidth="1"
            />
          ))}
          <circle cx={150} cy={100} r={2} fill={CC.ink} fillOpacity="0.7" />
          <path
            d="M150 100 V104"
            fill="none"
            stroke={CC.ink}
            strokeOpacity="0.7"
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow-grey)`}
          />

          {/* TILE 2: the single generated source token (the accent tile) */}
          <rect
            x={66}
            y={106}
            width={168}
            height={46}
            rx={8}
            fill={CC.surface}
            stroke={CC.cardBorder}
            strokeWidth="1"
          />
          <text x={78} y={120} fill={CC.inkDim} fontSize="8.5">
            ProductApi
          </text>
          <text
            x={222}
            y={120}
            textAnchor="end"
            fill={CC.navLabel}
            fontSize="7.5"
            letterSpacing="0.05em"
          >
            .cs
          </text>
          <line
            x1={66}
            y1={126}
            x2={234}
            y2={126}
            stroke={CC.cardBorder}
            strokeWidth="1"
          />

          {/* the one cream annotation + the load-bearing teal source token */}
          <text x={78} y={142} fill={CC.heading} fontSize="9.5">
            [QueryType]
          </text>
          <text x={150} y={142} fill={CC.accent} fontSize="9.5">
            ProductApi
          </text>
          <line
            x1={150}
            y1={145}
            x2={206}
            y2={145}
            stroke={CC.accent}
            strokeWidth="2"
          />

          {/* signature teal callout: dot on the token, leader dropped into the
              clear band below the tile, terminating at the micro-label */}
          <circle cx={178} cy={140} r={2.5} fill={CC.accent} />
          <path d="M178 140 V159" fill="none" stroke={CC.accent} strokeWidth="1" />
          <text
            x={178}
            y={166}
            textAnchor="middle"
            fill={CC.accent}
            fontSize="7"
            letterSpacing="0.12em"
          >
            ONE SOURCE
          </text>
        </svg>

        {/* Stat duo footer: the 4 -> 1 collapse */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="4" label="hand-wired glue files" />
          <Stat figure="1" label="generated from source" />
        </div>
      </div>
    </div>
  );
}

interface FileGlyphProps {
  readonly x: number;
  readonly y: number;
}

/** A small 1px document glyph in the row gutter (a non-accent pointer). */
function FileGlyph({ x, y }: FileGlyphProps) {
  return (
    <g fill="none" stroke={CC.navLabel} strokeWidth="1" strokeLinejoin="round">
      <path d={`M${x} ${y} H${x + 4} L${x + 7} ${y + 3} V${y + 10} H${x} Z`} />
      <path d={`M${x + 4} ${y} V${y + 3} H${x + 7}`} />
    </g>
  );
}

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
