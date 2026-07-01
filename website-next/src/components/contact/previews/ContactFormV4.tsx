"use client";

import { useId, useState, type ChangeEvent, type FormEvent } from "react";
import { SolidButton } from "@/src/design-system/Button";

const SUBJECTS = [
  "Schedule a Demo",
  "Pricing & Plans",
  "Sales",
  "Technical Support",
  "Partnership",
  "Other",
] as const;

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface CheckIconProps {
  readonly className?: string;
}

function CheckIcon({ className }: CheckIconProps) {
  return (
    <svg viewBox="0 0 20 20" aria-hidden="true" className={className}>
      <path
        fillRule="evenodd"
        clipRule="evenodd"
        d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm3.857-9.809a.75.75 0 0 0-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 1 0-1.06 1.061l2.5 2.5a.75.75 0 0 0 1.137-.089l4-5.5Z"
      />
    </svg>
  );
}

interface InlineInputProps {
  readonly value: string;
  readonly onChange: (value: string) => void;
  readonly ariaLabel: string;
  readonly placeholder: string;
  readonly type?: "text" | "email";
  readonly invalid?: boolean;
  readonly describedBy?: string;
  readonly className?: string;
}

function InlineInput({
  value,
  onChange,
  ariaLabel,
  placeholder,
  type = "text",
  invalid = false,
  describedBy,
  className = "",
}: InlineInputProps) {
  return (
    <input
      type={type}
      value={value}
      onChange={(event: ChangeEvent<HTMLInputElement>) =>
        onChange(event.target.value)
      }
      aria-label={ariaLabel}
      aria-invalid={invalid ? true : undefined}
      aria-describedby={describedBy}
      placeholder={placeholder}
      className={[
        "mx-1 inline-block field-sizing-content border-0 border-b bg-transparent px-1 pb-0.5",
        "text-cc-heading caret-cc-accent align-baseline transition-colors focus:outline-hidden",
        invalid
          ? "border-red-500 placeholder:text-red-400"
          : "border-cc-card-border placeholder:text-cc-ink-dim hover:border-cc-card-border-hover focus:border-cc-accent",
        className,
      ].join(" ")}
    />
  );
}

interface InlineSelectProps {
  readonly value: string;
  readonly onChange: (value: string) => void;
  readonly ariaLabel: string;
}

function InlineSelect({ value, onChange, ariaLabel }: InlineSelectProps) {
  return (
    <select
      value={value}
      onChange={(event: ChangeEvent<HTMLSelectElement>) =>
        onChange(event.target.value)
      }
      aria-label={ariaLabel}
      className={[
        "border-cc-card-border mx-1 cursor-pointer border-0 border-b bg-transparent px-1 pb-0.5",
        "text-cc-accent align-baseline transition-colors",
        "hover:border-cc-card-border-hover focus:border-cc-accent focus:outline-hidden",
      ].join(" ")}
    >
      {SUBJECTS.map((option) => (
        <option key={option} value={option} className="bg-cc-bg text-cc-ink">
          {option}
        </option>
      ))}
    </select>
  );
}

interface FieldErrors {
  readonly name?: string;
  readonly email?: string;
  readonly company?: string;
}

interface ContactFormV4Props {
  readonly className?: string;
}

