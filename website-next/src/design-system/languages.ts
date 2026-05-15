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

export const STEP_PALETTE: Record<
  number,
  { bg: string; border: string; text: string }
> = {
  1: { bg: "rgba(20, 184, 166, 0.18)", border: "rgba(20, 184, 166, 0.55)", text: "#5eead4" },
  2: { bg: "rgba(245, 158, 11, 0.18)", border: "rgba(245, 158, 11, 0.55)", text: "#fcd34d" },
  3: { bg: "rgba(168, 85, 247, 0.18)", border: "rgba(168, 85, 247, 0.55)", text: "#c4b5fd" },
  4: { bg: "rgba(244, 114, 182, 0.18)", border: "rgba(244, 114, 182, 0.55)", text: "#f9a8d4" },
};

export function stepStyle(step: number): { backgroundColor: string; borderColor: string; color: string } {
  const c = STEP_PALETTE[step] ?? STEP_PALETTE[1];
  return { backgroundColor: c.bg, borderColor: c.border, color: c.text };
}
