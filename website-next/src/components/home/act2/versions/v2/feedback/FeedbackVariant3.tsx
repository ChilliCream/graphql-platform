interface FeedbackVariant3Props {
  readonly className?: string;
}

/**
 * Agentic coding, variant 3 - v2 "Flow Diagrams".
 *
 * Concept: grounding sources converge to MCP. Four existing server artifacts
 * (schema, published ops, client registry, skillz) feed INWARD via grey
 * connectors into a single `/graphql/mcp` core box; the core emits ONE tool-call
 * out to a coding agent terminal node. The single teal path is the traced route
 * the headline names: it starts at the active MCP core, runs along the emitted
 * tool-call connector, and settles on the grounded agent. Every source stays
 * cream/grey because it is merely present, not in-flight. MERGE/CONVERGE
 * topology. Built from the locked Chip/Box vocabulary on cc-surface; the curved
 * convergence connectors are 1px SVG strokes. Settled final frame, no motion.
 * Every svg id is prefixed `v2-feedback-3-`.
 */

/** The four existing artifacts that ground the agent, converging into the core. */
const SOURCES: readonly string[] = [
  "schema",
  "published ops",
  "client registry",
  "skillz",
] as const;

// Local cc-* palette, kept inline so this RSC pulls in no shared module.
const ACCENT = "#5eead4"; // cc-accent: the single traced path
const CONNECTOR = "rgba(245,241,234,0.16)"; // cc-ink-faint: grey connectors

export function FeedbackVariant3({ className }: FeedbackVariant3Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          grounding sources converge to mcp
        </p>

        {/* Convergence body: four source chips (left) -> core -> agent (right). */}
        <div className="mt-4 flex items-stretch gap-2">
          {/* left column: the four grounding sources */}
          <div className="flex flex-col justify-center gap-1.5">
            {SOURCES.map((label) => (
              <span
                key={label}
                className="border-cc-card-border text-cc-ink bg-cc-surface rounded-lg border px-2.5 py-1.5 text-center font-mono text-[0.65rem] whitespace-nowrap"
              >
                {label}
              </span>
            ))}
          </div>

          {/* connectors: four grey merge paths in, one teal tool-call path out */}
          <div className="relative w-14 shrink-0">
            <svg
              viewBox="0 0 56 132"
              width="100%"
              height="100%"
              preserveAspectRatio="none"
              className="absolute inset-0"
              style={{ display: "block" }}
            >
              <defs>
                {/* grey inward arrowhead */}
                <marker
                  id="v2-feedback-3-head-grey"
                  markerWidth="6"
                  markerHeight="6"
                  refX="5"
                  refY="3"
                  orient="auto"
                >
                  <path
                    d="M0.5 0.8 L5 3 L0.5 5.2"
                    fill="none"
                    stroke={CONNECTOR}
                    strokeWidth="1"
                  />
                </marker>
              </defs>

              {/* four grey merge connectors: source rows -> core (right edge) */}
              {[18, 50, 82, 114].map((y) => (
                <path
                  key={y}
                  d={`M0 ${y} H20 Q40 ${y} 40 66 V66`}
                  fill="none"
                  stroke={CONNECTOR}
                  strokeWidth="1"
                  markerEnd="url(#v2-feedback-3-head-grey)"
                />
              ))}
            </svg>
          </div>

          {/* right column: the core box and the agent terminal it grounds */}
          <div className="flex flex-1 flex-col justify-center gap-2">
            {/* MCP core: the active source node of the single teal path */}
            <div
              className="bg-cc-surface rounded-lg border px-3 py-2"
              style={{ borderColor: "rgba(94,234,212,0.6)" }}
            >
              <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.08em] uppercase">
                core
              </p>
              <p className="mt-0.5 font-mono text-xs" style={{ color: ACCENT }}>
                /graphql/mcp
              </p>
            </div>

            {/* teal tool-call connector: core -> grounded agent */}
            <div className="flex items-center gap-1.5 pl-1">
              <svg
                viewBox="0 0 40 8"
                width="40"
                height="8"
                style={{ display: "block" }}
              >
                <defs>
                  {/* teal outward arrowhead */}
                  <marker
                    id="v2-feedback-3-head-teal"
                    markerWidth="6"
                    markerHeight="6"
                    refX="5"
                    refY="3"
                    orient="auto"
                  >
                    <path
                      d="M0.5 0.8 L5 3 L0.5 5.2"
                      fill="none"
                      stroke={ACCENT}
                      strokeWidth="1"
                    />
                  </marker>
                </defs>
                <line
                  x1="0"
                  y1="4"
                  x2="33"
                  y2="4"
                  stroke={ACCENT}
                  strokeWidth="1"
                  markerEnd="url(#v2-feedback-3-head-teal)"
                />
              </svg>
              <span
                className="font-mono text-[0.55rem] tracking-[0.08em] uppercase"
                style={{ color: ACCENT }}
              >
                tool call
              </span>
            </div>

            {/* terminal: the grounded coding agent (derived, rounded-md) */}
            <span
              className="bg-cc-surface rounded-md border px-2.5 py-1 text-center font-mono text-[0.65rem] whitespace-nowrap"
              style={{ borderColor: "rgba(94,234,212,0.6)", color: ACCENT }}
            >
              coding agent
            </span>
          </div>
        </div>

        {/* caption: the relationship the diagram encodes */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            existing artifacts ground the agent
          </p>
        </div>
      </div>
    </div>
  );
}
