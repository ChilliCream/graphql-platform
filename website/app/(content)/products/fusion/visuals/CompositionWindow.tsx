import { AppWindow } from "@/src/components/AppWindow";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { tk } from "@/src/components/syntaxTokens";

/**
 * The `any-server` panel: a DOM terminal window, in the release-safety
 * CI-card style, telling the federation page's composition-fails-the-build
 * story in Fusion vocabulary (subgraph, coherent graph). Rows reveal on
 * scroll with a staggered delay; a rest frame (the fixed, passing run) is
 * always the final state, so `motion-reduce` needs nothing special beyond
 * what `RevealOnScroll` already honours.
 */

const ROW_CLASS = "border-cc-card-border border-b px-4 py-3 last:border-b-0";
const CODE_CLASS = "font-mono text-[0.7rem] leading-relaxed whitespace-pre";

interface DiffRowProps {
  readonly sign: "-" | "+";
  readonly code: string;
}

function DiffRow({ sign, code }: DiffRowProps) {
  const added = sign === "+";
  return (
    <div
      className={`flex items-stretch ${added ? "bg-cc-success/[0.06]" : "bg-cc-danger/[0.07]"}`}
    >
      <span
        className={`w-5 shrink-0 py-0.5 pl-1 ${CODE_CLASS} select-none ${added ? "text-cc-success/70" : "text-cc-danger/70"}`}
      >
        {sign}
      </span>
      <span
        className={`text-cc-prose min-w-0 flex-1 py-0.5 pr-2 ${CODE_CLASS}`}
      >
        {code}
      </span>
    </div>
  );
}

export function CompositionWindow() {
  return (
    <AppWindow
      title={<span className="text-cc-prose">ci · composition</span>}
      footer={
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="text-cc-ink-dim font-mono text-[0.7rem]">
            pipeline gate
          </span>
          <span className="text-cc-success font-mono text-[0.7rem]">
            ✓ composition passed
          </span>
        </div>
      }
    >
      <RevealOnScroll className={ROW_CLASS}>
        <div className="text-cc-ink-dim font-mono text-[0.7rem] tracking-[0.08em] uppercase">
          schema change · billing
        </div>
        <div className="mt-2 space-y-0.5">
          <DiffRow sign="-" code="id: ID!" />
          <DiffRow sign="+" code="id: Int!" />
        </div>
      </RevealOnScroll>

      <RevealOnScroll className={`${ROW_CLASS} delay-150`}>
        <div className={CODE_CLASS}>
          <div>
            {tk.punc("$ ")}
            {tk.fld("fusion compose")}
          </div>
          <div className="text-cc-danger">
            ✕ OUTPUT_FIELD_TYPES_NOT_MERGEABLE
          </div>
          <div>{tk.punc("  Product.id: Int! (billing) ≠ ID! (catalog)")}</div>
          <div className="text-cc-danger">✕ exit 1 · nothing deployed</div>
        </div>
      </RevealOnScroll>

      <RevealOnScroll className={`${ROW_CLASS} delay-300`}>
        <div className={CODE_CLASS}>
          <div>
            {tk.punc("$ ")}
            {tk.fld("fusion compose")}
          </div>
          <div className="text-cc-success">
            ✓ composed 5 subgraphs · 0 errors
          </div>
          <div className="text-cc-accent">
            → coherent graph · loaded by the gateway
          </div>
        </div>
      </RevealOnScroll>
    </AppWindow>
  );
}
