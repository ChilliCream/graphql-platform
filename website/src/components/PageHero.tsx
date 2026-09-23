import type { ReactNode } from "react";

export function PageHero({
  eyebrow,
  title,
  subtitle,
  teaser,
  children,
}: {
  eyebrow?: string;
  title: ReactNode;
  subtitle?: string;
  teaser?: ReactNode;
  children?: ReactNode;
}) {
  return (
    <section className="py-16 text-center sm:py-24">
      {eyebrow && (
        <div className="text-cc-ink-dim mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          {eyebrow}
        </div>
      )}
      <h1 className="text-cc-ink text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
        {title}
      </h1>
      {subtitle && (
        <p className="text-cc-ink mt-2 text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
          {subtitle}
        </p>
      )}
      {teaser && (
        <p className="text-cc-ink-dim mx-auto mt-6 max-w-2xl text-base sm:text-lg">
          {teaser}
        </p>
      )}
      {children}
    </section>
  );
}
