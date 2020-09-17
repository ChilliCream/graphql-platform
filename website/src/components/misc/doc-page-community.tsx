import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { DocPageCommunityFragment } from "../../../graphql-types";
import { IconContainer } from "./icon-container";
import { Link } from "./link";

import GitHubIconSvg from "../../images/github.svg";
import SlackIconSvg from "../../images/slack.svg";

interface DocPageCommunityProperties {
  data: DocPageCommunityFragment;
  originPath: string;
}

export const DocPageCommunity: FunctionComponent<DocPageCommunityProperties> = ({
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
            <IconContainer>
              <GitHubIconSvg />
            </IconContainer>
            Edit on GitHub
          </CommunityLink>
        </CommunityItem>
        <CommunityItem>
          <CommunityLink to={metadata.tools!.slack!}>
            <IconContainer>
              <SlackIconSvg />
            </IconContainer>
            Discuss on Slack
          </CommunityLink>
        </CommunityItem>
      </CommunityItems>
    </Container>
  );
};

export const DocPageCommunityGraphQLFragment = graphql`
  fragment DocPageCommunity on Query {
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

const Container = styled.section`
  margin-bottom: 20px;
`;

const Title = styled.h6`
  padding: 0 25px;
  font-size: 0.833em;

  @media only screen and (min-width: 1320px) {
    padding: 0 20px;
  }
`;

const CommunityItems = styled.ol`
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0 25px 10px;
  list-style-type: none;

  @media only screen and (min-width: 1320px) {
    padding: 0 20px 10px;
  }
`;

const CommunityItem = styled.li`
  flex: 0 0 auto;
  margin: 5px 0;
  padding: 0;
  line-height: initial;
`;

const CommunityLink = styled(Link)`
  font-size: 0.833em;
  color: #666;

  > ${IconContainer} {
    margin-right: 10px;

    > svg {
      fill: #666;
    }
  }

  :hover {
    color: #000;

    > ${IconContainer} > svg {
      fill: #000;
    }
  }
`;
