import styled from "styled-components";

import { Link } from "@/components/misc";
import { THEME_COLORS } from "@/style";

export const Box = styled.li`
  position: relative;
  display: flex;
  box-sizing: border-box;
  margin-bottom: 0;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--box-border-radius);
  width: 100%;
  backdrop-filter: blur(2px);
  background-color: #1511353d;

  @media only screen and (min-width: 860px) {
    width: calc((100% / 3) - (32px / 3));
  }

  @media only screen and (min-width: 992px) {
    width: calc((100% / 3) - (48px / 3));
  }
`;

export const BoxLink = styled(Link)`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;

  > .gatsby-image-wrapper {
    flex: 0 0 auto;
    border-radius: var(--box-border-radius) var(--box-border-radius) 0 0;
  }
`;

export const Boxes = styled.ul`
  display: flex;
  flex-direction: column;
  align-items: stretch;
  justify-content: flex-start;
  margin: 0;
  gap: 16px;
  list-style-type: none;

  & > ${Box}:nth-child(2) {
    animation-delay: 0.15s;
  }

  & > ${Box}:nth-child(3) {
    animation-delay: 0.3s;
  }

  @media only screen and (min-width: 860px) {
    flex-direction: row;
    flex-wrap: wrap;
  }

  @media only screen and (min-width: 992px) {
    gap: 24px;
  }
`;
