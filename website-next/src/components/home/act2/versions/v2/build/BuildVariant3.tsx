interface BuildVariant3Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration, v2 "Flow Diagrams", concept 3:
 * "Source-gen build pass".
 *
 * A left-to-right PIPELINE of ordered build stages joined by thin grey text
 * arrows, mirroring the ScrollScenes Chip + Arrow vocabulary. The single teal
 * path traces the in-flight source-generation pass: the active "source-gen"
 * stage chip and the connectors that carry its emit into the derived artifact
 * nodes (rounded-md terminal chips). A grey "restore" stage precedes the pass
 * and a grey "compile" stage follows it, so the generator runs as one ordered
 * step inside the build, not the whole build. The named generator
 * (HotChocolate.Types.Analyzers) emits the SDL and code artifacts on a divided
 * section below, and a Stat duo footer separates the per-generator emit cost
 * (0.4s) from the whole-build total (1.8s).
 *
 * React Server Component: no "use client", no hooks, no handlers, no motion.
 * Settled final frame. cc-* palette only. Self-contained.
 */

const cc = {
  cardBg: "rgba(12,19,34,0.55)",
  cardBorder: "rgba(245,241,234,0.12)",
  surface: "#0c1322",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  inkFaint: "rgba(245,241,234,0.16)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace';
const HEADING = '"Josefin Sans", Futura, sans-serif';

// Ordered build stages, left to right. The source-gen pass is the in-flight
// step the headline names, so it carries the teal accent.
const STAGES: readonly { readonly label: string; readonly active: boolean }[] = [
  { label: "restore", active: false },
  { label: "source-gen", active: true },
  { label: "compile", active: false },
];

// Artifacts the generator pass emits, shown as derived terminal chips.
const EMITTED: readonly string[] = [
  "schema.graphql",
  "Catalog.Resolvers.g.cs",
  "ProductByIdDataLoader.g.cs",
];

function StageChip({
  label,
  active,
}: {
  readonly label: string;
  readonly active: boolean;
}) {
  return (
    <span
      style={{
        borderRadius: "8px",
        border: `1px solid ${active ? "rgba(94,234,212,0.6)" : cc.cardBorder}`,
        backgroundColor: cc.surface,
        padding: "6px 10px",
        fontFamily: MONO,
        fontSize: "0.65rem",
        whiteSpace: "nowrap",
        color: active ? cc.accent : cc.ink,
      }}
    >
      {label}
    </span>
  );
}

function Arrow({ accent = false }: { readonly accent?: boolean }) {
  return (
    <span
      aria-hidden="true"
      style={{
        color: accent ? cc.accent : cc.inkFaint,
        padding: "0 2px",
        fontSize: "0.875rem",
        fontFamily: MONO,
      }}
    >
      &rarr;
    </span>
  );
}

function Stat({
  figure,
  label,
}: {
  readonly figure: string;
  readonly label: string;
}) {
  return (
    <div>
      <p
        style={{
          fontFamily: HEADING,
          color: cc.heading,
          fontSize: "1.5rem",
          lineHeight: 1,
          fontWeight: 600,
          margin: 0,
        }}
      >
        {figure}
      </p>
      <p
        style={{
          color: cc.inkDim,
          fontSize: "0.75rem",
          marginTop: "6px",
          marginBottom: 0,
        }}
      >
        {label}
      </p>
    </div>
  );
}

export function BuildVariant3({ className }: BuildVariant3Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div
        style={{
          backgroundColor: cc.cardBg,
          border: `1px solid ${cc.cardBorder}`,
          borderRadius: "16px",
          padding: "20px",
          backdropFilter: "blur(4px)",
        }}
      >
        {/* eyebrow */}
        <p
          style={{
            color: cc.navLabel,
            fontFamily: MONO,
            fontSize: "0.58rem",
            letterSpacing: "0.15em",
            textTransform: "uppercase",
            margin: 0,
          }}
        >
          source-gen build pass
        </p>

        {/* PIPELINE: ordered build stages joined by text arrows */}
        <div
          style={{
            marginTop: "16px",
            display: "flex",
            flexWrap: "wrap",
            alignItems: "center",
            justifyContent: "center",
            gap: "4px",
          }}
        >
          {STAGES.map((stage, index) => (
            <span
              key={stage.label}
              style={{ display: "inline-flex", alignItems: "center" }}
            >
              {index > 0 && (
                // The teal path enters and leaves the source-gen stage.
                <Arrow accent={stage.active || STAGES[index - 1].active} />
              )}
              <StageChip label={stage.label} active={stage.active} />
            </span>
          ))}
        </div>

        {/* emit hop: source-gen -> emitted artifacts (the teal terminus) */}
        <div
          style={{
            marginTop: "16px",
            borderTop: `1px solid ${cc.cardBorder}`,
            paddingTop: "16px",
          }}
        >
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: "6px",
              marginBottom: "10px",
            }}
          >
            <span
              style={{
                color: cc.navLabel,
                fontFamily: MONO,
                fontSize: "0.55rem",
                letterSpacing: "0.1em",
                textTransform: "uppercase",
              }}
            >
              HotChocolate.Types.Analyzers
            </span>
            <Arrow accent />
            <span
              style={{
                color: cc.navLabel,
                fontFamily: MONO,
                fontSize: "0.55rem",
                letterSpacing: "0.1em",
                textTransform: "uppercase",
              }}
            >
              emits
            </span>
          </div>

          <div
            style={{
              display: "flex",
              flexDirection: "column",
              gap: "6px",
            }}
          >
            {EMITTED.map((artifact) => (
              <span
                key={artifact}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "6px",
                }}
              >
                <Arrow accent />
                <span
                  style={{
                    borderRadius: "6px",
                    border: `1px solid ${cc.cardBorder}`,
                    backgroundColor: cc.surface,
                    padding: "4px 8px",
                    fontFamily: MONO,
                    fontSize: "0.65rem",
                    whiteSpace: "nowrap",
                    color: cc.ink,
                  }}
                >
                  {artifact}
                </span>
              </span>
            ))}
          </div>
        </div>

        {/* Stat duo: per-generator emit cost vs. whole-build total */}
        <div
          style={{
            marginTop: "16px",
            borderTop: `1px solid ${cc.cardBorder}`,
            paddingTop: "16px",
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: "16px",
          }}
        >
          <Stat figure="0.4s" label="schema emitted" />
          <Stat figure="1.8s" label="build succeeded" />
        </div>
      </div>
    </div>
  );
}
