"use client";

import dynamic from "next/dynamic";
import { type ComponentType, useId, useMemo, useState } from "react";

import { PlaybackToggle } from "@/src/components/mocha/PlaybackToggle";
import { VisualScroller } from "@/src/components/mocha/VisualScroller";
import { VisualSettingsContext } from "@/src/components/mocha/VisualSettings";

// Each visual loads only on pages that render it.
const VISUALS: Record<string, ComponentType> = {
  mediator: dynamic(() =>
    import("@/app/(content)/products/mocha/visuals/MediatorVisual").then(
      (m) => m.MediatorVisual,
    ),
  ),
  "request-reply": dynamic(() =>
    import("@/app/(content)/products/mocha/visuals/RequestReplyVisual").then(
      (m) => m.RequestReplyVisual,
    ),
  ),
  fanout: dynamic(() =>
    import("@/app/(content)/products/mocha/visuals/FanoutVisual").then(
      (m) => m.FanoutVisual,
    ),
  ),
  outbox: dynamic(() =>
    import("@/app/(content)/products/mocha/visuals/OutboxVisual").then(
      (m) => m.OutboxVisual,
    ),
  ),
  saga: dynamic(() =>
    import("@/app/(content)/products/mocha/visuals/SagaVisual").then(
      (m) => m.SagaVisual,
    ),
  ),
  "why-messaging": dynamic(() =>
    import("@/app/(content)/products/mocha/visuals/WhyMessagingVisual").then(
      (m) => m.WhyMessagingVisual,
    ),
  ),
  "build-time": dynamic(() =>
    import("@/src/components/mocha/BuildTimeVisual").then(
      (m) => m.BuildTimeVisual,
    ),
  ),
  retry: dynamic(() =>
    import("@/src/components/mocha/RetryVisual").then((m) => m.RetryVisual),
  ),
  // The comparison articles' running examples. "saga", "request-reply" and
  // "mediator" keep the product page's refund saga, GetProduct request and
  // PlaceOrderCommand pipeline.
  "order-saga": dynamic(() =>
    import("@/src/components/mocha/OrderSagaVisual").then(
      (m) => m.OrderSagaVisual,
    ),
  ),
  "order-status": dynamic(() =>
    import("@/src/components/mocha/OrderStatusVisual").then(
      (m) => m.OrderStatusVisual,
    ),
  ),
  "place-order": dynamic(() =>
    import("@/src/components/mocha/PlaceOrderVisual").then(
      (m) => m.PlaceOrderVisual,
    ),
  ),
};

interface MochaVisualProps {
  /** Key of the visual, e.g. "mediator" or "retry". */
  readonly name: string;
  /** One sentence on what the animation shows, rendered below it. */
  readonly caption: string;
}

/**
 * An animated Mocha diagram with a caption, for use in Markdown content. The
 * drawing is hidden from assistive technology; the caption describes it, and
 * a button outside the hidden part pauses the loop. Diagrams render in their
 * readable style, and scroll sideways where they are wider than the column.
 */
export function MochaVisual({ name, caption }: MochaVisualProps) {
  const [paused, setPaused] = useState(false);
  const settings = useMemo(() => ({ paused, readable: true }), [paused]);
  const captionId = useId();
  const Visual = VISUALS[name];
  if (!Visual) {
    throw new Error(
      `Unknown Mocha visual "${name}". Use one of: ${Object.keys(VISUALS).join(", ")}.`,
    );
  }

  return (
    <figure className="my-8">
      <div className="relative">
        <VisualSettingsContext.Provider value={settings}>
          <VisualScroller>
            <Visual />
          </VisualScroller>
        </VisualSettingsContext.Provider>
        <PlaybackToggle
          paused={paused}
          onToggle={() => setPaused((p) => !p)}
          describedBy={captionId}
          className="absolute top-3 right-3 z-10"
        />
      </div>
      <figcaption
        id={captionId}
        className="text-cc-ink-dim mt-3 text-center text-sm text-pretty"
      >
        {caption}
      </figcaption>
    </figure>
  );
}
