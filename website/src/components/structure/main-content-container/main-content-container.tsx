import React, { FunctionComponent, useEffect, useRef } from "react";
import { useDispatch } from "react-redux";
import styled from "styled-components";
import { hasScrolled } from "../../../state/common";
import { PageTop } from "../../misc/page-top";
import { ContentComponent } from "./content";
import { Footer } from "./footer";

export const MainContentContainer: FunctionComponent = ({ children }) => {
  const ref = useRef<HTMLDivElement>(null);
  const dispatch = useDispatch();

  useEffect(() => {
    dispatch(
      hasScrolled({
        yScrollPosition: 0,
      })
    );

    const handleScroll = () => {
      if (!ref.current || ref.current.scrollTop === undefined) {
        return;
      }

      dispatch(
        hasScrolled({
          yScrollPosition: ref.current.scrollTop,
        })
      );
    };

    ref.current?.addEventListener("scroll", handleScroll);
    return () => {
      ref.current?.removeEventListener("scroll", handleScroll);
    };
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
