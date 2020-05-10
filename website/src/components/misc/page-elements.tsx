import styled from "styled-components";

export const Intro = styled.header<{ url: string }>`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  padding: 25px 0;
  width: 100%;
  min-height: 500px;
  background-image: url("${(props) => props.url}");
  background-attachment: scroll;
  background-position-x: 50%;
  background-position-y: 100%;
  background-repeat: no-repeat;
  background-size: cover;

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
  text-shadow: 2px 2px 2px rgba(0, 0, 0, 0.25);
  color: #fff;
`;

export const Teaser = styled.p`
  flex: 0 0 auto;
  margin: 0 60px 20px;
  max-width: 800px;
  font-size: 1.222em;
  text-align: center;
  text-shadow: 2px 2px 2px rgba(0, 0, 0, 0.25);
  color: #fff;
`;

export const Section = styled.section`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 25px 0;

  > * {
    margin: 0 20px 20px;
    max-width: 800px;
  }
`;

export const SectionTitle = styled.h1`
  flex: 0 0 auto;
  font-size: 2.222em;
  text-align: center;
  color: #667;

  @media only screen and (min-width: 768px) {
    margin-bottom: 20px;
  }
`;
