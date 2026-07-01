"use client";

import { useState, type FormEvent } from "react";
import { CheckIcon } from "@/src/components/CheckIcon";
import { SolidButton } from "@/src/design-system/Button";
import { Dropdown, DropdownItem } from "@/src/design-system/Dropdown";
import { Input } from "@/src/design-system/Input";
import { TextArea } from "@/src/design-system/TextArea";
import { SlackIcon } from "@/src/icons/Slack";

const SUBJECTS = [
  "Schedule a Demo",
  "Pricing & Plans",
  "Sales",
  "Technical Support",
  "Partnership",
  "Other",
] as const;

type Subject = (typeof SUBJECTS)[number];

const EXPECTATIONS = [
  "A reply within one business day",
  "Straight to the core engineers",
  "No sales runaround",
] as const;

const BRAND_SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface FieldErrors {
  readonly name?: string;
  readonly email?: string;
  readonly company?: string;
}

interface ContactFormV2Props {
  readonly className?: string;
}

export function ContactFormV2({ className }: ContactFormV2Props) {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [company, setCompany] = useState("");
  const [subject, setSubject] = useState<Subject>(SUBJECTS[0]);
  const [message, setMessage] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});
  const [sent, setSent] = useState(false);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const nextErrors: FieldErrors = {
      name: name.trim() ? undefined : "Please enter your name.",
      email: !email.trim()
        ? "Please enter your email."
        : EMAIL_PATTERN.test(email.trim())
          ? undefined
          : "Enter a valid email address.",
      company: company.trim() ? undefined : "Please tell us your company.",
    };

    setErrors(nextErrors);

    const isValid =
      !nextErrors.name && !nextErrors.email && !nextErrors.company;
    if (isValid) {
      setSent(true);
    }
  }

  return (
    <div
      className={[
        "border-cc-card-border bg-cc-card-bg mx-auto w-full max-w-4xl overflow-hidden rounded-2xl border",
        className ?? "",
      ]
        .join(" ")
        .trim()}
    >
      <div
        aria-hidden="true"
        className="h-px w-full"
        style={{ background: BRAND_SPECTRUM }}
      />

      <div className="grid md:grid-cols-2">
        <section className="flex flex-col gap-8 p-8 sm:p-10">
          <div className="flex flex-col gap-4">
            <span className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
              Talk to us
            </span>
            <h2 className="font-heading text-cc-heading text-3xl leading-tight font-semibold tracking-tight">
              Let&rsquo;s scope your GraphQL platform together.
            </h2>
            <p className="text-cc-ink-dim text-sm leading-relaxed">
              Tell us where you are and where you&rsquo;re headed. You&rsquo;ll
              talk with the people who build Hot Chocolate, Fusion, and Nitro,
              not a first-line queue.
            </p>
          </div>

          <ul className="flex flex-col gap-3">
            {EXPECTATIONS.map((item) => (
              <li key={item} className="flex items-start gap-3 text-sm">
                <span
                  className="text-cc-accent mt-[3px] inline-flex shrink-0"
                  aria-hidden="true"
                >
                  <CheckIcon />
                </span>
                <span className="text-cc-ink">{item}</span>
              </li>
            ))}
          </ul>

          <div className="border-cc-white/10 mt-auto flex flex-col gap-3 border-t pt-6">
            <a
              href="mailto:contact@chillicream.com"
              className="text-cc-ink hover:text-cc-accent text-sm font-medium no-underline transition-colors"
            >
              contact@chillicream.com
            </a>
            <a
              href="https://slack.chillicream.com/"
              target="_blank"
              rel="noopener noreferrer"
              className="text-cc-ink-dim hover:text-cc-accent inline-flex items-center gap-2 text-sm no-underline transition-colors"
            >
              <SlackIcon aria-hidden="true" className="h-4 w-4 fill-current" />
              Join the community Slack
            </a>
          </div>
        </section>

        <section className="bg-cc-surface/50 border-cc-white/10 border-t p-8 sm:p-10 md:border-t-0 md:border-l">
          {sent ? (
            <div className="flex h-full flex-col items-start justify-center gap-4 py-6">
              <span
                className="text-cc-accent ring-cc-accent/30 inline-flex h-12 w-12 items-center justify-center rounded-full ring-1"
                aria-hidden="true"
              >
                <CheckIcon size={22} />
              </span>
              <h3 className="font-heading text-cc-heading text-2xl font-semibold tracking-tight">
                Thanks, we&rsquo;ll be in touch.
              </h3>
              <p className="text-cc-ink-dim text-sm leading-relaxed">
                Your message is on its way to the core team. Expect a reply
                within one business day.
              </p>
            </div>
          ) : (
            <form
              noValidate
              onSubmit={handleSubmit}
              className="flex flex-col gap-5"
            >
              <Input
                label="Name"
                name="name"
                autoComplete="name"
                required
                value={name}
                onChange={(event) => setName(event.target.value)}
                error={errors.name}
              />
              <Input
                label="Email"
                name="email"
                type="email"
                autoComplete="email"
                required
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                error={errors.email}
              />
              <Input
                label="Company"
                name="company"
                autoComplete="organization"
                required
                value={company}
                onChange={(event) => setCompany(event.target.value)}
                error={errors.company}
              />

              <Dropdown
                label="Subject"
                trigger={<span className="font-medium">{subject}</span>}
                panelClassName="z-20"
              >
                <ul className="m-0 flex list-none flex-col p-1">
                  {SUBJECTS.map((item) => (
                    <DropdownItem
                      key={item}
                      active={item === subject}
                      onClick={() => setSubject(item)}
                    >
                      {item}
                    </DropdownItem>
                  ))}
                </ul>
              </Dropdown>

              <TextArea
                label="Message"
                name="message"
                rows={4}
                value={message}
                onChange={(event) => setMessage(event.target.value)}
                placeholder="What are you building?"
              />

              <SolidButton type="submit" className="w-full">
                Talk to us
              </SolidButton>
            </form>
          )}
        </section>
      </div>
    </div>
  );
}
