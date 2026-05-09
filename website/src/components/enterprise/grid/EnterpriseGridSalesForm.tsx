"use client";

import React, { forwardRef } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridSection,
  GridSplit,
} from "@/components/redesign-system/grid";

import { SalesFormFields } from "../SalesFormFields";

// Sales form (archetype L variant + companion 3-step "what happens next"
// list). Form on the left of a 60/40 GridSplit, the 3 next-steps as a
// vertical list on the right. Both cells share a 1px hairline border so the
// section reads as a single block, no rounded corners or chrome gradients.

interface NextStep {
  readonly key: string;
  readonly num: string;
  readonly title: string;
  readonly body: string;
}

const NEXT_STEPS: readonly NextStep[] = [
  {
    key: "discovery",
    num: "01",
    title: "Discovery call",
    body: "30 minutes with a solution architect, no decks. Map your stack, your blockers, and the metrics you'd need to greenlight a pilot.",
  },
  {
    key: "architecture",
    num: "02",
    title: "Architecture review",
    body: "We review your current federation/BFF/gateway design and return a written deployment plan tailored to your infra and compliance posture.",
  },
  {
    key: "pilot",
    num: "03",
    title: "Pilot SOW",
    body: "A scoped two-week paid pilot: schema audit, working Fusion mesh on one slice, rollout plan with rollback drill. One named architect, no handoff.",
  },
];

const FormCell = styled.div`
  display: flex;
  flex-direction: column;
  gap: 24px;
  min-height: 480px;
`;

const FormCopy = styled.div`
  display: flex;
  flex-direction: column;
  gap: 12px;
`;

const FormCopyLede = styled.p`
  font-size: 15px;
  line-height: 1.6;
  color: var(--cc-ink-dim);
  margin: 0;
  text-wrap: pretty;
`;

const NextCell = styled.ol`
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  height: 100%;
`;

const NextStepItem = styled.li`
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 24px 0;
  flex: 1;
  border-bottom: 1px solid var(--cc-ink-faint);

  &:first-child {
    padding-top: 0;
  }
  &:last-child {
    border-bottom: 0;
    padding-bottom: 0;
  }
`;

const NextStepNum = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  color: var(--cc-accent);
`;

const NextStepTitle = styled.h3`
  font-size: 18px;
  font-weight: 500;
  letter-spacing: -0.015em;
  color: var(--cc-ink);
  margin: 0;
`;

const NextStepBody = styled.p`
  font-size: 14px;
  line-height: 1.55;
  color: var(--cc-ink-dim);
  margin: 0;
  text-wrap: pretty;
`;

// Local style scope that adapts SalesFormFields' rounded inputs to the
// Grid variant's square hairline aesthetic. The form fields component is
// shared across variants so we cannot edit it directly; this scope flips
// the radii, swaps the input borders to the hairline token, and squares
// the submit button to match GridButton.
const FormScope = styled.div`
  .cc-form-input,
  .cc-form-select,
  .cc-form-textarea {
    border-radius: 0;
    border-color: var(--cc-ink-faint);
  }
  .cc-form-success {
    border-radius: 0;
  }
  .cc-form-success-icon {
    border-radius: 0;
  }
  .cc-btn {
    border-radius: 0;
  }
`;

export const EnterpriseGridSalesForm = forwardRef<HTMLElement>(
  function EnterpriseGridSalesForm(_, ref) {
    return (
      <GridSection hairlineTop>
        <section ref={ref} id="contact-form">
          <div className="cc-grid-section-head">
            <span className="cc-grid-eyebrow">Talk to sales</span>
            <h2 className="cc-grid-h2">
              Tell us about your stack. We'll come back with a plan.
            </h2>
            <p>
              Four fields, one engineer on the other end of the line. We'll
              reply with a deployment plan, a POC scope, and pricing within one
              business day.
            </p>
          </div>
          <GridSplit ratio="60-40">
            <GridCard>
              <FormCell>
                <FormCopy>
                  <span className="cc-grid-eyebrow">
                    Talk to a solution architect
                  </span>
                  <FormCopyLede>
                    Reply within one business day. 30-minute discovery call.
                    Tailored demo and POC plan. DPA and SOC 2 report on request.
                  </FormCopyLede>
                </FormCopy>
                <FormScope>
                  <SalesFormFields variant="inline" />
                </FormScope>
              </FormCell>
            </GridCard>
            <GridCard>
              <NextCell aria-label="What happens next">
                {NEXT_STEPS.map((step) => (
                  <NextStepItem key={step.key}>
                    <NextStepNum>{step.num}</NextStepNum>
                    <NextStepTitle>{step.title}</NextStepTitle>
                    <NextStepBody>{step.body}</NextStepBody>
                  </NextStepItem>
                ))}
              </NextCell>
            </GridCard>
          </GridSplit>
        </section>
      </GridSection>
    );
  }
);
