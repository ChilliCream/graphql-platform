import { ButtonRow } from "@/src/components/ButtonRow";
import { IconFeatureCard } from "@/src/components/IconFeatureCard";
import { MarketingHero } from "@/src/components/MarketingHero";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CalendarIcon } from "@/src/icons/CalendarIcon";
import { CallIcon } from "@/src/icons/CallIcon";
import { ChannelIcon } from "@/src/icons/ChannelIcon";

const SCENARIOS = [
  {
    label: "A quick question",
    title: "Message us, hear back in minutes",
    copy: "You hit an exception you've never seen before. Instead of debugging for hours, you send us a message and get an answer in no time.",
    Icon: ChannelIcon,
  },
  {
    label: "Production is down",
    title: "We get on a call until you're back",
    copy: "Something critical breaks. You email us and open a ticket, and a core team member jumps on a call, staying with you until production is running again.",
    Icon: CallIcon,
  },
  {
    label: "A second opinion",
    title: "Book time with the team",
    copy: "Planning a migration or reviewing a schema? Schedule a session and we'll work through it together, live, with the people who build the platform.",
    Icon: CalendarIcon,
  },
];

/**
 * The support hero: a name on the other side of the channel, framed by the three
 * ways support actually plays out (a question, an outage, a planned session).
 */
export function SupportHero() {
  return (
    <MarketingHero
      eyebrow="ChilliCream Support"
      title="Support from the people who build the platform."
      lead={
        <>
          However you reach us, you&rsquo;re working with the core engineers who
          build Hot Chocolate, Fusion and Nitro, not a first-line queue. Here is
          what that looks like in practice.
        </>
      }
      actions={
        <ButtonRow align="center">
          <SolidButton href="#plans">See the four plans</SolidButton>
          <OutlineButton href="/services/support/contact">
            Talk to us
          </OutlineButton>
        </ButtonRow>
      }
    >
      <div className="mt-14 grid gap-4 md:grid-cols-3">
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
      </div>
    </MarketingHero>
  );
}
