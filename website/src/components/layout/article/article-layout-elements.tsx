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
  grid-template-columns: 300px minmax(0, 1fr) 300px;

  ${IsSmallDesktop(`
    grid-template-columns: 300px minmax(0, 1fr);
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
  box-sizing: border-box;
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
  grid-column: 1;
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

  ${IsDesktop(`
    position: fixed;
    top: 72px;
    left: 0;
    width: 300px;
    height: calc(100vh - 72px);
    max-height: none;
    padding: 32px 24px 32px 32px;
    overflow-y: auto;
    background-color: ${THEME_COLORS.background};
  `)}
`;

export const ArticleWrapper = styled.div`
  display: flex;
  justify-content: center;
  grid-row: 1;
  grid-column: 2;
  min-width: 0;
  overflow: visible;

  ${IsTablet(`
    grid-column: 1;
  `)}
`;

export const ArticleContainer = styled.div`
  width: 100%;
  max-width: 920px;
  padding: 20px 48px 0;
  box-sizing: border-box;
  min-width: 0;
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
`;

export const FooterSlot = styled.div`
  grid-row: 2;
  grid-column: 2;
  min-width: 0;

  ${IsTablet(`
    grid-column: 1;
  `)}
`;

export interface AsideProps {
  readonly height: string;
  readonly show: boolean;
}

export const Aside = styled.aside<AsideProps>`
  ${DocArticleStickySideBarStyle}
  grid-row: 1;
  grid-column: 3;
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
    position: fixed;
    top: 72px;
    right: 0;
    width: 300px;
    height: calc(100vh - 72px);
    max-height: none;
    padding: 32px 32px 32px 24px;
    overflow-y: auto;
    background-color: ${THEME_COLORS.background};
  `)}
`;
