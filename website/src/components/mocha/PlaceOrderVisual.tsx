"use client";

import { MediatorVisual } from "@/app/(content)/products/mocha/visuals/MediatorVisual";

/**
 * The mediator pipeline with the comparison articles' example: `PlaceOrder`
 * goes through the middleware to `PlaceOrderHandler`, which returns a `Guid`.
 */
export function PlaceOrderVisual() {
  return <MediatorVisual handler="PlaceOrderHandler" result="Guid" />;
}
