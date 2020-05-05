import { graphql } from "gatsby";
import React, {
  FunctionComponent,
  MouseEvent,
  useCallback,
  useEffect,
  useState,
} from "react";
import { useDispatch, useSelector } from "react-redux";
import styled, { createGlobalStyle } from "styled-components";
import { DocPageNavigationFragment } from "../../../graphql-types";
import { State } from "../../state";
import { toggleNavigationGroup } from "../../state/common";
import { IconContainer } from "./icon-container";
import { Link } from "./link";
import { useStickyElement } from "./useStickyElement";

import ArrowDownIconSvg from "../../images/arrow-down.svg";
import ArrowRightIconSvg from "../../images/arrow-right.svg";
import ArrowUpIconSvg from "../../images/arrow-up.svg";
import TimesIconSvg from "../../images/times.svg";
import ProductSwitcherIconSvg from "../../images/th-large.svg";

interface DocPageNavigationProperties {
  data: DocPageNavigationFragment;
  selectedPath: string;
  selectedProduct: string;
}

export const DocPageNavigation: FunctionComponent<DocPageNavigationProperties> = ({
  data,
  selectedPath,
  selectedProduct,
}) => {
  const [sideNavOpen, setSideNavOpen] = useState<boolean>(false);
  const { containerRef, elementRef } = useStickyElement<
    HTMLElement,
    HTMLDivElement
  >(1050);
  const expandedPaths = useSelector<State, string[]>(
    (state) => state.common.expandedPaths
  );
  const dispatch = useDispatch();
  const [productSwitcherOpen, setProductSwitcherOpen] = useState(false);
  const currentProduct =
    data.config?.products &&
    data.config.products.find((product) => product?.path === selectedProduct);

  const handleClickDialog = useCallback((event: MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
  }, []);

  const handleCloseClick = useCallback(() => {
    setProductSwitcherOpen(false);
  }, []);

  const handleCloseSideNav = useCallback(() => {
    setSideNavOpen(false);
  }, []);

  const handleOpenSideNav = useCallback(() => {
    setSideNavOpen(true);
  }, []);

  const handleToggleClick = useCallback(
    (event: MouseEvent<HTMLButtonElement>, isOpen) => {
      setProductSwitcherOpen(!isOpen);
      event.stopPropagation();
    },
    []
  );

  const handleToggleExpand = useCallback((path: string) => {
    dispatch(toggleNavigationGroup({ path }));
  }, []);

  const buildNavigationStructure = (items: Item[], basePath: string) => (
    <NavigationList open={!productSwitcherOpen}>
      {items.map(({ path, title, items: subItems }) => {
        const itemPath =
          !subItems && path === "index" ? basePath : basePath + "/" + path;

        return (
          <NavigationItem
            key={itemPath + (subItems ? "/parent" : "")}
            className={
              !subItems && isActive(selectedPath, itemPath) ? "active" : ""
            }
          >
            {subItems ? (
              <NavigationGroup
                expanded={expandedPaths.indexOf(itemPath) !== -1}
              >
                <NavigationGroupToggle
                  onClick={() => handleToggleExpand(itemPath)}
                >
                  {title}
                  <IconContainer size={16}>
                    <ArrowDownIconSvg className="arrow-down" />
                    <ArrowUpIconSvg className="arrow-up" />
                  </IconContainer>
                </NavigationGroupToggle>
                <NavigationGroupContent>
                  {buildNavigationStructure(subItems, itemPath)}
                </NavigationGroupContent>
              </NavigationGroup>
            ) : (
              <NavigationLink to={itemPath}>{title}</NavigationLink>
            )}
          </NavigationItem>
        );
      })}
    </NavigationList>
  );

  useEffect(() => {
    window.addEventListener("click", handleCloseClick);

    return () => {
      window.removeEventListener("click", handleCloseClick);
    };
  }, [handleCloseClick]);

  return (
    <Navigation ref={containerRef}>
      <BodyStyle disableScrolling={sideNavOpen} />
      <MenuOpener onClick={handleOpenSideNav} />
      <FixedContainer ref={elementRef} open={sideNavOpen}>
        <ProductSwitcher>
          <ProductSwitcherButton
            onClick={(e) => handleToggleClick(e, productSwitcherOpen)}
          >
            {currentProduct?.title}
            <IconContainer size={16}>
              <ProductSwitcherIconSvg />
            </IconContainer>
          </ProductSwitcherButton>
          <CloseButton onClick={handleCloseSideNav} />
          <ProductSwitcherDialog
            open={productSwitcherOpen}
            onClick={handleClickDialog}
          >
            {data.config?.products &&
              data.config.products.map((product) =>
                product === currentProduct ? (
                  <CurrentProduct onClick={handleCloseClick}>
                    <ProductTitle>{product!.title!}</ProductTitle>
                    <ProductDescription>
                      {product!.description!}
                    </ProductDescription>
                  </CurrentProduct>
                ) : (
                  <ProductLink to={`/docs/${product!.path!}`}>
                    <ProductTitle>{product!.title!}</ProductTitle>
                    <ProductDescription>
                      {product!.description!}
                    </ProductDescription>
                  </ProductLink>
                )
              )}
          </ProductSwitcherDialog>
        </ProductSwitcher>
        {currentProduct?.items &&
          buildNavigationStructure(
            currentProduct.items
              .filter((item) => !!item)
              .map<Item>((item) => ({
                path: item!.path!,
                title: item!.title!,
                items: item!.items
                  ? item?.items
                      .filter((item) => !!item)
                      .map<Item>((item) => ({
                        path: item!.path!,
                        title: item!.title!,
                      }))
                  : undefined,
              })),
            `/docs/${currentProduct.path!}`
          )}
      </FixedContainer>
    </Navigation>
  );
};

