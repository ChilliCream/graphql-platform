import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { LinkedinShareButton, TwitterShareButton } from "react-share";
import styled from "styled-components";
import { BlogArticleSharebarFragment } from "../../../graphql-types";

import LinkedinIconSvg from "../../images/linkedin-square.svg";
import TwitterIconSvg from "../../images/twitter-square.svg";

interface BlogArticleSharebarProperties {
  data: BlogArticleSharebarFragment;
  tags: string[];
}

export const BlogArticleSharebar: FunctionComponent<BlogArticleSharebarProperties> = ({
  data: { mdx, site },
  tags,
}) => {
  const { frontmatter } = mdx!;
  const articelUrl = site!.siteMetadata!.siteUrl! + frontmatter!.path!;
  const title = frontmatter!.title!;

  return (
    <ShareButtons>
      <TwitterShareButton
        url={articelUrl}
        title={title}
        via={site!.siteMetadata!.author!}
        hashtags={tags}
      >
        <TwitterIcon />
      </TwitterShareButton>
      <LinkedinShareButton url={articelUrl} title={title}>
        <LinkedinIcon />
      </LinkedinShareButton>
    </ShareButtons>
  );
};

export const BlogArticleSharebarGraphQLFragment = graphql`
  fragment BlogArticleSharebar on Query {
    mdx(frontmatter: { path: { eq: $path } }) {
      frontmatter {
        path
        tags
        title
      }
    }
    site {
      siteMetadata {
        author
        siteUrl
      }
    }
  }
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
