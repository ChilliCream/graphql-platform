import { graphql } from "gatsby";
import { Disqus } from "gatsby-plugin-disqus";
import React, { FunctionComponent } from "react";
import { LinkedinShareButton, TwitterShareButton } from "react-share";
import styled from "styled-components";
import { DocPageFragment } from "../../../graphql-types";
import { ArticleTitle } from "../misc/blog-article-elements";

import LinkedinIconSvg from "../../images/linkedin-square.svg";
import TwitterIconSvg from "../../images/twitter-square.svg";

interface DocPageProperties {
  data: DocPageFragment;
}

export const DocPage: FunctionComponent<DocPageProperties> = ({
  data: { markdownRemark, site },
}) => {
  const { frontmatter, html } = markdownRemark!;
  const path = frontmatter!.path!;
  const articelUrl = site!.siteMetadata!.baseUrl! + path;
  const title = frontmatter!.title!;
  const disqusConfig = {
    url: articelUrl,
    identifier: path,
    title,
  };

  return (
    <Container>
      <ShareButtons>
        <TwitterShareButton
          url={articelUrl}
          title={title}
          via={site!.siteMetadata!.author!}
        >
          <TwitterIcon />
        </TwitterShareButton>
        <LinkedinShareButton url={articelUrl} title={title}>
          <LinkedinIcon />
        </LinkedinShareButton>
      </ShareButtons>
      <BlogContent>
        <Article>
          <ArticleTitle>{title}</ArticleTitle>
          <Content dangerouslySetInnerHTML={{ __html: html! }} />
        </Article>
        <DisqusWrapper config={disqusConfig} />
      </BlogContent>
    </Container>
  );
};

export const DocPageGraphQLFragment = graphql`
  fragment DocPage on Query {
    markdownRemark(frontmatter: { path: { eq: $path } }) {
      frontmatter {
        path
        title
      }
      html
    }
    site {
      siteMetadata {
        author
        baseUrl
      }
    }
  }
`;

const Container = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  width: 100%;
  max-width: 800px;
`;

const ShareButtons = styled.aside`
  position: fixed;
  left: calc(50% - 480px);
  display: none;
  flex-direction: column;
  padding: 150px 0 250px;
  width: 60px;

  > button {
    flex: 0 0 50px;

    > svg {
      width: 30px;
    }
  }

  @media only screen and (min-width: 992px) {
    display: flex;
  }
`;

const TwitterIcon = styled(TwitterIconSvg)`
  fill: #1da0f2;
`;

const LinkedinIcon = styled(LinkedinIconSvg)`
  fill: #0073b0;
`;

const BlogContent = styled.div`
  display: flex;
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

const Content = styled.div`
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
