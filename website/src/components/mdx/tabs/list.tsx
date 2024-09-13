import styled from "styled-components";

import { THEME_COLORS } from "@/style";

export const List = styled.div`
  display: flex;
  gap: 32px;
  margin-bottom: 24px;
  border-radius: var(--box-border-radius);
  padding-right: 20px;
  padding-left: 20px;
  border: 1px solid ${THEME_COLORS.boxBorder};
  background-color: ${THEME_COLORS.backgroundAlt};
`;
