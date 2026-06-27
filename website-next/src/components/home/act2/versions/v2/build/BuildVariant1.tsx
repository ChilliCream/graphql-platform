interface BuildVariant1Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration, v2 "Flow Diagrams" take on concept #1,
 * "annotated source to generated artifacts".
 *
 * A fan-out relationship diagram in the locked v2 flow system: one active source
 * node, the annotated `[QueryType] ProductApi` C# class, fans out along three 1px
 * connectors into three derived artifact boxes the source generator emits, a
 * `schema.graphql` SDL snippet, a resolver-pipeline registration, and a typed
 * `ProductDataLoader` signature. Exactly one teal path is traced: source ->
 * `schema.graphql`, the contract the headline names; the other two connectors and
 * artifact boxes stay grey. A Stat duo footer reports the 1-source / 3-artifacts
 * span. Settled final frame, no animation. Every svg id is prefixed `v2-build-1-`.
 */
export function BuildVariant1({ className }: BuildVariant1Props) {
  const ID = "v2-build-1-";

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          source &rarr; generated
        </p>

        {/* fan-out: one source node radiates into three derived artifact boxes */}
        <svg
          viewBox="0 0 296 132"
          width="100%"
          role="img"
          aria-label="One annotated C# source class generating three artifacts"
          className="mt-4"
          style={{ display: "block", fontFamily: MONO }}
        >
          <defs>
            <marker
              id={`${ID}arrow-grey`}
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={C.inkFaint}
                strokeWidth="1"
              />
            </marker>
            <marker
              id={`${ID}arrow-teal`}
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={C.accent}
                strokeWidth="1"
              />
            </marker>
          </defs>

          {/* connectors: single-elbow orthogonal, source hub -> three artifacts */}
          {/* teal traced path: source -> schema.graphql (the generated contract) */}
          <path
            d="M120 66 H140 V22 H160"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow-teal)`}
          />
          {/* grey: source -> resolver registration */}
          <path
            d="M120 66 H160"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow-grey)`}
          />
          {/* grey: source -> typed DataLoader signature */}
          <path
            d="M120 66 H140 V110 H160"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow-grey)`}
          />

          {/* active source node (teal): the annotated [QueryType] class */}
          <Node
            x={4}
            y={52}
            w={114}
            label="[QueryType]"
            value="ProductApi.cs"
            active
          />

          {/* derived artifact boxes (grey), rounded-md to read as generated */}
          <Node
            x={162}
            y={8}
            w={130}
            label="schema.graphql"
            value="product(id): Product"
            derived
          />
          <Node
            x={162}
            y={52}
            w={130}
            label="ProductApi.g.cs"
            value=".AddResolver()"
            derived
          />
          <Node
            x={162}
            y={96}
            w={130}
            label="ProductDataLoader"
            value="LoadAsync(int)"
            derived
          />
        </svg>

        {/* Stat duo footer: the generation span */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="1" label="annotated source" />
          <Stat figure="3" label="generated artifacts" />
        </div>

        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            one class, no hand-written glue
          </p>
        </div>
      </div>
    </div>
  );
}

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked v2 flow palette: surfaces + the single teal accent, no off-brand hex. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  navLabel: "#62748e",
  accent: "#5eead4",
} as const;

interface NodeProps {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly label: string;
  readonly value: string;
  readonly active?: boolean;
  readonly derived?: boolean;
}

/**
 * A two-line Box node on the cc-surface fill: a uppercase nav-label eyebrow over a
 * cc-ink value line. The active source node tints border + label teal; derived
 * artifact nodes use a tighter radius to read as generated outputs.
 */
function Node({
  x,
  y,
  w,
  label,
  value,
  active = false,
  derived = false,
}: NodeProps) {
  const h = 28;
  const border = active ? C.accent : C.cardBorder;
  const labelFill = active ? C.accent : C.navLabel;
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height={h}
        rx={derived ? 5 : 7}
        fill={C.surface}
        stroke={border}
        strokeWidth="1"
        strokeOpacity={active ? 0.6 : 1}
      />
      <text
        x={x + 8}
        y={y + 11}
        fill={labelFill}
        fontSize="7"
        letterSpacing="0.08em"
        style={{ textTransform: "uppercase" }}
      >
        {label}
      </text>
      <text x={x + 8} y={y + 22} fill={C.ink} fontSize="9">
        {value}
      </text>
    </g>
  );
}

interface StatProps {
  readonly figure: string;
  readonly label: string;
}

/** The ScrollScenes Stat: a display numeral over a small dim caption. */
function Stat({ figure, label }: StatProps) {
  return (
    <div>
      <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-1.5 text-xs">{label}</p>
    </div>
  );
}
