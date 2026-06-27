/**
 * "Release safety" scene, variant 3 - v2 flow-diagram system.
 *
 * Re-expresses the v1 Nitro client-impact mini-list as a fan-out relationship
 * diagram in the locked v2 flow vocabulary (the ScrollScenes Chip / Arrow / Stat
 * / thin-bar set on cc-* surfaces). One breaking change node, `checkout-v3`,
 * fans out to the three published clients registered against the schema; each
 * client carries a per-client readiness bar reading the share of its operations
 * that still validate. The single teal path traces the change out to the client
 * the headline is about: Partner, the consumer whose every operation breaks.
 *
 * Content carried verbatim from the v1 widget so the gallery caption stays
 * accurate: change `checkout-v3` reconciled against Web 5/5 (validated, OK),
 * iOS 3/5 (at risk, amber), Partner 0/4 (queued, coral / breaking). Status hues
 * encode genuine per-client readiness only; the lone decorative accent is teal.
 *
 * React Server Component: no "use client", no hooks, no handlers, no motion.
 * Static settled frame. Inline SVG connectors prefixed "v2-guardrails-3-".
 */

interface GuardrailsVariant3Props {
  readonly className?: string;
}

/* cc-* palette mirrored as local constants for the inline SVG connectors. The
 * Tailwind cc-* utilities carry the same values on the chip / bar markup. */
const cc = {
  accent: "#5eead4",
  inkFaint: "rgba(245,241,234,0.16)",
} as const;

type ClientStatus = "ok" | "at-risk" | "queued";

interface ClientRow {
  readonly name: string;
  readonly status: ClientStatus;
  /** Operations that still validate against the proposed schema. */
  readonly ok: number;
  /** Total registered operations for this client. */
  readonly total: number;
}

/* The three registered clients and how each fares against `checkout-v3`. */
const CLIENTS: readonly ClientRow[] = [
  { name: "Web", status: "ok", ok: 5, total: 5 },
  { name: "iOS", status: "at-risk", ok: 3, total: 5 },
  { name: "Partner", status: "queued", ok: 0, total: 4 },
];

const STATUS_WORD: Record<ClientStatus, string> = {
  ok: "validated",
  "at-risk": "at risk",
  queued: "queued",
};

/* Per-client readiness bar color. Real status only: healthy teal-ish reads as
 * the OK end of the traced path, amber = at-risk, coral = the breaking client. */
function barColorClass(status: ClientStatus): string {
  if (status === "at-risk") {
    return "bg-cc-status-investigating";
  }
  if (status === "queued") {
    return "bg-cc-status-firing";
  }
  return "bg-cc-accent";
}

function statusTextClass(status: ClientStatus): string {
  if (status === "at-risk") {
    return "text-cc-status-investigating";
  }
  if (status === "queued") {
    return "text-cc-status-firing";
  }
  return "text-cc-accent";
}

