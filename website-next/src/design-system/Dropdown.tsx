import type { ReactNode } from "react";
import { ChevronDownIcon } from "@/src/icons/ChevronDown";

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
          "rounded-md border border-slate-200 bg-white px-3 py-2",
          "text-left text-sm text-slate-800 transition-colors",
          "hover:border-slate-300 hover:bg-slate-50",
          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600",
          "[&::-webkit-details-marker]:hidden",
        ].join(" ")}
      >
        <span className="min-w-0 flex-1">{trigger}</span>
        <ChevronDownIcon
          aria-hidden="true"
          className="h-3 w-3 flex-none fill-current text-slate-500 transition-transform duration-200 group-open:rotate-180"
        />
      </summary>
      <div
        className={[
          "absolute inset-x-0 top-full z-20 mt-1",
          "overflow-hidden rounded-md border border-slate-200 bg-white shadow-lg",
          panelClassName ?? "",
        ].join(" ")}
      >
        {children}
      </div>
    </details>
  );
}
