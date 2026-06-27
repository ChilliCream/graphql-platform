interface WorkflowsVariant5Props {
  readonly className?: string;
}

/**
 * v6 "Workflow" hook, variant 5: the atomic-commit / dedupe diptych.
 *
 * Bespoke, one-off illustration (no shared v6 theme). Two ledger boxes sit side
 * by side. On the left, an OUTBOX commits a database write row and an
 * `OrderPlaced` message row inside one teal bracket labeled "same transaction".
 * That single amber message is delivered twice across the gap: the first copy
 * lands on the inbox as `processed` and the lone clean message reaches a handler;
 * the second copy is stamped a faded, struck-through "duplicate - skipped".
 *
 * This is the only duplicate-rejection motif in the set: at-least-once delivery
 * in, exactly-once processing out. Rendered as the settled final frame, no
 * animation, so the screenshot is fully legible. cc-* dark palette only, status
 * colors carry meaning (amber message in flight, teal/green committed and
 * processed, dim grey rejected). Every svg id is prefixed "v6-workflows-5-".
 */

const C = {
  surface: "#0c1322",
  page: "#0b0f1a",
  border: "rgba(245, 241, 234, 0.12)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  healthy: "#34d399",
  amber: "#fbbf24",
} as const;

const MONO =
  "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', monospace";

