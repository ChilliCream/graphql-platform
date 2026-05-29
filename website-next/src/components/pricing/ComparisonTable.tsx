import {
  type Cell,
  COMPARISON_COLUMNS,
  COMPARISON_MATRIX,
  type ComparisonColumnKey,
} from "@/src/data/pricing/comparisonMatrix";
import { CheckIcon } from "./CheckIcon";

function ComparisonCell({ cell, isAccent }: { cell: Cell; isAccent: boolean }) {
  const tdClass =
    "border-b border-[rgba(245,241,234,0.06)] px-4 py-4 align-top text-sm text-[var(--cc-ink)]" +
    (isAccent ? " bg-[rgba(20,36,60,0.35)]" : "");
  switch (cell.kind) {
    case "check":
      return (
        <td className={tdClass}>
          <span className="block text-fuchsia-400" aria-label="Included">
            <CheckIcon size={16} />
          </span>
        </td>
      );
    case "value":
      return (
        <td className={tdClass}>
          <span className="text-[var(--cc-ink)]">{cell.label}</span>
        </td>
      );
    case "meter":
      return (
        <td className={tdClass}>
          <span className="flex flex-col gap-1">
            <span className="font-medium text-[var(--cc-ink)]">
              {cell.included}
            </span>
            {cell.overage && cell.unit && (
              <span className="font-mono text-[11px] tracking-[0.04em] text-[var(--cc-ink-dim)]">
                then {cell.overage} / {cell.unit}
              </span>
            )}
          </span>
        </td>
      );
    case "custom":
      return (
        <td className={tdClass}>
          <span
            className="font-mono text-[11px] uppercase tracking-[0.18em] text-[var(--cc-ink-dim)]"
            aria-label="Custom — contact sales"
          >
            Custom
          </span>
        </td>
      );
    case "none":
    default:
      return (
        <td className={tdClass}>
          <span
            className="font-mono text-sm text-[var(--cc-ink-faint)]"
            aria-label="Not included"
          >
            —
          </span>
        </td>
      );
  }
}

// Sticky thead engages once the user scrolls past the section heading,
// sitting below the 72px sticky site header so column titles stay visible.
const STICKY_TH =
  "sticky top-[72px] z-20 border-b border-fuchsia-400/30 px-4 pb-4 pt-5 text-left align-bottom backdrop-blur-md";

export function ComparisonTable() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-7 max-w-2xl text-center">
        <div className="mb-2 font-mono text-xs font-semibold uppercase tracking-widest text-[var(--cc-ink-dim)]">
          Compare plans
        </div>
        <h2 className="text-3xl font-semibold tracking-tight text-[var(--cc-ink)] sm:text-4xl">
          Every meter, every cell.
        </h2>
      </div>

      <p className="mx-auto mb-9 max-w-3xl border-l-2 border-fuchsia-400 px-5 py-3.5 text-sm leading-relaxed text-[var(--cc-ink-dim)]">
        <strong className="font-medium text-[var(--cc-ink)]">
          Everything in Open Source is free forever
        </strong>
        , MIT-licensed, and runs without ChilliCream. Nitro tiers add hosted
        operations, schema governance, and 24x7 support on top.
      </p>

      <div className="-mx-2 overflow-x-auto px-2">
        <table className="w-full min-w-[1080px] border-collapse">
          <thead>
            <tr>
              <th
                scope="col"
                className={`${STICKY_TH} w-[280px] min-w-[280px] bg-[#0c1322]/95`}
              >
                <span className="block text-sm font-medium text-[var(--cc-ink)]">
                  Feature
                </span>
              </th>
              {COMPARISON_COLUMNS.map((col) => (
                <th
                  key={col.key}
                  scope="col"
                  className={`${STICKY_TH} w-[18%] min-w-[160px] ${
                    col.accent ? "bg-[rgba(20,36,60,0.95)]" : "bg-[#0c1322]/95"
                  }`}
                >
                  <span className="mb-1 block text-sm font-medium text-[var(--cc-ink)]">
                    {col.label}
                  </span>
                  <span className="mb-0.5 block text-lg font-medium tracking-tight text-[var(--cc-ink)]">
                    {col.priceLabel}
                  </span>
                  <span className="block font-mono text-[10px] uppercase leading-snug tracking-[0.12em] text-[var(--cc-ink-dim)]">
                    {col.subLabel}
                  </span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {COMPARISON_MATRIX.map((group, gi) => (
              <GroupRows key={group.title} group={group} gi={gi} />
            ))}
          </tbody>
        </table>
      </div>

      <p className="mx-auto mt-7 text-center font-mono text-[11px] uppercase tracking-[0.14em] text-[var(--cc-ink-dim)]">
        Hard limits, budget alerts, no surprise invoices on every Nitro tier.
        Pay-as-you-go is opt-in.
      </p>
    </section>
  );
}

// Render one row-group: a colgroup header row promoted with an accent rule,
// then its feature rows. Kept as a sub-component so the map stays readable.
type Group = (typeof COMPARISON_MATRIX)[number];
const GroupRows = ({ group, gi }: { group: Group; gi: number }) => (
  <>
    <tr>
      <th
        scope="colgroup"
        colSpan={1 + COMPARISON_COLUMNS.length}
        className={
          "px-4 pb-3 pt-9 text-left " +
          (gi === 0
            ? "pt-7"
            : "border-t border-fuchsia-400/30")
        }
      >
        <span className="mb-1 block font-mono text-[13px] font-medium uppercase tracking-[0.16em] text-fuchsia-400">
          {group.title}
        </span>
        {group.summary && (
          <span className="block max-w-[80ch] text-[13px] font-normal normal-case leading-relaxed tracking-normal text-[var(--cc-ink-dim)]">
            {group.summary}
          </span>
        )}
      </th>
    </tr>
    {group.rows.map((row) => (
      <tr
        key={row.label}
        className="transition-colors odd:bg-white/[0.012] hover:bg-white/[0.04]"
      >
        <th
          scope="row"
          className="border-b border-[rgba(245,241,234,0.06)] px-4 py-4 text-left align-top text-sm font-normal text-[var(--cc-ink)]"
        >
          {row.label}
          {row.hint && (
            <span className="mt-1 block text-xs leading-snug text-[var(--cc-ink-dim)]">
              {row.hint}
            </span>
          )}
        </th>
        {COMPARISON_COLUMNS.map((col) => (
          <ComparisonCell
            key={col.key}
            cell={row.cells[col.key as ComparisonColumnKey]}
            isAccent={!!col.accent}
          />
        ))}
      </tr>
    ))}
  </>
);
