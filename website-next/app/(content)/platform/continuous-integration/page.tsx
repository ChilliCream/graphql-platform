import { ContentSection } from "@/src/components/ContentSection";
import { NextStepsSection } from "@/src/components/NextStepsSection";
import { PageHero } from "@/src/components/PageHero";
import { SolidButton } from "@/src/design-system/Button";

export default function ContinuousIntegrationPage() {
  return (
    <>
      <PageHero
        title="Innovate with Confidence"
        teaser="Ensure stability by validating changes and preventing breaking changes. A centralized registry keeps teams aligned, managing clients and APIs in one place, and supports safe client development. Stay informed with auto-generated changelogs and standardized workflows for safe schema evolution."
      />
      <div className="flex justify-center">
        <SolidButton href="/docs/nitro/apis/fusion">Get Started</SolidButton>
      </div>
      <ContentSection
        title="Connect Your Ecosystem"
        text="Easily integrate your CI/CD pipelines with our Nitro CLI. Whether you're using GitHub Actions, Azure DevOps, or other tools, Nitro CLI simplifies interactions with our service. This seamless integration fits into any deployment environment, allowing you to maintain your existing workflow without requiring significant changes."
        imageSrc="/images/continuous-integration/connect.png"
        imageAlt="Connect Your Ecosystem"
        imageMaxWidth={1200}
      />
      <ContentSection
        title="Adapt to Your Setup"
        text="Align your deployment process with your specific needs by defining environments such as development, QA, and production. Each environment can have its own active clients, schema versions, and history, giving you full control over your workflow."
        imageSrc="/images/continuous-integration/design.png"
        imageAlt="Adapt to Your Setup"
        imageMaxWidth={1200}
      />
      <ContentSection
        title="Validate Early, Succeed Sooner"
        text="Identify GraphQL schema issues right from the start. Early validation provides immediate feedback, ensuring your project stays on track from day one. This proactive approach allows you to catch and fix issues before deployment, preventing potential problems down the line."
      />
      <ContentSection
        title="Deploy with Zero Disruption"
        text="Deploy your updates with confidence, knowing that your changes won't break any client or schema. Our system ensures that once you merge your pull request, all changes are validated for compatibility. If any breaking changes are detected, the deployment is stopped to protect your production environment."
        imageSrc="/images/continuous-integration/deploy.png"
        imageAlt="Deploy with Zero Disruption"
        imageMaxWidth={1200}
      />
      <ContentSection
        title="Document, Track, and Review"
        text="Monitoring and managing schema changes is key to maintaining your API's integrity and functionality. Our platform provides tools to track every change made, along with a detailed version history. You can easily see what was changed, when it was changed, and why."
        imageSrc="/images/continuous-integration/track.png"
        imageAlt="Document, Track, and Review"
        imageMaxWidth={1200}
      />
      <NextStepsSection
        title="Ready To Deploy?"
        text="Prepare for deployment with a process optimized for efficiency and precision."
        primaryLink="/services/support/contact?subject=Schedule+a+Demo"
        primaryLinkText="Book a Demo"
        secondaryLink="https://nitro.chillicream.com"
        secondaryLinkText="Launch"
      />
    </>
  );
}
