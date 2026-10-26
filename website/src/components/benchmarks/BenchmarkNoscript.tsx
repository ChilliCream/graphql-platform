const TOOLTIP = `[data-point]:is(:hover, :focus-visible) > [data-tooltip] {
  --below: clamp(0, (25 - var(--y)) * 1000, 1);
  display: block;
  position: absolute;
  z-index: 10;
  top: 50%;
  left: 50%;
  translate: calc(var(--x) * -1%) calc(var(--below) * 16px + (1 - var(--below)) * (-100% - 16px));
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-cc-card-border);
  border-radius: 0.25rem;
  background: var(--color-cc-bg);
  box-shadow: 0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1);
  color: var(--color-cc-heading);
  font-size: 0.75rem;
  line-height: 1rem;
  text-align: left;
  white-space: nowrap;
  pointer-events: none;
}
[data-tooltip] > * { display: block; }
[data-tooltip] > :nth-child(1) { font-weight: 600; }
[data-tooltip] > :nth-child(3) { margin-top: 0.25rem; }
[data-tooltip] > :nth-child(n + 3) { color: var(--color-cc-ink-dim); }
[data-tooltip] > :nth-child(5) {
  margin-top: 0.5rem;
  padding-top: 0.5rem;
  border-top: 1px solid var(--color-cc-card-border);
  color: var(--color-cc-danger);
  font-size: 11px;
}`;

/** Without JavaScript, hides every element marked `data-benchmark-controls` and shows the tooltip of a history chart point on hover and focus. */
export function BenchmarkNoscript() {
  return (
    <noscript>
      <style>{`[data-benchmark-controls] { display: none !important; } ${TOOLTIP}`}</style>
    </noscript>
  );
}
