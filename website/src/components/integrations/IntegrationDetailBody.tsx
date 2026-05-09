"use client";

import React, { FC, useCallback, useState } from "react";

import type {
  Integration,
  IntegrationSection,
} from "@/data/integrations/integrations";

interface IntegrationDetailBodyProps {
  readonly integration: Integration;
}

interface CodeBlockProps {
  readonly language: string;
  readonly code: string;
}

// Same shape as the templates CodeBlock (silent clipboard failure, .is-copied
// flash for 1.6s). Scoped with the .cc-ind- class so the detail page styling
// stays self-contained.
const CodeBlock: FC<CodeBlockProps> = ({ language, code }) => {
  const [copied, setCopied] = useState(false);
  const onCopy = useCallback(async (): Promise<void> => {
    try {
      await navigator.clipboard.writeText(code);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1600);
    } catch {
      // Browser denied clipboard access. Silent failure is fine: users on
      // hardened browsers can still select+copy by hand.
    }
  }, [code]);

  return (
    <div className="cc-ind-code">
      <div className="cc-ind-code-head">
        <span className="cc-ind-code-lang">{language}</span>
        <button
          type="button"
          className={`cc-ind-code-copy${copied ? " is-copied" : ""}`}
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

interface InstallLineProps {
  readonly command: string;
}

// Single-line install command with a copy button. Mirrors the brief's
// "single-line install command for the primary section" requirement.
const InstallLine: FC<InstallLineProps> = ({ command }) => {
  const [copied, setCopied] = useState(false);
  const onCopy = useCallback(async (): Promise<void> => {
    try {
      await navigator.clipboard.writeText(command);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1600);
    } catch {
      // Silent failure (see CodeBlock).
    }
  }, [command]);

  return (
    <div className="cc-ind-install">
      <code>$ {command}</code>
      <button
        type="button"
        className={copied ? "is-copied" : ""}
        onClick={onCopy}
        aria-label={copied ? "Copied" : "Copy install command"}
      >
        {copied ? "Copied" : "Copy"}
      </button>
    </div>
  );
};

// Detail body: long-form sections from integration.body[]. The primary
// section gets the install line under its first paragraph (rendered by
// integrationsRoot's first-h2 rule). For now we keep MDX out of scope and
// render typed sections; once integrations grow READMEs we'll swap to remote
// MDX keyed by the same section schema, same plan as templates.
export const IntegrationDetailBody: FC<IntegrationDetailBodyProps> = ({
  integration,
}) => {
  const renderSection = (section: IntegrationSection, index: number) => {
    return (
      <section key={`${section.heading}-${index}`}>
        <h2>{section.heading}</h2>
        {section.paragraphs.map((p, pi) => (
          <p key={pi}>{p}</p>
        ))}
        {section.heading === "Setup" && (
          <InstallLine command={integration.install} />
        )}
        {section.code && (
          <CodeBlock
            language={section.code.language}
            code={section.code.code}
          />
        )}
      </section>
    );
  };

  return (
    <div className="cc-ind-body-main">
      {integration.body.map(renderSection)}
    </div>
  );
};
