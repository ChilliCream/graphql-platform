import React, { FC } from "react";
import styled from "styled-components";

import { IconContainer } from "@/components/misc/icon-container";
import { Link } from "@/components/misc/link";
import { Icon } from "@/components/sprites";
import { siteMetadata } from "@/lib/site-config";
import { THEME_COLORS } from "@/style";

// Icons
import GitHubIconSvg from "@/images/icons/github.svg";
import SlackIconSvg from "@/images/icons/slack.svg";

export interface DocArticleCommunityProps {
  readonly originPath: string;
}

export const DocArticleCommunity: FC<DocArticleCommunityProps> = ({
  originPath,
}) => {
  const docPath = `${siteMetadata.repositoryUrl}/blob/main/website/src/docs/${originPath}`;

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
          <CommunityLink to={siteMetadata.tools.slack}>
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

const Container = styled.section.attrs({
  className: "text-3",
})`
  margin-bottom: 24px;
`;

const Title = styled.h2`
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
