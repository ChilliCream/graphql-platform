/**
 * "Build loop" scene illustration (v5 "Schematic Lines", concept #5):
 * the glue tangle collapses to one token.
 *
 * A reductive monoline schematic. On the left, four separately hand-maintained
 * glue files (schema, types, client, dtos) sit as small grey open rings wired
 * into a crossing knot: the manual "kept in sync" tangle. Two grey funnel lines
 * collapse that knot rightward onto a single hollow teal source ring, the one
 * [QueryType] ProductApi token. From there the single teal thread (the answer
 * path) runs clean and straight to a focal output node, the generated resolver
 * pipeline and client. Everything structural is 1px cc-ink-faint grey; teal
 * appears only on the source ring, the thread, its arrowhead, and the terminal
 * dot.
 *
 * Settled final frame: static, no animation, no hooks, no client APIs. React
 * Server Component. Every svg id is prefixed "v5-build-5-".
 */

const P = "v5-build-5-";

// cc-* palette, inlined (the SVG floats on the shared sibling card).
const INK_FAINT = "rgba(245,241,234,0.16)"; // every grey structure stroke
const SURFACE = "#0c1322"; // occluder under a node where a line passes behind
const TEAL = "#5eead4"; // the one accent: source ring, thread, dot, arrowhead
const NAV = "#62748e"; // mono key-labels
const INK = "#a1a3af"; // mono value-labels

// Tangle nodes: four hand-wired glue files knotted together on the left.
const TANGLE = [
  { x: 46, y: 50 },
  { x: 86, y: 58 },
  { x: 48, y: 98 },
  { x: 88, y: 96 },
] as const;

const [A, B, C, D] = TANGLE;

const SOURCE = { x: 160, y: 75 } as const; // the one teal token, collapse target
const FOCAL = { x: 232, y: 75 } as const; // generated output, thread terminus

interface BuildVariant5Props {
  readonly className?: string;
}

export function BuildVariant5({ className }: BuildVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div
        className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm"
        aria-hidden="true"
      >
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          glue collapse
        </p>

        <svg
          width="100%"
          viewBox="0 0 280 150"
          fill="none"
          className="mt-3 block"
          aria-hidden="true"
        >
          <defs>
            <marker
              id={`${P}arrow-teal`}
              markerUnits="userSpaceOnUse"
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={TEAL}
                strokeWidth="1"
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
            </marker>
          </defs>

          {/* grey knot + collapse funnel (1px cc-ink-faint, the tangled glue) */}
          <g
            stroke={INK_FAINT}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          >
            <line x1={A.x} y1={A.y} x2={D.x} y2={D.y} />
            <line x1={C.x} y1={C.y} x2={B.x} y2={B.y} />
            <line x1={A.x} y1={A.y} x2={C.x} y2={C.y} />
            <line x1={B.x} y1={B.y} x2={SOURCE.x} y2={SOURCE.y} />
            <line x1={D.x} y1={D.y} x2={SOURCE.x} y2={SOURCE.y} />
          </g>

          {/* the single teal thread: one token -> generated output */}
          <line
            x1={SOURCE.x + 12}
            y1={SOURCE.y}
            x2={FOCAL.x - 8}
            y2={FOCAL.y}
            stroke={TEAL}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${P}arrow-teal)`}
          />

          {/* occlude grey lines beneath the tangle nodes, then draw open rings */}
          {TANGLE.map((n, i) => (
            <circle key={`${P}occ-${i}`} cx={n.x} cy={n.y} r="6" fill={SURFACE} />
          ))}
          {TANGLE.map((n, i) => (
            <circle
              key={`${P}node-${i}`}
              cx={n.x}
              cy={n.y}
              r="5"
              fill="none"
              stroke={INK_FAINT}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
          ))}

          {/* the one teal source ring: [QueryType] ProductApi */}
          <circle cx={SOURCE.x} cy={SOURCE.y} r="10" fill={SURFACE} />
          <circle
            cx={SOURCE.x}
            cy={SOURCE.y}
            r="11"
            fill="none"
            stroke={TEAL}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* focal output node + solid teal terminal dot */}
          <circle
            cx={FOCAL.x}
            cy={FOCAL.y}
            r="6"
            fill="none"
            stroke={TEAL}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx={FOCAL.x} cy={FOCAL.y} r="2.5" fill={TEAL} />

          {/* registration baseline ticks under the resolved flow (the scale) */}
          <g
            stroke={INK_FAINT}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          >
            {[120, 136, 152, 168, 184, 200, 216, 232].map((x) => (
              <line key={`${P}tick-${x}`} x1={x} y1={124} x2={x} y2={129} />
            ))}
          </g>

          {/* sparse mono micro-labels */}
          <text
            x={67}
            y={34}
            textAnchor="middle"
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize="7"
            letterSpacing="0.08em"
            fill={NAV}
          >
            KEPT IN SYNC
          </text>
          <text
            x={SOURCE.x}
            y={100}
            textAnchor="middle"
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize="7"
            letterSpacing="0.06em"
            fill={NAV}
          >
            [QueryType]
          </text>
          <text
            x={SOURCE.x}
            y={111}
            textAnchor="middle"
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize="8.5"
            fill={INK}
          >
            ProductApi
          </text>
          <text
            x={FOCAL.x}
            y={97}
            textAnchor="middle"
            fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
            fontSize="7"
            letterSpacing="0.08em"
            fill={NAV}
          >
            GENERATED
          </text>
        </svg>

        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            4 to 1
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            hand-wired glue files, one generated source
          </p>
        </div>
      </div>
    </div>
  );
}
