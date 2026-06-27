/**
 * Workflow scene, concept 4 - "Mediator vs bus, one wiring", v3 "Signal &
 * Metrics" (strict cc-* dark).
 *
 * Leads with the measured result. The honest headline for "mediator vs bus, one
 * wiring" is the leverage: one source-generated wiring drives two dispatch
 * modes. So the hero is a cream "1 -> 2" delta pair (layout C), the left numeral
 * "1" captioned "generated wiring", the right numeral "2" captioned "dispatch
 * modes", with a muted arrow between them.
 *
 * The single teal signal is a single trunk splitting to two marks: the one
 * generated wiring forks into an in-process mediator.Send (grey context) and a
 * cross-service bus.Publish. Only the bus leg is teal, the in-flight publish
 * lit this frame (a traced branch with a travelling packet and an arrowhead into
 * its node). No status hue, one model running two ways is not a state to flag.
 *
 * Content is faithful to the v1/v2 Mocha pair: the same ReviewPublished message
 * is dispatched in-process via the mediator (mediator.Send, handled in the same
 * process) or across services via the bus (bus.Publish, consumed elsewhere), and
 * one source-generated wiring serves both. Only the visual language is the
 * locked v3 system.
 *
 * Static settled frame: a React Server Component, no hooks, no motion, no "use
 * client". aria-hidden root. Local cc palette object with the exact cc-* hex;
 * teal is the only decorative hue and is bound to the in-flight publish leg.
 * Every svg id (none are needed here) would be prefixed "v3-workflows-4-".
 */

interface WorkflowsVariant4Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the
 * only decorative hue and is bound to the single in-flight publish leg; no
 * status hue. */
const cc = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  MONO: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
  HEADING: '"Josefin Sans", Futura, sans-serif',
} as const;

