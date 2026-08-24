import type { ReactNode } from "react";

import { AppWindow } from "@/src/components/AppWindow";
import { CheckIcon } from "@/src/components/CheckIcon";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { SectionShell } from "@/src/components/SectionShell";
import { CrossGlyph } from "@/src/icons/CrossGlyph";
import { SpinnerGlyph } from "@/src/icons/SpinnerGlyph";

function ValidatingDots() {
  return (
    <span>
      validating
      {[0, 1, 2].map((i) => (
        <span
          key={i}
          className="animate-[ellipsis-blink_1.2s_ease-in-out_infinite] motion-reduce:animate-none"
          style={{ animationDelay: `${i * 0.2}s` }}
        >
          .
        </span>
      ))}
    </span>
  );
}

interface CheckRowProps {
  readonly icon: "fail" | "pass" | "run";
  readonly name: string;
  readonly detail: ReactNode;
  readonly delayClassName: string;
}

function CheckRow({ icon, name, detail, delayClassName }: CheckRowProps) {
  const map = {
    fail: {
      node: <CrossGlyph width={12} height={12} />,
      color: "text-cc-danger",
    },
    pass: { node: <CheckIcon size={12} />, color: "text-cc-success" },
    run: {
      node: <SpinnerGlyph width={12} height={12} className="animate-spin motion-reduce:animate-none" />,
      color: "text-cc-warning",
    },
  } as const;
  const m = map[icon];
  return (
    <div className="border-cc-card-border flex items-center gap-3 border-b px-4 py-3 last:border-b-0">
      <span className={`flex h-5 w-5 items-center justify-center ${m.color}`}>
        <RevealOnScroll
          className={`flex ${delayClassName}`}
          hiddenClassName="scale-50 opacity-0 motion-reduce:scale-100"
          shownClassName="scale-100 opacity-100"
        >
          {m.node}
        </RevealOnScroll>
      </span>
      <span className="text-cc-heading text-[0.82rem] font-medium">{name}</span>
      <span className="text-cc-ink-dim ml-auto font-mono text-[0.7rem]">{detail}</span>
    </div>
  );
}

function CheckCard() {
  return (
    <AppWindow
      title={<span className="text-cc-prose">#482 Add Money type</span>}
      footer={
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="text-cc-ink-dim font-mono text-[0.66rem]">
            Required CI policy blocks merge until checks pass.
          </span>
          <span className="bg-cc-hover text-cc-prose ring-cc-card-border rounded-md px-2.5 py-1 font-mono text-[0.64rem] ring-1 ring-inset">
            Re-run check
          </span>
        </div>
      }
    >
      <div className="border-cc-card-border flex items-center gap-3 border-b px-4 py-3.5">
        <span className="bg-cc-danger/15 text-cc-danger ring-cc-danger/30 flex items-center gap-2 rounded-md px-2.5 py-1 font-mono text-[0.66rem] font-semibold tracking-wide ring-1 ring-inset">
          <CrossGlyph width={12} height={12} /> FAIL
        </span>
        <span className="text-cc-heading text-[0.84rem] font-medium">Registry check</span>
        <span className="text-cc-ink-dim ml-auto font-mono text-[0.68rem]">1 breaking · 2 safe</span>
      </div>
      <CheckRow
        icon="fail"
        name="Schema validation — breaking change"
        detail="Order.total removed"
        delayClassName="delay-0"
      />
      <CheckRow
        icon="pass"
        name="Schema validation — additive"
        detail="Money, totalAmount added"
        delayClassName="delay-150"
      />
      <CheckRow
        icon="run"
        name="Client compatibility — partner app"
        detail={<ValidatingDots />}
        delayClassName="delay-300"
      />
    </AppWindow>
  );
}

export function CheckCardSection() {
  return (
    <SectionShell
      title="Breaking changes fail the pull request."
      lead="Nitro fails the pull request when a proposed change would break an operation your clients have published. The problem shows up as a red status on the PR, not as an incident after release. Mark it as required and the merge button stays locked until the schema is safe."
      artifact={<CheckCard />}
      flip
    />
  );
}
