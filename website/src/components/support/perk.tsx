import React, { FC } from "react";
import styled from "styled-components";

export const Perk: FC = ({ children }) => {
  return (
    <PerkLayout>
      <Bullet>- </Bullet>
      <PerkContainer>{children}</PerkContainer>
    </PerkLayout>
  );
};

const Bullet = styled.div`
  margin-left: 12px;
`;

const PerkLayout = styled.li`
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
