import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { useStickyElement } from "./useStickyElement";

export const DocPageAside: FunctionComponent = ({ children }) => {
  const { containerRef, elementRef } = useStickyElement<
    HTMLElement,
    HTMLDivElement
  >(1300);

  return (
    <Aside ref={containerRef}>
      <FixedContainer ref={elementRef}>{children}</FixedContainer>
    </Aside>
  );
};

const Aside = styled.aside`
  display: none;
  flex: 0 0 250px;
  flex-direction: column;

  * {
    user-select: none;
  }

  @media only screen and (min-width: 1300px) {
    display: flex;
  }
`;

const FixedContainer = styled.div`
  position: fixed;
  padding: 25px 0;
  width: 250px;
`;
