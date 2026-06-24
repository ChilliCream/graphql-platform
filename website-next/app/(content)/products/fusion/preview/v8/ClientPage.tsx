"use client";

import type { ReactNode } from "react";
import { motion, useReducedMotion, type Transition } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroFusion } from "@/src/nitro";

// Brand spectrum, allowed at most once per page. Used on the summit horizon.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

const ACCENT = "#5eead4";

// -----------------------------------------------------------------------------
// Small primitives
// -----------------------------------------------------------------------------

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

interface WaypointTagProps {
  readonly value: string;
  readonly label: string;
}

function WaypointTag({ value, label }: WaypointTagProps) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim inline-flex items-center gap-2 rounded-full border px-2.5 py-1 font-mono text-[10.5px] tracking-widest uppercase">
      <span className="text-cc-accent tabular-nums">{value}</span>
      <span aria-hidden className="bg-cc-card-border h-3 w-px" />
      <span>{label}</span>
    </span>
  );
}

// -----------------------------------------------------------------------------
// Ridge divider: a polyline mountain silhouette that draws on enter-view once.
// One peak per divider gets a soft teal radial glow (the "active" waypoint).
// -----------------------------------------------------------------------------

interface RidgeDividerProps {
  readonly index: string;
  readonly label: string;
  readonly activePeak: number; // 0..5
}

// A consistent set of six peaks across the page so the dividers feel like a
// single, continuous mountain range. y values are tuned for a flat baseline.
const RIDGE_PEAKS: readonly { x: number; y: number }[] = [
  { x: 60, y: 52 },
  { x: 210, y: 30 },
  { x: 360, y: 60 },
  { x: 510, y: 22 },
  { x: 660, y: 48 },
  { x: 810, y: 38 },
];

const RIDGE_VALLEYS: readonly { x: number; y: number }[] = [
  { x: 0, y: 78 },
  { x: 135, y: 70 },
  { x: 285, y: 76 },
  { x: 435, y: 66 },
  { x: 585, y: 72 },
  { x: 735, y: 68 },
  { x: 880, y: 78 },
];

function ridgePath(): string {
  const points: string[] = [];
  for (let i = 0; i < RIDGE_PEAKS.length; i++) {
    points.push(`${RIDGE_VALLEYS[i].x},${RIDGE_VALLEYS[i].y}`);
    points.push(`${RIDGE_PEAKS[i].x},${RIDGE_PEAKS[i].y}`);
  }
  points.push(
    `${RIDGE_VALLEYS[RIDGE_VALLEYS.length - 1].x},${
      RIDGE_VALLEYS[RIDGE_VALLEYS.length - 1].y
    }`,
  );
  return `M ${points.join(" L ")}`;
}

