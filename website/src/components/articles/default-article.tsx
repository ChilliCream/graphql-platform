import { graphql } from "gatsby";
import { MDXRenderer } from "gatsby-plugin-mdx";
import React, { FC, useCallback, useEffect, useRef } from "react";
import { useDispatch } from "react-redux";

import {
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
} from "@/components/article-elements";
import { ArticleLayout } from "@/components/layout";
import { DefaultArticleFragment } from "@/graphql-types";
import { useObservable } from "@/state";
import { toggleAside } from "@/state/common";
import { ArticleContentFooter } from "./article-content-footer";
import { ArticleTableOfContent } from "./article-table-of-content";

// Icons
import { DefaultArticleNavigation } from "./default-article-navigation";
import { ResponsiveArticleMenu } from "./responsive-article-menu";

export interface DefaultArticleProps {
  readonly data: DefaultArticleFragment;
}

export const DefaultArticle: FC<DefaultArticleProps> = ({ data }) => {
  const dispatch = useDispatch();
  const responsiveMenuRef = useRef<HTMLDivElement>(null);

  const { fields, frontmatter, body } = data.file!.childMdx!;
  const title = frontmatter!.title!;
  const description = frontmatter!.description;
  const slug = fields!.slug!;

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
      aside={<ArticleTableOfContent data={data.file!.childMdx!} />}
    >
      <ArticleHeader>
        <ResponsiveArticleMenu />
        <ArticleTitle>{title}</ArticleTitle>
      </ArticleHeader>
      <ArticleContent>
        {description && <p>{description}</p>}
        <MDXRenderer>{body}</MDXRenderer>
        <ArticleContentFooter
          lastUpdated={fields!.lastUpdated!}
          lastAuthorName={fields!.lastAuthorName!}
        />
      </ArticleContent>
    </ArticleLayout>
  );
};

export const DefaultArticleGraphQLFragment = graphql`
  fragment DefaultArticle on Query {
    file(
      sourceInstanceName: { eq: "basic" }
      relativePath: { eq: $originPath }
    ) {
      childMdx {
        fields {
          slug
          lastUpdated
          lastAuthorName
        }
        frontmatter {
          title
          description
        }
        body
        ...ArticleSections
      }
    }
    ...DefaultArticleNavigation
  }
`;
