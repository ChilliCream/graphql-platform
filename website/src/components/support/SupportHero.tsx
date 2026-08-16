import { ButtonRow } from "@/src/components/ButtonRow";
import { CardGrid } from "@/src/components/CardGrid";
import { IconFeatureCard } from "@/src/components/IconFeatureCard";
import { MarketingHero } from "@/src/components/MarketingHero";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CalendarIcon } from "@/src/icons/CalendarIcon";
import { CallIcon } from "@/src/icons/CallIcon";
import { ChannelIcon } from "@/src/icons/ChannelIcon";

const SCENARIOS = [
  {
    label: "Technical questions",
    title: "Skip the first-line queue",
    copy: "Bring an exception, a confusing behavior, or a focused implementation question to the people who know the code behind your stack.",
    Icon: ChannelIcon,
  },
  {
    label: "Production incidents",
    title: "Escalate with a defined response time",
    copy: "Open a critical incident through your plan's support channel and track it against the incident allowance and response time in your agreement.",
    Icon: CallIcon,
  },
  {
    label: "Ongoing operations",
    title: "Keep the product team close",
    copy: "Use the private channels, issue tracking, and status reviews included with your plan to keep technical context available as your GraphQL platform evolves.",
    Icon: CalendarIcon,
  },
];

/**
 * The support hero, framed by technical questions, production incidents, and
 * ongoing operational context.
 */
export function SupportHero() {
  return (
    <MarketingHero
      eyebrow="GraphQL support plans"
      title="Support from the people who build the platform."
      lead={
        <>
          Work with the engineers who build Hot Chocolate, Fusion, and Nitro,
          not a first-line queue. Choose the plan with the incident allowances,
          response times, and support channels your production team needs.
        </>
      }
      actions={
        <ButtonRow align="center">
          <SolidButton href="#plans">Compare support plans</SolidButton>
          <OutlineButton href="/services/support/contact">
            Discuss support needs
          </OutlineButton>
        </ButtonRow>
      }
    >
      <div className="mt-14">
        <CardGrid cols={3} gap={4}>
          {SCENARIOS.map((scenario) => (
            <IconFeatureCard
              key={scenario.label}
              eyebrow={scenario.label}
              icon={<scenario.Icon />}
              title={scenario.title}
              copy={scenario.copy}
              size="lg"
              align="center"
            />
          ))}
        </CardGrid>
      </div>
    </MarketingHero>
  );
}
