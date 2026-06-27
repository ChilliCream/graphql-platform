"use client";

import { motion } from "motion/react";

interface WorkflowsVariant3Props {
  readonly className?: string;
}

/**
 * v6 "Workflow" hook, variant 3: a one-to-rail transport selector.
 *
 * Bespoke, one-off illustration (no shared v6 theme). A single amber
 * `await PublishAsync()` node sits at the top as the only source. One line drops
 * from it into a horizontal rail that fans out to five identical transport chips:
 * RabbitMQ, Postgres, Kafka, Azure SB, and in-process. RabbitMQ is lit amber
 * (the registered transport); the other four are visually identical but dimmed,
 * their drops dashed and inactive. Only the selected route glows through, so the
 * promise reads at a glance: one publish, any broker, switch by registration.
 *
 * The lone looping accent is a slow breathe on the amber route's glow halo; the
 * solid route, node, chips, and labels are static, so the resting and first
 * frame are fully legible. cc-* dark palette only, thin 1px strokes.
 */

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked v6 cc-* palette for this cell: dark surfaces, neutral ink, amber select. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  heading: "#f5f0ea",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  amber: "#fbbf24",
  amberFill: "rgba(251, 191, 36, 0.09)",
} as const;

interface Transport {
  /** Chip label (real Mocha transport name). */
  readonly name: string;
  /** Chip center on the rail axis. */
  readonly cx: number;
  /** The one registered, lit transport. */
  readonly selected: boolean;
}

const RAIL_Y = 84;
const CHIP_TOP = 116;
const CENTER_X = 162;

// RabbitMQ is the registered transport (lit); the other four are identical but
// dimmed. The amber route travels the shared rail and drops only into RabbitMQ.
const TRANSPORTS: readonly Transport[] = [
  { name: "RabbitMQ", cx: 38, selected: true },
  { name: "Postgres", cx: 100, selected: false },
  { name: "Kafka", cx: 162, selected: false },
  { name: "Azure SB", cx: 224, selected: false },
  { name: "in-process", cx: 286, selected: false },
];

const SELECTED = TRANSPORTS[0];
const ROUTE = `M${CENTER_X} 40 L${CENTER_X} ${RAIL_Y} L${SELECTED.cx} ${RAIL_Y} L${SELECTED.cx} ${CHIP_TOP}`;

export function WorkflowsVariant3({ className }: WorkflowsVariant3Props) {
  const ID = "v6-workflows-3-";

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-4 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          one publish, any broker
        </p>

        <svg
          viewBox="0 0 320 200"
          width="100%"
          role="img"
          aria-label="One await PublishAsync node at the top drops into a horizontal rail of five identical transports: RabbitMQ, Postgres, Kafka, Azure SB, and in-process. RabbitMQ is the registered transport, lit amber; the other four are available but dimmed."
          className="mt-3"
          style={{ display: "block" }}
        >
          <defs>
            <filter
              id={`${ID}glow`}
              x="-40%"
              y="-40%"
              width="180%"
              height="180%"
            >
              <feGaussianBlur stdDeviation="3.4" />
            </filter>
            <radialGradient id={`${ID}lit`} cx="50%" cy="0%" r="120%">
              <stop offset="0" stopColor={C.amber} stopOpacity="0.18" />
              <stop offset="70%" stopColor={C.amber} stopOpacity="0" />
            </radialGradient>
          </defs>

          {/* shared rail */}
          <line
            x1="38"
            y1={RAIL_Y}
            x2="286"
            y2={RAIL_Y}
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* dashed, inactive drops for the four unselected transports */}
          {TRANSPORTS.filter((t) => !t.selected).map((t) => (
            <line
              key={`drop-${t.name}`}
              x1={t.cx}
              y1={RAIL_Y + 4}
              x2={t.cx}
              y2={CHIP_TOP}
              stroke={C.cardBorder}
              strokeWidth="1"
              strokeDasharray="1.5 3"
              vectorEffect="non-scaling-stroke"
            />
          ))}

          {/* dim rail ticks to the right of the junction */}
          {TRANSPORTS.filter((t) => !t.selected && t.cx > CENTER_X).map((t) => (
            <circle
              key={`tick-${t.name}`}
              cx={t.cx}
              cy={RAIL_Y}
              r="1.8"
              fill={C.navLabel}
            />
          ))}

          {/* breathing glow: only the selected route glows through */}
          <motion.g
            initial={{ opacity: 0.42 }}
            animate={{ opacity: [0.42, 0.78, 0.42] }}
            transition={{ duration: 3.2, repeat: Infinity, ease: "easeInOut" }}
          >
            <rect
              x="100"
              y="12"
              width="124"
              height="28"
              rx="8"
              fill="none"
              stroke={C.amber}
              strokeWidth="2"
              filter={`url(#${ID}glow)`}
            />
            <path
              d={ROUTE}
              fill="none"
              stroke={C.amber}
              strokeWidth="2.4"
              strokeLinecap="round"
              strokeLinejoin="round"
              filter={`url(#${ID}glow)`}
            />
            <rect
              x={SELECTED.cx - 28}
              y={CHIP_TOP}
              width="56"
              height="38"
              rx="8"
              fill="none"
              stroke={C.amber}
              strokeWidth="2"
              filter={`url(#${ID}glow)`}
            />
          </motion.g>

          {/* solid amber route */}
          <path
            d={ROUTE}
            fill="none"
            stroke={C.amber}
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          />
          {/* junction split point + selected rail tick */}
          <circle cx={CENTER_X} cy={RAIL_Y} r="3" fill={C.amber} />
          <circle cx={SELECTED.cx} cy={RAIL_Y} r="2.6" fill={C.amber} />

          {/* the one source: await PublishAsync() */}
          <rect
            x="100"
            y="12"
            width="124"
            height="28"
            rx="8"
            fill={C.surface}
            stroke={C.amber}
            strokeWidth="1.25"
            vectorEffect="non-scaling-stroke"
          />
          <rect
            x="100"
            y="12"
            width="124"
            height="28"
            rx="8"
            fill={`url(#${ID}lit)`}
          />
          <text
            x={CENTER_X}
            y="30"
            textAnchor="middle"
            fontFamily={MONO}
            fontSize="9.5"
            fontWeight="600"
          >
            <tspan fill={C.inkDim} fontWeight="400">
              await{" "}
            </tspan>
            <tspan fill={C.amber}>PublishAsync()</tspan>
          </text>

          {/* five identical transport chips, one lit */}
          {TRANSPORTS.map((t) => (
            <g key={t.name}>
              <rect
                x={t.cx - 28}
                y={CHIP_TOP}
                width="56"
                height="38"
                rx="8"
                fill={t.selected ? C.amberFill : C.surface}
                stroke={t.selected ? C.amber : C.cardBorder}
                strokeWidth={t.selected ? 1.25 : 1}
                vectorEffect="non-scaling-stroke"
              />
              <rect
                x={t.cx - 3}
                y="124"
                width="6"
                height="6"
                rx="1.5"
                fill={t.selected ? C.amber : C.navLabel}
              />
              <text
                x={t.cx}
                y="146"
                textAnchor="middle"
                fontFamily={MONO}
                fontSize="7.5"
                fill={t.selected ? C.amber : C.inkDim}
              >
                {t.name}
              </text>
            </g>
          ))}
        </svg>

        {/* closing stat + promise */}
        <div className="border-cc-card-border mt-3 border-t pt-3">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            1 &rarr; 5
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            One PublishAsync, any broker. Production is a registration change,
            not a rewrite.
          </p>
        </div>
      </div>
    </div>
  );
}
