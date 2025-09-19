import React, { FC } from "react";
import styled from "styled-components";

import { SiteLayout } from "@/components/layout";
import {
  ContentSection,
  Hero,
  HeroLink,
  HeroTeaser,
  HeroTitleFirst,
  NextStepsContentSection,
  SEO,
} from "@/components/misc";
import {
  MostRecentBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";

// Images
import {
  CONTINUOUS_INTEGRATION_CONNECT_IMAGE_WIDTH,
  CONTINUOUS_INTEGRATION_DEPLOY_IMAGE_WIDTH,
  CONTINUOUS_INTEGRATION_DESIGN_IMAGE_WIDTH,
  CONTINUOUS_INTEGRATION_TRACK_IMAGE_WIDTH,
  ContinuousIntegrationConnectImage,
  ContinuousIntegrationDeployImage,
  ContinuousIntegrationDesignImage,
  ContinuousIntegrationTrackImage,
  SHIP_WITH_CONFIDENCE_IMAGE_WIDTH,
  ShipWithConfidenceImage,
} from "../../components/images";

const ContinuousIntegrationPage: FC = () => {
  return (
    <SiteLayout>
      <SEO title="Continuous Integration" />
      <ContinuousIntegrationPageHero>
        <HeroTitleFirst>Innovate with Confidence</HeroTitleFirst>
        <HeroTeaser>
          Ensure stability by validating changes and preventing breaking
          changes. A centralized registry keeps teams aligned, managing clients
          and APIs in one place, and supports safe client development. Stay
          informed with auto-generated changelogs and standardized workflows for
          safe schema evolution. Collaborate efficiently with validated
          modifications and integrated approval processes. Integrate schema
          composition, change validation, and approvals into CI/CD pipelines for
          smooth deployments. Phase out obsolete fields and track code changes
          from pull request to deployment. Integrate effortlessly with GitHub
          and DevOps tools for precise and confident API management.
        </HeroTeaser>
        <HeroLink to="/docs/nitro/apis/fusion">Get Started</HeroLink>
      </ContinuousIntegrationPageHero>
      <ContentSection
        title="Connect Your Ecosystem"
        text="
          Easily integrate your CI/CD pipelines with our Nitro CLI.
          Whether you're using GitHub Actions, Azure DevOps, or
          other tools, Nitro CLI simplifies interactions with our service.
          This seamless integration fits into any deployment environment,
          allowing you to maintain your existing workflow without
          requiring significant changes.
        "
        image={<ContinuousIntegrationConnectImage />}
        imagePosition="bottom"
        imageWidth={CONTINUOUS_INTEGRATION_CONNECT_IMAGE_WIDTH}
      />
      <ContentSection
        title="Adapt to Your Setup"
        text="
          Align your deployment process with your specific needs by
          defining environments such as development, QA, and production.
          Each environment can have its own active clients, schema
          versions, and history, giving you full control over your
          workflow.
          <br />
          Our platform allows you to visualize and manage your entire
          deployment pipeline, ensuring each stage matches your
          operational requirements.
        "
        image={<ContinuousIntegrationDesignImage />}
        imagePosition="bottom"
        imageWidth={CONTINUOUS_INTEGRATION_DESIGN_IMAGE_WIDTH}
      />
      <ContentSection
        title={
          <>
            Validate Early,
            <br />
            Succeed Sooner
          </>
        }
        text="
          Identify GraphQL schema issues right from the start. Early
          validation provides immediate feedback, ensuring your project
          stays on track from day one. This proactive approach allows
          you to catch and fix issues before deployment, preventing
          potential problems down the line.
        "
        image={<ShipWithConfidenceImage />}
        imagePosition="bottom"
        imageWidth={SHIP_WITH_CONFIDENCE_IMAGE_WIDTH}
      />
      <ContentSection
        title={<>Deploy with Zero Disruption</>}
        text="
          Deploy your updates with confidence, knowing that your changes
          won't break any client or schema. Our system ensures that once
          you merge your pull request, all changes are validated for
          compatibility. If any breaking changes are detected, the
          deployment is stopped to protect your production environment.
          This approach guarantees a reliable deployment process,
          allowing your team to innovate without disrupting existing
          clients.
        "
        image={<ContinuousIntegrationDeployImage />}
        imagePosition="bottom"
        imageWidth={CONTINUOUS_INTEGRATION_DEPLOY_IMAGE_WIDTH}
      />
      <ContentSection
        title={
          <>
            Document, Track,
            <br />
            and Review
          </>
        }
        text="
          Monitoring and managing schema changes is key to maintaining
          your API's integrity and functionality. Our platform provides
          tools to track every change made, along with a detailed version
          history. You can easily see what was changed, when it was
          changed, and why. This clarity helps you manage schema
          evolution, spot potential issues early, and keep your API
          running smoothly.
        "
        image={<ContinuousIntegrationTrackImage />}
        imagePosition="bottom"
        imageWidth={CONTINUOUS_INTEGRATION_TRACK_IMAGE_WIDTH}
      />
      <NextStepsContentSection
        title="Ready To Deploy?"
        text="
          Prepare for deployment with a process optimized for efficiency
          and precision. Ensure your project is ready for a smooth rollout
          and impactful launch.
        "
        primaryLink="mailto:contact@chillicream.com?subject=Demo"
        primaryLinkText="Book a Demo"
      />
      <NewsletterSection />
      <MostRecentBlogPostsSection />
    </SiteLayout>
  );
};

export default ContinuousIntegrationPage;

const ContinuousIntegrationPageHero = styled(Hero)`
  @media only screen and (min-width: 992px) {
    padding-bottom: 140px;
  }
`;
