import { PageHero } from "@/src/components/PageHero";

const SUBJECTS = [
  "Schedule a Demo",
  "Pricing & Plans",
  "Sales",
  "Technical Support",
  "Partnership",
  "Other",
];

export default function ContactPage() {
  return (
    <>
      <PageHero
        title="Contact Us"
        teaser="Tell us a bit about what you need and we'll be in touch."
      />
      <section className="mx-auto max-w-xl pb-16">
        <form
          action="mailto:contact@chillicream.com"
          method="POST"
          encType="text/plain"
          className="flex flex-col gap-5 rounded-xl border border-cc-card-border bg-cc-card-bg backdrop-blur-sm p-8 "
        >
          <Field label="Name" name="name" type="text" required />
          <Field label="Email" name="email" type="email" required />
          <Field label="Company" name="company" type="text" />
          <div className="flex flex-col gap-1">
            <label
              htmlFor="subject"
              className="text-sm font-medium text-cc-ink"
            >
              Subject
            </label>
            <select
              id="subject"
              name="subject"
              defaultValue={SUBJECTS[0]}
              className="rounded-md border border-cc-card-border bg-white/5 px-3 py-2 text-sm text-cc-ink focus:border-fuchsia-500 focus:outline-hidden focus:ring-2 focus:ring-fuchsia-500/30"
            >
              {SUBJECTS.map((s) => (
                <option key={s}>{s}</option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1">
            <label
              htmlFor="message"
              className="text-sm font-medium text-cc-ink"
            >
              Message
            </label>
            <textarea
              id="message"
              name="message"
              rows={5}
              required
              className="rounded-md border border-cc-card-border bg-white/5 px-3 py-2 text-sm text-cc-ink focus:border-fuchsia-500 focus:outline-hidden focus:ring-2 focus:ring-fuchsia-500/30"
            />
          </div>
          <button
            type="submit"
            className="self-start inline-flex items-center rounded-full bg-cc-ink px-7 py-3 text-sm font-medium text-[#0c1322] transition-colors hover:bg-white"
          >
            Send
          </button>
        </form>
      </section>
    </>
  );
}

function Field({
  label,
  name,
  type,
  required,
}: {
  label: string;
  name: string;
  type: string;
  required?: boolean;
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
        required={required}
        className="rounded-md border border-cc-card-border bg-white/5 px-3 py-2 text-sm text-cc-ink focus:border-fuchsia-500 focus:outline-hidden focus:ring-2 focus:ring-fuchsia-500/30"
      />
    </div>
  );
}
