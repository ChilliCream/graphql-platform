"use client";

import React from "react";

// Invisible 0x0 endpoint marker consumed by `<ConnectorLine>`. Drop one inside
// any positioned ancestor and pass its `id` to a connector to anchor a curve
// at this exact spot. Anchors are scoped to the document via their unique id;
// `<ConnectorLine>` resolves them through `document.querySelector` so the
// pair only needs to share a positioned ancestor, not a context.

export interface AnchorProps {
  /** Unique identifier referenced by `<ConnectorLine>` `from`/`to` props. */
  id: string;
  className?: string;
}

/**
 * Invisible zero-size span that publishes a named endpoint for `<ConnectorLine>`
 * to terminate on. Renders nothing visible and is hidden from assistive tech.
 */
export const Anchor: React.FC<AnchorProps> = ({ id, className }) => {
  return (
    <span
      data-cc-anchor={id}
      aria-hidden="true"
      className={className}
      style={{
        display: "inline-block",
        width: 0,
        height: 0,
        pointerEvents: "none",
      }}
    />
  );
};
