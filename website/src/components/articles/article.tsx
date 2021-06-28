import styled from "styled-components";
import React, { FunctionComponent } from "react";

export const Article: FunctionComponent = ({ children }) => {
  return <ArticleElement>{children}</ArticleElement>;
};

const ArticleElement = styled.article`
  overflow: hidden;
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  margin-bottom: 40px;
  padding-bottom: 20px;
  max-width: 820px;

  @media only screen and (min-width: 820px) {
    border-radius: 4px;
    box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);
  }
`;
