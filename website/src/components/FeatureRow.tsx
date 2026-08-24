import type { ReactNode } from "react";

import { SectionHeading } from "@/src/components/SectionHeading";

interface FeatureRowProps {
  readonly title: ReactNode;
  readonly body: ReactNode;
  readonly visual: ReactNode;
  readonly reverse?: boolean;
  readonly children?: ReactNode;
}

export function FeatureRow({ title, body, visual, reverse = false, children }: FeatureRowProps) {
  return (
    <div className="grid items-center gap-10 lg:grid-cols-12 lg:gap-16">
      <div className={`min-w-0 lg:col-span-5 ${reverse ? "lg:order-2" : ""}`}>
        <SectionHeading title={title} description={body} />
        {children}
      </div>
      <div className={`min-w-0 lg:col-span-7 ${reverse ? "lg:order-1" : ""}`}>{visual}</div>
    </div>
  );
}
