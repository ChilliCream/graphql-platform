import type { Metadata } from "next";
import type { ComponentType } from "react";

import { ContactFormV1 } from "@/src/components/contact/previews/ContactFormV1";
import { ContactFormV2 } from "@/src/components/contact/previews/ContactFormV2";
import { ContactFormV3 } from "@/src/components/contact/previews/ContactFormV3";
import { ContactFormV4 } from "@/src/components/contact/previews/ContactFormV4";
import { ContactFormV5 } from "@/src/components/contact/previews/ContactFormV5";

export const metadata: Metadata = {
  title: "Contact Form Variations",
  description: "Internal preview of contact-form design variations.",
  robots: { index: false, follow: false },
};

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

interface Version {
  readonly title: string;
  readonly description: string;
  readonly Form: ComponentType<{ readonly className?: string }>;
}

const VERSIONS: readonly Version[] = [
  {
    title: "Refined Card",
    description:
      "A polished single-column form in an elevated card with a reply-time reassurance. The safe, professional baseline.",
    Form: ContactFormV1,
  },
  {
    title: "Split Context",
    description:
      "A two-column talk-to-sales layout pairing a context panel (what to expect, alternate contacts) with the form.",
    Form: ContactFormV2,
  },
  {
    title: "Compact Grid",
    description:
      "A dense single card with the fields arranged in a responsive two-column grid. Less vertical space.",
    Form: ContactFormV3,
  },
  {
    title: "Conversational",
    description:
      "The form written as a flowing sentence with inline underline inputs. Friendly and distinctive.",
    Form: ContactFormV4,
  },
  {
    title: "Segmented Intent",
    description:
      "Leads with selectable subject chips, then reveals the fields with helper text tailored to the chosen intent.",
    Form: ContactFormV5,
  },
];

export default function ContactFormPreviewPage() {
  return (
    <div className="flex flex-col gap-16 py-2">
      <header>
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Contact / Preview
        </p>
        <h1 className="font-heading text-cc-heading text-h1 mt-4 font-semibold tracking-tight">
          Five takes on the{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            contact form
          </span>
          .
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-lg leading-relaxed">
          Internal preview, not indexed. Same fields (name, email, company,
          subject, message) in five different layouts. Each is interactive, type
          in one and submit to see its success state. No messages are actually
          sent.
        </p>
      </header>

      {VERSIONS.map((version, index) => (
        <VersionBlock key={version.title} version={version} n={index + 1} />
      ))}
    </div>
  );
}

interface VersionBlockProps {
  readonly version: Version;
  readonly n: number;
}

function VersionBlock({ version, n }: VersionBlockProps) {
  const { Form } = version;
  return (
    <section className="border-cc-card-border/60 border-t pt-12">
      <div className="flex items-baseline gap-4">
        <span className="text-cc-nav-label font-mono text-sm tabular-nums">
          {String(n).padStart(2, "0")}
        </span>
        <h2 className="font-heading text-cc-heading text-h4 font-semibold tracking-tight">
          {version.title}
        </h2>
      </div>
      <p className="text-cc-ink mt-3 max-w-2xl text-sm leading-relaxed">
        {version.description}
      </p>

      <div className="mt-8 flex justify-center">
        <Form className="w-full" />
      </div>
    </section>
  );
}
