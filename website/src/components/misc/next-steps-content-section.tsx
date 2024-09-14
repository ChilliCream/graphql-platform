import React, { ReactElement, ReactNode } from "react";
import styled, { css } from "styled-components";

import { IconContainer, LinkButton, LinkTextButton } from "@/components/misc";
import { Icon } from "@/components/sprites";
import { MAX_CONTENT_WIDTH } from "@/style";

// Icons
import ArrowRightIconSvg from "@/images/icons/arrow-right.svg";

export interface NextStepsContentSectionProps {
  readonly title: string;
  readonly text: ReactNode;
  readonly primaryLink: string;
  readonly primaryLinkText: string;
  readonly secondaryLink?: string;
  readonly secondaryLinkText?: string;
  readonly dense?: boolean;
}

export function NextStepsContentSection({
  title,
  text,
  primaryLink,
  primaryLinkText,
  secondaryLink,
  secondaryLinkText,
  dense,
}: NextStepsContentSectionProps): ReactElement {
  return (
    <ContentSection>
      <VisibleArea dense={dense}>
        <Content>
          <Title>{title}</Title>
          <Text>{text}</Text>
        </Content>
        <Actions>
          <LinkButton to={primaryLink}>{primaryLinkText}</LinkButton>
          {secondaryLink && secondaryLinkText && (
            <LinkTextButton to={secondaryLink}>
              {secondaryLinkText}
              <IconContainer $size={16}>
                <Icon {...ArrowRightIconSvg} />
              </IconContainer>
            </LinkTextButton>
          )}
        </Actions>
        {!dense && <RadialGradient />}
      </VisibleArea>
    </ContentSection>
  );
}

const ContentSection = styled.section.attrs({
  className: "animate",
})`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  width: 100%;
  overflow: visible;
  padding-right: 16px;
  padding-left: 16px;

  &.play .play-me {
    animation-play-state: running;
  }

  @media only screen and (min-width: 1246px) {
    padding-right: 0;
    padding-left: 0;
  }
`;

const VisibleArea = styled.div<{
  readonly dense?: boolean;
}>`
  position: relative;
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  justify-content: center;
  width: 100%;
  height: 100%;
  max-width: ${MAX_CONTENT_WIDTH}px;
  gap: 52px;
  overflow: visible;
  perspective: 1px;

  ${({ dense }) =>
    dense
      ? ""
      : css`
          padding-top: 120px;
          padding-bottom: 120px;
        `}
`;

const Content = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  gap: 24px;
  overflow: visible;

  @media only screen and (min-width: 992px) {
    gap: 32px;
  }
`;

const Title = styled.h2`
  text-align: center;
`;

const Text = styled.p.attrs({
  className: "text-2",
})`
  width: 80vw;
  text-align: center;

  @media only screen and (min-width: 992px) {
    width: 800px;
  }
`;

const Actions = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: row;
  align-items: center;
  justify-content: center;
  gap: 24px;
`;

const RadialGradient = styled.div`
  position: absolute;
  z-index: -1;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  background-image: radial-gradient(ellipse, #bcddf624 0%, #0a072100 60%);
`;
