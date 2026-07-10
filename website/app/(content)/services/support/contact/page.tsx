import { pageMetadata } from "@/src/helpers/pageMetadata";

import { ContactForm } from "./ContactForm";
import { ContactIntro } from "./ContactIntro";

export const metadata = pageMetadata({
  title: "Contact Us",
  description:
    "Contact the ChilliCream team: tell us what you need, from support plans and demos to GraphQL consulting and training, and we will be in touch shortly.",
  path: "/services/support/contact",
});

// Cyan-to-coral brand spectrum, faded at both edges, for the panel's top hairline.
const SPECTRUM =
  "linear-gradient(90deg, transparent, #16b9e4 30%, #7c92c6 50%, #f0786a 70%, transparent)";

export default function ContactPage() {
  return (
    <section className="py-12 sm:py-16">
      <div className="border-cc-card-border bg-cc-card-bg/40 relative overflow-hidden rounded-3xl border">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
        <div className="divide-cc-card-border grid divide-y lg:grid-cols-2 lg:divide-x lg:divide-y-0">
          <div className="p-8 sm:p-10 lg:p-12">
            <ContactIntro />
          </div>
          <div className="p-8 sm:p-10 lg:p-12">
            <ContactForm />
          </div>
        </div>
      </div>
    </section>
  );
}
