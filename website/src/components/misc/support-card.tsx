import React, { FC } from "react";
import styled from "styled-components";
import { FONT_FAMILY_HEADING, THEME_COLORS } from "../../shared-style";

export interface SupportCardProps {
  readonly name: string;
  readonly description: string;
  readonly perks: string[];
}

export const SupportCard: FC<SupportCardProps> = ({
  name,
  description,
  perks,
}) => {
  return (
    <Container>
      <TopHalf>
        <Name>{name}</Name>
        <Description>{description}</Description>
        <ContactUs
          href={`mailto:contact@chillicream.com?subject=${name} - Support`}
        >
          Contact Us
        </ContactUs>
      </TopHalf>
      <BottomHalf>
        <Perks>
          {perks.map((perk) => (
            <Perk>{perk}</Perk>
          ))}
        </Perks>
      </BottomHalf>
    </Container>
  );
};

const ContactUs = styled.a`
  display: block;
  text-align: center;
  cursor: pointer;
  font-family: ${FONT_FAMILY_HEADING};
  margin-top: 2rem;
  width: 100%;
  color: ${THEME_COLORS.textContrast};
  padding: 0.5rem 0;
  font-size: 0.875rem;
  line-height: 1.25rem;

  font-weight: 600;
  border: 1px solid transparent;
  border-radius: 0.375rem;
  background-color: ${THEME_COLORS.primary};

  :hover {
    background-color: ${THEME_COLORS.secondary};
  }
`;

const Container = styled.div`
  max-width: 350px;
  margin: 10px;
  box-shadow: rgba(0, 0, 0, 0.25) 0px 3px 6px 0px;
  border: 1px solid rgb(229, 231, 235);
  border-radius: var(--border-radius);
`;

const TopHalf = styled.div`
  padding: 1.5rem;
`;

const Name = styled.h2`
  margin: 0;
  color: rgb(17, 24, 39);
  font-size: 1.125rem;
  font-weight: 600;
  line-height: 1.5rem;
`;

const Description = styled.p`
  margin: 1rem 0 0;
  color: rgb(107, 114, 128);
  font-size: 0.875rem;
  line-height: 1.25rem;
`;

const BottomHalf = styled.div`
  border-top: 1px solid rgba(229, 231, 235, 1);
  padding: 1.5rem 1.5rem 2rem 1.5rem;
`;

const Perks = styled.ul`
  // Reset
  margin: 1.5rem 0 0;
  padding: 0;
  list-style: none;
`;

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
  grid-template-columns: 20px 1fr;
  grid-template-rows: auto;
`;

const PerkContainer = styled.div`
  margin-left: 12px;
  font-size: 0.875rem;
  line-height: 1.25rem;
`;
