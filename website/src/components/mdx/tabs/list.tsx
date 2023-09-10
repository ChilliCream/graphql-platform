import styled from "styled-components";

import { THEME_COLORS } from "@/shared-style";

export const List = styled.div`
  display: flex;
  gap: 2rem;
  background-color: ${THEME_COLORS.backgroundAlt};

  ~ .gatsby-highlight {
    margin-top: 0;
  }

  ~ p {
    margin-top: 14px;
  }
`;
