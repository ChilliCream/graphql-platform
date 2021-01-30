import React, { FunctionComponent } from "react";
import styled from "styled-components";

export interface SalesCardProps {
  price: number;
  name: string;
  description: string;
  perks: string[];
  onBuy: () => void;
}

export const SalesCard: FunctionComponent<SalesCardProps> = ({
  name,
  description,
  price,
  perks,
  onBuy,
}) => {
  return (
    <Container>
      <TopHalf>
        <Name>{name}</Name>
        <Description>{description}</Description>
        <PriceRow>
          <Price>${price}</Price> <PerMonth>/mo</PerMonth>
        </PriceRow>
        <Buy onClick={() => onBuy()}>Buy {name}</Buy>
      </TopHalf>
      <BottomHalf>
        <PerkLeadingText>What's included</PerkLeadingText>
        <Perks>
          {perks.map((p, i) => (
            <Perk key={i}>
              <PerkIcon
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 20 20"
                fill="currentColor"
                aria-hidden="true"
              >
                <path
                  fillRule="evenodd"
                  d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                  clip-rule="evenodd"
                />
              </PerkIcon>
              <PerkText>{p}</PerkText>
            </Perk>
          ))}
        </Perks>
      </BottomHalf>
    </Container>
  );
};

const Container = styled.div`
  box-shadow: 0 0 #0000, 0 0 #0000, 0 1px 2px 0 rgba(0, 0, 0, 0.05);
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
  font-weight: 500;
  color: rgb(17, 24, 39);
`;

const Description = styled.p`
  margin: 0;
  color: rgb(107, 114, 128);
  margin-top: 1rem;
  font-size: 0.875rem;
  line-height: 1.25rem;
`;

const PriceRow = styled.p`
  margin: 0;
  margin-top: 2rem;
`;

const Price = styled.span`
  color: rgb(17, 24, 39);
  font-size: 2.25rem;
  line-height: 2.5rem;
  font-weight: 800;
`;

const PerMonth = styled.span`
  color: rgb(107, 114, 128);
  font-size: 1rem;
  line-height: 1.5rem;
  font-weight: 500;
`;

const Buy = styled.button`
  cursor: pointer;
  font-family: "Roboto", sans-serif;
  margin-top: 2rem;
  width: 100%;
  color: #fff;
  padding: 0.5rem 0;
  font-size: 0.875rem;
  line-height: 1.25rem;

  font-weight: 600;
  border: 1px solid transparent;
  border-radius: 0.375rem;
  background-color: var(--brand-color);

  :hover {
    background-color: var(--brand-color-hover);
  }
`;

const BottomHalf = styled.div`
  padding: 1.5rem 1.5rem 2rem 1.5rem;
  border-top: 1px solid rgba(229, 231, 235, 1);
`;

const PerkLeadingText = styled.h3`
  margin: 0;
  font-family: "Roboto", sans-serif;
  letter-spacing: 0.025em;
  text-transform: uppercase;
  color: rgb(17, 24, 39);
  font-size: 0.75rem;
  line-height: 1rem;
  font-weight: 500;
`;

const Perks = styled.ul`
  // Reset
  margin: 0;
  padding: 0;
  list-style: none;

  margin-top: 1.5rem;
`;

const Perk = styled.li`
  display: flex;
`;

const PerkIcon = styled.svg`
  width: 1.25rem;
  height: 1.25rem;
  color: rgb(16, 185, 129);
`;

const PerkText = styled.span`
  color: rgb(107, 114, 128);
  font-family: "Roboto", sans-serif;
  margin-left: 12px;
  font-size: 0.875rem;
  line-height: 1.25rem;
`;
