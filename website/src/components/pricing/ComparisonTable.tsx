"use client";

import React from "react";

import {
  Cell,
  COMPARISON_COLUMNS,
  COMPARISON_MATRIX,
  ComparisonColumnKey,
} from "@/data/pricing/comparisonMatrix";

const CheckIcon: React.FC = () => (
  <svg viewBox="0 0 16 16" width="16" height="16" aria-hidden>
    <path
      d="M3 8.5 L6.5 12 L13 4.5"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </svg>
);

const ComparisonCell: React.FC<{
  cell: Cell;
  isAccent: boolean;
}> = ({ cell, isAccent }) => {
  const tdClass = "cc-cell" + (isAccent ? " is-accent" : "");
  switch (cell.kind) {
    case "check":
      return (
        <td className={tdClass + " is-accent-target"}>
          <span className="cc-cell-check" aria-label="Included">
            <CheckIcon />
          </span>
        </td>
      );
    case "value":
      return (
        <td className={tdClass}>
          <span className="cc-cell-value">{cell.label}</span>
        </td>
      );
    case "meter":
      return (
        <td className={tdClass}>
          <span className="cc-cell-meter">
            <span className="cc-cell-meter-included">{cell.included}</span>
            {cell.overage && cell.unit && (
              <span className="cc-cell-meter-overage">
                then {cell.overage} / {cell.unit}
              </span>
            )}
          </span>
        </td>
      );
    case "custom":
      return (
        <td className={tdClass}>
          <span className="cc-cell-custom" aria-label="Custom — contact sales">
            Custom
          </span>
        </td>
      );
    case "none":
    default:
      return (
        <td className={tdClass}>
          <span className="cc-cell-none" aria-label="Not included">
            —
          </span>
        </td>
      );
  }
};

// The comparison matrix lifted out of an outlined card: it lives directly on
// the band. Sticky thead engages once the user scrolls past the section
// heading. Row-group titles stay inside the table for accessibility (real
// `<th scope=colgroup>`) but are also promoted with a 1px accent rule above
// each group so the eye can break the wall into chapters.
export const ComparisonTable: React.FC = () => {
  return (
    <div className="cc-compare">
      <div className="cc-section-label">
        <span className="num">04</span> Compare plans
      </div>
      <div className="cc-compare-inner">
        <div className="cc-compare-heading">
          <div className="eyebrow">Compare plans</div>
          <h2 className="display">Every meter, every cell.</h2>
        </div>

        <p className="cc-compare-ribbon">
          <strong>Everything in Open Source is free forever</strong>,
          MIT-licensed, and runs without ChilliCream. Nitro tiers add hosted
          operations, schema governance, and 24x7 support on top.
        </p>

        <div className="cc-compare-scroll">
          <table className="cc-compare-table">
            <thead>
              <tr>
                <th scope="col" className="is-feature">
                  <span className="cc-compare-col-label">Feature</span>
                </th>
                {COMPARISON_COLUMNS.map((col) => (
                  <th
                    key={col.key}
                    scope="col"
                    className={"is-tier" + (col.accent ? " is-accent" : "")}
                  >
                    <span className="cc-compare-col-label">{col.label}</span>
                    <span className="cc-compare-col-price">
                      {col.priceLabel}
                    </span>
                    <span className="cc-compare-col-sub">{col.subLabel}</span>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {COMPARISON_MATRIX.map((group, gi) => (
                <React.Fragment key={group.title}>
                  <tr
                    className={"cc-group-head" + (gi === 0 ? " is-first" : "")}
                  >
                    <th
                      scope="colgroup"
                      colSpan={1 + COMPARISON_COLUMNS.length}
                    >
                      <span className="cc-compare-group-title">
                        {group.title}
                      </span>
                      {group.summary && (
                        <span className="cc-compare-group-summary">
                          {group.summary}
                        </span>
                      )}
                    </th>
                  </tr>
                  {group.rows.map((row) => (
                    <tr key={row.label} className="cc-row">
                      <th scope="row" className="cc-row-label">
                        {row.label}
                        {row.hint && (
                          <span className="cc-row-label-hint">{row.hint}</span>
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
                </React.Fragment>
              ))}
            </tbody>
          </table>
        </div>

        <p className="cc-compare-foot">
          Hard limits, budget alerts, no surprise invoices on every Nitro tier.
          Pay-as-you-go is opt-in.
        </p>
      </div>
    </div>
  );
};
