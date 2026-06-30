import type { ReactNode } from "react";

import {
  CONSULTING_MAILTO,
  CONTACT_FORM,
} from "@/src/components/advisory/advisoryLinks";
import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { CheckIcon } from "@/src/components/CheckIcon";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * The closing contact band: the pitch and the engagement facts on one side, the
 * contact CTAs on the other.
 */
export function ContactBand() {
  return (
    <Band
      skin="card"
      className="mt-20 mb-8 sm:mt-28"
      labelledBy="contact-heading"
      main={
        <div>
          <SectionHeading
            eyebrow="Ready when you are"
            title="One call is usually enough to know."
            titleId="contact-heading"
            description="Tell us what you are building. You walk us through it, we ask the questions, and you leave with a clear next step. If we are not the right fit, we will tell you."
          />
          <ContactSpec />
        </div>
      }
      aside={
        <div>
          <ButtonRow align="stacked">
            <SolidButton href={CONTACT_FORM} className="w-full">
              Talk to us
            </SolidButton>
            <OutlineButton href={CONSULTING_MAILTO} className="w-full">
              Email us
            </OutlineButton>
          </ButtonRow>
        </div>
      }
    />
  );
}

function ContactSpec() {
  const items: readonly {
    readonly label: string;
    readonly value: ReactNode;
  }[] = [
    { label: "Consulting", value: "Packages of hours" },
    { label: "Contracting", value: "Scoped statement of work" },
    { label: "NDA", value: "Mutual NDA on request" },
    { label: "Start", value: "Often the same week" },
  ];
  return (
    <ul className="mt-6 grid gap-3 sm:grid-cols-2">
      {items.map((item) => (
        <li key={item.label} className="flex items-start gap-3">
          <span className="text-cc-accent mt-[5px] flex-none">
            <CheckIcon />
          </span>
          <span className="text-cc-ink text-sm">
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              {item.label}
            </span>
            <br />
            {item.value}
          </span>
        </li>
      ))}
    </ul>
  );
}
