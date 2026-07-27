import type { ReactNode } from "react";

export type AdmonitionKind =
  | "note"
  | "tip"
  | "warning"
  | "caution"
  | "experimental";

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
    containerClass: "bg-cc-note/10 ring-1 ring-cc-note/30 text-cc-ink",
    labelClass: "text-cc-note",
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
    containerClass: "bg-cc-success/10 ring-1 ring-cc-success/30 text-cc-ink",
    labelClass: "text-cc-success",
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
  warning: {
    label: "Warning",
    containerClass: "bg-cc-warning/10 ring-1 ring-cc-warning/30 text-cc-ink",
    labelClass: "text-cc-warning",
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
    containerClass: "bg-cc-danger/10 ring-1 ring-cc-danger/30 text-cc-ink",
    labelClass: "text-cc-danger",
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
  experimental: {
    label: "Experimental",
    containerClass: "bg-cc-info/10 ring-1 ring-cc-info/30 text-cc-ink",
    labelClass: "text-cc-info",
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
        <path d="M14 2v6a2 2 0 0 0 .245.96l5.51 10.08A2 2 0 0 1 18 22H6a2 2 0 0 1-1.755-2.96l5.51-10.08A2 2 0 0 0 10 8V2" />
        <path d="M6.453 15h11.094" />
        <path d="M8.5 2h7" />
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
