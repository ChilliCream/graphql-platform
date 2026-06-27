interface WorkflowsVariant5Props {
  readonly className?: string;
}

/**
 * Workflow scene, v2 "Flow Diagrams", variant 5: outbox-to-inbox handoff.
 *
 * Left-to-right handoff topology in the locked cc-* flow-diagram system. A
 * transactional OUTBOX box on the left holds two committed messages; an
 * idempotent INBOX box on the right has processed them. One message is mid
 * transit: a single row that spans both boxes across a dashed 1px async hop,
 * carrying its MessageId (a4f1c2 OrderPlaced) as the inbox dedup key. That
 * crossing is the single teal path: the in-flight message chip and its dashed
 * connector are teal, the dedup endpoint chip closes the same path. Every
 * settled row, both box frames, and the seam stay cream label / grey ink on the
 * cc-surface fill. A Stat duo footer carries the two key numbers (dedup
 * window, duplicate deliveries dropped).
 *
 * React Server Component: no hooks, no client APIs, settled final frame only.
 * All svg ids are prefixed "v2-workflows-5-".
 */

const CC = {
  surface: "#0c1322",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  inkFaint: "rgba(245,241,234,0.16)",
  cardBorder: "rgba(245,241,234,0.12)",
  navLabel: "#62748e",
  accent: "#5eead4",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v2-workflows-5-";

interface SettledRow {
  /** Short MessageId (Guid prefix), the shared dedup key. */
  readonly id: string;
  /** Domain event type carried by the message. */
  readonly type: string;
}

/** Transactional outbox: events committed in the same write, then dispatched. */
const OUTBOX: readonly SettledRow[] = [
  { id: "91e0bd", type: "PaymentCaptured" },
  { id: "7c33a8", type: "ReviewPublished" },
];

/** Idempotent inbox: each MessageId processed at most once. */
const INBOX: readonly SettledRow[] = [
  { id: "62b7f4", type: "BasketCheckedOut" },
  { id: "5d0a19", type: "StockReserved" },
];

/** The single message in flight, carrying its MessageId across the seam. */
const IN_FLIGHT = { id: "a4f1c2", type: "OrderPlaced" } as const;

// Box geometry on the 320x150 canvas. Two boxes share width and top so the
// handoff reads as one left-to-right flow across the seam between them.
const BOX_W = 130;
const BOX_H = 92;
const BOX_Y = 6;
const OUT_X = 6;
const IN_X = 184;
const SEAM_MID = (OUT_X + BOX_W + IN_X) / 2;

// Settled rows inside each box, and the single in-flight row spanning the seam.
const ROW_H = 20;
const ROW0_Y = BOX_Y + 26;
const ROW_PITCH = 24;
const FLOW_Y = BOX_Y + BOX_H - 18;

export function WorkflowsVariant5({ className }: WorkflowsVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          outbox &rarr; inbox
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 320 150"
            width="100%"
            role="img"
            aria-label="A transactional outbox on the left hands off to an idempotent inbox on the right; one OrderPlaced message is in flight as a single row spanning both boxes across a dashed async hop, carrying its MessageId as the inbox dedup key for exactly-once delivery."
            style={{ display: "block" }}
          >
            <defs>
              <marker
                id={`${ID}arrowTeal`}
                markerWidth="7"
                markerHeight="7"
                refX="5"
                refY="3"
                orient="auto"
              >
                <path
                  d="M0 0 L5 3 L0 6"
                  fill="none"
                  stroke={CC.accent}
                  strokeWidth={1}
                />
              </marker>
            </defs>

            {/* Outbox and inbox frame boxes (grey, merely present). */}
            <FrameBox x={OUT_X} kind="transactional" title="OUTBOX" />
            <FrameBox x={IN_X} kind="idempotent" title="INBOX" />

            {/* Settled rows inside each box: committed on the left, processed on
                the right. All grey, none in flight. */}
            {OUTBOX.map((row, i) => (
              <SettledRowChips
                key={row.id}
                x={OUT_X}
                y={ROW0_Y + i * ROW_PITCH}
                row={row}
                status="dispatched"
              />
            ))}
            {INBOX.map((row, i) => (
              <SettledRowChips
                key={row.id}
                x={IN_X}
                y={ROW0_Y + i * ROW_PITCH}
                row={row}
                status="processed"
              />
            ))}

            {/* The single in-flight message: one teal row spanning both boxes
                across a dashed 1px async hop. This is the one traced path. */}
            <g>
              {/* Outbox side of the in-flight row (committed, active teal). */}
              <rect
                x={OUT_X + 8}
                y={FLOW_Y - ROW_H / 2}
                width={BOX_W - 16}
                height={ROW_H}
                rx="6"
                fill={CC.surface}
                stroke={CC.accent}
                strokeWidth={1}
              />
              <text
                x={OUT_X + 18}
                y={FLOW_Y + 3.5}
                fill={CC.accent}
                fontFamily={MONO}
                fontSize={9}
              >
                {IN_FLIGHT.id}
              </text>
              <text
                x={OUT_X + BOX_W - 16}
                y={FLOW_Y + 3.5}
                fill={CC.accent}
                fillOpacity={0.7}
                fontFamily={MONO}
                fontSize={7}
                letterSpacing="0.06em"
                textAnchor="end"
              >
                COMMITTED
              </text>

              {/* Dashed deferred hop carrying the MessageId across the seam. */}
              <line
                x1={OUT_X + BOX_W - 4}
                y1={FLOW_Y}
                x2={IN_X + 1}
                y2={FLOW_Y}
                stroke={CC.accent}
                strokeWidth={1}
                strokeDasharray="3 3"
                markerEnd={`url(#${ID}arrowTeal)`}
              />
              <text
                x={SEAM_MID}
                y={FLOW_Y - 8}
                fill={CC.accent}
                fontFamily={MONO}
                fontSize={6.5}
                letterSpacing="0.08em"
                textAnchor="middle"
              >
                MessageId
              </text>

              {/* Inbox side of the in-flight row (same MessageId, deduped). The
                  terminus closes the single teal path, smaller derived radius. */}
              <rect
                x={IN_X + 8}
                y={FLOW_Y - ROW_H / 2}
                width={BOX_W - 16}
                height={ROW_H}
                rx="5"
                fill={CC.surface}
                stroke={CC.accent}
                strokeWidth={1}
              />
              <text
                x={IN_X + 18}
                y={FLOW_Y + 3.5}
                fill={CC.accent}
                fontFamily={MONO}
                fontSize={9}
              >
                {IN_FLIGHT.id}
              </text>
              <text
                x={IN_X + BOX_W - 16}
                y={FLOW_Y + 3.5}
                fill={CC.accent}
                fillOpacity={0.7}
                fontFamily={MONO}
                fontSize={7}
                letterSpacing="0.06em"
                textAnchor="end"
              >
                DEDUP
              </text>
            </g>
          </svg>
        </div>

        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              1&times;
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">
              processed per MessageId
            </p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              0
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">
              duplicate deliveries
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

interface FrameBoxProps {
  readonly x: number;
  readonly title: string;
  readonly kind: string;
}

/** One ledger box: a 1px grey frame with an eyebrow title and dimmed subtitle. */
function FrameBox({ x, title, kind }: FrameBoxProps) {
  return (
    <g>
      <rect
        x={x}
        y={BOX_Y}
        width={BOX_W}
        height={BOX_H}
        rx="8"
        fill={CC.surface}
        stroke={CC.cardBorder}
        strokeWidth={1}
      />
      <text
        x={x + 10}
        y={BOX_Y + 14}
        fill={CC.navLabel}
        fontFamily={MONO}
        fontSize={7}
        letterSpacing="0.1em"
      >
        {title}
      </text>
      <text
        x={x + BOX_W - 10}
        y={BOX_Y + 14}
        fill={CC.inkDim}
        fontFamily={MONO}
        fontSize={6.5}
        letterSpacing="0.06em"
        textAnchor="end"
      >
        {kind}
      </text>
    </g>
  );
}

interface SettledRowChipsProps {
  readonly x: number;
  readonly y: number;
  readonly row: SettledRow;
  readonly status: string;
}

/** One settled ledger row: MessageId, event type, and grey status, all present
 *  but not in flight. */
function SettledRowChips({ x, y, row, status }: SettledRowChipsProps) {
  return (
    <g>
      <rect
        x={x + 8}
        y={y - ROW_H / 2}
        width={BOX_W - 16}
        height={ROW_H}
        rx="6"
        fill={CC.surface}
        stroke={CC.cardBorder}
        strokeWidth={1}
      />
      <text x={x + 18} y={y + 3.5} fill={CC.ink} fontFamily={MONO} fontSize={9}>
        {row.id}
      </text>
      <text
        x={x + BOX_W - 16}
        y={y + 3.5}
        fill={CC.inkDim}
        fontFamily={MONO}
        fontSize={6.5}
        letterSpacing="0.04em"
        textAnchor="end"
      >
        {status}
      </text>
    </g>
  );
}