function RidgeDivider({ index, label, activePeak }: RidgeDividerProps) {
  const reduced = useReducedMotion();
  const path = ridgePath();
  const peak =
    RIDGE_PEAKS[Math.max(0, Math.min(RIDGE_PEAKS.length - 1, activePeak))];

  const draw: Transition = reduced
    ? { duration: 0 }
    : { duration: 0.9, ease: "easeOut" };

  const chevron: Transition = reduced
    ? { duration: 0 }
    : { duration: 0.45, ease: "easeOut", delay: 0.15 };

  return (
    <div className="relative py-10" aria-hidden>
      <div className="relative">
        {/* radial glow under the active peak */}
        <div
          className="pointer-events-none absolute inset-0"
          style={{
            background: `radial-gradient(160px 80px at ${
              (peak.x / 880) * 100
            }% 60%, rgba(94,234,212,0.14), transparent 70%)`,
          }}
        />
        <svg
          viewBox="0 0 880 90"
          preserveAspectRatio="none"
          className="block h-[68px] w-full sm:h-[78px]"
          role="presentation"
        >
          {/* baseline elevation hairline */}
          <line
            x1="0"
            x2="880"
            y1="78"
            y2="78"
            stroke="rgba(245,241,234,0.08)"
            strokeWidth="1"
          />

          {/* ghost ridge behind, low opacity */}
          <path
            d={path}
            fill="none"
            stroke={ACCENT}
            strokeOpacity="0.18"
            strokeWidth="1"
            strokeLinejoin="round"
          />

          {/* animated front ridge */}
          <motion.path
            d={path}
            fill="none"
            stroke={ACCENT}
            strokeOpacity="0.55"
            strokeWidth="1.25"
            strokeLinejoin="round"
            initial={{
              pathLength: reduced ? 1 : 0,
              opacity: reduced ? 1 : 0.4,
            }}
            whileInView={{ pathLength: 1, opacity: 1 }}
            viewport={{ once: true, amount: 0.5 }}
            transition={draw}
          />

          {/* peak ticks */}
          {RIDGE_PEAKS.map((p, i) => (
            <circle
              key={i}
              cx={p.x}
              cy={p.y}
              r={i === activePeak ? 2.6 : 1.6}
              fill={ACCENT}
              fillOpacity={i === activePeak ? 0.95 : 0.45}
            />
          ))}
        </svg>

        {/* waypoint label, sits under the active peak */}
        <motion.div
          className="absolute inset-x-0 top-1/2 -translate-y-1/2"
          initial={{ opacity: reduced ? 1 : 0, y: reduced ? 0 : -4 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, amount: 0.5 }}
          transition={chevron}
        >
          <div
            className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.22em] uppercase"
            style={{
              position: "absolute",
              left: `${(peak.x / 880) * 100}%`,
              transform: "translate(-50%, -32px)",
              whiteSpace: "nowrap",
            }}
          >
            <span className="text-cc-accent tabular-nums">{index}</span>
            <span className="text-cc-ink-dim/70 mx-1.5">/</span>
            <span>{label}</span>
          </div>
        </motion.div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Hero panorama: three overlapping ridge silhouettes, drawn in sequence on
// mount only. No scroll coupling.
// -----------------------------------------------------------------------------

interface HeroPanoramaProps {
  readonly peaks: readonly { label: string; eyebrow: string }[];
}

function HeroPanorama({ peaks }: HeroPanoramaProps) {
  const reduced = useReducedMotion();

  // Three layers, each a polyline with its own opacity, offset, and amplitude.
  const layers = [
    {
      opacity: 0.08,
      path: "M 0 200 L 60 130 L 140 170 L 230 90 L 320 160 L 420 110 L 520 170 L 620 80 L 720 150 L 820 110 L 900 160 L 960 200 Z",
      delay: 0,
    },
    {
      opacity: 0.18,
      path: "M 0 210 L 50 160 L 130 190 L 220 120 L 310 180 L 410 140 L 510 190 L 610 110 L 710 170 L 810 140 L 900 180 L 960 210 Z",
      delay: 0.15,
    },
    {
      opacity: 0.35,
      // The "front" ridge holds the six labeled waypoints, peak x coords below.
      path: "M 0 220 L 80 170 L 200 130 L 340 180 L 480 120 L 620 160 L 760 110 L 900 170 L 960 220 Z",
      delay: 0.3,
    },
  ];

  // x positions of the six labeled peaks across the panorama (viewBox width 960).
  const labeled = [
    { x: 80, y: 170 },
    { x: 200, y: 130 },
    { x: 340, y: 180 },
    { x: 480, y: 120 },
    { x: 620, y: 160 },
    { x: 760, y: 110 },
  ];

  return (
    <div className="relative w-full overflow-hidden">
      {/* atmospheric vignette */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          background:
            "radial-gradient(700px 240px at 50% 110%, rgba(94,234,212,0.10), transparent 70%)",
        }}
      />
      <svg
        viewBox="0 0 960 240"
        className="block h-[200px] w-full sm:h-[260px]"
        role="img"
        aria-label="A six peak ridge panorama, each peak a stage in the Fusion lifecycle"
      >
        {layers.map((layer, idx) => (
          <motion.path
            key={idx}
            d={layer.path}
            fill={ACCENT}
            fillOpacity="0"
            stroke={ACCENT}
            strokeOpacity={layer.opacity}
            strokeWidth="1.25"
            strokeLinejoin="round"
            initial={{
              opacity: reduced ? 1 : 0,
              pathLength: reduced ? 1 : 0,
            }}
            animate={{ opacity: 1, pathLength: 1 }}
            transition={
              reduced
                ? { duration: 0 }
                : { duration: 1.1, delay: layer.delay, ease: "easeOut" }
            }
          />
        ))}

        {/* labeled peak markers on the front ridge */}
        {labeled.map((p, i) => (
          <g key={i}>
            <line
              x1={p.x}
              x2={p.x}
              y1={p.y}
              y2={p.y - 14}
              stroke={ACCENT}
              strokeOpacity="0.45"
              strokeWidth="1"
            />
            <circle
              cx={p.x}
              cy={p.y - 16}
              r="2.4"
              fill={ACCENT}
              fillOpacity={i === 5 ? "0.95" : "0.6"}
            />
            <text
              x={p.x}
              y={p.y - 22}
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="9"
              fill={i === 5 ? ACCENT : "rgba(245,241,234,0.62)"}
            >
              {peaks[i].eyebrow}
            </text>
            <text
              x={p.x}
              y={p.y + 18}
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="9.5"
              fill="rgba(245,241,234,0.45)"
            >
              {peaks[i].label}
            </text>
          </g>
        ))}

        {/* "satisfiable" chip parked at the summit (rightmost labeled peak) */}
        <g transform="translate(720 78)">
          <rect
            width="84"
            height="18"
            rx="9"
            fill="rgba(94,234,212,0.10)"
            stroke={ACCENT}
            strokeOpacity="0.55"
          />
          <text
            x="42"
            y="13"
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9"
            fill={ACCENT}
          >
            satisfiable
          </text>
        </g>

        {/* faint baseline */}
        <line
          x1="0"
          x2="960"
          y1="232"
          y2="232"
          stroke="rgba(245,241,234,0.08)"
          strokeWidth="1"
        />
      </svg>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Left rail trail-map TOC, sticky on lg.
// -----------------------------------------------------------------------------

interface TrailWaypoint {
  readonly id: string;
  readonly index: string;
  readonly label: string;
}

interface TrailRailProps {
  readonly waypoints: readonly TrailWaypoint[];
}

function TrailRail({ waypoints }: TrailRailProps) {
  const reduced = useReducedMotion();
  return (
    <nav
      aria-label="Page sections"
      className="hidden lg:sticky lg:top-24 lg:block lg:self-start"
    >
      <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.22em] uppercase">
        trail map
      </div>
      <ol className="relative mt-4 flex flex-col gap-3 pl-5">
        <span
          aria-hidden
          className="absolute top-1 bottom-1 left-1.5 w-px"
          style={{ background: "rgba(94,234,212,0.35)" }}
        />
        {waypoints.map((w, i) => (
          <motion.li
            key={w.id}
            initial={{ opacity: reduced ? 1 : 0, x: reduced ? 0 : -4 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true, amount: 0.5 }}
            transition={
              reduced ? { duration: 0 } : { duration: 0.35, delay: i * 0.04 }
            }
            className="relative"
          >
            <svg
              aria-hidden
              viewBox="0 0 8 8"
              className="absolute top-1.5 -left-[14px] h-2 w-2"
            >
              <path
                d="M 1 1 L 6 4 L 1 7"
                fill="none"
                stroke={ACCENT}
                strokeWidth="1.4"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
            <a
              href={`#${w.id}`}
              className="group block leading-snug no-underline"
            >
              <div className="text-cc-accent font-mono text-[10.5px] tabular-nums">
                {w.index}
              </div>
              <div className="text-cc-ink group-hover:text-cc-heading font-mono text-[11.5px] tracking-tight uppercase transition-colors">
                {w.label}
              </div>
            </a>
          </motion.li>
        ))}
      </ol>
    </nav>
  );
}

// -----------------------------------------------------------------------------
// Elevation strip used to the right of feature cards: ascending steps with a
// planted flag at the summit.
// -----------------------------------------------------------------------------

interface ElevationStripProps {
  readonly steps: readonly string[];
  readonly flag: string;
}

function ElevationStrip({ steps, flag }: ElevationStripProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 relative h-full rounded-lg border p-4">
      <div className="text-cc-ink-dim font-mono text-[10px] tracking-widest uppercase">
        elevation
      </div>
      <svg
        viewBox="0 0 200 220"
        className="mt-3 block h-auto w-full"
        role="img"
        aria-label={`Elevation strip with stages culminating in ${flag}`}
      >
        {/* baseline */}
        <line
          x1="8"
          x2="192"
          y1="200"
          y2="200"
          stroke="rgba(245,241,234,0.16)"
          strokeWidth="1"
        />
        {steps.map((s, i) => {
          const x = 12 + (i * 184) / Math.max(1, steps.length - 1);
          const y = 200 - ((i + 1) * 150) / steps.length;
          const nextX =
            i < steps.length - 1
              ? 12 + ((i + 1) * 184) / Math.max(1, steps.length - 1)
              : null;
          const nextY =
            i < steps.length - 1 ? 200 - ((i + 2) * 150) / steps.length : null;
          const isSummit = i === steps.length - 1;
          return (
            <g key={s}>
              {nextX !== null && nextY !== null && (
                <line
                  x1={x}
                  y1={y}
                  x2={nextX}
                  y2={nextY}
                  stroke={ACCENT}
                  strokeOpacity="0.5"
                  strokeWidth="1.2"
                />
              )}
              <circle
                cx={x}
                cy={y}
                r={isSummit ? "4" : "2.6"}
                fill={ACCENT}
                fillOpacity={isSummit ? "0.95" : "0.6"}
              />
              <text
                x={x}
                y={y - 8}
                textAnchor="middle"
                fontFamily="ui-monospace, monospace"
                fontSize="8.5"
                fill={isSummit ? ACCENT : "rgba(245,241,234,0.62)"}
              >
                {s}
              </text>
              {isSummit && (
                <>
                  <line
                    x1={x}
                    y1={y - 12}
                    x2={x}
                    y2={y - 28}
                    stroke={ACCENT}
                    strokeOpacity="0.85"
                    strokeWidth="1.2"
                  />
                  <path
                    d={`M ${x} ${y - 28} L ${x + 14} ${y - 24} L ${x} ${y - 20} Z`}
                    fill={ACCENT}
                    fillOpacity="0.85"
                  />
                </>
              )}
            </g>
          );
        })}
      </svg>
      <div className="text-cc-ink-dim mt-3 font-mono text-[10px] tracking-widest uppercase">
        summit, {flag}
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Inline diagrams: a tighter, ridge-flavored variant for the paired blocks.
// -----------------------------------------------------------------------------

function SatisfiabilityRidge() {
  return (
    <svg
      viewBox="0 0 320 160"
      className="block h-auto w-full"
      role="img"
      aria-label="Three resolvable trails reaching three summits"
    >
      <line
        x1="0"
        x2="320"
        y1="140"
        y2="140"
        stroke="rgba(245,241,234,0.1)"
        strokeWidth="1"
      />
      {/* ridge silhouette */}
      <path
        d="M 0 140 L 50 90 L 110 110 L 170 60 L 230 100 L 280 70 L 320 110 L 320 140 Z"
        fill="rgba(94,234,212,0.05)"
        stroke="rgba(94,234,212,0.4)"
        strokeWidth="1"
      />
      {/* root */}
      <circle cx="14" cy="140" r="3" fill={ACCENT} fillOpacity="0.9" />
      <text
        x="14"
        y="156"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9"
        fill="rgba(245,241,234,0.6)"
      >
        Query
      </text>
      {/* three resolvable paths to three summits */}
      {[
        { peak: [170, 60], label: "order(id)" },
        { peak: [230, 100], label: "order.items" },
        { peak: [280, 70], label: "order.shipping" },
      ].map((t) => (
        <g key={t.label}>
          <path
            d={`M 14 140 Q ${t.peak[0] / 2} 130, ${t.peak[0]} ${t.peak[1]}`}
            fill="none"
            stroke={ACCENT}
            strokeOpacity="0.55"
            strokeWidth="1.2"
            strokeDasharray="3 3"
          />
          <circle
            cx={t.peak[0]}
            cy={t.peak[1]}
            r="3"
            fill={ACCENT}
            fillOpacity="0.9"
          />
          <text
            x={t.peak[0]}
            y={t.peak[1] - 8}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9"
            fill={ACCENT}
          >
            {t.label}
          </text>
        </g>
      ))}
    </svg>
  );
}

function FederationConvergeRidge() {
  return (
    <svg
      viewBox="0 0 320 160"
      className="block h-auto w-full"
      role="img"
      aria-label="Apollo Federation and Hot Chocolate trails converge onto one ridge"
    >
      <line
        x1="0"
        x2="320"
        y1="140"
        y2="140"
        stroke="rgba(245,241,234,0.1)"
        strokeWidth="1"
      />
      <path
        d="M 0 140 L 60 80 L 120 110 L 180 60 L 240 90 L 320 140 Z"
        fill="rgba(94,234,212,0.05)"
        stroke="rgba(94,234,212,0.4)"
        strokeWidth="1"
      />
      {/* Apollo trail */}
      <path
        d="M 10 138 Q 90 120, 180 60"
        fill="none"
        stroke="rgba(245,241,234,0.45)"
        strokeWidth="1.2"
        strokeDasharray="3 3"
      />
      <text
        x="10"
        y="156"
        fontFamily="ui-monospace, monospace"
        fontSize="9"
        fill="rgba(245,241,234,0.55)"
      >
        Apollo Federation v2
      </text>
      {/* Hot Chocolate trail */}
      <path
        d="M 130 138 Q 160 100, 180 60"
        fill="none"
        stroke="rgba(245,241,234,0.45)"
        strokeWidth="1.2"
        strokeDasharray="3 3"
      />
      <text
        x="130"
        y="156"
        fontFamily="ui-monospace, monospace"
        fontSize="9"
        fill="rgba(245,241,234,0.55)"
      >
        Hot Chocolate
      </text>
      {/* converged summit flag */}
      <circle cx="180" cy="60" r="4" fill={ACCENT} fillOpacity="0.95" />
      <line
        x1="180"
        y1="56"
        x2="180"
        y2="36"
        stroke={ACCENT}
        strokeOpacity="0.85"
        strokeWidth="1.2"
      />
      <path d="M 180 36 L 198 40 L 180 44 Z" fill={ACCENT} fillOpacity="0.85" />
      <text
        x="180"
        y="30"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9"
        fill={ACCENT}
      >
        Composite Schemas
      </text>
    </svg>
  );
}

// Topographic plan: parallel branches and a batched merge, laid out as a
// stylized trail map.
function PlanTopography() {
  return (
    <svg
      viewBox="0 0 720 280"
      className="block h-auto w-full"
      role="img"
      aria-label="Topographic trail map showing parallel branches and a batched merge"
    >
      {/* contour lines, faint */}
      {[40, 80, 120, 160, 200, 240].map((y) => (
        <line
          key={y}
          x1="0"
          x2="720"
          y1={y}
          y2={y}
          stroke="rgba(245,241,234,0.05)"
          strokeWidth="1"
        />
      ))}

      {/* client op */}
      <rect
        x="20"
        y="124"
        width="100"
        height="32"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke={ACCENT}
        strokeOpacity="0.55"
      />
      <text
        x="70"
        y="144"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill={ACCENT}
      >
        client op
      </text>

      {/* parallel branches */}
      {[
        { y: 60, label: "fetch catalog", ms: "12ms" },
        { y: 110, label: "fetch checkout", ms: "10ms" },
        { y: 160, label: "fetch reviews", ms: "9ms" },
      ].map((n) => (
        <g key={n.label}>
          <path
            d={`M 120 140 C 180 140, 190 ${n.y + 14}, 240 ${n.y + 14}`}
            fill="none"
            stroke={ACCENT}
            strokeOpacity="0.5"
            strokeWidth="1.2"
          />
          <rect
            x="240"
            y={n.y}
            width="180"
            height="28"
            rx="4"
            fill="rgba(245,241,234,0.04)"
            stroke="rgba(245,241,234,0.18)"
          />
          <text
            x="252"
            y={n.y + 18}
            fontFamily="ui-monospace, monospace"
            fontSize="10.5"
            fill="rgba(245,241,234,0.7)"
          >
            {n.label}
          </text>
          <text
            x="412"
            y={n.y + 18}
            textAnchor="end"
            fontFamily="ui-monospace, monospace"
            fontSize="10"
            fill={ACCENT}
          >
            {n.ms}
          </text>
        </g>
      ))}

      {/* converge to batched stage */}
      {[60, 110, 160].map((y) => (
        <path
          key={y}
          d={`M 420 ${y + 14} C 480 ${y + 14}, 500 220, 540 220`}
          fill="none"
          stroke={ACCENT}
          strokeOpacity="0.5"
          strokeWidth="1.2"
          strokeDasharray="3 3"
        />
      ))}

      <rect
        x="440"
        y="200"
        width="260"
        height="44"
        rx="6"
        fill="rgba(94,234,212,0.08)"
        stroke={ACCENT}
        strokeOpacity="0.55"
      />
      <text
        x="452"
        y="222"
        fontFamily="ui-monospace, monospace"
        fontSize="11"
        fill="#f5f0ea"
      >
        batch reviews(productIds: [...])
      </text>
      <text
        x="452"
        y="236"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.6)"
      >
        one HTTP/2 call, no N+1 across the graph
      </text>

      {/* legend */}
      <g transform="translate(560 30)">
        <text
          x="0"
          y="0"
          fontFamily="ui-monospace, monospace"
          fontSize="9.5"
          fill="rgba(245,241,234,0.55)"
        >
          legend
        </text>
        <line
          x1="0"
          y1="14"
          x2="20"
          y2="14"
          stroke={ACCENT}
          strokeOpacity="0.55"
          strokeWidth="1.2"
        />
        <text
          x="26"
          y="17"
          fontFamily="ui-monospace, monospace"
          fontSize="9"
          fill="rgba(245,241,234,0.7)"
        >
          parallel
        </text>
        <line
          x1="0"
          y1="30"
          x2="20"
          y2="30"
          stroke={ACCENT}
          strokeOpacity="0.55"
          strokeWidth="1.2"
          strokeDasharray="3 3"
        />
        <text
          x="26"
          y="33"
          fontFamily="ui-monospace, monospace"
          fontSize="9"
          fill="rgba(245,241,234,0.7)"
        >
          batched
        </text>
      </g>
    </svg>
  );
}

// Tiny ASP.NET pipeline ridge: one highlighted peak for Fusion.
function DotNetPipelineRidge() {
  const peaks = ["AuthN", "Headers", "Fusion", "Cache", "Telemetry"];
  return (
    <svg
      viewBox="0 0 480 160"
      className="block h-auto w-full"
      role="img"
      aria-label="ASP.NET Core pipeline ridge with the Fusion peak highlighted"
    >
      <line
        x1="0"
        x2="480"
        y1="140"
        y2="140"
        stroke="rgba(245,241,234,0.1)"
        strokeWidth="1"
      />
      <path
        d="M 0 140 L 80 100 L 160 120 L 240 50 L 320 110 L 400 90 L 480 140 Z"
        fill="rgba(94,234,212,0.05)"
        stroke="rgba(94,234,212,0.4)"
        strokeWidth="1"
      />
      {peaks.map((p, i) => {
        const x = 80 + i * 80;
        const y = [100, 120, 50, 110, 90][i];
        const active = p === "Fusion";
        return (
          <g key={p}>
            <circle
              cx={x}
              cy={y}
              r={active ? 4 : 2.6}
              fill={ACCENT}
              fillOpacity={active ? 0.95 : 0.55}
            />
            <text
              x={x}
              y={y - 10}
              textAnchor="middle"
              fontFamily="ui-monospace, monospace"
              fontSize="10"
              fill={active ? ACCENT : "rgba(245,241,234,0.62)"}
            >
              {p}
            </text>
          </g>
        );
      })}
      <text
        x="240"
        y="156"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="9.5"
        fill="rgba(245,241,234,0.5)"
      >
        ASP.NET Core middleware pipeline
      </text>
    </svg>
  );
}

// Self-run panorama: dashed network boundary with the gateway perched on the
// highest peak, subgraphs as smaller peaks.
function SelfRunPanorama() {
  return (
    <svg
      viewBox="0 0 720 280"
      className="block h-auto w-full"
      role="img"
      aria-label="Your network boundary contains a panorama with the gateway on the highest peak"
    >
      <rect
        x="10"
        y="14"
        width="700"
        height="252"
        rx="12"
        fill="rgba(245,241,234,0.02)"
        stroke="rgba(245,241,234,0.22)"
        strokeDasharray="4 4"
      />
      <text
        x="28"
        y="36"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.55)"
      >
        your network
      </text>
      {/* baseline */}
      <line
        x1="40"
        x2="690"
        y1="230"
        y2="230"
        stroke="rgba(245,241,234,0.1)"
        strokeWidth="1"
      />
      {/* range */}
      <path
        d="M 40 230 L 110 180 L 180 200 L 240 160 L 320 100 L 410 160 L 490 140 L 570 180 L 640 150 L 690 200 L 690 230 Z"
        fill="rgba(94,234,212,0.05)"
        stroke="rgba(94,234,212,0.4)"
        strokeWidth="1"
      />
      {/* subgraph peaks */}
      {[
        { x: 110, y: 180, label: "catalog" },
        { x: 240, y: 160, label: "checkout" },
        { x: 490, y: 140, label: "reviews" },
        { x: 640, y: 150, label: "search" },
      ].map((s) => (
        <g key={s.label}>
          <circle cx={s.x} cy={s.y} r="2.8" fill={ACCENT} fillOpacity="0.6" />
          <text
            x={s.x}
            y={s.y - 10}
            textAnchor="middle"
            fontFamily="ui-monospace, monospace"
            fontSize="9.5"
            fill="rgba(245,241,234,0.6)"
          >
            {s.label}
          </text>
        </g>
      ))}
      {/* gateway on the highest peak */}
      <g transform="translate(320 100)">
        <circle r="5" fill={ACCENT} fillOpacity="0.95" />
        <line
          x1="0"
          y1="-5"
          x2="0"
          y2="-30"
          stroke={ACCENT}
          strokeOpacity="0.9"
          strokeWidth="1.4"
        />
        <path d="M 0 -30 L 22 -25 L 0 -20 Z" fill={ACCENT} fillOpacity="0.9" />
        <text
          x="0"
          y="-38"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="11"
          fill={ACCENT}
        >
          Fusion gateway
        </text>
        <text
          x="0"
          y="20"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="9.5"
          fill="rgba(245,241,234,0.6)"
        >
          ASP.NET Core
        </text>
      </g>
      <text
        x="28"
        y="256"
        fontFamily="ui-monospace, monospace"
        fontSize="10"
        fill="rgba(245,241,234,0.5)"
      >
        no hosted hop
      </text>
    </svg>
  );
}

