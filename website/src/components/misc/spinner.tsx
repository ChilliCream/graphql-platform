import React, { FC, memo } from "react";
import styled from "styled-components";

import { FULL_ROTATION, THEME_COLORS, ThemeColors } from "@/style";

export interface SpinnerProps {
  readonly colorSelector?: (colors: ThemeColors) => string;
}

export const Spinner: FC<SpinnerProps> = memo(({ colorSelector }) => {
  const color = colorSelector?.(THEME_COLORS);

  return (
    <Wrapper>
      <Quarter color={color} />
      <Quarter color={color} />
      <Quarter color={color} />
      <Quarter color={color} />
    </Wrapper>
  );
});

const Quarter = styled.div<{
  readonly color?: string;
}>`
  position: absolute;
  display: block;
  box-sizing: border-box;
  width: 40px;
  height: 40px;
  margin: 4px;
  border-color: ${({ color }) => color || THEME_COLORS.spinner} transparent
    transparent transparent;
  border-radius: 100%;
  border-style: solid;
  border-width: 4px;
  animation: ${FULL_ROTATION} 1.2s cubic-bezier(0.5, 0, 0.5, 1) infinite;
`;

const Wrapper = styled.div`
  width: 48px;
  height: 48px;

  > ${Quarter}:nth-child(1) {
    animation-delay: -0.45s;
  }

  > ${Quarter}:nth-child(2) {
    animation-delay: -0.3s;
  }

  > ${Quarter}:nth-child(3) {
    animation-delay: -0.15s;
  }
`;
