import React, { FunctionComponent, useEffect, useRef } from "react";
import styled from "styled-components";
import { Footer } from "./footer";
import { PageTop } from "../../misc/page-top";
import { useDispatch } from "react-redux";
import { hasScrolled } from "../../../state/common";
import { ContentComponent } from "./content";

export const MainContentContainer: FunctionComponent = ({ children }) => {
  const ref = useRef<HTMLDivElement>(null);

  const dispatch = useDispatch();
  const handleScroll = () => {
    const top = ref.current?.scrollTop || 0;
    dispatch(hasScrolled({ yScrollPosition: top }));
  };

  useEffect(() => {
    ref.current?.addEventListener("scroll", handleScroll);
    return () => ref.current?.removeEventListener("scroll", handleScroll);
  }, []);

  useEffect(() => {
    const { hash } = window.location;

    if (hash) {
      const headlineElement = document.getElementById(hash.substring(1));

      if (headlineElement) {
        window.setTimeout(
          () => window.scrollTo(0, headlineElement.offsetTop - 80),
          100
        );
      }
    }
  });

  const scrollToTop = () => {
    ref.current?.scrollTo({
      top: 0,
      behavior: "smooth",
    });
  };

  return (
    <MainContentWrapper ref={ref}>
      <ContentComponent>{children}</ContentComponent>
      <Footer />
      <PageTop onTopScroll={scrollToTop} />
    </MainContentWrapper>
  );
};

const MainContentWrapper = styled.div`
  width: 100%;
  grid-row: 2;
  display: grid;
  grid-template-columns: 1fr;
  grid-template-rows: 1fr auto;
  justify-content: center;
  position: relative;
  overflow-y: auto;
`;
