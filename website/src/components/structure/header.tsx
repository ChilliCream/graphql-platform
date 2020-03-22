import { graphql, useStaticQuery } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetHeaderDataQuery } from "../../../graphql-types";
import { Link } from "../misc/link";

import GithubIconSvg from "../../images/github.svg";
import LogoIconSvg from "../../images/chillicream.svg";
import LogoTextSvg from "../../images/chillicream-text.svg";
import SlackIconSvg from "../../images/slack.svg";
import TwitterIconSvg from "../../images/twitter.svg";

export const Header: FunctionComponent = () => {
  const data = useStaticQuery<GetHeaderDataQuery>(graphql`
    query getHeaderData {
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
    }
  `);
  const { topnav, tools } = data.site!.siteMetadata!;

  return (
    <Container id="header">
      <ContainerWrapper>
        <LogoLink to="/">
          <LogoIcon />
          <LogoText />
        </LogoLink>
        <Navigation>
          {topnav!.map((item, index) => (
            <NavLink
              key={`topnav-item-${index}`}
              to={item!.link!}
              activeClassName="active"
              partiallyActive
            >
              {item!.name}
            </NavLink>
          ))}
        </Navigation>
        <Group>
          <Search>
            <SearchField placeholder="Search ..." />
          </Search>
          <Tools>
            <ToolLink to={tools!.slack!}>
              <SlackIcon />
            </ToolLink>
            <ToolLink to={tools!.twitter!}>
              <TwitterIcon />
            </ToolLink>
            <ToolLink to={tools!.github!}>
              <GithubIcon />
            </ToolLink>
          </Tools>
        </Group>
      </ContainerWrapper>
    </Container>
  );
};

const Container = styled.header`
  width: 100vw;
  height: 120px;
  background-color: #f40010;

  @media only screen and (min-width: 992px) {
    position: fixed;
    z-index: 20;
    height: 60px;
    box-shadow: 0px 3px 3px 0px rgba(0, 0, 0, 0.25);
  }
`;

const ContainerWrapper = styled.header`
  position: relative;
  display: flex;
  justify-content: center;
  height: 100%;

  @media only screen and (min-width: 992px) {
    justify-content: initial;
  }
`;

const LogoLink = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  padding: 0 20px;
  height: 60px;
`;

const LogoIcon = styled(LogoIconSvg)`
  height: 40px;
  fill: #fff;
`;

const LogoText = styled(LogoTextSvg)`
  display: none;
  padding-left: 15px;
  height: 24px;
  fill: #fff;

  @media only screen and (min-width: 600px) {
    display: inline;
  }

  @media only screen and (min-width: 992px) {
    display: none;
  }

  @media only screen and (min-width: 1200px) {
    display: inline;
  }
`;

const Navigation = styled.nav`
  position: absolute;
  bottom: 0;
  display: flex;
  flex: 1 1 auto;
  flex-direction: row;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 60px;

  @media only screen and (min-width: 992px) {
    position: initial;
    bottom: initial;
    width: initial;
  }
`;

const NavLink = styled(Link)`
  flex: 0 0 auto;
  margin: 0 2px;
  border-radius: 4px;
  padding: 10px 15px;
  font-family: "Roboto", sans-serif;
  font-size: 0.833em;
  color: #fff;
  text-decoration: none;
  text-transform: uppercase;
  transition: background-color 0.2s ease-in-out;

  &.active,
  &:hover {
    background-color: #b7020a;
  }
`;

const Group = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: row;
  justify-content: flex-end;
  padding: 0 20px;
  height: 60px;

  @media only screen and (min-width: 992px) {
    flex: 0 0 auto;
  }
`;

const Search = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;

  @media only screen and (min-width: 992px) {
    display: flex;
  }
`;

const SearchField = styled.input`
  border: 0;
  border-radius: 4px;
  padding: 10px 15px;
  font-family: "Roboto", sans-serif;
  font-size: 0.833em;
  line-height: 1em;
  background-color: #fff;
`;

const Tools = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;

  @media only screen and (min-width: 992px) {
    display: flex;
  }
`;

const ToolLink = styled(Link)`
  flex: 0 0 auto;
  margin-left: 15px;
  text-decoration: none;

  > svg {
    transition: fill 0.2s ease-in-out;
  }

  :hover > svg {
    fill: #b7020a;
  }
`;

const GithubIcon = styled(GithubIconSvg)`
  height: 26px;
  fill: #fff;
`;

const SlackIcon = styled(SlackIconSvg)`
  height: 22px;
  fill: #fff;
`;

const TwitterIcon = styled(TwitterIconSvg)`
  height: 22px;
  fill: #fff;
`;
