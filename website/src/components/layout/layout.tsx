import { MDXProvider } from "@mdx-js/react";
import { ThemeProvider } from "@mui/material";
import React, { FC } from "react";
import { CodeBlock } from "../mdx/code-block";
import { InlineCode } from "../mdx/inline-code";
import { CookieConsent } from "../misc/cookie-consent";
import { GlobalStyle } from "../misc/global-style";
import { MUI_THEME } from "../misc/mui-theme";
import { Header } from "./header";
import { Main } from "./main";

export const Layout: FC = ({ children }) => {
  const components = {
    pre: CodeBlock,
    inlineCode: InlineCode,
  };

  return (
    <>
      <GlobalStyle />
      <ThemeProvider theme={MUI_THEME}>
        <Header />
        <MDXProvider components={components}>
          <Main>{children}</Main>
        </MDXProvider>
        <CookieConsent />
      </ThemeProvider>
    </>
  );
};
