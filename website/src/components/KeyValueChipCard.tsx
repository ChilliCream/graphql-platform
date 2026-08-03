import type { ReactNode } from "react";

interface KeyValueChipCardProps {
  readonly label: string;
  /** Code line shown below the label row, e.g. tokenized syntax spans. */
  readonly value: ReactNode;
  /** Icon or mark shown at the end of the label row, e.g. a status check. */
  readonly icon?: ReactNode;
  readonly className?: string;
}

/**
 * A taller, stacked variant of the label/value pairing: a label row (with an
 * optional trailing icon) above a code line, inside a rounded card. Used for
 * catalog tiles where the value is a full code snippet rather than a short
 * inline value.
 */
export function KeyValueChipCard({
  label,
  value,
  icon,
  className = "",
}: KeyValueChipCardProps) {
  return (
    <div
      className={`border-cc-card-border bg-cc-card-bg flex flex-col gap-2 rounded-xl border p-3 ${className}`.trim()}
    >
      <div className="flex items-center justify-between gap-1.5">
        <span className="text-cc-nav-label min-w-0 truncate font-mono text-[0.55rem] tracking-[0.12em] uppercase">
          {label}
        </span>
        {icon}
      </div>
      <code className="block truncate font-mono text-[0.6rem]">{value}</code>
    </div>
  );
}
