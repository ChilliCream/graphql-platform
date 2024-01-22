import { useDocSearchKeyboardEvents } from "@docsearch/react";
import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, {
  FC,
  MouseEventHandler,
  ReactNode,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { useSelector } from "react-redux";
import styled from "styled-components";

import { WorkshopNdcCopenhagen } from "@/components/images/workshop-ndc-copenhagen";
import { WorkshopNdcOslo } from "@/components/images/workshop-ndc-oslo";
import { WorkshopOnline } from "@/components/images/workshop-online";
import { IconContainer } from "@/components/misc/icon-container";
import { Link } from "@/components/misc/link";
import { SearchModal } from "@/components/misc/search-modal";
import { Brand, Logo } from "@/components/sprites";
import {
  DocsJson,
  DocsJsonVersions,
  GetHeaderDataQuery,
  Maybe,
  SiteSiteMetadataTools,
} from "@/graphql-types";
import { FONT_FAMILY_HEADING, THEME_COLORS } from "@/shared-style";
import { State, WorkshopsState, useObservable } from "@/state";

// Brands
import GithubIconSvg from "@/images/brands/github.svg";
import LinkedInIconSvg from "@/images/brands/linkedin.svg";
import SlackIconSvg from "@/images/brands/slack.svg";
import TwitterIconSvg from "@/images/brands/twitter.svg";
import YouTubeIconSvg from "@/images/brands/youtube.svg";

// Icons
import AngleRightIconSvg from "@/images/angle-right.svg";
import ArrowDownSvg from "@/images/arrow-down.svg";
import BarsIconSvg from "@/images/bars.svg";
import ExternalLinkSvg from "@/images/external-link.svg";
import NewspaperIconSvg from "@/images/newspaper.svg";
import SearchIconSvg from "@/images/search.svg";
import TimesIconSvg from "@/images/times.svg";

// Logos
import LogoTextSvg from "@/images/logo/chillicream-text.svg";
import LogoIconSvg from "@/images/logo/chillicream-winking.svg";

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
            linkedIn
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
          latestStableVersion
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
    const subscription = showShadow$.subscribe((showShadow) => {
      containerRef.current?.classList.toggle("shadow", showShadow);
    });

    return () => {
      subscription.unsubscribe();
    };
  }, [showShadow$]);

  useDocSearchKeyboardEvents({
    isOpen: searchOpen,
    onOpen: handleSearchOpen,
    onClose: handleSearchClose,
  });

  return (
    <Container ref={containerRef}>
      <ContainerWrapper>
        <LogoLink to="/">
          <LogoIcon {...LogoIconSvg} />
          <LogoText {...LogoTextSvg} />
        </LogoLink>
        <Navigation open={topNavOpen}>
          <NavigationHeader>
            <LogoLink to="/">
              <LogoIcon {...LogoIconSvg} />
              <LogoText {...LogoTextSvg} />
            </LogoLink>
            <HamburgerCloseButton onClick={handleTopNavClose}>
              <HamburgerCloseIcon />
            </HamburgerCloseButton>
          </NavigationHeader>
          <Nav>
            <ProductsNavItem firstBlogPost={firstBlogPost} />
            <DeveloperNavItem products={products} tools={tools!} />
            <ServicesNavItem />
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
  padding-left: 20px;
  height: 60px;
`;

const LogoIcon = styled(Logo)`
  height: 40px;
  fill: ${THEME_COLORS.textContrast};
  transition: fill 0.2s ease-in-out;
`;

const LogoText = styled(Logo)`
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
  align-items: center;
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

interface ProductsNavItemProps {
  readonly firstBlogPost: any;
}

const ProductsNavItem: FC<ProductsNavItemProps> = ({ firstBlogPost }) => {
  const featuredImage =
    firstBlogPost.frontmatter!.featuredImage?.childImageSharp?.gatsbyImageData;

  const [subNav, navHandlers, linkHandlers] = useSubNav((hideSubNav) => (
    <>
      <SubNavMain>
        <TileLink to="/products/bananacakepop" onClick={hideSubNav}>
          <TileLinkTitle>Banana Cake Pop</TileLinkTitle>
          <TileLinkDescription>
            The IDE to create, explore, manage, and test GraphQL APIs with ease.
          </TileLinkDescription>
        </TileLink>
        <TileLink to="/products/hotchocolate" onClick={hideSubNav}>
          <TileLinkTitle>Hot Chocolate</TileLinkTitle>
          <TileLinkDescription>
            The server to create high-performance .NET GraphQL APIs in no time.
          </TileLinkDescription>
        </TileLink>
        <TileLink to="/products/strawberryshake" onClick={hideSubNav}>
          <TileLinkTitle>Strawberry Shake</TileLinkTitle>
          <TileLinkDescription>
            Effortlessly create modern .NET apps that consume GraphQL APIs.
          </TileLinkDescription>
        </TileLink>
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
            {firstBlogPost.frontmatter?.date}
            {firstBlogPost?.readingTime?.text &&
              " ・ " + firstBlogPost.readingTime.text}
          </TeaserMetadata>
          <TeaserTitle>{firstBlogPost.frontmatter!.title}</TeaserTitle>
        </TeaserLink>
      </SubNavAdditionalInfo>
    </>
  ));

  return (
    <NavItemContainer {...navHandlers}>
      <NavLink to="/products" prefetch={false} {...linkHandlers}>
        Products
        <IconContainer size={10}>
          <ArrowDownSvg />
        </IconContainer>
      </NavLink>
      {subNav}
    </NavItemContainer>
  );
};

interface DeveloperNavItemProps {
  readonly products: Maybe<
    Pick<DocsJson, "path" | "title" | "latestStableVersion"> & {
      versions?: Maybe<Maybe<Pick<DocsJsonVersions, "path">>[]>;
    }
  >[];
  readonly tools: Pick<
    SiteSiteMetadataTools,
    "bcp" | "github" | "linkedIn" | "shop" | "slack" | "twitter" | "youtube"
  >;
}

const DeveloperNavItem: FC<DeveloperNavItemProps> = ({ products, tools }) => {
  const workshop = useSelector<State, WorkshopsState[number] | undefined>(
    (state) => state.workshops.find(({ hero, active }) => hero && active)
  );

  const [subNav, navHandlers, linkHandlers] = useSubNav((hideSubNav) => (
    <>
      <SubNavMain>
        <SubNavGroup>
          <SubNavTitle>Documentation</SubNavTitle>
          {products.map((product, index) => (
            <SubNavLink
              key={index}
              to={`/docs/${product!.path!}/${product?.latestStableVersion}`}
              onClick={hideSubNav}
            >
              <IconContainer size={16}>
                <AngleRightIconSvg />
              </IconContainer>
              {product!.title}
            </SubNavLink>
          ))}
        </SubNavGroup>
        <SubNavSeparator />
        <SubNavGroup>
          <SubNavTitle>More Resources</SubNavTitle>
          <SubNavLink to="/blog" onClick={hideSubNav}>
            <IconContainer size={20}>
              <NewspaperIconSvg />
            </IconContainer>
            Blog
          </SubNavLink>
          <SubNavLink to={tools.github!} onClick={hideSubNav}>
            <IconContainer size={20}>
              <Brand {...GithubIconSvg} />
            </IconContainer>
            GitHub
          </SubNavLink>
          <SubNavLink to={tools.slack!} onClick={hideSubNav}>
            <IconContainer size={20}>
              <Brand {...SlackIconSvg} />
            </IconContainer>
            Slack / Community
          </SubNavLink>
          <SubNavLink to={tools.youtube!} onClick={hideSubNav}>
            <IconContainer size={20}>
              <Brand {...YouTubeIconSvg} />
            </IconContainer>
            YouTube Channel
          </SubNavLink>
          <SubNavLink to={tools.twitter!} onClick={hideSubNav}>
            <IconContainer size={20}>
              <Brand {...TwitterIconSvg} />
            </IconContainer>
            Twitter
          </SubNavLink>
          <SubNavLink to={tools.linkedIn!} onClick={hideSubNav}>
            <IconContainer size={20}>
              <Brand {...LinkedInIconSvg} />
            </IconContainer>
            LinkedIn
          </SubNavLink>
        </SubNavGroup>
      </SubNavMain>
      <SubNavAdditionalInfo>
        {workshop && (
          <>
            <SubNavTitle>Upcoming Workshop</SubNavTitle>
            <TeaserLink to={workshop.url}>
              <TeaserImage>
                <WorkshopHero image={workshop.image} />
              </TeaserImage>
              <TeaserMetadata>
                {`${workshop.date} ・ ${workshop.host} `}
                <NoWrap>{workshop.place}</NoWrap>
              </TeaserMetadata>
              <TeaserTitle>{workshop.title}</TeaserTitle>
              <TeaserMessage>{workshop.teaser}</TeaserMessage>
            </TeaserLink>
          </>
        )}
      </SubNavAdditionalInfo>
    </>
  ));

  return (
    <NavItemContainer {...navHandlers}>
      <NavLink to="/docs" prefetch={false} {...linkHandlers}>
        Developers
        <IconContainer size={10}>
          <ArrowDownSvg />
        </IconContainer>
      </NavLink>
      {subNav}
    </NavItemContainer>
  );
};

const ServicesNavItem: FC = () => {
  const [subNav, navHandlers, linkHandlers] = useSubNav((hideSubNav) => (
    <>
      <SubNavMain>
        <TileLink to="/services/advisory" onClick={hideSubNav}>
          <TileLinkTitle>Advisory</TileLinkTitle>
          <TileLinkDescription>
            We're your gateway to move your projects faster and smarter than
            ever before.
          </TileLinkDescription>
        </TileLink>
        <TileLink to="/services/training" onClick={hideSubNav}>
          <TileLinkTitle>Training</TileLinkTitle>
          <TileLinkDescription>
            Level up or upskill your teams on your own terms.
          </TileLinkDescription>
        </TileLink>
        <TileLink to="/services/support" onClick={hideSubNav}>
          <TileLinkTitle>Support</TileLinkTitle>
          <TileLinkDescription>
            Set your teams up for success with peace of mind.
          </TileLinkDescription>
        </TileLink>
      </SubNavMain>
      <SubNavAdditionalInfo>
        <SubNavTitle>Get in Touch</SubNavTitle>
        <TeaserLink to="mailto:contact@chillicream.com?subject=Services">
          <TeaserHero>
            Your technology journey.
            <br />
            Our expertise.
          </TeaserHero>
          <TeaserDescription>
            <strong>ChilliCream</strong> helps you unlock your full potential,
            delivering on its promise to transform your business.
          </TeaserDescription>
        </TeaserLink>
      </SubNavAdditionalInfo>
    </>
  ));

  return (
    <NavItemContainer {...navHandlers}>
      <NavLink to="/services" prefetch={false} {...linkHandlers}>
        Services
        <IconContainer size={10}>
          <ArrowDownSvg />
        </IconContainer>
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
      <NavLink to={shopLink}>
        Shop
        <IconContainer size={10}>
          <ExternalLinkSvg />
        </IconContainer>
      </NavLink>
    </NavItemContainer>
  );
};

function isTouchDevice() {
  return (
    window.PointerEvent &&
    "maxTouchPoints" in navigator &&
    navigator.maxTouchPoints > 0
  );
}

type NavHandlers = Record<"onMouseEnter" | "onMouseLeave", MouseEventHandler>;
type LinkHandlers = Record<"onClick", MouseEventHandler>;

function useSubNav(
  children: (hideSubNav: () => void) => ReactNode
): [subNav: ReactNode, navHandlers: NavHandlers, linkHandlers: LinkHandlers] {
  const [show, setShow] = useState<boolean>(false);

  const toggle = useCallback(() => {
    setShow((state) => !state);
  }, []);

  const hide = useCallback(() => {
    setShow(false);
  }, []);

  const subNav = show && (
    <SubNavContainer>
      <SubNav>{children(hide)}</SubNav>
    </SubNavContainer>
  );

  const navHandlers = useMemo<NavHandlers>(
    () => ({
      onMouseEnter: () => {
        if (!isTouchDevice()) {
          setShow(true);
        }
      },
      onMouseLeave: hide,
    }),
    []
  );

  const linkHandlers = useMemo<LinkHandlers>(
    () => ({
      onClick: (event) => {
        event.preventDefault();

        toggle();
      },
    }),
    []
  );

  return [subNav, navHandlers, linkHandlers];
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

  ${IconContainer} {
    margin-bottom: 2px;
    margin-left: 6px;

    > svg {
      fill: ${THEME_COLORS.textContrast};
    }
  }

  @media only screen and (min-width: 284px) {
    font-size: 1.5em;
    font-weight: 400;
    line-height: 2em;
  }

  @media only screen and (min-width: 992px) {
    font-size: 0.833em;
    font-weight: 500;
    line-height: 1em;
  }
`;

const SubNavContainer = styled.div`
  position: fixed;
  top: 60px;
  right: 20px;
  bottom: 20px;
  left: 20px;
  display: flex;
  flex-direction: column;
  align-items: center;
  overflow: visible;

  @media only screen and (min-width: 992px) {
    top: 50px;
    right: calc(50vw - 350px);
    bottom: initial;
    left: calc(50vw - 350px);
    padding-top: 4px;
  }
`;

const NavItemContainer = styled.li`
  flex: 0 0 auto;
  margin: 0;

  @media only screen and (min-width: 992px) {
    margin-top: 8px;
    padding-bottom: 8px;

    &:hover ${NavLink} {
      background-color: ${THEME_COLORS.secondary};
    }
  }
`;

const SubNav = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  min-width: 100%;
  border-radius: var(--border-radius);
  background-color: ${THEME_COLORS.background};
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);

  @media only screen and (min-width: 600px) {
    flex-direction: row;
  }

  @media only screen and (min-width: 992px) {
    width: 700px;
  }
`;

const SubNavMain = styled.div`
  display: flex;
  flex: 1 1 55%;
  flex-direction: column;
`;

const SubNavGroup = styled.div`
  margin: 10px 0;

  @media only screen and ((min-width: 600px) and (min-height: 430px)) {
    margin: 15px 0;
  }
`;

const SubNavTitle = styled.h1`
  margin: 5px 15px 10px;
  font-size: 0.75em;
  font-weight: 600;
  letter-spacing: 0.05em;
  color: ${THEME_COLORS.heading};
  transition: color 0.2s ease-in-out;

  @media only screen and ((min-width: 600px) and (min-height: 430px)) {
    margin: 5px 30px 10px;
  }
`;

const SubNavSeparator = styled.div`
  margin: 0 10px;
  height: 1px;
  background-color: ${THEME_COLORS.backgroundAlt};

  @media only screen and ((min-width: 600px) and (min-height: 430px)) {
    margin: -5px 20px;
  }
`;

const SubNavLink = styled(Link)`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 5px 15px;
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

  @media only screen and ((min-width: 600px) and (min-height: 430px)) {
    margin: 5px 30px;
  }
`;

const TileLinkTitle = styled.h1`
  margin-bottom: 6px;
  font-size: 1em;
  line-height: 1.5em;
  transition: color 0.2s ease-in-out;
`;

const TileLinkDescription = styled.p`
  margin: 0;
  font-size: 0.833em;
  line-height: 1.5em;
  color: ${THEME_COLORS.primary};
  transition: color 0.2s ease-in-out;
`;

const TileLink = styled(Link)`
  display: flex;
  flex-direction: column;
  margin: 5px 10px;
  border-radius: var(--border-radius);
  width: auto;
  min-height: 60px;
  padding: 5px 10px;
  background-color: ${THEME_COLORS.background};
  transition: background-color 0.2s ease-in-out;

  &:hover {
    background-color: ${THEME_COLORS.primary};

    ${TileLinkTitle},
    ${TileLinkDescription} {
      color: ${THEME_COLORS.background};
    }
  }

  @media only screen and ((min-width: 600px) and (min-height: 430px)) {
    margin: 5px 20px;
    padding: 10px;
  }
`;

const SubNavAdditionalInfo = styled.div`
  display: flex;
  flex: 1 1 45%;
  flex-direction: column;
  border-radius: 0 var(--border-radius) var(--border-radius) 0;
  padding: 10px 0;
  background-color: ${THEME_COLORS.backgroundAlt};

  @media only screen and ((min-width: 600px) and (min-height: 430px)) {
    padding: 25px 0;
  }
`;

const TeaserHero = styled.h1`
  display: flex;
  align-items: center;
  justify-content: center;
  text-align: center;
  font-size: 1em;
  line-height: 1.5em;
  max-width: 80%;
  margin: auto;
  aspect-ratio: 16/9;
  border-radius: var(--border-radius);
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);
  transition: box-shadow 0.2s ease-in-out;
  color: ${THEME_COLORS.textContrast};
  background-color: ${THEME_COLORS.primary};
  background: linear-gradient(180deg, ${THEME_COLORS.primary} 0%, #3d5f9f 100%);

  @media only screen and ((min-width: 600px) and (min-height: 430px)) {
    max-width: 400px;
  }
`;

const TeaserLink = styled(Link)`
  margin: 5px 15px;

  .gatsby-image-wrapper {
    pointer-events: none;
  }

  &:hover {
    > * {
      color: ${THEME_COLORS.primary};
    }

    .gatsby-image-wrapper {
      box-shadow: initial;
    }

    ${TeaserHero} {
      color: ${THEME_COLORS.textContrast};
      box-shadow: 0 1px 1px rgba(0, 0, 0, 0.25);
    }
  }

  @media only screen and ((min-width: 600px) and (min-height: 430px)) {
    margin: 5px 30px;
  }
`;

interface WorkshopHeroProps {
  readonly image: string;
}

const WorkshopHero: FC<WorkshopHeroProps> = ({ image }) => {
  switch (image) {
    case "ndc-oslo":
      return <WorkshopNdcOslo />;

    case "ndc-copenhagen":
      return <WorkshopNdcCopenhagen />;

    case "online":
      return <WorkshopOnline />;

    default:
      return null;
  }
};

const TeaserImage = styled.div`
  overflow: visible;
  max-width: 80%;
  margin: auto;

  .gatsby-image-wrapper {
    border-radius: var(--border-radius);
    box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);
    transition: box-shadow 0.2s ease-in-out;
  }

  @media only screen and ((min-width: 600px) and (min-height: 430px)) {
    max-width: fit-content;
  }
`;

const TeaserMetadata = styled.div`
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  align-items: center;
  margin: 15px 0 7px;
  font-size: 0.778em;
  line-height: 1.25;
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;
`;

const NoWrap = styled.span`
  white-space: nowrap;
`;

const TeaserTitle = styled.h2`
  margin: 0;
  font-size: 1em;
  line-height: 1.5em;
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;
`;

const TeaserMessage = styled.div`
  font-size: 0.778em;
  line-height: 1.2;
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;
`;

const TeaserDescription = styled.div`
  margin: 15px 0 7px;
  font-size: 0.833em;
  line-height: 1.5em;
  color: ${THEME_COLORS.text};
  transition: color 0.2s ease-in-out;
`;

const Group = styled.div`
  display: none;
  flex: 1 1 auto;
  flex-direction: row;
  justify-content: flex-end;
  height: 60px;

  @media only screen and (min-width: 284px) {
    display: flex;
  }

  @media only screen and (min-width: 992px) {
    flex: 0 0 auto;
    padding-right: 20px;
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
