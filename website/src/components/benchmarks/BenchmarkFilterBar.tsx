"use client";

import { useEffect, useId, useRef, useState } from "react";
import { BenchmarkColumnHint } from "@/src/components/benchmarks/BenchmarkColumnHint";
import { FilterIcon } from "@/src/icons/FilterIcon";
import type { BenchmarkFilters as Filters } from "@/src/components/benchmarks/benchmarkFilters";

const PREVIEWS = {
  label: "Include previews",
  hint: "Shows preview releases, such as nightly builds, prerelease versions and variants on a prerelease runtime, alongside stable releases.",
};

const BEST_VERSION = {
  label: "Best version only",
  hint: "Shows only the fastest preview and the fastest non-preview variant of each gateway in each table and hides its other variants.",
};

interface BenchmarkFilterBarProps {
  readonly technologies: readonly string[];
  readonly filters: Filters;
  readonly onChange: (filters: Filters) => void;
}

export function BenchmarkFilterBar({
  technologies,
  filters,
  onChange,
}: BenchmarkFilterBarProps) {
  const dialog = useRef<HTMLDialogElement>(null);
  const menu = useRef<HTMLDetailsElement>(null);
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState(filters);

  useEffect(() => {
    const closeOutside = (event: PointerEvent) => {
      if (menu.current && !menu.current.contains(event.target as Node)) {
        menu.current.open = false;
      }
    };
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape" && menu.current?.open) {
        menu.current.open = false;
        menu.current.querySelector("summary")?.focus();
      }
    };
    document.addEventListener("pointerdown", closeOutside);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("pointerdown", closeOutside);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, []);
  useEffect(() => {
    if (!open) {
      return;
    }
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previous;
    };
  }, [open]);

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
    <div
      data-benchmark-controls
      className="pointer-events-auto relative shrink-0 self-start"
    >
      <details ref={menu} className="hidden md:block">
        <summary
          aria-label="Filters"
          title="Filters"
          className="text-cc-ink-dim bg-cc-bg/90 border-cc-card-border hover:text-cc-heading focus-visible:outline-cc-accent flex size-11 cursor-pointer list-none items-center justify-center rounded-full border shadow-sm focus-visible:outline-2 [&::-webkit-details-marker]:hidden"
        >
          <FilterIcon className="size-4 shrink-0" />
        </summary>
        <div className="border-cc-card-border bg-cc-bg absolute top-full right-0 z-10 mt-2 w-72 rounded-lg border p-5 shadow-xl">
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
            <FilterSwitch
              {...BEST_VERSION}
              checked={filters.bestVersionOnly}
              size="size-4"
              onChange={(checked) =>
                onChange({ ...filters, bestVersionOnly: checked })
              }
            />
          </div>
        </div>
      </details>
      <button
        type="button"
        aria-label="Filters"
        title="Filters"
        className="text-cc-ink-dim bg-cc-bg/90 border-cc-card-border hover:text-cc-heading focus-visible:outline-cc-accent flex size-11 cursor-pointer items-center justify-center rounded-full border shadow-sm focus-visible:outline-2 md:hidden"
        aria-haspopup="dialog"
        onClick={() => {
          setDraft(filters);
          dialog.current?.showModal();
          setOpen(true);
        }}
      >
        <FilterIcon className="size-4 shrink-0" />
      </button>
      <dialog
        ref={dialog}
        aria-labelledby="benchmark-filter-title"
        className="border-cc-card-border bg-cc-bg text-cc-heading backdrop:bg-cc-bg/80 fixed inset-x-0 top-auto bottom-0 m-0 max-h-[85dvh] w-full max-w-none overflow-y-auto rounded-t-2xl border p-0"
        onClose={() => setOpen(false)}
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
          <h2
            id="benchmark-filter-title"
            className="text-h6 mb-5 font-semibold"
          >
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
          <fieldset className="border-cc-card-border mt-5 border-t pt-5">
            <legend className="sr-only">Variants</legend>
            <FilterSwitch
              {...BEST_VERSION}
              checked={draft.bestVersionOnly}
              size="size-5"
              onChange={(checked) =>
                setDraft({ ...draft, bestVersionOnly: checked })
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
