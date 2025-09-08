import { createRef, RefObject, useLayoutEffect } from "react";
import { useDispatch } from "react-redux";

import { setArticleHeight } from "@/state/common";

export function useCalculateArticleHeight(): RefObject<HTMLDivElement> {
  const ref = createRef<HTMLDivElement>();
  const dispatch = useDispatch();

  useLayoutEffect(() => {
    const handleResize = () => {
      const totalArticleHeight = ref.current?.offsetHeight ?? 0;

      if (totalArticleHeight > 0) {
        const articleViewportHeight =
          window.innerHeight > totalArticleHeight
            ? totalArticleHeight - 25
            : window.innerHeight - 85;

        dispatch(
          setArticleHeight({
            articleHeight: articleViewportHeight + "px",
          })
        );
      } else {
        dispatch(
          setArticleHeight({
            articleHeight: "94vh",
          })
        );
      }
    };

    handleResize();
    window.addEventListener("resize", handleResize);

    return () => window.removeEventListener("resize", handleResize);
  });

  return ref;
}
