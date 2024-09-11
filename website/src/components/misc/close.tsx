import React, { FC, HTMLAttributes } from "react";
import styled from "styled-components";

import { Icon } from "@/components/sprites";
import { IconContainer } from "./icon-container";

// Icons
import XmarkIconSvg from "@/images/icons/xmark.svg";

type CloseProps = HTMLAttributes<HTMLButtonElement>;

export const Close: FC<CloseProps> = (props) => {
  return (
    <ButtonContainer {...props}>
      <IconContainer $size={20}>
        <Icon {...XmarkIconSvg} />
      </IconContainer>
    </ButtonContainer>
  );
};

export const ButtonContainer = styled.button`
  fill: #fafafa;
  stroke: #fafafa;

  &:hover {
    fill: #ececec;
    stroke: #ececec;
  }
`;
