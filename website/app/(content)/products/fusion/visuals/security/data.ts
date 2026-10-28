export interface ClientSpec {
  readonly key: string;
  readonly label: string;
}

export const CLIENTS: readonly ClientSpec[] = [
  { key: "web", label: "Web" },
  { key: "mobile", label: "Mobile" },
  { key: "partner", label: "Partner" },
  { key: "unknown", label: "Unknown op" },
];

export interface CheckSpec {
  readonly key: string;
  readonly label: string;
}

export const CHECKS: readonly CheckSpec[] = [
  { key: "safelist", label: "Safelist" },
  { key: "opa", label: "OPA" },
  { key: "custom", label: "Custom policy" },
];

export interface ServiceSpec {
  readonly key: string;
  readonly label: string;
}

export const SERVICES: readonly ServiceSpec[] = [
  { key: "catalog", label: "Catalog" },
  { key: "orders", label: "Orders" },
  { key: "billing", label: "Billing" },
];

export interface CheckpointEvent {
  readonly client: number;
  readonly failAt: number | null;
  readonly service: number | null;
  readonly auditLine: string;
}

export const EVENTS: readonly CheckpointEvent[] = [
  {
    client: 0,
    failAt: null,
    service: 0,
    auditLine: "allowed · query GetOrder · trusted",
  },
  {
    client: 1,
    failAt: null,
    service: 1,
    auditLine: "allowed · mutation UpdatePrice · OPA",
  },
  {
    client: 2,
    failAt: 1,
    service: null,
    auditLine: "denied · mutation DeleteCustomer · OPA · 403",
  },
  {
    client: 3,
    failAt: 0,
    service: null,
    auditLine: "rejected · unknown operation · not in safelist",
  },
  {
    client: 0,
    failAt: null,
    service: 2,
    auditLine: "allowed · query GetInvoice · custom policy",
  },
];

export const PHASE_MS = 900;

export const PHASES_PER_EVENT = 4;

export type CheckStatus = "idle" | "pass" | "fail";

export function checkStatus(
  failAt: number | null,
  rowIndex: number,
): CheckStatus {
  if (failAt === null) return "pass";
  if (rowIndex < failAt) return "pass";
  if (rowIndex === failAt) return "fail";
  return "idle";
}

export interface CheckpointFrame {
  readonly eventIndex: number;
  readonly event: CheckpointEvent;
  readonly phase: number;
}

export function frameAt(step: number): CheckpointFrame {
  const eventIndex = Math.floor(step / PHASES_PER_EVENT) % EVENTS.length;
  return {
    eventIndex,
    event: EVENTS[eventIndex],
    phase: step % PHASES_PER_EVENT,
  };
}

export function wrap(index: number, length: number): number {
  return ((index % length) + length) % length;
}
