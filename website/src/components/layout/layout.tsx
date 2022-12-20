import { MDXProvider } from "@mdx-js/react";
import React, { FC, PropsWithChildren } from "react";

import { BlockQuote } from "@/components/mdx/block-quote";
import { CodeBlock } from "@/components/mdx/code-block";
import {
  Annotation,
  Code,
  ExampleTabs,
  Schema,
} from "@/components/mdx/example-tabs";
import { InlineCode } from "@/components/mdx/inline-code";
import { PackageInstallation } from "@/components/mdx/package-installation";
import { CookieConsent } from "@/components/misc/cookie-consent";
import { GlobalStyle } from "@/components/misc/global-style";
import { Header } from "./header";
import { Main } from "./main";

export const Layout: FC<PropsWithChildren<unknown>> = ({ children }) => {
  const components = {
    pre: CodeBlock,
    inlineCode: InlineCode,
    blockquote: BlockQuote,
    ExampleTabs,
    Annotation,
    Code,
    Schema,
    PackageInstallation,
  };

  return (
    <>
      <GlobalStyle />
      <Header />
      <MDXProvider components={components}>
        <Main>{children}</Main>
      </MDXProvider>
      <CookieConsent />
    </>
  );
};
