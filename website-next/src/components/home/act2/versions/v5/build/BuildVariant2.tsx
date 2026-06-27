interface BuildVariant2Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration, v5 "Schematic Lines" take on concept #2,
 * "DataLoader request collapsing".
 *
 * Skeleton: a converging funnel. Six inbound resolver key-requests collected
 * within one tick (the keys 7, 42, 42, 13, 88, 42) fan in from the left as thin
 * grey monolines; the two repeated 42's render as dimmed dashed rings so the
 * dedupe reads at a glance. They collapse into the hollow teal source ring (the
 * dedupe throat) where the single teal thread begins, runs as one trunk into the
 * loader hub, and terminates in a solid teal dot. The teal route is the one
 * surviving batched fetch. A single footer numeral carries the 6 -> 1 collapse.
 *
 * Static settled frame, no animation. Every svg id is prefixed `v5-build-2-`.
 */
export function BuildVariant2({ className }: BuildVariant2Props) {
  const idPrefix = "v5-build-2-";

  const faint = "rgba(245,241,234,0.16)";
  const teal = "#5eead4";
  const navLabel = "#62748e";
  const inkValue = "#a1a3af";
  const mono =
    'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

  // Six inbound requests within one tick; the two trailing 42's are duplicates.
  const requests: readonly { readonly dup: boolean }[] = [
    { dup: false },
    { dup: false },
    { dup: true },
    { dup: false },
    { dup: false },
    { dup: true },
  ];

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

        <svg
          width="100%"
          viewBox="0 0 280 150"
          fill="none"
          className="mt-4 block"
        >
          <defs>
            <marker
              id={`${idPrefix}arrow-teal`}
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
                stroke={teal}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </marker>
          </defs>

          {/* grey funnel: six request rings fan in and collapse at the throat */}
          {requests.map((r, i) => {
            const cy = 78 + (i - 2.5) * 20;
            return (
              <g
                key={i}
                stroke={faint}
                strokeOpacity={r.dup ? 0.5 : 1}
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <line
                  x1="51"
                  y1={cy}
                  x2="144"
                  y2="78"
                  strokeWidth="1"
                  vectorEffect="non-scaling-stroke"
                  strokeDasharray={r.dup ? "2 3" : undefined}
                />
                <circle
                  cx="46"
                  cy={cy}
                  r="5"
                  fill="none"
                  strokeWidth="1"
                  vectorEffect="non-scaling-stroke"
                  strokeDasharray={r.dup ? "2 3" : undefined}
                />
              </g>
            );
          })}

          {/* teal thread: hollow source ring -> single trunk -> loader hub + dot */}
          <circle
            cx="150"
            cy="78"
            r="6"
            fill="none"
            stroke={teal}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <line
            x1="156"
            y1="78"
            x2="224"
            y2="78"
            stroke={teal}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            markerEnd={`url(#${idPrefix}arrow-teal)`}
          />
          <circle
            cx="236"
            cy="78"
            r="11"
            fill="none"
            stroke={teal}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="236" cy="78" r="2.5" fill={teal} />

          {/* sparse micro-labels */}
          <text
            x="46"
            y="18"
            textAnchor="middle"
            fontFamily={mono}
            fontSize="7"
            letterSpacing="0.08em"
            fill={navLabel}
          >
            ONE TICK
          </text>
          <text
            x="150"
            y="102"
            textAnchor="middle"
            fontFamily={mono}
            fontSize="7"
            letterSpacing="0.08em"
            fill={navLabel}
          >
            DEDUPE
          </text>
          <text
            x="236"
            y="102"
            textAnchor="middle"
            fontFamily={mono}
            fontSize="8"
            fill={inkValue}
          >
            LoadAsync
          </text>
        </svg>

        {/* single footer numeral: six requests collapse to one batched fetch */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            6 &rarr; 1
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">one batched fetch</p>
        </div>
      </div>
    </div>
  );
}
