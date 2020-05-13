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
  width: 100vw;
  background-color: #fff;

  @media only screen and (min-width: 1500px) {
    margin: 0 auto;
    width: 1300px;
  }
`;
