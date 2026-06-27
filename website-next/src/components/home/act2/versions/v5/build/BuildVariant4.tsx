interface BuildVariant4Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration, v5 "Schematic Lines" (concept 4:
 * build feedback before runtime).
 *
 * A single monoline schematic whose field is split by a dashed boundary into a
 * BUILD-TIME band and a RUNTIME band. The teal thread starts at the hollow
 * `dotnet build` source ring, threads through the source generator and its
 * emitted `types + schema` node, and terminates green-equivalent (teal) at the
 * `passed` focal node, entirely inside the build band. Past the boundary the
 * runtime stage is a single dashed, not-yet-reached node a faint deferred hop
 * away, so the whole route that catches errors lives before anything runs.
 *
 * cc-* dark palette only, every stroke 1px non-scaling, exactly one teal accent
 * (the thread), no status hue (build cells carry none), settled final frame,
 * static, aria-hidden. Every marker id is prefixed "v5-build-4-".
 */

const cc = {
  faint: "rgba(245, 241, 234, 0.16)",
  surface: "#0c1322",
  accent: "#5eead4",
  navLabel: "#62748e",
  ink: "#a1a3af",
} as const;

const stroke1 = {
  vectorEffect: "non-scaling-stroke",
  strokeLinecap: "round",
  strokeLinejoin: "round",
} as const;

export function BuildVariant4({ className }: BuildVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          build before runtime
        </p>

        <svg
          viewBox="0 0 280 150"
          width="100%"
          style={{ display: "block", marginTop: "0.5rem" }}
        >
          <defs>
            <marker
              id="v5-build-4-arrow-teal"
              viewBox="0 0 6 6"
              refX="5"
              refY="3"
              markerWidth="6"
              markerHeight="6"
              markerUnits="userSpaceOnUse"
              orient="auto"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={cc.accent}
                strokeWidth="1"
                {...stroke1}
              />
            </marker>
            <marker
              id="v5-build-4-arrow-grey"
              viewBox="0 0 6 6"
              refX="5"
              refY="3"
              markerWidth="6"
              markerHeight="6"
              markerUnits="userSpaceOnUse"
              orient="auto"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={cc.faint}
                strokeWidth="1"
                {...stroke1}
              />
            </marker>
          </defs>

          {/* band labels: the split axis */}
          <text
            x={20}
            y={18}
            fontFamily="ui-monospace, SFMono-Regular, monospace"
            fontSize="7"
            letterSpacing="0.08em"
            fill={cc.navLabel}
          >
            BUILD-TIME
          </text>
          <text
            x={202}
            y={18}
            fontFamily="ui-monospace, SFMono-Regular, monospace"
            fontSize="7"
            letterSpacing="0.08em"
            fill={cc.navLabel}
          >
            RUNTIME
          </text>

          {/* dashed boundary splitting build-time from runtime */}
          <line
            x1={182}
            y1={26}
            x2={182}
            y2={118}
            stroke={cc.faint}
            strokeWidth="1"
            strokeDasharray="2 3"
            {...stroke1}
          />

          {/* teal thread: dotnet build -> generator -> types + schema -> passed */}
          <polyline
            points="50,66 84,66 124,66 150,66"
            fill="none"
            stroke={cc.accent}
            strokeWidth="1"
            markerEnd="url(#v5-build-4-arrow-teal)"
            {...stroke1}
          />

          {/* deferred hop: runtime is only reached after a green build */}
          <line
            x1={170}
            y1={66}
            x2={208}
            y2={66}
            stroke={cc.faint}
            strokeWidth="1"
            strokeDasharray="2 3"
            markerEnd="url(#v5-build-4-arrow-grey)"
            {...stroke1}
          />

          {/* hollow teal source ring: dotnet build */}
          <circle
            cx={42}
            cy={66}
            r={8}
            fill="none"
            stroke={cc.accent}
            strokeWidth="1"
            {...stroke1}
          />

          {/* generator (minor) and its emitted types + schema (minor) */}
          <circle
            cx={84}
            cy={66}
            r={5}
            fill={cc.surface}
            stroke={cc.faint}
            strokeWidth="1"
            {...stroke1}
          />
          <circle
            cx={124}
            cy={66}
            r={5}
            fill={cc.surface}
            stroke={cc.faint}
            strokeWidth="1"
            {...stroke1}
          />

          {/* focal node: build passed, the teal terminus */}
          <circle
            cx={160}
            cy={66}
            r={8}
            fill={cc.surface}
            stroke={cc.accent}
            strokeWidth="1"
            {...stroke1}
          />
          <circle cx={160} cy={66} r={2.5} fill={cc.accent} />

          {/* runtime: a single not-yet-reached dashed node */}
          <circle
            cx={218}
            cy={66}
            r={8}
            fill="none"
            stroke={cc.faint}
            strokeWidth="1"
            strokeDasharray="2 3"
            {...stroke1}
          />

          {/* baseline axis with registration ticks */}
          <line
            x1={20}
            y1={108}
            x2={260}
            y2={108}
            stroke={cc.faint}
            strokeWidth="1"
            {...stroke1}
          />
          {Array.from({ length: 18 }, (_, i) => 24 + i * 14).map((x) => (
            <line
              key={`v5-build-4-tick-${x}`}
              x1={x}
              y1={108}
              x2={x}
              y2={113}
              stroke={cc.faint}
              strokeWidth="1"
              {...stroke1}
            />
          ))}

          {/* sparse value labels along the traced route */}
          <text
            x={160}
            y={50}
            textAnchor="middle"
            fontFamily="ui-monospace, SFMono-Regular, monospace"
            fontSize="8"
            fill={cc.ink}
          >
            passed
          </text>
          <text
            x={42}
            y={92}
            textAnchor="middle"
            fontFamily="ui-monospace, SFMono-Regular, monospace"
            fontSize="8"
            fill={cc.ink}
          >
            dotnet build
          </text>
          <text
            x={124}
            y={92}
            textAnchor="middle"
            fontFamily="ui-monospace, SFMono-Regular, monospace"
            fontSize="8"
            fill={cc.ink}
          >
            types + schema
          </text>
        </svg>

        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            0
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">errors reach runtime</p>
        </div>
      </div>
    </div>
  );
}
