import React, { FC } from "react";
import styled from "styled-components";

export const ContentComponent: FC = ({ children }) => {
  return <Content>{children}</Content>;
};

const Content = styled.main`
  place-items: center;
  display: grid;
  overflow: visible;
  width: 100%;
`;
