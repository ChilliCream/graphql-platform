import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
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
import AngleRightIconSvg from "../../images/angle-right.svg";
import BarsIconSvg from "../../images/bars.svg";
import LogoTextSvg from "../../images/chillicream-text.svg";
import LogoIconSvg from "../../images/chillicream-winking.svg";
import GithubIconSvg from "../../images/github.svg";
import NewspaperIconSvg from "../../images/newspaper.svg";
import SearchIconSvg from "../../images/search.svg";
import SlackIconSvg from "../../images/slack.svg";
import TimesIconSvg from "../../images/times.svg";
import TwitterIconSvg from "../../images/twitter.svg";
import YouTubeIconSvg from "../../images/youtube.svg";
import { FONT_FAMILY_HEADING, THEME_COLORS } from "../../shared-style";
import { useObservable } from "../../state";
import { WorkshopNdcMinnesota } from "../images/workshop-ndc-minnesota";
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
      allMdx(
        limit: 1
        filter: { frontmatter: { path: { glob: "/blog/**/*" } } }
        sort: { fields: [frontmatter___date], order: DESC }
      ) {
        edges {
          node {
            id
            fields {
              readingTime {
                text
              }
            }
            frontmatter {
              featuredImage {
                childImageSharp {
                  gatsbyImageData(layout: CONSTRAINED, width: 400, quality: 100)
                }
              }
              path
              title
              date(formatString: "MMMM DD, YYYY")
            }
          }
        }
      }
    }
  `);
  const { siteUrl, tools } = data.site!.siteMetadata!;
  const products = data.docNav!.products!;
  const firstBlogPost = data.allMdx.edges[0].node;
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
            <ProductNavItem firstBlogPost={firstBlogPost} />
            <SupportNavItem />
            <DeveloperNavItem products={products} tools={tools!} />
            <ShopNavItem shopLink={tools!.shop!} />
          </Nav>
        </Navigation>
        <Group>
          <Tools>
            <SearchButton onClick={handleSearchOpen}>
              <IconContainer size={20}>
                <SearchIconSvg />
              </IconContainer>
            </SearchButton>
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
  bottom: 0;
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
    bottom: initial;
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

interface ProductNavItemProps {
  readonly firstBlogPost: any;
}

const ProductNavItem: FC<ProductNavItemProps> = ({ firstBlogPost }) => {
  const featuredImage =
    firstBlogPost.frontmatter!.featuredImage?.childImageSharp?.gatsbyImageData;

  const [subNav, showSubNav, hideSubNav] = useSubNav((hideSubNav) => (
    <>
      <SubNavMain>
        <ProductLink
          to="/products/bananacakepop"
          onClick={() => {
            hideSubNav();
          }}
        >
          <ProductLinkTitle>Banana Cake Pop</ProductLinkTitle>
          <ProductLinkDescription>
            The IDE to create, explore, manage, and test <em>GraphQL</em> APIs
            with ease.
          </ProductLinkDescription>
        </ProductLink>
        <ProductLink to="/docs/hotchocolate" onClick={hideSubNav}>
          <ProductLinkTitle>Hot Chocolate</ProductLinkTitle>
          <ProductLinkDescription>
            The server to create high-performance <em>.NET</em> <em>GraphQL</em>{" "}
            APIs in no time.
          </ProductLinkDescription>
        </ProductLink>
        <ProductLink to="/docs/strawberryshake" onClick={hideSubNav}>
          <ProductLinkTitle>Strawberry Shake</ProductLinkTitle>
          <ProductLinkDescription>
            The client to create modern <em>.NET</em> apps that consume{" "}
            <em>GraphQL</em> APIs effortless.
          </ProductLinkDescription>
        </ProductLink>
      </SubNavMain>
      <SubNavAdditionalInfo>
        <SubNavTitle>Latest Blog Post</SubNavTitle>
        <TeaserLink to={firstBlogPost.frontmatter!.path!}>
          {featuredImage && (
            <TeaserImage>
              <GatsbyImage
                image={featuredImage}
                alt={firstBlogPost.frontmatter!.title}
              />
            </TeaserImage>
          )}

          <TeaserMetadata>
            {firstBlogPost.frontmatter!.date!} ・{" "}
            {firstBlogPost.fields!.readingTime!.text!}
          </TeaserMetadata>
          <TeaserTitle>{firstBlogPost.frontmatter!.title}</TeaserTitle>
        </TeaserLink>
      </SubNavAdditionalInfo>
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
      <SubNavMain>
        <SubNavTitle>Documentation</SubNavTitle>
        {products.map((product, index) => (
          <SubNavLink
            key={`products-item-${index}`}
            to={`/docs/${product!.path!}/`}
            onClick={hideSubNav}
          >
            <IconContainer size={16}>
              <AngleRightIconSvg />
            </IconContainer>
            {product!.title}
          </SubNavLink>
        ))}
        <SubNavSeparator />
        <SubNavTitle>More Resources</SubNavTitle>
        <SubNavLink to="/blog" onClick={hideSubNav}>
          <IconContainer size={20}>
            <NewspaperIconSvg />
          </IconContainer>
          Blog
        </SubNavLink>
        <SubNavLink to={tools.slack!} onClick={hideSubNav}>
          <IconContainer size={20}>
            <SlackIconSvg />
          </IconContainer>
          Slack / Community
        </SubNavLink>
        <SubNavLink to={tools.twitter!} onClick={hideSubNav}>
          <IconContainer size={20}>
            <TwitterIconSvg />
          </IconContainer>
          Twitter
        </SubNavLink>
        <SubNavLink to={tools.youtube!} onClick={hideSubNav}>
          <IconContainer size={20}>
            <YouTubeIconSvg />
          </IconContainer>
          YouTube Channel
        </SubNavLink>
        <SubNavLink to={tools.github!} onClick={hideSubNav}>
          <IconContainer size={20}>
            <GithubIconSvg />
          </IconContainer>
          GitHub
        </SubNavLink>
      </SubNavMain>
      <SubNavAdditionalInfo>
        <SubNavTitle>Upcoming Workshop</SubNavTitle>
        <TeaserLink to="https://ndcminnesota.com/workshops/building-modern-applications-with-graphql-using-asp-net-core-6-hot-chocolate-and-relay/c58c2f23b8aa">
          <TeaserImage>
            <WorkshopNdcMinnesota />
          </TeaserImage>
          <TeaserMetadata>
            15 - 18 Nov 2022 ・ NDC {"{"} Minnesota {"}"}
          </TeaserMetadata>
          <TeaserTitle>
            Building Modern Apps with GraphQL in ASP.NET Core 7 and React 18
          </TeaserTitle>
        </TeaserLink>
      </SubNavAdditionalInfo>
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

interface ShopNavItemProps {
  readonly shopLink: string;
}

const ShopNavItem: FC<ShopNavItemProps> = ({ shopLink }) => {
  return (
    <NavItemContainer>
      <NavLink to={shopLink}>Shop</NavLink>
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
  top: 70px;
  right: 20px;
  bottom: 20px;
  left: 20px;
  display: none;
  flex-direction: column;
  align-items: center;
  overflow: visible;

  &.show {
    display: flex;
  }

  @media only screen and (min-width: 992px) {
    top: 50px;
    right: calc(50% - 350px);
    bottom: initial;
    left: calc(50% - 350px);
  }
`;

