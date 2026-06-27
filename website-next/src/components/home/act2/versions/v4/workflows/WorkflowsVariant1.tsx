interface WorkflowsVariant1Props {
  readonly className?: string;
}

/**
 * Workflow scene, v4 "Generated Artifacts", concept #1: the compile-time wiring
 * manifest.
 *
 * Pattern A (one hero artifact tile + a single teal callout). The tile is the
 * source-generated `MochaWiring.g.cs`, presented as a registry the generator
 * emits: three tagged rows for the small pieces it discovered and wired at build
 * time. A HANDLER row binds the in-flight `CreateReview` command to its
 * `CreateReviewHandler`, an EVENT row lists `ReviewCreated`, and a SAGA row lists
 * `ReviewSaga`. Kind tags stay cc-nav-label, discovered identifiers stay cc-ink,
 * and the filename is the one cream strong token.
 *
 * The single teal callout is the only teal in the cell: `CreateReviewHandler`,
 * the handler the generator auto-wired for the in-flight command, is the one
 * recolored token, marked by a 2.5px anchor dot, a 2px underline tick, a 1px
 * leader dropping into the open lower band, and a "WIRED AT BUILD" micro-label.
 * Strip the teal and the manifest reads as a neutral monochrome registry. No
 * status hue is used (the cell encodes no adverse state).
 *
 * Literal content (CreateReview / CreateReviewHandler, ReviewCreated, ReviewSaga)
 * is borrowed verbatim from the v1 / v2 / ScrollScenes siblings so the artifact is
 * accurate. React Server Component: no "use client", no hooks, no motion, settled
 * final frame only. Every svg id is prefixed "v4-workflows-1-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v4-workflows-1-";

// One discovered piece per registry row: a kind tag plus its emitted identifier.
const ROWS: readonly { readonly kind: string; readonly y: number }[] = [
  { kind: "HANDLER", y: 52 },
  { kind: "EVENT", y: 80 },
  { kind: "SAGA", y: 108 },
];

/** A bordered kind tag (HANDLER / EVENT / SAGA) on the cc-surface tile. */
function KindTag({ label, y }: { readonly label: string; readonly y: number }) {
  return (
    <g>
      <rect
        x={18}
        y={y - 10}
        width={48}
        height={14}
        rx={3}
        fill="none"
        stroke={C.cardBorder}
        strokeWidth={1}
      />
      <text
        x={42}
        y={y}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="7"
        letterSpacing="0.08em"
        fill={C.navLabel}
      >
        {label}
      </text>
    </g>
  );
}

export function WorkflowsVariant1({ className }: WorkflowsVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          compile-time wiring
        </p>

        {/* one hero tile: the generated wiring manifest, three discovered rows */}
        <svg
          viewBox="0 0 320 152"
          width="100%"
          role="img"
          aria-label="A source-generated MochaWiring.g.cs registry listing the handler, event, and saga discovered at build time, with CreateReviewHandler auto-wired to the in-flight CreateReview command."
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

          {/* hero tile */}
          <rect
            x={8}
            y={4}
            width={304}
            height={140}
            rx={8}
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth={1}
          />

          {/* title bar: filename (cream) + generated tag, closed by a divider */}
          <text
            x={20}
            y={22}
            fontFamily={MONO}
            fontSize="11"
            fontWeight={600}
            fill={C.heading}
          >
            MochaWiring.g.cs
          </text>
          <text
            x={300}
            y={22}
            textAnchor="end"
            fontFamily={MONO}
            fontSize="8"
            letterSpacing="0.05em"
            fill={C.navLabel}
          >
            generated
          </text>
          <line
            x1={8}
            y1={31}
            x2={312}
            y2={31}
            stroke={C.cardBorder}
            strokeWidth={1}
          />

          {/* kind tags for the three discovered pieces */}
          {ROWS.map((row) => (
            <KindTag key={row.kind} label={row.kind} y={row.y} />
          ))}

          {/* HANDLER row: the in-flight command bound to its discovered handler */}
          <text x={74} y={52} fontSize="8.5" fill={C.ink}>
            CreateReview
          </text>
          <text x={140} y={52} fontSize="8.5" fill={C.navLabel}>
            {"→"}
          </text>
          <text x={152} y={52} fontSize="8.5" fill={C.accent}>
            CreateReviewHandler
          </text>

          {/* EVENT row: the reaction event the generator also wired */}
          <text x={74} y={80} fontSize="8.5" fill={C.ink}>
            ReviewCreated
          </text>

          {/* SAGA row: the saga state machine the generator also wired */}
          <text x={74} y={108} fontSize="8.5" fill={C.ink}>
            ReviewSaga
          </text>

          {/* single teal callout: dot + underline tick + leader + micro-label */}
          <circle cx={152} cy={49} r={2.5} fill={C.accent} />
          <line
            x1={152}
            y1={58}
            x2={248}
            y2={58}
            stroke={C.accent}
            strokeWidth={2}
          />
          <path
            d="M219 58 C 219 86, 219 106, 222 120"
            fill="none"
            stroke={C.accent}
            strokeWidth={1}
            markerEnd={`url(#${ID}arrow)`}
          />
          <text
            x={298}
            y={124}
            textAnchor="end"
            fontFamily={MONO}
            fontSize="7"
            letterSpacing="0.12em"
            fill={C.accent}
          >
            WIRED AT BUILD
          </text>
        </svg>

        {/* dashed caption footer: generated, not hand-maintained */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            discovered and wired at build, not by hand
          </p>
        </div>
      </div>
    </div>
  );
}
