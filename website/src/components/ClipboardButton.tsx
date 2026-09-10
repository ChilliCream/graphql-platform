"use client";

import { useEffect, useRef, useState } from "react";
import { sendAnalyticsEvent } from "@/src/helpers/analytics";
import { CheckIcon } from "@/src/icons/Check";
import { CopyIcon } from "@/src/icons/Copy";
import { WarningIcon } from "@/src/icons/Warning";

type CopyStatus = "idle" | "copied" | "error";

interface ClipboardButtonBaseProps {
  readonly label: string;
  readonly copiedLabel: string;
  readonly errorLabel: string;
  readonly analyticsEventName: string;
  readonly analyticsParameters?: Readonly<
    Record<string, string | number | boolean>
  >;
  readonly className?: string;
  readonly labelClassName?: string;
  readonly showLabel?: boolean;
}

type ClipboardButtonProps = ClipboardButtonBaseProps &
  (
    | {
        readonly value: string;
        readonly sourceUrl?: never;
        readonly fallbackValue?: never;
      }
    | {
        readonly value?: never;
        readonly sourceUrl: string;
        readonly fallbackValue?: string;
      }
  );

export function ClipboardButton({
  value,
  label,
  copiedLabel,
  errorLabel,
  analyticsEventName,
  analyticsParameters,
  className = "",
  labelClassName = "",
  showLabel = false,
  sourceUrl,
  fallbackValue,
}: ClipboardButtonProps) {
  const [status, setStatus] = useState<CopyStatus>("idle");
  const resetTimer = useRef<number | undefined>(undefined);

  useEffect(() => () => window.clearTimeout(resetTimer.current), []);

  const statusLabel =
    status === "copied" ? copiedLabel : status === "error" ? errorLabel : label;

  const handleCopy = async () => {
    window.clearTimeout(resetTimer.current);

    try {
      let copyValue = value;
      if (sourceUrl) {
        if (fallbackValue !== undefined) {
          copyValue = fallbackValue;
        } else {
          const response = await fetch(sourceUrl, {
            headers: { Accept: "text/markdown" },
          });
          const contentType = response.headers.get("content-type") ?? "";
          if (!response.ok || !contentType.includes("text/markdown")) {
            throw new Error("Markdown source is unavailable");
          }
          copyValue = await response.text();
        }
      }
      if (copyValue === undefined) {
        throw new Error("Clipboard value is unavailable");
      }

      await navigator.clipboard.writeText(copyValue);
      setStatus("copied");
      sendAnalyticsEvent(analyticsEventName, {
        ...analyticsParameters,
        page_path: window.location.pathname,
      });
    } catch {
      setStatus("error");
    }

    resetTimer.current = window.setTimeout(() => setStatus("idle"), 2000);
  };

  return (
    <button
      type="button"
      onClick={handleCopy}
      aria-label={statusLabel}
      title={statusLabel}
      data-copy-status={status}
      className={`focus-visible:ring-cc-accent/50 inline-flex cursor-pointer items-center justify-center gap-2 rounded-md transition-colors focus-visible:ring-2 focus-visible:outline-none ${className}`}
    >
      {status === "copied" ? (
        <CheckIcon className="size-4" />
      ) : status === "error" ? (
        <WarningIcon className="size-4" />
      ) : (
        <CopyIcon className="size-4" />
      )}
      {showLabel ? <span className={labelClassName}>{statusLabel}</span> : null}
      <span className="sr-only" aria-live="polite" aria-atomic="true">
        {status === "idle" ? "" : statusLabel}
      </span>
    </button>
  );
}
