import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GlobalStyle } from "../misc/global-style";
import { Footer } from "./footer";
import { Header } from "./header";
import { CookieConsent } from "../misc/cookie-consent";
import { PageTop } from "../misc/page-top";

export const Layout: FunctionComponent = ({ children }) => {
  return (
    <>
      <GlobalStyle />
      <Header />
      <Content>{children}</Content>
      <Footer />
      <PageTop />
      <CookieConsent />
    </>
  );
};

const Content = styled.main`
  display: flex;
  flex-direction: column;
  align-items: center;
  padding-top: 60px;
  background-color: #fff;
`;
