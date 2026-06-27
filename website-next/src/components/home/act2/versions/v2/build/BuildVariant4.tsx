import type { ReactNode } from "react";

interface BuildVariant4Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration, v2 "Flow Diagrams" (concept 4:
 * build feedback before runtime).
 *
 * A left-to-right PIPELINE in the locked v2 flow vocabulary: the `dotnet build`
 * source node runs the source generator, which emits types and a schema, and
 * settles green before anything runs. The single teal path traces that one
 * route from `dotnet build` to the green `build: passed` terminus, so the
 * errors-surface-at-build-time relationship reads at a glance. The runtime
 * stage sits past a dashed deferred hop as a not-yet-reached dashed chip, and a
 * Stat duo footer carries the two numbers (errors caught at build, errors left
 * for runtime). cc-* palette only, 1px strokes, settled final frame, no
 * animation. Every styled id is prefixed "v2-build-4-".
 */

const cc = {
  healthy: "#34d399",
} as const;

export function BuildVariant4({ className }: BuildVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          build before runtime
        </p>

        {/* primary pipeline: dotnet build runs the generator and settles green */}
        <div className="mt-4 flex flex-wrap items-center justify-center gap-1">
          <Chip active>dotnet build</Chip>
          <Arrow accent />
          <Chip>source generator</Chip>
          <Arrow accent />
          <Chip>types + schema</Chip>
          <Arrow accent />
          <PassChip>build: passed</PassChip>
        </div>

        {/* deferred hop: only after a green build does anything actually run */}
        <div className="mt-4 flex items-center justify-center gap-2">
          <Arrow deferred />
          <DashedChip>runtime</DashedChip>
        </div>

        {/* two key numbers: caught at build vs. left for runtime */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="100%" label="errors caught at build" />
          <Stat figure="0" label="errors left for runtime" />
        </div>

        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            type and schema errors fail the compile, not the request
          </p>
        </div>
      </div>
    </div>
  );
}

/* primitives, extending the ScrollScenes Chip + Arrow vocabulary */

/** Rounded mono chip for an entity flowing through the pipeline. */
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

/** Terminal/derived pass node: tighter rounded-md pill with a healthy border. */
function PassChip({ children }: { readonly children: ReactNode }) {
  return (
    <span
      className="bg-cc-surface rounded-md border px-2 py-1 font-mono text-[0.65rem] whitespace-nowrap"
      style={{ borderColor: cc.healthy, color: cc.healthy }}
    >
      {children}
    </span>
  );
}

/** Not-yet-reached node: dashed faint border, dim ink (the WorkflowFlow pattern). */
function DashedChip({ children }: { readonly children: ReactNode }) {
  return (
    <span className="border-cc-ink-faint text-cc-ink-dim bg-cc-surface rounded-md border border-dashed px-2 py-1 font-mono text-[0.65rem] whitespace-nowrap">
      {children}
    </span>
  );
}

/** The ScrollScenes text arrow: grey by default, teal on the traced path. */
function Arrow({
  accent = false,
  deferred = false,
}: {
  readonly accent?: boolean;
  readonly deferred?: boolean;
}) {
  if (deferred) {
    return (
      <span
        aria-hidden="true"
        className="text-cc-ink-faint px-0.5 text-sm tracking-[0.1em]"
      >
        &middot;&middot;&rarr;
      </span>
    );
  }
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

/** Big display numeral with a small caption (the ScrollScenes Stat). */
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
