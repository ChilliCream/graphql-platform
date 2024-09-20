import React, { FC } from "react";
import styled from "styled-components";

import { ContentSectionElement } from "@/components/misc";
import { MAX_CONTENT_WIDTH, THEME_COLORS } from "@/style";

// Images
import CommunityIllustrationSvg from "@/images/startpage/community.svg";

export const CommunitySection: FC = () => {
  return (
    <ContentSection>
      <CommunityVisualization />
    </ContentSection>
  );
};

export const CommunityVisualization: FC = () => {
  return (
    <VisibleArea>
      <Graph>
        <GraphContent>
          <GraphTitle>A Growing Community</GraphTitle>
          <GraphText>
            Be part of our expanding community. A place for sharing, learning,
            <br />
            and evolving together, driving forward the future of GraphQL
            technologies.
          </GraphText>
        </GraphContent>
        <CommunityIllustration />
      </Graph>
      <Numbers>
        <Number>
          <NumberTitle>6k</NumberTitle>
          <NumberText>Slack Users</NumberText>
        </Number>
        <Number>
          <NumberTitle>465m</NumberTitle>
          <NumberText>Package Downloads</NumberText>
        </Number>
        <Number>
          <NumberTitle>5k</NumberTitle>
          <NumberText>GitHub Stars</NumberText>
        </Number>
        <Number>
          <NumberTitle>3.2k</NumberTitle>
          <NumberText>Pull Requests</NumberText>
        </Number>
      </Numbers>
    </VisibleArea>
  );
};

const ContentSection = styled(ContentSectionElement).attrs({
  className: "animate",
})`
  &.play .play-me {
    animation-play-state: running;
  }
`;

const VisibleArea = styled.div`
  position: relative;
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  width: 100%;
  height: 100%;
  max-width: ${MAX_CONTENT_WIDTH}px;
  gap: 16px;
  overflow: visible;

  @media only screen and (min-width: 992px) {
    gap: 24px;
  }
`;

const Graph = styled.div`
  position: relative;
  display: flex;
  flex: 1 1 568px;
  flex-direction: column;
  align-items: flex-start;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--box-border-radius);
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    to bottom,
    #e043da1d,
    #a43da91d,
    #6e32781d,
    #3f23481d,
    #18121a1d
  );
`;

const GraphContent = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: flex-start;
  width: 670px;
  padding: 40px;
`;

const GraphTitle = styled.h4`
  flex: 0 0 auto;

  @media only screen and (min-width: 992px) {
    margin-bottom: 16px;
  }
`;

const GraphText = styled.p.attrs({
  className: "text-2",
})`
  flex: 0 0 auto;

  @media only screen and (min-width: 992px) {
    margin-bottom: 40px;
  }
`;

const CommunityIllustration = styled(CommunityIllustrationSvg)`
  position: absolute;
  z-index: -1;
`;

const Numbers = styled.div`
  position: relative;
  display: flex;
  flex: 1 1 200px;
  flex-direction: rows;
  gap: 16px;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--box-border-radius);
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    to right top,
    #9b9aa51d,
    #7472811d,
    #4e4d5f1d,
    #2b2b3f1d,
    #0a07211d
  );

  @media only screen and (min-width: 992px) {
    gap: 24px;
  }
`;

const Number = styled.div`
  display: flex;
  flex: 1 1 282px;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
`;

const NumberTitle = styled.h2`
  flex: 0 0 auto;
`;

const NumberText = styled.p.attrs({
  className: "text-2",
})`
  flex: 0 0 auto;
`;
