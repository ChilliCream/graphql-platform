import styled, { createGlobalStyle } from "styled-components";
import { IsMobile, IsTablet } from "../../shared-style";

export const MostProminentSection = styled.div``;

export const DocPageStickySideBarStyle = `
  max-height: 90vh;
  display: block;
  align-self: start;
  position: sticky;
  top: 0;
  overflow-y: auto;
  z-index: 25;

  ${IsTablet(`
    max-height: none;
    width: 350px;
  `)}

  ${IsMobile(`
    width: 100%;
  `)}
`;

export const BodyStyle = createGlobalStyle<{ disableScrolling: boolean }>`
  body {
    overflow-y: ${({ disableScrolling }) =>
      disableScrolling ? "hidden" : "initial"};

    @media only screen and (min-width: 600px) {
      overflow-y: initial;
    }
  }
`;
