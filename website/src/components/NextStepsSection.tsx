import type { ReactNode } from "react";

import { Band, type BandSkin } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import type { AnalyticsEvent } from "@/src/helpers/analyticsEvents";

interface NextStepsProps {
  readonly title: string;
  readonly text: ReactNode;
  readonly primaryLink: string;
  readonly primaryLinkText: string;
  readonly secondaryLink: string;
  readonly secondaryLinkText: string;
  /** Key event reported when the primary button is clicked. */
  readonly primaryTrack?: AnalyticsEvent;
  /** Key event reported when the secondary button is clicked. */
  readonly secondaryTrack?: AnalyticsEvent;
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
  primaryTrack,
  secondaryTrack,
  note,
  skin = "bare",
  className = "py-20",
}: NextStepsProps) {
  return (
    <Band skin={skin} layout="centered" className={className}>
      <SectionHeading align="center" title={title} description={text} />
      <ButtonRow align="center" className="mt-8">
        <SolidButton href={primaryLink} track={primaryTrack}>
          {primaryLinkText}
        </SolidButton>
        <OutlineButton href={secondaryLink} track={secondaryTrack}>
          {secondaryLinkText}
        </OutlineButton>
      </ButtonRow>
      {note && <p className="text-cc-ink-dim mt-6 font-mono text-xs">{note}</p>}
    </Band>
  );
}
