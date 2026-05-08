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
  /* Site-wide palette aligned with the landing page (DesktopLandingRoot):
     deep navy background, warm off-white ink, faint cream borders. The
     primary button is a cream pill with dark text (matches .cc-btn-primary). */
  --cc-backdrop-color: rgba(12, 19, 34, 0.86);
  --cc-background-color: #0c1322;
  --cc-background-alt-color: rgba(255, 255, 255, 0.04);
  --cc-background-menu-color: rgba(12, 19, 34, 0.7);
  --cc-background-submenu-color: #09101c;
  --cc-shadow-color: rgba(0, 0, 0, 0.6);
  --cc-primary-color: #f5f1ea;
  --cc-menu-link-color: rgba(245, 241, 234, 0.62);
  --cc-menu-link-hover-color: #f5f1ea;
  --cc-primary-button-color: #f5f1ea;
  --cc-primary-button-border-color: transparent;
  --cc-primary-button-text-color: #0c1322;
  --cc-primary-button-hover-color: #ffffff;
  --cc-primary-button-hover-border-color: transparent;
  --cc-primary-button-hover-text-color: #0c1322;
  --cc-primary-text-button-color: rgba(245, 241, 234, 0.62);
  --cc-primary-text-button-hover-color: #f5f1ea;
  --cc-secondary-color: rgba(245, 241, 234, 0.62);
  --cc-tertiary-color: rgba(245, 241, 234, 0.4);
  --cc-quaternary-color: rgba(245, 241, 234, 0.16);
  --cc-link-color: #f5f1ea;
  --cc-link-hover-color: #ffffff;
  --cc-text-color: rgba(245, 241, 234, 0.78);
  --cc-text-alt-color: rgba(245, 241, 234, 0.5);
  --cc-heading-text-color: #f5f1ea;
  --cc-footer-text-color: rgba(245, 241, 234, 0.62);
  --cc-footer-link-color: rgba(245, 241, 234, 0.62);
  --cc-footer-link-hover-color: #f5f1ea;
  --cc-text-contrast-color: #f5f1ea;
  --cc-box-highlight-color: rgba(255, 255, 255, 0.04);
  --cc-box-border-color: rgba(245, 241, 234, 0.16);
  --cc-warning-color: #ffba0066;
  --cc-spinner-color: #f5f1ea;
`;

export const FONT_FAMILY =
  'system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol"';

export const FONT_FAMILY_CODE =
  'Consolas, Monaco, "Andale Mono", "Ubuntu Mono", monospace';

export const FONT_FAMILY_HEADING = `var(--font-radio-canada), ${FONT_FAMILY}`;

export const MAX_CONTENT_WIDTH = 1200;
