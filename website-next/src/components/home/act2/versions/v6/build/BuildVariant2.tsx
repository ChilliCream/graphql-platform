interface BuildVariant2Props {
  readonly className?: string;
}

/**
 * Bespoke "Build loop" hook illustration (v6, concept 2: DataLoader batch ledger).
 *
 * Visual promise: "N+1 queries, gone by default." Five thin grey key-lookup
 * lines on the left (labeled 41 17 88 17 41) converge through a small focusing
 * lens into ONE thick teal beam that lands on a single database cylinder. The
 * repeated key occurrences (the second 17 and second 41) are dimmed and dashed
 * to show the dedupe; a quiet mono counter reads "5 keys -> 1 fetch".
 *
 * This is the inverse of the scene's fan-out cell: it funnels many inbound
 * requests down to one batched fetch. Pure schematic line-art in the site's dark
 * cc-* language, thin 1px strokes, generous negative space. Rendered as the
 * settled final frame, no animation. Every SVG id is prefixed "v6-build-2-".
 */
export function BuildVariant2({ className }: BuildVariant2Props) {
  const MONO =
    "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', monospace";

  // The five inbound resolver key lookups within one tick. The second 17 and
  // second 41 are duplicate occurrences: kept only once, so they read as deduped.
  const keys: readonly { readonly id: string; readonly y: number; readonly dup: boolean }[] =
    [
      { id: "41", y: 44, dup: false },
      { id: "17", y: 70, dup: false },
      { id: "88", y: 96, dup: false },
      { id: "17", y: 122, dup: true },
      { id: "41", y: 148, dup: true },
    ];

  const focusX = 186;
  const focusY = 96;

  return (
    <div
      className={[
        "border-cc-card-border bg-cc-card-bg/60 mx-auto w-full max-w-[320px] rounded-2xl border p-5 backdrop-blur-sm select-none",
        className ?? "",
      ].join(" ")}
    >
      <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
        dedupe + batch
      </p>

      <svg
        className="mt-3 block w-full"
        viewBox="0 0 320 158"
        fill="none"
        role="img"
        aria-label="Five repeated key lookups converge through a lens and resolve in a single batched database fetch"
      >
        <defs>
          <linearGradient
            id="v6-build-2-beam"
            x1="0"
            y1="0"
            x2="1"
            y2="0"
          >
            <stop offset="0" stopColor="#5eead4" />
            <stop offset="1" stopColor="#99f6e4" />
          </linearGradient>
        </defs>

        {/* converging key-lookup lines + labels */}
        {keys.map((k, i) => (
          <g key={i}>
            <line
              x1={40}
              y1={k.y}
              x2={focusX}
              y2={focusY}
              stroke={
                k.dup ? "rgba(245,241,234,0.10)" : "rgba(245,241,234,0.30)"
              }
              strokeWidth={1}
              strokeDasharray={k.dup ? "3 3" : undefined}
            />
            <circle
              cx={36}
              cy={k.y}
              r={2.4}
              fill={k.dup ? "rgba(245,241,234,0.22)" : "#a1a3af"}
            />
            <text
              x={24}
              y={k.y}
              dy="0.32em"
              textAnchor="end"
              fontFamily={MONO}
              fontSize={10.5}
              fill={k.dup ? "rgba(245,241,234,0.32)" : "#f5f0ea"}
            >
              {k.id}
            </text>
          </g>
        ))}

        {/* focusing lens at the convergence point */}
        <path
          d="M186 96 Q194 78 202 96 Q194 114 186 96 Z"
          fill="rgba(94,234,212,0.08)"
        />
        <path
          d="M186 96 Q194 78 202 96"
          stroke="rgba(94,234,212,0.7)"
          strokeWidth={1.2}
        />
        <path
          d="M186 96 Q194 114 202 96"
          stroke="rgba(94,234,212,0.7)"
          strokeWidth={1.2}
        />

        {/* the single batched fetch: one thick teal beam */}
        <line
          x1={202}
          y1={96}
          x2={238}
          y2={96}
          stroke="rgba(94,234,212,0.16)"
          strokeWidth={7}
          strokeLinecap="round"
        />
        <line
          x1={202}
          y1={96}
          x2={238}
          y2={96}
          stroke="url(#v6-build-2-beam)"
          strokeWidth={3}
          strokeLinecap="round"
        />

        {/* database cylinder: the single landing point */}
        <path
          d="M238 78 L238 114 A18 5 0 0 0 274 114 L274 78"
          fill="#0c1322"
          stroke="#5eead4"
          strokeWidth={1.2}
          strokeLinejoin="round"
        />
        <path
          d="M238 96 A18 5 0 0 0 274 96"
          stroke="rgba(94,234,212,0.45)"
          strokeWidth={1}
        />
        <ellipse
          cx={256}
          cy={78}
          rx={18}
          ry={5}
          fill="#0c1322"
          stroke="#5eead4"
          strokeWidth={1.2}
        />

        <text
          x={256}
          y={134}
          textAnchor="middle"
          fontFamily={MONO}
          fontSize={10}
          fill="rgba(245,241,234,0.62)"
        >
          ProductById
        </text>
      </svg>

      <p className="text-cc-ink-dim mt-3 text-center font-mono text-[0.72rem] tracking-[0.04em]">
        5 keys{" "}
        <span aria-hidden="true" className="text-cc-ink-faint">
          &rarr;
        </span>{" "}
        <span className="text-cc-accent">1 fetch</span>
      </p>
    </div>
  );
}
