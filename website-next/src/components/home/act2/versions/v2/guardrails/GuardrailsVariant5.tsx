/**
 * Release-safety scene, variant 5 - v2 flow-diagram system.
 *
 * Concept: a schema.graphql registry version timeline. A left-to-right rail of
 * registry versions of schema.graphql: three already-published versions plus one
 * gated ghost version that cannot land. The relationship the headline names is
 * that every version is gated before it can publish, so the current candidate is
 * held PENDING REVIEW. Verdict is encoded by node status color (healthy = safe,
 * amber = risky-but-shipped, coral = the one breaking version). The single teal
 * path traces the current promotion attempt from the last published version to
 * the gated candidate, the one in-flight route the concept is about.
 *
 * Topology: PIPELINE / version timeline (motif 1). Published versions are solid
 * chips on the rail; the candidate is a dashed not-yet-reached terminal pill, and
 * the hop into it is a dashed deferred connector (the gate). One Stat duo footer
 * carries the two key numbers (published count, gate verdict).
 *
 * Content preserved so the gallery caption stays accurate:
 *   - the registry tracks schema.graphql versions
 *   - published: eshops@2260 (safe), eshops@2268 (risky), eshops@2274 (safe)
 *   - gated candidate: eshops@2291, BLOCKED pending review, because
 *     Product.rating was removed (a breaking change)
 *
 * Static render of the settled final frame. React Server Component: no "use
 * client", no hooks, no handlers, no animation. cc-* palette only; exactly one
 * teal traced path; status colors encode genuine verdict only. SVG ids (the
 * arrowhead marker) are prefixed "v2-guardrails-5-".
 */

import type { CSSProperties } from "react";

interface GuardrailsVariant5Props {
  readonly className?: string;
}

// cc-* palette, inline so this stays self-contained.
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
  healthy: "#34d399",
  amber: "#fbbf24",
  coral: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

type Verdict = "safe" | "risky" | "breaking";

// One registry version on the rail. `gated` = the candidate held before publish.
interface Version {
  readonly tag: string;
  readonly verdict: Verdict;
  readonly gated?: boolean;
}

const VERSIONS: readonly Version[] = [
  { tag: "eshops@2260", verdict: "safe" },
  { tag: "eshops@2268", verdict: "risky" },
  { tag: "eshops@2274", verdict: "safe" },
  { tag: "eshops@2291", verdict: "breaking", gated: true },
];

const VERDICT_DOT: Record<Verdict, string> = {
  safe: cc.healthy,
  risky: cc.amber,
  breaking: cc.coral,
};

const idp = "v2-guardrails-5-";

const MONO: CSSProperties = { fontFamily: cc.mono };

export function GuardrailsVariant5({ className }: GuardrailsVariant5Props) {
  const lastPubIdx = VERSIONS.length - 2; // eshops@2274, the current prod tag
  const gatedIdx = VERSIONS.length - 1; // eshops@2291, the held candidate

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div
        style={{
          background: cc.cardBg,
          border: `1px solid ${cc.cardBorder}`,
          borderRadius: 16,
          padding: 20,
          backdropFilter: "blur(4px)",
          WebkitBackdropFilter: "blur(4px)",
        }}
      >
        {/* eyebrow: the panel header, ScrollScenes style */}
        <p
          style={{
            ...MONO,
            margin: 0,
            fontSize: "0.58rem",
            letterSpacing: "0.15em",
            textTransform: "uppercase",
            color: cc.navLabel,
          }}
        >
          schema.graphql registry
        </p>

        {/* the rail: published versions, then a deferred gate into the candidate */}
        <div
          style={{
            marginTop: 18,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            flexWrap: "nowrap",
          }}
        >
          {VERSIONS.map((v, i) => {
            const traced = i === lastPubIdx || i === gatedIdx;
            return (
              <span
                key={v.tag}
                style={{ display: "inline-flex", alignItems: "center" }}
              >
                {i > 0 &&
                  (v.gated ? (
                    <GateConnector />
                  ) : (
                    <RailConnector
                      seq={i}
                      traced={traced && i - 1 === lastPubIdx}
                    />
                  ))}
                <VersionNode version={v} traced={traced} />
              </span>
            );
          })}
        </div>

        {/* verdict legend: which node color means what */}
        <div
          style={{
            marginTop: 16,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            gap: 12,
          }}
        >
          <LegendDot color={cc.healthy} label="safe" />
          <LegendDot color={cc.amber} label="risky" />
          <LegendDot color={cc.coral} label="breaking" />
        </div>

        {/* the reason the candidate is held, on a divider */}
        <div
          style={{
            marginTop: 16,
            paddingTop: 14,
            borderTop: `1px solid ${cc.cardBorder}`,
            display: "flex",
            alignItems: "baseline",
            gap: 8,
          }}
        >
          <span
            style={{
              flex: "0 0 auto",
              width: 6,
              height: 6,
              borderRadius: "50%",
              background: cc.coral,
              transform: "translateY(1px)",
            }}
          />
          <span
            style={{
              ...MONO,
              fontSize: "0.65rem",
              color: cc.ink,
              lineHeight: 1.4,
            }}
          >
            <span style={{ color: cc.heading }}>Product.rating</span> removed
            &middot; breaking change
          </span>
        </div>

        {/* Stat duo footer: the two key numbers */}
        <div
          style={{
            marginTop: 16,
            paddingTop: 14,
            borderTop: `1px solid ${cc.cardBorder}`,
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: 16,
          }}
        >
          <Stat figure="3" label="published" />
          <Stat figure="hold" label="gate verdict" accent />
        </div>
      </div>
    </div>
  );
}

