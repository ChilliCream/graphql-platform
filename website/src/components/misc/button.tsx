import styled from "styled-components";

import { THEME_COLORS } from "@/shared-style";

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
