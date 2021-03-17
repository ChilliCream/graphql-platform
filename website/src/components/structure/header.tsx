import { graphql, useStaticQuery } from "gatsby";
import React, { FunctionComponent, useState } from "react";
import styled, { createGlobalStyle } from "styled-components";
import { GetHeaderDataQuery } from "../../../graphql-types";
import { IconContainer } from "../misc/icon-container";
import { Link } from "../misc/link";
import { Search } from "../misc/search";

import BarsIconSvg from "../../images/bars.svg";
import GithubIconSvg from "../../images/github.svg";
import LogoIconSvg from "../../images/chillicream-winking.svg";
import LogoTextSvg from "../../images/chillicream-text.svg";
import SlackIconSvg from "../../images/slack.svg";
import TimesIconSvg from "../../images/times.svg";
import TwitterIconSvg from "../../images/twitter.svg";
import { useSelector } from "react-redux";
import { State } from "../../state";

export const Header: FunctionComponent = () => {
  const showShadow = useSelector<State, boolean>(
    (state) => state.common.yScrollPosition > 0
  );

  const [topNavOpen, setTopNavOpen] = useState<boolean>(false);
  const data = useStaticQuery<GetHeaderDataQuery>(graphql`
    query getHeaderData {
      site {
        siteMetadata {
          siteUrl
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
  const { siteUrl, topnav, tools } = data.site!.siteMetadata!;

  const handleHamburgerOpenClick = () => {
    setTopNavOpen(true);
  };

  const handleHamburgerCloseClick = () => {
    setTopNavOpen(false);
  };

  return (
    <Container enableShadow={showShadow}>
      <BodyStyle disableScrolling={topNavOpen} />
      <ContainerWrapper>
        <LogoLink to="/">
          <LogoIcon />
          <LogoText />
        </LogoLink>
        <Navigation open={topNavOpen}>
          <NavigationHeader>
            <LogoLink to="/">
              <LogoIcon />
              <LogoText />
            </LogoLink>
            <HamburgerCloseButton onClick={handleHamburgerCloseClick}>
              <HamburgerCloseIcon />
            </HamburgerCloseButton>
          </NavigationHeader>
          <Nav>
            {topnav!.map((item, index) => (
              <NavItem key={`topnav-item-${index}`}>
                <NavLink
                  to={item!.link!}
                  activeClassName="active"
                  partiallyActive
                >
                  {item!.name}
                </NavLink>
              </NavItem>
            ))}
          </Nav>
        </Navigation>
        <Group>
          <Search siteUrl={siteUrl!} />
          <Tools>
            <ToolLink to={tools!.slack!}>
              <IconContainer>
                <SlackIcon />
              </IconContainer>
            </ToolLink>
            <ToolLink to={tools!.twitter!}>
              <IconContainer>
                <TwitterIcon />
              </IconContainer>
            </ToolLink>
            <ToolLink to={tools!.github!}>
              <IconContainer>
                <GithubIcon />
              </IconContainer>
            </ToolLink>
          </Tools>
        </Group>
        <HamburgerOpenButton onClick={handleHamburgerOpenClick}>
          <HamburgerOpenIcon />
        </HamburgerOpenButton>
      </ContainerWrapper>
    </Container>
  );
};

const Container = styled.header<{ enableShadow: boolean }>`
  position: fixed;
  z-index: 30;
  width: 100vw;
  height: 60px;
  background-color: var(--brand-color);
  ${({ enableShadow }) =>
    enableShadow && "box-shadow: 0px 3px 6px 0px rgba(0, 0, 0, 0.25);"}
  transition: box-shadow 0.2s ease-in-out;
`;

const BodyStyle = createGlobalStyle<{ disableScrolling: boolean }>`
  body {
    overflow-y: ${({ disableScrolling }) =>
      disableScrolling ? "hidden" : "initial"};

    @media only screen and (min-width: 992px) {
      overflow-y: initial;
    }
  }
`;

const ContainerWrapper = styled.div`
  position: relative;
  display: flex;
  justify-content: center;
  height: 100%;

  @media only screen and (min-width: 992px) {
    justify-content: initial;
  }

  @media only screen and (min-width: 1400px) {
    margin: 0 auto;
    width: 1400px;
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
    display: inline-block;
  }
`;

const HamburgerOpenButton = styled.div`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  margin-left: auto;
  padding: 0 20px;
  height: 60px;
  cursor: pointer;

  @media only screen and (min-width: 992px) {
    display: none;
  }
`;

const HamburgerOpenIcon = styled(BarsIconSvg)`
  height: 26px;
  fill: #fff;
`;

const Navigation = styled.nav<{ open: boolean }>`
  position: fixed;
  top: 0;
  right: 0;
  left: 0;
  z-index: 30;
  display: ${({ open }) => (open ? "flex" : "none")};
  flex: 1 1 auto;
  flex-direction: column;
  max-height: 100vh;
  background-color: var(--brand-color);
  opacity: ${({ open }) => (open ? "1" : "0")};
  box-shadow: 0px 3px 6px 0px rgba(0, 0, 0, 0.25);
  transition: opacity 0.2s ease-in-out;

  @media only screen and (min-width: 992px) {
    position: initial;
    top: initial;
    right: initial;
    left: initial;
    z-index: initial;
    display: flex;
    flex-direction: row;
    height: 60px;
    max-height: initial;
    background-color: initial;
    opacity: initial;
    box-shadow: initial;
  }
`;

const NavigationHeader = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  height: 60px;

  @media only screen and (min-width: 992px) {
    display: none;
  }
`;

const HamburgerCloseButton = styled.div`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  margin-left: auto;
  padding: 0 20px;
  height: 60px;
  cursor: pointer;

  @media only screen and (min-width: 992px) {
    display: none;
  }
`;

const HamburgerCloseIcon = styled(TimesIconSvg)`
  height: 26px;
  fill: #fff;
`;

const Nav = styled.ol`
  display: flex;
  flex: 1 1 calc(100% - 80px);
  flex-direction: column;
  align-items: center;
  justify-content: center;
  margin: 0;
  padding: 0;
  list-style-type: none;
  overflow-y: initial;

  @media only screen and (min-width: 992px) {
    flex: 1 1 auto;
    flex-direction: row;
    margin: 0;
    height: 60px;
    overflow-y: hidden;
  }
`;

const NavItem = styled.li`
  flex: 0 0 auto;
  margin: 0 2px;
  padding: 0;
  height: 50px;

  @media only screen and (min-width: 992px) {
    height: initial;
  }
`;

const NavLink = styled(Link)`
  flex: 0 0 auto;
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
    background-color: var(--brand-color-hover);
  }
`;

const Group = styled.div`
  display: none;
  flex: 1 1 auto;
  flex-direction: row;
  justify-content: flex-end;
  padding: 0 20px;
  height: 60px;

  @media only screen and (min-width: 284px) {
    display: flex;
  }

  @media only screen and (min-width: 992px) {
    flex: 0 0 auto;
  }
`;

const Tools = styled.div`
  display: none;
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

  > ${IconContainer} > svg {
    transition: fill 0.2s ease-in-out;
  }

  :hover > ${IconContainer} > svg {
    fill: var(--brand-color-hover);
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
