import type { ReactNode } from "react";

import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

interface NextStepsProps {
  title: string;
  text: ReactNode;
  primaryLink: string;
  primaryLinkText: string;
  secondaryLink: string;
  secondaryLinkText: string;
  /** Optional fine-print line under the buttons (e.g. a contact address). */
  note?: ReactNode;
}

export function NextStepsSection({
  title,
  text,
  primaryLink,
  primaryLinkText,
  secondaryLink,
  secondaryLinkText,
  note,
}: NextStepsProps) {
  return (
    <Band skin="bare" layout="centered" className="py-20">
      <SectionHeading align="center" title={title} description={text} />
      <ButtonRow align="center" className="mt-8">
        <SolidButton href={primaryLink}>{primaryLinkText}</SolidButton>
        <OutlineButton href={secondaryLink}>{secondaryLinkText}</OutlineButton>
      </ButtonRow>
      {note && <p className="text-cc-ink-dim mt-6 font-mono text-xs">{note}</p>}
    </Band>
  );
}