function isActive(selectedPath: string, itemPath: string) {
  return itemPath === selectedPath.substring(0, selectedPath.lastIndexOf("/"));
}

export const DocPageNavigationGraphQLFragment = graphql`
  fragment DocPageNavigation on Query {
    config: file(
      sourceInstanceName: { eq: "docs" }
      relativePath: { eq: "docs.json" }
    ) {
      products: childrenDocsJson {
        path
        title
        description
        items {
          path
          title
          items {
            path
            title
          }
        }
      }
    }
  }
`;

interface Item {
  path: string;
  title: string;
  items?: Item[];
}

const BodyStyle = createGlobalStyle<{ disableScrolling: boolean }>`
  body {
    overflow: ${({ disableScrolling }) =>
      disableScrolling ? "hidden" : "initial"};

    @media only screen and (min-width: 600px) {
      overflow: initial;
    }
  }
`;

const Navigation = styled.nav`
  position: fixed;
  top: 60px;
  left: 0;
  z-index: 2;
  display: flex;
  flex-direction: column;
  width: 50px;
  height: calc(100vh - 60px);

  * {
    user-select: none;
  }

  @media only screen and (min-width: 1050px) {
    position: relative;
    top: initial;
    left: initial;
    flex: 0 0 250px;
    width: initial;
    height: initial;
  }
`;

const MenuOpener = styled(ArrowRightIconSvg)`
  margin-top: 30px;
  border-radius: 0 4px 4px 0;
  border: 1px solid #aaa;
  border-left: none;
  padding: 5px;
  width: 24px;
  height: 24px;
  opacity: 0.3;
  cursor: pointer;
  background-color: white;
  box-shadow: 0px 3px 6px 0px rgba(0, 0, 0, 0.25);
  transition: opacity 0.2s ease-in-out;
  animation: pulse 4s infinite;

  &:hover {
    opacity: 1;
    animation: initial;
  }

  @media only screen and (min-width: 1050px) {
    display: none;
  }

  @keyframes pulse {
    0% {
      opacity: 0.3;
    }

    25% {
      opacity: 0.7;
    }

    100% {
      opacity: 0.3;
    }
  }
`;

const FixedContainer = styled.div<{ open: boolean }>`
  position: absolute;
  display: ${({ open }) => (open ? "initial" : "none")};
  padding: 25px 0 0;
  width: 100vw;
  height: calc(100% - 25px);
  overflow-y: initial;
  background-color: white;
  opacity: ${({ open }) => (open ? "1" : "0")};
  transition: opacity 0.2s ease-in-out;

  @media only screen and (min-width: 600px) {
    width: 450px;
    box-shadow: 0px 3px 6px 0px rgba(0, 0, 0, 0.25);
  }

  @media only screen and (min-width: 1050px) {
    position: fixed;
    display: initial;
    padding: 25px 0;
    width: 250px;
    height: initial;
    overflow-y: hidden;
    background-color: initial;
    opacity: initial;
    box-shadow: initial;
  }
`;

