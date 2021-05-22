import React, { FunctionComponent, useCallback } from "react";
import { useDispatch, useSelector } from "react-redux";
import { State } from "../../state";
import { toggleAside } from "../../state/common";
import { BodyStyle, DocPageStickySideBarStyle } from "./doc-page-elements";
import { DocPagePaneHeader } from "./doc-page-pane-header";
import styled from "styled-components";
import {
  BoxShadow,
  IsSmallDesktop,
  SmallDesktopBreakpointNumber,
} from "../../shared-style";

export const DocPageAside: FunctionComponent = ({ children }) => {
  const showAside = useSelector<State, boolean>(
    (state) => state.common.showAside
  );

  const height = useSelector<State, string>((state) => {
    return state.common.articleViewportHeight;
  });

  const dispatch = useDispatch();

  const handleCloseAside = useCallback(() => {
    dispatch(toggleAside());
  }, []);

  return (
    <Aside calculatedHeight={height} className={showAside ? "show" : ""}>
      <BodyStyle disableScrolling={showAside} />
      <DocPagePaneHeader
        title="About this article"
        showWhenScreenWidthIsSmallerThan={SmallDesktopBreakpointNumber}
        onClose={handleCloseAside}
      />
      {children}
    </Aside>
  );
};

export const Aside = styled.aside<{ calculatedHeight: string }>`
  ${DocPageStickySideBarStyle}

  height: 100%;
  margin-left: 0;
  transition: transform 250ms;
  background-color: white;
  padding: 25px 0 0;

  &.show {
    transform: none;
  }

  ${({ calculatedHeight }) =>
    IsSmallDesktop(`
      transform: translateX(100%);
      height: ${calculatedHeight};
      position: fixed;
      top: 60px;
      right: 0;
      ${BoxShadow}
    `)}
`;
