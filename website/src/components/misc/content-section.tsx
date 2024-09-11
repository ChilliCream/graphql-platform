import React, { FC, ReactNode } from "react";
import styled, { css } from "styled-components";

import { MAX_CONTENT_WIDTH } from "@/style";

export type ImagePosition = "auto" | "bottom";

export interface ContentSectionProps {
  readonly title?: ReactNode;
  readonly text?: ReactNode;
  readonly children?: ReactNode;
  readonly image?: ReactNode;
  readonly imageWidth?: number;
  readonly noBackground?: boolean;
  readonly titleSpace?: Space;
  readonly imagePosition?: ImagePosition;
}

export const ContentSection: FC<ContentSectionProps> = ({
  title,
  text,
  children,
  image,
  noBackground,
  imageWidth = 0,
  titleSpace = "small",
  imagePosition = "auto",
}) => {
  return (
    <Container $imagePosition={imagePosition} $imageWidth={imageWidth}>
      <VisibleArea>
        <Content $imagePosition={imagePosition} $noImage={!image}>
          {title && (
            <Title
              $imagePosition={imagePosition}
              $noImage={!image}
              $space={titleSpace}
            >
              {title}
            </Title>
          )}
          {text && (
            <Text $imagePosition={imagePosition} $noImage={!image}>
              {text}
            </Text>
          )}
          {children}
        </Content>
        {image && (
          <Image $imagePosition={imagePosition} $imageWidth={imageWidth}>
            {image}
          </Image>
        )}
        {!noBackground ||
          (imagePosition === "bottom" && (
            <RadialGradient $imageWidth={imageWidth} />
          ))}
      </VisibleArea>
    </Container>
  );
};

export const ContentSectionElement = styled.section`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  width: 100%;
  padding: 60px 16px;

  @media only screen and (min-width: 992px) {
    padding-top: 120px;
    padding-bottom: 120px;
  }

  @media only screen and (min-width: 1246px) {
    padding-right: 0;
    padding-left: 0;
  }
`;

const Image = styled.div<{
  readonly $imagePosition: ImagePosition;
  readonly $imageWidth: number;
}>`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  width: 100%;

  @media only screen and (min-width: 992px) {
    flex: 0 0
      ${({ $imagePosition, $imageWidth }) =>
        $imagePosition === "bottom"
          ? "auto"
          : $imageWidth
          ? $imageWidth + "px"
          : "400px"};
    box-sizing: initial;
    padding: 0;
  }

  > * {
    width: 100%;
  }
`;

export type Space = "small" | "medium" | "large";

const Title = styled.h2<{
  readonly $imagePosition: ImagePosition;
  readonly $noImage: boolean;
  readonly $space: Space;
}>`
  flex: 0 0 auto;
  margin-bottom: 10px;
  text-align: center;

  @media only screen and (min-width: 768px) {
    margin-bottom: 20px;
  }

  @media only screen and (min-width: 992px) {
    ${({ $space }) => {
      switch ($space) {
        case "small":
          return css`
            margin-bottom: 32px;
          `;

        case "medium":
          return css`
            margin-bottom: 48px;
          `;

        case "large":
          return css`
            margin-bottom: 72px;
          `;
      }
    }}
    text-align: ${({ $imagePosition, $noImage }) =>
      $noImage || $imagePosition === "bottom" ? "center" : "initial"};
  }
`;

const Text = styled.p.attrs({
  className: "text-2",
})<{
  readonly $imagePosition: ImagePosition;
  readonly $noImage: boolean;
}>`
  flex: 0 0 auto;
  width: 80vw;

  ${({ $imagePosition, $noImage }) =>
    $noImage || $imagePosition === "bottom"
      ? css`
          margin-bottom: 48px;
        `
      : ""}

  @media only screen and (min-width: 992px) {
    width: 100%;
  }
`;

const Content = styled.div<{
  readonly $imagePosition: ImagePosition;
  readonly $noImage: boolean;
}>`
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  overflow: visible;

  > ${Text} {
    text-align: center;
  }

  @media only screen and (min-width: 992px) {
    flex: 1 1 auto;
    height: 100%;

    ${({ $imagePosition, $noImage }) =>
      $noImage || $imagePosition === "bottom"
        ? css`
            align-items: center;

            > ${Title} {
              text-align: center;
            }

            > ${Text} {
              max-width: 620px;
              text-align: center;
            }
          `
        : css`
            align-items: flex-start;

            > ${Title} {
              text-align: initial;
            }

            > ${Text} {
              text-align: initial;
            }
          `};
  }
`;

const RadialGradient = styled.div<{
  readonly $imageWidth: number;
}>`
  position: absolute;
  z-index: -1;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  display: none;
  max-width: ${({ $imageWidth }) =>
    $imageWidth ? MAX_CONTENT_WIDTH - ($imageWidth + 75) : 600}px;
  background-image: radial-gradient(circle, #bcddf624 0%, #0a072100 60%);

  @media only screen and (min-width: 992px) {
    display: initial;
  }
`;

const VisibleArea = styled.div`
  position: relative;
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  gap: 36px;
  max-width: ${MAX_CONTENT_WIDTH}px;
  perspective: 1px;
  overflow: visible;

  @media only screen and (min-width: 992px) {
    flex-direction: row-reverse;
    gap: 150px;
  }
`;

const Container = styled(ContentSectionElement)<{
  readonly $imageWidth: number;
  readonly $imagePosition: ImagePosition;
}>`
  @media only screen and (min-width: 992px) {
    ${({ $imagePosition, $imageWidth }) => {
      if ($imagePosition === "bottom") {
        return css`
          & > ${VisibleArea} {
            flex-direction: column;
            gap: 0;

            > ${RadialGradient} {
              bottom: ${$imageWidth ? $imageWidth + 75 : 0}px;
            }
          }
        `;
      }

      return css`
        &:nth-child(even) > ${VisibleArea} {
          flex-direction: row-reverse;

          > ${RadialGradient} {
            left: ${$imageWidth ? $imageWidth + 75 : 0}px;
          }
        }

        &:nth-child(odd) > ${VisibleArea} {
          flex-direction: row;

          > ${RadialGradient} {
            right: ${$imageWidth ? $imageWidth + 75 : 0}px;
          }
        }
      `;
    }}
  }
`;
