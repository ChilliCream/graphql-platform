import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { Section } from "./shared";

const CONTACT_HREF =
  "/services/support/contact?subject=Sales&context=GraphQL%20Federation";

export function FusionCta() {
  return (
    <Section id="fusion">
      <Band skin="accent" layout="centered">
        <SectionHeading
          align="center"
          title="One gateway for both federations."
          description="Fusion composes source schemas written for the GraphQL Federation specification or Apollo Federation, plus OpenAPI and gRPC sources, into one composite schema, and Nitro tells your teams which schema changes are safe for real clients."
        />
        <ButtonRow align="center" className="mt-9">
          <SolidButton href="/products/fusion">Meet Fusion</SolidButton>
          <OutlineButton href={CONTACT_HREF}>Contact an Expert</OutlineButton>
        </ButtonRow>
      </Band>
    </Section>
  );
}
