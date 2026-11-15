import { CheckIcon } from "@/src/components/CheckIcon";
import { CheckListItem } from "@/src/components/CheckListItem";

type CheckListVariant = "plain" | "pill";
type CheckListSize = "sm" | "base";

interface CheckListProps {
  readonly items: readonly string[];
  readonly variant?: CheckListVariant;
  /** Columns at the `sm` breakpoint (plain variant only). */
  readonly columns?: 1 | 2;
  /** Text size for the `plain` variant. Defaults to `sm`, today's size. */
  readonly size?: CheckListSize;
  /** Extra classes for the list, e.g. a top margin supplied by the caller. */
  readonly className?: string;
}

const SIZE_CLASS: Record<CheckListSize, string> = {
  sm: "text-sm",
  base: "text-base",
};

/**
 * A checklist of short facts, each with an accent check. The `plain` variant is
 * a simple list (optionally two columns); the `pill` variant wraps each item in
 * a bordered chip.
 */
export function CheckList({
  items,
  variant = "plain",
  columns = 1,
  size = "sm",
  className = "",
}: CheckListProps) {
  if (variant === "pill") {
    return (
      <ul className={`grid gap-3 ${className}`.trim()}>
        {items.map((item) => (
          <CheckListItem
            key={item}
            className="border-cc-card-border bg-cc-bg/40 rounded-xl border px-4 py-3"
          >
            {item}
          </CheckListItem>
        ))}
      </ul>
    );
  }

  return (
    <ul
      className={`text-cc-ink grid gap-2 ${SIZE_CLASS[size]} ${columns === 2 ? "sm:grid-cols-2" : ""} ${className}`.trim()}
    >
      {items.map((item) => (
        <li key={item} className="flex items-start gap-2">
          <span className="text-cc-accent mt-1 flex-none">
            <CheckIcon />
          </span>
          <span>{item}</span>
        </li>
      ))}
    </ul>
  );
}
