import { PageHero } from "@/src/components/PageHero";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export default function ThankYouPage() {
  return (
    <>
      <PageHero
        title="Thank You!"
        teaser="We've received your message and will be in touch shortly. In the meantime, browse our docs or join the community Slack."
      />
      <div className="flex flex-wrap justify-center gap-4 pb-16">
        <SolidButton href="/docs/hotchocolate">Read the Docs</SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Join Slack
        </OutlineButton>
      </div>
    </>
  );
}
