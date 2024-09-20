import { useLayoutEffect } from "react";

export function useAnimationIntersectionObserver(): void {
  useLayoutEffect(() => {
    const elements = document.querySelectorAll(".animate");

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("play");
          }
        });
      },
      {
        rootMargin: "72px 0px 0px 0px",
        threshold: 0.25,
      }
    );

    elements.forEach((element) => {
      observer.observe(element);
    });

    return () => {
      observer.disconnect();
    };
  }, []);
}
