/**
 * Layout component that queries for data
 * with Gatsby's useStaticQuery component
 *
 * See: https://www.gatsbyjs.org/docs/use-static-query/
 */

// import { useStaticQuery, graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GlobalStyle } from "./global-style";
import { Header } from "./header";

const Layout: FunctionComponent = ({ children }) => {
  return (
    <>
      <GlobalStyle />
      <Header />
      <Content id="content">
        <main>{children}</main>
        <footer>© {new Date().getFullYear()} by ChilliCream</footer>
      </Content>
    </>
  );
};

export default Layout;

const Content = styled.div`
  width: 100vw;
  height: 100vh;
  overflow-y: auto;
`;
