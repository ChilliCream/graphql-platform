import styled from "styled-components";

import { Link } from "@/components/misc/link";
import { THEME_COLORS } from "@/shared-style";

export const CardOffer = styled.div`
  padding: 1.5rem 1.5rem 0;

  & > header {
    min-height: 5em;

    & > h2 {
      margin: 0;
      font-size: 1.25rem;
      font-weight: 600;
      line-height: 1.75em;
      color: ${THEME_COLORS.primary};
    }

    & strong {
      font-size: 1.125em;
      color: ${THEME_COLORS.secondary};
    }

    & small {
      font-size: 0.75em;
      color: ${THEME_COLORS.tertiary};
    }
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

export const CardsContainer = styled.div<{ readonly dense?: true }>`
  display: grid;
  grid-template-columns: minmax(250px, 300px);
  gap: 1rem;
  align-items: stretch;
  justify-content: center;
  overflow: visible;

  ${CardOffer} > header {
    min-height: ${({ dense }) => (dense ? 3.5 : 5)}em;
  }

  @media only screen and (min-width: 400px) {
    grid-template-columns: minmax(300px, 350px);
  }

  @media only screen and (min-width: 600px) {
    grid-template-columns: repeat(2, minmax(225px, 275px));
  }

  @media only screen and (min-width: 768px) {
    grid-template-columns: repeat(2, minmax(250px, 350px));
  }

  @media only screen and (min-width: 992px) {
    grid-template-columns: repeat(3, minmax(250px, 300px));
  }

  @media only screen and (min-width: 1320px) {
    grid-template-columns: repeat(3, minmax(275px, 375px));
  }
`;

export const Card = styled.div`
  background: ${THEME_COLORS.background};
  box-shadow: rgb(46 41 51 / 8%) 0px 1px 2px, rgb(71 63 79 / 8%) 0px 2px 4px;
  margin: 0px;
  box-sizing: border-box;
  position: relative;
  flex-direction: column;
  border: 1px solid #d9d7e0;
  border-radius: var(--border-radius);
  padding: 0px;
  display: grid;
  grid-template-rows: auto 1fr;
  cursor: default;
  transition: border 0.2 ease-in-out;

  &:hover {
    border-color: ${THEME_COLORS.primary};
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
