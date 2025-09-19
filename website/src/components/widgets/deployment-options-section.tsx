import { Link } from "gatsby";
import React, { FC } from "react";
import styled from "styled-components";

import { ContentSection, IconContainer } from "@/components/misc";
import { Icon } from "@/components/sprites";
import { THEME_COLORS } from "@/style";
import { Box, Boxes } from "./box-elements";

// Icons
import ArrowRightIconSvg from "@/images/icons/arrow-right.svg";
import BuildingIconSvg from "@/images/icons/building.svg";
import CloudIconSvg from "@/images/icons/cloud.svg";
import ServerIconSvg from "@/images/icons/server.svg";

export const DeploymentOptionsSection: FC = () => (
  <ContentSection
    title="Deploy Your Way"
    text="
      Choose the deployment option that best fits your needs. Whether you prefer
      the convenience of our cloud infrastructure, the exclusivity of a 
      dedicated server, or the autonomy of self-hosting on your own 
      infrastructure, our platform integrates seamlessly into your preferred 
      environment.
    "
    noBackground
  >
    <Boxes>
      <OptionBox>
        <Title>
          Shared
          <IconContainer $size={20}>
            <Icon {...CloudIconSvg} />
          </IconContainer>
        </Title>
        <Text>
          Utilize our cloud infrastructure with shared clusters for a quick,
          simple, and cost-effective solution. Get started quickly without the
          hassle of managing hardware.
        </Text>
        <LinkTextButton to="/pricing">
          Compare
          <IconContainer $size={16}>
            <Icon {...ArrowRightIconSvg} />
          </IconContainer>
        </LinkTextButton>
      </OptionBox>
      <OptionBox>
        <Title>
          Dedicated
          <IconContainer $size={20}>
            <Icon {...ServerIconSvg} />
          </IconContainer>
        </Title>
        <Text>
          Opt for a fully managed dedicated server with no complexity. Enjoy the
          benefits of your own instance with dedicated performance tailored to
          your needs.
        </Text>
        <LinkTextButton to="/pricing">
          Compare
          <IconContainer $size={16}>
            <Icon {...ArrowRightIconSvg} />
          </IconContainer>
        </LinkTextButton>
      </OptionBox>
      <OptionBox>
        <Title>
          On-premise
          <IconContainer $size={20}>
            <Icon {...BuildingIconSvg} />
          </IconContainer>
        </Title>
        <Text>
          Host on your own infrastructure for complete control. This option
          offers no volume limits and full data ownership, perfect for meeting
          stringent security and compliance requirements.
        </Text>
        <LinkTextButton to="/pricing">
          Compare
          <IconContainer $size={16}>
            <Icon {...ArrowRightIconSvg} />
          </IconContainer>
        </LinkTextButton>
      </OptionBox>
    </Boxes>
  </ContentSection>
);

const OptionBox = styled(Box)`
  flex-direction: column;
  padding: 40px;
  background-color: initial;
  background-image: linear-gradient(
    to right bottom,
    #379dc83d,
    #2b80ad3d,
    #2263903d,
    #1a48743d,
    #112f573d
  );

  :nth-child(2) {
    background-image: linear-gradient(
      to right bottom,
      #37c8ab3d,
      #2baa933d,
      #218d7c3d,
      #1872653d,
      #11574e3d
    );
  }

  :nth-child(3) {
    background-image: linear-gradient(
      to right bottom,
      #ab7bb03d,
      #9162953d,
      #784a7c3d,
      #6033633d,
      #481d4b3d
    );
  }
`;

const Title = styled.h4`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin-bottom: 24px;

  & > ${IconContainer} {
    margin-left: 24px;

    & > svg {
      fill: ${THEME_COLORS.heading};
    }
  }
`;

const Text = styled.p.attrs({
  className: "text-2",
})`
  margin-bottom: 36px;
`;

export const LinkTextButton = styled(Link).attrs({
  className: "text-2",
})`
  display: flex;
  flex-direction: row;
  align-items: center;

  & > ${IconContainer} {
    margin-left: 10px;

    & > svg {
      fill: ${THEME_COLORS.link};
      transition: fill 0.2s ease-in-out;
    }
  }

  &:hover > ${IconContainer} > svg {
    fill: ${THEME_COLORS.linkHover};
  }
`;
