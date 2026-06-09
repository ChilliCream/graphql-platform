import { PageHero } from "@/src/components/PageHero";
import { ContactForm } from "./ContactForm";

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
