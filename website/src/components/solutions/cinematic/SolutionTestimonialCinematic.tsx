"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import {
  ActLabel,
  FrostedExplainer,
} from "@/components/redesign-system/cinematic";
import type { Testimonial } from "@/data/solutions/types";

interface SolutionTestimonialCinematicProps {
  readonly testimonials: readonly Testimonial[];
  readonly stepNumber: string;
}

// Cinematic variant of section 06 (Voices). The quote body is wrapped in
// a `<FrostedExplainer tone="dark">` plate so each pull-quote reads as
// the homepage's frosted-copy device. The attribution row stays bare so
// the plate doesn't bleed into the monogram + role lockup.
export const SolutionTestimonialCinematic: FC<
  SolutionTestimonialCinematicProps
> = ({ testimonials, stepNumber }) => {
  if (testimonials.length === 0) {
    return null;
  }

  return (
    <Band variant="default" ariaLabel="Voices" className="cc-band">
      <ActLabel n={stepNumber} name="Voices" />
      <div className="cc-sl-section cc-sl-testimonials">
        <div className="cc-sl-testimonials-inner">
          {testimonials.map((t, i) => (
            <figure key={`${t.company}-${i}`} className="cc-sl-testimonial">
              <FrostedExplainer
                tone="dark"
                maxWidth="100%"
                className="cc-sl-cin-quote-plate"
              >
                <blockquote className="cc-sl-testimonial-quote">
                  &ldquo;{t.quote}&rdquo;
                </blockquote>
              </FrostedExplainer>
              <figcaption className="cc-sl-testimonial-attribution">
                <div className="cc-sl-testimonial-mono">
                  {t.monogram ?? t.company.slice(0, 2).toUpperCase()}
                </div>
                <div className="cc-sl-testimonial-meta">
                  <span className="name">{t.title}</span>
                  <span>{t.company}</span>
                </div>
              </figcaption>
            </figure>
          ))}
        </div>
      </div>
    </Band>
  );
};
