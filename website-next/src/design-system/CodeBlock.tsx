import {
  Children,
  isValidElement,
  type ComponentPropsWithoutRef,
  type ReactNode,
} from "react";
import { codeToHtml } from "shiki";
import { LANGUAGES, STEP_PALETTE } from "./languages";
import { parseCodeBlockMeta } from "@/src/helpers/parseCodeBlockMeta";

const THEME = "github-dark";

type CodeBlockProps = ComponentPropsWithoutRef<"pre">;

type ExtractedCode = {
  code: string;
  language: string;
  meta: string;
};

function extract(children: ReactNode): ExtractedCode | null {
  const codeEl = Children.toArray(children).find(isValidElement);
  if (!codeEl) {
    return null;
  }

  const props = codeEl.props as {
    className?: string;
    "data-meta"?: string;
    children?: ReactNode;
  };
  const className = props.className ?? "";
  const langMatch = className.match(/(?:^|\s)language-([^\s]+)/);
  const language = langMatch?.[1] ?? "text";
  const meta = props["data-meta"] ?? "";
  const code = String(props.children ?? "").replace(/\n$/, "");

  return { code, language, meta };
}

export async function CodeBlock({ children, className = "" }: CodeBlockProps) {
  const extracted = extract(children);
  if (!extracted) {
    return <pre className={className}>{children}</pre>;
  }

  const { code, language, meta } = extracted;
  const parsed = parseCodeBlockMeta(meta);
  const descriptor = LANGUAGES[language];
  const shikiLang = descriptor?.shiki ?? language;

  let html: string;
  try {
    html = await codeToHtml(code, {
      lang: shikiLang,
      theme: THEME,
      transformers: [
        {
          line(node, line) {
            if (parsed.highlightedLines.has(line)) {
              const existing =
                typeof node.properties.class === "string"
                  ? node.properties.class
                  : "";
              node.properties.class = `${existing} line-highlighted`.trim();
            }
            const stepsForLine = parsed.steps.filter((s) => s.line === line);
            if (stepsForLine.length === 0) {
              return;
            }
            for (const child of node.children) {
              if (
                child.type !== "element" ||
                child.tagName !== "span" ||
                !child.children?.length ||
                child.children[0].type !== "text"
              ) {
                continue;
              }
              const rawText = child.children[0].value;
              const text = rawText.trim();
              const match = stepsForLine.find((s) => s.text === text);
              if (!match) {
                continue;
              }
              const palette = STEP_PALETTE[match.step] ?? STEP_PALETTE[1];
              const leading = rawText.match(/^\s*/)?.[0] ?? "";
              const trailing = rawText.slice(leading.length).match(/\s*$/)?.[0] ?? "";
              const coreLen = rawText.length - leading.length - trailing.length;
              if (coreLen <= 0) {
                continue;
              }
              const core = rawText.slice(
                leading.length,
                leading.length + coreLen
              );
              const innerSpan = {
                type: "element" as const,
                tagName: "span",
                properties: {
                  "data-step": match.step,
                  style: `background:${palette.bg};border:1px solid ${palette.border};border-radius:0.25rem;padding:0 0.25rem;color:${palette.text}`,
                },
                children: [{ type: "text" as const, value: core }],
              };
              const newChildren: typeof child.children = [];
              if (leading) {
                newChildren.push({ type: "text" as const, value: leading });
              }
              newChildren.push(innerSpan);
              if (trailing) {
                newChildren.push({ type: "text" as const, value: trailing });
              }
              child.children = newChildren;
            }
          },
        },
      ],
    });
  } catch {
    html = await codeToHtml(code, { lang: "text", theme: THEME });
  }

  return (
    <figure className="my-6 overflow-hidden rounded-lg ring-1 ring-slate-700 bg-[#0d1117] shadow-md">
      {(descriptor || parsed.filename) && (
        <figcaption className="flex items-center gap-3 border-b border-slate-700/60 bg-[#161b22] px-4 py-2 text-xs">
          {descriptor ? (
            <span
              className="rounded px-2 py-0.5 font-semibold uppercase tracking-wider"
              style={{
                color: descriptor.color,
                backgroundColor: `${descriptor.color}1f`,
              }}
            >
              {descriptor.label}
            </span>
          ) : (
            <span className="font-mono text-slate-500">{language}</span>
          )}
          {parsed.filename ? (
            <span className="font-mono text-slate-300">{parsed.filename}</span>
          ) : null}
        </figcaption>
      )}
      <div
        className="shiki-wrapper overflow-x-auto text-sm leading-6"
        dangerouslySetInnerHTML={{ __html: html }}
      />
    </figure>
  );
}
