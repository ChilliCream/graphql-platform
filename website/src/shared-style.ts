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
  readonly primary: string;
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
  primary: "var(--primary-color)",
  secondary: "var(--secondary-color)",
  tertiary: "var(--tertiary-color)",
  quaternary: "var(--quaternary-color)",
  link: "var(--link-color)",
  text: "var(--text-color)",
  heading: "var(--heading-color)",
  footerText: "var(--footer-text-color)",
  textContrast: "var(--text-contrast-color)",
  boxHighlight: "var(--box-highlight-color)",
  boxBorder: "var(--box-border-color)",
  warning: "var(--warning-color)",
};

export const DEFAULT_THEME_COLORS = css`
  --primary-color: #3b4f74; //before: f40010;
  --secondary-color: #516083;
  --tertiary-color: #7989ab;
  --quaternary-color: #bfcef1;
  --link-color: #f4125b;
  --text-color: #667;
  --heading-text-color: #3b4f74;
  --footer-text-color: #c6c6ce;
  --text-contrast-color: #fff;
  --box-highlight-color: #e8ecf5;
  --box-border-color: #bfcef1;
  --warning-color: #ffba00;
`;

export const FONT_FAMILY =
  'system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol"';

export const FONT_FAMILY_CODE =
  'Consolas, Monaco, "Andale Mono", "Ubuntu Mono", monospace';

export const FONT_FAMILY_HEADING = `"Roboto", ${FONT_FAMILY}`;
