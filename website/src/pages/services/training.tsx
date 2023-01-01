import React, { FC } from "react";
import styled, { css } from "styled-components";

import { Layout } from "@/components/layout";
import { Link } from "@/components/misc/link";
import {
  ContentContainer,
  EnvelopeIcon,
  ImageContainer,
  Section,
  SectionRow,
  SectionTitle,
} from "@/components/misc/marketing-elements";
import { Hero, Intro, Teaser, Title } from "@/components/misc/page-elements";
import { SEO } from "@/components/misc/seo";
import { SupportCard } from "@/components/misc/support-card";
import { Artwork } from "@/components/sprites";
import { IsPhablet } from "@/shared-style";

// Artwork
import ContactUsSvg from "@/images/artwork/contact-us.svg";

interface Service {
  readonly kind: "Corporate Training" | "Corporate Workshop";
  readonly description: string;
  readonly perks: string[];
}

interface Workshop {
  readonly title: string;
  readonly date: string;
  readonly host: string;
  readonly place: string;
  readonly url: string;
}

const TrainingPage: FC = () => {
  const areaTitle = "Training";

  const services: Service[] = [
    {
      kind: "Corporate Training",
      description:
        "Get your team trained in GraphQL, any of our products, and even React/Relay. Beginner Team? Advanced Team? Or Mixed? Don't panic! Our curriculum is designed to teach in-depth and works really well, but isn't set in stone.",
      perks: [
        "Level up their proficiency",
        "Catered to different skills",
        "Overcome challenges they've been wrestling with",
        "Get everybody on the same technical page",
      ],
    },
    {
      kind: "Corporate Workshop",
      description:
        "We will look at how to build a GraphQL server with ASP.NET Core 7 and Hot Chocolate. You will learn how to explore and manage large schemas. Further, we will dive into React and explore how to efficiently build fast and fluent web interfaces using Relay.",
      perks: [
        "Core concepts and advanced",
        "Deepen knowledge of GraphQL API",
        "Work on a real project",
        "Scale and production quirks",
        "Level up your entire team at once",
        "Have Lots of Fun!",
      ],
    },
  ];

  const workshops: Workshop[] = [
    {
      title: "Reactive Mobile Apps with GraphQL and Maui",
      date: "23 - 24 Jan 2023",
      host: "NDC",
      place: "{ London }",
      url: "https://ndclondon.com/workshops/reactive-mobile-apps-with-graphql-and-maui/8a69a3c2659d",
    },
    {
      title: "Building Modern Apps with GraphQL in ASP.NET Core 7 and React 18",
      date: "20 - 21 Apr 2023",
      host: "dotnetdays",
      place: "lasi, Romania",
      url: "https://dotnetdays.ro/workshops/Building-Modern-Apps-with-GraphQL-and-net7",
    },
    {
      title: "Building Modern Apps with GraphQL in ASP.NET Core 7 and React 18",
      date: "22 - 23 May 2023",
      host: "NDC",
      place: "{ Oslo }",
      url: "https://ndcoslo.com/workshops/building-modern-applications-with-graphql-using-asp-net-core-6-hot-chocolate-and-relay/cb7ce0173d8f",
    },
  ];

  return (
    <Layout>
      <SEO title={areaTitle} />
      <Intro>
        <Title>{areaTitle}</Title>
        <Hero>Learning Is Easier From Experts</Hero>
        <Teaser>
          At ChilliCream, we want you to be successful.
          <br />
          We’ll tell you how it is, and what you need to get there.
        </Teaser>
      </Intro>
      <Section>
        <WorkshopsContainer>
          <WorkshopsHeader>Upcoming Public Workshops</WorkshopsHeader>
          <WorkshopsMatrix>
            {workshops.map(({ title, date, host, place, url }) => (
              <Link key={url} to={url}>
                <dl>
                  <dt>
                    <p>{title}</p>
                  </dt>
                  <dd>
                    <small>{date}</small>
                  </dd>
                  <dd>
                    <p>{host}</p>
                    <small>{place}</small>
                  </dd>
                </dl>
              </Link>
            ))}
          </WorkshopsMatrix>
          <WorkshopsFooter />
        </WorkshopsContainer>
        <CardsContainer>
          {services.map(({ kind, description, perks }) => (
            <SupportCard
              key={kind}
              name={kind}
              description={description}
              perks={perks}
            />
          ))}
        </CardsContainer>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer>
            <Artwork {...ContactUsSvg} />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Get in Touch</SectionTitle>
            <p>
              Want to learn more? Get the right help for your team and reach out
              to us today. Write us an{" "}
              <a href="mailto:contact@chillicream.com?subject=Training">
                <EnvelopeIcon />
              </a>{" "}
              and we will come back to you shortly!
            </p>
          </ContentContainer>
        </SectionRow>
      </Section>
    </Layout>
  );
};

