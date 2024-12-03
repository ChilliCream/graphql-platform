import React, { ReactElement, ReactNode } from "react";
import { useDispatch, useSelector } from "react-redux";

import { State } from "@/state";
import { closeTOC } from "@/state/common";
import { Navigation } from "./article-layout-elements";
import { SidePaneHeader } from "./article-layout-side-pane-header";

export interface ArticleLayoutNavigationProps {
  readonly children: ReactNode;
}

export function ArticleLayoutNavigation({
  children,
}: ArticleLayoutNavigationProps): ReactElement {
  const height = useSelector<State, string>((state) => {
    return state.common.articleViewportHeight;
  });
  const showTOC = useSelector<State, boolean>((state) => state.common.showTOC);
  const dispatch = useDispatch();

  return (
    <Navigation $height={height} $show={showTOC}>
      <SidePaneHeader
        title="Table of contents"
        showWhenScreenWidthIsSmallerThan={1070}
        onClose={() => dispatch(closeTOC())}
      />
      {children}
    </Navigation>
  );
}
