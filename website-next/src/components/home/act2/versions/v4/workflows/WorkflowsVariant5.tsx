interface WorkflowsVariant5Props {
  readonly className?: string;
}

/**
 * Workflow scene, v4 "Generated Artifacts", concept #5: outbox to inbox.
 *
 * Pattern B (two cc-surface artifact tiles + one teal connector bridging a source
 * token to where it lands). The top tile is the transactional OUTBOX (events
 * committed alongside the write); the bottom tile is the idempotent INBOX (each
 * MessageId processed at most once). One message is mid-transit: `a4f1c2`
 * `OrderPlaced` sits as the bottom row of the outbox (committed) and again as the
 * top row of the inbox, where the same MessageId becomes the dedup key.
 *
 * The single teal callout is the only teal in the cell and doubles as the Pattern B
 * connector: a 2.5px anchor dot on the outbox MessageId, a 1px curved leader with a
 * chevron landing on the load-bearing inbox token `a4f1c2`, a 2px underline tick
 * beneath it, and an "EXACTLY ONCE" micro-label. Strip the teal and both tiles read as
 * neutral monochrome mono ledger rows; tile borders, dividers, and titles stay grey.
 *
 * Literal content (Mocha.Messaging outbox/inbox rows, MessageId prefixes, event
 * types, statuses, and the exactly-once guarantee) is borrowed verbatim from the v1
 * sibling. React Server Component: no "use client", no hooks, no animation, settled
 * final frame. Every svg id is prefixed "v4-workflows-5-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v4-workflows-5-";

export function WorkflowsVariant5({ className }: WorkflowsVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          outbox &rarr; inbox
        </p>

        {/* outbox tile -> inbox tile, one teal connector on the shared MessageId */}
        <svg
          viewBox="0 0 320 162"
          width="100%"
          role="img"
          aria-label="A transactional outbox tile committing the message a4f1c2 OrderPlaced, bridged by its MessageId to an idempotent inbox tile where the same id is the dedup key, for exactly-once handoff."
          className="mt-4"
          style={{ display: "block", fontFamily: MONO }}
        >
          <defs>
            <marker
              id={`${ID}arrow`}
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
          </defs>

          {/* OUTBOX tile (grey): the transactional outbox ledger */}
          <rect
            x={10}
            y={4}
            width={300}
            height={60}
            rx={8}
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth="1"
          />
          <text x={22} y={19} fill={C.inkDim} fontSize="9.5" fontWeight={600}>
            Outbox
          </text>
          <text
            x={298}
            y={19}
            textAnchor="end"
            fill={C.navLabel}
            fontSize="7.5"
            letterSpacing="0.08em"
          >
            committed
          </text>
          <line
            x1={10}
            y1={28}
            x2={310}
            y2={28}
            stroke={C.cardBorder}
            strokeWidth="1"
          />
          {/* row 1: a settled committed event */}
          <text x={22} y={44} fill={C.ink} fontSize="9">
            91e0bd
          </text>
          <text x={66} y={44} fill={C.navLabel} fontSize="9">
            PaymentCaptured · dispatched
          </text>
          {/* row 2: the in-flight message, committed in the same write (source) */}
          <text x={22} y={58} fill={C.ink} fontSize="9">
            a4f1c2
          </text>
          <text x={70} y={58} fill={C.navLabel} fontSize="9">
            OrderPlaced · committed
          </text>

          {/* INBOX tile (grey): the idempotent inbox ledger */}
          <rect
            x={10}
            y={94}
            width={300}
            height={64}
            rx={8}
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth="1"
          />
          <text x={22} y={109} fill={C.inkDim} fontSize="9.5" fontWeight={600}>
            Inbox
          </text>
          <text
            x={298}
            y={109}
            textAnchor="end"
            fill={C.navLabel}
            fontSize="7.5"
            letterSpacing="0.08em"
          >
            deduped
          </text>
          <line
            x1={10}
            y1={118}
            x2={310}
            y2={118}
            stroke={C.cardBorder}
            strokeWidth="1"
          />
          {/* row 1: where the message lands; the same MessageId is the dedup key */}
          <text x={22} y={133} fill={C.navLabel} fontSize="9">
            OrderPlaced
          </text>
          <text
            x={110}
            y={133}
            fill={C.accent}
            fontSize="11"
            fontWeight={600}
            style={{ fontVariantNumeric: "tabular-nums" }}
          >
            a4f1c2
          </text>
          {/* row 2: a settled processed event */}
          <text x={22} y={148} fill={C.ink} fontSize="9">
            62b7f4
          </text>
          <text x={66} y={148} fill={C.navLabel} fontSize="9">
            BasketCheckedOut · processed
          </text>

          {/* single teal callout (only teal): outbox MessageId dot -> 1px curved
              leader -> the inbox dedup-key token, with underline tick + label */}
          <circle cx={60} cy={54.5} r="2.5" fill={C.accent} />
          <path
            d="M60 54.5 C 60 92, 100 110, 110 124"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow)`}
          />
          <line
            x1={110}
            y1={138}
            x2={152}
            y2={138}
            stroke={C.accent}
            strokeWidth="2"
          />
          <text
            x={168}
            y={135}
            fill={C.accent}
            fontSize="7"
            letterSpacing="0.08em"
          >
            EXACTLY ONCE
          </text>
        </svg>

        {/* Dashed caption: the honest guarantee carried by the shared MessageId */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            exactly-once handoff between services
          </p>
        </div>
      </div>
    </div>
  );
}
