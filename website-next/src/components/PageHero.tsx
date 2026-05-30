export function PageHero({
  eyebrow,
  title,
  subtitle,
  teaser,
}: {
  eyebrow?: string;
  title: string;
  subtitle?: string;
  teaser?: string;
}) {
  return (
    <section className="py-16 text-center sm:py-24">
      {eyebrow && (
        <div className="mb-3 font-mono text-xs font-semibold uppercase tracking-widest text-cc-ink-dim">
          {eyebrow}
        </div>
      )}
      <h1 className="text-5xl font-semibold leading-tight tracking-tight text-cc-ink sm:text-6xl lg:text-7xl">
        {title}
      </h1>
      {subtitle && (
        <p className="mt-2 text-5xl font-semibold leading-tight tracking-tight text-cc-ink sm:text-6xl lg:text-7xl">
          {subtitle}
        </p>
      )}
      {teaser && (
        <p className="mx-auto mt-6 max-w-2xl text-base text-cc-ink-dim sm:text-lg">
          {teaser}
        </p>
      )}
    </section>
  );
}
