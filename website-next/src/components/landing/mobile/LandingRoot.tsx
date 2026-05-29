import type { ReactNode } from "react";

export const SHARED_PRODUCTS = [
  { key: "hot-chocolate", label: "Hot Chocolate" },
  { key: "nitro", label: "Nitro" },
  { key: "mocha", label: "Mocha" },
  { key: "strawberry-shake", label: "Strawberry Shake" },
] as const;

export const SHARED_SERVICES = [
  { key: "catalog", label: "Catalog", color: "var(--cc-col-cat)" },
  { key: "billing", label: "Billing", color: "var(--cc-col-bil)" },
  { key: "ordering", label: "Ordering", color: "var(--cc-col-ord)" },
  { key: "shipping", label: "Shipping", color: "var(--cc-col-shi)" },
  { key: "users", label: "Users", color: "var(--cc-col-usr)" },
] as const;

export function LandingRoot({ children }: { children: ReactNode }) {
  return <div className="cc-landing-mobile-root">{children}</div>;
}