// -----------------------------------------------------------------------------
// Console card, reused for the CI compose and Program.cs blocks.
// -----------------------------------------------------------------------------

interface ConsoleCardProps {
  readonly file: string;
  readonly tag: string;
  readonly children: ReactNode;
}

function ConsoleCard({ file, tag, children }: ConsoleCardProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
        <span
          className="bg-cc-status-firing h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-investigating h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span
          className="bg-cc-status-healthy h-2.5 w-2.5 rounded-full opacity-70"
          aria-hidden
        />
        <span className="text-cc-ink-dim ml-3 font-mono text-[11px]">
          {file}
        </span>
        <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-wider uppercase">
          {tag}
        </span>
      </div>
      <pre className="text-cc-ink overflow-x-auto px-5 py-4 font-mono text-[12.5px] leading-6">
        {children}
      </pre>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Bullet list, with a small teal check.
// -----------------------------------------------------------------------------

interface BulletsProps {
  readonly items: readonly string[];
}

function Bullets({ items }: BulletsProps) {
  return (
    <ul className="mt-5 flex flex-col gap-2.5">
      {items.map((b) => (
        <li
          key={b}
          className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
        >
          <span className="text-cc-accent mt-1 shrink-0">
            <CheckIcon size={14} />
          </span>
          <span>{b}</span>
        </li>
      ))}
    </ul>
  );
}

// -----------------------------------------------------------------------------
// Capability card with a vertical elevation strip on the left, used in the
// 6-up basecamp grid. Cards stagger vertically (subtle staircase).
// -----------------------------------------------------------------------------

interface CapabilityCardProps {
  readonly index: string;
  readonly label: string;
  readonly proof: string;
}

function CapabilityCard({ index, label, proof }: CapabilityCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative flex gap-3 rounded-lg border p-4">
      {/* left elevation strip */}
      <div className="flex w-[14px] shrink-0 flex-col items-center gap-1 pt-1">
        <span
          aria-hidden
          className="w-px flex-1"
          style={{ background: "rgba(94,234,212,0.35)" }}
        />
        <svg viewBox="0 0 8 6" className="h-1.5 w-2">
          <path
            d="M 1 5 L 4 1 L 7 5"
            fill="none"
            stroke={ACCENT}
            strokeOpacity="0.85"
            strokeWidth="1.2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </div>
      <div className="flex flex-col">
        <span className="text-cc-accent font-mono text-[10.5px] tabular-nums">
          {index}
        </span>
        <span className="text-cc-heading font-heading mt-1 text-base leading-tight font-semibold tracking-tight">
          {label}
        </span>
        <span className="text-cc-ink-dim mt-1.5 font-mono text-[11px] leading-snug">
          {proof}
        </span>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

const WAYPOINTS: readonly TrailWaypoint[] = [
  { id: "basecamp", index: "00", label: "Basecamp" },
  { id: "composition", index: "01", label: "Composition" },
  { id: "satisfiability", index: "02", label: "Satisfiability" },
  { id: "federation", index: "03", label: "Federation" },
  { id: "plan", index: "04", label: "Query plan" },
  { id: "dotnet", index: "05", label: ".NET-native" },
  { id: "self-run", index: "06", label: "Self-run" },
  { id: "trace", index: "07", label: "Trace at altitude" },
  { id: "summit", index: "08", label: "Summit" },
];

const HERO_PEAKS = [
  { eyebrow: "01", label: "catalog" },
  { eyebrow: "02", label: "compose" },
  { eyebrow: "03", label: "satisfy" },
  { eyebrow: "04", label: "plan" },
  { eyebrow: "05", label: "gateway" },
  { eyebrow: "06", label: "self-run" },
];

export function ClientPage() {
  return (
    <div className="relative">
      {/* HERO: eyebrow + h1 + lead + dual CTA, full-bleed ridge panorama below */}
      <section id="basecamp" className="scroll-mt-24 pt-12 pb-6 sm:pt-20">
        <div className="max-w-4xl">
          <Eyebrow>Ridge walk, distributed GraphQL gateway</Eyebrow>
          <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
            From many peaks to one summit, a composed graph proven before you
            ship.
          </h1>
          <p className="text-cc-prose mt-6 max-w-2xl text-lg leading-relaxed">
            Fusion is ChilliCream&apos;s distributed GraphQL gateway. Point it
            at independent subgraphs, compose them into one composite schema in
            CI, and ship a versioned plan that is proven answerable before a
            client ever sends a query. Built on Hot Chocolate, run as your own
            ASP.NET Core app.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <SolidButton href="/docs/fusion">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>
          <dl className="border-cc-card-border mt-10 grid max-w-2xl grid-cols-3 gap-6 border-t pt-6">
            <div>
              <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                License
              </dt>
              <dd className="text-cc-ink mt-1 text-sm">MIT</dd>
            </div>
            <div>
              <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                Runtime
              </dt>
              <dd className="text-cc-ink mt-1 text-sm">ASP.NET Core</dd>
            </div>
            <div>
              <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                Spec
              </dt>
              <dd className="text-cc-ink mt-1 text-sm">Composite Schemas</dd>
            </div>
          </dl>
        </div>
        <div className="mt-12">
          <HeroPanorama peaks={HERO_PEAKS} />
        </div>
      </section>

      {/* Main body: trail rail + stacked single column content */}
      <div className="grid grid-cols-1 gap-10 lg:grid-cols-[11rem_minmax(0,1fr)] lg:gap-14">
        <TrailRail waypoints={WAYPOINTS} />

        <div className="min-w-0">
          {/* Ridge divider 01 + Basecamp / Capabilities */}
          <RidgeDivider index="01" label="basecamp" activePeak={0} />
          <section
            id="capabilities"
            aria-label="Capabilities at a glance"
            className="pb-10 sm:pb-16"
          >
            <div className="flex items-center gap-3">
              <WaypointTag value="00" label="capabilities" />
              <Eyebrow>Basecamp</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              The six waypoints, named at basecamp.
            </h2>
            <div className="mt-8 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {[
                {
                  i: "01",
                  label: "Build-time composition",
                  proof: "phases run in CI, gateway loads a .far archive",
                  offset: "lg:translate-y-0",
                },
                {
                  i: "02",
                  label: "Satisfiability proof",
                  proof: "every reachable field has a resolver path",
                  offset: "lg:translate-y-4",
                },
                {
                  i: "03",
                  label: "Federation v2 interop",
                  proof: "@key, @requires, @provides read directly",
                  offset: "lg:-translate-y-2",
                },
                {
                  i: "04",
                  label: "Distributed query plan",
                  proof: "parallel fan-out, batched entity fetches",
                  offset: "lg:translate-y-6",
                },
                {
                  i: "05",
                  label: ".NET-native gateway",
                  proof: "AddGraphQLGateway() on ASP.NET Core",
                  offset: "lg:translate-y-1",
                },
                {
                  i: "06",
                  label: "Self-run, always",
                  proof: "your network, no hosted hop",
                  offset: "lg:translate-y-5",
                },
              ].map((c) => (
                <div key={c.i} className={c.offset}>
                  <CapabilityCard index={c.i} label={c.label} proof={c.proof} />
                </div>
              ))}
            </div>
          </section>

          {/* Ridge divider 02 + Waypoint 01 Composition */}
          <RidgeDivider index="02" label="composition" activePeak={1} />
          <section id="composition" className="scroll-mt-24 pb-10 sm:pb-16">
            <div className="flex items-center gap-3">
              <WaypointTag value="01" label="composition" />
              <Eyebrow>Waypoint</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Composition runs in CI, not on a hot path.
            </h2>
            <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,1fr)_14rem]">
              <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-6">
                <p className="text-cc-prose text-base leading-relaxed sm:text-lg">
                  A composition pipeline reads each subgraph SDL, validates it
                  against the others, and emits a Fusion archive your gateway
                  loads at startup. Type, enum, and field conflicts surface as
                  diagnostics with stable codes, on the build server, before
                  deploy. The gateway never sees raw source schemas.
                </p>
                <Bullets
                  items={[
                    "Runs fully offline as a build step or in CI. Nitro cloud is optional, not in the request path.",
                    "Halts on the first failing phase with a stable diagnostic code you can match in scripts.",
                    "Emits a versioned, inspectable .far artifact you can diff between releases.",
                  ]}
                />
                <div className="mt-6">
                  <ConsoleCard file="ci/compose.sh" tag="shell">
                    <span style={{ color: "#8b949e" }}>
                      {
                        "# Compose subgraphs and fail the build on any conflict.\n"
                      }
                    </span>
                    <span style={{ color: "#ff7b72" }}>nitro</span>
                    <span style={{ color: "#c9d1d9" }}> fusion compose \\</span>
                    {"\n"}
                    <span style={{ color: "#c9d1d9" }}>
                      {"  --subgraph catalog=./catalog.graphql \\"}
                    </span>
                    {"\n"}
                    <span style={{ color: "#c9d1d9" }}>
                      {"  --subgraph checkout=./checkout.graphql \\"}
                    </span>
                    {"\n"}
                    <span style={{ color: "#c9d1d9" }}>
                      {"  --subgraph reviews=./reviews.graphql \\"}
                    </span>
                    {"\n"}
                    <span style={{ color: "#c9d1d9" }}>
                      {"  --output ./gateway.far"}
                    </span>
                    {"\n\n"}
                    <span style={{ color: "#5eead4" }}>{"OK"}</span>
                    <span style={{ color: "#c9d1d9" }}>
                      {" composed 3 subgraphs, 0 errors, "}
                    </span>
                    <span style={{ color: "#a5d6ff" }}>gateway.far</span>
                    <span style={{ color: "#c9d1d9" }}>{" written"}</span>
                  </ConsoleCard>
                </div>
              </div>
              <ElevationStrip
                steps={["parse", "enrich", "validate", "merge", "satisfy"]}
                flag="gateway.far"
              />
            </div>
          </section>

          {/* Ridge divider 03 + paired Sat + Federation */}
          <RidgeDivider index="03" label="reachability" activePeak={2} />
          <section className="pb-10 sm:pb-16">
            <div className="grid gap-6 lg:grid-cols-2">
              <article
                id="satisfiability"
                className="border-cc-card-border bg-cc-card-bg scroll-mt-24 rounded-xl border p-6"
              >
                <div className="flex items-center gap-3">
                  <WaypointTag value="02" label="satisfiability" />
                </div>
                <h2 className="text-cc-heading font-heading mt-4 text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                  If it composes, it answers.
                </h2>
                <p className="text-cc-prose mt-3 text-base leading-relaxed">
                  Composition&apos;s final phase walks every reachable field
                  from the root types and proves it can be resolved across your
                  subgraphs given the available lookups and keys. Unreachable
                  shapes fail composition with UNSATISFIABLE_QUERY_PATH.
                </p>
                <div className="mt-5">
                  <SatisfiabilityRidge />
                </div>
                <Bullets
                  items={[
                    "Reachability analysis over the full composed graph, shipped in Fusion.Composition.Satisfiability.",
                    "Catches contract drift between subgraphs before a client ever sends the query.",
                    "Failures cite the exact field path, so the broken shape is the next thing you fix.",
                  ]}
                />
              </article>

              <article
                id="federation"
                className="border-cc-card-border bg-cc-card-bg scroll-mt-24 rounded-xl border p-6"
              >
                <div className="flex items-center gap-3">
                  <WaypointTag value="03" label="federation" />
                </div>
                <h2 className="text-cc-heading font-heading mt-4 text-2xl font-semibold tracking-tight text-balance sm:text-3xl">
                  Apollo Federation spec compatible, on an open standard.
                </h2>
                <p className="text-cc-prose mt-3 text-base leading-relaxed">
                  Fusion implements the GraphQL Composite Schemas specification
                  under the GraphQL Foundation, and reads Apollo Federation v2
                  subgraphs through a dedicated connector. Bring existing @key,
                  @requires, and @provides directives into a Fusion composition
                  without rewriting resolvers.
                </p>
                <div className="mt-5">
                  <FederationConvergeRidge />
                </div>
                <Bullets
                  items={[
                    "GraphQL Composite Schemas spec, vendor-neutral. Subgraph schemas stay portable.",
                    "Apollo Federation v2 interop via Fusion.Connectors.ApolloFederation.",
                    "Documented migration path from Federation v2 with directive-by-directive mapping.",
                  ]}
                />
              </article>
            </div>
          </section>

          {/* Ridge divider 04 + Waypoint 04 Query Plan */}
          <RidgeDivider index="04" label="plan" activePeak={3} />
          <section id="plan" className="scroll-mt-24 pb-10 sm:pb-16">
            <div className="flex items-center gap-3">
              <WaypointTag value="04" label="query plan" />
              <Eyebrow>Waypoint</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              One client request, a planned distributed fetch.
            </h2>
            <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,18rem)_minmax(0,1fr)]">
              <div>
                <p className="text-cc-prose text-base leading-relaxed">
                  The gateway compiles each incoming operation into a query plan
                  over your subgraphs. Independent fetches run in parallel,
                  dependent fetches sequence behind them, and shared entity keys
                  are batched into single HTTP/2 calls.
                </p>
                <Bullets
                  items={[
                    "Parallel fan-out where fetches are independent, sequencing only where a fetch needs prior data.",
                    "Entity keys collected across the plan and batched, no N+1 across services.",
                    "Persisted operations and conservative cache control merged from subgraph policies.",
                  ]}
                />
              </div>
              <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5">
                <PlanTopography />
              </div>
            </div>
          </section>

          {/* Ridge divider 05 + Waypoint 05 .NET-native */}
          <RidgeDivider index="05" label="gateway" activePeak={4} />
          <section id="dotnet" className="scroll-mt-24 pb-10 sm:pb-16">
            <div className="flex items-center gap-3">
              <WaypointTag value="05" label=".net-native" />
              <Eyebrow>Waypoint</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              The gateway is your code, on Hot Chocolate.
            </h2>
            <div className="mt-6 grid gap-6 lg:grid-cols-12">
              <div className="lg:col-span-5">
                <p className="text-cc-prose text-base leading-relaxed">
                  Fusion&apos;s gateway is an ASP.NET Core app, configured with
                  AddGraphQLGateway() and built on Hot Chocolate. Your DI
                  container, your authentication, your middleware, your logging.
                  No standalone binary, no YAML, no Node runtime in the request
                  path.
                </p>
                <Bullets
                  items={[
                    "AddGraphQLGateway() integrates with the ASP.NET Core middleware pipeline you already operate.",
                    "Auth, header propagation, and cache control land where you expect them in .NET.",
                    "An existing Hot Chocolate server is already a valid subgraph, no federation library needed.",
                  ]}
                />
                <div className="border-cc-card-border bg-cc-card-bg mt-6 rounded-xl border p-5">
                  <DotNetPipelineRidge />
                </div>
              </div>
              <div className="lg:col-span-7">
                <ConsoleCard file="Program.cs" tag="C#">
                  <span style={{ color: "#c9d1d9" }}>{"var builder = "}</span>
                  <span style={{ color: "#ffa657" }}>WebApplication</span>
                  <span style={{ color: "#c9d1d9" }}>{"."}</span>
                  <span style={{ color: "#d2a8ff" }}>CreateBuilder</span>
                  <span style={{ color: "#c9d1d9" }}>{"(args);"}</span>
                  {"\n\n"}
                  <span style={{ color: "#c9d1d9" }}>{"builder.Services"}</span>
                  {"\n"}
                  <span style={{ color: "#c9d1d9" }}>{"    ."}</span>
                  <span style={{ color: "#d2a8ff" }}>AddGraphQLGateway</span>
                  <span style={{ color: "#c9d1d9" }}>{"()"}</span>
                  {"\n"}
                  <span style={{ color: "#c9d1d9" }}>{"    ."}</span>
                  <span style={{ color: "#d2a8ff" }}>
                    AddFileSystemConfiguration
                  </span>
                  <span style={{ color: "#c9d1d9" }}>{"("}</span>
                  <span style={{ color: "#a5d6ff" }}>{'"./gateway.far"'}</span>
                  <span style={{ color: "#c9d1d9" }}>{");"}</span>
                  {"\n\n"}
                  <span style={{ color: "#c9d1d9" }}>
                    {"var app = builder."}
                  </span>
                  <span style={{ color: "#d2a8ff" }}>Build</span>
                  <span style={{ color: "#c9d1d9" }}>{"();"}</span>
                  {"\n"}
                  <span style={{ color: "#c9d1d9" }}>{"app."}</span>
                  <span style={{ color: "#d2a8ff" }}>MapGraphQL</span>
                  <span style={{ color: "#c9d1d9" }}>{"();"}</span>
                  {"\n"}
                  <span style={{ color: "#c9d1d9" }}>{"app."}</span>
                  <span style={{ color: "#d2a8ff" }}>Run</span>
                  <span style={{ color: "#c9d1d9" }}>{"();"}</span>
                </ConsoleCard>
              </div>
            </div>
          </section>

          {/* Ridge divider 06 + Waypoint 06 Self-run */}
          <RidgeDivider index="06" label="self-run" activePeak={5} />
          <section id="self-run" className="scroll-mt-24 pb-10 sm:pb-16">
            <div className="flex items-center gap-3">
              <WaypointTag value="06" label="self-run" />
              <Eyebrow>Waypoint</Eyebrow>
            </div>
            <h2 className="text-cc-heading font-heading mt-5 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              The gateway is always self-run, never a hosted hop.
            </h2>
            <div className="mt-6 grid gap-6 lg:grid-cols-12">
              <div className="lg:col-span-5">
                <p className="text-cc-prose text-base leading-relaxed">
                  Fusion runs in your infrastructure, period. Every client
                  request and every subgraph fetch stay inside your network
                  boundary. You choose the cluster, the auth, the egress, the
                  audit trail. Nitro cloud is available for managed composition
                  delivery, never as a hop in the request path.
                </p>
                <Bullets
                  items={[
                    "Runs in any environment that runs ASP.NET Core, on your own compute.",
                    "No third-party gateway in the request path, no data egress you did not approve.",
                    "Standard ASP.NET Core auth (JWT, cookie, OIDC, mTLS) and header propagation to subgraphs.",
                  ]}
                />
              </div>
              <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-5 lg:col-span-7">
                <SelfRunPanorama />
              </div>
            </div>
          </section>

          {/* Nitro trace at altitude */}
          <RidgeDivider index="07" label="altitude" activePeak={4} />
          <section id="trace" className="scroll-mt-24 pb-10 sm:pb-16">
            <div className="flex items-end gap-6 sm:flex-row sm:items-end">
              <div className="flex-1">
                <div className="flex items-center gap-3">
                  <WaypointTag value="07" label="trace" />
                  <Eyebrow>Altitude</Eyebrow>
                </div>
                <h2 className="text-cc-heading font-heading mt-4 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
                  The query plan, traced at altitude.
                </h2>
                <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
                  Fusion emits OpenTelemetry spans for each request, the
                  planning step, and every subgraph fetch. Nitro renders the
                  plan as a navigable trace, so when a single subgraph slows
                  down you see which step in the plan, which subgraph, and which
                  keys were in the batch.
                </p>
              </div>
              <div className="hidden lg:block lg:text-right">
                <p className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
                  spans: ExecuteRequest, PlanOperation, ExecutePlanNode
                </p>
              </div>
            </div>
            <div className="border-cc-card-border bg-cc-surface mx-auto mt-8 max-w-5xl overflow-hidden rounded-xl border">
              <NitroFusion />
            </div>
            {/* faint ridge baseline beneath the trace */}
            <div className="mt-2">
              <svg
                viewBox="0 0 880 24"
                preserveAspectRatio="none"
                className="block h-3 w-full"
                aria-hidden
              >
                <path
                  d="M 0 18 L 90 8 L 180 16 L 280 6 L 380 14 L 480 8 L 580 16 L 680 8 L 780 14 L 880 8"
                  fill="none"
                  stroke={ACCENT}
                  strokeOpacity="0.35"
                  strokeWidth="1"
                />
              </svg>
            </div>
          </section>

          {/* Summit / closing CTA */}
          <Summit />
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Summit: centered closing CTA with summit peak SVG and the one brand-spectrum
// horizon hairline.
// -----------------------------------------------------------------------------

