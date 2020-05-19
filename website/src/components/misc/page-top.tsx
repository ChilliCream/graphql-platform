import React, {
  createRef,
  FunctionComponent,
  useEffect,
  useCallback,
} from "react";
import styled from "styled-components";

import ArrowUpIconSvg from "../../images/arrow-up.svg";

export const PageTop: FunctionComponent = () => {
  const ref = createRef<HTMLButtonElement>();

  const handleClick = useCallback(() => {
    window.scrollTo(0, 0);
  }, []);

  useEffect(() => {
    const handler = () => {
      const top = document.body.scrollTop || document.documentElement.scrollTop;

      ref.current!.style.display = top > 60 ? "initial" : "none";
    };

    handler();
    document.addEventListener("scroll", handler);

    return () => {
      document.removeEventListener("scroll", handler);
    };
  });

  return (
    <JumpToTop ref={ref} onClick={handleClick}>
      <ArrowUpIconSvg />
    </JumpToTop>
  );
};

const JumpToTop = styled.button`
  position: fixed;
  right: 50px;
  bottom: 50px;
  z-index: 20;
  display: none;
  border-radius: 50%;
  padding: 8px;
  width: 50px;
  height: 50px;
  background-color: white;
  opacity: 0.6;
  box-shadow: 0px 3px 6px 0px rgba(0, 0, 0, 0.25);
  transition: opacity 0.2s ease-in-out;

  &:hover {
    opacity: 1;
  }

  svg {
    width: 30px;
    height: 30px;
  }

  @media only screen and (min-width: 1600px) {
    right: calc(((100vw - 1300px) / 2) - 100px);
  }
`;
