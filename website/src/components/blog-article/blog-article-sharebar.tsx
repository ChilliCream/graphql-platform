import { graphql } from "gatsby";
import React, { FC } from "react";
import { LinkedinShareButton, TwitterShareButton } from "react-share";
import styled from "styled-components";

import { Brand } from "@/components/sprites";
import { BlogArticleSharebarFragment } from "@/graphql-types";

// Brands
import LinkedInIconSvg from "@/images/brands/linkedin-square.svg";
import TwitterIconSvg from "@/images/brands/twitter-square.svg";

export interface BlogArticleSharebarProps {
  readonly data: BlogArticleSharebarFragment;
  readonly tags: string[];
}

export const BlogArticleSharebar: FC<BlogArticleSharebarProps> = ({
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

const TwitterIcon = styled(Brand).attrs(TwitterIconSvg)`
  fill: #1da0f2;
`;

const LinkedinIcon = styled(Brand).attrs(LinkedInIconSvg)`
  fill: #0073b0;
`;
