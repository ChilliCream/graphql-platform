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
  svg {
    fill: #2e2857;
    stroke: #2e2857;

    &:hover {
      fill: #3b3370;
      stroke: #3b3370;
    }
  }
`;
