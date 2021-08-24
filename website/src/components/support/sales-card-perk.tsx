import React, { FunctionComponent } from "react";
import styled from "styled-components";

export const SalesCardPerk: FunctionComponent = ({ children }) => {
  return (
    <Perk>
      <PerkIcon
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 20 20"
        fill="currentColor"
        aria-hidden="true"
      >
        <path
          fillRule="evenodd"
          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
          clipRule="evenodd"
        />
      </PerkIcon>
      <PerkContainer>{children}</PerkContainer>
    </Perk>
  );
};

const Perk = styled.li`
  display: grid;
  grid-template-rows: auto;
  grid-template-columns: 20px 1fr;
`;

const PerkIcon = styled.svg`
  flex-shrink: 0;
  width: 1.25rem;
  height: 1.25rem;
  color: rgb(16, 185, 129);
`;

const PerkContainer = styled.div`
  margin-left: 12px;
  font-size: 0.875rem;
  line-height: 1.25rem;
`;
