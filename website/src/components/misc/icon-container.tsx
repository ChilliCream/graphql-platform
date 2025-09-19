import { THEME_COLORS } from "@/style";
import styled from "styled-components";

export interface IconContainerProps {
  readonly $size?: 10 | 12 | 14 | 16 | 20 | 24 | 28 | 32;
}

export const IconContainer = styled.span<IconContainerProps>`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: ${({ $size }) => $size || 24}px;
  height: ${({ $size }) => $size || 24}px;
  font-size: ${({ $size }) => $size || 24}px;
  line-height: ${({ $size }) => $size || 24}px;
  vertical-align: middle;

  > svg {
    width: 100%;
    height: 100%;
    fill: ${THEME_COLORS.text};
  }
`;
