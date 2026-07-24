import type { ReactNode } from "react";

import { Icon } from "../icons/Icon";

interface CheckListItemProps {
  readonly children: ReactNode;
  /** Tints the check icon. Defaults to the brand accent (`text-cc-accent`). */
  readonly iconClassName?: string;
  /** Extra classes for the row, e.g. layout overrides supplied by the caller. */
  readonly className?: string;
}

/**
 * A single checklist row: an accent check mark beside arbitrary content. The
 * row primitive `CheckList`'s plain variant renders, and the shape `Offering`,
 * `PerkCard`, `ContactBand`, and `TierGrid` reimplement inline for their perk
 * and fact lists.
 */
export function CheckListItem({
  children,
  iconClassName = "text-cc-accent",
  className = "",
}: CheckListItemProps) {
  return (
    <li className={`flex items-start gap-3 ${className}`.trim()}>
      <span className="inline-flex">
        <Icon icon="check" size="xs" className={iconClassName} />
      </span>
      <span className="text-cc-ink text-sm">{children}</span>
    </li>
  );
}
