import { CheckIcon } from "@/src/components/CheckIcon";
import type { Cell } from "@/src/components/pricing/pricingData";
import { COMPARISON, TIERS } from "@/src/components/pricing/pricingData";

/**
 * The full feature comparison: every tier as a column, capabilities grouped
 * into labelled sections. A boolean cell renders a check or a dash, a string
 * cell renders the value. The table scrolls horizontally on narrow screens.
 */
export function CompareTable() {
  return (
    <section
      aria-labelledby="compare-heading"
      className="mt-24 scroll-mt-24 sm:mt-28"
      id="compare"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Compare plans
        </p>
        <h2
          id="compare-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Feature comparison
        </h2>
      </div>

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
                {TIERS.map((tier) => (
                  <th
                    key={tier.id}
                    scope="col"
                    className="text-cc-heading font-heading px-5 py-4 text-sm font-semibold"
                  >
                    {tier.name}
                  </th>
                ))}
              </tr>
            </thead>
            {COMPARISON.map((group, groupIndex) => (
              <tbody key={group.title}>
                <tr
                  className={`bg-cc-card-bg/60 ${
                    groupIndex === 0 ? "" : "border-cc-card-border border-t"
                  }`}
                >
                  <th
                    scope="colgroup"
                    colSpan={TIERS.length + 1}
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
                    {TIERS.map((tier) => (
                      <CompareCell key={tier.id} value={row[tier.id]} />
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
