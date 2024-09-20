import styled from "styled-components";

import { FONT_FAMILY_HEADING, THEME_COLORS } from "@/style";
import { IconContainer } from "./icon-container";
import { Link } from "./link";

export const Button = styled.button`
  padding: 10px;
  border-radius: var(--border-radius);
  font-size: var(--font-size);
  color: ${THEME_COLORS.textContrast};

  background-color: ${THEME_COLORS.primary};
  transition: background-color 0.2s ease-in-out;

  &:hover {
    background-color: ${THEME_COLORS.secondary};
  }
`;

export const LinkButton = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  box-sizing: border-box;
  border-radius: var(--button-border-radius);
  height: 48px;
  padding: 0 30px;
  border: 2px solid ${THEME_COLORS.primaryButtonBorder};
  color: ${THEME_COLORS.primaryButtonText};
  background-color: ${THEME_COLORS.primaryButton};
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 1.125rem;
  text-decoration: none;
  font-weight: 500;
  transition: background-color 0.2s ease-in-out, border-color 0.2s ease-in-out,
    color 0.2s ease-in-out;

  :hover {
    border-color: ${THEME_COLORS.primaryButtonBorder};
    color: ${THEME_COLORS.primaryButtonHoverText};
    background-color: ${THEME_COLORS.primaryButtonHover};
  }
`;

export const LinkTextButton = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  height: 48px;
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 1.125rem;
  font-weight: 500;

  & > ${IconContainer} {
    margin-left: 10px;

    & > svg {
      fill: ${THEME_COLORS.link};
      transition: fill 0.2s ease-in-out;
    }
  }

  &:hover > ${IconContainer} > svg {
    fill: ${THEME_COLORS.linkHover};
  }
`;
