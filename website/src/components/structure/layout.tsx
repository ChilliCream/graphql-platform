import { MDXProvider } from "@mdx-js/react";
import React, { FunctionComponent, useEffect } from "react";
import { AutoLinkedHeading } from "../mdx/auto-linked-heading";
import { CodeBlock } from "../mdx/code-block";
import { CookieConsent } from "../misc/cookie-consent";
import { GlobalLayoutStyle, GlobalStyle } from "../misc/global-style";
import { SluggerContextProvider } from "../slugger-context";
import { Header } from "./header";
import { MainContentContainer } from "./main-content-container/main-content-container";

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

  const components = {
    pre: CodeBlock,
    h1: (props: any) => <AutoLinkedHeading size="h1" {...props} />,
    h2: (props: any) => <AutoLinkedHeading size="h2" {...props} />,
    h3: (props: any) => <AutoLinkedHeading size="h3" {...props} />,
    h4: (props: any) => <AutoLinkedHeading size="h4" {...props} />,
    h5: (props: any) => <AutoLinkedHeading size="h5" {...props} />,
    h6: (props: any) => <AutoLinkedHeading size="h6" {...props} />,
  };

  return (
    <>
      <GlobalStyle />
      <GlobalLayoutStyle />
      <Header />
      <SluggerContextProvider>
        <MDXProvider components={components}>
          <MainContentContainer>{children}</MainContentContainer>
        </MDXProvider>
      </SluggerContextProvider>
      <CookieConsent />
    </>
  );
};
