"use client";

import React, { FC, useCallback, useRef } from "react";

import { EnterpriseGridCompliance } from "./EnterpriseGridCompliance";
import { EnterpriseGridFederation } from "./EnterpriseGridFederation";
import { EnterpriseGridHero } from "./EnterpriseGridHero";
import { EnterpriseGridMigration } from "./EnterpriseGridMigration";
import { EnterpriseGridPillars } from "./EnterpriseGridPillars";
import { EnterpriseGridRoi } from "./EnterpriseGridRoi";
import { EnterpriseGridRoot } from "./EnterpriseGridRoot";
import { EnterpriseGridSalesForm } from "./EnterpriseGridSalesForm";
import { EnterpriseGridSkus } from "./EnterpriseGridSkus";

// Grid variant of `/enterprise`. The third sibling of Default and Cinematic.
// Lifts Vercel's `vercel-sol-saas` structural template per
// `.work/reviews/grid-design-system.md` §9.2: hero -> 4-stat strip ->
// 3-up pillars -> federation deep-dive (asymmetric pair) -> 3-up SKU cards
// -> compliance grid (3 attestations + 5 capabilities) -> 3-up migration ->
// sales form split.
//
// All sections sit inside `EnterpriseGridRoot` which owns the dark-navy
// surface tokens, square corners (border-radius: 0 everywhere), and the
// hairline border palette. Adjacent sections share their hairlines: each
// `<GridSection>` gets a top hairline only where the previous section did
// not already paint one, so there are never double 1px lines.
export const EnterpriseGrid: FC = () => {
  const formRef = useRef<HTMLElement>(null);

  const handleScrollToForm = useCallback(() => {
    formRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, []);

  return (
    <EnterpriseGridRoot>
      <EnterpriseGridHero onPrimaryClick={handleScrollToForm} />
      <EnterpriseGridRoi />
      <EnterpriseGridPillars />
      <EnterpriseGridFederation />
      <EnterpriseGridSkus />
      <EnterpriseGridCompliance />
      <EnterpriseGridMigration />
      <EnterpriseGridSalesForm ref={formRef} />
    </EnterpriseGridRoot>
  );
};
