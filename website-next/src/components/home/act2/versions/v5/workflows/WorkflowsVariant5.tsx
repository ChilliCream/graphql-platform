interface WorkflowsVariant5Props {
  readonly className?: string;
}

/**
 * Workflow scene, v5 "Schematic Lines", concept #5: outbox to inbox handoff.
 *
 * Skeleton: two ring clusters bridged across a service boundary. On the left, a
 * transactional OUTBOX cluster of two solid grey rings (the already dispatched
 * PaymentCaptured / ReviewPublished messages) plus one front ring committed with
 * the write. On the right, an idempotent INBOX cluster of two dashed grey rings
 * (BasketCheckedOut / StockReserved, pending idempotent receipt) plus one front
 * dedup ring. Strip the teal and it reads as a quiet grey pair of message
 * clusters separated by a gap, the boundary between two services.
 *
 * The single teal thread is the only accent and the one route the headline names:
 * the in-flight OrderPlaced message crossing exactly once. It begins at the hollow
 * teal source ring (the committed outbox message), runs as one unbroken teal line
 * carrying its MessageId across the boundary, and terminates on the focal inbox
 * dedup ring (stroked teal) with a teal chevron and a solid teal landing dot.
 * Nothing else is teal; there is no status hue here.
 *
 * React Server Component: no hooks, no client APIs, settled final frame only. The
 * MessageId and event names are borrowed verbatim from the v2 sibling. Every svg
 * id is prefixed "v5-workflows-5-".
 */

const C = {
  surface: "#0c1322",
  ink: "#a1a3af",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-workflows-5-";

export function WorkflowsVariant5({ className }: WorkflowsVariant5Props) {
  // The two grey outbox messages already dispatched, and the two dashed inbox
  // messages still pending idempotent receipt. Only their centers vary.
  const outboxGrey: readonly number[] = [56, 104];
  const inboxPending: readonly number[] = [56, 104];

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

        {/* one committed message crossing the service boundary exactly once */}
        <svg
          width="100%"
          viewBox="0 0 280 150"
          fill="none"
          className="mt-4 block"
          style={{ fontFamily: C.mono }}
        >
          <defs>
            <marker
              id={`${ID}arrow-teal`}
              markerUnits="userSpaceOnUse"
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={C.accent}
                strokeWidth="1"
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
            </marker>
          </defs>

          {/* outbox cluster: two solid grey rings, already dispatched */}
          <g
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          >
            {outboxGrey.map((cy) => (
              <circle key={cy} cx="44" cy={cy} r="8" fill="none" />
            ))}
          </g>

          {/* inbox cluster: two dashed grey rings, pending idempotent receipt */}
          <g
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeDasharray="2 3"
            vectorEffect="non-scaling-stroke"
          >
            {inboxPending.map((cy) => (
              <circle key={cy} cx="236" cy={cy} r="8" fill="none" />
            ))}
          </g>

          {/* teal thread: the committed message carries its MessageId across */}
          <line
            x1="84"
            y1="80"
            x2="194"
            y2="80"
            stroke={C.accent}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}arrow-teal)`}
          />

          {/* hollow teal source ring: the committed outbox message in flight */}
          <circle
            cx="72"
            cy="80"
            r="11"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* focal teal dedup ring + solid teal terminal dot: processed once */}
          <circle
            cx="208"
            cy="80"
            r="11"
            fill={C.surface}
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="208" cy="80" r="2.5" fill={C.accent} />

          {/* sparse micro-labels: the two clusters and the MessageId it carries */}
          <text
            x="58"
            y="34"
            textAnchor="middle"
            fill={C.navLabel}
            fontSize="7"
            letterSpacing="0.1em"
          >
            OUTBOX
          </text>
          <text
            x="222"
            y="34"
            textAnchor="middle"
            fill={C.navLabel}
            fontSize="7"
            letterSpacing="0.1em"
          >
            INBOX
          </text>
          <text
            x="139"
            y="71"
            textAnchor="middle"
            fill={C.navLabel}
            fontSize="7"
            letterSpacing="0.08em"
          >
            MessageId
          </text>
          <text x="139" y="96" textAnchor="middle" fill={C.ink} fontSize="8.5">
            a4f1c2
          </text>
        </svg>

        {/* lone footer numeral: the exactly-once guarantee */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            1&times;
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            processed per MessageId
          </p>
        </div>
      </div>
    </div>
  );
}
