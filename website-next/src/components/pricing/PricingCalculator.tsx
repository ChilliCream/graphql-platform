"use client";

import { useMemo, useState } from "react";

// Inline mini-calculator for the /pricing hero.
//
// Single horizontal log-scale slider for "requests / month" (1k -> 100M)
// with a live $/mo readout that reflects the Nitro Hosted plan: $499 base
// includes 10M requests, $0.40 per 1M requests of overage. Below 1M the
// reader fits Nitro Free; below 10M they're inside the Hosted base.
//
// One input, one output, no submit.

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

const SCALE_TICKS = ["1k", "10k", "100k", "1M", "10M", "100M"];

export function PricingCalculator() {
  const [slider, setSlider] = useState<number>(() => rpmToSlider(5_000_000));
  const rpm = useMemo(() => sliderToRpm(slider), [slider]);
  const result = useMemo(() => computePrice(rpm), [rpm]);

  return (
    <div
      role="group"
      aria-label="Estimate monthly cost"
      className="mx-auto max-w-2xl rounded-2xl border border-[var(--cc-card-border)] bg-[var(--cc-card-bg)] p-6 text-left backdrop-blur-sm sm:p-7"
    >
      <div className="mb-4 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <label className="block cursor-pointer" htmlFor="cc-calc-rpm">
          <span className="mb-1 block font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--cc-ink-dim)]">
            Requests / month
          </span>
          <span className="block text-2xl font-medium tabular-nums text-[var(--cc-ink)] sm:text-3xl">
            {formatRpm(rpm)}
          </span>
        </label>

        <div className="text-left sm:text-right" aria-live="polite">
          <span className="mb-1 block font-mono text-[10px] uppercase tracking-[0.16em] text-[var(--cc-ink-dim)]">
            Estimated
          </span>
          <span className="block text-3xl font-semibold leading-none tabular-nums text-fuchsia-400 sm:text-4xl">
            {result.priceLabel}
            <span className="text-base font-normal text-[var(--cc-ink-dim)]">
              {" "}
              / mo
            </span>
          </span>
          <span className="mt-1.5 block font-mono text-[11px] uppercase tracking-[0.12em] text-[var(--cc-ink)]">
            on {result.tierLabel}
          </span>
        </div>
      </div>

      <input
        id="cc-calc-rpm"
        type="range"
        min={0}
        max={STEPS}
        step={1}
        value={slider}
        onChange={(e) => setSlider(Number(e.currentTarget.value))}
        aria-valuetext={`${formatRpm(rpm)} requests per month`}
        className="cc-pricing-slider my-2 h-1.5 w-full cursor-pointer appearance-none rounded-full"
        style={{
          background:
            "linear-gradient(90deg, rgba(217,70,239,0.5), rgba(245,241,234,0.12))",
        }}
      />

      <div
        className="flex justify-between font-mono text-[10px] tracking-[0.08em] text-[var(--cc-ink-dim)]"
        aria-hidden
      >
        {SCALE_TICKS.map((tick) => (
          <span key={tick}>{tick}</span>
        ))}
      </div>

      <p className="mt-3 text-sm leading-relaxed text-[var(--cc-ink-dim)]">
        {result.note}
      </p>

      <style>{`
        .cc-pricing-slider:focus-visible {
          outline: 2px solid #d946ef;
          outline-offset: 6px;
        }
        .cc-pricing-slider::-webkit-slider-thumb {
          -webkit-appearance: none;
          appearance: none;
          width: 20px;
          height: 20px;
          border-radius: 50%;
          background: #f5f1ea;
          border: 3px solid #d946ef;
          cursor: pointer;
          box-shadow: 0 0 0 6px rgba(217, 70, 239, 0.18);
          transition: transform 0.15s ease;
        }
        .cc-pricing-slider::-webkit-slider-thumb:hover {
          transform: scale(1.08);
        }
        .cc-pricing-slider::-moz-range-thumb {
          width: 20px;
          height: 20px;
          border-radius: 50%;
          background: #f5f1ea;
          border: 3px solid #d946ef;
          cursor: pointer;
          box-shadow: 0 0 0 6px rgba(217, 70, 239, 0.18);
        }
      `}</style>
    </div>
  );
}
