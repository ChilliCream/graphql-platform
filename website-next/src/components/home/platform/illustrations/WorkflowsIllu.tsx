"use client";

import { motion } from "motion/react";

interface WorkflowsIlluProps {
  readonly className?: string;
}

/**
 * Event-driven fan-out, section grade.
 *
 * A single producer publishes ONE `ReviewCreated` event the instant a
 * `POST /reviews` request returns 200. From that one node, three dashed async
 * connectors fan out across a service boundary to three independent reactions
 * (`SearchIndexer`, `NotifyAuthor`, `Analytics`), each its own node on
 * cc-surface. One copy of the event is in flight on the middle connector: an
 * amber token whose lit path warms from amber at the source to teal as it
 * arrives. A small saga strip (Draft -> Checked -> Published) shows the work
 * continuing after the request already returned.
 *
 * Dark cc-* palette only. Teal is the primary, amber is reserved for the single
 * in-flight message, healthy green marks the returned request, dashed strokes
 * mean async / eventual hops. The sole motion accent is a teal comet advancing
 * off the token toward the consumers; it reads perfectly as a static frame.
 * Every svg id is prefixed "illu-workflows-".
 */
export function WorkflowsIllu({ className }: WorkflowsIlluProps) {
  return (
    <div
      aria-hidden="true"
      className={["mx-auto w-full", className ?? ""].join(" ")}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 mx-auto max-w-md rounded-2xl border p-5 backdrop-blur-sm">
        {/* header: section eyebrow + a calm async signal for the dashed hops */}
        <div className="flex items-center justify-between gap-3">
          <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            event-driven fan-out
          </span>
          <span className="border-cc-accent/35 text-cc-accent bg-cc-accent/10 inline-flex shrink-0 items-center rounded-full border px-2.5 py-0.5 font-mono text-[0.6rem] font-medium">
            async
          </span>
        </div>

        {/* topology: one event -> three independent reactions across a boundary */}
        <svg
          viewBox="0 0 440 224"
          width="100%"
          className="mt-3"
          style={{ display: "block", overflow: "visible", fontFamily: MONO }}
        >
          <defs>
            <radialGradient id={`${ID}lit`} cx="50%" cy="34%" r="82%">
              <stop offset="0" stopColor={C.accent} stopOpacity="0.18" />
              <stop offset="68%" stopColor={C.accent} stopOpacity="0" />
            </radialGradient>
            <linearGradient
              id={`${ID}flow`}
              gradientUnits="userSpaceOnUse"
              x1="124"
              y1="112"
              x2="300"
              y2="112"
            >
              <stop offset="0" stopColor={C.amber} />
              <stop offset="1" stopColor={C.accent} />
            </linearGradient>
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

          {/* faint teal radial behind the producer, the one lit origin */}
          <rect x="2" y="62" width="150" height="100" fill={`url(#${ID}lit)`} />

          {/* service boundary: a faint dashed seam the events cross */}
          <line
            x1="256"
            y1="16"
            x2="256"
            y2="214"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeDasharray="3 5"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="256"
            y="9"
            textAnchor="middle"
            fontSize="6"
            letterSpacing="1.4"
            fill={C.navLabel}
          >
            SERVICE BOUNDARY
          </text>

          {/* two idle async hops: dashed, waiting for their copy of the event */}
          {[REACTIONS[0], REACTIONS[2]].map((r) => (
            <path
              key={`hop-${r.name}`}
              d={`M124 112 C 200 112 224 ${r.cy} 300 ${r.cy}`}
              fill="none"
              stroke={C.accent}
              strokeOpacity="0.3"
              strokeWidth="1"
              strokeDasharray="4 4"
              strokeLinecap="round"
              vectorEffect="non-scaling-stroke"
            />
          ))}

          {/* the active hop: solid, amber at the source warming to teal on arrival */}
          <line
            x1="124"
            y1="112"
            x2="296"
            y2="112"
            stroke={`url(#${ID}flow)`}
            strokeOpacity="0.95"
            strokeWidth="1.75"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* connection dots: amber at the lit source, teal where copies land */}
          <circle cx="124" cy="112" r="2.5" fill={C.amber} />
          <circle cx="300" cy={REACTIONS[0].cy} r="2" fill={C.accent} />
          <circle cx="300" cy={REACTIONS[1].cy} r="2.5" fill={C.accent} />
          <circle
            cx="300"
            cy={REACTIONS[2].cy}
            r="2"
            fill={C.accent}
            fillOpacity="0.5"
          />

          {/* producer node: the single event, published the moment 200 returns */}
          <rect
            x="14"
            y="86"
            width="110"
            height="52"
            rx="12"
            fill="none"
            stroke={C.accent}
            strokeOpacity="0.45"
            strokeWidth="2"
            filter={`url(#${ID}glow)`}
          />
          <rect
            x="14"
            y="86"
            width="110"
            height="52"
            rx="12"
            fill={C.surface}
            stroke={C.accent}
            strokeWidth="1.25"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="69"
            y="104"
            textAnchor="middle"
            fontSize="6"
            letterSpacing="1.5"
            fill={C.navLabel}
          >
            EVENT
          </text>
          <text
            x="69"
            y="119"
            textAnchor="middle"
            fontSize="11"
            fontWeight="600"
            fill={C.accent}
          >
            ReviewCreated
          </text>
          <text x="69" y="131" textAnchor="middle" fontSize="7" fill={C.inkDim}>
            POST /reviews
          </text>

          {/* returned-200 flag: the request is already done while the fan-out runs */}
          <rect
            x="80"
            y="70"
            width="44"
            height="16"
            rx="8"
            fill={C.surface}
            stroke={C.healthy}
            strokeOpacity="0.7"
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="102"
            y="81"
            textAnchor="middle"
            fontSize="7.5"
            fontWeight="500"
            fill={C.healthy}
          >
            200 OK
          </text>

          {/* the three reactions, each an independent node on cc-surface */}
          {REACTIONS.map((r) => (
            <g key={r.name}>
              <rect
                x="300"
                y={r.cy - 22}
                width="128"
                height="44"
                rx="10"
                fill={C.surface}
                fillOpacity="0.55"
                stroke={C.cardBorder}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="314"
                y={r.cy - 9}
                fontSize="6"
                letterSpacing="1.4"
                fill={C.navLabel}
              >
                {r.kind}
              </text>
              <text x="314" y={r.cy + 2} fontSize="9.5" fill={C.inkDim}>
                {r.name}
              </text>
              <text x="314" y={r.cy + 13} fontSize="6.5" fill={C.faint}>
                {r.service}
              </text>
            </g>
          ))}

          {/* in-flight token: one amber copy of the event riding the active hop */}
          <rect
            x="156"
            y="101"
            width="72"
            height="22"
            rx="11"
            fill={C.surface}
            stroke={C.amber}
            strokeOpacity="0.9"
            strokeWidth="1.25"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="192"
            y="115"
            textAnchor="middle"
            fontSize="8.5"
            fontWeight="500"
            fill={C.amber}
          >
            ReviewCreated
          </text>

          {/* leading tip of the message + the sole motion accent: a teal comet */}
          <circle cx="228" cy="112" r="3" fill={C.amber} />
          <motion.circle
            cy="112"
            r="2.5"
            fill={C.accent}
            initial={{ cx: 232, opacity: 0 }}
            animate={{ cx: [232, 296], opacity: [0, 0.9, 0] }}
            transition={{ duration: 1.8, repeat: Infinity, ease: "easeOut" }}
          />
        </svg>

        {/* footer: work continues after 200, plus the fan-out count */}
        <div className="border-cc-card-border mt-4 flex items-end justify-between gap-4 border-t pt-4">
          <div>
            <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.15em] uppercase">
              saga &middot; continues after 200
            </p>
            <div className="mt-2 flex items-center">
              {SAGA.map((s, i) => (
                <span key={s.name} className="flex items-center">
                  {i > 0 && (
                    <span
                      aria-hidden="true"
                      className="text-cc-ink-faint px-1 text-[0.7rem]"
                    >
                      &rarr;
                    </span>
                  )}
                  <span
                    className={[
                      "rounded-md border px-2 py-0.5 font-mono text-[0.6rem]",
                      s.state === "active"
                        ? "border-cc-accent/60 text-cc-accent bg-cc-accent/5"
                        : s.state === "done"
                          ? "border-cc-card-border text-cc-ink-dim"
                          : "border-cc-ink-faint text-cc-ink-dim border-dashed",
                    ].join(" ")}
                  >
                    {s.name}
                  </span>
                </span>
              ))}
            </div>
          </div>

          <div className="shrink-0 text-right">
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              3
            </p>
            <p className="text-cc-ink-dim mt-1 text-xs">fan-out reactions</p>
          </div>
        </div>
      </div>
    </div>
  );
}

const ID = "illu-workflows-";

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** The three independent reactions the one event fans out to, top to bottom. */
const REACTIONS: readonly {
  readonly kind: "HANDLER" | "CONSUMER";
  readonly name: string;
  readonly service: string;
  readonly cy: number;
}[] = [
  { kind: "HANDLER", name: "SearchIndexer", service: "search-svc", cy: 40 },
  { kind: "HANDLER", name: "NotifyAuthor", service: "email-svc", cy: 112 },
  { kind: "CONSUMER", name: "Analytics", service: "metrics-svc", cy: 184 },
];

/** Saga that keeps running after the request returned. */
const SAGA: readonly {
  readonly name: string;
  readonly state: "done" | "active" | "next";
}[] = [
  { name: "Draft", state: "done" },
  { name: "Checked", state: "active" },
  { name: "Published", state: "next" },
];

/** Locked cc-* palette for this cell: dark surfaces, neutral ink, teal, amber. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  faint: "rgba(245, 241, 234, 0.42)",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  amber: "#fbbf24",
  healthy: "#34d399",
} as const;
