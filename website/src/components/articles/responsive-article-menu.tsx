import React, { ReactElement, useCallback, useEffect, useRef } from "react";
import { useDispatch } from "react-redux";
import styled from "styled-components";

import { Icon } from "@/components/sprites";
import { useObservable } from "@/state";
import { toggleAside, toggleTOC } from "@/state/common";
import { IsDesktop, IsSmallDesktop, IsTablet, THEME_COLORS } from "@/style";

// Icons
import NewspaperIconSvg from "@/images/icons/newspaper.svg";
import RectangleListIconSvg from "@/images/icons/rectangle-list.svg";
import { IconContainer } from "../misc";

export function ResponsiveArticleMenu(): ReactElement {
  const dispatch = useDispatch();
  const responsiveMenuRef = useRef<HTMLDivElement>(null);

  const hasScrolled$ = useObservable((state) => {
    return state.common.yScrollPosition > 20;
  });

  const handleToggleTOC = useCallback(() => {
    dispatch(toggleTOC());
  }, []);

  const handleToggleAside = useCallback(() => {
    dispatch(toggleAside());
  }, []);

  useEffect(() => {
    const subscription = hasScrolled$.subscribe((hasScrolled) => {
      responsiveMenuRef.current?.classList.toggle("scrolled", hasScrolled);
    });

    return () => {
      subscription.unsubscribe();
    };
  }, [hasScrolled$]);

  return (
    <ResponsiveMenuWrapper>
      <ResponsiveMenu ref={responsiveMenuRef}>
        <Button onClick={handleToggleTOC} className="toc-toggle">
          <IconContainer $size={14}>
            <Icon {...RectangleListIconSvg} />
          </IconContainer>
          Table of contents
        </Button>
        <Button onClick={handleToggleAside} className="aside-toggle">
          <IconContainer $size={14}>
            <Icon {...NewspaperIconSvg} />
          </IconContainer>
          About this article
        </Button>
      </ResponsiveMenu>
    </ResponsiveMenuWrapper>
  );
}

const ResponsiveMenuWrapper = styled.div`
  position: absolute;
  left: 0;
  right: 0;
`;

const ResponsiveMenu = styled.div`
  position: fixed;
  top: 71px;
  right: 16px;
  left: 16px;
  z-index: 3;
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  box-sizing: border-box;
  width: auto;
  height: 60px;
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    180deg,
    ${THEME_COLORS.backgroundMenu} 30%,
    #0a072100 100%
  );
  transition: all 100ms linear 0s;

  @media only screen and (min-width: 700px) {
    right: unset;
    left: unset;
    width: 660px;
  }

  ${IsDesktop(`
    display: none;
  `)}

  ${IsSmallDesktop(`
    > .toc-toggle {
      display: none;
    }
  `)}

  ${IsTablet(`
    > .toc-toggle {
      display: flex;
    }
  `)}
`;

const Button = styled.button`
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 8px;
  font-size: 0.875rem;
  color: ${THEME_COLORS.link};
  transition: color 0.2s ease-in-out;

  ${IconContainer} > svg {
    fill: ${THEME_COLORS.link};
    transition: fill 0.2s ease-in-out;
  }

  &.aside-toggle {
    margin-left: auto;
  }

  &:hover {
    color: ${THEME_COLORS.linkHover};

    ${IconContainer} > svg {
      fill: ${THEME_COLORS.linkHover};
    }
  }
`;
