import React, { FC, useEffect, useRef } from "react";
import styled from "styled-components";

import { Icon } from "@/components/sprites";
import { useObservable } from "@/state";
import { THEME_COLORS } from "@/style";

// Icons
import ChevronUpIconSvg from "@/images/icons/chevron-up.svg";

export interface PageTopProps {
  readonly onTopScroll: () => void;
}

export const PageTop: FC<PageTopProps> = ({ onTopScroll }) => {
  const ref = useRef<HTMLButtonElement>(null);

  const showButton$ = useObservable((state) => {
    return state.common.yScrollPosition > 72;
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
      <Icon {...ChevronUpIconSvg} />
    </JumpToTop>
  );
};

const JumpToTop = styled.button`
  display: none;
  position: fixed;
  right: 24px;
  bottom: 24px;
  z-index: 20;
  border-radius: 12px;
  padding: 6px 4px 2px;
  width: 42px;
  height: 42px;
  background-color: ${THEME_COLORS.text};
  opacity: 0.6;
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
