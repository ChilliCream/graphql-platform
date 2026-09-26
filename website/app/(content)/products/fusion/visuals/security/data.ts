/**
 * The script the Security Checkpoint graphic replays: the clients that send
 * requests, the router's stacked policy checks, and the services behind it.
 * Every event names which check (if any) stops the request, so the router
 * never has to be told twice which state to draw.
 */

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

/** The router's three stacked checks, in evaluation order. */
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
  /** Index into `CLIENTS`. */
  readonly client: number;
  readonly operation: string;
  /** Index into `CHECKS` this request fails at, or `null` when it passes all three. */
  readonly failAt: number | null;
  /** Index into `SERVICES` the request reaches, `null` when it never does. */
  readonly service: number | null;
  readonly auditLine: string;
}

/** One cycle of the checkpoint: two allowed, one denied, one rejected before the safelist check. */
export const EVENTS: readonly CheckpointEvent[] = [
  {
    client: 0,
    operation: "query GetOrder",
    failAt: null,
    service: 0,
    auditLine: "allowed · query GetOrder · trusted",
  },
  {
    client: 1,
    operation: "mutation UpdatePrice",
    failAt: null,
    service: 1,
    auditLine: "allowed · mutation UpdatePrice · OPA",
  },
  {
    client: 2,
    operation: "mutation DeleteCustomer",
    failAt: 1,
    service: null,
    auditLine: "denied · mutation DeleteCustomer · OPA · 403",
  },
  {
    client: 3,
    operation: "query 7f3a…c21",
    failAt: 0,
    service: null,
    auditLine: "rejected · unknown operation · not in safelist",
  },
  {
    client: 0,
    operation: "query GetInvoice",
    failAt: null,
    service: 2,
    auditLine: "allowed · query GetInvoice · custom policy",
  },
];

/** Ms per phase; four phases (arrive, check, resolve, settle) per event. */
export const PHASE_MS = 900;

export const PHASES_PER_EVENT = 4;

export type CheckStatus = "idle" | "pass" | "fail";

/** A check row's status for a request that fails at `failAt` (or never fails). */
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

/** Decodes a cycle step into the event it belongs to and its phase within it. */
export function frameAt(step: number): CheckpointFrame {
  const eventIndex = Math.floor(step / PHASES_PER_EVENT) % EVENTS.length;
  return {
    eventIndex,
    event: EVENTS[eventIndex],
    phase: step % PHASES_PER_EVENT,
  };
}

/** Wraps `index` into `[0, length)`, for reading the audit history backwards. */
export function wrap(index: number, length: number): number {
  return ((index % length) + length) % length;
}
