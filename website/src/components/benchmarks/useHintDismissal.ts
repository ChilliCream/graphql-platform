import { useEffect } from "react";

/** Lets Escape hide the tooltip of the hovered or keyboard focused hint button until the pointer leaves the button and focus moves away. */
export function useHintDismissal() {
  useEffect(() => {
    const dismiss = (event: KeyboardEvent) => {
      if (event.key !== "Escape") {
        return;
      }
      for (const button of document.querySelectorAll<HTMLElement>(
        "[data-hint-button]:is(:hover,:focus-visible)",
      )) {
        if (button.dataset.hintDismissed !== undefined) {
          continue;
        }
        button.dataset.hintDismissed = "";
        const release = () => {
          if (document.activeElement === button || button.matches(":hover")) {
            return;
          }
          delete button.dataset.hintDismissed;
          button.removeEventListener("pointerleave", onPointerLeave);
          button.removeEventListener("blur", onBlur);
        };
        // The pointer has left once `pointerleave` fires, and focus has moved once `blur` fires.
        const onPointerLeave = () => requestAnimationFrame(release);
        const onBlur = () => requestAnimationFrame(release);
        button.addEventListener("pointerleave", onPointerLeave);
        button.addEventListener("blur", onBlur);
      }
    };
    document.addEventListener("keydown", dismiss);
    return () => document.removeEventListener("keydown", dismiss);
  }, []);
}
