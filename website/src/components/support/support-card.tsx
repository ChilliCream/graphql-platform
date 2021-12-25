import React, { FC } from "react";
import styled from "styled-components";
import { Perk } from "./perk";

export interface SupportCardProps {
  name: string;
  description: string;
  perks: string[];
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
  font-family: "Roboto", sans-serif;
  margin-top: 2rem;
  width: 100%;
  color: var(--text-color-contrast);
  padding: 0.5rem 0;
  font-size: 0.875rem;
  line-height: 1.25rem;

  font-weight: 600;
  border: 1px solid transparent;
  border-radius: 0.375rem;
  background-color: var(--primary-color);

  :hover {
    background-color: var(--secondary-color);
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
  line-height: 1.5rem;
  font-size: 1.125rem;
  font-weight: 600;
  color: rgb(17, 24, 39);
`;

const Description = styled.p`
  margin: 0;
  color: rgb(107, 114, 128);
  margin-top: 1rem;
  font-size: 0.875rem;
  line-height: 1.25rem;
`;

const BottomHalf = styled.div`
  padding: 1.5rem 1.5rem 2rem 1.5rem;
  border-top: 1px solid rgba(229, 231, 235, 1);
`;

const Perks = styled.ul`
  // Reset
  margin: 0;
  padding: 0;
  list-style: none;

  margin-top: 1.5rem;
`;
