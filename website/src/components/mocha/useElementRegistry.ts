"use client";

import { type RefCallback, useState } from "react";

export interface ElementRegistry {
  readonly els: Map<string, SVGElement | null>;
  readonly set: (key: string) => RefCallback<SVGElement>;
}

function createRegistry(): ElementRegistry {
  const els = new Map<string, SVGElement | null>();
  const callbacks = new Map<string, RefCallback<SVGElement>>();
  return {
    els,
    set: (key) => {
      let cb = callbacks.get(key);
      if (!cb) {
        cb = (node) => {
          els.set(key, node);
        };
        callbacks.set(key, cb);
      }
      return cb;
    },
  };
}

export function useElementRegistry(): ElementRegistry {
  const [registry] = useState(createRegistry);
  return registry;
}
