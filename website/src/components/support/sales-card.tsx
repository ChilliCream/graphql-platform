import React, { FunctionComponent } from "react";
import styled from "styled-components";

export interface SalesCardProps {
  price: number;
  cycle: string;
  name: string;
  description: string;
}

export const SalesCard: FunctionComponent<SalesCardProps> = ({
  name,
  description,
  price,
  cycle,
  children,
}) => {
  return (
    <Container>
      <TopHalf>
        <Name>{name}</Name>
        <Description>{description}</Description>
        <PriceRow>
          <Price>${USFormat.format(price)}</Price> <PerMonth>/{cycle}</PerMonth>
        </PriceRow>
        <Buy href={`mailto:sales@chillicream.com?subject=${name} Support`}>
          Buy {name}
        </Buy>
      </TopHalf>
      <BottomHalf>
        <PerkLeadingText>What's included</PerkLeadingText>
        <Perks>{children}</Perks>
      </BottomHalf>
    </Container>
  );
};

const USFormat = new Intl.NumberFormat();

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

const Buy = styled.a`
  display: block;
  text-align: center;
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
