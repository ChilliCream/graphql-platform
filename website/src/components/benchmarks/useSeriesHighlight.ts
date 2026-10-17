import { useState } from "react";

/** The highlighted series of a history chart: the hovered one, otherwise the keyboard-focused one, otherwise the pinned one. */
export function useSeriesHighlight() {
  const [hovered, setHovered] = useState<string>();
  const [focused, setFocused] = useState<string>();
  const [pinned, setPinned] = useState<string>();
  return {
    active: hovered ?? focused ?? pinned,
    pinned,
    setHovered,
    setFocused,
    togglePinned: (id: string) =>
      setPinned((current) => (current === id ? undefined : id)),
  };
}