export default TrainingPage;

const CardsContainer = styled.div`
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1.5rem;
  justify-items: center;
  margin-top: 1.5rem auto 0;
  max-width: 800px;

  > :not(:first-child) {
    margin-top: 16px;
  }

  > div {
    margin-top: 0 !important;
  }

  ${IsPhablet(css`
    margin-top: 1rem;
    grid-template-columns: minmax(0, 1fr);
  `)}
`;

const WorkshopsContainer = styled.div`
  display: flex;
  flex-direction: column;
  width: 100%;
  max-width: 960px;
  margin-top: -1em;
  padding: 0 20px;
  box-sizing: border-box;

  @media only screen and (min-width: 400px) {
    padding: 0 40px;
    margin-bottom: 40px;
    overflow: visible;
  }
`;

const WorkshopsHeader = styled.div`
  display: block;
  background: none;
  text-align: center;
  outline: none;
  padding: 0;
  color: var(--cc-text-color);
  font-size: 1.25em;
  line-height: 1.5em;
  cursor: default;
`;

const WorkshopsFooter = styled.div`
  display: block;
  background: none;
  text-align: center;
  outline: none;
  padding: 0;
  border: 0;
  color: var(--cc-text-color);
  font-size: 1.25em;
  line-height: 1.5em;
  cursor: default;
`;

const WorkshopsMatrix = styled.div`
  border-top: 1px solid #d2d2d7;
  border-bottom: 1px solid #d2d2d7;
  padding: 0.5em 0;
  margin: 0.5em 0;
  scroll-snap-type: x mandatory;
  overflow-x: scroll;
  overscroll-behavior-x: none;
  scrollbar-width: none;

  &::-webkit-scrollbar {
    display: none;
  }

  & dl {
    margin: 0;
    padding: 0.5em;
    display: grid;
    grid-template-columns: auto;
    min-height: 2.5em;
    background-color: var(--cc-background-color);
  }

  & a:nth-child(odd) dl {
    background-color: #fafafa;
  }

  & a:hover dl {
    background-color: #f3f4f8;
  }

  & dt {
    margin: 0;
    padding: 0.25em 0.5em;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    text-align: left;
    position: sticky;
    left: 0;
    background-color: inherit;
    scroll-snap-align: start;
  }

  & dt p {
    margin: 0;
    font-size: 0.825rem;
    line-height: 1;
    color: #3d5f9f;
    text-align: center;
  }

  & dd {
    margin: 0;
    padding: 0.25em 0.5em;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    font-weight: 600;
    text-align: center;
    scroll-snap-align: start;
  }

  & dd strong {
    font-size: 1em;
    color: #232129;
  }

  & dd p {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
    line-height: 1;
    color: #36313d;
  }

  & small {
    font-size: 0.75em;
    color: #36313d;
  }

  @media only screen and (min-width: 600px) {
    && dt {
      align-items: flex-start;
    }

    && dt p {
      text-align: start;
    }

    && dl {
      grid-template-columns: 60% repeat(2, 1fr);
    }
  }
`;
