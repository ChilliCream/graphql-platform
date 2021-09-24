import React, { FC, useCallback } from "react";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";
import {
  BoxShadow,
  IsSmallDesktop,
  SmallDesktopBreakpointNumber,
} from "../../shared-style";
import { State } from "../../state";
import { toggleAside } from "../../state/common";
import { BodyStyle, DocPageStickySideBarStyle } from "./doc-page-elements";
import { DocPagePaneHeader } from "./doc-page-pane-header";

export const DocPageAside: FC = ({ children }) => {
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
    <Aside height={height} show={showAside}>
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

export const Aside = styled.aside<{ height: string; show: boolean }>`
  ${DocPageStickySideBarStyle}

  margin-left: 0;
  transition: transform 250ms;
  background-color: white;
  padding: 25px 0 0;
  overflow-y: hidden;
  margin-bottom: 50px;
  display: flex;
  flex-direction: column;

  ${(show) => show && `transform: none;`}

  ${({ height }) =>
    IsSmallDesktop(`
      transform: translateX(100%);
      height: ${height};
      position: fixed;
      top: 60px;
      right: 0;
      ${BoxShadow}
    `)}
`;
