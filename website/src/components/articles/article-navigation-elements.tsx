import styled, { css } from "styled-components";

import { IconContainer, Link } from "@/components/misc";
import { THEME_COLORS } from "@/style";

export const NavigationList = styled.ol`
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0 18px 0px;
  list-style-type: none;

  @media only screen and (min-width: 1070px) {
    display: flex;
    padding: 0 4px 0;
  }
`;

export const NavigationGroupToggle = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  min-height: 20px;
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;

  > ${IconContainer} > svg {
    fill: ${THEME_COLORS.text};
    transition: fill 0.2s ease-in-out;
  }

  :hover {
    color: ${THEME_COLORS.linkHover};

    > ${IconContainer} > svg {
      fill: ${THEME_COLORS.linkHover};
    }
  }
`;

export const NavigationGroupContent = styled.div`
  > ${NavigationList} {
    padding: 5px 10px;
  }
`;

export const NavigationGroup = styled.div<{
  readonly expanded: boolean;
}>`
  display: flex;
  flex-direction: column;
  cursor: pointer;

  > ${NavigationGroupContent} {
    display: ${({ expanded }) => (expanded ? "initial" : "none")};
  }

  > ${NavigationGroupToggle} > ${IconContainer} {
    margin-left: auto;

    .expand {
      display: ${({ expanded }) => (expanded ? "none" : "flex")};
      fill: ${THEME_COLORS.text};
    }

    .collapse {
      display: ${({ expanded }) => (expanded ? "flex" : "none")};
      fill: ${THEME_COLORS.text};
    }
  }
`;

export const NavigationLink = styled(Link)`
  color: ${THEME_COLORS.text};

  :hover {
    color: ${THEME_COLORS.linkHover};
  }
`;

export const NavigationItem = styled.li<{
  readonly active: boolean;
}>`
  flex: 0 0 auto;
  margin: 5px 0;
  min-height: 20px;
  padding: 0;

  ${({ active }) =>
    active &&
    css`
      > ${NavigationLink}, > ${NavigationGroup} > ${NavigationGroupToggle} {
        font-weight: 600;
      }
    `}
`;

export const NavigationTitle = styled.h6`
  margin-bottom: 12px;
  padding: 0 25px;
  font-size: 0.875rem;

  @media only screen and (min-width: 1070px) {
    padding: 0 4px 0;
  }
`;
