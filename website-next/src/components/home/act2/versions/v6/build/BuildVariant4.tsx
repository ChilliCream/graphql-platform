interface BuildVariant4Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration, v6 (bespoke, concept: caught at build, not in
 * prod).
 *
 * A barrier stopping a red break. On the left a small ProductApi.cs editor line
 * shows a field renamed `price` -> `cost`, the new token underlined with a coral
 * squiggle. A coral wire leaves that field heading toward the `client` chip, but
 * it SNAPS at a vertical teal `build` gate: the cable frays and the break is
 * stopped at the line. Beyond the gate sits a calm, dimmed `production` zone the
 * coral never reaches, where the same client dependency is drawn as a quiet grey
 * stub. Coral is rationed to the single broken wire; the gate and production stay
 * grey/teal.
 *
 * cc-* dark palette only, thin 1px non-scaling strokes, settled final frame,
 * static, aria-hidden. Every svg id is prefixed "v6-build-4-".
 */

const cc = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  dim: "rgba(245, 241, 234, 0.62)",
  eyebrow: "#62748e",
  border: "rgba(245, 241, 234, 0.12)",
  accent: "#5eead4",
  coral: "#f0786a",
} as const;

const mono = "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";

const stroke1 = {
  vectorEffect: "non-scaling-stroke",
  strokeLinecap: "round",
  strokeLinejoin: "round",
} as const;

export function BuildVariant4({ className }: BuildVariant4Props) {
  return (
    <div
      className={["mx-auto w-full select-none", className ?? ""].join(" ")}
      style={{ maxWidth: "320px" }}
      aria-hidden="true"
    >
      <svg
        viewBox="0 0 320 188"
        width="100%"
        style={{ display: "block", overflow: "visible" }}
      >
        <defs>
          <marker
            id="v6-build-4-arrow"
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
              stroke={cc.border}
              strokeWidth="1"
              {...stroke1}
            />
          </marker>
        </defs>

        {/* dimmed production zone: the break never reaches here */}
        <rect
          x={230}
          y={50}
          width={80}
          height={100}
          rx={10}
          fill="rgba(245, 241, 234, 0.02)"
          stroke={cc.border}
          strokeWidth="1"
          {...stroke1}
        />
        <text
          x={240}
          y={66}
          fontFamily={mono}
          fontSize="8"
          letterSpacing="0.08em"
          fill={cc.eyebrow}
        >
          production
        </text>

        {/* quiet grey client dependency, calm on the protected side */}
        <line
          x1={219}
          y1={112}
          x2={240}
          y2={112}
          stroke={cc.border}
          strokeWidth="1"
          strokeDasharray="2 3"
          markerEnd="url(#v6-build-4-arrow)"
          {...stroke1}
        />
        <rect
          x={242}
          y={100}
          width={58}
          height={24}
          rx={6}
          fill={cc.surface}
          stroke={cc.border}
          strokeWidth="1"
          {...stroke1}
        />
        <circle cx={254} cy={112} r={3} fill={cc.accent} opacity={0.7} />
        <text
          x={263}
          y={113}
          fontFamily={mono}
          fontSize="9"
          fill={cc.dim}
          dominantBaseline="middle"
        >
          client
        </text>

        {/* the build gate: a teal barrier that stops the break at the line */}
        <rect
          x={213}
          y={50}
          width={6}
          height={100}
          fill={cc.accent}
          opacity={0.08}
        />
        <line
          x1={216}
          y1={50}
          x2={216}
          y2={150}
          stroke={cc.accent}
          strokeWidth="1.5"
          {...stroke1}
        />
        <line
          x1={210}
          y1={50}
          x2={222}
          y2={50}
          stroke={cc.accent}
          strokeWidth="1.5"
          {...stroke1}
        />
        <line
          x1={210}
          y1={150}
          x2={222}
          y2={150}
          stroke={cc.accent}
          strokeWidth="1.5"
          {...stroke1}
        />
        <text
          x={216}
          y={42}
          textAnchor="middle"
          fontFamily={mono}
          fontSize="10"
          letterSpacing="0.1em"
          fill={cc.accent}
        >
          build
        </text>

        {/* editor card: the source of the rename */}
        <rect
          x={12}
          y={42}
          width={150}
          height={94}
          rx={10}
          fill={cc.surface}
          stroke={cc.border}
          strokeWidth="1"
          {...stroke1}
        />
        <text x={22} y={61} fontFamily={mono} fontSize="9" fill={cc.dim}>
          ProductApi.cs
        </text>
        <text
          x={152}
          y={61}
          textAnchor="end"
          fontFamily={mono}
          fontSize="8"
          letterSpacing="0.06em"
          fill={cc.eyebrow}
        >
          C#
        </text>
        <line
          x1={20}
          y1={70}
          x2={154}
          y2={70}
          stroke={cc.border}
          strokeWidth="1"
          {...stroke1}
        />
        <text x={22} y={91} fontFamily={mono} fontSize="9" fill={cc.eyebrow}>
          [QueryType]
        </text>

        {/* the rename line: price -> cost, cost flagged coral */}
        <text x={22} y={114} fontFamily={mono} fontSize="10" fill={cc.dim}>
          price
        </text>
        <line
          x1={21}
          y1={110.5}
          x2={51}
          y2={110.5}
          stroke={cc.dim}
          strokeWidth="1"
          {...stroke1}
        />
        <text x={56} y={114} fontFamily={mono} fontSize="10" fill={cc.eyebrow}>
          &#8594;
        </text>
        <text x={72} y={114} fontFamily={mono} fontSize="10" fill={cc.coral}>
          cost
        </text>
        <path
          d="M72 120 q 2 -3 4 0 t 4 0 t 4 0 t 4 0 t 4 0 t 4 0"
          fill="none"
          stroke={cc.coral}
          strokeWidth="1"
          {...stroke1}
        />

        {/* the single broken wire: leaves cost, snaps at the gate */}
        <path
          d="M100 112 L204 112"
          fill="none"
          stroke={cc.coral}
          strokeWidth="1.5"
          {...stroke1}
        />
        <circle cx={204} cy={112} r={6} fill={cc.coral} opacity={0.12} />
        <path
          d="M204 112 L210 108"
          fill="none"
          stroke={cc.coral}
          strokeWidth="1.5"
          {...stroke1}
        />
        <path
          d="M204 112 L210 116"
          fill="none"
          stroke={cc.coral}
          strokeWidth="1.5"
          {...stroke1}
        />
      </svg>
    </div>
  );
}
