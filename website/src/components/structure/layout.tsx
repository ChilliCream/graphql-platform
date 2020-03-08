import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GlobalStyle } from "../misc/global-style";
import { Footer } from "./footer";
import { Header } from "./header";

const Layout: FunctionComponent = ({ children }) => {
  return (
    <>
      <GlobalStyle />
      <Header />
      <Content id="content">{children}</Content>
      <Footer />
    </>
  );
};

export default Layout;

const Content = styled.main`
  display: flex;
  flex-direction: main;
  padding-top: 60px;
  background-color: #fff;
`;
