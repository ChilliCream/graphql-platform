"use client";

import { useState, type FormEvent } from "react";

const SUBJECTS = [
  "Schedule a Demo",
  "Pricing & Plans",
  "Sales",
  "Technical Support",
  "Partnership",
  "Other",
];

const SUBMIT_ENDPOINT = "https://forms.chillicream.com/api/SupportForm";
const THANK_YOU_PATH = "/services/support/thank-you";
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface FormData {
  name: string;
  email: string;
  company: string;
  subject: string;
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

const inputClasses =
  "rounded-md border bg-white/5 px-3 py-2 text-sm text-cc-ink focus:outline-hidden focus:ring-2 focus:ring-fuchsia-500/30 disabled:opacity-60";

export function ContactForm() {
  const [data, setData] = useState<FormData>(INITIAL);
  const [errors, setErrors] = useState<FormErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

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

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();

    if (!validate()) {
      return;
    }

    setIsSubmitting(true);

    try {
      const response = await fetch(SUBMIT_ENDPOINT, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          Name: data.name,
          Email: data.email,
          Company: data.company,
          SupportPlan: data.subject,
          Message: data.message,
        }),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      window.gtag?.("event", "contact_form_submit", {
        event_label: data.subject,
        page_path: window.location.pathname,
      });

      window.location.href = THANK_YOU_PATH;
    } catch {
      alert("There was an error submitting your request. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      className="flex flex-col gap-5 rounded-xl border border-cc-card-border bg-cc-card-bg p-8 backdrop-blur-sm"
    >
      <Field
        label="Name"
        name="name"
        type="text"
        required
        value={data.name}
        error={errors.name}
        disabled={isSubmitting}
        onChange={(v) => update("name", v)}
      />
      <Field
        label="Email"
        name="email"
        type="email"
        required
        value={data.email}
        error={errors.email}
        disabled={isSubmitting}
        onChange={(v) => update("email", v)}
      />
      <Field
        label="Company"
        name="company"
        type="text"
        required
        value={data.company}
        error={errors.company}
        disabled={isSubmitting}
        onChange={(v) => update("company", v)}
      />
      <div className="flex flex-col gap-1">
        <label htmlFor="subject" className="text-sm font-medium text-cc-ink">
          Subject
        </label>
        <select
          id="subject"
          name="subject"
          value={data.subject}
          disabled={isSubmitting}
          onChange={(e) => update("subject", e.target.value)}
          className={`${inputClasses} border-cc-card-border focus:border-fuchsia-500`}
        >
          {SUBJECTS.map((s) => (
            <option key={s}>{s}</option>
          ))}
        </select>
      </div>
      <div className="flex flex-col gap-1">
        <label htmlFor="message" className="text-sm font-medium text-cc-ink">
          Message
        </label>
        <textarea
          id="message"
          name="message"
          rows={5}
          value={data.message}
          disabled={isSubmitting}
          onChange={(e) => update("message", e.target.value)}
          className={`${inputClasses} border-cc-card-border focus:border-fuchsia-500`}
        />
      </div>
      <button
        type="submit"
        disabled={isSubmitting}
        className="inline-flex items-center self-start rounded-full bg-cc-ink px-7 py-3 text-sm font-medium text-[#0c1322] transition-colors hover:bg-white disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isSubmitting ? "Sending..." : "Send"}
      </button>
    </form>
  );
}

function Field({
  label,
  name,
  type,
  required,
  value,
  error,
  disabled,
  onChange,
}: {
  label: string;
  name: string;
  type: string;
  required?: boolean;
  value: string;
  error?: string;
  disabled?: boolean;
  onChange: (value: string) => void;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={name} className="text-sm font-medium text-cc-ink">
        {label}
        {required && <span className="ml-1 text-fuchsia-400">*</span>}
      </label>
      <input
        id={name}
        name={name}
        type={type}
        value={value}
        disabled={disabled}
        aria-invalid={error ? true : undefined}
        onChange={(e) => onChange(e.target.value)}
        className={`${inputClasses} ${
          error
            ? "border-red-500 focus:border-red-500"
            : "border-cc-card-border focus:border-fuchsia-500"
        }`}
      />
      {error && <span className="text-sm text-red-500">{error}</span>}
    </div>
  );
}
