export const SUPPORT_PLANS = ["Startup", "Business", "Enterprise"] as const;

export type SupportPlan = (typeof SUPPORT_PLANS)[number];

export const CONTACT_SUBJECTS = [
  "Schedule a Demo",
  "Pricing & Plans",
  "Technical Support",
  "Partnership",
  "Other",
] as const;

export type ContactSubject = (typeof CONTACT_SUBJECTS)[number];
