import type { ReactNode } from "react";

import { AppWindow } from "@/src/components/AppWindow";
import { ButtonRow } from "@/src/components/ButtonRow";
import { DotGridSurface } from "@/src/components/DotGridSurface";
import { PageSection } from "@/src/components/PageSection";
import { StatusChip } from "@/src/components/StatusChip";
import type { ChangeStatus } from "@/src/components/StatusChip";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { tk } from "./syntaxTokens";

type DiffSign = "+" | "-" | " ";

interface DiffLine {
  readonly old: number | null;
  readonly nw: number | null;
  readonly sign: DiffSign;
  readonly code: ReactNode;
  readonly status?: ChangeStatus;
  readonly pinned?: boolean;
}

function diffRowColor(sign: DiffSign): string {
  if (sign === "+") {
    return "bg-cc-success/[0.06]";
  }
  if (sign === "-") {
    return "bg-cc-danger/[0.07]";
  }
  return "";
}

function gutterColor(sign: DiffSign): string {
  if (sign === "+") {
    return "text-cc-success/70";
  }
  if (sign === "-") {
    return "text-cc-danger/70";
  }
  return "text-cc-nav-label";
}

interface DiffRowProps {
  readonly line: DiffLine;
}

function DiffRow({ line }: DiffRowProps) {
  return (
    <div className={`flex items-stretch ${diffRowColor(line.sign)}`}>
      <span className="border-cc-card-border text-cc-ink-dim w-9 shrink-0 border-r py-1 pr-2 text-right font-mono text-[0.66rem] select-none">
        {line.old ?? ""}
      </span>
      <span className="border-cc-card-border text-cc-ink-dim w-9 shrink-0 border-r py-1 pr-2 text-right font-mono text-[0.66rem] select-none">
        {line.nw ?? ""}
      </span>
      <span
        className={`w-5 shrink-0 py-1 pl-2 font-mono text-[0.78rem] select-none ${gutterColor(line.sign)}`}
      >
        {line.sign}
      </span>
      <span className="text-cc-prose min-w-0 flex-1 py-1 pr-3 font-mono text-[0.78rem] leading-relaxed whitespace-pre">
        {line.code}
      </span>
    </div>
  );
}

interface HeroDiffLine extends DiffLine {
  readonly key: string;
}

const HERO_DIFF: readonly HeroDiffLine[] = [
  {
    key: "type-order",
    old: 41,
    nw: 41,
    sign: " ",
    code: (
      <>
        {tk.kw("type")} {tk.ty("Order")} {tk.punc("{")}
      </>
    ),
  },
  {
    key: "id",
    old: 42,
    nw: 42,
    sign: " ",
    code: (
      <>
        {"  "}
        {tk.fld("id")}
        {tk.punc(": ID!")}
      </>
    ),
  },
  {
    key: "totalAmount",
    old: null,
    nw: 43,
    sign: "+",
    code: (
      <>
        {"  "}
        {tk.fld("totalAmount")}
        {tk.punc(": ")}
        {tk.ty("Money!")}
      </>
    ),
    status: "safe",
  },
  {
    key: "total",
    old: 43,
    nw: null,
    sign: "-",
    code: (
      <>
        {"  "}
        {tk.fld("total")}
        {tk.punc(": ")}
        {tk.ty("Float!")}
      </>
    ),
    status: "breaking",
    pinned: true,
  },
  {
    key: "status",
    old: 44,
    nw: 44,
    sign: " ",
    code: (
      <>
        {"  "}
        {tk.fld("status")}
        {tk.punc(": ")}
        {tk.ty("OrderStatus!")}
      </>
    ),
  },
  {
    key: "placedAt",
    old: null,
    nw: 45,
    sign: "+",
    code: (
      <>
        {"  "}
        {tk.fld("placedAt")}
        {tk.punc(": ")}
        {tk.ty("DateTime")} {tk.dir("@deprecated")}
        {tk.punc('(reason: "use createdAt")')}
      </>
    ),
    status: "dangerous",
  },
  {
    key: "close",
    old: 45,
    nw: 46,
    sign: " ",
    code: <>{tk.punc("}")}</>,
  },
];

