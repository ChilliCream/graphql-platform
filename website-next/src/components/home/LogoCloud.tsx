import { GalaxusLogo } from "@/src/icons/GalaxusLogo";
import { MicrosoftLogo } from "@/src/icons/MicrosoftLogo";
import { SwissLifeLogo } from "@/src/icons/SwissLifeLogo";

/**
 * Social-proof logo cloud beneath the hero: a small uppercase label over a row
 * of customer wordmarks. The logos are inlined single-path SVGs that inherit
 * the near-white heading color via `currentColor`, so they read as a calm,
 * monochrome band rather than competing brand colors.
 */
export function LogoCloud() {
  return (
    <section className="mx-auto max-w-7xl px-5 py-12 text-center sm:px-12 sm:py-16">
      <p className="text-cc-ink-dim font-mono text-xs tracking-[0.2em] uppercase">
        Trusted by Enterprises
      </p>
      <div className="text-cc-heading mt-10 flex flex-wrap items-center justify-center gap-x-16 gap-y-10 sm:mt-14 sm:gap-x-24 lg:grid lg:grid-cols-3 lg:place-items-center lg:gap-x-8">
        <GalaxusLogo className="h-9 w-auto sm:h-11" />
        <SwissLifeLogo className="h-18 w-auto sm:h-22" />
        <MicrosoftLogo className="h-9 w-auto sm:h-11" />
      </div>
    </section>
  );
}
