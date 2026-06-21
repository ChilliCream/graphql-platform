import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

interface NextStepsProps {
  title: string;
  text: ReactNode;
  primaryLink: string;
  primaryLinkText: string;
  secondaryLink: string;
  secondaryLinkText: string;
}

export function NextStepsSection({
  title,
  text,
  primaryLink,
  primaryLinkText,
  secondaryLink,
  secondaryLinkText,
}: NextStepsProps) {
  return (
    <section className="py-20 text-center">
      <h2 className="text-cc-ink text-3xl font-semibold tracking-tight sm:text-4xl">
        {title}
      </h2>
      <div className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base sm:text-lg">
        {text}
      </div>
      <div className="mt-8 flex flex-wrap justify-center gap-4">
        <SolidButton href={primaryLink}>{primaryLinkText}</SolidButton>
        <OutlineButton href={secondaryLink}>{secondaryLinkText}</OutlineButton>
      </div>
    </section>
  );
}
