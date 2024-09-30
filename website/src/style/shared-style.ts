import { css, keyframes, SimpleInterpolation } from "styled-components";

/** Fix a problem with the interpolation parsing that invalidates the first rule passed. */
const fixForInterpolation = (style: SimpleInterpolation) => ";" + style;

export function IsMobile(innerStyle: SimpleInterpolation) {
  return css`
    @media only screen and (max-width: 450px) {
      ${fixForInterpolation(innerStyle)}
    }
  `;
}

export function IsPhablet(innerStyle: SimpleInterpolation) {
  return css`
    @media only screen and (max-width: 860px) {
      ${fixForInterpolation(innerStyle)}
    }
  `;
}

export function IsSmallTablet(innerStyle: SimpleInterpolation) {
  return css`
    @media only screen and (max-width: 992px) {
      ${fixForInterpolation(innerStyle)}
    }
  `;
}

export function IsTablet(innerStyle: SimpleInterpolation) {
  return css`
    @media only screen and (max-width: 1110px) {
      ${fixForInterpolation(innerStyle)}
    }
  `;
}

export function IsSmallDesktop(innerStyle: SimpleInterpolation) {
  return css`
    @media only screen and (max-width: 1280px) {
      ${fixForInterpolation(innerStyle)}
    }
  `;
}

export function IsDesktop(innerStyle: SimpleInterpolation) {
  return css`
    @media only screen and (min-width: 1281px) {
      ${fixForInterpolation(innerStyle)}
    }
  `;
}

export function ApplyBackdropBlur(
  blur: number,
  backgroundStyle: SimpleInterpolation
) {
  return css`
    // NOTE:
    // hack to apply backdrop-filter in chrome under all circumstances e.g.
    // nested elements where both an ancestor and a child uses a filter
    ::before {
      position: absolute;
      top: 0;
      right: 0;
      bottom: 0;
      left: 0;
      z-index: -1;
      backdrop-filter: blur(${blur}px);
      ${fixForInterpolation(backgroundStyle)}
      content: "";
    }
  `;
}

export const FADE_IN = keyframes`
  0% {
    opacity: 0;
  }
  100% {
    opacity: 1;
  }
`;

export const FULL_ROTATION = keyframes`
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
`;

export const SLIDE_DOWN = keyframes`
  0% {
    transform: translateY(-75px);
  }
  100% {
    transform: translateY(0);
  }
`;

export const SLIDE_UP = keyframes`
  0% {
    transform: translateY(75px);
  }
  100% {
    transform: translateY(0);
  }
`;

export const ZOOM_IN = keyframes`
  0% {
    transform: translateZ(-1px);
  }
  100% {
    transform: translateZ(0);
  }
`;

export const ZOOM_OUT = keyframes`
  0% {
    transform: translateZ(0.25px);
  }
  100% {
    transform: translateZ(0);
  }
`;

export const DocArticleDesktopGridColumns = css`
  grid-template-columns: 1fr 230px 740px 230px 1fr;
`;

export interface ThemeColors {
  readonly backdrop: string;
  readonly background: string;
  readonly backgroundAlt: string;
  readonly backgroundMenu: string;
  readonly backgroundSubmenu: string;
  readonly shadow: string;
  readonly primary: string;
  readonly menuLink: string;
  readonly menuLinkHover: string;
  readonly primaryButton: string;
  readonly primaryButtonBorder: string;
  readonly primaryButtonText: string;
  readonly primaryButtonHover: string;
  readonly primaryButtonBorderHover: string;
  readonly primaryButtonHoverText: string;
  readonly primaryTextButton: string;
  readonly primaryTextButtonHover: string;
  readonly secondary: string;
  readonly tertiary: string;
  readonly quaternary: string;
  readonly link: string;
  readonly linkHover: string;
  readonly text: string;
  readonly textAlt: string;
  readonly heading: string;
  readonly footerText: string;
  readonly footerLink: string;
  readonly footerLinkHover: string;
  readonly textContrast: string;
  readonly boxHighlight: string;
  readonly boxBorder: string;
  readonly warning: string;
  readonly spinner: string;
}

