import React, { FC } from "react";
import styled from "styled-components";

import { Layout } from "@/components/layout";
import {
  ActionLink,
  Card,
  CardDetails,
  CardOffer,
  CardsContainer,
} from "@/components/misc/cards";
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
import { Artwork } from "@/components/sprites";

// Artwork
import ContactUsSvg from "@/images/artwork/contact-us.svg";

// Icons
import CheckSvg from "@/images/check.svg";
import NoneSvg from "@/images/none.svg";
import { THEME_COLORS } from "@/shared-style";

interface Plans {
  readonly title: string;
  readonly price: string;
  readonly billed: string;
  readonly description: string;
  readonly action: {
    readonly message: string;
    readonly url: string;
  };
  readonly scope: string;
  readonly checklist: string[];
}

const SupportPage: FC = () => {
  const areaTitle = "Support";

  const plans: Plans[] = [
    {
      title: "Community",
      price: "$0",
      billed: "/year",
      description: "For personal or non-commercial projects, to start hacking.",
      action: {
        message: "Start for free",
        url: "http://slack.chillicream.com/",
      },
      scope: "Includes",
      checklist: ["Public Slack channel"],
    },
    {
      title: "Professional",
      price: "$5,000",
      billed: "/year",
      description:
        "For small teams with moderate bandwidth and projects of low to medium complexity.",
      action: {
        message: "Contact Sales",
        url: "mailto:contact@chillicream.com?subject=Professional Support",
      },
      scope: "Everything in Starter, plus",
      checklist: [
        "Up to 5 critical incidents",
        "Up to 2 non-critical incidents",
        "Private issue tracking board",
      ],
    },
    {
      title: "Business",
      price: "$15,000",
      billed: "/year",
      description: "For larger teams with business-critical projects.",
      action: {
        message: "Contact Sales",
        url: "mailto:contact@chillicream.com?subject=Business Support",
      },
      scope: "Everything in Professional, plus",
      checklist: [
        "Unlimited critical incidents",
        "Up to 4 non-critical incidents",
        "Email support",
      ],
    },
    {
      title: "Enterprise",
      price: "Custom",
      billed: "",
      description:
        "For the whole organization, all your teams and business units, and with tailor made SLAs.",
      action: {
        message: "Contact Sales",
        url: "mailto:contact@chillicream.com?subject=Enterprise Support",
      },
      scope: "Everything in Business, plus",
      checklist: [
        "Up to 10 non-critical incidents",
        "Phone support",
        "Dedicated account manager",
        "Status reviews",
      ],
    },
  ];

  return (
    <Layout>
      <SEO title={areaTitle} />
      <Intro>
        <Title>{areaTitle}</Title>
        <Hero>Expert Help When You Need It</Hero>
        <Teaser>
          At ChilliCream, we want you to be successful.
          <br />
          Our Support plans are designed to give you peace of mind on every
          project.
        </Teaser>
      </Intro>
      <Section>
        <CardsContainer>
          {plans.map(
            ({
              title,
              price,
              billed,
              description,
              action,
              scope,
              checklist,
            }) => (
              <Card key={title}>
                <CardOffer>
                  <header>
                    <h2>{title}</h2>
                    <div>
                      <strong>{price}</strong>
                      {!!billed && <small> {billed}</small>}
                    </div>
                  </header>
                  <p>{description}</p>
                  <ActionLink to={action.url}>{action.message}</ActionLink>
                </CardOffer>
                <CardDetails>
                  <h3>{scope}:</h3>
                  <ul>
                    {checklist.map((value) => (
                      <li key={value}>
                        <CheckSvg />
                        <span>{value}</span>
                      </li>
                    ))}
                  </ul>
                </CardDetails>
              </Card>
            )
          )}
        </CardsContainer>
        <FeatureContainer>
          <FeatureHeader>Feature Matrix</FeatureHeader>
          <FeatureMatrix>
            <dl>
              <dt></dt>
              <dd>
                <strong>Community</strong>
              </dd>
              <dd>
                <strong>Starter</strong>
              </dd>
              <dd>
                <strong>Professional</strong>
              </dd>
              <dd>
                <strong>Business</strong>
              </dd>
              <dd>
                <strong>Enterprise</strong>
              </dd>
            </dl>
            <dl>
              <dt>
                <p>Critical incidents</p>
              </dt>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <p>2</p>
                <small>next business day</small>
              </dd>
              <dd>
                <p>5</p>
                <small>next business day</small>
              </dd>
              <dd>
                <p>unlimited</p>
                <small>next business day</small>
              </dd>
              <dd>
                <p>unlimited</p>
                <small>12 hours</small>
              </dd>
            </dl>
            <dl>
              <dt>
                <p>Non-critical incidents</p>
              </dt>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <p>2</p>
                <small>5 business days</small>
              </dd>
              <dd>
                <p>4</p>
                <small>3 business days</small>
              </dd>
              <dd>
                <p>10</p>
                <small>next business day</small>
              </dd>
            </dl>
            <dl>
              <dt>
                <p>Public Slack channel</p>
              </dt>
              <dd>
                <CheckIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
            </dl>
            <dl>
              <dt>
                <p>Private Slack channel</p>
              </dt>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
            </dl>
            <dl>
              <dt>
                <p>Private issue tracking board</p>
              </dt>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
            </dl>
            <dl>
              <dt>
                <p>Email support</p>
              </dt>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
            </dl>
            <dl>
              <dt>
                <p>Phone support</p>
              </dt>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
            </dl>
            <dl>
              <dt>
                <p>Dedicated account manager</p>
              </dt>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
            </dl>
            <dl>
              <dt>
                <p>Status reviews</p>
              </dt>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <NoneIcon />
              </dd>
              <dd>
                <CheckIcon />
              </dd>
            </dl>
          </FeatureMatrix>
          <FeatureFooter />
        </FeatureContainer>
      </Section>
      <Section>
        <SectionRow>
          <ImageContainer>
            <Artwork {...ContactUsSvg} />
          </ImageContainer>
          <ContentContainer>
            <SectionTitle>Get in Touch</SectionTitle>
            <p>
              {
                "Want to learn more? Get the right help for your team and reach out to us today. Write us an  "
              }
              <a href="mailto:contact@chillicream.com?subject=Support">
                <EnvelopeIcon />
              </a>
              {" and we will come back to you shortly!"}
            </p>
          </ContentContainer>
        </SectionRow>
      </Section>
    </Layout>
  );
};

