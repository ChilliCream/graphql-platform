import type { ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { SectionHeading } from "@/src/components/SectionHeading";

export const GRADIENT = "linear-gradient(90deg, #5eead4, #16b9e4)";

interface SectionProps {
  readonly id: string;
  readonly children: ReactNode;
}

export function Section({ id, children }: SectionProps) {
  return (
    <section id={id} className="border-cc-card-border scroll-mt-24 border-t">
      <PageSection maxWidth="7xl" className="py-16 sm:py-24">
        {children}
      </PageSection>
    </section>
  );
}

interface IntroProps {
  readonly eyebrow?: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

export function Intro({ eyebrow, title, children }: IntroProps) {
  return (
    <div className="max-w-2xl">
      <SectionHeading eyebrow={eyebrow} title={title} />
      {children && (
        <div className="text-cc-ink mt-5 space-y-4 text-base">{children}</div>
      )}
    </div>
  );
}

export function SubHeading({
  id,
  children,
}: {
  readonly id: string;
  readonly children: ReactNode;
}) {
  return (
    <h3
      id={id}
      className="font-heading text-cc-heading text-h5 scroll-mt-24 font-semibold text-balance"
    >
      {children}
    </h3>
  );
}
