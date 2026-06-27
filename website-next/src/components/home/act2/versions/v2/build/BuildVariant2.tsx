import type { ReactNode } from "react";

interface BuildVariant2Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration, v2 flow-diagram take on concept #2,
 * "DataLoader request collapsing".
 *
 * Topology: MERGE / CONVERGE. Six inbound resolver key-requests land within one
 * tick on the left (the keys 7, 42, 42, 13, 88, 42; the three repeated 42's are
 * the duplicates). Thin grey single-elbow connectors run each request rightward
 * into one convergence node, which fans a single teal trunk into the batched
 * fetch node on the right. The one teal path traces the surviving deduped route:
 * the distinct-key set collapsing into a single LoadAsync call. Duplicate key
 * chips render dimmed with dashed connectors so the collapse reads at a glance.
 * A Stat duo footer carries the two numbers: the 6 -> 1 dedupe and the single
 * resulting query. Settled final frame, no animation. Every svg id is prefixed
 * `v2-build-2-`.
 */
export function BuildVariant2({ className }: BuildVariant2Props) {
  const idPrefix = "v2-build-2-";

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          dataloader batch
        </p>

        {/* converge diagram: six inbound keys -> one batched fetch */}
        <div className="mt-4 grid grid-cols-[auto_1fr_auto] items-center gap-2">
          {/* left column: six inbound resolver key requests within one tick */}
          <div className="flex flex-col gap-1">
            {KEYS.map((k, i) => (
              <KeyChip key={i} label={k.label} dup={k.dup} />
            ))}
          </div>

          {/* center: grey connectors converge, one teal trunk continues */}
          <Connectors idPrefix={idPrefix} />

          {/* right: the single batched fetch node */}
          <div className="flex flex-col items-start gap-1">
            <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.08em] uppercase">
              one fetch
            </span>
            <span className="border-cc-accent/60 text-cc-accent bg-cc-surface rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap">
              LoadAsync([7,42,13,88])
            </span>
            <span className="border-cc-card-border bg-cc-surface text-cc-ink-dim mt-0.5 rounded-md border px-2 py-1 font-mono text-[0.6rem] whitespace-nowrap">
              ProductBatch
            </span>
          </div>
        </div>

        {/* one-tick caption under a dashed divider */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            collected within one tick, fetched once
          </p>
        </div>

        {/* stat duo: the dedupe and the single resulting query */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure={<>6 &rarr; 1</>} label="keys deduped" />
          <Stat figure={<>1</>} label="batched query" />
        </div>
      </div>
    </div>
  );
}

/** The six inbound key requests within one tick; the repeated 42's are dupes. */
const KEYS: readonly { readonly label: string; readonly dup: boolean }[] = [
  { label: "key 7", dup: false },
  { label: "key 42", dup: false },
  { label: "key 42", dup: true },
  { label: "key 13", dup: false },
  { label: "key 88", dup: false },
  { label: "key 42", dup: true },
];

interface KeyChipProps {
  readonly label: string;
  readonly dup: boolean;
}

/** One inbound resolver key-request chip; duplicates dim with a dashed border. */
function KeyChip({ label, dup }: KeyChipProps) {
  return (
    <span
      className={[
        "bg-cc-surface rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap",
        dup
          ? "border-cc-ink-faint text-cc-ink-dim border-dashed"
          : "border-cc-card-border text-cc-ink",
      ].join(" ")}
    >
      {label}
    </span>
  );
}

interface ConnectorsProps {
  readonly idPrefix: string;
}

/**
 * Thin 1px connectors from the six inbound key rows into one convergence node,
 * then a single teal trunk to the batched fetch. Distinct-key connectors are
 * grey; duplicate-key connectors are dashed and dimmer; the merged trunk is the
 * single teal traced path.
 */
function Connectors({ idPrefix }: ConnectorsProps) {
  // Six rows spaced to line up with the gap-1 chip column (~26px row pitch).
  const rowY = [13, 39, 65, 91, 117, 143];
  const mergeX = 40;
  const mergeY = 78;
  const faint = "rgba(245,241,234,0.16)";
  const accent = "#5eead4";

  return (
    <svg
      viewBox="0 0 72 156"
      width="100%"
      role="img"
      aria-label="Six key requests converging into a single batched fetch"
      className="h-[156px] w-full"
      style={{ display: "block" }}
    >
      <defs>
        <marker
          id={`${idPrefix}arrow-grey`}
          viewBox="0 0 6 6"
          refX="5"
          refY="3"
          markerWidth="5"
          markerHeight="5"
          orient="auto-start-reverse"
        >
          <path d="M0 0 L6 3 L0 6" fill="none" stroke={faint} strokeWidth="1" />
        </marker>
        <marker
          id={`${idPrefix}arrow-teal`}
          viewBox="0 0 6 6"
          refX="5"
          refY="3"
          markerWidth="5"
          markerHeight="5"
          orient="auto-start-reverse"
        >
          <path d="M0 0 L6 3 L0 6" fill="none" stroke={accent} strokeWidth="1" />
        </marker>
      </defs>

      {/* grey single-elbow connectors from each key row into the merge node */}
      {KEYS.map((k, i) => (
        <path
          key={i}
          d={`M0 ${rowY[i]} H${mergeX - 12} V${mergeY}`}
          fill="none"
          stroke={faint}
          strokeWidth="1"
          strokeDasharray={k.dup ? "2 2" : undefined}
          strokeOpacity={k.dup ? 0.5 : 1}
        />
      ))}

      {/* convergence node where the six requests merge */}
      <circle
        cx={mergeX - 12}
        cy={mergeY}
        r="2.5"
        fill="none"
        stroke={accent}
        strokeWidth="1"
      />

      {/* the single teal trunk: one deduped batch leaving the merge */}
      <path
        d={`M${mergeX - 12} ${mergeY} H72`}
        fill="none"
        stroke={accent}
        strokeWidth="1"
        markerEnd={`url(#${idPrefix}arrow-teal)`}
      />
    </svg>
  );
}

interface StatProps {
  readonly figure: ReactNode;
  readonly label: string;
}

/** A big display numeral over a small dim label, the ScrollScenes Stat. */
function Stat({ figure, label }: StatProps) {
  return (
    <div>
      <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-1.5 text-xs">{label}</p>
    </div>
  );
}
