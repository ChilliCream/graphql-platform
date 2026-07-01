import { PageHero } from "@/src/components/PageHero";
import { pageMetadata } from "@/src/helpers/pageMetadata";

import { ContactForm } from "./ContactForm";

export const metadata = pageMetadata({
  title: "Contact Us",
  description:
    "Contact the ChilliCream team: tell us what you need, from support plans and demos to GraphQL consulting and training, and we will be in touch shortly.",
  path: "/services/support/contact",
});

export default function ContactPage() {
  return (
    <>
      <PageHero
        title="Contact Us"
        teaser="Tell us a bit about what you need and we'll be in touch."
      />
      <section className="mx-auto max-w-xl pb-16">
        <ContactForm />
      </section>
    </>
  );
}