/* One version on the rail. Published versions are solid rounded-md chips whose
 * leading dot encodes the verdict; the gated candidate is a dashed not-yet-reached
 * pill. The traced (current promotion) nodes carry the teal accent border. */
interface VersionNodeProps {
  readonly version: Version;
  readonly traced: boolean;
}

function VersionNode({ version, traced }: VersionNodeProps) {
  const gated = version.gated === true;
  const borderColor = traced ? cc.accent : cc.cardBorder;
  const labelColor = gated ? cc.inkDim : traced ? cc.accent : cc.ink;
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 5,
        background: cc.surface,
        border: `1px solid ${borderColor}`,
        borderStyle: gated ? "dashed" : "solid",
        borderRadius: 6,
        padding: "5px 7px",
        whiteSpace: "nowrap",
      }}
    >
      <span
        style={{
          flex: "0 0 auto",
          width: 6,
          height: 6,
          borderRadius: "50%",
          background: VERDICT_DOT[version.verdict],
        }}
      />
      <span style={{ ...MONO, fontSize: "0.6rem", color: labelColor }}>
        {version.tag}
      </span>
    </span>
  );
}

/* A solid 1px rail connector between published versions. The single segment on
 * the current promotion path is traced in teal; the rest stay grey. `seq` keeps
 * each connector's arrowhead marker id unique. */
interface RailConnectorProps {
  readonly traced: boolean;
  readonly seq: number;
}

function RailConnector({ traced, seq }: RailConnectorProps) {
  const color = traced ? cc.accent : cc.inkFaint;
  const markerId = `${idp}rail-arrow-${seq}`;
  return (
    <svg
      aria-hidden="true"
      width={20}
      height={10}
      viewBox="0 0 20 10"
      style={{ display: "block", flex: "0 0 auto" }}
    >
      <defs>
        <marker
          id={markerId}
          markerWidth={5}
          markerHeight={5}
          refX={4}
          refY={2.5}
          orient="auto"
        >
          <path d="M0,0 L5,2.5 L0,5" fill="none" stroke={color} />
        </marker>
      </defs>
      <line
        x1={1}
        y1={5}
        x2={16}
        y2={5}
        stroke={color}
        strokeWidth={1}
        markerEnd={`url(#${markerId})`}
      />
    </svg>
  );
}

/* The gate: a dashed deferred teal hop into the held candidate. This is the last
 * segment of the single traced promotion path, marking the version as gated
 * before publish. */
function GateConnector() {
  return (
    <span
      style={{
        display: "inline-flex",
        flexDirection: "column",
        alignItems: "center",
        gap: 3,
        flex: "0 0 auto",
        padding: "0 1px",
      }}
    >
      <span
        style={{
          ...MONO,
          fontSize: "0.5rem",
          letterSpacing: "0.06em",
          textTransform: "uppercase",
          color: cc.amber,
          whiteSpace: "nowrap",
        }}
      >
        gate
      </span>
      <svg
        aria-hidden="true"
        width={22}
        height={10}
        viewBox="0 0 22 10"
        style={{ display: "block" }}
      >
        <defs>
          <marker
            id={`${idp}gate-arrow`}
            markerWidth={5}
            markerHeight={5}
            refX={4}
            refY={2.5}
            orient="auto"
          >
            <path d="M0,0 L5,2.5 L0,5" fill="none" stroke={cc.accent} />
          </marker>
        </defs>
        <line
          x1={1}
          y1={5}
          x2={18}
          y2={5}
          stroke={cc.accent}
          strokeWidth={1}
          strokeDasharray="3 3"
          markerEnd={`url(#${idp}gate-arrow)`}
        />
      </svg>
    </span>
  );
}

/* One verdict-legend entry: a small status dot plus its mono label. */
interface LegendDotProps {
  readonly color: string;
  readonly label: string;
}

function LegendDot({ color, label }: LegendDotProps) {
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 5 }}>
      <span
        style={{
          width: 6,
          height: 6,
          borderRadius: "50%",
          background: color,
        }}
      />
      <span
        style={{
          ...MONO,
          fontSize: "0.55rem",
          letterSpacing: "0.04em",
          color: cc.inkDim,
        }}
      >
        {label}
      </span>
    </span>
  );
}

/* The ScrollScenes Stat: a font-heading numeral over a small dim caption. The
 * gate-verdict figure may carry the teal accent as the terminus of the traced
 * promotion path. */
interface StatProps {
  readonly figure: string;
  readonly label: string;
  readonly accent?: boolean;
}

function Stat({ figure, label, accent = false }: StatProps) {
  return (
    <div>
      <p
        style={{
          margin: 0,
          fontFamily: '"Josefin Sans", Futura, sans-serif',
          fontSize: "1.5rem",
          lineHeight: 1,
          fontWeight: 600,
          color: accent ? cc.accent : cc.heading,
        }}
      >
        {figure}
      </p>
      <p
        style={{
          margin: "6px 0 0",
          fontSize: "0.75rem",
          color: cc.inkDim,
        }}
      >
        {label}
      </p>
    </div>
  );
}
