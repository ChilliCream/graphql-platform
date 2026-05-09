"use client";

import React, { FC } from "react";

import { SalesFormFields } from "@/components/enterprise/SalesFormFields";
import { ContactSalesSocialProof } from "./ContactSalesSocialProof";

export const ContactSalesLayout: FC = () => {
  return (
    <section className="cc-cs-section cc-cs-layout">
      <div className="cc-cs-layout-inner">
        <div className="cc-cs-form-card">
          <div className="cc-cs-form-inner">
            <div className="eyebrow">The form</div>
            <h2 className="cc-cs-form-heading">Five fields, real reply.</h2>
            <SalesFormFields variant="full" />
          </div>
        </div>
        <ContactSalesSocialProof />
      </div>
    </section>
  );
};
