"use client";

import { useEffect, useRef, useState } from "react";

interface CopyCommandProps {
  /** The full shell command, without the leading "$". */
  readonly command: string;
  /** Extra container classes; call sites supply the background tint. */
  readonly className?: string;
  readonly size?: "sm" | "md";
}

/** A plain two-rectangle copy affordance; decorative, inherits currentColor. */
function CopyGlyph({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <rect x="9" y="9" width="11" height="11" rx="2" />
      <path d="M5 15V5a2 2 0 0 1 2-2h8" />
    </svg>
  );
}

/** Check mark shown briefly after a successful copy. */
function CopiedGlyph({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M5 12.5 10 17.5 19 7" />
    </svg>
  );
}

/**
 * A one-line shell command in the mono command-box idiom, with a working
 * copy-to-clipboard button. The first token (the executable) renders in the
 * accent color, the rest in ink, matching the static command pills it
 * replaces.
 */
export function CopyCommand({
  command,
  className,
  size = "md",
}: CopyCommandProps) {
  const [copied, setCopied] = useState(false);
  const resetTimer = useRef<number | undefined>(undefined);

  useEffect(() => () => window.clearTimeout(resetTimer.current), []);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(command);
      setCopied(true);
      window.clearTimeout(resetTimer.current);
      resetTimer.current = window.setTimeout(() => setCopied(false), 1600);
    } catch {
      // Clipboard unavailable (permissions or insecure context); do nothing.
    }
  };

  const [executable, ...args] = command.split(" ");

  return (
    <div
      className={[
        "border-cc-card-border relative border font-mono",
        size === "md" ? "rounded-xl p-4" : "rounded-lg px-3 py-2.5",
        className,
      ]
        .filter(Boolean)
        .join(" ")}
    >
      <button
        type="button"
        onClick={handleCopy}
        aria-label={copied ? "Copied" : "Copy command"}
        className={[
          "absolute top-1/2 -translate-y-1/2 transition-colors",
          size === "md" ? "right-3" : "right-2.5",
          copied
            ? "text-cc-accent"
            : "text-cc-ink-faint hover:text-cc-ink cursor-pointer",
        ].join(" ")}
      >
        {copied ? (
          <CopiedGlyph className="size-4" />
        ) : (
          <CopyGlyph className="size-4" />
        )}
      </button>
      <code
        className={[
          "block [scrollbar-width:none]! overflow-x-auto pr-7 leading-relaxed whitespace-nowrap [&::-webkit-scrollbar]:hidden!",
          size === "md" ? "text-sm" : "text-[0.65rem]",
        ].join(" ")}
      >
        <span className="text-cc-ink-faint select-none">$ </span>
        <span className="text-cc-accent">{executable}</span>
        <span className="text-cc-ink"> {args.join(" ")}</span>
      </code>
    </div>
  );
}
