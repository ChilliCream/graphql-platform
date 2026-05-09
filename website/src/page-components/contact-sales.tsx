"use client";

import React, { FC, useEffect } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { ContactSalesHero } from "@/components/contact-sales/ContactSalesHero";
import { ContactSalesLayout } from "@/components/contact-sales/ContactSalesLayout";
import { ContactSalesRoot } from "@/components/contact-sales/ContactSalesRoot";
import { WhatHappensNext } from "@/components/contact-sales/WhatHappensNext";

const ContactSalesPage: FC = () => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <SiteLayout disableStars>
      <SEO
        title="Contact Sales"
        description="Talk to ChilliCream's sales team. Get a custom demo, deployment fit review, and a POC plan within one business day."
      />
      <LandingGlobalStyle />
      <ContactSalesRoot>
        <ContactSalesHero />
        <ContactSalesLayout />
        <WhatHappensNext />
      </ContactSalesRoot>
    </SiteLayout>
  );
};

export default ContactSalesPage;
