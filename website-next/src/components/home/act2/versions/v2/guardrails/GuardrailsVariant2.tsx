interface GuardrailsVariant2Props {
  readonly className?: string;
}

/**
 * Release-safety scene, variant 2 (v2 "Flow Diagrams"): registry check blocks
 * merge.
 *
 * A fan-out: one required Nitro registry check radiates to four sub-step facet
 * chips that each name a distinct, non-overlapping facet of the same 3-change
 * set (compose / diff / impact / policy). The teal traced path runs from the
 * active "registry check" node out to the one facet the headline is about, the
 * `diff` step, which fires BREAKING (coral status) on the removed Product.rating
 * field. Because the policy is block-on-break, the path terminates at the merge
 * gate, drawn BLOCKED (coral border) instead of mergeable. The other three
 * facets stay grey, present but not the traced route.
 *
 * Nodes are the locked Chip primitive (bg-cc-surface, 1px cc-card-border). The
 * merge gate is the derived/terminal node (rounded-md, tighter padding) whose
 * border encodes real status. Connectors are 1px: grey by default, teal only
 * along the single traced check -> diff -> gate route; coral marks only the one
 * genuinely breaking facet and the shut gate. A two-stat footer carries the two
 * numbers the concept turns on. Fully static, no hooks, no animation.
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  ink: "#a1a3af",
  inkFaint: "rgba(245,241,234,0.16)",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
  coral: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v2-guardrails-2-";

/* The four required sub-steps of the one registry check. Each names a distinct,
 * non-overlapping facet of the same 3-change set; the diff facet is the one that
 * fires BREAKING on the Product.rating removal and carries the traced path. */
interface Facet {
  readonly label: string;
  readonly note: string;
  readonly breaking: boolean;
  /** y-center of the facet chip in the svg coordinate space. */
  readonly cy: number;
}

const FACETS: readonly Facet[] = [
  { label: "compose", note: "3 staged", breaking: false, cy: 18 },
  { label: "diff", note: "1 breaking", breaking: true, cy: 44 },
  { label: "impact", note: "3 clients", breaking: false, cy: 70 },
  { label: "policy", note: "block on break", breaking: false, cy: 96 },
];

