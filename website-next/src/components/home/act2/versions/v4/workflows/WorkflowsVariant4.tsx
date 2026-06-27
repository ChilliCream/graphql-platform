interface WorkflowsVariant4Props {
  readonly className?: string;
}

/**
 * Workflow scene, v4 "Generated Artifacts", variant 4: "Mediator vs bus, one
 * wiring" (locked v4 PATTERN B, two artifact tiles tied by one teal callout).
 *
 * Two cc-surface code tiles render the same Mocha model dispatched two ways. The
 * top tile is the in-process path: `mediator.SendAsync(new CreateReview(id))`
 * handled in-process by `CreateReviewHandler`. The bottom tile is the
 * cross-service path: `bus.PublishAsync(new ReviewPublished(id))` consumed across
 * services by `ProductRatingProjection`. A small generated wiring node
 * (`Mocha.g.cs`) sits between them with two faint cc-ink ties, the "one wiring"
 * that registers both transports.
 *
 * The single teal callout is the only teal in the cell and marks the one
 * load-bearing token, the in-flight `ReviewPublished` event: a 3px teal anchor
 * dot at the token, a 2px teal underline tick beneath it, a 1px teal leader
 * dropping into the `ProductRatingProjection` consumer where it lands, and a 7px
 * uppercase "IN FLIGHT" micro-label. Strip the teal and both tiles read as
 * neutral mono code; the in-process command/handler relation stays a separate
 * neutral cc-nav-label connector and never collides with the teal token.
 *
 * Literal content (CreateReview, CreateReviewHandler, bus.PublishAsync,
 * ReviewPublished(id), ProductRatingProjection, in-process / across services) is
 * borrowed verbatim from the v1 / ScrollScenes Workflow siblings.
 *
 * React Server Component: no "use client", no hooks, no animation, settled final
 * frame. Every svg id is prefixed "v4-workflows-4-".
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
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v4-workflows-4-";

export function WorkflowsVariant4({ className }: WorkflowsVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          one wiring, two sends
        </p>

        <div className="mt-4">
          <svg
            viewBox="0 0 320 172"
            width="100%"
            role="img"
            aria-label="One generated Mocha wiring registers both an in-process mediator CreateReview command and a cross-service bus ReviewPublished publish; the in-flight ReviewPublished event is lit as it reaches its ProductRatingProjection consumer."
            style={{ display: "block" }}
          >
            <defs>
              {/* Open teal chevron for the in-flight callout leader. */}
              <marker
                id={`${ID}teal`}
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
              {/* Open neutral chevron for the in-process dispatch relation. */}
              <marker
                id={`${ID}faint`}
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
                  stroke={C.navLabel}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* ---- Tile A: in-process mediator command + handler ---- */}
            <rect
              x={6}
              y={2}
              width={308}
              height={62}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={16}
              y={17}
              fontFamily={MONO}
              fontSize={10}
              fontWeight={600}
              fill={C.heading}
            >
              Mediator
            </text>
            <text
              x={306}
              y={17}
              textAnchor="end"
              fontFamily={MONO}
              fontSize={8}
              letterSpacing="0.1em"
              fill={C.navLabel}
            >
              in-process
            </text>
            <line
              x1={6}
              y1={25}
              x2={314}
              y2={25}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* L1: the in-process dispatch call. */}
            <text x={14} y={43} fontFamily={MONO} fontSize={8.5}>
              <tspan fill={C.navLabel}>{"await "}</tspan>
              <tspan fill={C.ink}>mediator</tspan>
              <tspan fill={C.navLabel}>{"."}</tspan>
              <tspan fill={C.ink}>SendAsync</tspan>
              <tspan fill={C.navLabel}>{"(new "}</tspan>
              <tspan fill={C.ink}>CreateReview</tspan>
              <tspan fill={C.navLabel}>{"("}</tspan>
              <tspan fill={C.ink}>id</tspan>
              <tspan fill={C.navLabel}>{"));"}</tspan>
            </text>

            {/* L2: the in-process handler, indented under the command token. */}
            <text
              x={162}
              y={57}
              fontFamily={MONO}
              fontSize={8.5}
              fill={C.inkDim}
            >
              CreateReviewHandler
            </text>

            {/* Neutral in-process relation: command -> handler. */}
            <path
              d="M192 47 L192 53"
              fill="none"
              stroke={C.navLabel}
              strokeWidth={1}
              markerEnd={`url(#${ID}faint)`}
            />

            {/* ---- One generated wiring node tying both transports ---- */}
            <line
              x1={160}
              y1={64}
              x2={160}
              y2={76}
              stroke={C.inkFaint}
              strokeWidth={1}
            />
            <rect
              x={120}
              y={76}
              width={82}
              height={15}
              rx={6}
              fill={C.surface}
              stroke={C.inkFaint}
              strokeWidth={1}
            />
            <path
              d="M129 80 H134 L137 83 V89 H129 Z"
              fill="none"
              stroke={C.inkDim}
              strokeWidth={1}
              strokeLinejoin="round"
            />
            <path
              d="M134 80 V83 H137"
              fill="none"
              stroke={C.inkDim}
              strokeWidth={1}
              strokeLinejoin="round"
            />
            <text x={142} y={87} fontFamily={MONO} fontSize={8} fill={C.inkDim}>
              Mocha.g.cs
            </text>
            <line
              x1={160}
              y1={91}
              x2={160}
              y2={104}
              stroke={C.inkFaint}
              strokeWidth={1}
            />

            {/* ---- Tile B: cross-service bus publish + consumer ---- */}
            <rect
              x={6}
              y={104}
              width={308}
              height={64}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />
            <text
              x={16}
              y={119}
              fontFamily={MONO}
              fontSize={10}
              fontWeight={600}
              fill={C.heading}
            >
              Bus
            </text>
            <text
              x={306}
              y={119}
              textAnchor="end"
              fontFamily={MONO}
              fontSize={8}
              letterSpacing="0.1em"
              fill={C.navLabel}
            >
              across services
            </text>
            <line
              x1={6}
              y1={127}
              x2={314}
              y2={127}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* In-flight micro-label, tagged on the lit publish. */}
            <text
              x={306}
              y={140}
              textAnchor="end"
              fontFamily={MONO}
              fontSize={7}
              letterSpacing="0.12em"
              fill={C.accent}
            >
              IN FLIGHT
            </text>

            {/* L1: the cross-service publish; ReviewPublished is the lit token. */}
            <text x={14} y={148} fontFamily={MONO} fontSize={8.5}>
              <tspan fill={C.navLabel}>{"await "}</tspan>
              <tspan fill={C.ink}>bus</tspan>
              <tspan fill={C.navLabel}>{"."}</tspan>
              <tspan fill={C.ink}>PublishAsync</tspan>
              <tspan fill={C.navLabel}>{"(new "}</tspan>
              <tspan fill={C.accent} fontWeight={600}>
                ReviewPublished
              </tspan>
              <tspan fill={C.navLabel}>{"("}</tspan>
              <tspan fill={C.ink}>id</tspan>
              <tspan fill={C.navLabel}>{"));"}</tspan>
            </text>

            {/* L2: the consumer, indented under the published event token. */}
            <text x={152} y={162} fontFamily={MONO} fontSize={8.5} fill={C.ink}>
              ProductRatingProjection
            </text>

            {/* Signature teal callout on the in-flight ReviewPublished token. */}
            <circle cx={149} cy={144} r={3} fill={C.accent} />
            <line
              x1={152}
              y1={152}
              x2={228}
              y2={152}
              stroke={C.accent}
              strokeWidth={2}
              strokeLinecap="round"
            />
            <path
              d="M190 153 L190 158"
              fill="none"
              stroke={C.accent}
              strokeWidth={1}
              markerEnd={`url(#${ID}teal)`}
            />
          </svg>
        </div>

        {/* Dashed caption: one model, generated wiring, either transport. */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            same model, in-process or across services
          </p>
        </div>
      </div>
    </div>
  );
}
