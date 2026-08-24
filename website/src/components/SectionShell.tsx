import type { ReactNode } from "react";

import { SectionHeading } from "@/src/components/SectionHeading";

interface SectionShellProps {
  readonly title: string;
  readonly lead: string;
  readonly artifact: ReactNode;
  readonly flip?: boolean;
}

export function SectionShell({ title, lead, artifact, flip = false }: SectionShellProps) {
  return (
    <section className="grid items-center gap-10 lg:grid-cols-2 lg:gap-14">
      <div className={`min-w-0 ${flip ? "lg:order-2" : ""}`}>
        <SectionHeading title={title} description={lead} />
      </div>
      <div className={`min-w-0 ${flip ? "lg:order-1" : ""}`}>{artifact}</div>
    </section>
  );
}
