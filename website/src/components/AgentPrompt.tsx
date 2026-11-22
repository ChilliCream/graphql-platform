"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import { ClipboardButton } from "@/src/components/ClipboardButton";
import { ChevronDownIcon } from "@/src/icons/ChevronDown";

interface AgentPromptProps {
  /** Heading of the collapsed card, e.g. "Migration prompt". */
  readonly title: string;
  /** A fenced code block with the prompt text. */
  readonly children: ReactNode;
}

/** A collapsed prompt for coding agents with a copy button; the prompt text shows when expanded. */
export function AgentPrompt({ title, children }: AgentPromptProps) {
  const content = useRef<HTMLDivElement>(null);
  const [prompt, setPrompt] = useState("");

  useEffect(() => {
    setPrompt(content.current?.querySelector("pre")?.textContent ?? "");
  }, []);

  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 relative my-6 rounded-lg border">
      <ClipboardButton
        value={prompt}
        label="Copy prompt"
        copiedLabel="Prompt copied"
        errorLabel="Could not copy the prompt"
        analyticsEventName="copy_agent_prompt"
        analyticsParameters={{ prompt_title: title }}
        showLabel
        className="border-cc-card-border bg-cc-white/2.5 text-cc-ink-dim hover:border-cc-card-border-hover hover:bg-cc-white/5 hover:text-cc-heading absolute top-2.5 right-3 z-10 h-8 border px-3 text-xs font-medium"
      />
      <details className="group">
        <summary className="flex min-h-13 cursor-pointer list-none items-center gap-3 py-3 pr-36 pl-4">
          <ChevronDownIcon
            aria-hidden="true"
            className="text-cc-ink-dim size-3.5 shrink-0 fill-current transition-transform group-open:rotate-180"
          />
          <span className="text-cc-heading text-sm font-semibold whitespace-nowrap">
            {title}
          </span>
          <span
            data-llms-ignore
            className="text-cc-ink-dim hidden text-xs sm:inline sm:group-open:hidden"
          >
            Show prompt
          </span>
          <span
            data-llms-ignore
            className="text-cc-ink-dim hidden text-xs sm:group-open:inline"
          >
            Hide prompt
          </span>
        </summary>
        <div
          ref={content}
          className="px-4 pb-4 [&_figcaption]:hidden [&_figure]:my-0 [&_figure>button]:hidden"
        >
          {children}
        </div>
      </details>
    </div>
  );
}
