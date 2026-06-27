interface FeedbackVariant3Props {
  readonly className?: string;
}

/**
 * Agentic-coding scene, variant 3 (v4 "Generated Artifacts"): grounding sources
 * converge to MCP.
 *
 * A top "grounding sources" registry tile lists the four existing artifacts that
 * ground the agent, each a monochrome row of source name plus the literal it
 * ships: schema -> `Product.price`, published ops -> `ProductCard`, client
 * registry -> `3 clients`, skillz -> `SKILL.md`. Four faint 1px grey leaders
 * funnel down into one `/graphql/mcp` core node, and a grey route emits below into
 * the coding-agent tile, which renders the single tool-call the agent receives:
 * `products { id name price }`.
 *
 * The one teal accent is the version signature, and it sits only on the
 * convergence core: the `/graphql/mcp` token is teal, a teal anchor dot on the
 * node carries a 1px leader into the open right margin, a 2px teal underline tick,
 * and a "CONVERGED" micro-label. The core border, both tile borders, the four
 * converge leaders, and the emit route all stay grey; cream lives only in the Stat
 * numerals. Strip the teal and the cell reads as a neutral registry, a neutral
 * hub, and a neutral GraphQL snippet. Literal content is borrowed verbatim from
 * the v1 / v2 siblings: the four sources, the `/graphql/mcp` endpoint, and the
 * `products` tool. React Server Component, settled final frame, no hooks, no
 * animation. Every svg id is prefixed "v4-feedback-3-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v4-feedback-3-";

/** The four existing artifacts that ground the agent, with the literal each ships. */
const SOURCES: readonly { readonly name: string; readonly artifact: string }[] =
  [
    { name: "schema", artifact: "Product.price" },
    { name: "published ops", artifact: "ProductCard" },
    { name: "client registry", artifact: "3 clients" },
    { name: "skillz", artifact: "SKILL.md" },
  ];

/** X origins of the four faint converge leaders along the source tile's lower edge. */
const FUNNEL_X: readonly number[] = [44, 112, 188, 256];

export function FeedbackVariant3({ className }: FeedbackVariant3Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* Panel eyebrow (ScrollScenes header voice). */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          sources &rarr; mcp
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 320 192"
            width="100%"
            role="img"
            aria-label="Four grounding artifacts (schema, published operations, client registry, skillz) converge into one /graphql/mcp core, which emits a single products tool-call to the coding agent."
            style={{ display: "block" }}
          >
            <defs>
              {/* Grey open chevron for the single emit route into the agent tile. */}
              <marker
                id={`${ID}arrow`}
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
                />
              </marker>
            </defs>

            {/* ---- Source tile: the grounding-sources registry ---- */}
            <rect
              x={8}
              y={2}
              width={304}
              height={80}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={20}
              y={15}
              fontFamily={C.mono}
              fontSize={9.5}
              fontWeight={600}
              fill={C.inkDim}
            >
              grounding sources
            </text>
            <text
              x={300}
              y={15}
              textAnchor="end"
              fontFamily={C.mono}
              fontSize={8}
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              4 artifacts
            </text>
            <line
              x1={8}
              y1={22}
              x2={312}
              y2={22}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* four registry rows: source name + the literal it ships */}
            {SOURCES.map((s, i) => {
              const baseline = 35 + i * 14.5;
              return (
                <g key={s.name}>
                  {i > 0 && (
                    <line
                      x1={16}
                      y1={baseline - 10}
                      x2={304}
                      y2={baseline - 10}
                      stroke={C.cardBorder}
                      strokeWidth={1}
                    />
                  )}
                  <circle cx={24} cy={baseline - 3} r={2.5} fill={C.navLabel} />
                  <text
                    x={34}
                    y={baseline}
                    fontFamily={C.mono}
                    fontSize={9}
                    fill={C.ink}
                  >
                    {s.name}
                  </text>
                  <text
                    x={300}
                    y={baseline}
                    textAnchor="end"
                    fontFamily={C.mono}
                    fontSize={8.5}
                    fill={C.inkDim}
                  >
                    {s.artifact}
                  </text>
                </g>
              );
            })}

            {/* ---- Four faint converge leaders funneling into the MCP core ---- */}
            {FUNNEL_X.map((x) => (
              <g key={`funnel-${x}`}>
                <circle cx={x} cy={82} r={1.4} fill={C.navLabel} />
                <line
                  x1={x}
                  y1={82}
                  x2={160}
                  y2={96}
                  stroke={C.inkFaint}
                  strokeWidth={1}
                />
              </g>
            ))}

            {/* ---- Core node: the converged /graphql/mcp hub (teal token only) ---- */}
            <rect
              x={110}
              y={96}
              width={100}
              height={22}
              rx={6}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={160}
              y={111}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={11}
              fontWeight={600}
              fill={C.accent}
            >
              /graphql/mcp
            </text>

            {/* ---- Emit route: the core hands one tool-call to the agent tile ---- */}
            <line
              x1={160}
              y1={118}
              x2={160}
              y2={130}
              stroke={C.inkFaint}
              strokeWidth={1}
              markerEnd={`url(#${ID}arrow)`}
            />

            {/* ---- Agent tile: the single emitted tool-call it receives ---- */}
            <rect
              x={8}
              y={132}
              width={304}
              height={58}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={20}
              y={145}
              fontFamily={C.mono}
              fontSize={9.5}
              fontWeight={600}
              fill={C.inkDim}
            >
              coding agent
            </text>
            <text
              x={300}
              y={145}
              textAnchor="end"
              fontFamily={C.mono}
              fontSize={8}
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              .mcp
            </text>
            <line
              x1={8}
              y1={152}
              x2={312}
              y2={152}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={160}
              y={172}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={9.5}
            >
              <tspan fill={C.inkDim}>&rarr; </tspan>
              <tspan fill={C.ink}>products </tspan>
              <tspan fill={C.navLabel}>{"{ "}</tspan>
              <tspan fill={C.ink}>id name price</tspan>
              <tspan fill={C.navLabel}>{" }"}</tspan>
            </text>

            {/* ---- Signature teal callout: node dot -> leader -> tick -> label ---- */}
            <circle cx={210} cy={107} r={2.5} fill={C.accent} />
            <path
              d="M210 107 H248"
              fill="none"
              stroke={C.accent}
              strokeWidth={1}
            />
            <line
              x1={249}
              y1={115}
              x2={297}
              y2={115}
              stroke={C.accent}
              strokeWidth={2}
            />
            <text
              x={249}
              y={111}
              fontFamily={C.mono}
              fontSize={7}
              letterSpacing="0.12em"
              fill={C.accent}
            >
              CONVERGED
            </text>
          </svg>
        </div>

        {/* Stat duo footer: the convergence, four artifacts to one tool-call. */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              4
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">grounding sources</p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              1
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">tool-call emitted</p>
          </div>
        </div>
      </div>
    </div>
  );
}
