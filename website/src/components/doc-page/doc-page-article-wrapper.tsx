import React, { createRef, FunctionComponent, useEffect } from "react";
import styled from "styled-components";
import { DocPageDesktopGridColumns, IsSmallDesktop } from "./shared-style";
import { useDispatch } from "react-redux";
import { setArticleHeight } from "../../state/common";

export const ArticleWrapper: FunctionComponent = ({ children }) => {
  const ref = createRef<HTMLDivElement>();
  const dispatch = useDispatch();

  useEffect(() => {
    const handleResize = () => {
      const totalArticleHeight = ref.current?.offsetHeight ?? 0;

      if (totalArticleHeight > 0) {
        const articleViewportHeight =
          window.innerHeight > totalArticleHeight
            ? totalArticleHeight - 25
            : window.innerHeight - 85;
        dispatch(
          setArticleHeight({ articleHeight: articleViewportHeight + "px" })
        );
      } else {
        dispatch(setArticleHeight({ articleHeight: "94vh" }));
      }
    };
    window.addEventListener("resize", handleResize);
    handleResize();
    return () => window.removeEventListener("resize", handleResize);
  });

  return <ArticleWrapperElement ref={ref}>{children}</ArticleWrapperElement>;
};

export const ArticleWrapperElement = styled.div`
  display: grid;
  ${DocPageDesktopGridColumns};
  ${IsSmallDesktop(`
    grid-template-columns: 1fr;
  `)}
  grid-template-rows: 1fr;
  padding: 6px;
`;
