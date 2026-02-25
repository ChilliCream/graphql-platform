"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { SiteLayout } from "@/components/layout";
import {
  ContentSection,
  Hero,
  HeroTeaser,
  HeroTitleFirst,
  HeroTitleSecond,
  Plans,
  SEO,
} from "@/components/misc";
import {
  MostRecentBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";
import { RecentBlogPost } from "@/components/widgets/most-recent-blog-posts-section";
import { THEME_COLORS } from "@/style";

interface PricingPageProps {
  recentPosts?: RecentBlogPost[];
}

const PricingPage: FC<PricingPageProps> = ({ recentPosts }) => {
  return (
    <SiteLayout>
      <SEO title="Pricing" />
      <Hero>
        <HeroTitleFirst>Nitro Plans</HeroTitleFirst>
        <HeroTitleSecond>For any Scale</HeroTitleSecond>
        <HeroTeaser>Choose the right plan for your Team.</HeroTeaser>
      </Hero>
      <ContentSection
        title="Nitro Platform"
        noBackground
        titleSpace="large"
      >
        <Plans
          plans={[
            {
              title: "Shared",
              price: 0,
              period: "month",
              fromPrice: true,
              description: "Hosted on shared infrastructure",
              features: [
                "Schema & Client Registry",
                "GraphQL IDE",
                "Fusion Management",
                "Document Sharing",
                "OpenTelemetry",
                "Github or Google Login",
                "Usage-based pricing"
              ],
              ctaText: "Start for Free",
              ctaLink: "https://nitro.chillicream.com",
            },
            {
              title: "Scale",
              price: 430,
              period: "month",
              fromPrice: true,
              description: "Dedicated Infrastructure with advanced features",
              features: [
                "Dedicated Infrastructure",
                "Dedicated DBs",
                "Choose your region",
                "Single Sign-On",
                "Scheduled Maintenance",
                "Volume Based Pricing",
                "Bring Your Own Domain*",
                "VNET Peering*",
                "BYOC*",
              ],
              ctaText: "Contact Sales",
              ctaLink: "mailto:contact@chillicream.com?subject=Scale%20Plan",
            },
            {
              title: "Enterprise",
              price: "custom",
              description: "Everything in Scale plus enterprise features",
              features: [
                "Everything in Scale",
                "On-Premise Option",
                "Source Code Access",
                "Dedicated Account Manager",
                "24/7 Support"
              ],
              ctaText: "Contact Sales",
              ctaLink: "mailto:contact@chillicream.com?subject=Enterprise%20Plan",
            },
          ]}
        />
        <p style={{ marginTop: "2rem", fontSize: "0.9rem", color: "#666", textAlign: "center" }}>
          *Available in Platinum and Enterprise plans
        </p>
      </ContentSection>

      <ContentSection
        title="Support Plans"
        noBackground
        titleSpace="large"
      >
        <Plans
          plans={[
            {
              title: "Professional",
              price: 450,
              period: "month",
              description: "Essential support for growing teams ($5,000/year)",
              features: [
                "2 Critical Incidents",
                "24h Business Hour SLA",
                "Private Slack Channel",
                "Access to expert engineers"
              ],
              ctaText: "Contact Sales",
              ctaLink: "mailto:contact@chillicream.com?subject=Professional%20Support",
            },
            {
              title: "Business",
              price: 1300,
              period: "month",
              description: "Comprehensive support for business teams ($15,000/year)",
              features: [
                "Unlimited Critical Incidents",
                "24h Business Hour SLA",
                "Private Slack Channel",
                "Access to expert engineers",
                "4 Non-Critical Incidents",
                "Issue Tracking Board",
                "Email Support",
              ],
              ctaText: "Contact Sales",
              ctaLink: "mailto:contact@chillicream.com?subject=Business%20Support",
            },
            {
              title: "Enterprise",
              price: "custom",
              description: "Premium support with dedicated resources",
              features: [
                "Unlimited Critical Incidents",
                "Custom SLA",
                "Private Slack Channel",
                "Access to expert engineers",
                "10 Non-Critical Incidents",
                "Issue Tracking Board",
                "Email Support",
                "Phone Support",
                "Dedicated Account Manager",
                "Quarterly Status Reviews",
                "Source Code Access",
                "Nitro License included"
              ],
              ctaText: "Contact Sales",
              ctaLink: "mailto:contact@chillicream.com?subject=Enterprise%20Support",
            },
          ]}
        />
      </ContentSection>

      <ContentSection
        title="Add-On Services"
        noBackground
        titleSpace="large"
      >
        <Plans
          plans={[
            {
              title: "Advisory Services",
              price: 300,
              period: "hour",
              fromPrice: true,
              description: "Expert guidance and consulting for your GraphQL implementation",
              features: [
                "GraphQL expertise",
                "Architecture reviews",
                "Performance optimization",
                "Best practices guidance",
                "Custom solution design"
              ],
              ctaText: "Contact Sales",
              ctaLink: "mailto:contact@chillicream.com?subject=Advisory%20Services",
            },
            {
              title: "Private Workshops",
              price: 8000,
              period: "day",
              description: "Customized training sessions for your team",
              features: [
                "Full-day workshop",
                "Customized curriculum",
                "Hands-on exercises",
                "Expert instructors",
                "Post-workshop support",
                "Materials included"
              ],
              ctaText: "Contact Sales",
              ctaLink: "mailto:contact@chillicream.com?subject=Private%20Workshops",
            },
            {
              title: "Monthly Sessions",
              price: 15000,
              period: "year",
              description: "Regular collaboration sessions with our expert team",
              features: [
                "Monthly 2-hour sessions",
                "Direct access to experts",
                "Progress tracking",
                "Strategic planning",
                "Problem-solving support",
                "Knowledge transfer"
              ],
              ctaText: "Contact Sales",
              ctaLink: "mailto:contact@chillicream.com?subject=Monthly%20Collaboration%20Sessions",
            },
          ]}
        />

        <p style={{
          textAlign: "center",
          fontSize: "0.9rem",
          color: "#666",
          fontStyle: "italic",
          marginTop: "2rem"
        }}>
          Add-on services are optional for all plans and can be combined with any Nitro or Support plan.
        </p>
      </ContentSection>

      <ContentSection
        title="Open Source Libraries"
        noBackground
        titleSpace="large"
      >
        <OpenSourceContainer>
          <OpenSourceTitle>
            All Our Core Libraries Are Free & Open Source
          </OpenSourceTitle>
          <OpenSourceDescription>
            <strong>HotChocolate</strong>, <strong>StrawberryShake</strong>, and <strong>GreenDonut</strong> are MIT licensed and completely free to use in any project, commercial or otherwise.
          </OpenSourceDescription>
          <OpenSourceButtons>
            <OpenSourceButton
              href="https://github.com/ChilliCream/graphql-platform"
              target="_blank"
              rel="noopener noreferrer"
            >
              View on GitHub
            </OpenSourceButton>
            <OpenSourceButton
              href="https://github.com/ChilliCream/graphql-platform/blob/main/LICENSE"
              target="_blank"
              rel="noopener noreferrer"
            >
              MIT License
            </OpenSourceButton>
          </OpenSourceButtons>
        </OpenSourceContainer>
      </ContentSection>


      <NewsletterSection />
      <MostRecentBlogPostsSection posts={recentPosts} />
    </SiteLayout>
  );
};

export default PricingPage;

const OpenSourceContainer = styled.div`
  max-width: 800px;
  margin: 0 auto;
  text-align: center;
  padding: 2rem;
  border: 1px solid #37353f;
  border-radius: var(--box-border-radius);
  backdrop-filter: blur(2px);
  background-image: radial-gradient(
    ellipse at bottom,
    #15113599 0%,
    #0c0c2399 40%
  );
  box-shadow: 0 0 120px 60px #fdfdfd12;
`;

const OpenSourceTitle = styled.h3`
  font-size: 1.5rem;
  margin-bottom: 1rem;
  color: ${THEME_COLORS.heading};
`;

const OpenSourceDescription = styled.p`
  font-size: 1.1rem;
  line-height: 1.6;
  color: ${THEME_COLORS.text};
  margin-bottom: 1.5rem;
`;

const OpenSourceButtons = styled.div`
  display: flex;
  justify-content: center;
  gap: 2rem;
  flex-wrap: wrap;
`;

const OpenSourceButton = styled.a`
  display: inline-block;
  padding: 0.75rem 1.5rem;
  background-color: transparent;
  border: 1px solid #6b6775;
  color: ${THEME_COLORS.heading};
  text-decoration: none;
  border-radius: 4px;
  font-weight: 500;
  transition: all 0.2s ease;

  &:hover {
    border-color: ${THEME_COLORS.heading};
    color: ${THEME_COLORS.heading};
  }
`;
