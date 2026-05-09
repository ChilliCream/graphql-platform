"use client";

import { useEffect, useId, useState } from "react";

/**
 * Renders a Mermaid diagram from its plain-text source. Mermaid is loaded
 * lazily on mount so the ~250 KB library only ships on pages that actually
 * use it. Output is a plain SVG with no chrome — sits directly on the page
 * background.
 */
export function MermaidDiagram({ source }: { source: string }) {
  const reactId = useId();
  const id = `mmd-${reactId.replace(/[^a-zA-Z0-9]/g, "")}`;
  const [svg, setSvg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const mermaid = (await import("mermaid")).default;
        mermaid.initialize({
          startOnLoad: false,
          securityLevel: "strict",
          theme: "default",
        });
        const { svg } = await mermaid.render(id, source);
        if (!cancelled) {
          setSvg(svg);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : String(err));
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [id, source]);

  if (error) {
    return (
      <pre className="my-6 overflow-x-auto rounded border border-red-300 bg-red-50 p-4 text-sm text-red-800">
        {`Mermaid render error: ${error}\n\n${source}`}
      </pre>
    );
  }

  if (!svg) {
    return (
      <div
        className="my-6 text-sm text-stone-500"
        role="status"
        aria-busy="true"
      >
        Rendering diagram…
      </div>
    );
  }

  return (
    <div
      className="my-6 flex justify-center [&>svg]:max-w-full [&>svg]:h-auto"
      dangerouslySetInnerHTML={{ __html: svg }}
    />
  );
}
