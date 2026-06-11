import { SolidButton } from "@/src/design-system/Button";
import { CheckIcon } from "./CheckIcon";

export interface OfferingCallToAction {
  title: string;
  link: string;
}

export interface OfferingProps {
  title: string;
  description: string;
  perks: string[];
  callToAction?: OfferingCallToAction;
  headingLevel?: "h2" | "h3";
}

export function Offering({
  title,
  description,
  perks,
  callToAction,
  headingLevel: Heading = "h3",
}: OfferingProps) {
  return (
    <div className="flex flex-col rounded-xl border border-cc-card-border bg-cc-card-bg p-8 backdrop-blur-sm">
      <Heading className="text-2xl font-semibold text-cc-ink">{title}</Heading>
      <p className="mt-3 text-sm text-cc-ink-dim">{description}</p>
      <ul className="mt-6 flex flex-1 flex-col gap-2 text-sm text-cc-ink">
        {perks.map((perk) => (
          <li key={perk} className="flex items-start gap-2">
            <span className="flex h-5 flex-none items-center text-cc-accent">
              <CheckIcon />
            </span>
            <span>{perk}</span>
          </li>
        ))}
      </ul>
      {callToAction && (
        <SolidButton href={callToAction.link} className="mt-8">
          {callToAction.title}
        </SolidButton>
      )}
    </div>
  );
}
