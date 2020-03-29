import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { DocPageFragment } from "../../../graphql-types";
import { ArticleComments } from "../misc/article-comments";
import {
  Article,
  ArticleContent,
  ArticleTitle,
  ArticleWrapper,
} from "../misc/article-elements";
import { DocPageAside } from "../misc/doc-page-aside";
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
  const path = `/docs/${fields!.slug!.substring(1)}`;
  const title = frontmatter!.title!;

  return (
    <Container>
      <DocPageNavigation data={data} />
      <ArticleWrapper>
        <Article>
          <ArticleTitle>{title}</ArticleTitle>
          <ArticleContent dangerouslySetInnerHTML={{ __html: html! }} />
        </Article>
        <ArticleComments data={data} path={path} title={title} />
      </ArticleWrapper>
      <DocPageAside data={data} originPath={originPath} />
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
      }
    }
    ...ArticleComments
    ...DocPageAside
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
