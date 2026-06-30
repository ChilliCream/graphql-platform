import { CheckIcon } from "@/src/components/CheckIcon";
import { SectionHeading } from "@/src/components/SectionHeading";

type Cell = boolean | string;

interface ComparisonGroup {
  readonly title: string;
  readonly rows: readonly {
    readonly label: string;
    /** One cell per column, in the same order as `columns`. */
    readonly cells: readonly Cell[];
  }[];
}

interface FeatureComparisonProps {
  readonly id: string;
  /** Outer section spacing utilities, e.g. "mt-24 sm:mt-28". */
  readonly className?: string;
  readonly eyebrow: string;
  readonly heading: string;
  /** Column headers (one per plan/tier). */
  readonly columns: readonly string[];
  readonly groups: readonly ComparisonGroup[];
}

/**
 * The full feature comparison: every plan as a column, capabilities grouped into
 * labelled sections. A boolean cell renders a check or a dash, a string cell
 * renders the value. The table scrolls horizontally on narrow screens. Shared by
 * the pricing tiers and the support plans.
 */
export function FeatureComparison({
  id,
  className,
  eyebrow,
  heading,
  columns,
  groups,
}: FeatureComparisonProps) {
  return (
    <section aria-labelledby={`${id}-heading`} className={className} id={id}>
      <SectionHeading
        align="center"
        eyebrow={eyebrow}
        title={heading}
        titleId={`${id}-heading`}
      />

      <div className="border-cc-card-border bg-cc-card-bg/40 mt-10 overflow-hidden rounded-3xl border">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[820px] border-collapse text-left text-sm">
            <thead>
              <tr className="border-cc-card-border border-b">
                <th
                  scope="col"
                  className="text-cc-nav-label px-5 py-4 font-mono text-[0.65rem] tracking-[0.15em] uppercase"
                >
                  Capability
                </th>
                {columns.map((name) => (
                  <th
                    key={name}
                    scope="col"
                    className="text-cc-heading font-heading px-5 py-4 text-sm font-semibold"
                  >
                    {name}
                  </th>
                ))}
              </tr>
            </thead>
            {groups.map((group, groupIndex) => (
              <tbody key={group.title}>
                <tr
                  className={`bg-cc-card-bg/60 ${
                    groupIndex === 0 ? "" : "border-cc-card-border border-t"
                  }`}
                >
                  <th
                    scope="colgroup"
                    colSpan={columns.length + 1}
                    className="text-cc-nav-label px-5 py-3 text-left font-mono text-[0.65rem] tracking-[0.15em] uppercase"
                  >
                    {group.title}
                  </th>
                </tr>
                {group.rows.map((row) => (
                  <tr
                    key={row.label}
                    className="border-cc-ink-faint border-b last:border-0"
                  >
                    <th
                      scope="row"
                      className="text-cc-ink px-5 py-3 align-top text-sm font-medium"
                    >
                      {row.label}
                    </th>
                    {row.cells.map((value, index) => (
                      <CompareCell key={columns[index]} value={value} />
                    ))}
                  </tr>
                ))}
              </tbody>
            ))}
          </table>
        </div>
      </div>
    </section>
  );
}

function CompareCell({ value }: { readonly value: Cell }) {
  if (value === true) {
    return (
      <td className="px-5 py-3 align-top">
        <span className="text-cc-accent inline-flex">
          <CheckIcon />
        </span>
        <span className="sr-only">Included</span>
      </td>
    );
  }
  if (value === false) {
    return (
      <td className="text-cc-ink-faint px-5 py-3 align-top">
        <span aria-hidden="true">-</span>
        <span className="sr-only">Not included</span>
      </td>
    );
  }
  return (
    <td className="text-cc-ink px-5 py-3 align-top font-mono text-xs">
      {value}
    </td>
  );
}
