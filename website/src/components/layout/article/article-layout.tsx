import React, { ReactElement, ReactNode } from "react";

import { TabGroupProvider } from "@/components/mdx/tabs";
import { ArticleLayoutAside } from "./article-layout-aside";
import {
  Article,
  ArticleContainer,
  ArticleWrapper,
  LayoutContainer,
} from "./article-layout-elements";
import { ArticleLayoutNavigation } from "./article-layout-navigation";
import { useCalculateArticleHeight } from "./use-calculate-article-height";

export interface ArticleLayoutProps {
  readonly children: ReactNode;
  readonly navigation: ReactNode;
  readonly aside: ReactNode;
}

export function ArticleLayout({
  children,
  navigation,
  aside,
}: ArticleLayoutProps): ReactElement {
  const ref = useCalculateArticleHeight();

  return (
    <TabGroupProvider>
      <LayoutContainer>
        <ArticleLayoutNavigation>{navigation}</ArticleLayoutNavigation>
        <ArticleWrapper ref={ref}>
          <ArticleContainer>
            <Article>{children}</Article>
          </ArticleContainer>
        </ArticleWrapper>
        <ArticleLayoutAside>{aside}</ArticleLayoutAside>
      </LayoutContainer>
    </TabGroupProvider>
  );
}
