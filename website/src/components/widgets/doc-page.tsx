import { graphql } from "gatsby";
import { Disqus } from "gatsby-plugin-disqus";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { DocPageFragment } from "../../../graphql-types";
import { ArticleTitle } from "../misc/article-elements";
import { DocPageAside } from "../misc/doc-page-aside";
import { DocPageNavigation } from "../misc/doc-page-navigation";

interface DocPageProperties {
  data: DocPageFragment;
}

export const DocPage: FunctionComponent<DocPageProperties> = ({ data }) => {
  const { file, site } = data;
  const { fields, frontmatter, html } = file!.childMarkdownRemark!;
  const metadata = site!.siteMetadata!;
  const slug = fields!.slug!;
  const path = `/docs/${slug.substring(1)}`;
  const articelUrl = metadata.baseUrl! + path;
  const title = frontmatter!.title!;
  const disqusConfig = {
    url: articelUrl,
    identifier: path,
    title,
  };

  return (
    <Container>
      <DocPageNavigation data={data} />
      <Content>
        <Article>
          <ArticleTitle>{title}</ArticleTitle>
          <ArticleContent dangerouslySetInnerHTML={{ __html: html! }} />
        </Article>
        <DisqusWrapper config={disqusConfig} />
      </Content>
      <DocPageAside data={data} />
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
    site {
      siteMetadata {
        author
        baseUrl
      }
    }
    ...DocPageAside
    ...DocPageNavigation
  }
`;

const Container = styled.div`
  display: flex;
  flex-direction: row;
  width: 100%;
  max-width: 1400px;
`;

const Content = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
`;

const Article = styled.article`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  margin-bottom: 40px;
  padding-bottom: 20px;

  @media only screen and (min-width: 800px) {
    border: 1px solid #ccc;
    border-top: 0 none;
  }
`;

const ArticleContent = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;

  > * {
    padding-right: 20px;
    padding-left: 20px;
  }

  > h1 > a.anchor.before,
  > h2 > a.anchor.before,
  > h3 > a.anchor.before,
  > h4 > a.anchor.before,
  > h5 > a.anchor.before,
  > h6 > a.anchor.before {
    padding-right: 4px;
    transform: translateX(0px);
  }

  > table {
    margin-right: 20px;
    margin-left: 20px;
    padding-right: 0;
    padding-left: 0;
    width: calc(100% - 40px);
  }

  > .gatsby-highlight {
    padding-right: 0;
    padding-left: 0;

    > pre {
      padding: 30px 20px;
    }
  }

  @media only screen and (min-width: 800px) {
    > * {
      padding-right: 50px;
      padding-left: 50px;
    }

    > h1 > a.anchor.before,
    > h2 > a.anchor.before,
    > h3 > a.anchor.before,
    > h4 > a.anchor.before,
    > h5 > a.anchor.before,
    > h6 > a.anchor.before {
      transform: translateX(30px);
    }

    > table {
      margin-right: 50px;
      margin-left: 50px;
      padding-right: 0;
      padding-left: 0;
      width: calc(100% - 100px);
    }

    > .gatsby-highlight {
      > pre {
        padding-right: 50px;
        padding-left: 50px;
      }
    }
  }
`;

const DisqusWrapper = styled(Disqus)`
  margin: 0 20px;

  @media only screen and (min-width: 800px) {
    margin: 0 50px;
  }
`;
