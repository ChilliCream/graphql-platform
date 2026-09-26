import type { ReactNode } from "react";

import { SectionHeading } from "@/src/components/SectionHeading";

interface FeatureRowProps {
  readonly title: ReactNode;
  readonly body: ReactNode;
  readonly visual: ReactNode;
  readonly reverse?: boolean;
  /** Widens the visual column to text 4 / visual 8 from xl up; default stays 5/7. */
  readonly wide?: boolean;
  readonly children?: ReactNode;
}

export function FeatureRow({
  title,
  body,
  visual,
  reverse = false,
  wide = false,
  children,
}: FeatureRowProps) {
  const textSpan = wide ? "lg:col-span-5 xl:col-span-4" : "lg:col-span-5";
  const visualSpan = wide ? "lg:col-span-7 xl:col-span-8" : "lg:col-span-7";

  return (
    <div className="grid items-center gap-10 lg:grid-cols-12 lg:gap-16">
      <div className={`min-w-0 ${textSpan} ${reverse ? "lg:order-2" : ""}`}>
        <SectionHeading title={title} description={body} />
        {children}
      </div>
      <div className={`min-w-0 ${visualSpan} ${reverse ? "lg:order-1" : ""}`}>
        {visual}
      </div>
    </div>
  );
}
