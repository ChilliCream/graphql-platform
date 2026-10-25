"use client";

import { useEffect, useRef, useState } from "react";
import { sendAnalyticsEvent } from "@/src/helpers/analytics";
import { CheckIcon } from "@/src/icons/Check";
import { ShareIcon } from "@/src/icons/Share";
import { WarningIcon } from "@/src/icons/Warning";
import { ClipboardButton } from "./ClipboardButton";

type ShareStatus = "idle" | "shared" | "copied" | "error";

interface DocsPageActionsProps {
  readonly fallbackMarkdown?: string;
  readonly markdownUrl: string;
  readonly title: string;
}

const ACTION_CLASS_NAME =
  "border-cc-card-border bg-cc-white/2.5 text-cc-ink-dim hover:border-cc-card-border-hover hover:bg-cc-white/5 hover:text-cc-heading h-9 border px-3 text-xs font-medium";

export function DocsPageActions({
  fallbackMarkdown,
  markdownUrl,
  title,
}: DocsPageActionsProps) {
  const [shareStatus, setShareStatus] = useState<ShareStatus>("idle");
  const resetTimer = useRef<number | undefined>(undefined);

  useEffect(() => () => window.clearTimeout(resetTimer.current), []);

  const shareLabel =
    shareStatus === "shared"
      ? "Shared"
      : shareStatus === "copied"
        ? "Link copied"
        : shareStatus === "error"
          ? "Could not share"
          : "Share";

  const handleShare = async () => {
    window.clearTimeout(resetTimer.current);
    const url = window.location.href;

    try {
      if (navigator.share) {
        await navigator.share({ title, url });
        setShareStatus("shared");
        sendAnalyticsEvent("share_docs", {
          method: "native",
          page_path: window.location.pathname,
        });
      } else {
        await navigator.clipboard.writeText(url);
        setShareStatus("copied");
        sendAnalyticsEvent("share_docs", {
          method: "copy_link",
          page_path: window.location.pathname,
        });
      }
    } catch (error) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return;
      }
      setShareStatus("error");
    }

    resetTimer.current = window.setTimeout(() => setShareStatus("idle"), 2000);
  };

  return (
    <div
      role="group"
      className="flex flex-wrap items-center gap-2 print:hidden"
      aria-label="Document actions"
    >
      <ClipboardButton
        sourceUrl={markdownUrl}
        fallbackValue={fallbackMarkdown}
        label="Copy as Markdown"
        copiedLabel="Markdown copied"
        errorLabel="Could not copy Markdown"
        analyticsEventName="copy_docs_markdown"
        showLabel
        className={ACTION_CLASS_NAME}
      />
      <button
        type="button"
        onClick={handleShare}
        aria-label={shareLabel}
        title={shareLabel}
        data-share-status={shareStatus}
        className={`focus-visible:ring-cc-accent/50 inline-flex cursor-pointer items-center justify-center gap-2 rounded-md transition-colors focus-visible:ring-2 focus-visible:outline-none ${ACTION_CLASS_NAME}`}
      >
        {shareStatus === "shared" || shareStatus === "copied" ? (
          <CheckIcon className="size-4" />
        ) : shareStatus === "error" ? (
          <WarningIcon className="size-4" />
        ) : (
          <ShareIcon className="size-4" />
        )}
        <span>{shareLabel}</span>
        <span className="sr-only" aria-live="polite" aria-atomic="true">
          {shareStatus === "idle" ? "" : shareLabel}
        </span>
      </button>
    </div>
  );
}
