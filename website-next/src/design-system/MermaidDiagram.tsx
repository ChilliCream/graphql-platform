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
          theme: "base",
          themeVariables: {
            darkMode: true,
            background: "#0c1322",
            // Node fills: dark surface with light, legible text.
            primaryColor: "#0c1322",
            primaryTextColor: "#f5f1ea",
            primaryBorderColor: "rgba(245, 241, 234, 0.32)",
            secondaryColor: "#141d31",
            secondaryTextColor: "#f5f1ea",
            secondaryBorderColor: "rgba(245, 241, 234, 0.32)",
            tertiaryColor: "#1a2540",
            tertiaryTextColor: "#f5f1ea",
            tertiaryBorderColor: "rgba(245, 241, 234, 0.32)",
            // Edges, lines and general text on the dark page.
            lineColor: "rgba(245, 241, 234, 0.55)",
            textColor: "#f5f1ea",
            mainBkg: "#0c1322",
            nodeBorder: "rgba(245, 241, 234, 0.32)",
            clusterBkg: "rgba(12, 19, 34, 0.55)",
            clusterBorder: "rgba(245, 241, 234, 0.16)",
            titleColor: "#f5f1ea",
            edgeLabelBackground: "#0b0f1a",
            // Accent used for active/special states matches the site accent.
            nodeTextColor: "#f5f1ea",
          },
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
      <pre className="my-6 overflow-x-auto rounded border border-red-500/40 bg-red-950/40 p-4 text-sm text-red-200">
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
