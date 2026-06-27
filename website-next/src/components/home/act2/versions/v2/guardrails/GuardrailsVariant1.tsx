/**
 * Release-safety scene, variant 1 - v2 "Flow Diagrams" locked system.
 *
 * Topology: FAN-OUT. One source node (schema.graphql) fans out to the three
 * changed lines of a `type Product` hunk, each line classified by risk via a
 * derived chip. Two land SAFE (cream/grey), the single removed field is flagged
 * BREAKING with a coral status border (real status, the firing gate), and the
 * one teal path traces that breaking line down to its pinned registry-bot
 * Resolve thread. Every change classified by risk; teal follows only the route
 * the headline names (the breaking field to its resolve thread).
 *
 * Borrowed literal content from v1: file schema.graphql (+2 / -1), fields
 * reviewCount: Int! (SAFE), legacySku: String (BREAKING), price: Money!
 * @deprecated (SAFE), and the registry-bot resolve thread on 6 ops.
 *
 * Static, settled final frame. React Server Component, no hooks, no motion.
 * cc-* palette only; teal is the single accent. Every svg id prefixed
 * "v2-guardrails-1-".
 */

interface GuardrailsVariant1Props {
  readonly className?: string;
}

const cc = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  accent: "#5eead4",
  coral: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

type Risk = "SAFE" | "BREAKING";

interface ChangeLine {
  readonly field: string;
  readonly risk: Risk;
}

const CHANGES: readonly ChangeLine[] = [
  { field: "+ reviewCount: Int!", risk: "SAFE" },
  { field: "- legacySku: String", risk: "BREAKING" },
  { field: "+ price: Money!", risk: "SAFE" },
];

export function GuardrailsVariant1({ className }: GuardrailsVariant1Props) {
  const idp = "v2-guardrails-1-";

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* Panel eyebrow. */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          schema diff classified
        </p>

        {/* FAN-OUT: source schema fans to three risk-classified change lines. */}
        <div className="mt-4 flex items-stretch gap-2">
          {/* Source node. */}
          <div className="flex shrink-0 items-center">
            <span
              className="rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap"
              style={{
                background: cc.surface,
                borderColor: cc.cardBorder,
                color: cc.ink,
              }}
            >
              schema.graphql
            </span>
          </div>

          {/* Fan-out connectors: three grey 1px branches, teal on the breaking one. */}
          <svg
            viewBox="0 0 28 96"
            width="28"
            height="96"
            aria-hidden="true"
            style={{ flex: "0 0 auto", alignSelf: "center" }}
          >
            <defs>
              <marker
                id={`${idp}arrow`}
                viewBox="0 0 6 6"
                refX="5"
                refY="3"
                markerWidth="5"
                markerHeight="5"
                orient="auto-start-reverse"
              >
                <path d="M0 0 L6 3 L0 6" fill="none" stroke={cc.inkFaint} />
              </marker>
              <marker
                id={`${idp}arrowAccent`}
                viewBox="0 0 6 6"
                refX="5"
                refY="3"
                markerWidth="5"
                markerHeight="5"
                orient="auto-start-reverse"
              >
                <path d="M0 0 L6 3 L0 6" fill="none" stroke={cc.accent} />
              </marker>
            </defs>
            {/* trunk from the source, vertically centered */}
            <line
              x1="0"
              y1="48"
              x2="8"
              y2="48"
              stroke={cc.inkFaint}
              strokeWidth="1"
            />
            {/* branch to SAFE row (top) */}
            <path
              d="M8 48 L8 16 L28 16"
              fill="none"
              stroke={cc.inkFaint}
              strokeWidth="1"
              markerEnd={`url(#${idp}arrow)`}
            />
            {/* branch to BREAKING row (middle) - the single teal path */}
            <path
              d="M8 48 L28 48"
              fill="none"
              stroke={cc.accent}
              strokeWidth="1"
              markerEnd={`url(#${idp}arrowAccent)`}
            />
            {/* branch to SAFE row (bottom) */}
            <path
              d="M8 48 L8 80 L28 80"
              fill="none"
              stroke={cc.inkFaint}
              strokeWidth="1"
              markerEnd={`url(#${idp}arrow)`}
            />
          </svg>

          {/* The three classified change chips. */}
          <div className="flex flex-1 flex-col justify-between gap-1.5">
            {CHANGES.map((change) => (
              <ChangeChip key={change.field} change={change} />
            ))}
          </div>
        </div>

        {/* Deferred (async) hop from the breaking line to its pinned thread. */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-4">
          <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
            pinned thread
          </p>
          <div
            className="mt-2 flex items-center gap-2 rounded-md border px-3 py-2"
            style={{ background: cc.surface, borderColor: `${cc.coral}55` }}
          >
            <span
              aria-hidden="true"
              style={{
                width: 6,
                height: 6,
                flex: "0 0 auto",
                borderRadius: 999,
                background: cc.coral,
              }}
            />
            <span
              className="font-mono text-[0.65rem]"
              style={{ color: cc.coral }}
            >
              registry-bot
            </span>
            <span className="text-cc-ink-dim font-mono text-[0.65rem]">
              removes field on 6 ops
            </span>
            <span style={{ flex: 1 }} />
            <span
              className="rounded border px-1.5 py-0.5 font-mono text-[0.5rem] tracking-[0.08em]"
              style={{ borderColor: `${cc.coral}55`, color: cc.coral }}
            >
              RESOLVE
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}

/* A derived change chip: a rounded-md node whose border encodes the risk class.
 * SAFE chips stay cream/grey; the BREAKING chip carries a coral status border
 * and a coral tag (real status, the one firing gate). */
function ChangeChip({ change }: { readonly change: ChangeLine }) {
  const breaking = change.risk === "BREAKING";
  const borderColor = breaking ? `${cc.coral}66` : cc.cardBorder;
  const tagColor = breaking ? cc.coral : cc.accent;
  return (
    <div
      className="flex items-center gap-2 rounded-md border px-2.5 py-1.5"
      style={{ background: cc.surface, borderColor }}
    >
      <span
        className="flex-1 font-mono text-[0.65rem] whitespace-nowrap"
        style={{ color: cc.ink }}
      >
        {change.field}
      </span>
      <span
        className="rounded-sm border px-1 py-px font-mono text-[0.5rem] tracking-[0.06em]"
        style={{ borderColor: `${tagColor}66`, color: tagColor }}
      >
        {change.risk}
      </span>
    </div>
  );
}
