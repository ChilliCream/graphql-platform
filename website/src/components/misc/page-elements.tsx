import styled, { css } from "styled-components";

import { IsMobile, IsSmallDesktop, THEME_COLORS } from "@/shared-style";

export const Intro = styled.header`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  padding: 25px 0;
  width: 100%;
  background-color: ${THEME_COLORS.primary};
  background: linear-gradient(
    180deg,
    ${THEME_COLORS.primary} 0%,
    #3d5f9f 100%
  ); //before: ff892a

  @media only screen and (min-width: 992px) {
    padding: 60px 0;
  }
`;

export const Title = styled.h3`
  flex: 0 0 auto;
  font-size: 1em;
  text-align: center;
  color: ${THEME_COLORS.quaternary};
`;

export const Hero = styled.h1`
  flex: 0 0 auto;
  margin-bottom: 20px;
  max-width: 800px;
  font-size: 2.222em;
  text-align: center;
  color: ${THEME_COLORS.textContrast};

  ${IsMobile(css`
    padding: 0 15px;
  `)}

  ${IsSmallDesktop(css`
    padding: 0 40px;
  `)}
`;

export const Teaser = styled.p`
  flex: 0 0 auto;
  max-width: 800px;
  font-size: 1.222em;
  text-align: center;
  color: ${THEME_COLORS.quaternary};

  ${IsMobile(css`
    padding: 0 15px;
  `)}

  ${IsSmallDesktop(css`
    padding: 0 40px;
  `)}
`;
