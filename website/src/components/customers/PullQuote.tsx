"use client";

import React, { FC } from "react";

import type { PullQuote as PullQuoteData } from "@/data/customers/stories";

interface PullQuoteProps {
  readonly quote: PullQuoteData;
}

// Large italic quote with a thin amber rule on the left and an attribution
// in monospace eyebrow style. Named speakers print "Name · Role · Company";
// anonymous speakers print "Role · Company" only.
export const PullQuote: FC<PullQuoteProps> = ({ quote }) => {
  const attribution = quote.speakerName
    ? `${quote.speakerName} · ${quote.speakerRole} · ${quote.speakerCompany}`
    : `${quote.speakerRole} · ${quote.speakerCompany}`;

  return (
    <blockquote className="cc-csd-pullquote">
      <p className="cc-csd-pullquote-text">“{quote.text}”</p>
      <footer className="cc-csd-pullquote-attribution">{attribution}</footer>
    </blockquote>
  );
};
