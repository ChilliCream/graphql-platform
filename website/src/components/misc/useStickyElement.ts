import { createRef, useEffect, useState } from "react";

export function useStickyElement<
  ContainerRef extends HTMLElement,
  ElementRef extends HTMLElement
>(beStickyStartingWithWidth: number) {
  const [beSticky, updateBeSticky] = useState(false);
  const containerRef = createRef<ContainerRef>();
  const elementRef = createRef<ElementRef>();

  useEffect(() => {
    const verify = () => {
      const currentViewportWidth =
        document.body.clientWidth || document.documentElement.clientWidth;

      updateBeSticky(currentViewportWidth >= beStickyStartingWithWidth);
    };

    verify();
    window.addEventListener("resize", verify);

    return () => {
      window.removeEventListener("resize", verify);
    };
  }, [beStickyStartingWithWidth]);

  useEffect(() => {
    if (containerRef.current && elementRef.current) {
      const container = containerRef.current;
      const element = elementRef.current;

      if (beSticky) {
        const recalculate = () => calculatePosition(container, element);

        recalculate();
        document.addEventListener("scroll", recalculate);
        window.addEventListener("resize", recalculate);

        return () => {
          document.removeEventListener("scroll", recalculate);
          window.removeEventListener("resize", recalculate);
        };
      } else {
        resetPosition(element);
      }
    }
  }, [beSticky]);

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

function resetPosition<ElementRef extends HTMLElement>(element: ElementRef) {
  element.style.position = "";
  element.style.top = "";
}
