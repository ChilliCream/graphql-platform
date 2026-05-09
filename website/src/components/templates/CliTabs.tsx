"use client";

import React, { FC, useCallback, useState } from "react";

import type { CliCommand } from "@/data/templates/templates";

interface CliTabsProps {
  readonly commands: readonly CliCommand[];
}

// Multi-tab CLI snippet — Vercel uses npm / yarn / pnpm / bun for their
// templates; we use dotnet / nitro init / git clone where applicable. The
// active tab is local state because the choice is per-template and per-
// reader: there is no good URL or persistence story for it. We keep the
// copy button aligned to the top-right so the surface stays predictable as
// the snippet length changes between tabs.
export const CliTabs: FC<CliTabsProps> = ({ commands }) => {
  const [activeKey, setActiveKey] = useState(commands[0]?.key ?? "");
  const [copied, setCopied] = useState(false);

  const active = commands.find((c) => c.key === activeKey) ?? commands[0];

  const onCopy = useCallback(async (): Promise<void> => {
    if (!active) {
      return;
    }
    try {
      await navigator.clipboard.writeText(active.code);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1600);
    } catch {
      // Silent failure (see CodeBlock).
    }
  }, [active]);

  if (!active) {
    return null;
  }

  return (
    <div className="cc-tpd-clitabs">
      <div className="cc-tpd-clitabs-head" role="tablist">
        {commands.map((c) => (
          <button
            key={c.key}
            type="button"
            role="tab"
            aria-selected={c.key === activeKey}
            className={`cc-tpd-clitabs-tab${
              c.key === activeKey ? " is-active" : ""
            }`}
            onClick={() => setActiveKey(c.key)}
          >
            {c.label}
          </button>
        ))}
      </div>
      <div className="cc-tpd-clitabs-body">
        <button
          type="button"
          className={`cc-tpd-clitabs-copy${copied ? " is-copied" : ""}`}
          onClick={onCopy}
          aria-label={copied ? "Copied" : "Copy command"}
        >
          {copied ? "Copied" : "Copy"}
        </button>
        <pre>
          <code>{active.code}</code>
        </pre>
      </div>
    </div>
  );
};
