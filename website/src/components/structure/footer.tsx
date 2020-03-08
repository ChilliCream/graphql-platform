import React, { FunctionComponent } from "react";
import styled from "styled-components";

export const Footer: FunctionComponent = () => {
  return (
    <Container>
      <Copyright>© {new Date().getFullYear()} ChilliCream</Copyright>
    </Container>
  );
};

const Container = styled.footer`
  position: sticky;
  right: 0;
  bottom: 0;
  left: 0;
  display: flex;
  flex: 1 1 auto;
`;

const Copyright = styled.div``;
