import React, { FC } from "react";
import { useSelector } from "react-redux";
import styled from "styled-components";

import { ContentSection, SrOnly } from "@/components/misc";
import { State, WorkshopsState } from "@/state";
import { THEME_COLORS } from "@/style";
import { Box, Boxes, BoxLink } from "./box-elements";

export const PublicWorkshopSection: FC = () => {
  const workshops = useSelector<State, WorkshopsState>((state) =>
    state.workshops.filter(({ active }) => active)
  );

  return workshops.length ? (
    <ContentSection title="Upcoming Public Workshops" noBackground>
      <SrOnly>
        Here you find the latest news about the ChilliCream and its entire
        GraphQL Platform.
      </SrOnly>
      <Boxes>
        {workshops.map((workshop) => (
          <Box key={`workshop-${workshop.id}`}>
            <BoxLink to={workshop.url}>
              <Metadata>
                <Space>
                  <Title>{workshop.title}</Title>
                  <Footer>
                    {[workshop.date, workshop.host, workshop.place]
                      .filter((value) => !!value?.length)
                      .join(" ・ ")}
                  </Footer>
                </Space>
              </Metadata>
            </BoxLink>
          </Box>
        ))}
      </Boxes>
    </ContentSection>
  ) : null;
};

const Metadata = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  justify-content: space-between;
  margin: 28px 24px;
`;

const Space = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  justify-content: space-between;
`;

const Title = styled.h5`
  margin-bottom: 28px;
`;

const Footer = styled.div.attrs({
  className: "text-3",
})`
  display: flex;
  flex-direction: row;
  align-items: center;
  color: ${THEME_COLORS.textAlt};
`;
