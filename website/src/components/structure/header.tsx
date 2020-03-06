import { graphql, useStaticQuery } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetSiteMetadataQuery } from "../../../graphql-types";
import { Link } from "../misc/link";

import GithubIconSvg from "../../images/github.svg";
import LogoIconSvg from "../../images/chillicream.svg";
import LogoTextSvg from "../../images/chillicream-text.svg";
import SlackIconSvg from "../../images/slack.svg";
import TwitterIconSvg from "../../images/twitter.svg";

export const Header: FunctionComponent = () => {
  const data = useStaticQuery<GetSiteMetadataQuery>(graphql`
    query getSiteMetadata {
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
      <LogoLink to="/">
        <LogoIcon />
        <LogoText />
      </LogoLink>
      <Navigation>
        {topnav!.map(item => (
          <NavLink to={item!.link!}>{item!.name}</NavLink>
        ))}
      </Navigation>
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
    </Container>
  );
};

const Container = styled.header`
  position: sticky;
  display: flex;
  padding: 0 20px;
  height: 60px;
  background-color: #f40010;
  box-shadow: 0px 3px 3px 0px rgba(0, 0, 0, 0.25);
`;

const LogoIcon = styled(LogoIconSvg)`
  height: 40px;
  fill: #fff;
`;

const LogoLink = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
`;

const LogoText = styled(LogoTextSvg)`
  display: none;
  padding-left: 15px;
  height: 24px;
  fill: #fff;

  @media only screen and (min-width: 1200px) {
    display: inline;
  }
`;

const Navigation = styled.nav`
  display: flex;
  flex: 1 1 auto;
  flex-direction: row;
  align-items: center;
  justify-content: center;
  padding: 0 15px;
`;

const NavLink = styled(Link)`
  flex: 0 0 auto;
  margin: 0 2px;
  border-radius: 4px;
  padding: 10px 15px;
  font-family: "Roboto", sans-serif;
  font-size: 1.25em;
  color: #fff;
  text-decoration: none;
  text-transform: uppercase;
  transition: background-color 0.2s ease-in-out;

  :hover {
    background-color: #b7020a;
  }
`;

const Search = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
`;

const SearchField = styled.input`
  border: 0;
  border-radius: 4px;
  padding: 10px 15px;
  font-family: "Roboto", sans-serif;
  font-size: 1.25em;
  background-color: #fff;
`;

const Tools = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
`;

const ToolLink = styled(Link)`
  flex: 0 0 auto;
  margin-left: 15px;
  text-decoration: none;

  > svg {
    fill: #fff;
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
