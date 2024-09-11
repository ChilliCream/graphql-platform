import styled, { css } from "styled-components";

import {
  FADE_IN,
  IsMobile,
  MAX_CONTENT_WIDTH,
  SLIDE_DOWN,
  SLIDE_UP,
  ZOOM_OUT,
} from "@/style";
import { LinkButton } from "./button";

export const Hero = styled.header`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  padding-top: 64px;
  padding-bottom: 120px;
  width: 100%;
  perspective: 1px;

  @media only screen and (min-width: 992px) {
    padding-top: 148px;
    padding-bottom: 120px;
  }
`;

export const HeroTitleFirst = styled.h1`
  flex: 0 0 auto;
  max-width: ${MAX_CONTENT_WIDTH}px;
  text-align: center;
  animation: 0.5s ease-in-out ${FADE_IN} forwards,
    0.5s ease-in-out ${SLIDE_DOWN} forwards;
  opacity: 0;
  transform: translateY(-75px);

  ${IsMobile(css`
    padding-right: 16px;
    padding-left: 16px;
  `)}
`;

export const HeroTitleSecond = styled.h1.attrs({
  className: "prominent",
})`
  flex: 0 0 auto;
  max-width: ${MAX_CONTENT_WIDTH}px;
  text-align: center;
  animation: 0.25s ease-in-out 0.25s ${FADE_IN} forwards,
    0.25s ease-in-out 0.25s ${SLIDE_DOWN} forwards;
  opacity: 0;
  transform: translateY(-75px);

  ${IsMobile(css`
    padding-right: 16px;
    padding-left: 16px;
  `)}
`;

export const HeroTitleThird = styled.h2`
  flex: 0 0 auto;
  max-width: ${MAX_CONTENT_WIDTH}px;
  text-align: center;
  animation: 0.5s ease-in-out ${FADE_IN} forwards,
    0.5s ease-in-out ${SLIDE_DOWN} forwards;
  opacity: 0;
  transform: translateY(-75px);
`;

export const HeroTeaser = styled.p`
  flex: 0 0 auto;
  margin-top: 24px;
  width: 80vw;
  text-align: center;
  animation: 0.5s ease-in-out ${FADE_IN} forwards,
    0.5s ease-in-out ${SLIDE_UP} forwards;
  opacity: 0;
  transform: translateY(75px);

  @media only screen and (min-width: 992px) {
    margin-top: 32px;
    width: 800px;
  }
`;

export const HeroLink = styled(LinkButton)`
  margin-top: 20px;
  animation: 0.5s ease-in-out 0.1s ${FADE_IN} forwards,
    0.5s ease-in-out 0.1s ${ZOOM_OUT} forwards;
  opacity: 0;
  transform: translateZ(0.25px);

  @media only screen and (min-width: 992px) {
    margin-top: 58px;
  }
`;

export const HeroImageContainer = styled.div`
  margin-top: 140px;
`;
