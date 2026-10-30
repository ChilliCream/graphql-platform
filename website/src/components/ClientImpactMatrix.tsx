import type { ReactNode } from "react";

import { AppWindow } from "@/src/components/AppWindow";

interface ClientImpactRow {
  readonly client: string;
  readonly environment: string;
  readonly ok: number;
  readonly total: number;
  readonly status: "ok" | "risk" | "outside";
  readonly note?: string;
}

interface ClientImpactMatrixProps {
  readonly title: ReactNode;
  readonly rows: readonly ClientImpactRow[];
  readonly density?: "compact" | "comfortable";
}

interface ImpactBarProps {
  readonly ok: number;
  readonly total: number;
  readonly status: ClientImpactRow["status"];
  readonly comfortable: boolean;
}

// Below 400px of the operations cell's own container width, a full-size bar
// plus its ok/total label no longer fit the compact column, so the label
// wraps under a shrinkable bar instead of spilling into the status column
// (a Tailwind container query, scoped to the row, not the viewport). At and
// above it every compact class is the literal it always was, so the desktop
// widths render byte-identical. Every class below is a static string:
// Tailwind's scanner can't see one built by interpolating a JS constant.
function ImpactBar({ ok, total, status, comfortable }: ImpactBarProps) {
  const cells = Array.from({ length: total });
  const color =
    status === "ok"
      ? "bg-cc-success"
      : status === "risk"
        ? "bg-cc-warning"
        : "bg-cc-ink-dim/50";
  const barCls = comfortable
    ? "flex min-w-0 gap-1"
    : "flex gap-1 @max-[399px]:min-w-0";
  const cellCls = comfortable
    ? "h-2 w-5 min-w-0 shrink rounded-[2px]"
    : "h-2 w-5 rounded-[2px] @max-[399px]:min-w-0 @max-[399px]:shrink";
  return (
    <span className={barCls}>
      {cells.map((_, i) => (
        <span
          key={i}
          className={`${cellCls} ${i < ok ? color : "bg-cc-ink-faint"}`}
        />
      ))}
    </span>
  );
}

export function ClientImpactMatrix({
  title,
  rows,
  density = "compact",
}: ClientImpactMatrixProps) {
  const comfortable = density === "comfortable";
  const statusLabel: Record<
    ClientImpactRow["status"],
    { text: string; cls: string }
  > = {
    ok: { text: "OK", cls: "text-cc-success" },
    risk: { text: "at risk", cls: "text-cc-warning" },
    outside: { text: "outside result", cls: "text-cc-ink-dim" },
  };
  const headerSize = comfortable ? "text-[0.7rem]" : "text-[0.6rem]";
  const environmentSize = comfortable ? "text-[0.7rem]" : "text-[0.62rem]";
  const barLabelSize = comfortable ? "text-[0.7rem]" : "text-[0.68rem]";
  return (
    <AppWindow title={title}>
      <div className="@container">
        <table className="w-full table-fixed border-collapse">
          <colgroup>
            <col className="w-[43.3%]" />
            <col className="w-[33.3%]" />
            <col className="w-[26.6%]" />
          </colgroup>
          <thead>
            <tr
              className={`border-cc-card-border text-cc-ink-dim border-b font-mono ${headerSize} tracking-[0.14em] uppercase`}
            >
              <th scope="col" className="px-4 py-2 text-left font-normal">
                client
              </th>
              <th scope="col" className="px-4 py-2 text-left font-normal">
                operations passing
              </th>
              <th scope="col" className="px-4 py-2 text-right font-normal">
                status
              </th>
            </tr>
          </thead>
          <tbody>
            {rows.map((c) => {
              const s = statusLabel[c.status];
              return (
                <tr
                  key={c.client}
                  className="border-cc-card-border border-b last:border-b-0"
                >
                  <td className="min-w-0 px-4 py-3">
                    <div className="text-cc-heading truncate font-mono text-[0.78rem]">
                      {c.client}
                    </div>
                    <div
                      className={`text-cc-ink-dim font-mono ${environmentSize}`}
                    >
                      {c.environment}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <div
                      className={
                        comfortable
                          ? "flex min-w-0 flex-wrap items-center gap-3"
                          : "flex items-center gap-3 @max-[399px]:min-w-0 @max-[399px]:flex-wrap"
                      }
                    >
                      {c.total === 0 ? (
                        <span
                          className={`text-cc-heading font-mono ${barLabelSize}`}
                        >
                          {c.note ?? "none published"}
                        </span>
                      ) : (
                        <>
                          <ImpactBar
                            ok={c.ok}
                            total={c.total}
                            status={c.status}
                            comfortable={comfortable}
                          />
                          <span
                            className={
                              comfortable
                                ? `text-cc-ink-dim font-mono ${barLabelSize} shrink-0`
                                : `text-cc-ink-dim font-mono ${barLabelSize} @max-[399px]:shrink-0`
                            }
                          >
                            {c.ok}/{c.total}
                          </span>
                        </>
                      )}
                    </div>
                  </td>
                  <td
                    className={`px-4 py-3 text-right font-mono text-[0.72rem] font-semibold ${s.cls}`}
                  >
                    {s.text}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </AppWindow>
  );
}
