import { MDXProvider } from "@mdx-js/react";
import React, { FunctionComponent } from "react";
import { CodeBlock } from "../mdx/code-block";
import { InlineCode } from "../mdx/inline-code";
import { CookieConsent } from "../misc/cookie-consent";
import { GlobalLayoutStyle, GlobalStyle } from "../misc/global-style";
import { Header } from "./header";
import { MainContentContainer } from "./main-content-container/main-content-container";

export const Layout: FunctionComponent = ({ children }) => {
  const components = {
    pre: CodeBlock,
    inlineCode: InlineCode,
  };

  return (
    <>
      <GlobalStyle />
      <GlobalLayoutStyle />
      <Header />
      <MDXProvider components={components}>
        <MainContentContainer>{children}</MainContentContainer>
      </MDXProvider>
      <CookieConsent />
    </>
  );
};
