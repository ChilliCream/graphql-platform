/**
 * "Agentic coding" scene, concept 1 ("Approval-gated agent action"), v3 "Signal
 * & Metrics" (dark cc-* panel).
 *
 * Re-expresses the v2 approval-gate transcript (a coding agent calling
 * createReview, the change holding at a human approval gate from PENDING to
 * GRANTED, then one validated patch landing) as the measured result the v3
 * strategy leads with. The hero is a single cream "1" over a lowercase mono
 * caption ("safe patch"): exactly one patch reaches the schema, and only after a
 * human grant. Layout A pairs that numeral with the lone teal signal on the
 * right, a 1px flow that reads left to right. The proposed createReview call is a
 * grey (pending) stroke into one human approval gate, the cell's only genuine
 * status element, inked governance violet. Past the gate the route turns teal
 * (granted) and ends in the single filled teal node, the landed patch the
 * headline names. Teal owns the granted route and its one node; violet owns only
 * the gate; the numeral stays cream.
 *
 * Static settled frame: no hooks, no motion, no "use client". Server component,
 * aria-hidden root. Local cc palette mirrors the cc-* tokens exactly. Any svg id
 * would be prefixed "v3-feedback-1-" (this take needs none).
 */
const cc = {
  surface: "#0c1322",
  inkFaint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  violet: "#7c92c6",
} as const;

const HEADING = '"Josefin Sans", Futura, sans-serif';

interface FeedbackVariant1Props {
  readonly className?: string;
}

export function FeedbackVariant1({ className }: FeedbackVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow: names the view, identical placement across the set */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          approval gate
        </p>

        {/* layout A: hero numeral left, the lone teal flow signal right */}
        <div className="mt-4 flex items-center gap-4">
          {/* hero: the one honest figure, cream over a mono caption */}
          <div className="shrink-0">
            <span
              className="block leading-none font-semibold"
              style={{
                fontFamily: HEADING,
                fontSize: "2.5rem",
                color: cc.heading,
              }}
            >
              1
            </span>
            <span
              className="mt-1.5 block font-mono lowercase"
              style={{
                fontSize: "0.62rem",
                letterSpacing: "0.06em",
                color: cc.inkDim,
              }}
            >
              safe patch
            </span>
          </div>

          {/* the single teal signal: a 1px flow from the proposed createReview
              call (grey, pending) through the violet human gate to the granted
              teal route, ending in the one filled teal node, the landed patch */}
          <div className="min-w-0 flex-1">
            {/* the gate state, settled from pending to granted, over the gate */}
            <div
              className="mb-1.5 flex items-center justify-center gap-1.5 font-mono"
              style={{ fontSize: "0.5rem", letterSpacing: "0.1em" }}
            >
              <span
                style={{ color: cc.navLabel, textDecoration: "line-through" }}
              >
                PENDING
              </span>
              <span style={{ color: cc.navLabel }}>&rarr;</span>
              <span style={{ color: cc.violet }}>GRANTED</span>
            </div>

            <svg
              viewBox="0 0 190 34"
              width="100%"
              aria-hidden="true"
              style={{ display: "block", overflow: "visible" }}
            >
              {/* pending segment: the proposed call, grey until the gate grants */}
              <line
                x1="6"
                y1="17"
                x2="89"
                y2="17"
                stroke={cc.inkFaint}
                strokeWidth="1"
              />
              {/* origin node: the createReview call */}
              <circle cx="6" cy="17" r="2" fill={cc.navLabel} />

              {/* granted segment: the live, safe route past the gate */}
              <line
                x1="101"
                y1="17"
                x2="182"
                y2="17"
                stroke={cc.accent}
                strokeWidth="1"
              />

              {/* the human approval gate, the one genuine status element */}
              <circle
                cx="95"
                cy="17"
                r="5"
                fill={cc.surface}
                stroke={cc.violet}
                strokeWidth="1"
              />
              <circle cx="95" cy="17" r="1.75" fill={cc.violet} />

              {/* the one filled teal node: the landed patch the headline names */}
              <circle cx="182" cy="17" r="4" fill={cc.surface} />
              <circle cx="182" cy="17" r="2.75" fill={cc.accent} />
            </svg>

            {/* flow endpoints, aligned under the signal */}
            <div
              className="mt-1 flex items-center justify-between font-mono"
              style={{
                fontSize: "0.5rem",
                letterSpacing: "0.04em",
                color: cc.navLabel,
              }}
            >
              <span>createReview</span>
              <span>+ patch</span>
            </div>
          </div>
        </div>

        {/* dashed-divider footnote: the honest read, no second accent */}
        <div
          className="mt-4 border-t border-dashed pt-3"
          style={{ borderColor: cc.inkFaint }}
        >
          <p
            className="text-center"
            style={{ fontSize: "0.7rem", lineHeight: 1.5, color: cc.ink }}
          >
            createReview holds until a human grants it
          </p>
        </div>
      </div>
    </div>
  );
}
