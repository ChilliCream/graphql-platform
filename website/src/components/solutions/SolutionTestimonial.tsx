"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import type { Testimonial } from "@/data/solutions/types";

interface SolutionTestimonialProps {
  readonly testimonials: readonly Testimonial[];
  readonly stepNumber: string;
}

// Section 06: pull quotes. One treatment for every quote: large italic
// display body, monogram + role + company underneath. When two are
// supplied they stack at full text-column width so each occupies the page
// in turn rather than being cropped into half-tiles. Operational, not
// aspirational.
export const SolutionTestimonial: FC<SolutionTestimonialProps> = ({
  testimonials,
  stepNumber,
}) => {
  if (testimonials.length === 0) {
    return null;
  }

  return (
    <Band variant="default" ariaLabel="Operators">
      <div className="cc-sl-section cc-sl-testimonials">
        <div className="cc-section-label">
          <span className="num">{stepNumber}</span> Operators
        </div>
        <div className="cc-sl-testimonials-inner">
          {testimonials.map((t, i) => (
            <figure key={`${t.company}-${i}`} className="cc-sl-testimonial">
              <blockquote className="cc-sl-testimonial-quote">
                &ldquo;{t.quote}&rdquo;
              </blockquote>
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
