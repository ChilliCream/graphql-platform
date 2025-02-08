import styled, { css } from "styled-components";

import {
  IsDesktop,
  IsMobile,
  IsPhablet,
  IsSmallDesktop,
  IsTablet,
  THEME_COLORS,
} from "@/style";

export const LayoutContainer = styled.div`
  display: grid;
  grid-template-columns: 1fr 230px 740px 230px 1fr;

  ${IsSmallDesktop(`
    grid-template-columns: 230px 1fr;
    width: auto;
  `)}

  ${IsTablet(`
    grid-template-columns: 1fr;
  `)}

  grid-template-rows: 1fr;
  width: 100%;
  height: 100%;
  overflow: visible;
`;

const DocArticleStickySideBarStyle = css`
  position: sticky;
  top: 0;
  z-index: 25;
  display: flex;
  flex-direction: column;
  align-self: start;
  max-height: 90vh;
  padding: 25px 0 0;
  overflow-y: hidden;
  background-color: ${THEME_COLORS.background};

  ${IsTablet(`
    width: 350px;
    max-height: none;
  `)}

  ${IsMobile(`
    width: 100%;
  `)}
`;

export interface NavigationProps {
  readonly $height: string;
  readonly $show: boolean;
}

export const Navigation = styled.nav.attrs({
  className: "text-3",
})<NavigationProps>`
  ${DocArticleStickySideBarStyle}
  grid-row: 1;
  grid-column: 2;
  transition: margin-left 250ms;

  ${({ $show }) =>
    $show &&
    css`
      margin-left: 0 !important;
    `}

  ${({ $height }) =>
    IsTablet(`
      position: fixed;
      top: 60px;
      left: 0;
      margin-left: -100%;
      height: ${$height};
    `)}

  ${IsSmallDesktop(`
    grid-column: 1;
  `)}

  ${IsDesktop(`
    top: 72px;
    background-color: initial;
  `)}
`;

export const ArticleWrapper = styled.div`
  display: flex;
  grid-row: 1;
  grid-column: 3 / 4;
  overflow: visible;

  ${IsSmallDesktop(`
    grid-column: 2;
  `)}

  ${IsTablet(`
    grid-column: 1;
  `)}
`;

export const ArticleContainer = styled.div`
  padding: 20px 40px 0;
  overflow: visible;

  ${IsPhablet(`
    width: 100%;
    padding: 0;
  `)}
`;

export const Article = styled.article`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  overflow: visible;
  background-color: ${THEME_COLORS.background};
  box-shadow: 0 0 120px 120px ${THEME_COLORS.background};

  @media only screen and (min-width: 700px) {
    max-width: 660px;
  }
`;

export interface AsideProps {
  readonly height: string;
  readonly show: boolean;
}

export const Aside = styled.aside<AsideProps>`
  ${DocArticleStickySideBarStyle}
  grid-row: 1;
  grid-column: 4;
  margin-left: 0;
  transition: transform 250ms;

  ${IsPhablet(`
    grid-column: 1;
  `)}

  ${({ height, show }) =>
    IsSmallDesktop(`
      position: fixed;
      top: 72px;
      right: 0;
      height: ${height};
      transform: ${show ? "none" : "translateX(100%)"};
    `)}

  ${IsDesktop(`
    top: 72px;
    background-color: initial;
`)}
`;
