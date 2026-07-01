"use client";

import { useState } from "react";
import { Input } from "@/src/design-system/Input";
import { TextArea } from "@/src/design-system/TextArea";
import { Dropdown, DropdownItem } from "@/src/design-system/Dropdown";
import { SolidButton } from "@/src/design-system/Button";

const SUBJECTS = [
  "Schedule a Demo",
  "Pricing & Plans",
  "Sales",
  "Technical Support",
  "Partnership",
  "Other",
] as const;

type Subject = (typeof SUBJECTS)[number];

interface FieldErrors {
  readonly name?: string;
  readonly email?: string;
  readonly company?: string;
}

interface ContactFormV3Props {
  readonly className?: string;
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function ContactFormV3({ className }: ContactFormV3Props) {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [company, setCompany] = useState("");
  const [subject, setSubject] = useState<Subject>(SUBJECTS[0]);
  const [message, setMessage] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});
  const [sent, setSent] = useState(false);

  function validate(): FieldErrors {
    const next: { name?: string; email?: string; company?: string } = {};

    if (!name.trim()) {
      next.name = "Please enter your name.";
    }

    if (!email.trim()) {
      next.email = "Please enter your email.";
    } else if (!EMAIL_PATTERN.test(email.trim())) {
      next.email = "Please enter a valid email address.";
    }

    if (!company.trim()) {
      next.company = "Please enter your company.";
    }

    return next;
  }

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();

    const next = validate();
    setErrors(next);

    if (Object.keys(next).length === 0) {
      setSent(true);
    }
  }

  return (
    <div
      className={`bg-cc-card-bg border-cc-card-border w-full max-w-2xl rounded-xl border p-6 backdrop-blur-sm sm:p-8 ${className ?? ""}`.trim()}
    >
      <div className="mb-6">
        <p className="text-cc-nav-label font-mono text-xs tracking-widest uppercase">
          Contact
        </p>
        <h2 className="font-heading text-cc-heading mt-2 text-2xl">
          Talk to us
        </h2>
        <p className="text-cc-ink-dim mt-1 text-sm">
          Tell us what you need and the right person will reply.
        </p>
      </div>

      {sent ? (
        <div
          className="border-cc-accent/30 bg-cc-accent/5 flex items-start gap-3 rounded-lg border p-5"
          role="status"
        >
          <span
            aria-hidden="true"
            className="bg-cc-accent/15 text-cc-accent mt-0.5 flex h-8 w-8 flex-none items-center justify-center rounded-full"
          >
            <CheckIcon className="h-4 w-4 fill-current" />
          </span>
          <div>
            <p className="text-cc-heading font-heading text-lg">
              Thanks, we&apos;ll be in touch
            </p>
            <p className="text-cc-ink-dim mt-1 text-sm">
              Your message about &ldquo;{subject}&rdquo; is on its way. We
              typically respond within one business day.
            </p>
          </div>
        </div>
      ) : (
        <form
          noValidate
          onSubmit={handleSubmit}
          className="flex flex-col gap-4"
        >
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input
              label="Name"
              name="name"
              autoComplete="name"
              placeholder="Ada Lovelace"
              required
              value={name}
              error={errors.name}
              onChange={(e) => setName(e.target.value)}
            />
            <Input
              label="Email"
              name="email"
              type="email"
              autoComplete="email"
              placeholder="ada@example.com"
              required
              value={email}
              error={errors.email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input
              label="Company"
              name="company"
              autoComplete="organization"
              placeholder="Analytical Engine Co."
              required
              value={company}
              error={errors.company}
              onChange={(e) => setCompany(e.target.value)}
            />
            <Dropdown label="Subject" trigger={subject} panelClassName="p-1">
              {SUBJECTS.map((option) => (
                <DropdownItem
                  key={option}
                  active={option === subject}
                  onClick={() => setSubject(option)}
                >
                  {option}
                </DropdownItem>
              ))}
            </Dropdown>
          </div>

          <TextArea
            label="Message"
            name="message"
            rows={4}
            placeholder="How can we help?"
            value={message}
            onChange={(e) => setMessage(e.target.value)}
          />

          <div className="mt-1 flex items-center justify-between gap-4">
            <p className="text-cc-ink-dim text-xs">
              <span className="text-cc-accent">*</span> Required fields
            </p>
            <SolidButton type="submit">Send</SolidButton>
          </div>
        </form>
      )}
    </div>
  );
}

interface CheckIconProps {
  readonly className?: string;
}

function CheckIcon({ className }: CheckIconProps) {
  return (
    <svg viewBox="0 0 20 20" aria-hidden="true" className={className}>
      <path d="M16.7 5.3a1 1 0 0 1 0 1.4l-7.5 7.5a1 1 0 0 1-1.4 0l-3.5-3.5a1 1 0 1 1 1.4-1.4l2.8 2.79 6.8-6.79a1 1 0 0 1 1.4 0Z" />
    </svg>
  );
}
