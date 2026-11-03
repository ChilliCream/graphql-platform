"use client";

import {
  useId,
  useRef,
  useState,
  type CSSProperties,
  type ReactNode,
} from "react";
import { BenchmarkColumnHint } from "@/src/components/benchmarks/BenchmarkColumnHint";
import type { BenchmarkFilters as Filters } from "@/src/components/benchmarks/benchmarkFilters";

const FILTER_STYLES =
  "html:has(dialog[data-benchmark-filter]:modal){overflow:hidden}";

const PREVIEWS = {
  label: "Include previews",
  hint: "Shows preview releases, such as nightly builds and prerelease versions, alongside stable releases.",
};

interface BenchmarkFilterBarProps {
  readonly technologies: readonly string[];
  readonly filters: Filters;
  readonly onChange: (filters: Filters) => void;
  readonly className?: string;
  readonly triggerClassName: string;
  /** The content of the button that opens the filters. */
  readonly children: ReactNode;
}

/** The technology and preview filters: a popover on wide screens and a bottom sheet on narrow screens, opened by a trigger the caller styles. */
export function BenchmarkFilterBar({
  technologies,
  filters,
  onChange,
  className = "",
  triggerClassName,
  children,
}: BenchmarkFilterBarProps) {
  const titleId = useId();
  const popoverId = useId();
  const anchor = `--benchmark-filter${popoverId.replace(/[^\w-]/g, "")}`;
  const dialog = useRef<HTMLDialogElement>(null);
  const [draft, setDraft] = useState(filters);

  const toggle = (current: Filters, technology: string): Filters => ({
    ...current,
    technologies: current.technologies.includes(technology)
      ? current.technologies.filter((item) => item !== technology)
      : technologies.filter(
          (item) => item === technology || current.technologies.includes(item),
        ),
  });
  const choices = (current: Filters, update: (filters: Filters) => void) => (
    <fieldset>
      <legend className="text-cc-heading mb-2 text-sm font-semibold">
        Technology
      </legend>
      <div className="flex flex-wrap gap-2">
        {technologies.map((technology) => {
          const selected = current.technologies.includes(technology);
          return (
            <button
              key={technology}
              type="button"
              aria-pressed={selected}
              className={`focus-visible:outline-cc-accent min-h-9 cursor-pointer rounded border px-3 text-sm transition-colors focus-visible:outline-2 ${selected ? "border-cc-accent/60 bg-cc-accent/10 text-cc-accent" : "border-cc-card-border text-cc-ink-dim hover:border-cc-heading/40 hover:text-cc-heading"}`}
              onClick={() => update(toggle(current, technology))}
            >
              {technology}
            </button>
          );
        })}
      </div>
    </fieldset>
  );

  return (
    <div data-benchmark-controls className={`relative ${className}`}>
      <style href="benchmark-filter" precedence="default">
        {FILTER_STYLES}
      </style>
      <div className="hidden h-full md:block">
        <button
          type="button"
          popoverTarget={popoverId}
          className={`cursor-pointer ${triggerClassName}`}
          style={{ anchorName: anchor } as CSSProperties}
        >
          {children}
        </button>
        <div
          id={popoverId}
          popover="auto"
          className="border-cc-card-border bg-cc-bg text-cc-heading inset-auto m-0 mt-2 w-72 rounded-lg border p-5 shadow-xl [position-area:bottom_span-left] [position-try-fallbacks:flip-block]"
          style={{ positionAnchor: anchor } as CSSProperties}
        >
          {choices(filters, onChange)}
          <div className="border-cc-card-border mt-4 border-t pt-3">
            <FilterSwitch
              {...PREVIEWS}
              checked={filters.showPreviews}
              size="size-4"
              onChange={(checked) =>
                onChange({ ...filters, showPreviews: checked })
              }
            />
          </div>
        </div>
      </div>
      <button
        type="button"
        className={`cursor-pointer md:hidden ${triggerClassName}`}
        aria-haspopup="dialog"
        onClick={() => {
          setDraft(filters);
          dialog.current?.showModal();
        }}
      >
        {children}
      </button>
      <dialog
        ref={dialog}
        data-benchmark-filter
        aria-labelledby={titleId}
        className="border-cc-card-border bg-cc-bg text-cc-heading backdrop:bg-cc-bg/80 fixed inset-x-0 top-auto bottom-0 m-0 max-h-[85dvh] w-full max-w-none overflow-y-auto rounded-t-2xl border p-0"
        onClick={(event) => {
          if (event.target === event.currentTarget) {
            const bounds = event.currentTarget.getBoundingClientRect();
            if (event.clientY < bounds.top) {
              dialog.current?.close();
            }
          }
        }}
      >
        <div className="px-5 pt-5">
          <h2 id={titleId} className="text-h6 mb-5 font-semibold">
            Filter results
          </h2>
          {choices(draft, setDraft)}
          <fieldset className="border-cc-card-border mt-5 border-t pt-5">
            <legend className="sr-only">Preview releases</legend>
            <FilterSwitch
              {...PREVIEWS}
              checked={draft.showPreviews}
              size="size-5"
              onChange={(checked) =>
                setDraft({ ...draft, showPreviews: checked })
              }
            />
          </fieldset>
        </div>
        <div className="border-cc-card-border bg-cc-bg sticky bottom-0 mt-5 border-t px-5 py-4 pb-[max(1rem,env(safe-area-inset-bottom))]">
          <button
            type="button"
            className="bg-cc-accent text-cc-bg focus-visible:outline-cc-accent min-h-11 w-full cursor-pointer rounded-md px-4 text-sm font-semibold focus-visible:outline-2"
            onClick={() => {
              onChange(draft);
              dialog.current?.close();
            }}
          >
            Show results
          </button>
        </div>
      </dialog>
    </div>
  );
}

interface FilterSwitchProps {
  readonly label: string;
  readonly hint: string;
  readonly checked: boolean;
  readonly size: "size-4" | "size-5";
  readonly onChange: (checked: boolean) => void;
}

function FilterSwitch({
  label,
  hint,
  checked,
  size,
  onChange,
}: FilterSwitchProps) {
  const id = useId();
  return (
    <div className="text-cc-heading relative flex min-h-11 items-center justify-between gap-3 text-sm">
      <label
        htmlFor={id}
        aria-hidden="true"
        className="absolute inset-0 cursor-pointer"
      />
      <span className="pointer-events-none relative">
        {label}
        <span className="pointer-events-auto">
          <BenchmarkColumnHint label={label}>{hint}</BenchmarkColumnHint>
        </span>
      </span>
      <input
        id={id}
        type="checkbox"
        role="switch"
        aria-label={label}
        className={`accent-cc-accent relative cursor-pointer ${size}`}
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
      />
    </div>
  );
}
