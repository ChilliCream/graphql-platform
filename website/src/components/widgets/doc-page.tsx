import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { DocPageFragment } from "../../../graphql-types";
import { ArticleComments } from "../misc/article-comments";
import {
  Article,
  ArticleContent,
  ArticleHeader,
  ArticleTitle,
  ArticleWrapper,
} from "../misc/article-elements";
import { ArticleTableOfContent } from "../misc/article-table-of-content";
import { DocPageAside } from "../misc/doc-page-aside";
import { DocPageCommunity } from "../misc/doc-page-community";
import { DocPageLegacy } from "../misc/doc-page-legacy";
import { DocPageNavigation } from "../misc/doc-page-navigation";

interface DocPageProperties {
  data: DocPageFragment;
  originPath: string;
}

export const DocPage: FunctionComponent<DocPageProperties> = ({
  data,
  originPath,
}) => {
  const { fields, frontmatter, html } = data.file!.childMarkdownRemark!;
  const slug = fields!.slug!.substring(1);
  const path = `/docs/${slug}`;
  const selectedProduct = slug.substring(0, slug.indexOf("/"));
  const title = frontmatter!.title!;

  return (
    <Container>
      <DocPageNavigation
        data={data}
        selectedPath={path}
        selectedProduct={selectedProduct}
      />
      <ArticleWrapper>
        <Article>
          <DocPageLegacy />
          <ArticleHeader>
            <ArticleTitle>{title}</ArticleTitle>
          </ArticleHeader>
          <ArticleContent dangerouslySetInnerHTML={{ __html: html! }} />
        </Article>
        <ArticleComments data={data} path={path} title={title} />
      </ArticleWrapper>
      <DocPageAside>
        <DocPageCommunity data={data} originPath={originPath} />
        <ArticleTableOfContent data={data.file!.childMarkdownRemark!} />
      </DocPageAside>
    </Container>
  );
};

export const DocPageGraphQLFragment = graphql`
  fragment DocPage on Query {
    file(
      sourceInstanceName: { eq: "docs" }
      relativePath: { eq: $originPath }
    ) {
      childMarkdownRemark {
        fields {
          slug
        }
        frontmatter {
          title
        }
        html
        ...ArticleTableOfContent
      }
    }
    ...ArticleComments
    ...DocPageCommunity
    ...DocPageNavigation
  }
`;

const Container = styled.div`
  display: flex;
  flex-direction: row;
  width: 100%;
  max-width: 800px;

  @media only screen and (min-width: 1050px) {
    max-width: 1050px;
  }

  @media only screen and (min-width: 1300px) {
    max-width: 1300px;
  }
`;
