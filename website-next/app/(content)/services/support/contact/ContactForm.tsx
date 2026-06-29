"use client";

import { useEffect, useState, type FormEvent } from "react";
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

// Subject starts empty so the prerendered HTML and the first client render
// agree (no hydration mismatch). It is populated from the URL after mount.
const INITIAL: FormData = {
  name: "",
  email: "",
  company: "",
  subject: "",
  message: "",
};

function resolveSubject(subject: string | null): string {
  if (!subject) {
    return SUBJECTS[0];
  }

  const match = SUBJECTS.find((s) => s.toLowerCase() === subject.toLowerCase());

  return match ?? SUBJECTS[0];
}

export function ContactForm() {
  const [data, setData] = useState<FormData>(INITIAL);
  const [errors, setErrors] = useState<FormErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Resolve the subject from the `subject` query param once mounted. Reading it
  // during render would diverge from the static HTML and break hydration.
  useEffect(() => {
    const subject = new URLSearchParams(window.location.search).get("subject");
    // eslint-disable-next-line react-hooks/set-state-in-effect -- syncing from a browser-only value on mount
    setData((prev) => ({ ...prev, subject: resolveSubject(subject) }));
  }, []);

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
      className="border-cc-card-border bg-cc-card-bg flex flex-col gap-5 rounded-xl border p-8 backdrop-blur-sm"
    >
      <Input
        label="Name"
        name="name"
        type="text"
        required
        value={data.name}
        error={errors.name}
        disabled={isSubmitting}
        onChange={(e) => update("name", e.target.value)}
      />
      <Input
        label="Email"
        name="email"
        type="email"
        required
        value={data.email}
        error={errors.email}
        disabled={isSubmitting}
        onChange={(e) => update("email", e.target.value)}
      />
      <Input
        label="Company"
        name="company"
        type="text"
        required
        value={data.company}
        error={errors.company}
        disabled={isSubmitting}
        onChange={(e) => update("company", e.target.value)}
      />
      <Dropdown
        label="Subject"
        className={isSubmitting ? "pointer-events-none opacity-60" : undefined}
        panelClassName="p-1"
        trigger={
          <span className="text-cc-ink text-sm">
            {/* Blank until the subject is resolved from the URL after mount, so
                no placeholder text flashes before the real value appears. */}
            {data.subject || "\u00A0"}
          </span>
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
        disabled={isSubmitting}
        onChange={(e) => update("message", e.target.value)}
      />
      <SolidButton type="submit" disabled={isSubmitting} className="self-start">
        {isSubmitting ? "Sending..." : "Send"}
      </SolidButton>
    </form>
  );
}
