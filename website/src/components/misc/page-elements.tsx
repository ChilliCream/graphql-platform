import styled from "styled-components";
import { IsMobile, IsSmallDesktop } from "../../shared-style";

export const Intro = styled.header`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  padding: 25px 0;
  width: 100%;
  background-color: var(--primary-color);
  background: linear-gradient(
    180deg,
    var(--primary-color) 70%,
    #3d5f9f 100%
  ); //before: ff892a

  @media only screen and (min-width: 992px) {
    padding: 60px 0;
  }
`;

export const Title = styled.h3`
  flex: 0 0 auto;
  font-size: 1em;
  text-align: center;
  color: var(--quaternary-color);
  text-transform: uppercase;
`;

export const Hero = styled.h1`
  flex: 0 0 auto;
  margin-bottom: 20px;
  max-width: 800px;
  font-size: 2.222em;
  text-align: center;
  color: var(--text-color-contrast);
  ${IsSmallDesktop(`
    padding: 0 40px;
  `)}

  ${IsMobile(`
    padding: 0 15px;
  `)}
`;

export const Teaser = styled.p`
  flex: 0 0 auto;
  max-width: 800px;
  font-size: 1.222em;
  text-align: center;
  color: var(--quaternary-color);
  ${IsSmallDesktop(`
    padding: 0 40px;
  `)}

  ${IsMobile(`
    padding: 0 15px;
  `)}
`;
