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
