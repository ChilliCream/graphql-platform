interface WorkflowsVariant4Props {
  readonly className?: string;
}

/**
 * "Workflow" hook illustration, v6 bespoke, concept: same handlers, near or far.
 *
 * One shared origin pill, `POST /reviews`, forks into two stacked lanes whose
 * nodes are styled IDENTICALLY, so the eye reads "one wiring, two distances".
 * The top lane, `mediator - in-process`, is a short hop: a `CreateReview`
 * command dispatched straight into a `[Handler]`. The bottom lane,
 * `bus - cross-service`, publishes a `ReviewCreated` event that crosses a dashed
 * transport boundary out to the same `[Handler]` shape, with a single peeking
 * card behind it to imply remote consumers. The two handler chips sit in one
 * aligned column with matching styling: only the verb and the distance change.
 *
 * Static React Server Component: no hooks, no client APIs, settled final frame.
 * Dark cc-* palette only, thin 1px non-scaling strokes, generous negative space.
 * Teal is the brand accent on message markers, not a status. Every svg id is
 * prefixed "v6-workflows-4-".
 */

const ID = "v6-workflows-4-";

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked v6 cc-* palette for this cell: dark surfaces, neutral ink, teal accent. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  eyebrow: "#62748e",
  accent: "#5eead4",
  edge: "rgba(245, 241, 234, 0.22)",
} as const;

const STROKE = {
  vectorEffect: "non-scaling-stroke",
  strokeLinecap: "round",
  strokeLinejoin: "round",
} as const;

const CHIP_H = 26;

/** One lane node. Identical styling across both lanes sells "same handler". */
function ChipNode({
  x,
  y,
  w,
  label,
  marker,
}: {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly label: string;
  readonly marker?: boolean;
}) {
  const cy = y + CHIP_H / 2;
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height={CHIP_H}
        rx={7}
        fill={C.surface}
        stroke={C.cardBorder}
        strokeWidth={1}
        {...STROKE}
      />
      {marker === true && <circle cx={x + 12} cy={cy} r={3} fill={C.accent} />}
      <text
        x={marker === true ? x + 22 : x + w / 2}
        y={cy}
        textAnchor={marker === true ? "start" : "middle"}
        dominantBaseline="middle"
        fontFamily={MONO}
        fontSize={9.5}
        fill={C.heading}
      >
        {label}
      </text>
    </g>
  );
}

/** Eyebrow lane label, set apart from the chip row above each lane. */
function LaneLabel({
  x,
  y,
  children,
}: {
  readonly x: number;
  readonly y: number;
  readonly children: string;
}) {
  return (
    <text
      x={x}
      y={y}
      fontFamily={MONO}
      fontSize={8}
      letterSpacing="0.12em"
      fill={C.eyebrow}
    >
      {children}
    </text>
  );
}

export function WorkflowsVariant4({ className }: WorkflowsVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* header: names the single source on the left, asserts one model right */}
        <div className="flex items-center justify-between gap-3">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            one request, two lanes
          </p>
          <span
            className="font-mono text-[0.6rem] whitespace-nowrap"
            style={{
              borderRadius: 999,
              border: `1px solid ${C.accent}80`,
              background: `${C.accent}1a`,
              color: C.accent,
              padding: "2px 9px",
            }}
          >
            same model
          </span>
        </div>

        <svg
          viewBox="0 0 320 186"
          width="100%"
          role="img"
          aria-label="A POST /reviews request forks into two identically styled lanes. The in-process mediator lane dispatches a CreateReview command straight into a handler. The cross-service bus lane publishes a ReviewCreated event across a dashed transport boundary to the same handler shape."
          className="mt-3"
          style={{ display: "block", fontFamily: MONO }}
        >
          <defs>
            <marker
              id={`${ID}arrow`}
              viewBox="0 0 6 6"
              refX="4.5"
              refY="3"
              markerWidth="6"
              markerHeight="6"
              markerUnits="userSpaceOnUse"
              orient="auto"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={C.edge}
                strokeWidth="1"
                {...STROKE}
              />
            </marker>
          </defs>

          {/* shared origin pill: the single source both lanes fork from */}
          <rect
            x={6}
            y={80}
            width={92}
            height={30}
            rx={8}
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth={1}
            {...STROKE}
          />
          <text
            x={16}
            y={95}
            dominantBaseline="middle"
            fontFamily={MONO}
            fontSize="9.5"
          >
            <tspan fill={C.accent} fontWeight={600}>
              POST
            </tspan>
            <tspan fill={C.heading}> /reviews</tspan>
          </text>

          {/* the fork: one source splitting up and down into the two lanes */}
          <path
            d="M108 95 C 108 70 110 50 122 50"
            fill="none"
            stroke={C.edge}
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow)`}
            {...STROKE}
          />
          <path
            d="M108 95 C 108 120 110 140 122 140"
            fill="none"
            stroke={C.edge}
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow)`}
            {...STROKE}
          />
          <circle cx={108} cy={95} r={2.6} fill={C.accent} />

          {/* top lane: mediator, in-process, a short CreateReview -> handler hop */}
          <LaneLabel x={122} y={27}>
            mediator - in-process
          </LaneLabel>
          <ChipNode x={122} y={37} w={94} label="CreateReview" marker />
          <line
            x1={217}
            y1={50}
            x2={226}
            y2={50}
            stroke={C.edge}
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow)`}
            {...STROKE}
          />
          <ChipNode x={228} y={37} w={74} label="[Handler]" />

          {/* bottom lane: bus, cross-service, ReviewCreated across a transport */}
          <LaneLabel x={122} y={117}>
            bus - cross-service
          </LaneLabel>
          <ChipNode x={122} y={127} w={94} label="ReviewCreated" marker />
          <line
            x1={217}
            y1={140}
            x2={226}
            y2={140}
            stroke={C.edge}
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow)`}
            {...STROKE}
          />

          {/* the dashed transport boundary the event crosses to reach remote handlers */}
          <line
            x1={222}
            y1={121}
            x2={222}
            y2={159}
            stroke={C.eyebrow}
            strokeWidth="1"
            strokeDasharray="3 3"
            {...STROKE}
          />
          <text
            x={222}
            y={172}
            textAnchor="middle"
            fontFamily={MONO}
            fontSize="7"
            letterSpacing="0.08em"
            fill={C.eyebrow}
          >
            transport
          </text>

          {/* a single peeking card behind the remote handler: many consumers, one shape */}
          <rect
            x={232}
            y={131}
            width={74}
            height={26}
            rx={7}
            fill="none"
            stroke={C.cardBorder}
            strokeWidth={1}
            opacity={0.5}
            {...STROKE}
          />
          <ChipNode x={228} y={127} w={74} label="[Handler]" />
        </svg>

        {/* caption: the promise, only the verb and distance change */}
        <div className="border-cc-card-border mt-4 border-t pt-3">
          <p
            className="text-center font-mono text-[0.6rem]"
            style={{ color: C.inkDim }}
          >
            change the verb, not the model
          </p>
        </div>
      </div>
    </div>
  );
}
