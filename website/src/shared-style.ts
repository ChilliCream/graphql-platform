import { css, SimpleInterpolation } from "styled-components";

export function IsMobile(innerStyle: SimpleInterpolation) {
  return css`
    @media only screen and (max-width: 450px) {
      ${innerStyle}
    }
  `;
}

export function IsPhablet(innerStyle: SimpleInterpolation) {
  return css`
    @media only screen and (max-width: 860px) {
      ${innerStyle}
    }
  `;
}

export function IsTablet(innerStyle: SimpleInterpolation) {
  return css`
    @media only screen and (max-width: 1110px) {
      ${innerStyle}
    }
  `;
}

export function IsSmallDesktop(innerStyle: SimpleInterpolation) {
  return css`
    @media only screen and (max-width: 1280px) {
      ${innerStyle}
    }
  `;
}

export function IsDesktop(innerStyle: SimpleInterpolation) {
  return css`
    @media only screen and (min-width: 1281px) {
      ${innerStyle}
    }
  `;
}

export const DocPageDesktopGridColumns = css`
  grid-template-columns: 1fr 250px 820px 250px 1fr;
`;

export const BoxShadow = css`
  box-shadow: rgba(0, 0, 0, 0.25) 0px 3px 6px 0px;
`;

export interface ThemeColors {
  readonly backdrop: string;
  readonly background: string;
  readonly shadow: string;
  readonly primary: string;
  readonly primaryButton: string;
  readonly primaryButtonText: string;
  readonly primaryButtonHover: string;
  readonly primaryButtonHoverText: string;
  readonly secondary: string;
  readonly tertiary: string;
  readonly quaternary: string;
  readonly link: string;
  readonly text: string;
  readonly heading: string;
  readonly footerText: string;
  readonly textContrast: string;
  readonly boxHighlight: string;
  readonly boxBorder: string;
  readonly warning: string;
}

export const THEME_COLORS: ThemeColors = {
  backdrop: "var(--cc-backdrop-color)",
  background: "var(--cc-background-color)",
  shadow: "var(--cc-shadow-color)",
  primary: "var(--cc-primary-color)",
  primaryButton: "var(--cc-primary-button-color)",
  primaryButtonText: "var(--cc-primary-button-text-color)",
  primaryButtonHover: "var(--cc-primary-button-hover-color)",
  primaryButtonHoverText: "var(--cc-primary-button-hover-text-color)",
  secondary: "var(--cc-secondary-color)",
  tertiary: "var(--cc-tertiary-color)",
  quaternary: "var(--cc-quaternary-color)",
  link: "var(--cc-link-color)",
  text: "var(--cc-text-color)",
  heading: "var(--cc-heading-text-color)",
  footerText: "var(--cc-footer-text-color)",
  textContrast: "var(--cc-text-contrast-color)",
  boxHighlight: "var(--cc-box-highlight-color)",
  boxBorder: "var(--cc-box-border-color)",
  warning: "var(--cc-warning-color)",
};

export const DEFAULT_THEME_COLORS = css`
  --cc-backdrop-color: #cbd0db16;
  --cc-background-color: #ffffff;
  --cc-shadow-color: #0f1725;
  --cc-primary-color: #3b4f74; //before: f40010;
  --cc-primary-button-color: #cb1974;
  --cc-primary-button-text-color: #ffffff;
  --cc-primary-button-hover-color: #b10e61;
  --cc-primary-button-hover-text-color: #ffffff;
  --cc-secondary-color: #516083;
  --cc-tertiary-color: #7989ab;
  --cc-quaternary-color: #bfcef1;
  --cc-link-color: #f4125b;
  --cc-text-color: #667;
  --cc-heading-text-color: #3b4f74;
  --cc-footer-text-color: #c6c6ce;
  --cc-text-contrast-color: #fff;
  --cc-box-highlight-color: #e8ecf5;
  --cc-box-border-color: #bfcef1;
  --cc-warning-color: #ffba00;
`;

export const FONT_FAMILY =
  'system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol"';

export const FONT_FAMILY_CODE =
  'Consolas, Monaco, "Andale Mono", "Ubuntu Mono", monospace';

export const FONT_FAMILY_HEADING = "Roboto, ${FONT_FAMILY}";