export default SupportPage;

const FeatureContainer = styled.div`
  display: flex;
  flex-direction: column;
  width: 100%;
  max-width: 1400px;
  padding: 0 20px;
  box-sizing: border-box;

  @media only screen and (min-width: 400px) {
    padding: 0 40px;
  }
`;

const FeatureHeader = styled.div`
  display: block;
  background: none;
  text-align: center;
  outline: none;
  padding: 0;
  border: 0;
  border-bottom: 1px solid #d2d2d7;
  color: ${THEME_COLORS.text};
  font-size: 1.25em;
  line-height: 2;
  margin: 2em 0 0.5em;
  padding: 0;
  cursor: default;
`;

const FeatureFooter = styled.div`
  display: block;
  background: none;
  text-align: center;
  outline: none;
  padding: 0;
  border: 0;
  border-bottom: 1px solid #d2d2d7;
  color: var(--cc-text-color);
  font-size: 1.25em;
  line-height: 2;
  margin: 0.5em 0 0;
  padding: 0;
  cursor: default;
`;

const CheckIcon = styled(CheckSvg)`
  height: 1em;
  width: auto;
  fill: #3d5f9f;
`;

const NoneIcon = styled(NoneSvg)`
  height: 1em;
  width: auto;
  fill: #9b99af;
`;

const FeatureMatrix = styled.div`
  scroll-snap-type: x mandatory;
  overflow-x: scroll;
  overscroll-behavior-x: none;
  scrollbar-width: none;

  &::-webkit-scrollbar {
    display: none;
  }

  @media only screen and (min-width: 960px) {
    align-self: center;

    & dl {
      grid-template-columns: 20% repeat(5, 1fr);
    }
  }

  & dl {
    margin: 0 -0.5em;
    display: grid;
    grid-template-columns: repeat(6, 1fr);
    min-width: 960px;
    min-height: 2.5em;
    background-color: ${THEME_COLORS.background};
    cursor: default;
  }

  & dl:not(:first-of-type):hover {
    background-color: #f3f4f8;
  }

  & dt {
    margin: 0;
    padding: 0.5em;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: start;
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
    color: ${THEME_COLORS.text};
  }

  & dd {
    margin: 0;
    padding: 0.5em;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    font-weight: 600;
    text-align: center;
    scroll-snap-align: start;
  }

  & dd:not(:last-of-type) {
    border-right: 1px solid #e6e6e6;
  }

  & dd strong {
    font-size: 1em;
    color: ${THEME_COLORS.primary};
  }

  & dd p {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
    line-height: 1;
    color: ${THEME_COLORS.secondary};
  }

  & small {
    font-size: 0.75em;
    color: ${THEME_COLORS.text};
  }
`;
