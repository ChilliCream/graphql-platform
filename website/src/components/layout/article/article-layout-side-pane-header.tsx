import React, { FC } from "react";
import styled from "styled-components";

import { Icon } from "@/components/sprites";
import { THEME_COLORS } from "@/style";

// Icons
import XmarkIconSvg from "@/images/icons/xmark.svg";

export interface SidePaneHeaderProps {
  readonly showWhenScreenWidthIsSmallerThan: number;
  readonly title: string;
  readonly onClose: () => void;
}

export const SidePaneHeader: FC<SidePaneHeaderProps> = ({
  showWhenScreenWidthIsSmallerThan,
  title,
  onClose,
}) => (
  <Header $minWidth={showWhenScreenWidthIsSmallerThan}>
    <Title>{title}</Title>
    <CloseButton onClick={onClose} />
  </Header>
);

const Header = styled.header<{
  readonly $minWidth: number;
}>`
  display: flex;
  flex-direction: row;
  align-items: center;
  padding-bottom: 10px;

  @media only screen and (min-width: ${({ $minWidth }) => $minWidth}px) {
    display: none;
  }
`;

const Title = styled.h5`
  flex: 1 1 auto;
  margin-bottom: 0;
  margin-left: 25px;
`;

const CloseButton = styled(Icon).attrs(XmarkIconSvg)`
  flex: 0 0 auto;
  margin-right: 19px;
  margin-left: 20px;
  width: 26px;
  height: 26px;
  opacity: 0.5;
  fill: ${THEME_COLORS.text};
  cursor: pointer;
  transition: opacity 0.2s ease-in-out;

  :hover {
    opacity: 1;
  }
`;
