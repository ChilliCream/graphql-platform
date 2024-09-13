import React, { FC, ReactNode } from "react";
import styled from "styled-components";

import { THEME_COLORS } from "@/style";

type Props = {
  readonly children: ReactNode;
};

export const Warning: FC<Props> = ({ children }) => {
  return (
    <Container>
      <Heading>
        {warningIcon} <span>Warning</span>
      </Heading>

      {children}
    </Container>
  );
};

const warningIcon = (
  <svg
    xmlns="http://www.w3.org/2000/svg"
    width="16"
    height="16"
    viewBox="0 0 16 16"
  >
    <path d="M8.893 1.5c-.183-.31-.52-.5-.887-.5s-.703.19-.886.5L.138 13.499a.98.98 0 0 0 0 1.001c.193.31.53.501.886.501h13.964c.367 0 .704-.19.877-.5a1.03 1.03 0 0 0 .01-1.002L8.893 1.5zm.133 11.497H6.987v-2.003h2.039v2.003zm0-3.004H6.987V5.987h2.039v4.006z" />
  </svg>
);

const Heading = styled.div`
  fill: ${THEME_COLORS.textContrast};
  display: flex;
  align-items: center;
  gap: 10px;

  > span {
    margin-bottom: 3px;
    font-weight: 600;
    line-height: normal;
  }

  > svg {
    margin-left: 4px;
    transform: scale(1.3);
  }
`;

const Container = styled.div`
  margin-bottom: 24px;
  padding: 20px;
  border: 1px solid ${THEME_COLORS.boxBorder};
  background-color: ${THEME_COLORS.warning};
  color: ${THEME_COLORS.textContrast};

  br {
    margin-bottom: 16px;
  }

  a {
    color: white !important;
    font-weight: 600;
    text-decoration: underline;
  }

  code {
    border-color: ${THEME_COLORS.textContrast};
    color: ${THEME_COLORS.textContrast};
  }

  > p:last-child {
    margin-bottom: 0;
  }

  @media only screen and (min-width: 700px) {
    border-radius: var(--box-border-radius);
  }
`;