const ProductSwitcher = styled.div`
  display: flex;
  flex-wrap: wrap;
  align-items: center;

  @media only screen and (min-width: 1050px) {
    flex-wrap: initial;
  }
`;

const ProductSwitcherButton = styled.button`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  margin-left: 14px;
  border: 1px solid #ccc;
  border-radius: 5px;
  padding: 7px 10px;
  width: calc(100% - 74px);
  height: 38px;
  font-size: 0.833em;
  transition: background-color 0.2s ease-in-out;

  > ${IconContainer} {
    margin-left: auto;

    > svg {
      fill: #666;
    }
  }

  :hover {
    background-color: #ddd;
  }

  @media only screen and (min-width: 1050px) {
    margin: 6px 14px 20px;
    padding: 7px 5px;
    width: calc(100% - 28px);
    height: initial;
  }
`;

const CloseButton = styled(TimesIconSvg)`
  flex: 0 0 auto;
  padding: 17px 17px;
  width: 26px;
  height: 26px;
  opacity: 0.5;
  cursor: pointer;
  transition: opacity 0.2s ease-in-out;

  &:hover {
    opacity: 1;
  }

  @media only screen and (min-width: 1050px) {
    display: none;
  }
`;

const ProductSwitcherDialog = styled.div<{ open: boolean }>`
  display: ${({ open }) => (open ? "flex" : "none")};
  flex: 1 1 100%;
  flex-direction: column;
  padding: 10px;
  background-color: #fff;

  @media only screen and (min-width: 1050px) {
    position: fixed;
    z-index: 10;
    top: 130px;
    flex-direction: row;
    flex-wrap: wrap;
    margin: 0 14px;
    border-radius: 5px;
    width: 700px;
    height: initial;
    box-shadow: 0 3px 6px 0 rgba(0, 0, 0, 0.25);
  }
`;

const CurrentProduct = styled.div`
  flex: 0 0 auto;
  border: 1px solid #ccc;
  border-radius: 5px;
  margin: 5px;
  padding: 10px;
  font-size: 0.833em;
  color: #666;
  background-color: #ddd;

  @media only screen and (min-width: 1050px) {
    flex: 0 0 calc(50% - 32px);
  }
`;

const ProductLink = styled(Link)`
  flex: 0 0 auto;
  border: 1px solid #ccc;
  border-radius: 5px;
  margin: 5px;
  padding: 10px;
  font-size: 0.833em;
  color: #666;
  transition: background-color 0.2s ease-in-out;

  :hover {
    background-color: #ddd;
  }

  @media only screen and (min-width: 1050px) {
    flex: 0 0 calc(50% - 32px);
  }
`;

const ProductTitle = styled.h6`
  font-size: 1em;
`;

const ProductDescription = styled.p`
  margin-bottom: 0;
`;

const NavigationList = styled.ol<{ open: boolean }>`
  display: ${({ open }) => (open ? "flex" : "none")};
  flex-direction: column;
  margin: 0;
  padding: 0 25px 20px;
  list-style-type: none;

  @media only screen and (min-width: 1050px) {
    display: flex;
    padding: 0 20px 20px;
  }
`;

const NavigationItem = styled.li`
  flex: 0 0 auto;
  margin: 5px 0;
  padding: 0;
  min-height: 20px;
  line-height: initial;

  &.active > a {
    font-weight: bold;
  }
`;

const NavigationGroupToggle = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  min-height: 20px;
  font-size: 0.833em;
`;

const NavigationGroupContent = styled.div`
  > ${NavigationList} {
    padding: 5px 10px;
  }
`;

const NavigationGroup = styled.div<{ expanded: boolean }>`
  display: flex;
  flex-direction: column;
  cursor: pointer;

  > ${NavigationGroupContent} {
    display: ${({ expanded }) => (expanded ? "initial" : "none")};
  }

  > ${NavigationGroupToggle} > ${IconContainer} {
    margin-left: auto;

    > .arrow-down {
      display: ${({ expanded }) => (expanded ? "none" : "initial")};
      fill: #666;
    }

    > .arrow-up {
      display: ${({ expanded }) => (expanded ? "initial" : "none")};
      fill: #666;
    }
  }
`;

const NavigationLink = styled(Link)`
  font-size: 0.833em;
  color: #666;

  :hover {
    color: #000;
  }
`;
