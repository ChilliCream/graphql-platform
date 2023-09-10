import React, { FC, useEffect, useRef } from "react";
import styled from "styled-components";

import { useObservable } from "@/state";

// Icons
import ArrowUpIconSvg from "@/images/arrow-up.svg";

export interface PageTopProps {
  readonly onTopScroll: () => void;
}

export const PageTop: FC<PageTopProps> = ({ onTopScroll }) => {
  const ref = useRef<HTMLButtonElement>(null);

  const showButton$ = useObservable((state) => {
    return state.common.yScrollPosition > 60;
  });

  useEffect(() => {
    const subscription = showButton$.subscribe((showButton) => {
      ref.current?.classList.toggle("show", showButton);
    });

    return () => {
      subscription.unsubscribe();
    };
  }, [showButton$]);

  return (
    <JumpToTop ref={ref} onClick={onTopScroll}>
      <ArrowUpIconSvg />
    </JumpToTop>
  );
};

const JumpToTop = styled.button`
  display: none;
  position: fixed;
  right: 24px;
  bottom: 24px;
  z-index: 20;
  border-radius: 50%;
  padding: 8px;
  width: 50px;
  height: 50px;
  background-color: white;
  opacity: 0.6;
  box-shadow: 0px 3px 6px 0px rgba(0, 0, 0, 0.25);
  transition: opacity 0.2s ease-in-out;

  &.show {
    display: initial;
  }

  &:hover {
    opacity: 1;
  }
  svg {
    width: 30px;
    height: 30px;
  }
  @media only screen and (min-width: 1600px) {
    right: calc(((100vw - 1320px) / 2) - 100px);
  }
`;
