import { AppWindow } from "@/src/components/AppWindow";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { tk } from "@/src/components/syntaxTokens";
import { CheckGlyph } from "@/src/icons/CheckGlyph";

/**
 * The `both-specifications` panel: a DOM window, in the release-safety
 * impact-matrix rhythm, listing one row per subgraph with the specification
 * it composes through. Rows reveal on scroll with a staggered delay; a rest
 * frame (every subgraph composed) is always the final state, so
 * `motion-reduce` needs nothing special beyond what `RevealOnScroll` already
 * honours. Below 500px of its own width the protocol badge has no room beside the
 * name, so a container query (scoped to this window, not the viewport)
 * stacks it under the name instead of shrinking any text.
 */

type Protocol = "GraphQL Federation" | "Apollo Federation" | "OpenAPI" | "gRPC";

interface SubgraphRow {
  readonly name: string;
  readonly language: string;
  readonly protocol: Protocol;
}

const SUBGRAPHS: readonly SubgraphRow[] = [
  { name: "catalog", language: "TypeScript", protocol: "GraphQL Federation" },
  { name: "billing", language: "Java", protocol: "Apollo Federation" },
  { name: "ordering", language: "Go", protocol: "GraphQL Federation" },
  { name: "shipping", language: "Ruby", protocol: "OpenAPI" },
  { name: "inventory", language: "C#", protocol: "gRPC" },
];

/** Delay class per row, in order, so the reveal steps down the list. */
const ROW_DELAYS = ["", "delay-75", "delay-150", "delay-200", "delay-300"];

/**
 * Two tracks (name, status) below 500px of the window's own width; three
 * (name, badge, status) above it. The breakpoint is a Tailwind container
 * query, so it's the window's rendered width that decides, not the
 * viewport's — every class below is a static literal (Tailwind's scanner
 * can't see classes built by interpolating a JS constant into a string).
 */
const GRID_CLASS =
  "grid grid-cols-[1fr_auto] items-center gap-x-4 gap-y-1 @min-[500px]:grid-cols-[minmax(0,1fr)_auto_auto] @min-[500px]:gap-y-0";
const NAME_CELL_CLASS = "col-start-1 row-start-1 min-w-0";
const BADGE_CELL_CLASS =
  "col-start-1 row-start-2 justify-self-start @min-[500px]:col-start-2 @min-[500px]:row-start-1";
const STATUS_CELL_CLASS =
  "col-start-2 row-start-1 justify-self-end @min-[500px]:col-start-3";
const PROTOCOL_HEADER_CLASS =
  "hidden @min-[500px]:col-start-2 @min-[500px]:row-start-1 @min-[500px]:block";

const ROW_CLASS = "border-cc-card-border border-b px-4 py-3 last:border-b-0";
const HEADER_LABEL_CLASS =
  "text-cc-ink-dim font-mono text-[0.7rem] tracking-[0.14em] uppercase";
const BADGE_CLASS =
  "border-cc-nav-text/30 text-cc-nav-text inline-flex items-center whitespace-nowrap rounded-full border px-3 py-1 font-mono text-[0.7rem] tracking-[0.12em] uppercase";

interface ProtocolBadgeProps {
  readonly protocol: Protocol;
}

function ProtocolBadge({ protocol }: ProtocolBadgeProps) {
  return <span className={BADGE_CLASS}>{protocol}</span>;
}

function ComposedStatus() {
  return (
    <span className="text-cc-success inline-flex items-center gap-1.5 font-mono text-[0.7rem]">
      <CheckGlyph className="h-4 w-4" />
      composed
    </span>
  );
}

export function ProtocolsWindow() {
  return (
    <AppWindow
      title={<span className="text-cc-prose">fusion · subgraphs</span>}
      footer={
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="text-cc-ink-dim font-mono text-[0.7rem]">
            composition
          </span>
          <span className="text-cc-success font-mono text-[0.7rem]">
            ✓ composed 5 subgraphs · 1 coherent graph
          </span>
        </div>
      }
    >
      <div className="@container">
        <div
          className={`${GRID_CLASS} border-cc-card-border border-b px-4 py-2`}
        >
          <span className={`${NAME_CELL_CLASS} ${HEADER_LABEL_CLASS}`}>
            subgraph
          </span>
          <span className={`${PROTOCOL_HEADER_CLASS} ${HEADER_LABEL_CLASS}`}>
            protocol
          </span>
          <span className={`${STATUS_CELL_CLASS} ${HEADER_LABEL_CLASS}`}>
            status
          </span>
        </div>

        {SUBGRAPHS.map((row, i) => (
          <RevealOnScroll
            key={row.name}
            className={`${GRID_CLASS} ${ROW_CLASS} ${ROW_DELAYS[i]}`}
          >
            <div className={`${NAME_CELL_CLASS} font-mono text-[0.7rem]`}>
              <span className="text-cc-heading">{row.name}</span>
              <span className="text-cc-ink-dim"> · {row.language}</span>
            </div>
            <div className={BADGE_CELL_CLASS}>
              <ProtocolBadge protocol={row.protocol} />
            </div>
            <div className={STATUS_CELL_CLASS}>
              <ComposedStatus />
            </div>
          </RevealOnScroll>
        ))}

        <RevealOnScroll className="border-cc-card-border border-t px-4 py-3 delay-500">
          <div className="font-mono text-[0.7rem]">
            {tk.punc("$ ")}
            {tk.fld("fusion compose")}
          </div>
        </RevealOnScroll>
      </div>
    </AppWindow>
  );
}
