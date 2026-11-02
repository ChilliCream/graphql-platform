import type { ButtonHTMLAttributes, ReactNode } from "react";

export type IconButtonProps = {
  /** The icon to render, typically an SVG sized to ~1.25rem (`h-5 w-5`). */
  children: ReactNode;
  /** Accessible label, required since the button has no visible text. */
  "aria-label": string;
  className?: string;
  /** Button type. Defaults to `button`. */
  type?: "button" | "submit";
  disabled?: boolean;
  onClick?: ButtonHTMLAttributes<HTMLButtonElement>["onClick"];
};

const BASE_CLASSES =
  "inline-flex h-9 w-9 cursor-pointer items-center justify-center rounded-full text-cc-ink-dim transition-colors hover:bg-cc-ink-faint disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:bg-transparent";

export function IconButton({
  children,
  className,
  type = "button",
  disabled,
  onClick,
  ...rest
}: IconButtonProps) {
  const cls = [BASE_CLASSES, className ?? ""].filter(Boolean).join(" ");

  return (
    <button
      type={type}
      disabled={disabled}
      onClick={onClick}
      className={cls}
      aria-label={rest["aria-label"]}
    >
      {children}
    </button>
  );
}
