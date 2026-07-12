import type { ComponentPropsWithoutRef, ElementType } from "react";
import { CheckIcon } from "@/src/components/CheckIcon";
import { SectionHeading } from "@/src/components/SectionHeading";
import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";

/**
 * Eyebrow only types its passthrough props against `<p>` attributes, so a
 * `<th scope>` usage needs its own attribute type when rendered `as="th"`.
 */
const EyebrowAsTh = Eyebrow as unknown as (
  props: ComponentPropsWithoutRef<typeof Eyebrow> & {
    readonly as: ElementType;
  } & ComponentPropsWithoutRef<"th">,
) => ReturnType<typeof Eyebrow>;

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

      <Card variant="panel" className="mt-10">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[820px] border-collapse text-left text-sm">
            <thead>
              <tr className="border-cc-card-border border-b">
                <EyebrowAsTh
                  as="th"
                  scope="col"
                  size="2xs"
                  color="ink-dim"
                  className="px-5 py-4 text-left"
                >
                  Capability
                </EyebrowAsTh>
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
                  <EyebrowAsTh
                    as="th"
                    scope="colgroup"
                    colSpan={columns.length + 1}
                    size="2xs"
                    color="ink-dim"
                    className="px-5 py-3 text-left"
                  >
                    {group.title}
                  </EyebrowAsTh>
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
      </Card>
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
