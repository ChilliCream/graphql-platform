import { graphql, useStaticQuery } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetFooterDataQuery } from "../../../graphql-types";
import { Link } from "../misc/link";

import GithubIconSvg from "../../images/github.svg";
import LogoIconSvg from "../../images/chillicream.svg";
import LogoTextSvg from "../../images/chillicream-text.svg";
import SlackIconSvg from "../../images/slack.svg";
import TwitterIconSvg from "../../images/twitter.svg";

export const Footer: FunctionComponent = () => {
  const data = useStaticQuery<GetFooterDataQuery>(graphql`
    query getFooterData {
      site {
        siteMetadata {
          topnav {
            name
            link
          }
          tools {
            github
            slack
            twitter
          }
        }
      }
      bg: file(relativePath: { eq: "footer.svg" }) {
        publicURL
      }
    }
  `);
  const { topnav, tools } = data.site!.siteMetadata!;

  return (
    <Container url={data.bg!.publicURL!}>
      <ContainerWrapper>
        <About>
          <Logo>
            <LogoIcon />
            <LogoText />
          </Logo>
          <Description>
            We at ChilliCream build the ultimate GraphQL platform.
            <br />
            Most of our code is open-source and remains forever open-source.
            <br />
            You can be part of it by helping us starting today.
          </Description>
          <Connect>
            <ConnectLink to={tools!.github!}>
              <GithubIcon /> to work with us on the platform
            </ConnectLink>
            <ConnectLink to={tools!.slack!}>
              <SlackIcon /> to get in touch with us
            </ConnectLink>
            <ConnectLink to={tools!.twitter!}>
              <TwitterIcon /> to stay up-to-date
            </ConnectLink>
          </Connect>
        </About>
        <Links>
          <Title>Links</Title>
          <Navigation>
            {topnav!.map((item, index) => (
              <NavLink key={`topnav-item-${index}`} to={item!.link!}>
                {item!.name}
              </NavLink>
            ))}
          </Navigation>
        </Links>
        <Location>
          <Title>Location</Title>
          <Description>You can find us in Zurich</Description>
        </Location>
      </ContainerWrapper>
      <ContainerWrapper>
        <Copyright>© {new Date().getFullYear()} ChilliCream</Copyright>
      </ContainerWrapper>
    </Container>
  );
};

const Container = styled.footer<{ url: string }>`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  align-items: center;
  justify-content: flex-end;
  padding: 200px 20px 60px;
  min-height: 300px;
  background-color: #fff;
  background-image: url("${props => props.url}");
  background-attachment: scroll;
  background-position-x: 50%;
  background-position-y: 0%;
  background-repeat: no-repeat;
  background-size: cover;
  color: #666;

  @media only screen and (min-width: 1250px) {
    padding: 200px 0 60px;
  }
`;

const ContainerWrapper = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  width: 100%;
  max-width: 1100px;
`;

const About = styled.div`
  display: flex;
  flex: 5 1 auto;
  flex-direction: column;
  padding: 0 10px;
`;

const Logo = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  margin-bottom: 10px;
`;

const LogoIcon = styled(LogoIconSvg)`
  height: 40px;
  fill: #666;
`;

const LogoText = styled(LogoTextSvg)`
  padding-left: 15px;
  height: 24px;
  fill: #666;
`;

const Description = styled.p`
  font-size: 0.833em;
  line-height: 1.5em;
  margin-bottom: 10px;
`;

const Connect = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
`;

const ConnectLink = styled(Link)`
  flex: 0 0 auto;
  margin: 5px 0;
  font-size: 0.833em;
  text-decoration: none;
  color: #666;
  transition: color 0.2s ease-in-out;

  > svg {
    margin-right: 10px;
    vertical-align: middle;
    transition: fill 0.2s ease-in-out;
  }

  :hover {
    color: #000;

    > svg {
      fill: #000;
    }
  }
`;

const GithubIcon = styled(GithubIconSvg)`
  height: 26px;
  fill: #666;
`;

const SlackIcon = styled(SlackIconSvg)`
  height: 22px;
  fill: #666;
`;

const TwitterIcon = styled(TwitterIconSvg)`
  height: 22px;
  fill: #666;
`;

const Links = styled.div`
  display: none;
  flex: 2 1 auto;
  flex-direction: column;
  padding: 0 10px;

  @media only screen and (min-width: 768px) {
    display: flex;
  }
`;

const Navigation = styled.nav`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
`;

const NavLink = styled(Link)`
  flex: 0 0 auto;
  margin: 4px 0;
  font-family: "Roboto", sans-serif;
  font-size: 0.833em;
  line-height: 1.5em;
  color: #666;
  text-decoration: none;
  transition: color 0.2s ease-in-out;

  :hover {
    color: #000;
  }
`;

const Location = styled.div`
  display: none;
  flex: 3 1 auto;
  flex-direction: column;
  padding: 0 10px;
  line-height: 1.5em;

  @media only screen and (min-width: 768px) {
    display: flex;
  }
`;

const Title = styled.h3`
  margin: 15px 0 0;
  font-size: 1em;
  font-weight: bold;
  color: #666;
`;

const Copyright = styled.div`
  margin-top: 20px;
  padding: 0 10px;
`;
