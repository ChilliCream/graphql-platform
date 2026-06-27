interface GuardrailsVariant2Props {
  readonly className?: string;
}

/**
 * "Release safety" scene, v4 "Generated Artifacts", variant 2: the failing Nitro
 * schema-registry check that blocks the merge (locked v4 PATTERN A, a single
 * artifact tile with one callout).
 *
 * One cc-surface tile is the required `nitro schema check` printed back on the
 * PR: a title bar (shield glyph + check name + `required` tag) closed by a 1px
 * divider, then the four sub-steps the one required check runs - `compose`,
 * `diff`, `impact`, `policy` - each naming a distinct, non-overlapping facet of
 * the same 3-change set. Three facets complete with a neutral mono check; the
 * `diff` facet fails on the removed `Product.rating` field and carries the one
 * coral status channel. The verdict line reads `merge blocked`. All literals
 * (the four facet names, `3 staged`, `Product.rating`, `3 clients`, `block on
 * break`, `merge blocked`) are borrowed verbatim from the v1 / v2 siblings.
 *
 * Because the subject IS the block, status (coral) owns the single accent
 * cluster and teal steps aside - there is no teal in this cell. The signature
 * callout is coral: a 3px anchor dot on the load-bearing `merge blocked` verdict
 * token, a 1px coral leader running into the row's negative space with an open
 * chevron, and a "GATED" micro-label naming what the verdict IS. Strip the coral
 * and the artifact still reads as a neutral mono check; coral never competes with
 * a second highlight.
 *
 * React Server Component: no "use client", no hooks, no animation, settled final
 * frame. Every svg id is prefixed "v4-guardrails-2-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  coral: "#f0786a",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v4-guardrails-2-";

interface CheckStep {
  /** The facet of the 3-change set this required sub-step inspects. */
  readonly facet: string;
  /** Right-aligned result for the facet. */
  readonly value: string;
  /** Composition, impact, and policy complete; only the diff facet fails. */
  readonly fail?: boolean;
}

// The four sub-steps the one required check runs over the proposed schema. Each
// names a distinct facet of the same 3-change set; only `diff` fails, on the
// removed `Product.rating` field. Facet names and notes are lifted verbatim from
// the v2 sibling; the breaking field is lifted from the v1 sibling.
const STEPS: readonly CheckStep[] = [
  { facet: "compose", value: "3 staged" },
  { facet: "diff", value: "Product.rating", fail: true },
  { facet: "impact", value: "3 clients" },
  { facet: "policy", value: "block on break" },
];

const ROW_Y = [41, 60, 79, 98] as const;

export function GuardrailsVariant2({ className }: GuardrailsVariant2Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          registry check
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 320 156"
            width="100%"
            role="img"
            aria-label="The required nitro schema check on a pull request: compose, impact, and policy sub-steps complete, but the diff sub-step fails on the removed Product.rating field, so the verdict is merge blocked."
            style={{ display: "block", fontFamily: MONO }}
          >
            <defs>
              {/* Coral open chevron for the single (status-owned) callout leader. */}
              <marker
                id={`${ID}head`}
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
                  stroke={C.coral}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* Artifact tile: the required check printed back on the PR. */}
            <rect
              x={8}
              y={2}
              width={304}
              height={150}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Title bar: shield glyph + check name + `required` tag. */}
            <path
              d="M19 6 L25 8.2 V12.6 C25 16.6 22.4 18.9 19 20 C15.6 18.9 13 16.6 13 12.6 V8.2 Z"
              fill="none"
              stroke={C.inkDim}
              strokeWidth={1}
              strokeLinejoin="round"
            />
            <text x={31} y={16} fontSize={10} fontWeight={600} fill={C.heading}>
              nitro schema check
            </text>
            <text
              x={304}
              y={15}
              textAnchor="end"
              fontSize={8}
              letterSpacing="0.1em"
              fill={C.navLabel}
            >
              required
            </text>
            <line
              x1={8}
              y1={24}
              x2={312}
              y2={24}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Four required sub-steps: three neutral checks, one coral fail. */}
            {STEPS.map((step, i) => {
              const y = ROW_Y[i];
              const labelFill = step.fail ? C.coral : C.ink;
              const valueFill = step.fail ? C.coral : C.navLabel;
              return (
                <g key={`${ID}step-${i}`}>
                  {step.fail ? (
                    <path
                      d={`M18 ${y - 6.5} L24 ${y - 0.5} M24 ${y - 6.5} L18 ${y - 0.5}`}
                      fill="none"
                      stroke={C.coral}
                      strokeWidth={1.4}
                      strokeLinecap="round"
                    />
                  ) : (
                    <path
                      d={`M18 ${y - 3.5} L20.4 ${y - 1} L24.5 ${y - 7}`}
                      fill="none"
                      stroke={C.inkDim}
                      strokeWidth={1.4}
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  )}
                  <text x={32} y={y} fontSize={9} fill={labelFill}>
                    {step.facet}
                  </text>
                  <text
                    x={300}
                    y={y}
                    textAnchor="end"
                    fontSize={8.5}
                    fill={valueFill}
                    style={{ fontVariantNumeric: "tabular-nums" }}
                  >
                    {step.value}
                  </text>
                </g>
              );
            })}

            {/* Divider above the verdict line. */}
            <line
              x1={20}
              y1={111}
              x2={300}
              y2={111}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Verdict: the gate decision, the single load-bearing token. */}
            <text x={20} y={130} fontSize={11} fontWeight={600} fill={C.coral}>
              merge blocked
            </text>

            {/* Signature coral callout on `merge blocked`: dot, leader, label. */}
            <circle cx={108} cy={126} r={3} fill={C.coral} />
            <line
              x1={112}
              y1={126}
              x2={176}
              y2={126}
              stroke={C.coral}
              strokeWidth={1}
              markerEnd={`url(#${ID}head)`}
            />
            <text
              x={182}
              y={129}
              fontSize={7}
              letterSpacing="0.12em"
              fill={C.coral}
            >
              GATED
            </text>
          </svg>
        </div>

        {/* Dashed caption: the gate keeps unsafe changes off the main branch. */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            unsafe changes never reach main
          </p>
        </div>
      </div>
    </div>
  );
}
