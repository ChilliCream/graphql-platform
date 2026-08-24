import type { ReactNode } from "react";

import { Band, type BandSkin } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

interface NextStepsProps {
  readonly title: string;
  readonly text: ReactNode;
  readonly primaryLink: string;
  readonly primaryLinkText: string;
  readonly secondaryLink: string;
  readonly secondaryLinkText: string;
  /** Optional fine-print line under the buttons (e.g. a contact address). */
  readonly note?: ReactNode;
  readonly skin?: BandSkin;
  readonly className?: string;
}

export function NextStepsSection({
  title,
  text,
  primaryLink,
  primaryLinkText,
  secondaryLink,
  secondaryLinkText,
  note,
  skin = "bare",
  className = "py-20",
}: NextStepsProps) {
  return (
    <Band skin={skin} layout="centered" className={className}>
      <SectionHeading align="center" title={title} description={text} />
      <ButtonRow align="center" className="mt-8">
        <SolidButton href={primaryLink}>{primaryLinkText}</SolidButton>
        <OutlineButton href={secondaryLink}>{secondaryLinkText}</OutlineButton>
      </ButtonRow>
      {note && <p className="text-cc-ink-dim mt-6 font-mono text-xs">{note}</p>}
    </Band>
  );
}
