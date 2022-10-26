import { graphql, useStaticQuery } from "gatsby";
import React, {
  FC,
  ReactElement,
  ReactNode,
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";
import styled, { createGlobalStyle } from "styled-components";
import {
  DocsJson,
  DocsJsonVersions,
  GetHeaderDataQuery,
  Maybe,
  SiteSiteMetadataTools,
} from "../../../graphql-types";
import BarsIconSvg from "../../images/bars.svg";
import LogoTextSvg from "../../images/chillicream-text.svg";
import LogoIconSvg from "../../images/chillicream-winking.svg";
import GithubIconSvg from "../../images/github.svg";
import SearchIconSvg from "../../images/search.svg";
import SlackIconSvg from "../../images/slack.svg";
import TimesIconSvg from "../../images/times.svg";
import TwitterIconSvg from "../../images/twitter.svg";
import YouTubeIconSvg from "../../images/youtube.svg";
import { FONT_FAMILY_HEADING, THEME_COLORS } from "../../shared-style";
import { useObservable } from "../../state";
import { IconContainer } from "../misc/icon-container";
import { Link } from "../misc/link";
import { SearchModal } from "../misc/search-modal";

export const Header: FC = () => {
  const containerRef = useRef<HTMLHeadingElement>(null);
  const [topNavOpen, setTopNavOpen] = useState<boolean>(false);
  const [searchOpen, setSearchOpen] = useState<boolean>(false);
  const data = useStaticQuery<GetHeaderDataQuery>(graphql`
    query getHeaderData {
      site {
        siteMetadata {
          siteUrl
          tools {
            bcp
            github
            shop
            slack
            twitter
            youtube
          }
        }
      }
      docNav: file(
        sourceInstanceName: { eq: "docs" }
        relativePath: { eq: "docs.json" }
      ) {
        products: childrenDocsJson {
          path
          title
          versions {
            path
          }
        }
      }
    }
  `);
  const { siteUrl, tools } = data.site!.siteMetadata!;
  const products = data.docNav!.products!;
  const showShadow$ = useObservable((state) => {
    return state.common.yScrollPosition > 0;
  });

  const handleTopNavClose = useCallback(() => {
    setTopNavOpen(false);
  }, [setTopNavOpen]);

  const handleTopNavOpen = useCallback(() => {
    setTopNavOpen(true);
  }, [setTopNavOpen]);

  const handleSearchClose = useCallback(() => {
    setSearchOpen(false);
  }, [setSearchOpen]);

  const handleSearchOpen = useCallback(() => {
    setSearchOpen(true);
  }, [setSearchOpen]);

  useEffect(() => {
    const classes = containerRef.current?.className ?? "";

    const subscription = showShadow$.subscribe((showShadow) => {
      if (containerRef.current) {
        containerRef.current.className =
          classes + (showShadow ? " shadow" : "");
      }
    });

    return () => {
      subscription.unsubscribe();
    };
  }, [showShadow$]);

  return (
    <Container ref={containerRef}>
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
            <HamburgerCloseButton onClick={handleTopNavClose}>
              <HamburgerCloseIcon />
            </HamburgerCloseButton>
          </NavigationHeader>
          <Nav>
            <ProductNavItem />
            <SupportNavItem />
            <DeveloperNavItem products={products} tools={tools!} />
            <CompanyNavItem tools={tools!} />
          </Nav>
        </Navigation>
        <Group>
          <Tools>
            <ToolButton onClick={handleSearchOpen}>
              <IconContainer size={20}>
                <SearchIconSvg />
              </IconContainer>
            </ToolButton>
            <LaunchLink to={tools!.bcp!}>Open Banana Cake Pop</LaunchLink>
          </Tools>
        </Group>
        <HamburgerOpenButton onClick={handleTopNavOpen}>
          <HamburgerOpenIcon />
        </HamburgerOpenButton>
      </ContainerWrapper>
      <SearchModal
        open={searchOpen}
        siteUrl={siteUrl!}
        onClose={handleSearchClose}
      />
    </Container>
  );
};

const Container = styled.header`
  position: fixed;
  z-index: 30;
  width: 100vw;
  height: 60px;
  background-color: ${THEME_COLORS.primary};
  transition: box-shadow 0.2s ease-in-out;

  &.shadow {
    box-shadow: 0px 3px 6px 0px rgba(0, 0, 0, 0.25);
  }
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
  fill: ${THEME_COLORS.textContrast};
  transition: fill 0.2s ease-in-out;
`;

const LogoText = styled(LogoTextSvg)`
  display: none;
  padding-left: 15px;
  height: 24px;
  fill: ${THEME_COLORS.textContrast};
  transition: fill 0.2s ease-in-out;

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
  fill: ${THEME_COLORS.textContrast};
`;

const Navigation = styled.nav<{ readonly open: boolean }>`
  position: fixed;
  top: 0;
  right: 0;
  left: 0;
  z-index: 30;
  display: ${({ open }) => (open ? "flex" : "none")};
  flex: 1 1 auto;
  flex-direction: column;
  max-height: 100vh;
  background-color: ${THEME_COLORS.primary};
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
  fill: ${THEME_COLORS.textContrast};
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
  }
`;

const ProductNavItem: FC = () => {
  const [subNav, showSubNav, hideSubNav] = useSubNav((hideSubNav) => (
    <>
      <Products>
        <ProductLink
          to="/products/bananacakepop"
          onClick={() => {
            hideSubNav();
          }}
        >
          Banana Cake Pop
        </ProductLink>
        <ProductLink to="/docs/hotchocolate" onClick={hideSubNav}>
          Hot Chocolate
        </ProductLink>
        <ProductLink to="/docs/strawberryshake" onClick={hideSubNav}>
          Strawberry Shake
        </ProductLink>
      </Products>
      <AdditionalInfo></AdditionalInfo>
    </>
  ));

  return (
    <NavItemContainer onMouseOver={showSubNav} onMouseOut={hideSubNav}>
      <NavLink
        to="/products"
        activeClassName="active"
        partiallyActive
        onClick={(event) => {
          showSubNav();
          event.preventDefault();
        }}
      >
        Products
      </NavLink>
      {subNav}
    </NavItemContainer>
  );
};

const SupportNavItem: FC = () => {
  return (
    <NavItemContainer>
      <NavLink to="/support" activeClassName="active" partiallyActive>
        Support
      </NavLink>
    </NavItemContainer>
  );
};

interface DeveloperNavItemProps {
  readonly products: Maybe<
    Pick<DocsJson, "path" | "title"> & {
      versions?: Maybe<Maybe<Pick<DocsJsonVersions, "path">>[]>;
    }
  >[];
  readonly tools: Pick<
    SiteSiteMetadataTools,
    "bcp" | "github" | "shop" | "slack" | "twitter" | "youtube"
  >;
}

const DeveloperNavItem: FC<DeveloperNavItemProps> = ({ products, tools }) => {
  const [subNav, showSubNav, hideSubNav] = useSubNav((hideSubNav) => (
    <>
      <Products>
        {products.map((product, index) => (
          <ProductLink
            key={`products-item-${index}`}
            to={`/docs/${product!.path!}/`}
            onClick={hideSubNav}
          >
            {product!.title}
          </ProductLink>
        ))}
        <ProductLink to="/blog" onClick={hideSubNav}>
          Blog
        </ProductLink>
        <ToolLink to={tools.slack!} onClick={hideSubNav}>
          <IconContainer>
            <SlackIcon />
          </IconContainer>
        </ToolLink>
        <ToolLink to={tools.twitter!} onClick={hideSubNav}>
          <IconContainer>
            <TwitterIcon />
          </IconContainer>
        </ToolLink>
        <ToolLink to={tools.youtube!} onClick={hideSubNav}>
          <IconContainer>
            <YouTubeIcon />
          </IconContainer>
        </ToolLink>
        <ToolLink to={tools.github!} onClick={hideSubNav}>
          <IconContainer>
            <GithubIcon />
          </IconContainer>
        </ToolLink>
      </Products>
      <AdditionalInfo></AdditionalInfo>
    </>
  ));

  return (
    <NavItemContainer onMouseOver={showSubNav} onMouseOut={hideSubNav}>
      <NavLink
        to="/docs"
        activeClassName="active"
        partiallyActive
        onClick={(event) => {
          showSubNav();
          event.preventDefault();
        }}
      >
        Developers
      </NavLink>
      {subNav}
    </NavItemContainer>
  );
};

interface CompanyNavItemProps {
  readonly tools: Pick<
    SiteSiteMetadataTools,
    "bcp" | "github" | "shop" | "slack" | "twitter" | "youtube"
  >;
}

const CompanyNavItem: FC<CompanyNavItemProps> = ({ tools }) => {
  const [subNav, showSubNav, hideSubNav] = useSubNav((hideSubNav) => (
    <>
      <Products>
        <ProductLink to={tools.shop!} onClick={hideSubNav}>
          Shop
        </ProductLink>
      </Products>
      <AdditionalInfo></AdditionalInfo>
    </>
  ));

  return (
    <NavItemContainer onMouseOver={showSubNav} onMouseOut={hideSubNav}>
      <NavLink
        to="/company"
        activeClassName="active"
        partiallyActive
        onClick={(event) => {
          showSubNav();
          event.preventDefault();
        }}
      >
        Company
      </NavLink>
      {subNav}
    </NavItemContainer>
  );
};

function useSubNav(
  children: (hideSubNav: () => void) => ReactNode
): [ReactElement, () => void, () => void] {
  const ref = useRef<HTMLDivElement>(null);

  const hideSubNav = useCallback(() => {
    if (ref.current) {
      ref.current.classList.remove("show");
    }
  }, []);

  const showSubNav = useCallback(() => {
    if (ref.current) {
      ref.current.classList.add("show");
    }
  }, []);

  const subNavContainer = (
    <SubNavContainer ref={ref}>
      <SubNav>{children(hideSubNav)}</SubNav>
    </SubNavContainer>
  );

  return [subNavContainer, showSubNav, hideSubNav];
}

const NavLink = styled(Link)`
  flex: 0 0 auto;
  border-radius: var(--border-radius);
  padding: 10px 15px;
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 0.833em;
  font-weight: 500;
  color: ${THEME_COLORS.textContrast};
  text-decoration: none;
  transition: background-color 0.2s ease-in-out;

  &.active {
    background-color: ${THEME_COLORS.tertiary};
  }

  &.active:hover,
  &:hover {
    background-color: ${THEME_COLORS.secondary};
  }
`;

const SubNavContainer = styled.div`
  position: fixed;
  z-index: 1;
  top: 50px;
  right: calc(50% - 350px);
  left: calc(50% - 350px);
  display: none;
  flex-direction: column;
  align-items: center;
  overflow: visible;

  &.show {
    display: flex;
  }
`;

const NavItemContainer = styled.li`
  flex: 0 0 auto;
  margin: 0;
  padding: 0;
  height: 50px;

  @media only screen and (min-width: 992px) {
    height: initial;

    &:hover ${NavLink} {
      background-color: ${THEME_COLORS.secondary};
    }
  }
`;

const SubNav = styled.div`
  display: flex;
  flex-direction: row;
  margin-top: 2px;
  border-radius: var(--border-radius);
  width: 700px;
  background-color: ${THEME_COLORS.background};
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);
`;

const AdditionalInfo = styled.div`
  display: flex;
  flex: 1 1 40%;
  flex-direction: column;
  border-radius: 0 var(--border-radius) var(--border-radius) 0;
  padding: 10px 15px;
  background-color: ${THEME_COLORS.backgroundAlt};
`;

const Products = styled.div`
  display: flex;
  flex: 1 1 60%;
  flex-direction: column;
  padding: 20px 0;
`;

const ProductLink = styled(Link)`
  display: flex;
  margin: 10px 30px;
  border-radius: var(--border-radius);
  border: 1px solid ${THEME_COLORS.boxBorder};
  width: auto;
  min-height: 60px;
  padding: 10px 15px;
  background-color: ${THEME_COLORS.background};

  &:hover {
    background-color: ${THEME_COLORS.boxHighlight};
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
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
`;

const LaunchLink = styled(Link)`
  flex: 0 0 auto;
  margin-left: 5px;
  border-radius: var(--border-radius);
  padding: 10px 15px;
  color: ${THEME_COLORS.primaryButtonText};
  background-color: ${THEME_COLORS.primaryButton};
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 0.833em;
  text-decoration: none;
  font-weight: 500;
  transition: background-color 0.2s ease-in-out, color 0.2s ease-in-out;

  :hover {
    color: ${THEME_COLORS.primaryButtonHoverText};
    background-color: ${THEME_COLORS.primaryButtonHover};
  }
`;

const ToolButton = styled.button`
  flex: 0 0 auto;
  margin-left: 5px;
  border-radius: var(--border-radius);
  padding: 7px;
  transition: background-color 0.2s ease-in-out;

  > ${IconContainer} > svg {
    fill: ${THEME_COLORS.textContrast};
  }

  :hover {
    background-color: ${THEME_COLORS.secondary};
  }
`;

const ToolLink = styled(Link)`
  flex: 0 0 auto;
  margin-left: 5px;
  border-radius: var(--border-radius);
  padding: 7px;
  text-decoration: none;
  transition: background-color 0.2s ease-in-out;

  > ${IconContainer} > svg {
    fill: ${THEME_COLORS.textContrast};
  }

  :hover {
    background-color: ${THEME_COLORS.secondary};
  }
`;

const GithubIcon = styled(GithubIconSvg)`
  height: 26px;
`;

const SlackIcon = styled(SlackIconSvg)`
  height: 22px;
`;

const TwitterIcon = styled(TwitterIconSvg)`
  height: 22px;
`;

const YouTubeIcon = styled(YouTubeIconSvg)`
  height: 22px;
`;
