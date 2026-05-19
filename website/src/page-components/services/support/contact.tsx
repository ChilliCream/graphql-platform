"use client";

import { SiteLayout } from "@/components/layout";
import { ContactForm } from "@/components/misc/contact-form";
import { SEO } from "@/components/misc";
import { CONTACT_SUBJECTS, ContactSubject } from "@/types/support";
import { getValidatedQueryParam } from "@/utils/url-helpers";
import React, { FC, useEffect, useState } from "react";

const ContactPage: FC = () => {
  const [subject, setSubject] = useState<ContactSubject>("Schedule a Demo");

  useEffect(() => {
    const subjectFromUrl = getValidatedQueryParam("subject", CONTACT_SUBJECTS);
    if (subjectFromUrl) {
      setSubject(subjectFromUrl);
    }
  }, []);

  return (
    <SiteLayout>
      <SEO title="Contact Us" />
      <ContactForm initialSubject={subject} />
    </SiteLayout>
  );
};

export default ContactPage;
