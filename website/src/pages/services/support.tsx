import React, { FC } from "react";
import styled from "styled-components";

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
import { Artwork } from "@/components/sprites";

// Artwork
import ContactUsSvg from "@/images/artwork/contact-us.svg";

// Icons
import CheckSvg from "@/images/check.svg";
import NoneSvg from "@/images/none.svg";

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
      title: "Starter",
      price: "$2,500",
      billed: "/year",
      description: "For small teams experimenting on non-critical projects.",
      action: {
        message: "Contact Sales",
        url: "mailto:contact@chillicream.com?subject=Starter Support",
      },
      scope: "Everything in Community, plus",
      checklist: ["Up to 2 critical incidents", "Private Slack channel"],
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
                    <h2>
                      <strong>{title}</strong>
                    </h2>
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
              Want to learn more? Get the right help for your team and reach out
              to us today. Write us an{" "}
              <a href="mailto:contact@chillicream.com?subject=Support">
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

export default SupportPage;

const CardsContainer = styled.div`
  display: grid;
  grid-template-columns: minmax(250px, 300px);
  gap: 1rem;
  align-items: stretch;
  justify-content: center;
  overflow: visible;

  @media only screen and (min-width: 400px) {
    grid-template-columns: minmax(300px, 350px);
  }

  @media only screen and (min-width: 600px) {
    grid-template-columns: repeat(2, minmax(225px, 275px));
  }

  @media only screen and (min-width: 768px) {
    grid-template-columns: repeat(2, minmax(250px, 350px));
  }

  @media only screen and (min-width: 992px) {
    grid-template-columns: repeat(3, minmax(250px, 300px));
  }

  @media only screen and (min-width: 1320px) {
    grid-template-columns: repeat(3, minmax(275px, 375px));
  }
`;

const Card = styled.div`
  background: #ffffff;
  box-shadow: rgb(46 41 51 / 8%) 0px 1px 2px, rgb(71 63 79 / 8%) 0px 2px 4px;
  margin: 0px;
  box-sizing: border-box;
  position: relative;
  flex-direction: column;
  border: 1px solid #d9d7e0;
  border-radius: var(--border-radius);
  padding: 0px;
  display: grid;
  grid-template-rows: auto 1fr;
  cursor: default;
  transition: border 0.5s ease-out 0s;

  &:hover {
    border-color: #1d5185;
  }
`;

const CardOffer = styled.div`
  padding: 1.5rem 1.5rem 0;

  & header {
    min-height: 5em;
  }

  & h2 {
    margin: 0;
    font-size: 1.25rem;
    font-weight: 600;
    line-height: 1.75em;
    color: #000000;
  }

  & strong {
    font-size: 1.125em;
    color: #232129;
  }

  & small {
    font-size: 0.75em;
    color: #635e69;
  }

  & p {
    font-weight: normal;
    font-size: 1rem;
    line-height: 1.5;
    margin: 0 0 1.25rem;
    min-height: 5rem;
    color: #36313d;
  }
`;

const CardDetails = styled.div`
  padding: 1.5rem 1.5rem 2rem;
  border-radius: 0 0 var(--border-radius) var(--border-radius);
  color: #36313d;
  background: #f5f5f5;

  & h3 {
    margin: 0;
    font-size: 1rem;
    font-weight: 700;
    line-height: 1.25;
    color: #232129;
  }

  & ul {
    list-style: none;
    padding: 0;
    margin: 0.625rem 0 0;
    font-size: 1rem;
    line-height: 1.5;
    display: grid;
    gap: 0.5rem;
  }

  & li {
    margin: 0;
    display: grid;
    grid-template-columns: auto 1fr;
  }

  & svg {
    height: 1em;
    width: auto;
    margin-right: 0.5em;
    transform: translateY(0.25em);
    fill: #3d5f9f;
  }

  & span {
    line-height: 1.5;
  }
`;

const ActionLink = styled(Link)`
  align-items: center;
  border-radius: 6px;
  box-sizing: border-box;
  cursor: pointer;
  display: inline-flex;
  justify-content: center;
  transition: background 250ms ease 0s, border 250ms ease 0s,
    color 250ms ease 0s;
  line-height: 1;
  text-decoration: none;
  background: transparent;
  border: 1px solid #3d5f9f;
  color: #3d5f9f;
  font-size: 1rem;
  min-height: calc(2.25rem);
  min-width: calc(2.25rem);
  padding: 0.25rem 1rem;
  margin-bottom: 1.25rem;

  &:hover {
    border-color: #364cf8;
    color: #1d5185;
    background: #fafafa;
  }
`;

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
  color: var(--cc-text-color);
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
    background-color: var(--cc-background-color);
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
    color: #36313d;
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
`;
