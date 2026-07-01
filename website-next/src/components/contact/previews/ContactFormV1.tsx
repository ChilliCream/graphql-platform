"use client";

import { useState, type FormEvent } from "react";
import { SolidButton } from "@/src/design-system/Button";
import { Dropdown, DropdownItem } from "@/src/design-system/Dropdown";
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

interface ContactFormV1Props {
  readonly className?: string;
}

export function ContactFormV1({ className }: ContactFormV1Props) {
  const [data, setData] = useState<FormData>(INITIAL);
  const [errors, setErrors] = useState<FormErrors>({});
  const [sent, setSent] = useState(false);

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
    if (!EMAIL_REGEX.test(data.email.trim())) {
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
    if (!validate()) {
      return;
    }
    setSent(true);
  }

  return (
    <div
      className={`border-cc-card-border bg-cc-card-bg w-full max-w-xl rounded-2xl border p-8 backdrop-blur-sm ${className ?? ""}`.trim()}
    >
      <div className="mb-6 flex flex-col gap-2">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Contact
        </span>
        <h2 className="text-cc-heading font-heading text-2xl leading-tight">
          Talk to us
        </h2>
        <p className="text-cc-ink-dim text-sm">
          Tell us what you need and we usually reply within one business day.
        </p>
      </div>

      {sent ? (
        <div
          role="status"
          className="border-cc-accent/30 bg-cc-accent/5 flex flex-col gap-2 rounded-xl border p-6"
        >
          <span className="text-cc-accent font-mono text-xs tracking-[0.2em] uppercase">
            Message sent
          </span>
          <p className="text-cc-heading font-heading text-lg">
            Thanks, we will be in touch.
          </p>
          <p className="text-cc-ink-dim text-sm">
            A member of our team will get back to you within one business day.
          </p>
        </div>
      ) : (
        <form
          onSubmit={handleSubmit}
          noValidate
          className="flex flex-col gap-5"
        >
          <Input
            label="Name"
            name="name"
            type="text"
            required
            value={data.name}
            error={errors.name}
            autoComplete="name"
            onChange={(e) => update("name", e.target.value)}
          />
          <Input
            label="Email"
            name="email"
            type="email"
            required
            value={data.email}
            error={errors.email}
            autoComplete="email"
            onChange={(e) => update("email", e.target.value)}
          />
          <Input
            label="Company"
            name="company"
            type="text"
            required
            value={data.company}
            error={errors.company}
            autoComplete="organization"
            onChange={(e) => update("company", e.target.value)}
          />
          <Dropdown
            label="Subject"
            panelClassName="p-1"
            trigger={
              <span className="text-cc-ink text-sm">{data.subject}</span>
            }
          >
            <ul className="m-0 flex list-none flex-col p-0">
              {SUBJECTS.map((s) => (
                <DropdownItem
                  key={s}
                  active={s === data.subject}
                  onClick={() => update("subject", s)}
                >
                  {s}
                </DropdownItem>
              ))}
            </ul>
          </Dropdown>
          <TextArea
            label="Message"
            name="message"
            rows={5}
            value={data.message}
            onChange={(e) => update("message", e.target.value)}
          />
          <div className="border-cc-white/10 flex flex-col gap-4 border-t pt-5 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-cc-ink-dim text-xs">
              We usually reply within one business day.
            </p>
            <SolidButton type="submit" className="self-start sm:self-auto">
              Send
            </SolidButton>
          </div>
        </form>
      )}
    </div>
  );
}
