import type { ReactNode } from "react";

interface FeedbackVariant4Props {
  readonly className?: string;
}

/**
 * Agentic coding, concept 4 "Governed tool lifecycle", v2 "Flow Diagrams".
 *
 * Re-expresses the governed release path for one tool as a left-to-right
 * PIPELINE in the locked cc-* flow vocabulary (chips joined by text arrows on a
 * cc-card-bg panel). The tool `search-eshops-catalog` walks a single continuous
 * teal path: author -> validate -> stage -> trace -> approval gate -> production.
 * The approval gate is a status-bordered chip (healthy/granted) interrupting the
 * teal path, the only place a status color appears. The production terminus is a
 * tighter rounded-md "derived/settled" pill. A Stat duo footers the two key
 * numbers. One teal path, everything else cream/grey. Fully static, no hooks.
 */

const FONT_HEADING = '"Josefin Sans", Futura, sans-serif';

/** Promotion stages before the gate; the whole row carries the teal path. */
const STAGES: readonly string[] = ["author", "validate", "stage", "trace"];

/** A rounded-lg mono chip on the traced path (always the active teal style). */
function PathChip({ children }: { readonly children: ReactNode }) {
  return (
    <span className="border-cc-accent/60 text-cc-accent bg-cc-surface rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap">
      {children}
    </span>
  );
}

/** The thin teal text arrow used between chips on the traced path. */
function TealArrow() {
  return (
    <span aria-hidden="true" className="text-cc-accent px-0.5 text-sm">
      &rarr;
    </span>
  );
}

/** Big display numeral + small caption, the ScrollScenes Stat. */
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
        className="text-cc-heading text-h4 leading-none font-semibold"
        style={{ fontFamily: FONT_HEADING }}
      >
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-1.5 text-xs">{label}</p>
    </div>
  );
}

export function FeedbackVariant4({ className }: FeedbackVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          governed tool lifecycle
        </p>

        {/* The tool being promoted: a labelled box (source of the teal path). */}
        <div className="border-cc-card-border bg-cc-surface mt-3 rounded-lg border px-3 py-2">
          <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.08em] uppercase">
            tool
          </p>
          <p className="text-cc-ink mt-0.5 font-mono text-xs">
            search-eshops-catalog
          </p>
        </div>

        {/* The promotion path: author -> validate -> stage -> trace, all teal. */}
        <div className="mt-4 flex flex-wrap items-center justify-center gap-1">
          {STAGES.map((stage, index) => (
            <span key={stage} className="flex items-center">
              {index > 0 && <TealArrow />}
              <PathChip>{stage}</PathChip>
            </span>
          ))}
        </div>

        {/* The resolved approval gate: a healthy-bordered chip interrupting the
            teal path, then the settled production terminus (rounded-md). */}
        <div className="mt-3 flex flex-wrap items-center justify-center gap-1">
          <TealArrow />
          <span
            className="bg-cc-surface flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap"
            style={{ borderColor: "#34d399", color: "#34d399" }}
          >
            <span
              aria-hidden="true"
              className="inline-block h-1.5 w-1.5 rounded-full"
              style={{ background: "#34d399" }}
            />
            gate: granted
          </span>
          <TealArrow />
          <span className="border-cc-accent/60 text-cc-accent bg-cc-surface rounded-md border px-2 py-1 font-mono text-[0.65rem] whitespace-nowrap">
            production
          </span>
        </div>

        {/* The operation that actually ships, under a dashed divider caption. */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center font-mono text-[0.65rem]">
            searchCatalog(term: &quot;shoes&quot;)
          </p>
        </div>

        {/* Two key numbers for the promotion. */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="6" label="governed stages" />
          <Stat figure="1" label="approval gate" />
        </div>
      </div>
    </div>
  );
}
