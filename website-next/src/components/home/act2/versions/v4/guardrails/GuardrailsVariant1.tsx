interface GuardrailsVariant1Props {
  readonly className?: string;
}

/**
 * "Release safety" scene, v4 "Generated Artifacts", concept #1: the classified
 * schema diff (locked v4 PATTERN A, single hero tile with one status callout).
 *
 * One full-width artifact tile on cc-surface renders the proposed `schema.graphql`
 * hunk for `type Product`: each changed line carries the generated risk verdict as
 * a trailing pill. The two additive lines (`reviewCount: Int!`, `price: Money!
 * @deprecated`) read SAFE in neutral mono; the one removed field `legacySku: String`
 * reads BREAKING in the single coral status hue. Code stays monochrome cc-ink; the
 * verdict pills are the only classification signal.
 *
 * Because the breaking removal is the subject, status owns the single accent cluster
 * and teal steps aside: the signature callout is coral. A 2.5px coral dot anchors on
 * the load-bearing `legacySku` token, a 2px coral underline tick runs beneath it, a
 * 1px coral leader drops into the negative space below the diff and ends in a chevron
 * at an "AT-RISK" micro-label that names the consumers the removal threatens. There
 * is no teal in this cell, so coral never competes with a second highlight.
 *
 * Literal content (file schema.graphql, fields reviewCount: Int! / legacySku:
 * String / price: Money! @deprecated, SAFE vs BREAKING verdicts, the registry-bot
 * resolve thread) is borrowed verbatim from the v1 / v2 siblings. React Server
 * Component: no "use client", no hooks, no animation, settled final frame. Every
 * svg id is prefixed "v4-guardrails-1-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
  coral: "#f0786a",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v4-guardrails-1-";

export function GuardrailsVariant1({ className }: GuardrailsVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          schema diff classified
        </p>

        <div className="mt-4">
          <svg
            viewBox="0 0 320 150"
            width="100%"
            role="img"
            aria-label="A schema.graphql diff on type Product: two added fields marked safe and the removed legacySku field marked breaking, with its consumers flagged at risk."
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

            {/* Hero tile: the proposed schema.graphql hunk under review. */}
            <rect
              x={6}
              y={2}
              width={308}
              height={146}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Title bar: filename name left (inkDim) + kind tag right (navLabel). */}
            <text x={22} y={18} fill={C.inkDim} fontSize={9} fontWeight={600}>
              schema
            </text>
            <text
              x={306}
              y={18}
              textAnchor="end"
              fill={C.navLabel}
              fontSize={7.5}
              letterSpacing="0.05em"
            >
              .graphql
            </text>
            <line
              x1={6}
              y1={25}
              x2={314}
              y2={25}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* L1: context, the type opening. */}
            <text x={24} y={42} fontSize={9.5}>
              <tspan fill={C.navLabel}>{"type "}</tspan>
              <tspan fill={C.ink}>Product</tspan>
              <tspan fill={C.navLabel}>{" {"}</tspan>
            </text>

            {/* L2: added field, classified SAFE. */}
            <text
              x={15}
              y={60}
              textAnchor="middle"
              fontSize={9.5}
              fill={C.navLabel}
            >
              +
            </text>
            <text x={34} y={60} fontSize={9.5}>
              <tspan fill={C.ink}>reviewCount</tspan>
              <tspan fill={C.navLabel}>{": "}</tspan>
              <tspan fill={C.ink}>Int</tspan>
              <tspan fill={C.navLabel}>!</tspan>
            </text>
            <rect
              x={270}
              y={51}
              width={32}
              height={12}
              rx={6}
              fill="none"
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={286}
              y={59.5}
              textAnchor="middle"
              fontSize={6}
              letterSpacing="0.06em"
              fill={C.inkDim}
            >
              SAFE
            </text>

            {/* L3: added deprecation, classified SAFE. */}
            <text
              x={15}
              y={78}
              textAnchor="middle"
              fontSize={9.5}
              fill={C.navLabel}
            >
              +
            </text>
            <text x={34} y={78} fontSize={9.5}>
              <tspan fill={C.ink}>price</tspan>
              <tspan fill={C.navLabel}>{": "}</tspan>
              <tspan fill={C.ink}>Money</tspan>
              <tspan fill={C.navLabel}>{"! "}</tspan>
              <tspan fill={C.inkDim}>@deprecated</tspan>
            </text>
            <rect
              x={270}
              y={69}
              width={32}
              height={12}
              rx={6}
              fill="none"
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={286}
              y={77.5}
              textAnchor="middle"
              fontSize={6}
              letterSpacing="0.06em"
              fill={C.inkDim}
            >
              SAFE
            </text>

            {/* L4: removed field, classified BREAKING (the one coral status hue). */}
            <text
              x={15}
              y={96}
              textAnchor="middle"
              fontSize={9.5}
              fill={C.navLabel}
            >
              &#8722;
            </text>
            <text x={34} y={96} fontSize={9.5}>
              <tspan fill={C.ink}>legacySku</tspan>
              <tspan fill={C.navLabel}>{": "}</tspan>
              <tspan fill={C.ink}>String</tspan>
            </text>
            <rect
              x={248}
              y={87}
              width={54}
              height={12}
              rx={6}
              fill={C.coral}
              fillOpacity={0.1}
              stroke={C.coral}
              strokeWidth={1}
            />
            <text
              x={275}
              y={95.5}
              textAnchor="middle"
              fontSize={6}
              letterSpacing="0.06em"
              fill={C.coral}
            >
              BREAKING
            </text>

            {/* L5: context, the type closing. */}
            <text x={24} y={114} fontSize={9.5} fill={C.navLabel}>
              {"}"}
            </text>

            {/* Signature status callout on legacySku: coral dot, tick, leader, label. */}
            <circle cx={32} cy={90} r={2.5} fill={C.coral} />
            <line
              x1={34}
              y1={99}
              x2={84}
              y2={99}
              stroke={C.coral}
              strokeWidth={2}
              strokeLinecap="round"
            />
            <line
              x1={59}
              y1={99}
              x2={59}
              y2={123}
              stroke={C.coral}
              strokeWidth={1}
              markerEnd={`url(#${ID}arrow)`}
            />
            <text
              x={59}
              y={138}
              textAnchor="middle"
              fontSize={7}
              letterSpacing="0.1em"
              fill={C.coral}
            >
              AT-RISK
            </text>
          </svg>
        </div>

        {/* Dashed caption: the breaking line gets a pinned resolve thread. */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            registry-bot pins a resolve thread on the breaking change
          </p>
        </div>
      </div>
    </div>
  );
}