export function GuardrailsVariant3({ className }: GuardrailsVariant3Props) {
  const idp = "v2-guardrails-3-";

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div
        className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm"
        aria-hidden="true"
      >
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          published-client impact
        </p>

        {/* Fan-out: the breaking change source -> the three registered clients.
            A single teal connector traces the source to the breaking client. */}
        <div className="mt-4 flex items-stretch gap-2.5">
          {/* source node: the change under test (the teal-active in-flight item) */}
          <div className="flex shrink-0 items-center">
            <span className="border-cc-accent/60 text-cc-accent bg-cc-surface rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap">
              checkout-v3
            </span>
          </div>

          {/* fan-out connectors: one source -> three client rows. The teal path
              traces only the route to the breaking (queued) client. */}
          <svg
            viewBox="0 0 28 96"
            width="28"
            height="96"
            className="shrink-0 self-stretch"
            preserveAspectRatio="none"
            aria-hidden="true"
            style={{ display: "block" }}
          >
            <defs>
              <marker
                id={`${idp}head-grey`}
                viewBox="0 0 6 6"
                refX="5"
                refY="3"
                markerWidth="5"
                markerHeight="5"
                orient="auto-start-reverse"
              >
                <path
                  d="M0 0.5 L5 3 L0 5.5"
                  fill="none"
                  stroke={cc.inkFaint}
                  strokeWidth="1"
                />
              </marker>
              <marker
                id={`${idp}head-teal`}
                viewBox="0 0 6 6"
                refX="5"
                refY="3"
                markerWidth="5"
                markerHeight="5"
                orient="auto-start-reverse"
              >
                <path
                  d="M0 0.5 L5 3 L0 5.5"
                  fill="none"
                  stroke={cc.accent}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* shared origin stub from the source node */}
            <line
              x1="0"
              y1="48"
              x2="6"
              y2="48"
              stroke={cc.inkFaint}
              strokeWidth="1"
            />

            {/* grey single-elbow connector to the OK client (top row) */}
            <path
              d="M6 48 L10 48 L10 16 L26 16"
              fill="none"
              stroke={cc.inkFaint}
              strokeWidth="1"
              markerEnd={`url(#${idp}head-grey)`}
            />
            {/* grey single-elbow connector to the at-risk client (middle row) */}
            <path
              d="M6 48 L10 48 L26 48"
              fill="none"
              stroke={cc.inkFaint}
              strokeWidth="1"
              markerEnd={`url(#${idp}head-grey)`}
            />
            {/* teal traced path to the breaking client (bottom row) */}
            <path
              d="M6 48 L10 48 L10 80 L26 80"
              fill="none"
              stroke={cc.accent}
              strokeWidth="1"
              markerEnd={`url(#${idp}head-teal)`}
            />
          </svg>

          {/* the three client rows: name chip + readiness bar + tally */}
          <div className="flex flex-1 flex-col justify-between gap-2 py-0.5">
            {CLIENTS.map((row) => {
              const pct =
                row.total === 0 ? 0 : Math.round((row.ok / row.total) * 100);
              const breaking = row.status === "queued";
              return (
                <div
                  key={row.name}
                  className={[
                    "bg-cc-surface flex items-center gap-2 rounded-md border px-2 py-1",
                    breaking
                      ? "border-cc-status-firing/50 border-dashed"
                      : "border-cc-card-border",
                  ].join(" ")}
                >
                  <span className="text-cc-ink w-12 shrink-0 font-mono text-[0.65rem]">
                    {row.name}
                  </span>
                  <span className="bg-cc-surface relative h-2 flex-1 overflow-hidden rounded-full">
                    {pct > 0 && (
                      <span
                        className={[
                          "absolute top-0 left-0 h-full rounded-full opacity-80",
                          barColorClass(row.status),
                        ].join(" ")}
                        style={{ width: `${pct}%` }}
                      />
                    )}
                  </span>
                  <span className="text-cc-ink-dim w-7 shrink-0 text-right font-mono text-[0.6rem] tabular-nums">
                    {row.ok}/{row.total}
                  </span>
                </div>
              );
            })}
          </div>
        </div>

        {/* status legend strip: one word per client, status-colored */}
        <div className="border-cc-card-border mt-4 flex items-center justify-end gap-3 border-t pt-3">
          {CLIENTS.map((row) => (
            <span
              key={row.name}
              className={[
                "font-mono text-[0.55rem] tracking-[0.08em] uppercase",
                statusTextClass(row.status),
              ].join(" ")}
            >
              {STATUS_WORD[row.status]}
            </span>
          ))}
        </div>

        {/* Stat duo: the two numbers the verdict turns on. */}
        <div className="border-cc-card-border mt-3 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              1/3
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">
              clients fully ready
            </p>
          </div>
          <div>
            <p className="font-heading text-cc-status-firing text-h4 leading-none font-semibold">
              4 ops
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">break on Partner</p>
          </div>
        </div>
      </div>
    </div>
  );
}
