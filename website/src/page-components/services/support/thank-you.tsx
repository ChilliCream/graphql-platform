"use client";

import { SiteLayout } from "@/components/layout";
import { ContentSection, Link, SEO } from "@/components/misc";
import {
  MostRecentBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";
import { RecentBlogPost } from "@/components/widgets/most-recent-blog-posts-section";
import { FONT_FAMILY_HEADING, THEME_COLORS } from "@/style";
import React, { FC } from "react";
import styled from "styled-components";

interface SupportThankYouPageProps {
  recentPosts?: RecentBlogPost[];
}

const SupportThankYouPage: FC<SupportThankYouPageProps> = ({ recentPosts }) => {
  return (
    <SiteLayout>
      <SEO title="Thank You - Support Request Submitted" />
      <ContentSection noBackground titleSpace="large">
        <ThankYouContainer>
          <ThankYouTitle>Thanks for reaching out!</ThankYouTitle>

          <ThankYouMessage>
            We've received your support request and our team will review it
            shortly. You should expect to hear back from us within the next
            business day.
          </ThankYouMessage>

          <ThankYouMessage>
            In the meantime, you might find these resources helpful:
          </ThankYouMessage>

          <ActionButtons>
            <ActionButton to="/docs">Documentation</ActionButton>
            <ActionButton to="/services/support">Support Plans</ActionButton>
            <ActionButton to="https://slack.chillicream.com/">
              Join Our Slack
            </ActionButton>
          </ActionButtons>
        </ThankYouContainer>
      </ContentSection>
      <NewsletterSection />
      <MostRecentBlogPostsSection posts={recentPosts} />
    </SiteLayout>
  );
};

const ThankYouContainer = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  max-width: 600px;
  margin: 0 auto;
  padding: 40px;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--box-border-radius);
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    to right bottom,
    #379dc83d,
    #2b80ad3d,
    #2263903d,
    #1a48743d,
    #112f573d
  );
`;

const ThankYouTitle = styled.h2`
  margin: 0 0 24px 0;
  color: ${THEME_COLORS.heading};
`;

const ThankYouMessage = styled.p.attrs({
  className: "text-2",
})`
  margin: 0 0 16px 0;
  color: ${THEME_COLORS.text};
  line-height: 1.6;
`;

const ActionButtons = styled.div`
  display: flex;
  flex-direction: column;
  gap: 16px;
  margin-top: 32px;
  width: 100%;

  @media only screen and (min-width: 768px) {
    flex-direction: row;
    justify-content: center;
    gap: 24px;
  }
`;

const ActionButton = styled(Link)`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  border-radius: var(--border-radius);
  height: 48px;
  padding: 0 24px;
  border: 2px solid ${THEME_COLORS.primaryButtonBorder};
  color: ${THEME_COLORS.primaryButtonText};
  background-color: ${THEME_COLORS.primaryButton};
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 1rem;
  text-decoration: none;
  font-weight: 500;
  text-align: center;
  white-space: nowrap;
  transition: background-color 0.2s ease-in-out, border-color 0.2s ease-in-out,
    color 0.2s ease-in-out;

  :hover {
    border-color: ${THEME_COLORS.primaryButtonBorder};
    color: ${THEME_COLORS.primaryButtonHoverText};
    background-color: ${THEME_COLORS.primaryButtonHover};
  }

  @media only screen and (min-width: 768px) {
    flex: 0 1 auto;
    min-width: 180px;
  }
`;

export default SupportThankYouPage;
