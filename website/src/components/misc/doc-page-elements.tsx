import styled, { createGlobalStyle, css } from "styled-components";

const PaneBase = css`
  position: fixed;
  display: flex;
  flex-direction: column;

  * {
    user-select: none;
  }
`;

const FullSize = css`
  position: fixed;
  display: initial;
  padding: 40px 0;
  width: 250px;
  height: initial;
  background-color: initial;
  opacity: initial;
  box-shadow: initial;
`;

export const FixedContainer = styled.div`
  position: absolute;
  display: none;
  padding: 25px 0 0;
  width: 100vw;
  height: calc(100vh - 85px);
  overflow-y: initial;
  background-color: white;
  opacity: 0;
  transition: opacity 2s ease-in-out;

  &.show {
    display: initial;
    opacity: initial;
  }

  @media only screen and (min-width: 600px) {
    width: 450px;
    box-shadow: 0px 3px 6px 0px rgba(0, 0, 0, 0.25);
  }
`;

export const Aside = styled.aside`
  ${PaneBase}
  z-index: 2;
  right: 0;

  > ${FixedContainer} {
    right: 0;
  }

  @media only screen and (min-width: 1320px) {
    position: relative;
    right: initial;
    flex: 0 0 250px;

    > ${FixedContainer} {
      ${FullSize};
      right: initial;
    }
  }
`;

export const Navigation = styled.nav`
  ${PaneBase}
  z-index: 3;
  left: 0;

  > ${FixedContainer} {
    left: 0;
  }

  @media only screen and (min-width: 1070px) {
    position: relative;
    left: initial;
    flex: 0 0 250px;

    > ${FixedContainer} {
      ${FullSize};
      left: initial;
    }
  }
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
