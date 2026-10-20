export function BenchmarkPreviewBadge() {
  return (
    <span
      className="border-cc-accent/50 bg-cc-accent/10 text-cc-accent inline-flex size-4 shrink-0 items-center justify-center rounded border font-mono text-[10px] leading-none font-semibold"
      title="Preview"
    >
      <span aria-hidden="true">P</span>
      <span className="sr-only">Preview</span>
    </span>
  );
}
