export const SUPPORT_PLANS = ["Startup", "Business", "Enterprise"] as const;

export type SupportPlan = typeof SUPPORT_PLANS[number];
