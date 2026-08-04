import type { ReactNode } from "react";

interface SectionProps {
  title: string;
  children: ReactNode;
  className?: string;
}

export function Section({ title, children, className = "" }: SectionProps) {
  return (
    <section className={`py-16 ${className}`}>
      <h2 className="text-cc-ink mb-10 text-center text-3xl font-semibold tracking-tight sm:text-4xl">
        {title}
      </h2>
      {children}
    </section>
  );
}
