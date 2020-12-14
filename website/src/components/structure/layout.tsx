import React, {FunctionComponent, useEffect} from "react";
import {GlobalStyle} from "../misc/global-style";
import {Header} from "./header";
import {CookieConsent} from "../misc/cookie-consent";
import {MainContentContainer} from './main-content-container/main-content-container';

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
      <MainContentContainer>{children}</MainContentContainer>
      <CookieConsent />
    </>
  );
};
