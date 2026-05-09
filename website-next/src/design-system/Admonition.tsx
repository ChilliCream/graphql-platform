import type { ReactNode } from "react";

export type AdmonitionKind =
  | "note"
  | "tip"
  | "important"
  | "warning"
  | "caution";

const config: Record<
  AdmonitionKind,
  {
    label: string;
    containerClass: string;
    labelClass: string;
    icon: ReactNode;
  }
> = {
  note: {
    label: "Note",
    containerClass: "bg-sky-50 ring-1 ring-sky-200 text-sky-950",
    labelClass: "text-sky-700",
    icon: (
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-5 w-5"
        aria-hidden="true"
      >
        <circle cx="12" cy="12" r="10" />
        <path d="M12 16v-4" />
        <path d="M12 8h.01" />
      </svg>
    ),
  },
  tip: {
    label: "Tip",
    containerClass: "bg-emerald-50 ring-1 ring-emerald-200 text-emerald-950",
    labelClass: "text-emerald-700",
    icon: (
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-5 w-5"
        aria-hidden="true"
      >
        <path d="M9 18h6" />
        <path d="M10 22h4" />
        <path d="M12 2a7 7 0 0 0-4 12.7c.6.5 1 1.2 1 2V18h6v-1.3c0-.8.4-1.5 1-2A7 7 0 0 0 12 2Z" />
      </svg>
    ),
  },
  important: {
    label: "Important",
    containerClass: "bg-purple-50 ring-1 ring-purple-200 text-purple-950",
    labelClass: "text-purple-700",
    icon: (
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-5 w-5"
        aria-hidden="true"
      >
        <path d="M3 11v3a1 1 0 0 0 1 1h3l3.6 4.5a1 1 0 0 0 1.8-.6V5.1a1 1 0 0 0-1.8-.6L7 9H4a1 1 0 0 0-1 1v1Z" />
        <path d="M16 8a5 5 0 0 1 0 8" />
      </svg>
    ),
  },
  warning: {
    label: "Warning",
    containerClass: "bg-amber-50 ring-1 ring-amber-200 text-amber-950",
    labelClass: "text-amber-700",
    icon: (
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-5 w-5"
        aria-hidden="true"
      >
        <path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z" />
        <path d="M12 9v4" />
        <path d="M12 17h.01" />
      </svg>
    ),
  },
  caution: {
    label: "Caution",
    containerClass: "bg-red-50 ring-1 ring-red-200 text-red-950",
    labelClass: "text-red-700",
    icon: (
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-5 w-5"
        aria-hidden="true"
      >
        <polygon points="7.86 2 16.14 2 22 7.86 22 16.14 16.14 22 7.86 22 2 16.14 2 7.86 7.86 2" />
        <line x1="15" y1="9" x2="9" y2="15" />
        <line x1="9" y1="9" x2="15" y2="15" />
      </svg>
    ),
  },
};

type AdmonitionProps = {
  kind: AdmonitionKind;
  children: ReactNode;
};

export function Admonition({ kind, children }: AdmonitionProps) {
  const { label, containerClass, labelClass, icon } = config[kind];
  return (
    <div className={`my-6 rounded-lg p-5 ${containerClass}`}>
      <div className={`mb-3 flex items-center gap-2 font-bold ${labelClass}`}>
        <span className="shrink-0">{icon}</span>
        <span>{label}</span>
      </div>
      <div className="[&>*:first-child]:mt-0 [&>*:last-child]:mb-0">
        {children}
      </div>
    </div>
  );
}
