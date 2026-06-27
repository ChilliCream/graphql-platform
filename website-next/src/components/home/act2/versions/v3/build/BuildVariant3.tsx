interface BuildVariant3Props {
  readonly className?: string;
}

/**
 * "Build loop" scene, concept #3 ("Source-gen build pass"), v3 "Signal & Metrics"
 * (strict cc-* dark on a single floating card).
 *
 * Leads with the measured result: during `dotnet build` the Hot Chocolate source
 * generator runs its ordered stages (collect -> resolve -> emit) and writes the
 * schema in about 0.4s, so the hero is a single large cream HEADING numeral
 * "0.4 s" over a lowercase mono caption. The layout is archetype A (stat-left /
 * signal-right): the hero sits left, and the one teal accent is a 1px sparkline
 * on the right that rises through the ordered build-time stages to a single
 * filled teal node on the emit vertex - the moment the schema is written. The
 * three stage labels (collect / resolve / emit) sit under the line. No status hue
 * appears because the build succeeds; the palette stays cream / grey / teal. A
 * dashed-divider footnote keeps the honest split so the 0.4s emit cost is never
 * misread as the whole build: 0.4s emit within a 1.8s dotnet build.
 *
 * Content is faithful to the v1 terminal crop: the EShops `Catalog.Api` source-gen
 * pass run by `HotChocolate.Types.Analyzers`, "emitted in 0.4s", whole build
 * "Build succeeded in 1.8s". Only the visual language changes to the v3 dark
 * metrics panel.
 *
 * Static settled final frame: React Server Component, no animation, no hooks, no
 * "use client". Strict cc-* dark palette mirrored locally; teal is the single
 * decorative accent, bound to the build-stage sparkline and its one node. Every
 * svg id is prefixed "v3-build-3-".
 */

/* Strict cc-* dark palette (mirrors app/globals.css cc-* tokens). Teal is the
 * ONLY decorative hue and is bound to the source-gen sparkline. No status hue:
 * the build succeeds. */
const cc = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  faint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
} as const;

const HEADING = '"Josefin Sans", Futura, sans-serif';

const ID = "v3-build-3-";

// The ordered source-gen pass as accumulating relative progress: the generator
// collects types, resolves the model, then emits. The shape carries the read and
// the final point is the emit vertex (marked teal). The three labeled boundaries
// (collect, resolve, emit) sit under the line.
const STAGE_COST = [3, 7, 12, 18, 26, 33, 40] as const;
const STAGES = ["collect", "resolve", "emit"] as const;

// Sparkline plot geometry (user units; the svg scales to the well width).
const SPARK_W = 150;
const SPARK_H = 46;
const PAD_X = 5;
const PAD_T = 7;
const PAD_B = 7;

function buildSpark(values: readonly number[]) {
  const n = values.length;
  const min = Math.min(...values);
  const max = Math.max(...values);
  const top = PAD_T;
  const bottom = SPARK_H - PAD_B;
  const xOf = (i: number) => PAD_X + (i / (n - 1)) * (SPARK_W - 2 * PAD_X);
  const yOf = (v: number) =>
    max === min
      ? (top + bottom) / 2
      : bottom - ((v - min) / (max - min)) * (bottom - top);

  const points = values.map((v, i) => [xOf(i), yOf(v)] as const);
  const line = points
    .map(([x, y], i) => `${i === 0 ? "M" : "L"}${x} ${y}`)
    .join(" ");
  const last = points[points.length - 1];
  return { line, last, bottom };
}

export function BuildVariant3({ className }: BuildVariant3Props) {
  const { line, last, bottom } = buildSpark(STAGE_COST);

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow: names the view, identical placement across the set */}
        <p
          className="font-mono text-[0.58rem] tracking-[0.15em] uppercase"
          style={{ color: cc.navLabel }}
        >
          source-gen pass
        </p>

        {/* layout A: hero numeral left, the teal stage sparkline right */}
        <div className="mt-4 flex items-center gap-3">
          {/* HERO: the emit cost, the brightest thing in the cell */}
          <div className="shrink-0">
            <p className="leading-none whitespace-nowrap">
              <span
                className="font-semibold"
                style={{
                  fontFamily: HEADING,
                  fontSize: "2.75rem",
                  color: cc.heading,
                  fontVariantNumeric: "tabular-nums",
                }}
              >
                0.4
              </span>
              <span
                className="font-mono text-[1rem]"
                style={{ color: cc.navLabel, marginLeft: "0.25rem" }}
              >
                s
              </span>
            </p>
            <p
              className="mt-1.5 font-mono text-[0.7rem] whitespace-nowrap lowercase"
              style={{ color: cc.inkDim }}
            >
              schema emit time
            </p>
          </div>

          {/* the single teal measurement: ordered build stages rising to the
              marked emit vertex. This is the visual underline of the number. */}
          <div className="min-w-0 flex-1">
            <svg
              viewBox={`0 0 ${SPARK_W} ${SPARK_H}`}
              role="img"
              aria-label="Ordered source-gen stages rising to the marked schema-emit point"
              style={{
                display: "block",
                width: "100%",
                height: "auto",
                overflow: "visible",
              }}
            >
              {/* faint baseline the stages rest on */}
              <line
                x1={0}
                y1={bottom}
                x2={SPARK_W}
                y2={bottom}
                stroke={cc.faint}
                strokeWidth={1}
                vectorEffect="non-scaling-stroke"
              />
              {/* stage-progress polyline, single 1px teal stroke */}
              <path
                d={line}
                fill="none"
                stroke={cc.accent}
                strokeWidth={1}
                strokeLinecap="round"
                strokeLinejoin="round"
                opacity={0.85}
                vectorEffect="non-scaling-stroke"
              />
              {/* the one node: cc-surface ring + filled teal dot on the emit
                  vertex, the moment the schema is written */}
              <circle cx={last[0]} cy={last[1]} r={4} fill={cc.surface} />
              <circle cx={last[0]} cy={last[1]} r={2.6} fill={cc.accent} />
            </svg>

            {/* ordered stage labels: collect -> resolve -> emit */}
            <div className="mt-2 flex items-center justify-between">
              {STAGES.map((stage) => (
                <span
                  key={`${ID}stage-${stage}`}
                  className="font-mono text-[0.5rem] tracking-[0.08em] uppercase"
                  style={{
                    color: stage === "emit" ? cc.ink : cc.navLabel,
                  }}
                >
                  {stage}
                </span>
              ))}
            </div>
          </div>
        </div>

        {/* interpretation footnote under a dashed faint divider: the honest split
            so the emit cost is not misread as the whole build */}
        <div
          className="mt-4 border-t border-dashed pt-3"
          style={{ borderColor: cc.faint }}
        >
          <p
            className="text-center font-mono text-[0.62rem] lowercase"
            style={{ color: cc.ink }}
          >
            0.4s emit within a 1.8s dotnet build
          </p>
        </div>
      </div>
    </div>
  );
}