export const THEME_COLORS: ThemeColors = {
  backdrop: "var(--cc-backdrop-color)",
  background: "var(--cc-background-color)",
  backgroundAlt: "var(--cc-background-alt-color)",
  backgroundMenu: "var(--cc-background-menu-color)",
  backgroundSubmenu: "var(--cc-background-submenu-color)",
  shadow: "var(--cc-shadow-color)",
  primary: "var(--cc-primary-color)",
  menuLink: "var(--cc-menu-link-color)",
  menuLinkHover: "var(--cc-menu-link-hover-color)",
  primaryButton: "var(--cc-primary-button-color)",
  primaryButtonBorder: "var(--cc-primary-button-border-color)",
  primaryButtonText: "var(--cc-primary-button-text-color)",
  primaryButtonHover: "var(--cc-primary-button-hover-color)",
  primaryButtonBorderHover: "var(--cc-primary-button-border-color)",
  primaryButtonHoverText: "var(--cc-primary-button-hover-text-color)",
  primaryTextButton: "var(--cc-primary-text-button-color)",
  primaryTextButtonHover: "var(--cc-primary-text-button-hover-color)",
  secondary: "var(--cc-secondary-color)",
  tertiary: "var(--cc-tertiary-color)",
  quaternary: "var(--cc-quaternary-color)",
  link: "var(--cc-link-color)",
  linkHover: "var(--cc-link-hover-color)",
  text: "var(--cc-text-color)",
  textAlt: "var(--cc-text-alt-color)",
  heading: "var(--cc-heading-text-color)",
  footerText: "var(--cc-footer-text-color)",
  footerLink: "var(--cc-footer-link-color)",
  footerLinkHover: "var(--cc-footer-link-hover-color)",
  textContrast: "var(--cc-text-contrast-color)",
  boxHighlight: "var(--cc-box-highlight-color)",
  boxBorder: "var(--cc-box-border-color)",
  warning: "var(--cc-warning-color)",
  spinner: "var(--cc-spinner-color)",
};

export const DEFAULT_THEME_COLORS = css`
  --cc-backdrop-color: #0a0721db;
  --cc-background-color: #0a0721;
  --cc-background-alt-color: #270d4899;
  --cc-background-menu-color: #0a072199;
  --cc-background-submenu-color: #09061d;
  --cc-shadow-color: #0f1725;
  --cc-primary-color: #3b4f74; //before: f40010;
  --cc-menu-link-color: #ccc9e4;
  --cc-menu-link-hover-color: #fff;
  --cc-primary-button-color: #2493c2;
  --cc-primary-button-border-color: #123151;
  --cc-primary-button-text-color: #ddf1f9;
  --cc-primary-button-hover-color: #47a4cc;
  --cc-primary-button-hover-border-color: #123151;
  --cc-primary-button-hover-text-color: #ddf1f9;
  --cc-primary-text-button-color: #e29f83;
  --cc-primary-text-button-hover-color: #ffd5c3;
  --cc-secondary-color: #516083;
  --cc-tertiary-color: #7989ab;
  --cc-quaternary-color: #bfcef1;
  --cc-link-color: #e29f83;
  --cc-link-hover-color: #ffd5c3;
  --cc-text-color: #ccc9e4;
  --cc-text-alt-color: #b1a6b1;
  --cc-heading-text-color: #e9e7f4;
  --cc-footer-text-color: #ccc9e4;
  --cc-footer-link-color: #ccc9e4;
  --cc-footer-link-hover-color: #ffffff;
  --cc-text-contrast-color: #ccc9e4;
  --cc-box-highlight-color: var(--cc-background-alt-color);
  --cc-box-border-color: #ccc9e422;
  --cc-warning-color: #ffba0066;
  --cc-spinner-color: #3b4f74;
`;

export const FONT_FAMILY =
  'system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol"';

export const FONT_FAMILY_CODE =
  'Consolas, Monaco, "Andale Mono", "Ubuntu Mono", monospace';

export const FONT_FAMILY_HEADING = `"Radio Canada", ${FONT_FAMILY}`;

export const MAX_CONTENT_WIDTH = 1200;
