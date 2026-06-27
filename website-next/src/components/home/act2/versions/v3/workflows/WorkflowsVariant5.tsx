/**
 * "Workflow" scene, concept 5 ("Outbox to inbox"), v3 "Signal & Metrics".
 *
 * Leads with the measured guarantee: a transactional outbox feeding an
 * idempotent inbox hands a message across so it is processed exactly once. The
 * hero is the cream "1x" numeral over the lowercase mono caption "processed
 * exactly once" (layout B, stat-top / signal-row). The single teal signal is one
 * bar-meter row spanning both sides: the OrderPlaced message leaves the outbox,
 * crosses the dashed handoff boundary at the centre, and lands in the inbox,
 * where the lone filled teal node marks the one processing. Its MessageId
 * (a4f1c2) rides above the bar as the key carried across.
 *
 * The one status hue is the healthy "committed" tag: the outbox record was
 * written in the same transaction as the business change, a genuine state, never
 * decoration. Teal stays the sole decorative accent, bound to the handed-off
 * message and its single node; the hero numeral stays cream.
 *
 * Content is faithful to the v1/v2 Mocha.Messaging pair: the outbox commits
 * events in the same write, the idempotent inbox dedupes on the shared MessageId
 * (a4f1c2), and the in-flight OrderPlaced is committed then deduped. Only the
 * visual language is the locked v3 system.
 *
 * Static settled frame: a React Server Component, no hooks, no motion, no "use
 * client". aria-hidden root. Local cc palette object with exact cc-* hex; teal is
 * the only decorative hue, healthy green is rationed to the committed status. All
 * svg ids are prefixed "v3-workflows-5-".
 */

interface WorkflowsVariant5Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the only
 * decorative hue and is bound to the handed-off message + its one node; healthy
 * green encodes the single genuine status (the outbox write committed). */
const cc = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  healthy: "#34d399",
  mono: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace',
  display: '"Josefin Sans", Futura, sans-serif',
} as const;

export function WorkflowsVariant5({ className }: WorkflowsVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow + the one status tag: the outbox write committed (healthy) */}
        <div className="flex items-center justify-between gap-3">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            outbox / inbox
          </p>
          <span
            className="inline-flex shrink-0 items-center gap-1.5 rounded-full border px-2 py-0.5 font-mono text-[0.5rem] tracking-[0.08em] uppercase"
            style={{ borderColor: `${cc.healthy}59`, color: cc.healthy }}
          >
            <span
              className="inline-block h-1.5 w-1.5 rounded-full"
              style={{ background: cc.healthy }}
            />
            committed
          </span>
        </div>

        {/* hero numeral (layout B): the message is processed exactly once */}
        <div className="mt-3">
          <p
            className="text-cc-heading leading-none font-semibold"
            style={{
              fontFamily: cc.display,
              fontSize: "2.75rem",
              fontVariantNumeric: "tabular-nums",
            }}
          >
            1x
          </p>
          <p
            className="text-cc-ink-dim mt-2 lowercase"
            style={{ fontFamily: cc.mono, fontSize: "0.7rem" }}
          >
            processed exactly once
          </p>
        </div>

        {/* the teal signal: one bar-meter row spanning both sides. The
            OrderPlaced message leaves the outbox, crosses the dashed handoff
            boundary at the centre, and lands in the inbox, where the single
            filled teal node marks the lone processing. Its MessageId rides above
            the bar as the key carried across. */}
        <div className="mt-4">
          <svg
            viewBox="0 0 288 44"
            width="100%"
            aria-hidden="true"
            style={{ display: "block" }}
          >
            {/* endpoint + message labels: OUTBOX | a4f1c2 | INBOX */}
            <text
              x="2"
              y="9"
              fontFamily={cc.mono}
              fontSize="6"
              letterSpacing="0.12em"
              fill={cc.navLabel}
            >
              OUTBOX
            </text>
            <text
              x="144"
              y="9"
              textAnchor="middle"
              fontFamily={cc.mono}
              fontSize="6.5"
              letterSpacing="0.04em"
              fill={cc.ink}
            >
              a4f1c2
            </text>
            <text
              x="286"
              y="9"
              textAnchor="end"
              fontFamily={cc.mono}
              fontSize="6"
              letterSpacing="0.12em"
              fill={cc.navLabel}
            >
              INBOX
            </text>

            {/* bar-meter track: cc-surface pill + 1px card border */}
            <rect
              x="1"
              y="22"
              width="286"
              height="8"
              rx="4"
              fill={cc.surface}
              stroke={cc.cardBorder}
              strokeWidth="1"
            />

            {/* the handed-off message: teal fill from the outbox into the inbox */}
            <rect
              x="1"
              y="22"
              width="246"
              height="8"
              rx="4"
              fill={cc.accent}
              fillOpacity="0.7"
            />

            {/* the outbox -> inbox handoff boundary (the one dashed divider) */}
            <line
              x1="144"
              x2="144"
              y1="16"
              y2="36"
              stroke={cc.inkFaint}
              strokeWidth="1"
              strokeDasharray="2 2"
            />

            {/* the single teal node: the processing landing in the inbox */}
            <circle cx="247" cy="26" r="4.2" fill={cc.surface} />
            <circle cx="247" cy="26" r="3" fill={cc.accent} />
          </svg>
        </div>

        {/* interpretation caption under a dashed divider: the inbox dedup key,
            the mechanism behind exactly-once */}
        <div className="border-cc-ink-faint mt-3.5 border-t border-dashed pt-3">
          <p
            style={{
              fontFamily: cc.mono,
              fontSize: "0.62rem",
              color: cc.inkDim,
            }}
          >
            <span style={{ color: cc.ink }}>OrderPlaced</span> &middot; deduped
            by MessageId at the inbox
          </p>
        </div>
      </div>
    </div>
  );
}
