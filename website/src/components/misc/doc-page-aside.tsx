import React, { FunctionComponent, useCallback } from "react";
import { useDispatch, useSelector } from "react-redux";
import { State } from "../../state";
import { toggleAside } from "../../state/common";
import { Aside, BodyStyle, FixedContainer } from "./doc-page-elements";
import { DocPagePaneHeader } from "./doc-page-pane-header";
import { useStickyElement } from "./useStickyElement";

export const DocPageAside: FunctionComponent = ({ children }) => {
  const { containerRef, elementRef } = useStickyElement<
    HTMLElement,
    HTMLDivElement
  >(1300);
  const showAside = useSelector<State, boolean>(
    (state) => state.common.showAside
  );
  const dispatch = useDispatch();

  const handleCloseAside = useCallback(() => {
    dispatch(toggleAside());
  }, []);

  return (
    <Aside ref={containerRef}>
      <BodyStyle disableScrolling={showAside} />
      <FixedContainer ref={elementRef} className={showAside ? "show" : ""}>
        <DocPagePaneHeader
          title="In this article"
          showWhenScreenWidthIsSmallerThan={1300}
          onClose={handleCloseAside}
        />
        {children}
      </FixedContainer>
    </Aside>
  );
};
