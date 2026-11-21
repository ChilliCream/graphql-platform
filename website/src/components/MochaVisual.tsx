"use client";

import dynamic from "next/dynamic";
import type { ComponentType } from "react";

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
};

interface MochaVisualProps {
  /** Key of the visual, e.g. "mediator" or "retry". */
  readonly name: string;
  /** One sentence on what the animation shows, rendered below it. */
  readonly caption: string;
}

/** An animated Mocha diagram with a caption, for use in Markdown content. */
export function MochaVisual({ name, caption }: MochaVisualProps) {
  const Visual = VISUALS[name];
  if (!Visual) {
    throw new Error(
      `Unknown Mocha visual "${name}". Use one of: ${Object.keys(VISUALS).join(", ")}.`,
    );
  }

  return (
    <figure className="my-8">
      <Visual />
      <figcaption className="text-cc-ink-dim mt-3 text-center text-sm text-pretty">
        {caption}
      </figcaption>
    </figure>
  );
}
