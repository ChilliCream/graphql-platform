import { createRef, useEffect } from "react";

export function useStickyElement<
  ContainerRef extends HTMLElement,
  ElementRef extends HTMLElement
>() {
  const containerRef = createRef<ContainerRef>();
  const elementRef = createRef<ElementRef>();

  useEffect(() => {
    let handler: () => void | undefined;

    if (containerRef.current && elementRef.current) {
      const container = containerRef.current;
      const element = elementRef.current;

      calculatePosition(container, element);
      handler = () => calculatePosition(container, element);
      document.addEventListener("scroll", handler);
      window.addEventListener("resize", handler);
    }

    return () => {
      if (handler) {
        document.removeEventListener("scroll", handler);
        window.removeEventListener("resize", handler);
      }
    };
  });

  return {
    containerRef,
    elementRef,
  };
}

function calculatePosition<
  ContainerRef extends HTMLElement,
  ElementRef extends HTMLElement
>(container: ContainerRef, element: ElementRef) {
  const scrollTop =
    document.body.scrollTop || document.documentElement.scrollTop;
  const containerHeightAndTop = container.offsetTop + container.offsetHeight;

  if (scrollTop + element.offsetHeight >= containerHeightAndTop) {
    element.style.position = "absolute";
    element.style.top = container.offsetHeight - element.offsetHeight + "px";
  } else {
    element.style.position = "fixed";
    element.style.top = "initial";
  }
}
