/**
 * "Workflow" scene, concept 3 ("Pluggable transport swap"), v3 "Signal &
 * Metrics" (strict cc-* dark panel).
 *
 * Leads with the measurable result: one PublishAsync call runs over five
 * interchangeable transports, so the hero is a single large cream "5" over the
 * lowercase mono caption "swappable transports", with a "one PublishAsync call"
 * sub-line tying the count back to the single call. Archetype A (number-lead,
 * centered), which varies from the node-chain neighbor (concept 2) and the
 * dual-stat fork neighbor (concept 4).
 *
 * The demoted topology under the number is a transport selector: the five
 * transports as a vertical pick-list (the v2 chip set, re-expressed as rows).
 * RabbitMQ is the live selection and is the ONE teal accent, lit with a teal
 * left bar and teal node, with a faint "-> consumer" reading to mark the
 * in-flight message it is carrying. The other four transports stay grey to read
 * as available-but-not-selected: same call, swap the row. There is no status
 * hue; choosing a transport is a choice, not a health state.
 *
 * Content is faithful to the v2 sibling: PublishAsync over RabbitMQ (selected),
 * Postgres, in-process, Kafka, and Azure SB, publishing ReviewPublished to the
 * consumer. Only the visual language is the locked v3 metrics panel.
 *
 * Static settled frame: a React Server Component, no hooks, no motion, no "use
 * client". aria-hidden root. Local cc palette object with the exact cc-* hex;
 * teal is the only decorative hue and is bound to the one selected transport.
 * Any svg id (none are needed here) would be prefixed "v3-workflows-3-".
 */

interface WorkflowsVariant3Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the
 * only decorative hue and is bound to the one selected transport; no status hue,
 * because choosing a transport is not a state to flag. */
const cc = {
  surface: "#0c1322",
  cardBg: "rgba(12,19,34,0.55)",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  accentSoft: "rgba(94,234,212,0.08)",
  MONO: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
  HEADING: '"Josefin Sans", Futura, sans-serif',
} as const;

/* Five interchangeable transports behind one PublishAsync call. RabbitMQ is the
 * active selection carrying the in-flight ReviewPublished message. */
const TRANSPORTS: readonly {
  readonly name: string;
  readonly selected: boolean;
}[] = [
  { name: "RabbitMQ", selected: true },
  { name: "Postgres", selected: false },
  { name: "in-process", selected: false },
  { name: "Kafka", selected: false },
  { name: "Azure SB", selected: false },
];

export function WorkflowsVariant3({ className }: WorkflowsVariant3Props) {
  return (
    <div
      aria-hidden="true"
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div
        className="rounded-2xl border p-5 backdrop-blur-sm"
        style={{ borderColor: cc.cardBorder, backgroundColor: cc.cardBg }}
      >
        {/* Eyebrow: names the view; the right tag names the published message. */}
        <div className="flex items-baseline justify-between gap-3">
          <p
            style={{
              fontFamily: cc.MONO,
              fontSize: "0.58rem",
              letterSpacing: "0.15em",
              textTransform: "uppercase",
              color: cc.navLabel,
            }}
          >
            pluggable transport
          </p>
          <span
            className="shrink-0"
            style={{
              fontFamily: cc.MONO,
              fontSize: "0.55rem",
              color: cc.navLabel,
            }}
          >
            ReviewPublished
          </span>
        </div>

        {/* Hero: one honest figure - five swappable transports behind one call.
            The numeral is the brightest thing; teal lives only in the selector. */}
        <div className="mt-3 text-center">
          <p
            style={{
              fontFamily: cc.HEADING,
              fontWeight: 600,
              fontSize: "2.75rem",
              lineHeight: 1,
              color: cc.heading,
            }}
          >
            5
          </p>
          <p
            className="mt-2"
            style={{
              fontFamily: cc.MONO,
              fontSize: "0.7rem",
              color: cc.inkDim,
              textTransform: "lowercase",
            }}
          >
            swappable transports
          </p>
          <p
            className="mt-1"
            style={{
              fontFamily: cc.MONO,
              fontSize: "0.58rem",
              color: cc.navLabel,
              textTransform: "lowercase",
            }}
          >
            one PublishAsync call
          </p>
        </div>

        {/* The transport selector: five interchangeable transports as a pick-list.
            RabbitMQ is the single teal selection carrying the in-flight message;
            the other four stay grey as available-but-not-selected. */}
        <div className="mt-4 flex flex-col gap-0.5">
          {TRANSPORTS.map((t) => (
            <div
              key={t.name}
              className="flex items-center gap-2 py-1"
              style={{
                borderLeft: `2px solid ${t.selected ? cc.accent : "transparent"}`,
                paddingLeft: 8,
                borderRadius: t.selected ? 4 : 0,
                backgroundColor: t.selected ? cc.accentSoft : "transparent",
              }}
            >
              <span
                aria-hidden="true"
                className="rounded-full"
                style={{
                  width: 6,
                  height: 6,
                  flex: "0 0 auto",
                  background: t.selected ? cc.accent : cc.surface,
                  border: t.selected ? undefined : `1px solid ${cc.inkFaint}`,
                }}
              />
              <span
                style={{
                  fontFamily: cc.MONO,
                  fontSize: "0.62rem",
                  fontWeight: t.selected ? 600 : 400,
                  color: t.selected ? cc.heading : cc.ink,
                  whiteSpace: "nowrap",
                }}
              >
                {t.name}
              </span>
              <span style={{ flex: 1 }} />
              {t.selected ? (
                <span
                  style={{
                    fontFamily: cc.MONO,
                    fontSize: "0.5rem",
                    color: cc.navLabel,
                    whiteSpace: "nowrap",
                  }}
                >
                  &rarr; consumer
                </span>
              ) : null}
            </div>
          ))}
        </div>

        {/* Interpretation line under a dashed divider: the swap is config, not a
            rewrite. No teal, no status hue. */}
        <div
          className="mt-4 border-t border-dashed pt-3"
          style={{ borderColor: cc.inkFaint }}
        >
          <p
            style={{
              fontFamily: cc.MONO,
              fontSize: "0.62rem",
              color: cc.inkDim,
            }}
          >
            swap the transport, the call never changes
          </p>
        </div>
      </div>
    </div>
  );
}
