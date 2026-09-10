import type { ReactNode } from "react";

type ButtonRowAlign = "center" | "start" | "stacked";

interface ButtonRowProps {
  readonly children: ReactNode;
  readonly align?: ButtonRowAlign;
  /** Extra classes, e.g. a top margin supplied by the caller. */
  readonly className?: string;
}

const ALIGN: Record<ButtonRowAlign, string> = {
  center: "flex flex-wrap items-center justify-center gap-3",
  start: "flex flex-wrap gap-3",
  stacked: "flex flex-col gap-3",
};

/**
 * A row of call-to-action buttons. `center` and `start` wrap horizontally;
 * `stacked` lays the buttons out vertically (pair with full-width buttons).
 */
export function ButtonRow({
  children,
  align = "center",
  className = "",
}: ButtonRowProps) {
  return (
    <div className={`${ALIGN[align]} ${className}`.trim()}>{children}</div>
  );
}
