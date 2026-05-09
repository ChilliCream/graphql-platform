"use client";

import React, { FC } from "react";

import type { Testimonial } from "@/data/solutions/types";

interface SolutionTestimonialProps {
  readonly testimonials: readonly Testimonial[];
  readonly stepNumber: string;
}

// Section 06: one or two pull quotes. Operational, not aspirational. Each
// card has a thin amber rule on the left, a large italic quote in display
// type, and a monogram + role + company attribution underneath. When two
// quotes are provided, they sit side by side at desktop widths.
export const SolutionTestimonial: FC<SolutionTestimonialProps> = ({
  testimonials,
  stepNumber,
}) => {
  if (testimonials.length === 0) {
    return null;
  }
  const containerClass =
    testimonials.length >= 2
      ? "cc-sl-testimonials-inner has-two"
      : "cc-sl-testimonials-inner";

  return (
    <section className="cc-sl-section cc-sl-testimonials">
      <div className="cc-section-label">
        <span className="num">{stepNumber}</span> Operators
      </div>
      <div className={containerClass}>
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
    </section>
  );
};