const NavItemContainer = styled.li`
  flex: 0 0 auto;
  margin: 0 4px;
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
  flex: 1 1 auto;
  flex-direction: row;
  border-radius: var(--border-radius);
  background-color: ${THEME_COLORS.background};
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);

  @media only screen and (min-width: 992px) {
    margin-top: 2px;
    width: 700px;
  }
`;

const SubNavMain = styled.div`
  display: flex;
  flex: 1 1 55%;
  flex-direction: column;
  padding: 25px 0;
`;

const SubNavTitle = styled.h1`
  margin: 5px 30px 10px;
  font-size: 0.75em;
  font-weight: 600;
  letter-spacing: 0.05em;
  color: ${THEME_COLORS.heading};
  transition: color 0.2s ease-in-out;
`;

const SubNavSeparator = styled.div`
  margin: 15px 30px;
  height: 1px;
  background-color: ${THEME_COLORS.backgroundAlt};
`;

const SubNavLink = styled(Link)`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 6px 30px;
  font-size: 0.833em;
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;

  ${IconContainer} {
    margin-right: 5px;

    > svg {
      fill: ${THEME_COLORS.text};
      transition: fill 0.2s ease-in-out;
    }
  }

  &:hover {
    color: ${THEME_COLORS.primary};

    ${IconContainer} > svg {
      fill: ${THEME_COLORS.primary};
    }
  }
`;

const ProductLinkTitle = styled.h1`
  margin-bottom: 6px;
  font-size: 1em;
  line-height: 1.5em;
  transition: color 0.2s ease-in-out;
`;

const ProductLinkDescription = styled.p`
  margin: 0;
  font-size: 0.833em;
  line-height: 1.5em;
  color: ${THEME_COLORS.primary};
  transition: color 0.2s ease-in-out;
`;

const ProductLink = styled(Link)`
  display: flex;
  flex-direction: column;
  margin: 5px 30px;
  border-radius: var(--border-radius);
  width: auto;
  min-height: 60px;
  padding: 10px 15px;
  background-color: ${THEME_COLORS.background};
  transition: background-color 0.2s ease-in-out;

  &:hover {
    background-color: ${THEME_COLORS.primary};

    ${ProductLinkTitle},
    ${ProductLinkDescription} {
      color: ${THEME_COLORS.background};
    }
  }
`;

const SubNavAdditionalInfo = styled.div`
  display: flex;
  flex: 1 1 45%;
  flex-direction: column;
  border-radius: 0 var(--border-radius) var(--border-radius) 0;
  padding: 25px 0;
  background-color: ${THEME_COLORS.backgroundAlt};
`;

const TeaserLink = styled(Link)`
  margin: 5px 30px;

  &:hover {
    > * {
      color: ${THEME_COLORS.primary};
    }

    .gatsby-image-wrapper {
      box-shadow: initial;
    }
  }
`;

const TeaserImage = styled.div`
  overflow: visible;

  .gatsby-image-wrapper {
    border-radius: var(--border-radius);
    box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);
    transition: box-shadow 0.2s ease-in-out;
  }
`;

const TeaserMetadata = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 15px 0 7px;
  font-size: 0.778em;
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;
`;

const TeaserTitle = styled.h2`
  margin: 0 0 15px;
  font-size: 1em;
  line-height: 1.5em;
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;
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

const SearchButton = styled.button`
  flex: 0 0 auto;
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
