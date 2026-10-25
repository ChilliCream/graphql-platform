"use client";

import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { BenchmarkColumnHint } from "@/src/components/benchmarks/BenchmarkColumnHint";
import type { BenchmarkFilters as Filters } from "@/src/components/benchmarks/benchmarkFilters";

/** The height of the sticky site header in pixels, which the popover must not open behind. */
const HEADER_HEIGHT = 72;

/** The space between the trigger and the popover in pixels. */
const POPOVER_GAP = 8;

const PREVIEWS = {
  label: "Include previews",
  hint: "Shows preview releases, such as nightly builds, prerelease versions and variants on a prerelease runtime, alongside stable releases.",
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
  const dialog = useRef<HTMLDialogElement>(null);
  const menu = useRef<HTMLDetailsElement>(null);
  const popover = useRef<HTMLDivElement>(null);
  const [above, setAbove] = useState(false);
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
    <div data-benchmark-controls className={`relative ${className}`}>
      <details
        ref={menu}
        className="hidden h-full md:block"
        onToggle={(event) => {
          const details = event.currentTarget;
          if (!details.open || !popover.current) {
            return;
          }
          const trigger = details.getBoundingClientRect();
          const height = popover.current.offsetHeight + POPOVER_GAP;
          setAbove(
            trigger.bottom + height > window.innerHeight &&
              trigger.top - height >= HEADER_HEIGHT,
          );
        }}
      >
        <summary
          className={`cursor-pointer list-none [&::-webkit-details-marker]:hidden ${triggerClassName}`}
        >
          {children}
        </summary>
        <div
          ref={popover}
          className={`border-cc-card-border bg-cc-bg absolute right-0 z-10 w-72 rounded-lg border p-5 shadow-xl ${above ? "bottom-full mb-2" : "top-full mt-2"}`}
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
      </details>
      <button
        type="button"
        className={`cursor-pointer md:hidden ${triggerClassName}`}
        aria-haspopup="dialog"
        onClick={() => {
          setDraft(filters);
          dialog.current?.showModal();
          setOpen(true);
        }}
      >
        {children}
      </button>
      <dialog
        ref={dialog}
        aria-labelledby={titleId}
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
