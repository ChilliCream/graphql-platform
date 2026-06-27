interface FeedbackVariant4Props {
  readonly className?: string;
}

/**
 * "Agentic coding" hook illustration, v6 bespoke: a linear promotion rail at rest.
 *
 * One tool token has walked a flat horizontal rail of four resolved stage chips
 * (`author -> validate -> stage -> trace`), each marked complete in teal, then
 * cleared a STATIC, already-resolved violet approval gate and settled into the
 * `production` endcap as a `served` token with a healthy dot. Unlike a live gate
 * mid-review, this one is calm: every checkpoint is decided, the tool is already
 * serving traffic.
 *
 * Static React Server Component: no hooks, no client APIs, settled final frame.
 * Dark cc-* palette only; teal marks pipeline progress, violet marks governance
 * approval, healthy green marks a real served status. Every svg id is prefixed
 * "v6-feedback-4-".
 */
export function FeedbackVariant4({ className }: FeedbackVariant4Props) {
  const ID = "v6-feedback-4-";

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          tool promotion
        </p>

        {/* the promotion rail: four resolved stages, a settled violet gate, production */}
        <svg
          viewBox="0 0 336 94"
          width="100%"
          role="img"
          aria-label="A tool token that walked four resolved stages, author, validate, stage, and trace, then cleared a violet approval gate and now sits served in production"
          className="mt-4"
          style={{ display: "block", fontFamily: MONO }}
        >
          <defs>
            <marker
              id={`${ID}arrow`}
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
                stroke={C.accent}
                strokeOpacity="0.6"
                strokeWidth="1"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </marker>
          </defs>

          {/* production endcap: the calm destination the tool reached */}
          <rect
            x="238"
            y="22"
            width="94"
            height="52"
            rx="10"
            fill="rgba(245, 241, 234, 0.025)"
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="247"
            y="36"
            fontSize="7.5"
            letterSpacing="1.2"
            fill={C.navLabel}
          >
            production
          </text>

          {/* the rail: traversed track, then the segment past the gate into prod */}
          <line
            x1="18"
            y1="48"
            x2="202"
            y2="48"
            stroke={C.accent}
            strokeOpacity="0.45"
            strokeWidth="1.25"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
          />
          <line
            x1="218"
            y1="48"
            x2="243"
            y2="48"
            stroke={C.accent}
            strokeOpacity="0.45"
            strokeWidth="1.25"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            markerEnd={`url(#${ID}arrow)`}
          />

          {/* four resolved stage stations on the rail */}
          {STAGES.map((stage) => (
            <g key={stage.name}>
              <rect
                x={stage.cx - 8}
                y="40"
                width="16"
                height="16"
                rx="4"
                fill={C.surface}
                stroke={C.accent}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              <path
                d={`M${stage.cx - 3.5} 48 L${stage.cx - 1} 50.5 L${stage.cx + 3.5} 45`}
                fill="none"
                stroke={C.accent}
                strokeWidth="1.3"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
              <text
                x={stage.cx}
                y="70"
                textAnchor="middle"
                fontSize="8"
                fill={C.navLabel}
              >
                {stage.name}
              </text>
            </g>
          ))}

          {/* the static, already-resolved violet approval gate */}
          <text
            x="210"
            y="17"
            textAnchor="middle"
            fontSize="8.5"
            letterSpacing="1"
            fill={C.violet}
          >
            approved
          </text>
          <rect
            x="207"
            y="24"
            width="6"
            height="48"
            fill={C.violet}
            opacity="0.08"
          />
          <line
            x1="210"
            y1="24"
            x2="210"
            y2="72"
            stroke={C.violet}
            strokeWidth="1.4"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
          />
          <line
            x1="205"
            y1="24"
            x2="215"
            y2="24"
            stroke={C.violet}
            strokeWidth="1.4"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
          />
          <line
            x1="205"
            y1="72"
            x2="215"
            y2="72"
            stroke={C.violet}
            strokeWidth="1.4"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
          />
          <circle
            cx="210"
            cy="48"
            r="8"
            fill={C.surface}
            stroke={C.violet}
            strokeWidth="1.2"
            vectorEffect="non-scaling-stroke"
          />
          <path
            d="M206.5 48 L209 50.5 L213.5 45"
            fill="none"
            stroke={C.violet}
            strokeWidth="1.3"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {/* the settled tool token, served in production */}
          <rect
            x="245"
            y="39"
            width="80"
            height="18"
            rx="9"
            fill={C.surface}
            stroke={C.accent}
            strokeOpacity="0.5"
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="256" cy="48" r="3" fill={C.healthy} />
          <text x="265" y="51" fontSize="9" fill={C.heading}>
            served
          </text>
        </svg>

        {/* closing line: the tool that earned its way up */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="text-cc-heading font-mono text-[0.82rem] tracking-tight">
            search-eshops-catalog
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            Cleared every stage and the approval gate, now served.
          </p>
        </div>
      </div>
    </div>
  );
}

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** The four stages every tool walks before it earns its way into production. */
const STAGES: readonly { readonly name: string; readonly cx: number }[] = [
  { name: "author", cx: 30 },
  { name: "validate", cx: 84 },
  { name: "stage", cx: 138 },
  { name: "trace", cx: 192 },
];

/** Locked v6 cc-* palette for this cell: dark surfaces, teal, governance violet. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  heading: "#f5f0ea",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  violet: "#8b8ff0",
  healthy: "#34d399",
} as const;
