import React, { FunctionComponent } from "react";
import styled from "styled-components";

export const SalesCardPerkItem: FunctionComponent = ({ children }) => {
  return (
    <Container>
      <Bullet>- </Bullet>
      <Content>{children}</Content>
    </Container>
  );
};

const Container = styled.div`
  display: grid;
  grid-template-rows: auto;
  grid-template-columns: 20px 1fr;
  margin: 3px 0;
`;

const Bullet = styled.div`
  margin-left: 12px;
`;

const Content = styled.div`
  margin-left: 5px;
  font-size: 0.875rem;
  line-height: 1.25rem;
`;
