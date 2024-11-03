import React, { FC } from "react";
import styled from "styled-components";

import { FONT_FAMILY_CODE } from "@/style";

export const InlineCode: FC = ({ children }) => {
  return <Container>{children}</Container>;
};

const Container = styled.code`
  padding: 2px 5px;
  font-family: ${FONT_FAMILY_CODE};
  font-size: var(--font-size);
`;
