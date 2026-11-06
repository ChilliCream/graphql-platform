import type { Metadata } from "next";

import { PageHero } from "@/src/components/PageHero";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { pageMetadata } from "@/src/helpers/pageMetadata";

const PAGE = {
  title: "Message Received",
  description:
    "Your message has been sent to ChilliCream. Explore the Hot Chocolate documentation or join the community Slack while you wait for a reply.",
  path: "/services/support/thank-you",
} as const;

export const metadata: Metadata = {
  ...pageMetadata(PAGE),
  robots: { index: false, follow: true },
};

export default function ThankYouPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Support", path: "/services/support" },
          { name: "Thank You" },
        ]}
      />
      <PageHero
        eyebrow="Request sent"
        title="Thank you!"
        teaser="We've received your message and will be in touch shortly. In the meantime, explore the Hot Chocolate documentation or ask the community in Slack."
      />
      <div className="flex flex-wrap justify-center gap-4 pb-16">
        <SolidButton href="/docs/hotchocolate">
          Explore Hot Chocolate docs
        </SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Join community Slack
        </OutlineButton>
      </div>
    </>
  );
}
