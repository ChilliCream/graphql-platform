import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Messaging Hero candidates",
  robots: { index: false, follow: false },
};

const HEROES = [
  {
    v: 1,
    name: "Boot-up cascade",
    note: "services light up, a message ripples across",
  },
  {
    v: 6,
    name: "Ember network",
    note: "the ember slide with the messaging PCB network integrated",
  },
  {
    v: 7,
    name: "Why (site style)",
    note: "left/right layout, on-brand cool theme, blue+coral network panel",
  },
];

export default function MessagingHeroHubPage() {
  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-12 px-5 py-16 sm:px-12 sm:py-24">
      <header>
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          Internal · messaging hero
        </p>
        <h1 className="font-heading text-h2 text-cc-heading mt-5 font-semibold tracking-tight">
          Hero candidates
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-[1.1rem] leading-relaxed">
          Four board treatments for the messaging hero, same headline and copy.
          Use the version switcher to flip between them.
        </p>
      </header>

      <section className="grid gap-5 sm:grid-cols-2">
        {HEROES.map((hero) => (
          <Link
            key={hero.v}
            href={`/messaging-graphic/hero/v${hero.v}`}
            className="group border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex flex-col gap-3 rounded-xl border p-6 no-underline backdrop-blur-sm transition-colors"
          >
            <span className="border-cc-card-border bg-cc-surface text-cc-heading flex h-9 w-9 items-center justify-center rounded-full border font-mono text-[0.82rem] font-semibold tabular-nums">
              v{hero.v}
            </span>
            <p className="text-cc-heading group-hover:text-cc-accent font-heading text-h5 font-semibold tracking-tight transition-colors">
              {hero.name}
            </p>
            <p className="text-cc-ink text-sm">{hero.note}</p>
            <span className="text-cc-accent mt-auto text-[0.82rem] font-medium">
              Open hero →
            </span>
          </Link>
        ))}
      </section>
    </div>
  );
}
