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
    <div className="border-cc-card-border bg-cc-card-bg flex flex-col rounded-xl border p-8 backdrop-blur-sm">
      <Heading className="text-cc-ink text-2xl font-semibold">{title}</Heading>
      <p className="text-cc-ink-dim mt-3 text-sm">{description}</p>
      <ul className="text-cc-ink mt-6 flex flex-1 flex-col gap-2 text-sm">
        {perks.map((perk) => (
          <li key={perk} className="flex items-start gap-2">
            <span className="text-cc-accent flex h-5 flex-none items-center">
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
