"use client";

import React, { FC } from "react";

import type { FilterKind, FilterOption } from "@/data/templates/filters";

interface FilterAxisProps {
  readonly title: string;
  readonly kind: FilterKind;
  readonly options: readonly FilterOption[];
  readonly active: ReadonlySet<string>;
  readonly onToggle: (key: string) => void;
  readonly onClear: () => void;
}

// Single filter group on the rail: title in eyebrow style, an active-count
// chip when at least one option is on, a "Clear" link top-right (only
// rendered when there's something to clear), and the chip multi-select /
// single-select / toggle below.
//
// Single-select acts like a radio: clicking an active option clears it,
// clicking a different option replaces the active one. We model it as
// "toggle the same key off, toggle a new key on" rather than a separate
// shape so the parent state is uniformly Set<string>.
export const FilterAxis: FC<FilterAxisProps> = ({
  title,
  kind,
  options,
  active,
  onToggle,
  onClear,
}) => {
  const count = active.size;
  return (
    <div className="cc-tp-axis">
      <div className="cc-tp-axis-head">
        <span className="cc-tp-axis-title">
          {title}
          {count > 0 && <span className="cc-tp-axis-count">{count}</span>}
        </span>
        {count > 0 && (
          <button type="button" className="cc-tp-axis-clear" onClick={onClear}>
            Clear
          </button>
        )}
      </div>
      {kind === "toggle" ? (
        <button
          type="button"
          className={`cc-tp-toggle${
            active.has(options[0]?.key ?? "") ? " is-active" : ""
          }`}
          onClick={() => onToggle(options[0]?.key ?? "")}
          aria-pressed={active.has(options[0]?.key ?? "")}
        >
          <span className="cc-tp-toggle-dot" aria-hidden />
          {active.has(options[0]?.key ?? "") ? "Yes" : "Any"}
        </button>
      ) : (
        <div className="cc-tp-axis-options" role="group" aria-label={title}>
          {options.map((opt) => (
            <button
              key={opt.key}
              type="button"
              className={`cc-tp-chip${active.has(opt.key) ? " is-active" : ""}`}
              onClick={() => onToggle(opt.key)}
              aria-pressed={active.has(opt.key)}
            >
              {opt.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
};
