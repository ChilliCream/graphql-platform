import type { ReactNode } from "react";

import { PatternBand } from "@/src/components/PatternBand";
import { SectionHeading } from "@/src/components/SectionHeading";

interface ChapterBandProps {
  readonly title: ReactNode;
  readonly description?: ReactNode;
  readonly className?: string;
}

export function ChapterBand({ title, description, className = "" }: ChapterBandProps) {
  return (
    <PatternBand pattern="grid" contain={false} className={`border-y py-16 text-center sm:py-24 ${className}`}>
      <div className="mx-auto max-w-3xl px-5 sm:px-12">
        <SectionHeading align="center" size="lg" title={title} description={description} />
      </div>
    </PatternBand>
  );
}
