import styled from "styled-components";

import { Link } from "@/components/misc/link";
import { MAX_CONTENT_WIDTH, THEME_COLORS } from "@/style";

export const CardOffer = styled.div`
  padding: 1.5rem 1.5rem 0;

  & > header {
    margin-bottom: 16px;
  }

  & > p {
    font-weight: normal;
    font-size: 1rem;
    line-height: 1.5;
    margin: 0 0 1.25rem;
    min-height: 5rem;
    color: ${THEME_COLORS.text};
  }
`;

export const CardsContainer = styled.div`
  display: grid;
  grid-template-columns: minmax(250px, 300px);
  align-items: stretch;
  justify-content: center;
  gap: 16px;
  max-width: ${MAX_CONTENT_WIDTH}px;
  overflow: visible;

  @media only screen and (min-width: 400px) {
    grid-template-columns: minmax(300px, 400px);
  }

  @media only screen and (min-width: 600px) {
    grid-template-columns: repeat(2, minmax(225px, 300px));
  }

  @media only screen and (min-width: 768px) {
    grid-template-columns: repeat(2, minmax(250px, 375px));
  }

  @media only screen and (min-width: 992px) {
    grid-template-columns: repeat(3, minmax(300px, 350px));
    gap: 24px;
  }

  @media only screen and (min-width: 1246px) {
    grid-template-columns: repeat(3, minmax(275px, 400px));
  }
`;

export const Card = styled.div`
  position: relative;
  display: grid;
  flex-direction: column;
  grid-template-rows: auto 1fr;
  box-sizing: border-box;
  margin: 0px;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--box-border-radius);
  padding: 0px;
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    to right bottom,
    #379dc83d,
    #2b80ad3d,
    #2263903d,
    #1a48743d,
    #112f573d
  );
  cursor: default;

  &:hover {
    //border-color: ;
  }
`;

export const CardDetails = styled.div`
  padding: 1.5rem 1.5rem 2rem;
  border-radius: 0 0 var(--border-radius) var(--border-radius);
  color: ${THEME_COLORS.text};
  background: #f5f5f5;

  & h3 {
    margin: 0;
    font-size: 1rem;
    font-weight: 700;
    line-height: 1.25;
    color: ${THEME_COLORS.secondary};
  }

  & ul {
    list-style: none;
    padding: 0;
    margin: 0.625rem 0 0;
    font-size: 1rem;
    line-height: 1.5;
    display: grid;
    gap: 0.5rem;
  }

  & li {
    margin: 0;
    display: grid;
    grid-template-columns: auto 1fr;
  }

  & svg {
    height: 1em;
    width: auto;
    margin-right: 0.5em;
    transform: translateY(0.25em);
    fill: ${THEME_COLORS.secondary};
  }

  & span {
    line-height: 1.5;
  }
`;

export const ActionLink = styled(Link)`
  align-items: center;
  border-radius: 6px;
  box-sizing: border-box;
  cursor: pointer;
  display: inline-flex;
  justify-content: center;
  transition: background 0.2s ease-in-out, border 0.2s ease-in-out,
    color 0.2s ease-in-out;
  line-height: 1;
  text-decoration: none;
  background: transparent;
  border: 1px solid ${THEME_COLORS.tertiary};
  color: ${THEME_COLORS.tertiary};
  font-size: 1rem;
  min-height: calc(2.25rem);
  min-width: calc(2.25rem);
  padding: 0.25rem 1rem;
  margin-bottom: 1.25rem;

  &:hover {
    border-color: ${THEME_COLORS.secondary};
    color: ${THEME_COLORS.secondary};
    background: #fafafa;
  }
`;
