import styled from "styled-components";

export const Intro = styled.header`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  padding: 25px 0;
  width: 100%;
  background-color: var(--brand-color);
  background: linear-gradient(180deg, var(--brand-color) 70%, #ff892a 100%);

  @media only screen and (min-width: 992px) {
    padding: 60px 0;
  }
`;

export const Title = styled.h3`
  flex: 0 0 auto;
  font-size: 1em;
  text-align: center;
  color: rgba(255, 255, 255, 0.8);
  text-transform: uppercase;
`;

export const Hero = styled.h1`
  flex: 0 0 auto;
  margin: 0 60px 20px;
  max-width: 800px;
  font-size: 2.222em;
  text-align: center;
  text-shadow: 0px 3px 6px rgba(0, 0, 0, 0.25);
  color: #fff;
`;

export const Teaser = styled.p`
  flex: 0 0 auto;
  margin: 0 60px 20px;
  max-width: 800px;
  font-size: 1.222em;
  text-align: center;
  text-shadow: 0px 3px 6px rgba(0, 0, 0, 0.25);
  color: #fff;
`;
