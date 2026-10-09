import { PageStructuredData } from "@/src/components/PageStructuredData";
import { Card } from "@/src/design-system/Card";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { ORGANIZATION_ID, schemaRef } from "@/src/helpers/structuredData";

import { ContactForm } from "./ContactForm";
import { ContactIntro } from "./ContactIntro";

const PAGE = {
  title: "Contact GraphQL Experts",
  description:
    "Contact ChilliCream about Nitro pricing, GraphQL consulting, team training, partnerships, or technical support for Hot Chocolate, Fusion, and Nitro.",
  path: "/services/support/contact",
  keywords: [
    "contact ChilliCream",
    "GraphQL experts",
    "Hot Chocolate consulting",
    "Nitro sales",
    "GraphQL support contact",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

// Cyan-to-coral brand spectrum, faded at both edges, for the panel's top hairline.
const SPECTRUM =
  "linear-gradient(90deg, transparent, #16b9e4 30%, #7c92c6 50%, #f0786a 70%, transparent)";

export default function ContactPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="ContactPage"
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Services", path: "/services" },
          { name: "Contact" },
        ]}
        mainEntity={schemaRef(ORGANIZATION_ID)}
        about={schemaRef(ORGANIZATION_ID)}
      />
      <section className="py-12 sm:py-16">
        <Card variant="panel">
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
        </Card>
      </section>
    </>
  );
}
