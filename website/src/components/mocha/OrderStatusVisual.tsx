"use client";

import { RequestReplyVisual } from "@/app/(content)/products/mocha/visuals/RequestReplyVisual";

/**
 * Request/reply with the comparison articles' example: the ordering service
 * sends `CheckOrderStatus` and awaits an `OrderStatus` response.
 */
export function OrderStatusVisual() {
  return (
    <RequestReplyVisual
      handler="CheckOrderStatusHandler"
      response="OrderStatus"
    />
  );
}
