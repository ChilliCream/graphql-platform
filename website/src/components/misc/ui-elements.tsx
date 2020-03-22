import styled from "styled-components";

export const IconContainer = styled.span<{ size?: number }>`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: ${props => props.size || 24}px;
  height: ${props => props.size || 24}px;
  font-size: 24px;
  line-height: 24px;
`;
