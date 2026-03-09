import React, { FC } from "react";
import { LinkedinShareButton, TwitterShareButton } from "react-share";
import styled from "styled-components";

import { Icon } from "@/components/sprites";
import { siteMetadata } from "@/lib/site-config";

// Icons
import LinkedInIconSvg from "@/images/icons/linkedin-square.svg";
import XIconSvg from "@/images/icons/x-square.svg";

interface BlogArticleSharebarData {
  mdx?: {
    frontmatter?: {
      path?: string;
      tags?: Array<string | null>;
      title?: string;
    };
  };
}

export interface BlogArticleSharebarProps {
  readonly data: BlogArticleSharebarData;
  readonly tags: string[];
}

export const BlogArticleSharebar: FC<BlogArticleSharebarProps> = ({
  data: { mdx },
  tags,
}) => {
  const { frontmatter } = mdx || {};
  const articelUrl = siteMetadata.siteUrl + (frontmatter?.path || "");
  const title = frontmatter?.title || "";

  return (
    <ShareButtons>
      <TwitterShareButton
        url={articelUrl}
        title={title}
        via={siteMetadata.author}
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