export function WorkflowsVariant5({ className }: WorkflowsVariant5Props) {
  return (
    <div
      className={[
        "border-cc-card-border bg-cc-card-bg/60 mx-auto w-full max-w-[332px] rounded-2xl border p-5 backdrop-blur-sm select-none",
        className ?? "",
      ].join(" ")}
    >
      <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
        Mocha.Messaging
      </p>

      <svg
        className="mt-3 block w-full"
        viewBox="0 0 332 150"
        fill="none"
        role="img"
        aria-label="Outbox commits a database write and an OrderPlaced message in one transaction. The message is delivered twice to the inbox: the first copy is processed and reaches the handler, the duplicate is skipped."
      >
        <defs>
          <marker
            id="v6-workflows-5-amber"
            markerWidth="7"
            markerHeight="7"
            refX="6"
            refY="3.5"
            orient="auto"
            markerUnits="userSpaceOnUse"
          >
            <path d="M0 0 L7 3.5 L0 7 z" fill={C.amber} />
          </marker>
          <marker
            id="v6-workflows-5-teal"
            markerWidth="7"
            markerHeight="7"
            refX="6"
            refY="3.5"
            orient="auto"
            markerUnits="userSpaceOnUse"
          >
            <path d="M0 0 L7 3.5 L0 7 z" fill={C.accent} />
          </marker>
        </defs>

        {/* Outbox box. */}
        <rect
          x={4}
          y={16}
          width={108}
          height={112}
          rx={10}
          fill={C.surface}
          stroke={C.border}
        />
        <text
          x={16}
          y={33}
          fontFamily={MONO}
          fontSize={8.5}
          letterSpacing="0.12em"
          fill={C.navLabel}
        >
          OUTBOX
        </text>

        {/* "same transaction" bracket grouping the two committed rows. */}
        <path
          d="M14 42 L10 42 L10 100 L14 100"
          stroke="rgba(94, 234, 212, 0.5)"
          strokeWidth={1}
        />

        {/* Outbox row 1: the database write. */}
        <rect
          x={16}
          y={42}
          width={88}
          height={26}
          rx={5}
          fill={C.page}
          stroke={C.border}
        />
        <ellipse
          cx={26}
          cy={50}
          rx={4.5}
          ry={1.6}
          stroke={C.ink}
          strokeWidth={1}
        />
        <path d="M21.5 50 L21.5 59" stroke={C.ink} strokeWidth={1} />
        <path d="M30.5 50 L30.5 59" stroke={C.ink} strokeWidth={1} />
        <path
          d="M21.5 59 A4.5 1.6 0 0 0 30.5 59"
          stroke={C.ink}
          strokeWidth={1}
        />
        <text x={37} y={52} fontFamily={MONO} fontSize={9} fill={C.heading}>
          orders
        </text>
        <text
          x={37}
          y={62}
          fontFamily={MONO}
          fontSize={6.8}
          letterSpacing="0.04em"
          fill={C.navLabel}
        >
          table write
        </text>

        {/* Outbox row 2: the OrderPlaced message (the thing that travels). */}
        <rect
          x={16}
          y={74}
          width={88}
          height={26}
          rx={5}
          fill="rgba(251, 191, 36, 0.05)"
          stroke="rgba(251, 191, 36, 0.4)"
        />
        <rect
          x={19.5}
          y={83.5}
          width={9}
          height={6}
          rx={1}
          stroke={C.amber}
          strokeWidth={1}
        />
        <path d="M19.5 84 L24 87.5 L28.5 84" stroke={C.amber} strokeWidth={1} />
        <text x={36} y={85} fontFamily={MONO} fontSize={8.5} fill={C.heading}>
          OrderPlaced
        </text>
        <text
          x={36}
          y={95}
          fontFamily={MONO}
          fontSize={7}
          fill="rgba(251, 191, 36, 0.85)"
        >
          a4f1c2
        </text>

        {/* Outbox caption: the atomic guarantee. */}
        <text
          x={58}
          y={118}
          textAnchor="middle"
          fontFamily={MONO}
          fontSize={7.5}
          letterSpacing="0.04em"
          fill="rgba(94, 234, 212, 0.78)"
        >
          same transaction
        </text>

        {/* Two deliveries of the same message crossing the gap (at-least-once). */}
        <text
          x={130}
          y={32}
          textAnchor="middle"
          fontFamily={MONO}
          fontSize={8}
          fill={C.amber}
        >
          a4f1c2 x2
        </text>
        <line
          x1={112}
          y1={87}
          x2={147}
          y2={55}
          stroke={C.amber}
          strokeWidth={1}
          markerEnd="url(#v6-workflows-5-amber)"
        />
        <line
          x1={112}
          y1={87}
          x2={147}
          y2={87}
          stroke={C.amber}
          strokeWidth={1}
          markerEnd="url(#v6-workflows-5-amber)"
        />
        <circle cx={129} cy={73} r={1.8} fill={C.amber} />
        <circle cx={129} cy={87} r={1.8} fill={C.amber} />

        {/* Inbox box. */}
        <rect
          x={150}
          y={16}
          width={108}
          height={112}
          rx={10}
          fill={C.surface}
          stroke={C.border}
        />
        <text
          x={162}
          y={33}
          fontFamily={MONO}
          fontSize={8.5}
          letterSpacing="0.12em"
          fill={C.navLabel}
        >
          INBOX
        </text>

        {/* Inbox row 1: first copy, processed. */}
        <rect
          x={156}
          y={42}
          width={96}
          height={26}
          rx={5}
          fill="rgba(52, 211, 153, 0.05)"
          stroke="rgba(52, 211, 153, 0.4)"
        />
        <path
          d="M161.5 55 l2.4 2.6 l4.6 -5.4"
          stroke={C.healthy}
          strokeWidth={1.4}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <text x={176} y={52} fontFamily={MONO} fontSize={8.5} fill={C.heading}>
          OrderPlaced
        </text>
        <text x={176} y={62} fontFamily={MONO} fontSize={7} fill={C.healthy}>
          a4f1c2 processed
        </text>

        {/* Inbox row 2: second copy, deduped and skipped (faded, struck). */}
        <g opacity={0.85}>
          <rect
            x={156}
            y={74}
            width={96}
            height={26}
            rx={5}
            fill="rgba(245, 241, 234, 0.015)"
            stroke="rgba(245, 241, 234, 0.12)"
            strokeDasharray="3 3"
          />
          <path
            d="M162 84 L168 90 M168 84 L162 90"
            stroke={C.inkDim}
            strokeWidth={1.2}
            strokeLinecap="round"
          />
          <text x={176} y={85} fontFamily={MONO} fontSize={8.5} fill={C.inkDim}>
            OrderPlaced
          </text>
          <line
            x1={176}
            y1={82.5}
            x2={230}
            y2={82.5}
            stroke={C.inkDim}
            strokeWidth={0.8}
          />
          <text
            x={176}
            y={95}
            fontFamily={MONO}
            fontSize={6.8}
            letterSpacing="0.02em"
            fill={C.inkDim}
          >
            duplicate - skipped
          </text>
        </g>

        {/* Inbox caption: the dedup promise. */}
        <text
          x={204}
          y={118}
          textAnchor="middle"
          fontFamily={MONO}
          fontSize={7.5}
          letterSpacing="0.04em"
          fill={C.inkDim}
        >
          drops duplicates
        </text>

        {/* One clean message exits to the handler. */}
        <line
          x1={258}
          y1={55}
          x2={271}
          y2={55}
          stroke={C.accent}
          strokeWidth={1}
          markerEnd="url(#v6-workflows-5-teal)"
        />
        <rect
          x={274}
          y={35}
          width={54}
          height={40}
          rx={9}
          fill={C.surface}
          stroke="rgba(94, 234, 212, 0.5)"
        />
        <path d="M302 41 l-5 8 h3.4 l-1.4 7 6 -9 h-3.4 z" fill={C.accent} />
        <text
          x={301}
          y={69}
          textAnchor="middle"
          fontFamily={MONO}
          fontSize={7.5}
          fill={C.ink}
        >
          handler
        </text>
      </svg>

      <p className="text-cc-ink-dim mt-3 text-center font-mono text-[0.72rem] tracking-[0.04em]">
        Sent <span className="text-cc-accent">once</span>, processed{" "}
        <span className="text-cc-accent">once</span>.
      </p>
    </div>
  );
}
