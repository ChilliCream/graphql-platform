"use client";

import React, { FC, useState } from "react";
import styled from "styled-components";

// SalesFormFields owns the actual form: shared schema between the inline
// form on /enterprise (4 fields) and the longer /contact/sales form
// (5 fields, with the optional message textarea). The submit pipeline lives
// here too — POSTs to /api/contact-sales and inline-replaces with a
// confirmation block on success. No redirect.

export type SalesFormVariant = "inline" | "full";

export interface SalesFormProps {
  readonly variant: SalesFormVariant;
  readonly defaultInterest?: InterestKey;
}

export type InterestKey =
  | "fusion"
  | "nitro"
  | "self-hosted"
  | "support"
  | "other";

interface InterestOption {
  readonly value: InterestKey;
  readonly label: string;
}

const INTEREST_OPTIONS: readonly InterestOption[] = [
  { value: "fusion", label: "Fusion federation" },
  { value: "nitro", label: "Nitro" },
  { value: "self-hosted", label: "Self-hosted / air-gapped" },
  { value: "support", label: "Hot Chocolate enterprise support" },
  { value: "other", label: "Not sure yet" },
];

// Curated subset (~25) — every common buyer-country we'd expect, not
// 195 entries. Anything else is captured in the discovery call.
const COUNTRY_OPTIONS: readonly string[] = [
  "United States",
  "Canada",
  "United Kingdom",
  "Germany",
  "France",
  "Switzerland",
  "Austria",
  "Netherlands",
  "Belgium",
  "Sweden",
  "Norway",
  "Denmark",
  "Finland",
  "Ireland",
  "Spain",
  "Portugal",
  "Italy",
  "Poland",
  "Czechia",
  "Australia",
  "New Zealand",
  "Singapore",
  "Japan",
  "United Arab Emirates",
  "Other",
];

const StyledForm = styled.form`
  display: flex;
  flex-direction: column;
  gap: 16px;

  .cc-form-field {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }
  .cc-form-label {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
  .cc-form-input,
  .cc-form-select,
  .cc-form-textarea {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 15px;
    color: var(--cc-ink);
    background: rgba(255, 255, 255, 0.04);
    border: 1px solid var(--cc-ink-faint);
    border-radius: 10px;
    padding: 14px 16px;
    outline: none;
    transition: border-color 0.15s ease, background 0.15s ease,
      box-shadow 0.15s ease;
    width: 100%;
  }
  .cc-form-input::placeholder,
  .cc-form-textarea::placeholder {
    color: rgba(245, 241, 234, 0.32);
  }
  .cc-form-input:focus,
  .cc-form-select:focus,
  .cc-form-textarea:focus {
    border-color: rgba(245, 241, 234, 0.62);
    background: rgba(255, 255, 255, 0.06);
    box-shadow: 0 0 0 3px rgba(245, 241, 234, 0.08);
  }
  .cc-form-select {
    appearance: none;
    background-image: linear-gradient(
        45deg,
        transparent 50%,
        rgba(245, 241, 234, 0.62) 50%
      ),
      linear-gradient(135deg, rgba(245, 241, 234, 0.62) 50%, transparent 50%);
    background-position: calc(100% - 18px) 22px, calc(100% - 13px) 22px;
    background-size: 5px 5px, 5px 5px;
    background-repeat: no-repeat;
    padding-right: 36px;
  }
  .cc-form-select option {
    color: #0c1322;
    background: var(--cc-ink);
  }
  .cc-form-textarea {
    resize: vertical;
    min-height: 96px;
    font-family: var(--cc-font-sans), sans-serif;
    line-height: 1.5;
  }
  .cc-form-honey {
    position: absolute;
    left: -10000px;
    top: auto;
    width: 1px;
    height: 1px;
    overflow: hidden;
    display: none;
  }
  .cc-form-error {
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    color: var(--cc-col-cat);
    letter-spacing: 0.04em;
    margin: 0;
  }
  .cc-form-submit {
    margin-top: 8px;
    width: 100%;
  }
  .cc-form-fineprint {
    font-size: 12px;
    color: var(--cc-ink-dim);
    line-height: 1.5;
    margin: 8px 0 0;
  }

  /* Confirmation block (replaces the form on success) */
  .cc-form-success {
    display: flex;
    flex-direction: column;
    gap: 14px;
    padding: 36px 32px;
    border: 1px solid rgba(118, 200, 165, 0.32);
    border-radius: 16px;
    background: rgba(118, 200, 165, 0.06);
  }
  .cc-form-success-icon {
    width: 44px;
    height: 44px;
    border-radius: 12px;
    border: 1px solid rgba(118, 200, 165, 0.42);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--cc-col-ord);
  }
  .cc-form-success-title {
    font-size: 20px;
    font-weight: 500;
    letter-spacing: -0.01em;
    color: var(--cc-ink);
    margin: 0;
  }
  .cc-form-success-body {
    font-size: 14px;
    line-height: 1.6;
    color: var(--cc-ink-dim);
    margin: 0;
    text-wrap: pretty;
  }
  .cc-form-success-sig {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.16em;
    text-transform: uppercase;
    color: var(--cc-ink-dim);
  }
`;

