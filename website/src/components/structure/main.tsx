import React, { FC, useEffect, useRef } from "react";
import { useDispatch } from "react-redux";
import styled from "styled-components";
import { hasScrolled } from "../../state/common";
import { PageTop } from "../misc/page-top";
import { Footer } from "./footer";

export const Main: FC = ({ children }) => {
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
    <Container ref={ref}>
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
  margin-top: 60px;
  overflow-y: auto;
`;

const Content = styled.main`
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 100%;
  overflow: visible;
`;
