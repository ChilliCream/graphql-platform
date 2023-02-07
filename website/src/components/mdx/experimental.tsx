import { THEME_COLORS } from "@/shared-style";
import React, { FC } from "react";
import styled from "styled-components";

export const Experimental: FC = () => {
  return (
    <Container>
      <Heading>
        {scienceIcon} <span>Experimental</span>
      </Heading>
      This feature is not yet finished nor polished.
      <br />
      Please provide feedback if you encounter any issues or have ideas on how
      to improve the developer experience.
    </Container>
  );
};

const scienceIcon = (
  <svg
    xmlns="http://www.w3.org/2000/svg"
    height="24"
    width="24"
    viewBox="0 0 24 24"
  >
    <path d="M19.8,18.4L14,10.67V6.5l1.35-1.69C15.61,4.48,15.38,4,14.96,4H9.04C8.62,4,8.39,4.48,8.65,4.81L10,6.5v4.17L4.2,18.4 C3.71,19.06,4.18,20,5,20h14C19.82,20,20.29,19.06,19.8,18.4z" />
  </svg>
);

const Heading = styled.div`
  fill: ${THEME_COLORS.textContrast};
  display: flex;
  align-items: center;
  gap: 6px;

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
