interface FeedbackVariant2Props {
  readonly className?: string;
}

type Kind = "query" | "mutation";
type Hint = "idempotent" | "destructive" | "openWorld";

interface ToolRow {
  readonly name: string;
  readonly kind: Kind;
  readonly hint: Hint;
}

/**
 * Agentic-coding scene (variant 2), v2 "Flow Diagrams".
 *
 * Re-expresses the v1 "MCP tools" mini-list as a FAN-OUT flow diagram in the
 * locked cc-* system: one source node (`/graphql/mcp`) fans out to the four
 * published operations a coding agent can call. Each derived tool is a chip
 * carrying its query/mutation kind and one behavior-hint badge (idempotent /
 * destructive / openWorld) so the agent knows which calls are safe unattended.
 * The single teal path traces the one operation the headline is about: the
 * destructive `cancelOrder`, whose hint is the only one the agent must gate
 * (coral, a genuine status, not decoration). Everything else stays cream/grey.
 * Settled final frame, no animation, no hooks. Every svg id is prefixed
 * `v2-feedback-2-`.
 */

// Each fan-out arm originates from the source node and lands on a tool chip.
// Only the destructive arm is traced in teal; the rest stay faint grey.
const ARMS: readonly { readonly cy: number; readonly traced: boolean }[] = [
  { cy: 28, traced: false },
  { cy: 56, traced: false },
  { cy: 84, traced: false },
  { cy: 112, traced: true },
];

const SOURCE_X = 8;
const SOURCE_CY = 70;
const ELBOW_X = 40;
const TOOL_X = 52;

const FAINT = "rgba(245, 241, 234, 0.16)";
const ACCENT = "#5eead4";

/** The four operations the v1 list exposes, re-expressed as fan-out tool nodes. */
const ROWS: readonly ToolRow[] = [
  { name: "products", kind: "query", hint: "openWorld" },
  { name: "addToCart", kind: "mutation", hint: "idempotent" },
  { name: "placeOrder", kind: "mutation", hint: "idempotent" },
  { name: "cancelOrder", kind: "mutation", hint: "destructive" },
];

export function FeedbackVariant2({ className }: FeedbackVariant2Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          /graphql/mcp · tool catalog
        </p>

        <div className="mt-4 flex items-stretch gap-2">
          {/* fan-out connectors: source node -> four tool chips */}
          <div className="relative w-12 shrink-0">
            <svg
              viewBox="0 0 52 140"
              width="100%"
              height="100%"
              preserveAspectRatio="none"
              role="img"
              aria-label="One MCP source fanning out to four published-operation tools"
              style={{ display: "block" }}
            >
              <defs>
                <marker
                  id="v2-feedback-2-arrow-grey"
                  viewBox="0 0 6 6"
                  refX="5"
                  refY="3"
                  markerWidth="6"
                  markerHeight="6"
                  orient="auto-start-reverse"
                >
                  <path
                    d="M0 0 L6 3 L0 6"
                    fill="none"
                    stroke={FAINT}
                    strokeWidth="1"
                  />
                </marker>
                <marker
                  id="v2-feedback-2-arrow-teal"
                  viewBox="0 0 6 6"
                  refX="5"
                  refY="3"
                  markerWidth="6"
                  markerHeight="6"
                  orient="auto-start-reverse"
                >
                  <path
                    d="M0 0 L6 3 L0 6"
                    fill="none"
                    stroke={ACCENT}
                    strokeWidth="1"
                  />
                </marker>
              </defs>

              {/* the source node dot */}
              <circle cx={SOURCE_X} cy={SOURCE_CY} r="2.5" fill={ACCENT} />

              {ARMS.map((arm) => (
                <path
                  key={arm.cy}
                  d={`M${SOURCE_X} ${SOURCE_CY} H${ELBOW_X} V${arm.cy} H${TOOL_X}`}
                  fill="none"
                  stroke={arm.traced ? ACCENT : FAINT}
                  strokeWidth="1"
                  markerEnd={`url(#v2-feedback-2-arrow-${arm.traced ? "teal" : "grey"})`}
                />
              ))}
            </svg>
          </div>

          {/* tool chips: each a derived node with kind + behavior hint */}
          <div className="flex-1 space-y-1.5">
            {ROWS.map((row) => (
              <ToolChip
                key={row.name}
                row={row}
                traced={row.hint === "destructive"}
              />
            ))}
          </div>
        </div>

        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="4" label="exposed tools" />
          <Stat figure="1" label="gate required" status={ACCENT} />
        </div>

        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            the exact surface an agent can call
          </p>
        </div>
      </div>
    </div>
  );
}

/** Behavior-hint badge colors: only genuine status, never decoration. */
const HINT: Record<
  Hint,
  { readonly label: string; readonly border: string; readonly text: string }
> = {
  idempotent: {
    label: "idempotent",
    border: "rgba(52, 211, 153, 0.5)",
    text: "#34d399",
  },
  destructive: {
    label: "destructive",
    border: "rgba(240, 120, 106, 0.5)",
    text: "#f0786a",
  },
  openWorld: {
    label: "openWorld",
    border: "rgba(245, 241, 234, 0.16)",
    text: "rgba(245, 241, 234, 0.62)",
  },
};

/**
 * One published operation as a derived tool node: a rounded-md chip carrying the
 * operation name, its query/mutation kind, and a single behavior-hint badge. The
 * traced (destructive) tool takes the teal active-node treatment.
 */
function ToolChip({
  row,
  traced,
}: {
  readonly row: ToolRow;
  readonly traced: boolean;
}) {
  const hint = HINT[row.hint];
  return (
    <div
      className={[
        "bg-cc-surface flex items-center gap-2 rounded-md border px-2.5 py-1.5",
        traced ? "border-cc-accent/60" : "border-cc-card-border",
      ].join(" ")}
    >
      <span
        className={[
          "min-w-0 flex-1 truncate font-mono text-xs",
          traced ? "text-cc-accent" : "text-cc-ink",
        ].join(" ")}
      >
        {row.name}
      </span>
      <span className="text-cc-nav-label shrink-0 font-mono text-[0.55rem] tracking-[0.06em] uppercase">
        {row.kind}
      </span>
      <span
        className="shrink-0 rounded-full border px-1.5 py-px font-mono text-[0.5rem] tracking-[0.04em] whitespace-nowrap"
        style={{ borderColor: hint.border, color: hint.text }}
      >
        {hint.label}
      </span>
    </div>
  );
}

function Stat({
  figure,
  label,
  status,
}: {
  readonly figure: string;
  readonly label: string;
  readonly status?: string;
}) {
  return (
    <div>
      <p
        className="font-heading text-h4 leading-none font-semibold"
        style={{ color: status ?? "#f5f0ea" }}
      >
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-1.5 text-xs">{label}</p>
    </div>
  );
}
