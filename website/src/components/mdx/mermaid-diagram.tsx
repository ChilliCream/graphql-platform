"use client";

import React, { useEffect, useRef, useState } from "react";
import styled from "styled-components";

let mermaidPromise: Promise<typeof import("mermaid")> | null = null;

function getMermaid() {
  if (!mermaidPromise) {
    mermaidPromise = import("mermaid").then((m) => {
      m.default.initialize({
        startOnLoad: false,
        theme: "dark",
        fontFamily: "inherit",
      });
      return m;
    });
  }
  return mermaidPromise;
}

let idCounter = 0;

interface MermaidDiagramProps {
  readonly code: string;
}

export function MermaidDiagram({ code }: MermaidDiagramProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [svg, setSvg] = useState<string | null>(null);

  useEffect(() => {
    const id = `mermaid-${idCounter++}`;

    getMermaid().then(async (m) => {
      try {
        const { svg: rendered } = await m.default.render(id, code);
        setSvg(rendered);
      } catch {
        // On error, show the raw code
        setSvg(null);
      }
    });
  }, [code]);

  if (svg) {
    return (
      <Container ref={containerRef} dangerouslySetInnerHTML={{ __html: svg }} />
    );
  }

  return (
    <Container ref={containerRef}>
      <pre className="mermaid">{code}</pre>
    </Container>
  );
}

const Container = styled.div`
  display: flex;
  justify-content: center;
  margin-bottom: 16px;

  svg {
    max-width: 100%;
    height: auto;
  }
`;