export function WorkflowsVariant4({ className }: WorkflowsVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* Eyebrow: names the view, identical placement across the set. */}
        <p
          style={{
            fontFamily: cc.MONO,
            fontSize: "0.58rem",
            letterSpacing: "0.15em",
            textTransform: "uppercase",
            color: cc.navLabel,
          }}
        >
          mocha &middot; dispatch
        </p>

        {/* Hero "1 -> 2" delta: one generated wiring yields two dispatch modes.
            Both numerals are cream; the teal accent lives only in the proof
            diagram below. The arrow is muted grey, not a decorative hue. */}
        <div className="mt-3 flex items-start justify-center gap-4">
          <div className="text-center">
            <p
              style={{
                fontFamily: cc.HEADING,
                fontWeight: 600,
                fontSize: "2rem",
                lineHeight: 1,
                color: cc.heading,
              }}
            >
              1
            </p>
            <p
              className="mt-1.5"
              style={{
                fontFamily: cc.MONO,
                fontSize: "0.7rem",
                color: cc.inkDim,
                textTransform: "lowercase",
              }}
            >
              generated wiring
            </p>
          </div>

          <span
            aria-hidden="true"
            style={{
              fontFamily: cc.MONO,
              fontSize: "1.05rem",
              lineHeight: "2rem",
              color: cc.navLabel,
            }}
          >
            &rarr;
          </span>

          <div className="text-center">
            <p
              style={{
                fontFamily: cc.HEADING,
                fontWeight: 600,
                fontSize: "2rem",
                lineHeight: 1,
                color: cc.heading,
              }}
            >
              2
            </p>
            <p
              className="mt-1.5"
              style={{
                fontFamily: cc.MONO,
                fontSize: "0.7rem",
                color: cc.inkDim,
                textTransform: "lowercase",
              }}
            >
              dispatch modes
            </p>
          </div>
        </div>

        {/* Teal measurement: one trunk (the generated wiring) splitting to two
            marks. The in-process mediator leg is muted grey context; the
            cross-service bus leg is the single traced teal path, the in-flight
            publish lit this frame with a travelling packet and an arrowhead. */}
        <div
          className="mt-4 border-t pt-4"
          style={{ borderColor: cc.cardBorder }}
        >
          <svg
            viewBox="0 0 280 72"
            width="100%"
            role="img"
            aria-label="One generated Mocha wiring forks into two dispatch modes: an in-process mediator.Send leg in grey, and a cross-service bus.Publish leg traced teal as the in-flight publish."
            style={{ display: "block" }}
          >
            {/* the one generated wiring: the shared trunk origin */}
            <rect
              x="0.5"
              y="22"
              width="92"
              height="28"
              rx="7"
              fill={cc.surface}
              stroke={cc.cardBorder}
              strokeWidth="1"
            />
            <text
              x="11"
              y="35"
              fontFamily={cc.MONO}
              fontSize="7.5"
              letterSpacing="0.12em"
              fill={cc.navLabel}
            >
              GENERATED
            </text>
            <text
              x="11"
              y="45.5"
              fontFamily={cc.MONO}
              fontSize="9"
              fill={cc.ink}
            >
              Mocha wiring
            </text>

            {/* shared grey trunk + the in-process mediator leg (muted context) */}
            <path
              d="M92.5 36 H132"
              fill="none"
              stroke={cc.ink}
              strokeWidth="1"
              strokeOpacity="0.32"
            />
            <path
              d="M132 36 V18 H172"
              fill="none"
              stroke={cc.ink}
              strokeWidth="1"
              strokeOpacity="0.32"
              strokeLinejoin="round"
            />
            <circle cx="132" cy="36" r="1.8" fill={cc.ink} fillOpacity="0.5" />
            <path
              d="M168 14.5 L172 18 L168 21.5"
              fill="none"
              stroke={cc.ink}
              strokeWidth="1"
              strokeOpacity="0.32"
              strokeLinecap="round"
              strokeLinejoin="round"
            />

            {/* the single teal leg: the in-flight cross-service publish */}
            <path
              d="M132 36 V54 H172"
              fill="none"
              stroke={cc.accent}
              strokeWidth="1"
              strokeOpacity="0.85"
              strokeLinejoin="round"
            />
            {/* travelling publish packet on the live leg */}
            <circle cx="152" cy="54" r="2.5" fill={cc.accent} />
            {/* open chevron arrowhead into the bus node */}
            <path
              d="M168 50.5 L172 54 L168 57.5"
              fill="none"
              stroke={cc.accent}
              strokeWidth="1"
              strokeLinecap="round"
              strokeLinejoin="round"
            />

            {/* in-process mediator dispatch node (grey context) */}
            <rect
              x="172"
              y="4"
              width="107.5"
              height="28"
              rx="6"
              fill={cc.surface}
              stroke={cc.cardBorder}
              strokeWidth="1"
            />
            <text
              x="183"
              y="17"
              fontFamily={cc.MONO}
              fontSize="7.5"
              letterSpacing="0.12em"
              fill={cc.navLabel}
            >
              IN-PROCESS
            </text>
            <text
              x="183"
              y="27"
              fontFamily={cc.MONO}
              fontSize="9"
              fill={cc.ink}
            >
              mediator.Send
            </text>

            {/* cross-service bus dispatch node - the live publish this frame */}
            <rect
              x="172"
              y="40"
              width="107.5"
              height="28"
              rx="6"
              fill={cc.surface}
              stroke={cc.cardBorder}
              strokeWidth="1"
            />
            <text
              x="183"
              y="53"
              fontFamily={cc.MONO}
              fontSize="7.5"
              letterSpacing="0.12em"
              fill={cc.navLabel}
            >
              CROSS-SERVICE
            </text>
            <text
              x="183"
              y="63"
              fontFamily={cc.MONO}
              fontSize="9"
              fontWeight={600}
              fill={cc.heading}
            >
              bus.Publish
            </text>
          </svg>
        </div>

        {/* Interpretation line under a dashed divider: names the one shared
            message and grounds the "in-process or across services" claim. */}
        <div
          className="mt-4 border-t border-dashed pt-3"
          style={{ borderColor: cc.inkFaint }}
        >
          <p
            style={{
              fontFamily: cc.MONO,
              fontSize: "0.62rem",
              color: cc.ink,
            }}
          >
            same ReviewPublished, in-process or across services
          </p>
        </div>
      </div>
    </div>
  );
}
