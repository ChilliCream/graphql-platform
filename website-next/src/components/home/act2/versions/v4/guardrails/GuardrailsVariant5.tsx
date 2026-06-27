interface GuardrailsVariant5Props {
  readonly className?: string;
}

/**
 * "Release safety" scene, v4 "Generated Artifacts", concept #5: the schema version
 * timeline the registry records (locked v4 PATTERN E, a left-to-right rail of
 * version stages with one lit node).
 *
 * One cc-surface artifact tile renders the `schema.graphql` registry. The title bar
 * (file glyph + filename + `registry` kind tag) closes on a 1px divider, then a thin
 * horizontal rail threads four version nodes left to right: three published versions
 * (`eshops@2268`, `eshops@2271`, and the current `eshops@2274`) settle on the solid
 * part of the rail as neutral nodes, then a dashed not-yet-published hop reaches the
 * candidate `eshops@2291`. Verdict reads from node color, so the gated candidate is
 * the one lit (coral) node.
 *
 * Because the gate verdict is the subject, status owns the single accent cluster and
 * teal steps aside: there is no teal in this cell. The coral cluster is one tight
 * column on the candidate, a `BLOCKED` verdict pill above the lit node, the coral node
 * itself, then the signature callout (2px coral underline tick beneath the gated tag,
 * a 1px coral leader dropping into the negative space, an open chevron, and a "GATED"
 * micro-label). A monochrome reason subline names the breaking change the gate caught,
 * `Product.rating removed`. Strip the coral and the figure reads as neutral mono.
 *
 * Literal content (schema.graphql, eshops@2291, the current eshops@2274, the BLOCKED
 * verdict, Product.rating removed, breaking change) is borrowed verbatim from the v1
 * sibling. React Server Component: no "use client", no hooks, no animation, settled
 * final frame. Every svg id is prefixed "v4-guardrails-5-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  coral: "#f0786a",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v4-guardrails-5-";

// Version history threaded left to right on the rail, oldest first. The three
// published versions sit on the solid rail; the candidate sits past a dashed
// not-yet-published hop and carries the real (coral) verdict via its node color.
const VERSIONS: readonly {
  readonly tag: string;
  readonly x: number;
  readonly kind: "published" | "current" | "candidate";
}[] = [
  { tag: "eshops@2268", x: 50, kind: "published" },
  { tag: "eshops@2271", x: 125, kind: "published" },
  { tag: "eshops@2274", x: 200, kind: "current" },
  { tag: "eshops@2291", x: 275, kind: "candidate" },
];

export function GuardrailsVariant5({ className }: GuardrailsVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          version history
        </p>

        <div className="mt-4">
          <svg
            viewBox="0 0 320 156"
            width="100%"
            role="img"
            aria-label="A schema.graphql version timeline: three published versions eshops@2268, eshops@2271, and the current eshops@2274, then the candidate eshops@2291 held past the gate and blocked because Product.rating was removed, a breaking change."
            style={{ display: "block", fontFamily: MONO }}
          >
            <defs>
              {/* Open coral chevron for the single status callout leader. */}
              <marker
                id={`${ID}arrow`}
                markerWidth="6"
                markerHeight="6"
                refX="3"
                refY="4.6"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M0.5 0 L3 5 L5.5 0"
                  fill="none"
                  stroke={C.coral}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* Registry tile. */}
            <rect
              x={6}
              y={2}
              width={308}
              height={152}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Title bar: file glyph + filename (the one cream token) + kind tag. */}
            <path
              d="M14 6.5 H19.5 L23 10 V18 H14 Z"
              fill="none"
              stroke={C.inkDim}
              strokeWidth={1}
              strokeLinejoin="round"
            />
            <path
              d="M19.5 6.5 V10 H23"
              fill="none"
              stroke={C.inkDim}
              strokeWidth={1}
              strokeLinejoin="round"
            />
            <text x={30} y={16} fill={C.heading} fontSize={10} fontWeight={600}>
              schema.graphql
            </text>
            <text
              x={306}
              y={16}
              textAnchor="end"
              fill={C.navLabel}
              fontSize={8}
              letterSpacing="0.1em"
            >
              registry
            </text>
            <line
              x1={6}
              y1={24}
              x2={314}
              y2={24}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Rail: solid through the published history, dashed for the gated hop. */}
            <line
              x1={50}
              y1={66}
              x2={200}
              y2={66}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <line
              x1={200}
              y1={66}
              x2={275}
              y2={66}
              stroke={C.inkFaint}
              strokeWidth={1}
              strokeDasharray="3 2"
            />

            {/* Version nodes + tags. Verdict reads from node color. */}
            {VERSIONS.map((v) => (
              <g key={v.tag}>
                {v.kind === "candidate" ? (
                  <circle cx={v.x} cy={66} r={3} fill={C.coral} />
                ) : v.kind === "current" ? (
                  <circle
                    cx={v.x}
                    cy={66}
                    r={3.5}
                    fill={C.surface}
                    stroke={C.inkDim}
                    strokeWidth={1}
                  />
                ) : (
                  <circle cx={v.x} cy={66} r={2.5} fill={C.navLabel} />
                )}
                <text
                  x={v.x}
                  y={84}
                  textAnchor="middle"
                  fontSize={7}
                  fill={
                    v.kind === "candidate"
                      ? C.ink
                      : v.kind === "current"
                        ? C.inkDim
                        : C.navLabel
                  }
                  style={{ fontVariantNumeric: "tabular-nums" }}
                >
                  {v.tag}
                </text>
              </g>
            ))}

            {/* Current published head marker. */}
            <text
              x={200}
              y={52}
              textAnchor="middle"
              fontSize={6}
              letterSpacing="0.08em"
              fill={C.inkDim}
            >
              CURRENT
            </text>

            {/* Coral verdict pill above the one lit node. */}
            <rect
              x={250}
              y={40}
              width={50}
              height={14}
              rx={7}
              fill={C.coral}
              fillOpacity={0.1}
              stroke={C.coral}
              strokeWidth={1}
            />
            <text
              x={275}
              y={49.5}
              textAnchor="middle"
              fontSize={6.5}
              letterSpacing="0.08em"
              fill={C.coral}
            >
              BLOCKED
            </text>

            {/* Signature status callout on eshops@2291: tick, leader, label. */}
            <line
              x1={254}
              y1={90}
              x2={296}
              y2={90}
              stroke={C.coral}
              strokeWidth={2}
              strokeLinecap="round"
            />
            <line
              x1={275}
              y1={92}
              x2={275}
              y2={104}
              stroke={C.coral}
              strokeWidth={1}
              markerEnd={`url(#${ID}arrow)`}
            />
            <text
              x={275}
              y={116}
              textAnchor="middle"
              fontSize={7}
              letterSpacing="0.1em"
              fill={C.coral}
            >
              GATED
            </text>

            {/* Reason subline: the breaking change the gate caught (monochrome). */}
            <text x={22} y={140} fontSize={8.5}>
              <tspan fill={C.ink}>Product.rating</tspan>
              <tspan fill={C.inkDim}> removed </tspan>
              <tspan fill={C.navLabel}>&middot;</tspan>
              <tspan fill={C.inkDim}> breaking change</tspan>
            </text>
          </svg>
        </div>

        {/* Dashed caption: the gate sits before publish, every version. */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            gated before publish, never after
          </p>
        </div>
      </div>
    </div>
  );
}
