import React, { FC, ReactNode, useCallback, useEffect, useRef } from "react";
import { useDispatch } from "react-redux";

import {
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
} from "@/components/article-elements";
import { ArticleLayout } from "@/components/layout";
import { useObservable } from "@/state";
import { toggleAside } from "@/state/common";
import { ArticleContentFooter } from "./article-content-footer";
import { ArticleTableOfContent } from "./article-table-of-content";

// Icons
import { DefaultArticleNavigation } from "./default-article-navigation";
import { ResponsiveArticleMenu } from "./responsive-article-menu";

interface DefaultArticleMdx {
  fields?: {
    slug?: string;
    lastUpdated?: string;
    lastAuthorName?: string;
  };
  frontmatter?: {
    title?: string;
    description?: string;
  };
  headings?: Array<{
    depth?: number;
    value?: string;
  } | null>;
}

interface DefaultArticleData {
  file?: {
    childMdx?: DefaultArticleMdx;
  };
}

export interface DefaultArticleProps {
  readonly data: DefaultArticleData;
  readonly content: ReactNode;
}

export const DefaultArticle: FC<DefaultArticleProps> = ({ data, content }) => {
  const dispatch = useDispatch();
  const responsiveMenuRef = useRef<HTMLDivElement>(null);

  const { fields, frontmatter } = data.file?.childMdx || {};
  const title = frontmatter?.title || "";
  const description = frontmatter?.description;
  const slug = fields?.slug || "";

  const hasScrolled$ = useObservable((state) => {
    return state.common.yScrollPosition > 20;
  });

  const handleToggleAside = useCallback(() => {
    dispatch(toggleAside());
  }, []);

  useEffect(() => {
    const subscription = hasScrolled$.subscribe((hasScrolled) => {
      responsiveMenuRef.current?.classList.toggle("scrolled", hasScrolled);
    });

    return () => {
      subscription.unsubscribe();
    };
  }, [hasScrolled$]);

  return (
    <ArticleLayout
      navigation={<DefaultArticleNavigation data={data} selectedPath={slug} />}
      aside={<ArticleTableOfContent data={data.file?.childMdx || {}} />}
    >
      <ArticleHeader>
        <ResponsiveArticleMenu />
        <ArticleTitle>{title}</ArticleTitle>
      </ArticleHeader>
      <ArticleContent>
        {description && <p>{description}</p>}
        {content}
        <ArticleContentFooter
          lastUpdated={fields?.lastUpdated || ""}
          lastAuthorName={fields?.lastAuthorName || ""}
        />
      </ArticleContent>
    </ArticleLayout>
  );
};
