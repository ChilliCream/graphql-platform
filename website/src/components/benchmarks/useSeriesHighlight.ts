import { useState } from "react";

/** The highlighted series of a history chart: the hovered or focused one, otherwise the pinned one. */
export function useSeriesHighlight() {
  const [hovered, setHovered] = useState<string>();
  const [pinned, setPinned] = useState<string>();
  return {
    active: hovered ?? pinned,
    pinned,
    setHovered,
    togglePinned: (id: string) =>
      setPinned((current) => (current === id ? undefined : id)),
  };
}