function PinnedThread() {
  return (
    <div className="border-cc-danger/50 bg-cc-danger/[0.05] ml-[3.55rem] border-l-2">
      <div className="px-4 py-3">
        <div className="flex items-center gap-2">
          <span className="bg-cc-danger/15 text-cc-danger flex h-5 w-5 items-center justify-center rounded-full font-mono text-[0.6rem] font-semibold">
            R
          </span>
          <span className="text-cc-heading text-[0.78rem] font-medium">
            Registry
          </span>
          <StatusChip status="breaking" />
          <span className="text-cc-ink-dim ml-auto font-mono text-[0.62rem]">
            line 43
          </span>
        </div>
        <p className="text-cc-ink-dim mt-2 text-[0.78rem] leading-relaxed">
          Removing{" "}
          <code className="bg-cc-hover text-cc-prose rounded px-1 font-mono text-[0.72rem]">
            Order.total
          </code>{" "}
          breaks queries that still select it.{" "}
          <span className="text-cc-prose">
            Queries and mutations from 3 client versions published to this stage
            are affected.
          </span>{" "}
          Deprecate it, then remove it after those versions are retired or
          unpublished from the stage.
        </p>
      </div>
    </div>
  );
}

function HeroDiffMock() {
  return (
    <AppWindow
      title={<span className="text-cc-prose">schema.graphql</span>}
      footer={
        <div className="flex items-center justify-between">
          <span className="text-cc-ink-dim flex items-center gap-2 font-mono text-[0.66rem]">
            <span className="bg-cc-danger h-2 w-2 rounded-full" />
            registry check failed
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.66rem]">
            1 breaking · 1 dangerous · 1 safe
          </span>
        </div>
      }
    >
      <div
        role="region"
        aria-label="Illustrative schema diff"
        tabIndex={0}
        className="focus-visible:ring-cc-accent/40 overflow-x-auto focus-visible:ring-2 focus-visible:outline-none"
      >
        <div className="min-w-[30rem]">
          <div className="bg-cc-success/[0.04] text-cc-ink-dim px-4 py-1.5 font-mono text-[0.64rem]">
            @@ type Order @@
          </div>
          <div>
            {HERO_DIFF.map((line) => (
              <div key={line.key}>
                <DiffRow line={line} />
                {line.pinned === true && <PinnedThread />}
              </div>
            ))}
          </div>
        </div>
      </div>
    </AppWindow>
  );
}

export function HeroSection() {
  return (
    <DotGridSurface className="border-cc-card-border/50 bg-cc-surface/25 relative left-1/2 -mt-14 w-screen -translate-x-1/2 overflow-hidden border-b py-16 sm:py-24">
      <PageSection className="grid items-center gap-12 lg:grid-cols-[minmax(0,0.92fr)_minmax(0,1.08fr)]">
        <div className="min-w-0">
          <h1 className="font-heading text-h2 text-cc-heading font-bold tracking-tight">
            Ship GraphQL schema changes
            <br />
            with confidence.
          </h1>
          <p className="text-body text-cc-prose mt-5 max-w-xl leading-relaxed">
            Nitro&apos;s schema checks test a proposed change against the
            operations your clients rely on in production. Breaking change
            detection shows what would break, so you can fix it before it
            reaches users.
          </p>
          <ButtonRow align="start" className="mt-9">
            <SolidButton href="https://nitro.chillicream.com">
              Start for free
            </SolidButton>
            <OutlineButton href="/docs/nitro/apis/client-registry">
              Read client registry docs
            </OutlineButton>
          </ButtonRow>
        </div>
        <div className="relative min-w-0">
          <div
            aria-hidden
            className="absolute -inset-6 -z-10 rounded-3xl opacity-60 blur-2xl"
            style={{
              background:
                "radial-gradient(60% 60% at 70% 20%, rgba(124,146,198,0.18), transparent 70%)",
            }}
          />
          <HeroDiffMock />
        </div>
      </PageSection>
    </DotGridSurface>
  );
}
