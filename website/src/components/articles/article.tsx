import styled from "styled-components";

export const Article = styled.article`
  overflow: hidden;
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  margin-bottom: 40px;
  padding-bottom: 20px;

  @media only screen and (min-width: 860px) {
    border-radius: var(--border-radius);
    box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);
    max-width: 820px;
  }
`;
