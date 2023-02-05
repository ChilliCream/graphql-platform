import styled from "styled-components";

export interface IconContainerProps {
  readonly size?: 10 | 12 | 14 | 16 | 20 | 24 | 28 | 32;
}

export const IconContainer = styled.span<IconContainerProps>`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: ${(props) => props.size || 24}px;
  height: ${(props) => props.size || 24}px;
  font-size: 24px;
  line-height: 24px;
  vertical-align: middle;

  > svg {
    width: 100%;
    height: 100%;
  }
`;
