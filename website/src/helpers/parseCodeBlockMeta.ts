export type CodeStepSpec = { step: number; line: number; text: string };

export type CodeBlockMeta = {
  filename?: string;
  highlightedLines: Set<number>;
  steps: CodeStepSpec[];
};

export function parseCodeBlockMeta(meta: string): CodeBlockMeta {
  const result: CodeBlockMeta = {
    filename: undefined,
    highlightedLines: new Set(),
    steps: [],
  };

  if (!meta) {
    return result;
  }

  // filename="X.tsx"  or  filename=foo.ts
  const fileMatch = meta.match(/filename=("([^"]+)"|(\S+))/);
  if (fileMatch) {
    result.filename = fileMatch[2] ?? fileMatch[3];
  }

  // line highlight: {1,3-5}
  const lineMatch = meta.match(/\{([\d,\s\-]+)\}/);
  if (lineMatch) {
    for (const part of lineMatch[1].split(",")) {
      const trimmed = part.trim();
      if (!trimmed) continue;
      const [a, b] = trimmed.split("-").map((s) => parseInt(s.trim(), 10));
      const start = Number.isFinite(a) ? a : 0;
      const end = Number.isFinite(b) ? b : start;
      for (let i = start; i <= end; i++) {
        if (i > 0) result.highlightedLines.add(i);
      }
    }
  }

  // code steps: [[1, 7, "count"], [2, 7, "dispatchAction"], ...]
  const stepsMatch = meta.match(/\[\s*\[[\s\S]*\]\s*\]/);
  if (stepsMatch) {
    try {
      const parsed = JSON.parse(stepsMatch[0]);
      if (Array.isArray(parsed)) {
        for (const item of parsed) {
          if (
            Array.isArray(item) &&
            item.length === 3 &&
            typeof item[0] === "number" &&
            typeof item[1] === "number" &&
            typeof item[2] === "string"
          ) {
            result.steps.push({ step: item[0], line: item[1], text: item[2] });
          }
        }
      }
    } catch {
      // ignore malformed step spec
    }
  }

  return result;
}