function Summit() {
  const reduced = useReducedMotion();
  return (
    <section
      id="summit"
      className="border-cc-card-border relative scroll-mt-24 border-t pt-14 pb-20 sm:pt-20 sm:pb-28"
    >
      <div className="mx-auto max-w-3xl text-center">
        <div className="mx-auto flex justify-center">
          <WaypointTag value="08" label="summit" />
        </div>
        <h2 className="text-cc-heading font-heading mx-auto mt-5 max-w-3xl text-4xl font-semibold tracking-tight text-balance sm:text-5xl">
          One composite graph, proven before you ship it.
        </h2>
        <p className="text-cc-prose mx-auto mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
          Point Fusion at your subgraphs, compose in CI, and serve from a single
          .NET endpoint you operate yourself. The plan is built, the
          satisfiability is proven, and the runtime is the ASP.NET Core you
          already run.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <SolidButton href="/docs/fusion">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>

        {/* Summit peak with the spectrum horizon line beneath it */}
        <div className="relative mx-auto mt-12 w-full max-w-2xl">
          <svg
            viewBox="0 0 600 120"
            className="block h-auto w-full"
            aria-hidden
          >
            <path
              d="M 40 110 L 180 70 L 260 90 L 320 30 L 400 80 L 480 60 L 560 110"
              fill="none"
              stroke={ACCENT}
              strokeOpacity="0.35"
              strokeWidth="1.25"
              strokeLinejoin="round"
            />
            <circle cx="320" cy="30" r="4" fill={ACCENT} fillOpacity="0.95" />
            <line
              x1="320"
              y1="26"
              x2="320"
              y2="10"
              stroke={ACCENT}
              strokeOpacity="0.9"
              strokeWidth="1.2"
            />
            <path
              d="M 320 10 L 340 14 L 320 18 Z"
              fill={ACCENT}
              fillOpacity="0.9"
            />
          </svg>
          {/* spectrum horizon hairline */}
          <motion.div
            aria-hidden
            className="pointer-events-none absolute inset-x-0 bottom-0 h-px"
            style={{ background: SPECTRUM }}
            initial={{ opacity: reduced ? 1 : 0.6 }}
            animate={{ opacity: reduced ? 1 : [0.6, 1, 0.6] }}
            transition={
              reduced ? { duration: 0 } : { duration: 2, ease: "easeInOut" }
            }
          />
          {/* six waypoint dots along the horizon line */}
          <div className="pointer-events-none absolute inset-x-0 bottom-0">
            <svg
              viewBox="0 0 600 8"
              preserveAspectRatio="none"
              className="block h-2 w-full"
              aria-hidden
            >
              {[80, 170, 260, 340, 430, 520].map((x) => (
                <circle
                  key={x}
                  cx={x}
                  cy="4"
                  r="2"
                  fill={ACCENT}
                  fillOpacity="0.85"
                />
              ))}
            </svg>
          </div>
        </div>
      </div>
    </section>
  );
}