export function ContactFormV4({ className = "" }: ContactFormV4Props) {
  const baseId = useId();
  const nameErrorId = `${baseId}-name-error`;
  const emailErrorId = `${baseId}-email-error`;
  const companyErrorId = `${baseId}-company-error`;
  const messageId = `${baseId}-message`;

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [company, setCompany] = useState("");
  const [subject, setSubject] = useState<string>(SUBJECTS[0]);
  const [message, setMessage] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});
  const [sent, setSent] = useState(false);

  function clearError(field: keyof FieldErrors) {
    setErrors((previous) =>
      previous[field] ? { ...previous, [field]: undefined } : previous,
    );
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const next: { name?: string; email?: string; company?: string } = {};
    if (!name.trim()) {
      next.name = "Please tell us your name.";
    }
    if (!email.trim()) {
      next.email = "Please add your email address.";
    } else if (!EMAIL_PATTERN.test(email.trim())) {
      next.email = "Please enter a valid email address.";
    }
    if (!company.trim()) {
      next.company = "Please tell us your company.";
    }

    setErrors(next);
    if (!next.name && !next.email && !next.company) {
      setSent(true);
    }
  }

  function reset() {
    setSent(false);
    setName("");
    setEmail("");
    setCompany("");
    setSubject(SUBJECTS[0]);
    setMessage("");
    setErrors({});
  }

  const rootClassName = [
    "border-cc-card-border bg-cc-card-bg max-w-2xl rounded-2xl border p-8 sm:p-10",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  if (sent) {
    return (
      <div className={rootClassName}>
        <div className="flex flex-col items-start gap-4">
          <span className="bg-cc-accent/10 inline-flex h-12 w-12 items-center justify-center rounded-full">
            <CheckIcon className="text-cc-accent h-6 w-6 fill-current" />
          </span>
          <h2 className="font-heading text-cc-heading text-2xl">
            Thanks, {name.trim().split(" ")[0] || "there"}. We&apos;ll be in
            touch.
          </h2>
          <p className="text-cc-ink-dim">
            We received your note about {subject.toLowerCase()} and will reply
            at <span className="text-cc-ink">{email.trim()}</span> shortly.
          </p>
          <button
            type="button"
            onClick={reset}
            className="text-cc-accent text-sm font-medium underline-offset-4 hover:underline focus:outline-hidden"
          >
            Send another message
          </button>
        </div>
      </div>
    );
  }

  const hasErrors = Boolean(errors.name || errors.email || errors.company);

  return (
    <div className={rootClassName}>
      <div className="mb-6 flex items-center gap-3">
        <span
          aria-hidden="true"
          className="h-px w-10 flex-none rounded-full"
          style={{ backgroundImage: SPECTRUM }}
        />
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Let&apos;s talk
        </span>
      </div>

      <form onSubmit={handleSubmit} noValidate>
        <p className="text-cc-ink-dim text-xl leading-relaxed sm:text-2xl">
          Hi, I&apos;m
          <InlineInput
            value={name}
            onChange={(value) => {
              setName(value);
              clearError("name");
            }}
            ariaLabel="Your name"
            placeholder="your name"
            invalid={Boolean(errors.name)}
            describedBy={errors.name ? nameErrorId : undefined}
            className="min-w-[7ch]"
          />
          from
          <InlineInput
            value={company}
            onChange={(value) => {
              setCompany(value);
              clearError("company");
            }}
            ariaLabel="Your company"
            placeholder="your company"
            invalid={Boolean(errors.company)}
            describedBy={errors.company ? companyErrorId : undefined}
            className="min-w-[8ch]"
          />
          . Reach me at
          <InlineInput
            value={email}
            onChange={(value) => {
              setEmail(value);
              clearError("email");
            }}
            ariaLabel="Your email address"
            placeholder="you@company.com"
            type="email"
            invalid={Boolean(errors.email)}
            describedBy={errors.email ? emailErrorId : undefined}
            className="min-w-[14ch]"
          />
          . I&apos;d like to talk about
          <InlineSelect
            value={subject}
            onChange={setSubject}
            ariaLabel="What would you like to talk about?"
          />
          .
        </p>

        {hasErrors && (
          <ul className="mt-5 space-y-1 text-sm text-red-500" role="alert">
            {errors.name && <li id={nameErrorId}>{errors.name}</li>}
            {errors.email && <li id={emailErrorId}>{errors.email}</li>}
            {errors.company && <li id={companyErrorId}>{errors.company}</li>}
          </ul>
        )}

        <div className="mt-10">
          <label
            htmlFor={messageId}
            className="text-cc-heading font-heading block text-lg"
          >
            Anything else you&apos;d like us to know?
          </label>
          <textarea
            id={messageId}
            value={message}
            onChange={(event) => setMessage(event.target.value)}
            rows={3}
            placeholder="Optional, but the more context the better."
            className={[
              "border-cc-card-border placeholder:text-cc-ink-dim mt-3 w-full resize-none",
              "text-cc-ink border-0 border-b bg-transparent px-1 py-2 text-base leading-relaxed",
              "hover:border-cc-card-border-hover focus:border-cc-accent focus:outline-hidden",
            ].join(" ")}
          />
        </div>

        <div className="mt-8">
          <SolidButton type="submit">Send</SolidButton>
        </div>
      </form>
    </div>
  );
}
