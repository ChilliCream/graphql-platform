import type { ReactNode } from "react";

interface BuildVariant5Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration (v2 "Flow Diagrams", concept #5):
 * the glue tangle collapses to one token.
 *
 * BEFORE / AFTER topology. The top mini-flow shows four separately
 * hand-maintained glue files (schema, types, client, dtos) converging onto a
 * single "keep in sync" join, with one coral edge flagged as drift. A dashed
 * divider separates it from the bottom mini-flow, which carries the single teal
 * path: one [QueryType] ProductApi source token fans out to the resolver
 * pipeline and client, all generated from it. A Stat duo counts the collapse
 * (4 glue files -> 1 source of truth).
 *
 * Built from the ScrollScenes Chip / Arrow / Stat vocabulary in the locked
 * cc-* palette. Settled final frame: static, no animation, no hooks, no client
 * APIs. React Server Component. Every svg id is prefixed "v2-build-5-".
 */

const PREFIX = "v2-build-5-";

/** Rounded mono chip, matching the ScrollScenes Chip. */
function Chip({
  children,
  active = false,
  derived = false,
  dashed = false,
}: {
  readonly children: ReactNode;
  readonly active?: boolean;
  readonly derived?: boolean;
  readonly dashed?: boolean;
}) {
  return (
    <span
      className={[
        "bg-cc-surface whitespace-nowrap border font-mono",
        derived
          ? "rounded-md px-2 py-1 text-[0.6rem]"
          : "rounded-lg px-2.5 py-1.5 text-[0.65rem]",
        active
          ? "border-cc-accent/60 text-cc-accent"
          : dashed
            ? "border-cc-ink-faint text-cc-ink-dim border-dashed"
            : "border-cc-card-border text-cc-ink",
      ].join(" ")}
    >
      {children}
    </span>
  );
}

/** The grey text arrow from ScrollScenes. */
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

export function BuildVariant5({ className }: BuildVariant5Props) {
  const glue = ["schema", "types", "client", "dtos"];

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div
        className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm"
        aria-hidden="true"
      >
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          glue collapse
        </p>

        {/* BEFORE: four hand-wired glue files converge into one fragile join */}
        <div className="mt-4">
          <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.1em] uppercase">
            before
          </p>
          <div className="mt-2 flex items-stretch gap-2.5">
            <div className="flex flex-col gap-1">
              {glue.map((file, index) => (
                <span key={file} className="flex items-center gap-1.5">
                  <Chip dashed={index === 2}>{file}</Chip>
                  <MergeStub coral={index === 2} />
                </span>
              ))}
            </div>
            <div className="flex items-center">
              <Chip>kept in sync</Chip>
            </div>
          </div>
        </div>

        {/* AFTER: one source token generates the pipeline and client */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-4">
          <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.1em] uppercase">
            after
          </p>
          <div className="mt-2 flex items-center justify-center gap-1">
            <Chip active>[QueryType] ProductApi</Chip>
          </div>
          <div className="mt-2 flex items-center justify-center gap-1">
            <Arrow accent />
            <Chip derived>resolver pipeline</Chip>
            <Arrow />
            <Chip derived>client</Chip>
          </div>
        </div>

        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="4" label="hand-wired glue files" />
          <Stat figure="1" label="generated from source" />
        </div>
      </div>
    </div>
  );
}

/**
 * A short 1px converging connector with a thin open arrowhead, drawn as inline
 * SVG so the four glue rows merge toward the single "kept in sync" join. The
 * flagged drift row renders the same stub in coral status.
 */
function MergeStub({ coral }: { readonly coral: boolean }) {
  const stroke = coral ? "#f0786a" : "rgba(245,241,234,0.16)";
  return (
    <svg
      width="18"
      height="12"
      viewBox="0 0 18 12"
      fill="none"
      aria-hidden="true"
      className="shrink-0"
    >
      <defs>
        <marker
          id={`${PREFIX}head${coral ? "-drift" : ""}`}
          markerWidth="5"
          markerHeight="5"
          refX="4"
          refY="2.5"
          orient="auto"
        >
          <path d="M0 0L5 2.5L0 5" fill="none" stroke={stroke} strokeWidth="1" />
        </marker>
      </defs>
      <line
        x1="0"
        y1="6"
        x2="13"
        y2="6"
        stroke={stroke}
        strokeWidth="1"
        strokeDasharray={coral ? "2 2" : undefined}
        markerEnd={`url(#${PREFIX}head${coral ? "-drift" : ""})`}
      />
    </svg>
  );
}
