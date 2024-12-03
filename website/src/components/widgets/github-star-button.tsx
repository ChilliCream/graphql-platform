import React, { ReactElement } from "react";
import GitHubButton from "react-github-btn";
import styled from "styled-components";

export function GitHubStarButton(): ReactElement {
  return (
    <Container>
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
