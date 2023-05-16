import React, { FC, ReactNode, useCallback } from "react";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";

import { BoxShadow, IsSmallDesktop } from "@/shared-style";
import { State } from "@/state";
import { toggleAside } from "@/state/common";
import { BasicPagePaneHeader } from "./basic-page-pane-header";
import { BasicPageStickySideBarStyle } from "./basic-page-styles";

export interface BasicPageAsideProps {
  readonly children: ReactNode;
}

export const BasicPageAside: FC<BasicPageAsideProps> = ({ children }) => {
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
      <BasicPagePaneHeader
        title="About this article"
        showWhenScreenWidthIsSmallerThan={1280}
        onClose={handleCloseAside}
      />
      {children}
    </Aside>
  );
};

export interface AsideProps {
  readonly height: string;
  readonly show: boolean;
}

export const Aside = styled.aside<AsideProps>`
  ${BasicPageStickySideBarStyle}

  margin-left: 0;
  transition: transform 250ms;
  background-color: white;
  padding: 25px 0 0;
  overflow-y: hidden;
  margin-bottom: 50px;
  display: flex;
  flex-direction: column;

  ${({ height, show }) =>
    IsSmallDesktop(`
      transform: ${show ? "none" : "translateX(100%)"};
      height: ${height};
      position: fixed;
      top: 60px;
      right: 0;

      ${BoxShadow}
    `)}
`;
