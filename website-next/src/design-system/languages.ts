export type LanguageDescriptor = {
  label: string;
  color: string;
  shiki: string;
};

export const LANGUAGES: Record<string, LanguageDescriptor> = {
  ts: { label: "TypeScript", color: "#3178c6", shiki: "typescript" },
  typescript: { label: "TypeScript", color: "#3178c6", shiki: "typescript" },
  tsx: { label: "TypeScript", color: "#3178c6", shiki: "tsx" },
  js: { label: "JavaScript", color: "#f7df1e", shiki: "javascript" },
  javascript: { label: "JavaScript", color: "#f7df1e", shiki: "javascript" },
  jsx: { label: "JavaScript", color: "#f7df1e", shiki: "jsx" },
  csharp: { label: "C#", color: "#ffe261", shiki: "csharp" },
  bash: { label: "Bash", color: "#74dfc4", shiki: "bash" },
  css: { label: "CSS", color: "#5095d3", shiki: "css" },
  graphql: { label: "GraphQL", color: "#eb64b9", shiki: "graphql" },
  html: { label: "HTML", color: "#e34c26", shiki: "html" },
  sdl: { label: "SDL", color: "#eb64b9", shiki: "graphql" },
  http: { label: "HTTP", color: "#b381c5", shiki: "http" },
  json: { label: "JSON", color: "#40b4c4", shiki: "json" },
  sql: { label: "SQL", color: "#b4dce7", shiki: "sql" },
  xml: { label: "XML", color: "#ffffff", shiki: "xml" },
  diff: { label: "Diff", color: "#cccccc", shiki: "diff" },
  yaml: { label: "YAML", color: "#cb7676", shiki: "yaml" },
  yml: { label: "YAML", color: "#cb7676", shiki: "yaml" },
};

// Colors live in the theme (`--cc-step-*` in globals.css). The chip background
// and border are derived from each step's base hue via alpha.
function stepColors(
  base: string,
  text: string,
): { bg: string; border: string; text: string } {
  return {
    bg: `color-mix(in srgb, ${base} 18%, transparent)`,
    border: `color-mix(in srgb, ${base} 55%, transparent)`,
    text,
  };
}

export const STEP_PALETTE: Record<
  number,
  { bg: string; border: string; text: string }
> = {
  1: stepColors("var(--cc-step-1)", "var(--cc-step-1-text)"),
  2: stepColors("var(--cc-step-2)", "var(--cc-step-2-text)"),
  3: stepColors("var(--cc-step-3)", "var(--cc-step-3-text)"),
  4: stepColors("var(--cc-step-4)", "var(--cc-step-4-text)"),
};

export function stepStyle(step: number): {
  backgroundColor: string;
  borderColor: string;
  color: string;
} {
  const c = STEP_PALETTE[step] ?? STEP_PALETTE[1];
  return { backgroundColor: c.bg, borderColor: c.border, color: c.text };
}
