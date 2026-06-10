import Link from "next/link";
import { useId, type ComponentProps, type ReactNode } from "react";
import { ChevronDownIcon } from "@/src/icons/ChevronDown";
import { DropdownAutoClose } from "./DropdownAutoClose";

type DropdownProps = {
  trigger: ReactNode;
  children: ReactNode;
  /** Label rendered above the trigger, matching the form inputs. */
  label?: ReactNode;
  defaultOpen?: boolean;
  className?: string;
  panelClassName?: string;
};

export function Dropdown({
  trigger,
  children,
  label,
  defaultOpen,
  className,
  panelClassName,
}: DropdownProps) {
  const labelId = useId();

  return (
    <div className={`flex flex-col gap-1 ${className ?? ""}`.trim()}>
      {label && (
        <span id={labelId} className="text-sm font-medium text-cc-ink">
          {label}
        </span>
      )}
      <details open={defaultOpen} className="group relative">
        <summary
          aria-labelledby={label ? labelId : undefined}
          className={[
            "flex w-full cursor-pointer list-none select-none items-center gap-2",
            "rounded-md border border-cc-card-border bg-white/5 px-3 py-2.5",
            "text-left text-sm text-cc-ink transition-colors",
            // Hover affordance only while closed, so it can't override the
            // accent border below (group-open has lower specificity in v4).
            "[details:not([open])>&:hover]:border-cc-card-border-hover",
            // Accent highlight while open, mirroring an input's focus state.
            "group-open:border-cc-accent group-open:ring-2 group-open:ring-cc-accent/30",
            "focus-visible:outline-hidden focus-visible:border-cc-accent focus-visible:ring-2 focus-visible:ring-cc-accent/30",
            "[&::-webkit-details-marker]:hidden",
          ].join(" ")}
        >
          <span className="min-w-0 flex-1">{trigger}</span>
          <ChevronDownIcon
            aria-hidden="true"
            className="h-3 w-3 flex-none fill-current text-cc-ink-dim transition-transform duration-200 group-open:rotate-180 group-open:text-cc-accent"
          />
        </summary>
        <div
          className={[
            "absolute inset-x-0 top-full z-20 mt-1",
            "overflow-hidden rounded-md border border-cc-card-border bg-cc-bg shadow-lg",
            panelClassName ?? "",
          ].join(" ")}
        >
          {children}
        </div>
        <DropdownAutoClose />
      </details>
    </div>
  );
}

type DropdownItemCommon = {
  /** Renders the row in the accent color to mark the current selection. */
  active?: boolean;
  /** Optional secondary line rendered below the label. */
  description?: ReactNode;
  children: ReactNode;
};

type DropdownItemLinkProps = DropdownItemCommon &
  Omit<ComponentProps<typeof Link>, "children" | "className">;

type DropdownItemButtonProps = DropdownItemCommon &
  Omit<ComponentProps<"button">, "children" | "className"> & { href?: never };

export type DropdownItemProps = DropdownItemLinkProps | DropdownItemButtonProps;

/**
 * A single row inside a `Dropdown` panel. Renders a `Link` when `href` is
 * provided, otherwise a `<button>`. Shares the active/hover styling so every
 * dropdown looks the same.
 */
export function DropdownItem({
  active,
  description,
  children,
  ...rest
}: DropdownItemProps) {
  const className = [
    "block w-full cursor-pointer rounded px-3 py-2 text-left no-underline transition-colors",
    active ? "bg-cc-accent/10 text-cc-accent" : "text-cc-ink hover:bg-cc-hover",
  ].join(" ");

  const content = (
    <>
      <div className="text-sm font-medium">{children}</div>
      {description && (
        <div
          className={`text-xs ${active ? "text-cc-accent/80" : "text-cc-ink-dim"}`}
        >
          {description}
        </div>
      )}
    </>
  );

  return (
    <li>
      {"href" in rest && rest.href !== undefined ? (
        <Link
          {...rest}
          aria-current={active ? "page" : undefined}
          className={className}
        >
          {content}
        </Link>
      ) : (
        <button
          type="button"
          {...rest}
          aria-current={active ? "true" : undefined}
          className={className}
        >
          {content}
        </button>
      )}
    </li>
  );
}
