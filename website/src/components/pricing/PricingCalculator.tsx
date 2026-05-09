"use client";

import React, { useMemo, useState } from "react";

// Inline mini-calculator for the /pricing hero (P0-pricing-1).
//
// Single horizontal log-scale slider for "requests / month" (1k -> 100M)
// with a live $/mo readout that reflects the Nitro Hosted plan: $499 base
// includes 10M requests, $0.40 per 1M requests of overage. Below 1M the
// reader fits Nitro Free; below 10M they're inside the Hosted base.
//
// One input, one output, no submit. Plain styled-components, no library.

const MIN_RPM = 1_000;
const MAX_RPM = 100_000_000;
const FREE_INCLUDED = 1_000_000;
const HOSTED_BASE_USD = 499;
const HOSTED_INCLUDED = 10_000_000;
const HOSTED_OVERAGE_USD_PER_M = 0.4;

const LOG_MIN = Math.log10(MIN_RPM);
const LOG_MAX = Math.log10(MAX_RPM);
const STEPS = 1000;

const sliderToRpm = (slider: number): number => {
  const t = slider / STEPS;
  const exp = LOG_MIN + (LOG_MAX - LOG_MIN) * t;
  return Math.round(Math.pow(10, exp));
};

const rpmToSlider = (rpm: number): number => {
  const exp = Math.log10(rpm);
  const t = (exp - LOG_MIN) / (LOG_MAX - LOG_MIN);
  return Math.round(t * STEPS);
};

const formatRpm = (rpm: number): string => {
  if (rpm >= 1_000_000) {
    const m = rpm / 1_000_000;
    return m >= 10
      ? `${Math.round(m)}M`
      : `${m.toFixed(1).replace(/\.0$/, "")}M`;
  }
  if (rpm >= 1_000) {
    const k = rpm / 1_000;
    return k >= 10
      ? `${Math.round(k)}k`
      : `${k.toFixed(1).replace(/\.0$/, "")}k`;
  }
  return rpm.toLocaleString();
};

interface CalcResult {
  readonly tier: "free" | "hosted-base" | "hosted-overage";
  readonly priceLabel: string;
  readonly tierLabel: string;
  readonly note: string;
}

const computePrice = (rpm: number): CalcResult => {
  if (rpm <= FREE_INCLUDED) {
    return {
      tier: "free",
      priceLabel: "$0",
      tierLabel: "Nitro Free",
      note: "1M requests / mo included, free forever.",
    };
  }
  if (rpm <= HOSTED_INCLUDED) {
    return {
      tier: "hosted-base",
      priceLabel: `$${HOSTED_BASE_USD}`,
      tierLabel: "Nitro Hosted",
      note: "10M requests / mo included in the base.",
    };
  }
  const overageM = (rpm - HOSTED_INCLUDED) / 1_000_000;
  const overageUsd = overageM * HOSTED_OVERAGE_USD_PER_M;
  const total = HOSTED_BASE_USD + overageUsd;
  const totalRounded = Math.round(total);
  return {
    tier: "hosted-overage",
    priceLabel: `$${totalRounded.toLocaleString()}`,
    tierLabel: "Nitro Hosted",
    note: `Base $${HOSTED_BASE_USD} + ${formatRpm(
      rpm - HOSTED_INCLUDED
    )} overage at $${HOSTED_OVERAGE_USD_PER_M.toFixed(2)} / 1M.`,
  };
};

export const PricingCalculator: React.FC = () => {
  const [slider, setSlider] = useState<number>(() => rpmToSlider(5_000_000));
  const rpm = useMemo(() => sliderToRpm(slider), [slider]);
  const result = useMemo(() => computePrice(rpm), [rpm]);

  return (
    <div className="cc-calc" role="group" aria-label="Estimate monthly cost">
      <div className="cc-calc-row">
        <label className="cc-calc-label" htmlFor="cc-calc-rpm">
          <span className="cc-calc-eyebrow">Requests / month</span>
          <span className="cc-calc-value">{formatRpm(rpm)}</span>
        </label>

        <div className="cc-calc-output" aria-live="polite">
          <span className="cc-calc-eyebrow">Estimated</span>
          <span className="cc-calc-price">
            {result.priceLabel}
            <span className="cc-calc-price-unit"> / mo</span>
          </span>
          <span className="cc-calc-tier">on {result.tierLabel}</span>
        </div>
      </div>

      <input
        id="cc-calc-rpm"
        className="cc-calc-slider"
        type="range"
        min={0}
        max={STEPS}
        step={1}
        value={slider}
        onChange={(e) => setSlider(Number(e.currentTarget.value))}
        aria-valuetext={`${formatRpm(rpm)} requests per month`}
      />

      <div className="cc-calc-scale" aria-hidden>
        <span>1k</span>
        <span>10k</span>
        <span>100k</span>
        <span>1M</span>
        <span>10M</span>
        <span>100M</span>
      </div>

      <p className="cc-calc-note">{result.note}</p>
    </div>
  );
};
