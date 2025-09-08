import { graphql } from "gatsby";
import React, { FC } from "react";
import { LinkedinShareButton, TwitterShareButton } from "react-share";
import styled from "styled-components";

import { Icon } from "@/components/sprites";
import { BlogArticleSharebarFragment } from "@/graphql-types";

// Icons
import LinkedInIconSvg from "@/images/icons/linkedin-square.svg";
import XIconSvg from "@/images/icons/x-square.svg";

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
        <XIcon />
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

const ShareButtons = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 16px;

  > button {
    flex: 0 0 auto;

    > svg {
      width: 24px;
    }
  }

  @media only screen and (min-width: 992px) {
    display: flex;
  }
`;

const XIcon = styled(Icon).attrs(XIconSvg)`
  fill: #ffffff;
`;

const LinkedinIcon = styled(Icon).attrs(LinkedInIconSvg)`
  fill: #ffffff;
`;
