import styled, { css } from "styled-components";

import {
  IsDesktop,
  IsMobile,
  IsPhablet,
  IsSmallDesktop,
  IsTablet,
  THEME_COLORS,
} from "@/style";

const SIDEBAR_WIDTH_DESKTOP = 300;
const HEADER_HEIGHT = 72;

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

  ${IsDesktop(`
    display: block;
    padding: 0 ${SIDEBAR_WIDTH_DESKTOP}px;
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
    top: ${HEADER_HEIGHT}px;
    background-color: ${THEME_COLORS.background};
    position: fixed;
    left: 0;
    bottom: 0;
    width: ${SIDEBAR_WIDTH_DESKTOP}px;
    max-height: none;
    height: calc(100vh - ${HEADER_HEIGHT}px);
    box-sizing: border-box;
    padding: 32px 16px 32px 32px;
    overflow-y: auto;
    z-index: 20;
    border-right: 1px solid ${THEME_COLORS.boxBorder};
  `)}
`;

export const ArticleWrapper = styled.div`
  display: flex;
  grid-row: 1;
  grid-column: 3 / 4;
  min-width: 0;
  overflow: visible;

  ${IsSmallDesktop(`
    grid-column: 2;
  `)}

  ${IsTablet(`
    grid-column: 1;
  `)}

  ${IsDesktop(`
    display: block;
    grid-column: auto;
  `)}
`;

export const ArticleContainer = styled.div`
  padding: 20px 40px 0;
  min-width: 0;
  overflow: visible;

  ${IsPhablet(`
    width: 100%;
    padding: 0;
  `)}

  ${IsDesktop(`
    padding: 32px 56px 0;
    display: flex;
    justify-content: center;
  `)}
`;

export const Article = styled.article`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  overflow: visible;
  background-color: ${THEME_COLORS.background};

  @media only screen and (min-width: 700px) {
    max-width: 820px;
  }

  ${IsDesktop(`
    width: 100%;
    max-width: 880px;
    margin: 0 auto;
  `)}
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
    top: ${HEADER_HEIGHT}px;
    background-color: ${THEME_COLORS.background};
    position: fixed;
    right: 0;
    bottom: 0;
    width: ${SIDEBAR_WIDTH_DESKTOP}px;
    max-height: none;
    height: calc(100vh - ${HEADER_HEIGHT}px);
    box-sizing: border-box;
    padding: 32px 32px 32px 16px;
    overflow-y: auto;
    z-index: 20;
    border-left: 1px solid ${THEME_COLORS.boxBorder};
`)}
`;
