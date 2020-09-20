import React, { FunctionComponent, useEffect } from "react";
import styled from "styled-components";
import { GlobalStyle } from "../misc/global-style";
import { Footer } from "./footer";
import { Header } from "./header";
import { CookieConsent } from "../misc/cookie-consent";
import { PageTop } from "../misc/page-top";

export const Layout: FunctionComponent = ({ children }) => {
  useEffect(() => {
    const { hash } = window.location;

    if (hash) {
      const headlineElement = document.getElementById(hash.substring(1));

      if (headlineElement) {
        window.setTimeout(
          () => window.scrollTo(0, headlineElement.offsetTop - 80),
          100
        );
      }
    }
  });

  return (
    <>
      <GlobalStyle />
      <Header />
      <MainContentWrapper>
        <Content>{children}</Content>
        <Footer />
      </MainContentWrapper>
      <PageTop />
      <CookieConsent />
    </>
  );
};

const MainContentWrapper = styled.div`
  display: flex;
  flex-direction: column;
  width: 100vw;
  background-color: #fff;
`;

const Content = styled.main`
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-top: 60px;
  width: 100%;
`;
