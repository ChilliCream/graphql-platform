import React, { ReactElement, useEffect, useRef } from "react";
import GitHubButton from "react-github-btn";
import styled from "styled-components";

export function GitHubStarButton(): ReactElement {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    const addIframeTitle = () => {
      const iframe = containerRef.current?.querySelector("iframe");
      if (iframe && !iframe.title) {
        iframe.title = "GitHub stars for ChilliCream/graphql-platform";
      }
    };

    // The iframe is created asynchronously, observe for it
    const observer = new MutationObserver(addIframeTitle);
    observer.observe(containerRef.current, { childList: true, subtree: true });
    addIframeTitle();

    return () => observer.disconnect();
  }, []);

  return (
    <Container ref={containerRef}>
      <GitHubButton
        href="https://github.com/ChilliCream/graphql-platform"
        data-size="small"
        data-show-count="true"
        aria-label="Star ChilliCream/graphql-platform on GitHub"
      >
        Star
      </GitHubButton>
    </Container>
  );
}

const Container = styled.div`
  font-size: 0;
  letter-spacing: 0;
  line-height: 0;
`;
