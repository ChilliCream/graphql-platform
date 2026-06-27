interface WorkflowsVariant3Props {
  readonly className?: string;
}

/**
 * Workflow scene, variant 3 ("Pluggable transport swap") as a v2 flow diagram.
 *
 * One PublishAsync call sits at the top. A single teal connector drops into the
 * swappable slot, a row of five interchangeable transport chips. RabbitMQ is the
 * active selection, so the one teal path runs call -> RabbitMQ -> the in-flight
 * message arriving at the consumer. The other four transports stay grey to read
 * as available-but-not-selected: same call, swap the chip.
 *
 * React Server Component, no client behavior, settled final frame only. The
 * cc-* palette is inlined as local constants. All svg ids are prefixed
 * "v2-workflows-3-".
 */

const CC = {
  surface: "#0c1322",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  accent: "#5eead4",
  accentBorder: "rgba(94,234,212,0.6)",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, monospace';

// Five interchangeable transports behind one PublishAsync call. RabbitMQ is the
// active selection that carries the in-flight message.
const TRANSPORTS: readonly {
  readonly name: string;
  readonly active: boolean;
}[] = [
  { name: "RabbitMQ", active: true },
  { name: "Postgres", active: false },
  { name: "in-process", active: false },
  { name: "Kafka", active: false },
  { name: "Azure SB", active: false },
];

function Chip({
  label,
  active = false,
  derived = false,
}: {
  readonly label: string;
  readonly active?: boolean;
  readonly derived?: boolean;
}) {
  return (
    <span
      style={{
        display: "inline-block",
        borderRadius: derived ? 6 : 8,
        border: `1px solid ${active ? CC.accentBorder : CC.cardBorder}`,
        background: CC.surface,
        padding: derived ? "5px 8px" : "6px 10px",
        fontFamily: MONO,
        fontSize: "0.65rem",
        lineHeight: 1,
        whiteSpace: "nowrap",
        color: active ? CC.accent : CC.ink,
      }}
    >
      {label}
    </span>
  );
}

function TealDrop() {
  return (
    <div style={{ display: "flex", justifyContent: "center" }}>
      <svg
        width="14"
        height="20"
        viewBox="0 0 14 20"
        fill="none"
        aria-hidden="true"
        style={{ display: "block" }}
      >
        <path
          d="M7 0 L7 16"
          stroke={CC.accent}
          strokeWidth="1"
          markerEnd="url(#v2-workflows-3-head)"
        />
      </svg>
    </div>
  );
}

export function WorkflowsVariant3({ className }: WorkflowsVariant3Props) {
  return (
    <div
      aria-hidden="true"
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      {/* shared arrowhead marker for the single traced teal path */}
      <svg
        width="0"
        height="0"
        aria-hidden="true"
        style={{ position: "absolute" }}
      >
        <defs>
          <marker
            id="v2-workflows-3-head"
            markerWidth="7"
            markerHeight="7"
            refX="3.4"
            refY="3"
            orient="auto"
          >
            <path
              d="M0.5 0.5 L5.5 3 L0.5 5.5"
              fill="none"
              stroke={CC.accent}
              strokeWidth="1"
            />
          </marker>
        </defs>
      </svg>

      <div
        style={{
          borderRadius: 16,
          border: `1px solid ${CC.cardBorder}`,
          background: "rgba(12,19,34,0.55)",
          backdropFilter: "blur(4px)",
          padding: 20,
        }}
      >
        <p
          style={{
            fontFamily: MONO,
            fontSize: "0.58rem",
            letterSpacing: "0.15em",
            textTransform: "uppercase",
            color: CC.navLabel,
            margin: 0,
          }}
        >
          pluggable transport
        </p>

        {/* the one call at the top of the flow */}
        <div
          style={{
            marginTop: 16,
            display: "flex",
            justifyContent: "center",
          }}
        >
          <Chip label="PublishAsync(evt)" active />
        </div>

        {/* the single teal path drops into the swappable slot */}
        <div style={{ marginTop: 4 }}>
          <TealDrop />
        </div>

        {/* swappable slot: five transports, RabbitMQ selected */}
        <div
          style={{
            display: "flex",
            flexWrap: "wrap",
            justifyContent: "center",
            gap: 6,
          }}
        >
          {TRANSPORTS.map((t) => (
            <Chip key={t.name} label={t.name} active={t.active} />
          ))}
        </div>

        {/* the in-flight message leaves the selected transport */}
        <div style={{ marginTop: 4 }}>
          <TealDrop />
        </div>

        {/* terminus: the in-flight message arrives at the consumer */}
        <div
          style={{
            display: "flex",
            justifyContent: "center",
          }}
        >
          <Chip label="ReviewPublished -> consumer" derived active />
        </div>

        <div
          style={{
            marginTop: 16,
            borderTop: `1px dashed ${CC.inkFaint}`,
            paddingTop: 12,
          }}
        >
          <p
            style={{
              textAlign: "center",
              fontSize: "0.75rem",
              color: CC.inkDim,
              margin: 0,
            }}
          >
            swap transport, same publish
          </p>
        </div>
      </div>
    </div>
  );
}
