/** Without JavaScript, shows every element marked `data-benchmark-panel` and hides every element marked `data-benchmark-controls`. */
export function BenchmarkNoscript() {
  return (
    <noscript>
      <style>{`[data-benchmark-panel] { display: block !important; margin-top: 2rem; } [data-benchmark-controls] { display: none !important; }`}</style>
    </noscript>
  );
}
