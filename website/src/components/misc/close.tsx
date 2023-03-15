import React, { FC, HTMLAttributes } from "react";
import CloseIconSvg from "@/images/close.svg";
import { IconContainer } from "./icon-container";

import styled from "styled-components";

type CloseProps = HTMLAttributes<HTMLButtonElement>;

export const Close: FC<CloseProps> = (props) => {
  return (
    <ButtonContainer {...props}>
      <IconContainer size={20}>
        <CloseIconSvg />
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
