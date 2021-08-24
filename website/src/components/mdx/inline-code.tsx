import React, { FunctionComponent } from "react";
import styled from "styled-components";

export const InlineCode: FunctionComponent = ({ children }) => {
  return <Container>{children}</Container>;
};

const Container = styled.code`
  padding: 2px 5px;
  font-family: Consolas, Monaco, "Andale Mono", "Ubuntu Mono", monospace;
  font-size: var(--font-size);
`;
