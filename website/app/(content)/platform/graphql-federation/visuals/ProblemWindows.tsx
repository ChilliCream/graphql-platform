import type { ReactNode } from "react";

import { AppWindow } from "@/src/components/AppWindow";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { CheckGlyph } from "@/src/icons/CheckGlyph";
import { CrossGlyph } from "@/src/icons/CrossGlyph";
import { RingGlyph } from "@/src/icons/RingGlyph";

type RowStatus = "pass" | "fail" | "waiting";

const STATUS: Record<
  RowStatus,
  { readonly node: ReactNode; readonly color: string }
> = {
  pass: {
    node: <CheckGlyph width={12} height={12} />,
    color: "text-cc-success",
  },
  fail: {
    node: <CrossGlyph width={12} height={12} />,
    color: "text-cc-danger",
  },
  waiting: {
    node: <RingGlyph width={12} height={12} />,
    color: "text-cc-warning",
  },
};

interface WindowRowSpec {
  readonly status: RowStatus;
  readonly name: string;
  readonly detail: string;
}

interface WindowRowProps extends WindowRowSpec {
  readonly delayClassName: string;
}

function WindowRow({ status, name, detail, delayClassName }: WindowRowProps) {
  const { node, color } = STATUS[status];
  return (
    <div className="border-cc-card-border flex items-center gap-3 border-b px-4 py-3 last:border-b-0">
      <span
        className={`flex h-5 w-5 shrink-0 items-center justify-center ${color}`}
      >
        <RevealOnScroll
          className={`flex ${delayClassName}`}
          hiddenClassName="scale-50 opacity-0 motion-reduce:scale-100"
          shownClassName="scale-100 opacity-100"
        >
          {node}
        </RevealOnScroll>
      </span>
      <span className="text-cc-heading text-[0.82rem] font-medium">{name}</span>
      <span className="text-cc-ink-dim ml-auto font-mono text-[0.7rem]">
        {detail}
      </span>
    </div>
  );
}

interface WindowFooterProps {
  readonly ok: boolean;
  readonly children: ReactNode;
}

function WindowFooter({ ok, children }: WindowFooterProps) {
  const Glyph = ok ? CheckGlyph : CrossGlyph;
  return (
    <div
      className={`flex items-center gap-2 font-mono text-[0.7rem] ${
        ok ? "text-cc-success" : "text-cc-danger"
      }`}
    >
      <Glyph width={10} height={10} className="shrink-0" />
      <span>{children}</span>
    </div>
  );
}

const DELAY_CLASSES = ["delay-0", "delay-150", "delay-300", "delay-450"];

interface StatusWindowProps {
  readonly title: string;
  readonly rows: readonly WindowRowSpec[];
  readonly footerOk: boolean;
  readonly footer: ReactNode;
}

function StatusWindow({ title, rows, footerOk, footer }: StatusWindowProps) {
  return (
    <AppWindow
      title={<span className="text-cc-prose">{title}</span>}
      footer={<WindowFooter ok={footerOk}>{footer}</WindowFooter>}
    >
      {rows.map((row, i) => (
        <WindowRow key={row.name} {...row} delayClassName={DELAY_CLASSES[i]} />
      ))}
    </AppWindow>
  );
}

const BEFORE_ROWS: readonly WindowRowSpec[] = [
  {
    status: "pass",
    name: "catalog team · add Product.rating",
    detail: "merged",
  },
  {
    status: "waiting",
    name: "billing team · Money type",
    detail: "waiting for the release train",
  },
  {
    status: "waiting",
    name: "shipping team · delivery estimate",
    detail: "waiting",
  },
  {
    status: "fail",
    name: "ordering team · rename Order.total",
    detail: "blocked · conflicts with billing",
  },
];

const AFTER_ROWS: readonly WindowRowSpec[] = [
  {
    status: "pass",
    name: "catalog · schema.graphql",
    detail: "composed · released Tue",
  },
  {
    status: "pass",
    name: "billing · schema.graphql",
    detail: "composed · released Thu",
  },
  {
    status: "pass",
    name: "shipping · schema.graphql",
    detail: "composed · released today",
  },
  {
    status: "fail",
    name: "ordering · schema.graphql",
    detail: "conflict with billing · failed at build, nothing deployed",
  },
];

/**
 * Before/after windows for the Problem section, in the release-safety
 * AppWindow style: one release queue on one schema versus subgraphs composed
 * into one composite schema. Stacked at every width so every row keeps its
 * 11 px labels on one line.
 */
export function ProblemWindows() {
  return (
    <div className="grid gap-6">
      <StatusWindow
        title="release queue · schema.graphql · one server"
        rows={BEFORE_ROWS}
        footerOk={false}
        footer="one schema · one release queue · every team waits"
      />
      <StatusWindow
        title="composition · 4 subgraphs"
        rows={AFTER_ROWS}
        footerOk
        footer="one composite schema · teams release on their own schedule"
      />
    </div>
  );
}
