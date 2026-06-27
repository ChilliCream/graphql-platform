/**
 * Release-safety scene, variant 4 - v2 "Flow Diagram" (locked cc-* system).
 *
 * Re-expresses the v1 build-drift terminal as a BEFORE / AFTER relationship
 * diagram in the ScrollScenes Chip + Arrow vocabulary. A schema field change is
 * regenerated into the Strawberry Shake client; the C# compiler is what catches
 * the break. Two stacked mini-flows separated by a divider:
 *
 *   before  Product.rating: Int!  ->  client double?  ->  compiles
 *   after   Product.rating: Float ->  client double?  ->  CS0266 (build fails)
 *
 * The single teal path traces the "after" route: the retyped schema field flows
 * into the regenerated client and terminates at the CS0266 compiler error, the
 * coral build-failed gate marking the genuine failure status. Everything else
 * stays cream-label / grey-ink. A Stat duo footer carries the two key numbers.
 *
 * Server component. No hooks, no client APIs, no animation; the settled final
 * frame only. cc-* palette via Tailwind utilities; the one inline SVG (the
 * before/after connector) is id-prefixed "v2-guardrails-4-". No em dashes.
 */

import type { ReactNode } from "react";

interface GuardrailsVariant4Props {
  readonly className?: string;
}

/** Chip extending the ScrollScenes pill: entity flowing through the diagram. */
function Chip({
  children,
  active = false,
}: {
  readonly children: ReactNode;
  readonly active?: boolean;
}) {
  return (
    <span
      className={[
        "rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap",
        active
          ? "border-cc-accent/60 text-cc-accent bg-cc-surface"
          : "border-cc-card-border text-cc-ink bg-cc-surface",
      ].join(" ")}
    >
      {children}
    </span>
  );
}

/** Grey text arrow, the default connector for the inline before/after flows. */
function Arrow({ accent = false }: { readonly accent?: boolean }) {
  return (
    <span
      aria-hidden="true"
      className={[
        "px-0.5 text-sm",
        accent ? "text-cc-accent" : "text-cc-ink-faint",
      ].join(" ")}
    >
      &rarr;
    </span>
  );
}

/**
 * Terminal node: the settled / derived artifact. Smaller radius and tighter
 * padding to read as the outcome the flow lands on (the saga-pill pattern). A
 * status border encodes genuine state: healthy passed, coral compile error.
 */
function Outcome({
  children,
  status,
}: {
  readonly children: ReactNode;
  readonly status: "passed" | "error";
}) {
  return (
    <span
      className={[
        "bg-cc-surface rounded-md border px-2 py-1 font-mono text-[0.6rem] whitespace-nowrap",
        status === "error"
          ? "border-cc-status-firing/60 text-cc-status-firing"
          : "border-cc-card-border text-cc-ink-dim",
      ].join(" ")}
    >
      {children}
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
      <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-1.5 text-xs">{label}</p>
    </div>
  );
}

export function GuardrailsVariant4({ className }: GuardrailsVariant4Props) {
  const idp = "v2-guardrails-4-";

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          build drift
        </p>

        {/* === BEFORE: the field as it was, client compiled clean === */}
        <div className="mt-4">
          <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
            before
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-1">
            <Chip>rating: Int!</Chip>
            <Arrow />
            <Chip>int</Chip>
            <Arrow />
            <Outcome status="passed">compiles</Outcome>
          </div>
        </div>

        {/* connector showing the schema retype carries into the after flow */}
        <div className="mt-3 flex justify-center" aria-hidden="true">
          <svg
            id={`${idp}retype`}
            viewBox="0 0 16 18"
            width="16"
            height="18"
            fill="none"
            style={{ display: "block" }}
          >
            <line
              x1="8"
              y1="0"
              x2="8"
              y2="14"
              stroke="#5eead4"
              strokeWidth="1"
            />
            <polyline
              points="4.5,10 8,14 11.5,10"
              stroke="#5eead4"
              strokeWidth="1"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </div>

        {/* === AFTER: the retype regenerates and the compiler catches it === */}
        <div>
          <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
            after
          </p>
          <div className="mt-2 flex flex-wrap items-center justify-center gap-1">
            <Chip active>rating: Float</Chip>
            <Arrow accent />
            <Chip active>double?</Chip>
            <Arrow accent />
            <Outcome status="error">CS0266</Outcome>
          </div>
        </div>

        {/* === Caught-at-build callout under a dashed divider === */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center font-mono text-xs">
            ProductSummary.cs(42,28): cannot convert double? to int
          </p>
        </div>

        {/* === Stat duo footer: the two numbers that matter === */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="1" label="compile error" />
          <Stat figure="0" label="reached runtime" />
        </div>
      </div>
    </div>
  );
}