const Check: FC = () => (
  <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden>
    <path
      d="M5 12.5 L10 17.5 L19 7.5"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </svg>
);

export const SalesFormFields: FC<SalesFormProps> = ({
  variant,
  defaultInterest,
}) => {
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);

    const form = e.currentTarget;
    const data = new FormData(form);

    if (data.get("website_url_9b3c")) {
      // Honeypot tripped. Pretend success so the bot moves on.
      setSubmitted(true);
      return;
    }

    const payload = {
      email: String(data.get("email") ?? "").trim(),
      company: String(data.get("company") ?? "").trim(),
      country: String(data.get("country") ?? "").trim(),
      interest: String(data.get("interest") ?? "").trim(),
      message: String(data.get("message") ?? "").trim(),
      pageUri: typeof window !== "undefined" ? window.location.href : undefined,
    };

    if (
      !payload.email ||
      !payload.company ||
      !payload.country ||
      !payload.interest
    ) {
      setError("Please complete every required field.");
      return;
    }

    setSubmitting(true);
    try {
      const r = await fetch("/api/contact-sales", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });
      if (!r.ok) {
        throw new Error("Bad response");
      }
      setSubmitted(true);
    } catch {
      setError("Something went wrong. Please email contact@chillicream.com.");
    } finally {
      setSubmitting(false);
    }
  }

  if (submitted) {
    return (
      <StyledForm as="div" aria-live="polite">
        <div className="cc-form-success">
          <div className="cc-form-success-icon">
            <Check />
          </div>
          <h3 className="cc-form-success-title">Thanks — we've got it.</h3>
          <p className="cc-form-success-body">
            We'll be in touch within one business day with next steps: a
            30-minute discovery call with a solution architect, and a tailored
            demo of the parts of the platform you're evaluating.
          </p>
          <div className="cc-form-success-sig">— The ChilliCream team</div>
        </div>
      </StyledForm>
    );
  }

  return (
    <StyledForm onSubmit={handleSubmit} noValidate>
      <div className="cc-form-field">
        <label className="cc-form-label" htmlFor="cc-email">
          Work email
        </label>
        <input
          id="cc-email"
          name="email"
          type="email"
          required
          autoComplete="email"
          placeholder="you@company.com"
          className="cc-form-input"
        />
      </div>

      <div className="cc-form-field">
        <label className="cc-form-label" htmlFor="cc-company">
          Company
        </label>
        <input
          id="cc-company"
          name="company"
          type="text"
          required
          autoComplete="organization"
          placeholder="Company name"
          className="cc-form-input"
        />
      </div>

      <div className="cc-form-field">
        <label className="cc-form-label" htmlFor="cc-country">
          Country
        </label>
        <select
          id="cc-country"
          name="country"
          required
          defaultValue=""
          className="cc-form-select"
        >
          <option value="" disabled>
            Select a country
          </option>
          {COUNTRY_OPTIONS.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </select>
      </div>

      <div className="cc-form-field">
        <label className="cc-form-label" htmlFor="cc-interest">
          What are you exploring?
        </label>
        <select
          id="cc-interest"
          name="interest"
          required
          defaultValue={defaultInterest ?? ""}
          className="cc-form-select"
        >
          <option value="" disabled>
            Select one
          </option>
          {INTEREST_OPTIONS.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>

      {variant === "full" && (
        <div className="cc-form-field">
          <label className="cc-form-label" htmlFor="cc-message">
            Tell us about your project (optional)
          </label>
          <textarea
            id="cc-message"
            name="message"
            rows={4}
            placeholder="Stack, scale, regulatory constraints, target timeline."
            className="cc-form-textarea"
          />
        </div>
      )}

      <input
        type="text"
        name="website_url_9b3c"
        tabIndex={-1}
        autoComplete="off"
        className="cc-form-honey"
        aria-hidden="true"
      />

      {error && <p className="cc-form-error">{error}</p>}

      <button
        type="submit"
        className="cc-btn cc-btn-primary cc-form-submit"
        disabled={submitting}
      >
        {submitting ? "Sending…" : "Talk to sales →"}
      </button>

      <p className="cc-form-fineprint">
        We reply within one business day. No phone, no live chat — just a real
        engineer on a real call.
      </p>
    </StyledForm>
  );
};
