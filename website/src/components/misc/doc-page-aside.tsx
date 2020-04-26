import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { DocPageAsideFragment } from "../../../graphql-types";
import { IconContainer } from "./icon-container";
import { Link } from "./link";

import GitHubIconSvg from "../../images/github.svg";
import SlackIconSvg from "../../images/slack.svg";

interface DocPageAsideProperties {
  data: DocPageAsideFragment;
  originPath: string;
}

export const DocPageAside: FunctionComponent<DocPageAsideProperties> = ({
  data,
  originPath,
}) => {
  const metadata = data.site!.siteMetadata!;
  const docPath = `${metadata.repositoryUrl!}/blob/master/website/src/docs/${originPath}`;

  return (
    <Aside>
      <FixedContainer>
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
      </FixedContainer>
    </Aside>
  );
};

export const DocPageAsideGraphQLFragment = graphql`
  fragment DocPageAside on Query {
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

const Aside = styled.aside`
  display: none;
  flex: 0 0 250px;
  flex-direction: column;

  * {
    user-select: none;
  }

  @media only screen and (min-width: 1300px) {
    display: flex;
  }
`;

const FixedContainer = styled.div`
  position: fixed;
  padding: 25px 0 250px;
  width: 250px;
`;

const Title = styled.h6`
  padding: 0 20px 10px;
  font-size: 0.833em;
`;

const CommunityItems = styled.ol`
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0 20px 20px;
  list-style-type: none;
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
