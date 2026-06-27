interface BuildVariant2Props {
  readonly className?: string;
}

/**
 * "Build loop" scene, concept #2 ("DataLoader request collapsing"), v3
 * "Signal & Metrics" (strict cc-* dark) take.
 *
 * Lead with the measured result: within one tick a resolver issues six
 * load(key) calls, two of them duplicate keys, and the generated DataLoader
 * collapses them into a SINGLE batched fetch. The hero is the cream collapse
 * numeral "6 -> 1" over a lowercase mono caption ("load(key) calls per fetch").
 *
 * Archetype B (stat-top / signal-row). The six inbound calls are grey
 * structure: four unique calls converge along faint 1px wires while the two
 * repeated keys are dimmed and dead-end (folded away). The lone teal accent is
 * the single batched fetch: a short teal trace ending in exactly one filled
 * teal node, the "this is the one reading" glyph landing on the surviving 1.
 * One footer caption credits the dedupe count. No status hues, nothing here is
 * failing.
 *
 * Static settled frame, no motion, no hooks, server component. Every svg id is
 * prefixed "v3-build-2-".
 */
const cc = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  inkDim: "rgba(245,241,234,0.62)",
  ink: "#a1a3af",
  navLabel: "#62748e",
  teal: "#5eead4",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v3-build-2-";

/** The six inbound load(key) calls within one tick; two are duplicate keys. */
const TICKS: ReadonlyArray<{
  readonly key: string;
  readonly dup: boolean;
  readonly x: number;
}> = [
  { key: "7", dup: false, x: 14 },
  { key: "2", dup: false, x: 44 },
  { key: "7", dup: true, x: 74 },
  { key: "9", dup: false, x: 104 },
  { key: "4", dup: false, x: 134 },
  { key: "2", dup: true, x: 164 },
];

export function BuildVariant2({ className }: BuildVariant2Props) {
  // collapse geometry (viewBox 288 x 56).
  const nodeY = 15;
  const labelY = 7;
  const junctionX = 206;
  const junctionY = 42;
  const fetchX = 228;

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-4 backdrop-blur-sm">
        {/* eyebrow */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          dataloader
        </p>

        {/* HERO: collapse numeral "6 -> 1" + lowercase mono caption */}
        <div className="mt-3 flex items-baseline gap-2.5">
          <span
            className="text-cc-heading font-heading leading-none font-semibold"
            style={{ fontSize: "2rem" }}
          >
            6
          </span>
          <span
            aria-hidden="true"
            className="text-cc-ink-faint"
            style={{ fontFamily: MONO, fontSize: "0.95rem" }}
          >
            &rarr;
          </span>
          <span
            className="text-cc-heading font-heading leading-none font-semibold"
            style={{ fontSize: "2rem" }}
          >
            1
          </span>
        </div>
        <p className="text-cc-ink-dim mt-1.5 font-mono text-[0.62rem] lowercase">
          load(key) calls per fetch
        </p>

        {/* TEAL SIGNAL: six inbound load(key) calls within one tick. The four
            unique calls converge along faint grey wires into a single teal
            batched fetch; the two repeated keys are dimmed and dead-end. */}
        <svg
          viewBox="0 0 288 56"
          width="100%"
          role="img"
          aria-label="Six load(key) calls in one tick collapse into a single batched fetch; two duplicate keys are deduped."
          className="mt-4"
          style={{ display: "block" }}
        >
          {/* converging wires from the four unique calls (grey structure) */}
          {TICKS.filter((t) => !t.dup).map((t) => (
            <line
              key={`${ID}wire-${t.x}`}
              x1={t.x}
              y1={nodeY}
              x2={junctionX}
              y2={junctionY}
              stroke={cc.inkFaint}
              strokeWidth="1"
            />
          ))}

          {/* the single batched-fetch trace + node (the one teal accent) */}
          <line
            x1={junctionX}
            y1={junctionY}
            x2={fetchX - 4}
            y2={junctionY}
            stroke={cc.teal}
            strokeWidth="1"
          />
          <circle cx={fetchX} cy={junctionY} r="5" fill={cc.surface} />
          <circle cx={fetchX} cy={junctionY} r="3" fill={cc.teal} />
          <text
            x={fetchX + 10}
            y={junctionY + 3}
            fontFamily={MONO}
            fontSize="8.5"
            fill={cc.navLabel}
          >
            fetch
          </text>

          {/* the six inbound calls; duplicates dimmed and folded away */}
          {TICKS.map((t, i) => (
            <g key={`${ID}call-${i}`}>
              <text
                x={t.x}
                y={labelY}
                textAnchor="middle"
                fontFamily={MONO}
                fontSize="8.5"
                fill={t.dup ? cc.navLabel : cc.ink}
                opacity={t.dup ? 0.5 : 1}
              >
                {t.key}
              </text>
              <circle
                cx={t.x}
                cy={nodeY}
                r={t.dup ? 1.8 : 2.4}
                fill={t.dup ? cc.inkFaint : cc.ink}
              />
              {t.dup ? (
                <line
                  x1={t.x}
                  y1={nodeY + 3}
                  x2={t.x}
                  y2={nodeY + 14}
                  stroke={cc.inkFaint}
                  strokeWidth="1"
                  strokeDasharray="2 2"
                />
              ) : null}
            </g>
          ))}
        </svg>

        {/* footer caption over a dashed divider */}
        <div className="border-cc-ink-faint mt-3 border-t border-dashed pt-2.5">
          <p className="text-cc-ink-dim font-mono text-[0.6rem] lowercase">
            2 duplicate keys deduped
          </p>
        </div>
      </div>
    </div>
  );
}
