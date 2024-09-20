import { FONT_FAMILY_HEADING, MAX_CONTENT_WIDTH, THEME_COLORS } from "@/style";
import React, { ReactElement } from "react";
import styled from "styled-components";

import { Icon } from "@/components/sprites";
import { IconContainer } from "./icon-container";

// Icons
import CheckIconSvg from "@/images/icons/check.svg";
import { LinkButton } from "./button";

export interface PlansProps {
  readonly plans: readonly PlanProps[];
}

export function Plans({ plans }: PlansProps): ReactElement {
  return (
    <PlansContainer $maxColumns={plans.length > 4 ? 4 : plans.length}>
      {plans.map((props) => (
        <Plan key={props.title} {...props} />
      ))}
    </PlansContainer>
  );
}

export interface PlanProps {
  readonly title: string;
  readonly price?: number | string;
  readonly period?: "hour" | "month" | "year";
  readonly description: string;
  readonly features: readonly string[];
  readonly ctaText: string;
  readonly ctaLink: string;
}

function Plan({
  title,
  price,
  period,
  description,
  features,
  ctaText,
  ctaLink,
}: PlanProps): ReactElement {
  return (
    <>
      <PlanHeader>
        <PlanTitleContainer>
          <PlanTitle>{title}</PlanTitle>
        </PlanTitleContainer>
        {(typeof price === "string" ||
          (typeof price === "number" && period !== undefined)) && (
          <PlanPrice>
            {typeof price === "number" ? (
              <>
                ${price}
                <PlanPeriod>/{period}</PlanPeriod>
              </>
            ) : (
              price
            )}
          </PlanPrice>
        )}
        <PlanDescription>{description}</PlanDescription>
      </PlanHeader>
      <PlanSeparator />
      <PlanFeatures>
        {features.map((feature) => (
          <PlanFeature key={`plan-feature-${feature}`}>
            <IconContainer $size={14} style={{ flex: "0 0 auto" }}>
              <Icon {...CheckIconSvg} />
            </IconContainer>
            {feature}
          </PlanFeature>
        ))}
      </PlanFeatures>
      <PlanFooter>
        <LinkButton to={ctaLink}>{ctaText}</LinkButton>
      </PlanFooter>
    </>
  );
}

const PlansContainer = styled.div<{
  readonly $maxColumns: number;
}>`
  display: grid;
  grid-template-columns: repeat(
    ${({ $maxColumns }) => $maxColumns},
    minmax(200px, 300px)
  );
  grid-template-rows: min-content 1px auto min-content;
  border: 1px solid #37353f;
  border-radius: var(--box-border-radius);
  width: ${MAX_CONTENT_WIDTH};
  backdrop-filter: blur(2px);
  background-image: radial-gradient(
    ellipse at bottom,
    #15113599 0%,
    #0c0c2399 40%
  );
  box-shadow: 0 0 120px 60px #fdfdfd12;
  overflow: visible;
`;

const PlanHeader = styled.div`
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  grid-row: 1;
  gap: 48px;
  box-sizing: border-box;
  border-left: 1px solid #211f31;
  padding: 82px 32px 60px;
  overflow: visible;
`;

const PlanTitleContainer = styled.div.attrs({
  className: "text-1",
})`
  position: absolute;
  top: -13px;
  right: 0;
  left: 0;
  display: flex;
  justify-content: center;
`;

const PlanTitle = styled.div.attrs({
  className: "text-1",
})`
  padding: 0 10px;
  border: 1px solid #6b6775;
  border-radius: 12px;
  height: 24px;
  line-height: 1.2em !important;
  background-color: #281c3b;
`;

const PlanPrice = styled.div`
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 3rem;
  font-weight: 500;
  line-height: 1em;
  color: ${THEME_COLORS.heading};
`;

const PlanDescription = styled.div.attrs({
  className: "text-2",
})`
  text-align: center;
`;

const PlanPeriod = styled.span`
  font-size: 1rem;
  line-height: 1em;
`;

const PlanSeparator = styled.div`
  grid-row: 2;
  margin-right: auto;
  margin-left: auto;
  box-sizing: border-box;
  border-left: 1px solid #211f31;
  width: 50%;
  height: 1px;
  background-image: linear-gradient(
    to right,
    #ffffff00 0%,
    #ffffff4d 22%,
    #ffffff 50%,
    #ffffff4d 78%,
    #ffffff00 100%
  );
`;

const PlanFeatures = styled.ul.attrs({
  className: "text-2",
})`
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: flex-start;
  grid-row: 3;
  box-sizing: border-box;
  margin: 0;
  border-left: 1px solid #211f31;
  padding: 64px 32px 32px;
  list-style-type: none;
`;

const PlanFeature = styled.li`
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 8px;

  ${IconContainer} > svg {
    fill: ${THEME_COLORS.text};
  }
`;

const PlanFooter = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  grid-row: 4;
  box-sizing: border-box;
  margin: 0;
  border-left: 1px solid #211f31;
  padding: 32px;
`;
