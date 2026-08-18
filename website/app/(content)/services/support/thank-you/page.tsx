import type { Metadata } from "next";

import { PageHero } from "@/src/components/PageHero";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata: Metadata = {
  ...pageMetadata({
    title: "Message Received",
    description:
      "Your message has been sent to ChilliCream. Explore the Hot Chocolate documentation or join the community Slack while you wait for a reply.",
    path: "/services/support/thank-you",
  }),
  robots: { index: false, follow: true },
};

export default function ThankYouPage() {
  return (
    <>
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
