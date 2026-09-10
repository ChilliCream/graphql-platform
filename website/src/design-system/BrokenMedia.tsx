import type { CSSProperties } from "react";

type BrokenMediaProps = {
  message: string;
  className?: string;
  style?: CSSProperties;
};

export function BrokenMedia({ message, className, style }: BrokenMediaProps) {
  return (
    <span
      className={`bg-cc-card-bg text-cc-ink-dim ring-cc-card-border @container flex flex-col items-center justify-center gap-3 overflow-hidden ring-1 ${
        className ?? "my-6 aspect-video w-full rounded-md"
      }`}
      style={style}
    >
      <svg
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-10 w-10 shrink-0 @max-[8rem]:h-5 @max-[8rem]:w-5"
        aria-hidden="true"
      >
        <path d="m18.84 12.25 1.72-1.71a5 5 0 0 0-7.07-7.07l-1.72 1.71" />
        <path d="m5.17 11.75-1.71 1.71a5 5 0 0 0 7.07 7.07l1.71-1.71" />
        <line x1="8" y1="2" x2="8" y2="5" />
        <line x1="2" y1="8" x2="5" y2="8" />
        <line x1="16" y1="19" x2="16" y2="22" />
        <line x1="19" y1="16" x2="22" y2="16" />
      </svg>
      <span className="px-4 text-center text-sm font-medium @max-[14rem]:sr-only">
        {message}
      </span>
    </span>
  );
}
