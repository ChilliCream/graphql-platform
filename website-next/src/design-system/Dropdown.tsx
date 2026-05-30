import type { ReactNode } from "react";
import { ChevronDownIcon } from "@/src/icons/ChevronDown";
import { DropdownAutoClose } from "./DropdownAutoClose";

type DropdownProps = {
  trigger: ReactNode;
  children: ReactNode;
  defaultOpen?: boolean;
  className?: string;
  panelClassName?: string;
};

export function Dropdown({
  trigger,
  children,
  defaultOpen,
  className,
  panelClassName,
}: DropdownProps) {
  return (
    <details open={defaultOpen} className={`group relative ${className ?? ""}`}>
      <summary
        className={[
          "flex w-full cursor-pointer list-none select-none items-center gap-2",
          "rounded-md border border-cc-card-border bg-cc-bg px-3 py-2",
          "text-left text-sm text-cc-ink transition-colors",
          "hover:border-cc-card-border-hover hover:bg-cc-hover",
          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-cc-accent",
          "[&::-webkit-details-marker]:hidden",
        ].join(" ")}
      >
        <span className="min-w-0 flex-1">{trigger}</span>
        <ChevronDownIcon
          aria-hidden="true"
          className="h-3 w-3 flex-none fill-current text-cc-ink-dim transition-transform duration-200 group-open:rotate-180"
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
  );
}
