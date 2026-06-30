import { CheckIcon } from "@/src/components/CheckIcon";

type CheckListVariant = "plain" | "pill";

interface CheckListProps {
  readonly items: readonly string[];
  readonly variant?: CheckListVariant;
  /** Columns at the `sm` breakpoint (plain variant only). */
  readonly columns?: 1 | 2;
  /** Extra classes for the list, e.g. a top margin supplied by the caller. */
  readonly className?: string;
}

/**
 * A checklist of short facts, each with an accent check. The `plain` variant is
 * a simple list (optionally two columns); the `pill` variant wraps each item in
 * a bordered chip.
 */
export function CheckList({
  items,
  variant = "plain",
  columns = 1,
  className = "",
}: CheckListProps) {
  const listClass =
    variant === "pill"
      ? "grid gap-3"
      : `text-cc-ink grid gap-2 text-sm ${columns === 2 ? "sm:grid-cols-2" : ""}`;
  const itemClass =
    variant === "pill"
      ? "border-cc-card-border bg-cc-bg/40 flex items-start gap-3 rounded-xl border px-4 py-3"
      : "flex items-start gap-2";

  return (
    <ul className={`${listClass} ${className}`.trim()}>
      {items.map((item) => (
        <li key={item} className={itemClass}>
          <span className="text-cc-accent mt-1 flex-none">
            <CheckIcon />
          </span>
          <span
            className={variant === "pill" ? "text-cc-ink text-sm" : undefined}
          >
            {item}
          </span>
        </li>
      ))}
    </ul>
  );
}
