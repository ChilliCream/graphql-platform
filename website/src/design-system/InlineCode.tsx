import type { ComponentPropsWithoutRef } from "react";
import { codeToHtml } from "shiki";
import { CODE_THEME, LANGUAGES } from "./languages";

interface InlineCodeProps extends ComponentPropsWithoutRef<"code"> {
  readonly "data-language"?: string;
}

export function InlineCode({
  className = "",
  "data-language": language,
  children,
  ...props
}: InlineCodeProps) {
  const isBlock = className.startsWith("language-");
  if (isBlock) {
    return (
      <code className={className} {...props}>
        {children}
      </code>
    );
  }
  if (language && typeof children === "string") {
    return (
      <HighlightedInlineCode
        code={children}
        language={language}
        className={className}
        {...props}
      />
    );
  }
  return (
    <code
      className={`bg-cc-ink-faint text-cc-prose ring-cc-card-border rounded px-1.5 py-0.5 font-mono text-[0.875em] ring-1 ${className}`.trim()}
      {...props}
    >
      {children}
    </code>
  );
}

interface HighlightedInlineCodeProps extends Omit<
  ComponentPropsWithoutRef<"code">,
  "children"
> {
  readonly code: string;
  readonly language: string;
}

async function HighlightedInlineCode({
  code,
  language,
  className = "",
  ...props
}: HighlightedInlineCodeProps) {
  const shikiLang = LANGUAGES[language]?.shiki ?? language;

  let html: string;
  try {
    html = await codeToHtml(code, {
      lang: shikiLang,
      theme: CODE_THEME,
      structure: "inline",
    });
  } catch {
    return (
      <InlineCode className={className} {...props}>
        {code}
      </InlineCode>
    );
  }

  return (
    <code
      className={`bg-cc-code-bg ring-cc-card-border rounded px-1.5 py-0.5 font-mono text-[0.875em] ring-1 ${className}`.trim()}
      {...props}
      dangerouslySetInnerHTML={{ __html: html }}
    />
  );
}
