"use client";

import React, { FC, useCallback, useState } from "react";

interface CodeBlockProps {
  readonly language: string;
  readonly code: string;
}

// Reusable code block with a copy button. We keep this tiny: no syntax
// highlighting (the docs site already handles that and pulling in
// prism-react-renderer for eight templates is not worth the bytes), no
// line numbers, just monospace + cream ink. The copy button gives a brief
// visual confirmation by toggling .is-copied for 1.6s.
export const CodeBlock: FC<CodeBlockProps> = ({ language, code }) => {
  const [copied, setCopied] = useState(false);

  const onCopy = useCallback(async (): Promise<void> => {
    try {
      await navigator.clipboard.writeText(code);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1600);
    } catch {
      // Browser denied clipboard access. Silent failure is fine here:
      // users on hardened browsers can still select+copy by hand.
    }
  }, [code]);

  return (
    <div className="cc-tpd-code">
      <div className="cc-tpd-code-head">
        <span className="cc-tpd-code-lang">{language}</span>
        <button
          type="button"
          className={`cc-tpd-code-copy${copied ? " is-copied" : ""}`}
          onClick={onCopy}
          aria-label={copied ? "Copied" : "Copy code"}
        >
          {copied ? "Copied" : "Copy"}
        </button>
      </div>
      <pre>
        <code>{code}</code>
      </pre>
    </div>
  );
};
