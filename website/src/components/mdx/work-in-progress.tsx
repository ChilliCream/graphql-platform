import { THEME_COLORS } from "@/shared-style";
import React, { FC } from "react";
import styled from "styled-components";

export const WorkInProgress: FC = () => {
  return (
    <Container>
      <Heading>
        {warningSignIcon} <span>Work in progress</span>
      </Heading>
      This documentation is still being worked on.
    </Container>
  );
};

const warningSignIcon = (
  <svg
    xmlns="http://www.w3.org/2000/svg"
    width="22"
    height="22"
    viewBox="0 0 24 24"
  >
    <path d="M2,3H22V13H18V21H16V13H8V21H6V13H2V3M18.97,11L20,9.97V7.15L16.15,11H18.97M13.32,11L19.32,5H16.5L10.5,11H13.32M7.66,11L13.66,5H10.83L4.83,11H7.66M5.18,5L4,6.18V9L8,5H5.18Z" />
  </svg>
);

const Heading = styled.div`
  fill: ${THEME_COLORS.textContrast};
  display: flex;
  align-items: center;
  gap: 8px;

  > span {
    margin-bottom: 3px;
    font-weight: 600;
    line-height: normal;
  }

  > svg {
    margin-left: 4px;
  }
`;

const Container = styled.div`
  padding: 20px 20px;
  background-color: ${THEME_COLORS.warning};
  color: ${THEME_COLORS.textContrast};
  line-height: 1.4;

  @media only screen and (min-width: 860px) {
    padding: 20px 50px;
  }

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
`;
