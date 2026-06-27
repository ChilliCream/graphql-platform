interface WorkflowsVariant1Props {
  readonly className?: string;
}

/**
 * Workflow scene, v5 "Schematic Lines", concept #1: compile-time wiring manifest.
 *
 * A reductive monoline wiring map. One grey source-generator hub ring on the left
 * (`Mocha codegen`) fans grey 1px orthogonal connectors out to the four small
 * pieces it discovered and wired at build time: the `CreateReview` command, its
 * `ReviewHandler`, the `ReviewCreated` event, and the `PublishSaga`. Strip the
 * teal and the whole thing reads as a quiet grey wiring tree, exactly the manifest
 * the source generator emits.
 *
 * The single teal thread is the only accent and the one route the headline names:
 * the in-flight `CreateReview` command dispatched into its handler. It begins at
 * the hollow teal source ring (the command), runs down the one teal connector, and
 * terminates on the focal `ReviewHandler` (stroked teal) with a teal chevron and a
 * solid teal landing dot. Nothing else is teal; there is no status hue here.
 *
 * React Server Component: no hooks, no client APIs, settled final frame only. All
 * literal piece names are borrowed verbatim from the v2 sibling. Every svg id is
 * prefixed "v5-workflows-1-".
 */

const ID = "v5-workflows-1-";

interface Piece {
  /** Mono identifier rendered beside the ring. */
  readonly label: string;
  /** Vertical center of the ring in the 280x150 canvas. */
  readonly cy: number;
  /** The two pieces on the traced in-flight route read teal. */
  readonly accent?: "source" | "focal";
}

// The right rank of discovered/wired pieces. Kinds are carried by the names
// themselves (command, handler, event, saga). CreateReview and ReviewHandler
// form the single teal in-flight route.
const PIECES: readonly Piece[] = [
  { label: "CreateReview", cy: 26, accent: "source" },
  { label: "ReviewHandler", cy: 62, accent: "focal" },
  { label: "ReviewCreated", cy: 98 },
  { label: "PublishSaga", cy: 130 },
];

const HUB = { cx: 54, cy: 78, r: 11 } as const;
const RING_CX = 176;
const RING_R = 8;
const BUS_X = 96;

export function WorkflowsVariant1({ className }: WorkflowsVariant1Props) {
  const hubRightX = HUB.cx + HUB.r;
  const ringLeftX = RING_CX - RING_R - 3;

  // Single-elbow orthogonal connector: hub -> shared bus -> piece ring.
  function branch(cy: number): string {
    return `M${hubRightX} ${HUB.cy} H${BUS_X} V${cy} H${ringLeftX}`;
  }

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          wiring manifest
        </p>

        {/* Source-generator hub fanning to the discovered pieces; one teal route. */}
        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="Mocha's source generator at the left fans grey connectors out to the command, handler, event, and saga it discovered and wired at build time, with the CreateReview command traced in flight to its handler."
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

          {/* Grey wiring tree: source gen -> every discovered piece. */}
          {PIECES.map((piece) => (
            <path
              key={`${ID}branch-${piece.cy}`}
              d={branch(piece.cy)}
              fill="none"
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
              strokeLinejoin="round"
              markerEnd={`url(#${ID}arrow-grey)`}
            />
          ))}

          {/* Teal thread: the in-flight CreateReview command into its handler. */}
          <path
            d={`M${RING_CX} 34 V54`}
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
            markerEnd={`url(#${ID}arrow-teal)`}
          />

          {/* Source-generator hub ring. */}
          <circle
            cx={HUB.cx}
            cy={HUB.cy}
            r={HUB.r}
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* Discovered-piece rings (command + handler read teal). */}
          {PIECES.map((piece) => (
            <circle
              key={`${ID}ring-${piece.cy}`}
              cx={RING_CX}
              cy={piece.cy}
              r={RING_R}
              fill="none"
              stroke={piece.accent ? C.accent : C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
          ))}

          {/* Solid teal landing dot on the focal handler. */}
          <circle cx={RING_CX} cy={62} r="2.5" fill={C.accent} />

          {/* Sparse labels: the source generator plus each named piece. */}
          <text
            x={HUB.cx}
            y="100"
            textAnchor="middle"
            fontSize="8"
            fill={C.ink}
          >
            Mocha codegen
          </text>
          {PIECES.map((piece) => (
            <text
              key={`${ID}label-${piece.cy}`}
              x={RING_CX + 13}
              y={piece.cy + 3}
              fontSize="8"
              fill={C.ink}
            >
              {piece.label}
            </text>
          ))}
        </svg>

        {/* Lone footer numeral: the auto-wiring flex. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            0
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">lines of wiring code</p>
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
