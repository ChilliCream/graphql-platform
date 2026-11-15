/** Without JavaScript, hides every element marked `data-benchmark-controls`. */
export function BenchmarkNoscript() {
  return (
    <noscript>
      <style>{`[data-benchmark-controls] { display: none !important; }`}</style>
    </noscript>
  );
}
