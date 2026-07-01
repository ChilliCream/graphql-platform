"use client";

import { useId, useState, type FormEvent } from "react";
import { SolidButton } from "@/src/design-system/Button";
import { Input } from "@/src/design-system/Input";
import { TextArea } from "@/src/design-system/TextArea";

const SUBJECTS = [
  "Schedule a Demo",
  "Pricing & Plans",
  "Sales",
  "Technical Support",
  "Partnership",
  "Other",
] as const;

type Subject = (typeof SUBJECTS)[number];

const INTENT_HELPERS: Record<Subject, string> = {
  "Schedule a Demo":
    "Tell us about your team below and we'll set up a live walkthrough.",
  "Pricing & Plans":
    "Share your scale below and we'll map the right plan to it.",
  Sales: "Give us the shape of your project and we'll take it from there.",
  "Technical Support":
    "Describe what's blocking you so we can dig in straight away.",
  Partnership:
    "Let us know what you have in mind and we'll explore building together.",
  Other:
    "Whatever it is, drop it below and we'll route it to the right people.",
};

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface FormData {
  name: string;
  email: string;
  company: string;
  subject: Subject;
  message: string;
}

type FormErrors = Partial<Record<"name" | "email" | "company", string>>;

const INITIAL: FormData = {
  name: "",
  email: "",
  company: "",
  subject: SUBJECTS[0],
  message: "",
};

interface ContactFormV5Props {
  readonly className?: string;
}

function CheckIcon({ className }: { readonly className?: string }) {
  return (
    <svg
      viewBox="0 0 20 20"
      aria-hidden="true"
      className={`fill-current ${className ?? ""}`.trim()}
    >
      <path d="M8.143 14.06 4.2 10.118l1.32-1.32 2.623 2.622 6.02-6.02 1.32 1.32z" />
    </svg>
  );
}

export function ContactFormV5({ className }: ContactFormV5Props) {
  const [data, setData] = useState<FormData>(INITIAL);
  const [errors, setErrors] = useState<FormErrors>({});
  const [sent, setSent] = useState(false);
  const labelId = useId();

  function update<K extends keyof FormData>(field: K, value: FormData[K]) {
    setData((prev) => ({ ...prev, [field]: value }));
    if (field in errors) {
      setErrors((prev) => {
        const next = { ...prev };
        delete next[field as keyof FormErrors];
        return next;
      });
    }
  }

  function validate(): boolean {
    const next: FormErrors = {};
    if (data.name.trim().length < 2) {
      next.name = "Name is required";
    }
    if (!EMAIL_REGEX.test(data.email)) {
      next.email = "Please enter a valid email address";
    }
    if (data.company.trim().length < 2) {
      next.company = "Company is required";
    }
    setErrors(next);
    return Object.keys(next).length === 0;
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (validate()) {
      setSent(true);
    }
  }

  function reset() {
    setData(INITIAL);
    setErrors({});
    setSent(false);
  }

  if (sent) {
    return (
      <div
        className={`border-cc-card-border bg-cc-card-bg flex w-full max-w-2xl flex-col items-center gap-4 rounded-2xl border p-10 text-center backdrop-blur-sm ${className ?? ""}`.trim()}
      >
        <div
          className="flex size-12 items-center justify-center rounded-full"
          style={{
            background: "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)",
          }}
        >
          <CheckIcon className="text-cc-surface size-6" />
        </div>
        <div className="flex flex-col gap-1">
          <h3 className="font-heading text-cc-heading text-xl">
            Thanks, we&apos;ll be in touch
          </h3>
          <p className="text-cc-ink-dim text-sm">
            Your note about{" "}
            <span className="text-cc-accent">{data.subject}</span> is on its
            way. We usually reply within a business day.
          </p>
        </div>
        <button
          type="button"
          onClick={reset}
          className="border-cc-card-border text-cc-ink hover:border-cc-card-border-hover mt-2 inline-flex cursor-pointer items-center justify-center rounded-full border px-7 py-3 text-sm font-medium transition-colors"
        >
          Send another message
        </button>
      </div>
    );
  }

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      className={`flex w-full max-w-2xl flex-col gap-6 ${className ?? ""}`.trim()}
    >
      <section className="flex flex-col gap-3" aria-labelledby={labelId}>
        <div className="flex items-center gap-3">
          <span
            id={labelId}
            className="text-cc-nav-label font-mono text-xs tracking-wider uppercase"
          >
            What can we help with?
          </span>
          <span
            aria-hidden="true"
            className="h-px flex-1"
            style={{
              background: "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)",
            }}
          />
        </div>
        <div
          role="group"
          aria-labelledby={labelId}
          className="grid grid-cols-2 gap-2 sm:grid-cols-3"
        >
          {SUBJECTS.map((subject) => {
            const selected = subject === data.subject;
            return (
              <button
                key={subject}
                type="button"
                aria-pressed={selected}
                onClick={() => update("subject", subject)}
                className={`flex items-center justify-between gap-2 rounded-lg border px-3 py-2.5 text-left text-sm font-medium transition-colors ${
                  selected
                    ? "border-cc-accent bg-cc-accent/10 text-cc-heading"
                    : "border-cc-card-border text-cc-ink-dim hover:border-cc-card-border-hover hover:text-cc-ink"
                }`}
              >
                <span>{subject}</span>
                <CheckIcon
                  className={`size-4 shrink-0 transition-opacity ${
                    selected ? "text-cc-accent opacity-100" : "opacity-0"
                  }`}
                />
              </button>
            );
          })}
        </div>
      </section>

      <div className="border-cc-card-border bg-cc-card-bg flex flex-col gap-5 rounded-xl border p-6 backdrop-blur-sm">
        <p className="text-cc-ink-dim text-sm">
          {INTENT_HELPERS[data.subject]}
        </p>
        <Input
          label="Name"
          name="name"
          type="text"
          required
          value={data.name}
          error={errors.name}
          onChange={(e) => update("name", e.target.value)}
        />
        <Input
          label="Email"
          name="email"
          type="email"
          required
          value={data.email}
          error={errors.email}
          onChange={(e) => update("email", e.target.value)}
        />
        <Input
          label="Company"
          name="company"
          type="text"
          required
          value={data.company}
          error={errors.company}
          onChange={(e) => update("company", e.target.value)}
        />
        <TextArea
          label="Message"
          name="message"
          rows={5}
          value={data.message}
          onChange={(e) => update("message", e.target.value)}
        />
        <SolidButton type="submit" className="self-start">
          Talk to us
        </SolidButton>
      </div>
    </form>
  );
}
