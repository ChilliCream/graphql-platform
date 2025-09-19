import React, { FC, ReactNode, useCallback } from "react";
import { useDispatch, useSelector } from "react-redux";

import { State } from "@/state";
import { toggleAside } from "@/state/common";
import { Aside } from "./article-layout-elements";
import { SidePaneHeader } from "./article-layout-side-pane-header";

export interface ArticleLayoutAsideProps {
  readonly children: ReactNode;
}

export const ArticleLayoutAside: FC<ArticleLayoutAsideProps> = ({
  children,
}) => {
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
      <SidePaneHeader
        title="About this article"
        showWhenScreenWidthIsSmallerThan={1280}
        onClose={handleCloseAside}
      />
      {children}
    </Aside>
  );
};
