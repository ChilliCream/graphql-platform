import React, { FC, PropsWithChildren, useEffect } from "react";
import { useDispatch } from "react-redux";
import styled from "styled-components";

import { PageTop } from "@/components/misc";
import { hasScrolled } from "@/state/common";
import { Footer } from "./footer";
import { Header } from "./header";

export const Main: FC<PropsWithChildren<unknown>> = ({ children }) => {
  const dispatch = useDispatch();

  useEffect(() => {
    dispatch(
      hasScrolled({
        yScrollPosition: 0,
      })
    );

    const handleScroll = () => {
      dispatch(
        hasScrolled({
          yScrollPosition: window.scrollY,
        })
      );
    };

    window.addEventListener("scroll", handleScroll, { passive: true });

    return () => {
      window.removeEventListener("scroll", handleScroll);
    };
  }, [dispatch]);

  useEffect(() => {
    const { hash } = window.location;

    if (hash) {
      const headlineElement = document.getElementById(hash.substring(1));

      headlineElement?.scrollIntoView();
    }
  });

  const scrollToTop = () => {
    window.scrollTo({
      top: 0,
      behavior: "smooth",
    });
  };

  return (
    <Container>
      <Header />
      <Content>{children}</Content>
      <Footer />
      <PageTop onTopScroll={scrollToTop} />
    </Container>
  );
};

const Container = styled.div`
  position: relative;
  display: flex;
  flex-direction: column;
`;

const Content = styled.main`
  display: flex;
  position: relative;
  flex-direction: column;
  align-items: center;
  width: 100%;
  flex: 1 0 auto;
  overflow-x: hidden;

  /* Reset overflow-x if acrticle-layout is present to fix "position: sticky" */
  :has(.article-layout) {
    overflow-x: visible;
  }
`;
