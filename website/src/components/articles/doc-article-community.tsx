import { graphql } from "gatsby";
import React, { FC } from "react";
import styled from "styled-components";

import { IconContainer } from "@/components/misc/icon-container";
import { Link } from "@/components/misc/link";
import { Icon } from "@/components/sprites";
import { DocArticleCommunityFragment } from "@/graphql-types";
import { THEME_COLORS } from "@/style";

// Icons
import GitHubIconSvg from "@/images/icons/github.svg";
import SlackIconSvg from "@/images/icons/slack.svg";

export interface DocArticleCommunityProps {
  readonly data: DocArticleCommunityFragment;
  readonly originPath: string;
}

export const DocArticleCommunity: FC<DocArticleCommunityProps> = ({
  data,
  originPath,
}) => {
  const metadata = data.site!.siteMetadata!;
  const docPath = `${metadata.repositoryUrl!}/blob/master/website/src/docs/${originPath}`;

  return (
    <Container>
      <Title>Help us improving our content</Title>
      <CommunityItems>
        <CommunityItem>
          <CommunityLink to={docPath}>
            <IconContainer $size={20}>
              <Icon {...GitHubIconSvg} />
            </IconContainer>
            Edit on GitHub
          </CommunityLink>
        </CommunityItem>
        <CommunityItem>
          <CommunityLink to={metadata.tools!.slack!}>
            <IconContainer $size={20}>
              <Icon {...SlackIconSvg} />
            </IconContainer>
            Discuss on Slack
          </CommunityLink>
        </CommunityItem>
      </CommunityItems>
    </Container>
  );
};

export const DocArticleCommunityGraphQLFragment = graphql`
  fragment DocArticleCommunity on Query {
    site {
      siteMetadata {
        repositoryUrl
        tools {
          slack
        }
      }
    }
  }
`;

const Container = styled.section.attrs({
  className: "text-3",
})`
  margin-bottom: 24px;
`;

const Title = styled.h6`
  margin-bottom: 12px;
  padding: 0 25px;
  font-size: 0.875rem;

  @media only screen and (min-width: 1320px) {
    padding: 0;
  }
`;

const CommunityItems = styled.ol`
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0 25px;
  list-style-type: none;

  @media only screen and (min-width: 1320px) {
    padding: 0;
  }
`;

const CommunityItem = styled.li`
  flex: 0 0 auto;
  margin: 5px 0;
  padding: 0;
`;

const CommunityLink = styled(Link)`
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 12px;
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;

  > ${IconContainer} > svg {
    fill: ${THEME_COLORS.text};
    transition: fill 0.2s ease-in-out;
  }

  :hover {
    color: ${THEME_COLORS.linkHover};

    > ${IconContainer} > svg {
      fill: ${THEME_COLORS.linkHover};
    }
  }
`;
