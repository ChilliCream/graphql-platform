"use client";

import { motion } from "motion/react";

interface WorkflowsVariant1Props {
  readonly className?: string;
}

/**
 * "Workflow" hook illustration, v6 bespoke: a pre-wired handler topology.
 *
 * The whole graph is already connected before the message moves. A neutral
 * `command` node on the left feeds the teal, softly-lit `[Handler]` hub through
 * a single live connector. One amber `CreateReview` token rides mid-flight on
 * that connector. From the handler, three faint but SOLID, pre-drawn wires
 * already fan out to the dimmed reactions it is wired to: the `ReviewCreated`
 * event, the `SearchIndexer` handler, and the `NotifyAuthor` handler. No
 * dangling wires, nothing waiting to be registered.
 *
 * Sole looping accent: a soft amber halo pulses around the token's leading dot
 * as it heads into the handler, the only thing in motion in an already-wired
 * graph. The token, every label, and every connection are fully legible at rest
 * with no layout shift.
 *
 * Dark cc-* palette only; status color is purposeful (amber = the in-flight
 * message, teal = the handler you implement). Every svg id is prefixed
 * "v6-workflows-1-".
 */
export function WorkflowsVariant1({ className }: WorkflowsVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* header: pipeline eyebrow + a calm "wired" status pill */}
        <div className="flex items-center justify-between gap-3">
          <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            command pipeline
          </span>
          <span className="border-cc-accent/40 text-cc-accent bg-cc-accent/10 inline-flex shrink-0 items-center rounded-full border px-2.5 py-0.5 font-mono text-[0.6rem] font-medium">
            wired
          </span>
        </div>

        {/* topology: command -> [Handler] hub -> three pre-wired reactions */}
        <svg
          viewBox="0 0 336 200"
          width="100%"
          role="img"
          aria-label="A command node feeds a handler through one live connector carrying an amber CreateReview message; from the handler, three solid pre-drawn wires already fan out to a ReviewCreated event, a SearchIndexer handler, and a NotifyAuthor handler"
          className="mt-3"
          style={{ display: "block", overflow: "visible", fontFamily: MONO }}
        >
          <defs>
            <radialGradient id={`${ID}lit`} cx="50%" cy="32%" r="80%">
              <stop offset="0" stopColor={C.accent} stopOpacity="0.16" />
              <stop offset="65%" stopColor={C.accent} stopOpacity="0" />
            </radialGradient>
            <filter
              id={`${ID}glow`}
              x="-40%"
              y="-40%"
              width="180%"
              height="180%"
            >
              <feGaussianBlur stdDeviation="5" />
            </filter>
          </defs>

          {/* three faint but SOLID pre-drawn fan-out wires, no dangling ends */}
          {REACTIONS.map((r) => (
            <path
              key={`wire-${r.name}`}
              d={`M228 100 C 236 100 236 ${r.cy} 244 ${r.cy}`}
              fill="none"
              stroke={C.accent}
              strokeOpacity="0.32"
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
            />
          ))}
          {/* clean connection dots prove every wire terminates on a node */}
          <circle cx="228" cy="100" r="2.5" fill={C.accent} />
          {REACTIONS.map((r) => (
            <circle
              key={`dot-${r.name}`}
              cx="244"
              cy={r.cy}
              r="2"
              fill={C.accent}
              fillOpacity="0.5"
            />
          ))}

          {/* live connector stubs: command -> handler, amber = a message in flight */}
          <line
            x1="68"
            y1="100"
            x2="75"
            y2="100"
            stroke={C.amber}
            strokeWidth="1.5"
            vectorEffect="non-scaling-stroke"
          />
          <line
            x1="157"
            y1="100"
            x2="164"
            y2="100"
            stroke={C.amber}
            strokeWidth="1.5"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="68" cy="100" r="2" fill={C.amber} />

          {/* command node: neutral source */}
          <rect
            x="8"
            y="84"
            width="60"
            height="32"
            rx="8"
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="38"
            y="103.5"
            textAnchor="middle"
            fontSize="8.5"
            fill={C.inkDim}
          >
            command
          </text>

          {/* handler hub: the hero node you implement (teal, softly lit) */}
          <rect
            x="164"
            y="72"
            width="64"
            height="56"
            rx="12"
            fill="none"
            stroke={C.accent}
            strokeOpacity="0.5"
            strokeWidth="2"
            filter={`url(#${ID}glow)`}
          />
          <rect
            x="164"
            y="72"
            width="64"
            height="56"
            rx="12"
            fill={C.surface}
            stroke={C.accent}
            strokeWidth="1.25"
            vectorEffect="non-scaling-stroke"
          />
          <rect
            x="164"
            y="72"
            width="64"
            height="56"
            rx="12"
            fill={`url(#${ID}lit)`}
          />
          <text
            x="196"
            y="92"
            textAnchor="middle"
            fontSize="6.5"
            letterSpacing="1.3"
            fill={C.navLabel}
          >
            INTERFACE
          </text>
          <text
            x="196"
            y="109"
            textAnchor="middle"
            fontSize="10"
            fontWeight="600"
            fill={C.accent}
          >
            [Handler]
          </text>

          {/* the in-flight amber CreateReview token, mid-connector */}
          <rect
            x="75"
            y="89"
            width="82"
            height="22"
            rx="11"
            fill={C.surface}
            stroke={C.amber}
            strokeOpacity="0.85"
            strokeWidth="1.25"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="116"
            y="103"
            textAnchor="middle"
            fontSize="8.5"
            fontWeight="500"
            fill={C.amber}
          >
            CreateReview
          </text>

          {/* leading dot + sole looping accent: a soft halo as it nears the handler */}
          <motion.circle
            cx="160"
            cy="100"
            fill="none"
            stroke={C.amber}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            initial={{ r: 3, opacity: 0.55 }}
            animate={{ r: [3, 8, 3], opacity: [0.55, 0, 0.55] }}
            transition={{ duration: 2.4, repeat: Infinity, ease: "easeInOut" }}
          />
          <circle cx="160" cy="100" r="3" fill={C.amber} />

          {/* three dimmed, already-wired reactions */}
          {REACTIONS.map((r) => (
            <g key={r.name}>
              <rect
                x="244"
                y={r.cy - 17}
                width="88"
                height="34"
                rx="9"
                fill={C.surface}
                fillOpacity="0.55"
                stroke={C.cardBorder}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="258"
                y={r.cy - 3}
                fontSize="6"
                letterSpacing="1.2"
                fill={r.kind === "EVENT" ? C.accentDim : C.navLabel}
              >
                {r.kind}
              </text>
              <text x="258" y={r.cy + 10} fontSize="8.5" fill={C.inkDim}>
                {r.name}
              </text>
            </g>
          ))}
        </svg>

        {/* closing stat: the promise, quantified */}
        <div className="border-cc-card-border mt-3 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            0
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            lines of registration glue
          </p>
        </div>
      </div>
    </div>
  );
}

const ID = "v6-workflows-1-";

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** The three reactions the handler is already wired to, top to bottom. */
const REACTIONS: readonly {
  readonly kind: "EVENT" | "HANDLER";
  readonly name: string;
  readonly cy: number;
}[] = [
  { kind: "EVENT", name: "ReviewCreated", cy: 40 },
  { kind: "HANDLER", name: "SearchIndexer", cy: 100 },
  { kind: "HANDLER", name: "NotifyAuthor", cy: 160 },
];

/** Locked v6 cc-* palette for this cell: dark surfaces, neutral ink, teal, amber. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  heading: "#f5f0ea",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  accentDim: "rgba(94, 234, 212, 0.6)",
  amber: "#fbbf24",
} as const;
