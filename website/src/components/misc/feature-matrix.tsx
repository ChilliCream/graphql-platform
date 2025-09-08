import { MAX_CONTENT_WIDTH } from "@/style";
import React, { Fragment, ReactElement } from "react";
import styled from "styled-components";

import { Icon } from "@/components/sprites";
import { IconContainer } from "./icon-container";

// Icons
import CheckIconSvg from "@/images/icons/check.svg";
import MinusIconSvg from "@/images/icons/minus.svg";

export interface FeatureMatrixProps {
  readonly plans: readonly string[];
  readonly featureGroups: readonly FeatureGroup[];
}

export function FeatureMatrix({
  plans,
  featureGroups,
}: FeatureMatrixProps): ReactElement {
  return (
    <FeatureMatrixContainer $maxColumns={plans.length > 4 ? 4 : plans.length}>
      <PlanDescription>Plans</PlanDescription>
      {plans.map((title) => (
        <PlanName key={`plan-${title}`}>{title}</PlanName>
      ))}
      {featureGroups.map((props, index) => (
        <FeatureGroup
          key={`feature-group-${index}`}
          columns={plans.length + 1}
          {...props}
        />
      ))}
    </FeatureMatrixContainer>
  );
}

export interface FeatureGroup {
  readonly title: string;
  readonly features: readonly Feature[];
}

export interface Feature {
  readonly title: string;
  readonly description?: string;
  readonly values: readonly (boolean | string)[];
}

interface FeatureGroupProps extends FeatureGroup {
  readonly columns: number;
}

function FeatureGroup({
  title,
  features,
  columns,
}: FeatureGroupProps): ReactElement {
  function renderValue(value: boolean | string): ReactElement {
    switch (value) {
      case true:
        return (
          <IconContainer $size={14}>
            <Icon {...CheckIconSvg} />
          </IconContainer>
        );

      case false:
        return (
          <IconContainer $size={14}>
            <Icon {...MinusIconSvg} />
          </IconContainer>
        );

      default:
        return <>{value}</>;
    }
  }

  return (
    <>
      <FeatureGroupTitle $columns={columns}>{title}</FeatureGroupTitle>
      {features.map((feature, index) => (
        <Fragment key={`feature-${title}-${index}`}>
          <FeatureDescription>{feature.title}</FeatureDescription>
          {feature.values.map((value, valueIndex) => (
            <Feature key={`feature-${title}-${index}-value-${valueIndex}`}>
              {renderValue(value)}
            </Feature>
          ))}
        </Fragment>
      ))}
    </>
  );
}

const FeatureMatrixContainer = styled.div<{
  readonly $maxColumns: number;
}>`
  display: grid;
  grid-template-columns: 220px repeat(
      ${({ $maxColumns }) => $maxColumns},
      minmax(160px, 300px)
    );
  row-gap: 48px;
  border: 1px solid #37353f;
  border-radius: var(--box-border-radius);
  width: ${MAX_CONTENT_WIDTH};
  padding: 38px 44px 44px;
  backdrop-filter: blur(2px);
  background-image: radial-gradient(
    ellipse at bottom,
    #15113599 0%,
    #0c0c2399 40%
  );
  box-shadow: 0 0 120px 60px #fdfdfd12;
  overflow: visible;
`;

const PlanDescription = styled.div`
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: center;
  overflow: visible;
`;

const PlanName = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  overflow: visible;
`;

const FeatureGroupTitle = styled.div.attrs({
  className: "text-2",
})<{
  readonly $columns: number;
}>`
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: center;
  grid-column: 1 / ${({ $columns }) => $columns + 1};
  overflow: visible;
`;

const FeatureDescription = styled.div.attrs({
  className: "text-2",
})`
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: center;
  overflow: visible;
`;

const Feature = styled.div.attrs({
  className: "text-2",
})`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  overflow: visible;
`;