export function GuardrailsVariant2({ className }: GuardrailsVariant2Props) {
  // Hub x-edge and facet column geometry.
  const hubRight = 84; // right edge of the registry-check hub node
  const facetX = 150; // left edge of each facet chip
  const diffRight = 246; // right edge of the diff facet chip (path origin)

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* Panel eyebrow (ScrollScenes header). */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          registry check blocks merge
        </p>

        {/* Fan-out: one required check -> four facets; the diff facet fires
            breaking and the traced teal path drops to a blocked merge gate. */}
        <div className="mt-4">
          <svg
            viewBox="0 0 288 152"
            width="100%"
            role="img"
            aria-label="Flow diagram: one required Nitro registry check fans out to four sub-step facets (compose, diff, impact, policy) over a 3-change set; the schema-diff facet fires breaking on the removed Product.rating field, and the merge gate is blocked"
            style={{ display: "block" }}
          >
            <defs>
              {/* Grey open arrowhead for the fan-out facet connectors. */}
              <marker
                id={`${ID}headGrey`}
                markerWidth="6"
                markerHeight="6"
                refX="4.4"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M1 1 L4.5 3 L1 5"
                  fill="none"
                  stroke={C.inkFaint}
                  strokeWidth="1"
                />
              </marker>
              {/* Teal open arrowhead for the traced check -> diff segment. */}
              <marker
                id={`${ID}headTeal`}
                markerWidth="6"
                markerHeight="6"
                refX="4.4"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M1 1 L4.5 3 L1 5"
                  fill="none"
                  stroke={C.accent}
                  strokeWidth="1"
                />
              </marker>
              {/* Coral open arrowhead for the breaking -> blocked-gate segment. */}
              <marker
                id={`${ID}headCoral`}
                markerWidth="6"
                markerHeight="6"
                refX="4.4"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M1 1 L4.5 3 L1 5"
                  fill="none"
                  stroke={C.coral}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* ---- Fan-out connectors (drawn under the nodes) ---- */}
            {FACETS.map((facet) => {
              const traced = facet.breaking;
              const elbowX = 117; // single shared elbow column for the fan
              return (
                <path
                  key={`${ID}edge-${facet.label}`}
                  d={`M${hubRight} 57 L${elbowX} 57 L${elbowX} ${facet.cy} L${facetX} ${facet.cy}`}
                  fill="none"
                  stroke={traced ? C.accent : C.inkFaint}
                  strokeWidth="1"
                  markerEnd={`url(#${ID}head${traced ? "Teal" : "Grey"})`}
                />
              );
            })}

            {/* ---- Node: required registry check (active hub, teal) ---- */}
            <g>
              <rect
                x="6"
                y="42"
                width="78"
                height="30"
                rx="8"
                fill={C.surface}
                stroke={C.accent}
                strokeWidth="1"
                strokeOpacity="0.6"
              />
              <text
                x="45"
                y="54"
                textAnchor="middle"
                fill={C.navLabel}
                fontFamily={C.mono}
                fontSize="6.5"
                letterSpacing="0.1em"
              >
                REQUIRED
              </text>
              <text
                x="45"
                y="66"
                textAnchor="middle"
                fill={C.accent}
                fontFamily={C.mono}
                fontSize="9"
              >
                registry check
              </text>
            </g>

            {/* ---- The four facet chips fanned out from the hub ---- */}
            {FACETS.map((facet) => {
              const breaking = facet.breaking;
              const stroke = breaking ? C.coral : C.cardBorder;
              const labelFill = breaking ? C.coral : C.ink;
              const noteFill = breaking ? C.coral : C.inkDim;
              return (
                <g key={`${ID}node-${facet.label}`}>
                  <rect
                    x={facetX}
                    y={facet.cy - 11}
                    width={diffRight - facetX}
                    height="22"
                    rx="6"
                    fill={C.surface}
                    stroke={stroke}
                    strokeWidth="1"
                  />
                  {/* leading status dot only on the genuinely breaking facet */}
                  {breaking && (
                    <circle
                      cx={facetX + 9}
                      cy={facet.cy}
                      r="2.4"
                      fill={C.coral}
                    />
                  )}
                  <text
                    x={breaking ? facetX + 17 : facetX + 9}
                    y={facet.cy + 3}
                    fill={labelFill}
                    fontFamily={C.mono}
                    fontSize="9"
                  >
                    {facet.label}
                  </text>
                  <text
                    x={diffRight - 8}
                    y={facet.cy + 3}
                    textAnchor="end"
                    fill={noteFill}
                    fontFamily={C.mono}
                    fontSize="7.5"
                    letterSpacing="0.02em"
                  >
                    {facet.note}
                  </text>
                </g>
              );
            })}

            {/* ---- Traced coral segment: the breaking diff facet drops the
                 path down to the shut merge gate (block on break) ---- */}
            <path
              d="M198 55 L198 128"
              fill="none"
              stroke={C.coral}
              strokeWidth="1"
              markerEnd={`url(#${ID}headCoral)`}
            />

            {/* ---- Derived/terminal node: the merge gate, drawn BLOCKED ---- */}
            <g>
              <rect
                x="150"
                y="128"
                width="132"
                height="20"
                rx="5"
                fill={C.surface}
                stroke={C.coral}
                strokeWidth="1"
              />
              <circle cx="160" cy="138" r="2.4" fill={C.coral} />
              <text
                x="170"
                y="141"
                fill={C.coral}
                fontFamily={C.mono}
                fontSize="8.5"
                letterSpacing="0.04em"
              >
                merge blocked
              </text>
            </g>

            {/* ---- The one breaking change, as a mono removal-diff annotation
                 anchored under the hub on the left ---- */}
            <g>
              <text
                x="6"
                y="116"
                fill={C.coral}
                fontFamily={C.mono}
                fontSize="9"
                letterSpacing="0.02em"
              >
                &#8722; Product.rating
              </text>
              <text
                x="6"
                y="130"
                fill={C.inkDim}
                fontFamily={C.mono}
                fontSize="7.5"
                letterSpacing="0.02em"
              >
                field removed
              </text>
            </g>
          </svg>
        </div>

        {/* Two-stat footer (ServiceTopology pattern): the two numbers the
            blocked check turns on. */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              1 of 3
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">changes breaking</p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              3
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">clients affected</p>
          </div>
        </div>

        {/* Caption under a dashed divider (ScrollScenes voice). */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            unsafe changes never reach main
          </p>
        </div>
      </div>
    </div>
  );
}
