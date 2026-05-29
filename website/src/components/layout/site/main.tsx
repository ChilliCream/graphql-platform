import React, { FC, PropsWithChildren, useEffect, useState } from "react";
import { useDispatch } from "react-redux";
import styled from "styled-components";

import { PageTop } from "@/components/misc";
import { hasScrolled } from "@/state/common";
import { Footer } from "./footer";
import { Header } from "./header";
import { ScrollRootContext } from "./scroll-root-context";

export const Main: FC<PropsWithChildren<unknown>> = ({ children }) => {
  // Callback-ref into state so the ScrollRootContext re-renders descendants
  // once the container actually mounts.
  const [scrollEl, setScrollEl] = useState<HTMLDivElement | null>(null);
  const dispatch = useDispatch();

  useEffect(() => {
    dispatch(hasScrolled({ yScrollPosition: 0 }));
    if (!scrollEl) {
      return;
    }
    const handleScroll = () => {
      dispatch(hasScrolled({ yScrollPosition: scrollEl.scrollTop }));
    };
    scrollEl.addEventListener("scroll", handleScroll, { passive: true });
    return () => {
      scrollEl.removeEventListener("scroll", handleScroll);
    };
  }, [scrollEl, dispatch]);

  useEffect(() => {
    const { hash } = window.location;
    if (hash) {
      document.getElementById(hash.substring(1))?.scrollIntoView();
    }
  }, []);

  const scrollToTop = () => {
    scrollEl?.scrollTo({ top: 0, behavior: "smooth" });
  };

  return (
    <ScrollRootContext.Provider value={scrollEl}>
      <Container ref={setScrollEl}>
        <Header />
        <Content>{children}</Content>
        <Footer />
        <PageTop onTopScroll={scrollToTop} />
      </Container>
    </ScrollRootContext.Provider>
  );
};

const Container = styled.div`
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow-x: hidden;
  overflow-y: auto;
`;

const Content = styled.main`
  display: flex;
  position: relative;
  flex-direction: column;
  align-items: center;
  width: 100%;
  overflow: visible;
  flex: 1 0 auto;
`;
