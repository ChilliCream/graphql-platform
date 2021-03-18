import React, { FunctionComponent, useState } from "react";
import styled from "styled-components";
import { SalesCard } from "../components/support/sales-card";
import { SalesCardPerk } from "../components/support/sales-card-perk";
import { SalesCardPerkItem } from "../components/support/sales-card-perk-item";
import { IsMobile, IsSmallDesktop, IsPhablet } from "../shared-style";

type Cycle = "monthly" | "biannually" | "annually";
type Plan = "Basic" | "Enterprise";

export const SalesPartial: FunctionComponent = () => {
  const [cycle, setCycle] = useState<Cycle>("annually");

  const planPrices: Record<Plan, Record<Cycle, number>> = {
    Basic: {
      monthly: 325,
      biannually: 300,
      annually: 275,
    },
    Enterprise: {
      monthly: 2250,
      biannually: 2000,
      annually: 1750,
    },
  };

  return (
    <Container>
      <SwiterContainer>
        <Title>Support Plans</Title>
        <LeadingText>
          Do you use HotChocolate or any other ChilliCream product? Do you need
          help?<br></br>Choose one of our support plans.
        </LeadingText>
        <CycleContainer>
          <CyclePlan
            isActive={cycle === "monthly"}
            onClick={() => setCycle("monthly")}
          >
            Monthly
          </CyclePlan>
          <CyclePlan
            style={{ marginLeft: "0.125rem" }}
            isActive={cycle === "biannually"}
            onClick={() => setCycle("biannually")}
          >
            Biannually
          </CyclePlan>
          <CyclePlan
            style={{ marginLeft: "0.125rem" }}
            isActive={cycle === "annually"}
            onClick={() => setCycle("annually")}
          >
            Annually
          </CyclePlan>
        </CycleContainer>
      </SwiterContainer>
      <CardContainer>
        <SalesCard
          name="Basic"
          description="The support plan for companies that want to get in contact with a ChilliCream expert
          and prioritised bug fixes for reported incidents."
          price={planPrices.Basic[cycle]}
          cycle="mo"
        >
          <SalesCardPerk>
            Your own company slack channel, where a maintainer answers your
            questions
          </SalesCardPerk>
          <SalesCardPerk>
            24 hours response time based on european working days
          </SalesCardPerk>
          <SalesCardPerk>One prioritized incident per month</SalesCardPerk>
        </SalesCard>
        <SalesCard
          name="Enterprise"
          description="The support plan for companies that need a GraphQL sparring partner and want a dedicated time pensum for their project per month."
          price={planPrices.Enterprise[cycle]}
          cycle="mo"
        >
          <SalesCardPerk>
            Your own company slack channel, where a maintainer answers your
            questions
          </SalesCardPerk>
          <SalesCardPerk>
            24 hours response time based on european working days
          </SalesCardPerk>
          <SalesCardPerk>Two prioritized incidents per month</SalesCardPerk>
          <SalesCardPerk>
            12 hours per month at your free disposal
            <PerkItemlistHeader>The time can be spend on:</PerkItemlistHeader>
            <SalesCardPerkItem>
              Educating your team with workshops
            </SalesCardPerkItem>
            <SalesCardPerkItem>
              Consulting on GraphQL, testing and architecture
            </SalesCardPerkItem>
            <SalesCardPerkItem>Regular code reviews</SalesCardPerkItem>
            <SalesCardPerkItem>
              Pushing a feature of your choice
            </SalesCardPerkItem>
          </SalesCardPerk>
        </SalesCard>
      </CardContainer>
    </Container>
  );
};

const PerkItemlistHeader = styled.div`
  margin-top: 5px;
  margin-bottom: 5px;
`;

const Container = styled.div`
  width: 100%;
  overflow: hidden;
  box-sizing: border-box;

  ${IsSmallDesktop(`
      padding: 40px;
      padding-top: 0;
    `)}

  ${IsMobile(`  
      padding: 15px;
      padding-top: 0;
    `)}
`;

const SwiterContainer = styled.div`
  display: flex;
  flex-direction: column;
`;

const Title = styled.h1`
  flex: 0 0 auto;
  font-size: 1.75em;
  color: #667;
  text-align: center;

  ${IsPhablet(`
    text-align: center;
  `)}
`;

const LeadingText = styled.p`
  margin: 0;
  margin-top: 1.25rem;
  font-size: 1.25rem;
  line-height: 1.75rem;
  max-width: 800px;
  align-self: center;

  color: rgb(107, 114, 128);
  text-align: center;
`;

interface CyclePlanpRrops {
  readonly isActive: boolean;
}

const CyclePlan = styled.button<CyclePlanpRrops>`
  padding-top: 0.5rem;
  padding-bottom: 0.5rem;
  padding-left: 2rem;
  padding-right: 2rem;
  cursor: pointer;
  color: rgb(55, 65, 81);

  font-size: 0.875rem;
  line-height: 1.25rem;
  font-weight: 500;
  border-radius: 0.375rem;
  border-color: rgb(229, 231, 235);
  background-color: ${(p) => (p.isActive ? "rgb(255, 255, 255)" : "inherit")};
  box-shadow: ${(p) =>
    p.isActive ? "rgba(0, 0, 0, 0.25) 0px 1px 2px 0px" : "none"};

  ${IsMobile(`
    padding-left: 0.75rem;
    padding-right: 0.75rem;
  `)}
`;

const CycleContainer = styled.div`
  display: flex;
  align-self: center;
  border-radius: 0.5rem;

  padding: 0.125rem;
  margin-top: 2rem;

  background-color: rgb(243, 244, 246);

  ${CyclePlan} {
    width: 50%;
  }

  ${IsMobile(`
    ${CyclePlan} {
      width: auto;
    }
  `)}
`;

const CardContainer = styled.div`
  margin-top: 1.5rem;
  justify-items: center;

  > :not(:first-child) {
    margin-top: 16px;
  }
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));

  max-width: 800px;
  margin-left: auto;
  margin-right: auto;

  gap: 1.5rem;

  > div {
    margin-top: 0 !important;
  }

  ${IsPhablet(`
    margin-top: 1rem;
    grid-template-columns: minmax(0, 1fr)
  `)}
`;
