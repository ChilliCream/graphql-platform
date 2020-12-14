import {graphql} from "gatsby";
import React, {FunctionComponent, MouseEvent, useCallback, useEffect, useState} from "react";
import {useDispatch, useSelector} from "react-redux";
import styled, {css} from "styled-components";
import {DocPageNavigationFragment} from "../../../graphql-types";
import {State} from "../../state";
import {closeTOC, expandNavigationGroup, toggleNavigationGroup, toggleTOC} from "../../state/common";
import {DocPageStickySideBarStyle, MostProminentSection} from "./doc-page-elements";
import {DocPagePaneHeader} from "./doc-page-pane-header";
import {IconContainer} from "../misc/icon-container";
import {Link} from "../misc/link";

import ArrowDownIconSvg from "../../images/arrow-down.svg";
import ArrowUpIconSvg from "../../images/arrow-up.svg";
import ProductSwitcherIconSvg from "../../images/th-large.svg";
import {BoxShadow, IsTablet} from './shared-style';

interface DocPageNavigationProperties {
  data: DocPageNavigationFragment;
  selectedPath: string;
  selectedProduct: string;
  selectedVersion: string;
}

export const DocPageNavigation: FunctionComponent<DocPageNavigationProperties> = ({
  data,
  selectedPath,
  selectedProduct,
  selectedVersion,
}) => {
  const expandedPaths = useSelector<State, string[]>(
    (state) => state.common.expandedPaths
  );
  const showTOC = useSelector<State, boolean>((state) => state.common.showTOC);
  const dispatch = useDispatch();
  const [productSwitcherOpen, setProductSwitcherOpen] = useState(false);
  const activeProduct = data.config?.products?.find(
    (product) => product?.path === selectedProduct
  );
  const activeVersion = activeProduct?.versions?.find(
    (version) => version?.path === selectedVersion
  );

  const handleClickDialog = useCallback((event: MouseEvent<HTMLDivElement>) => {
    event.stopPropagation();
    dispatch(closeTOC());
  }, []);

  const handleCloseClick = useCallback(() => {
    setProductSwitcherOpen(false);
  }, []);

  const handleCloseTOC = useCallback(() => {
    dispatch(toggleTOC());
  }, []);

  const handleClickNavigationItem = useCallback(() => {
    dispatch(closeTOC());
  }, []);

  const handleToggleClick = useCallback(
    (event: MouseEvent<HTMLButtonElement>, isOpen) => {
      event.stopPropagation();
      setProductSwitcherOpen(!isOpen);
    },
    []
  );

  const handleToggleExpand = useCallback(
    (event: MouseEvent<HTMLDivElement>, path: string) => {
      event.stopPropagation();
      dispatch(toggleNavigationGroup({ path }));
    },
    []
  );

  const buildNavigationStructure = (items: Item[], basePath: string) => (
    <NavigationList>
      {items.map(({ path, title, items: subItems }) => {
        const itemPath =
          !subItems && path === "index" ? basePath : basePath + "/" + path;

        return (
          <NavigationItem
            key={itemPath + (subItems ? "/parent" : "")}
            className={
              subItems
                ? containsActiveItem(selectedPath, itemPath)
                  ? "active"
                  : ""
                : isActive(selectedPath, itemPath)
                ? "active"
                : ""
            }
            onClick={handleClickNavigationItem}
          >
            {subItems ? (
              <NavigationGroup
                expanded={expandedPaths.indexOf(itemPath) !== -1}
              >
                <NavigationGroupToggle
                  onClick={(e) => handleToggleExpand(e, itemPath)}
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
              <NavigationLink to={itemPath + "/"}>{title}</NavigationLink>
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

  useEffect(() => {
    /*
      Ensures that all groups along the selected path are expanded on page load.
    */
    if (activeVersion?.items) {
      const selectedVersionLength =
        selectedVersion.length > 0 ? selectedVersion.length + 1 : 0;
      const index =
        selectedPath.indexOf(selectedProduct) +
        selectedProduct.length +
        1 +
        selectedVersionLength;
      const parts = selectedPath
        .substring(index)
        .split("/")
        .filter((part) => part.length > 0);

      if (parts.length > 0) {
        const rootFolder = activeVersion.items.find(
          (item) => item!.path === parts[0] && item!.items
        );

        if (rootFolder) {
          const path = selectedPath.substring(
            0,
            parts.length > 1
              ? selectedPath.lastIndexOf(parts[parts.length - 1]) - 1
              : selectedPath.length - 1
          );

          dispatch(expandNavigationGroup({ path }));
        }
      }
    }
  }, [activeProduct, selectedPath, selectedProduct]);

  const height = useSelector<State, string>(
    (state) => {
      return state.common.articleViewportHeight;
    }
  );

  return (
    <Navigation calculatedHeight={height} className={showTOC ? "show" : ""}>
        <DocPagePaneHeader
          title="Table of contents"
          showWhenScreenWidthIsSmallerThan={1111}
          onClose={handleCloseTOC}
        />
        <ProductSwitcher>
          <ProductSwitcherButton
            onClick={(e) => handleToggleClick(e, productSwitcherOpen)}
          >
            {activeProduct?.title}
            <IconContainer size={16}>
              <ProductSwitcherIconSvg />
            </IconContainer>
          </ProductSwitcherButton>
          <ProductSwitcherDialog
            open={productSwitcherOpen}
            onClick={handleClickDialog}
          >
            {data.config?.products?.map((product) =>
              product === activeProduct ? (
                <ActiveProduct key={product!.path!} onClick={handleCloseClick}>
                  <ProductTitle>{product!.title!}</ProductTitle>
                  <ProductDescription>
                    {product!.description!}
                  </ProductDescription>
                </ActiveProduct>
              ) : (
                <ProductLink
                  key={product!.path!}
                  to={
                    product!.versions![0]!.path! === ""
                      ? `/docs/${product!.path!}/`
                      : `/docs/${product!.path!}/${product!.versions![0]!
                          .path!}/`
                  }
                >
                  <ProductTitle>{product!.title!}</ProductTitle>
                  <ProductDescription>
                    {product!.description!}
                  </ProductDescription>
                </ProductLink>
              )
            )}
          </ProductSwitcherDialog>
        </ProductSwitcher>
        {!productSwitcherOpen && activeProduct!.versions!.length > 1 && (
          <ProductVersions>
            {activeProduct!.versions!.map((version, index) => (
              <ProductVersion
                key={version!.path! + index}
                className={
                  activeVersion!.path! === version!.path! ? "active" : undefined
                }
              >
                <ProductVersionLink
                  to={
                    version!.path! === ""
                      ? `/docs/${activeProduct!.path!}/`
                      : `/docs/${activeProduct!.path!}/${version!.path!}/`
                  }
                >
                  {version!.title}
                </ProductVersionLink>
              </ProductVersion>
            ))}
          </ProductVersions>
        )}
        {!productSwitcherOpen && activeVersion?.items && (
          <MostProminentSection>
            {buildNavigationStructure(
              activeVersion.items
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
              `/docs/${activeProduct!.path!}${
                activeVersion?.path?.length && activeVersion.path.length > 0
                  ? "/" + activeVersion.path!
                  : ""
              }`
            )}
          </MostProminentSection>
        )}
    </Navigation>
  );
};

function containsActiveItem(selectedPath: string, itemPath: string) {
  const itemPathWithSlash = itemPath.endsWith("/") ? itemPath : itemPath + "/";

  return selectedPath.startsWith(itemPathWithSlash);
}

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
        versions {
          path
          title
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
  }
`;

interface Item {
  path: string;
  title: string;
  items?: Item[];
}

export const Navigation = styled.nav<{calculatedHeight: string}>`
  ${DocPageStickySideBarStyle}
  padding: 25px 0 0;
  transition: margin-left 250ms;
  background-color: white;

  &.show {
    margin-left: 0;
  }

  ${({ calculatedHeight }) => IsTablet(`
      margin-left: -105%;
      height: ${calculatedHeight};
      position: fixed;
      top: 60px;
      left: 0;
      ${BoxShadow}
    `)
  }
`;

const ProductSwitcher = styled.div`
  display: flex;
  flex-wrap: wrap;
  align-items: center;

  @media only screen and (min-width: 1070px) {
    position: relative;
    flex-wrap: initial;
    overflow-y: initial;
  }
`;

const ProductSwitcherButton = styled.button`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  border: 1px solid #ccc;
  border-radius: 5px;
  margin: 6px 14px 10px;
  padding: 7px 10px;
  width: calc(100% - 28px);
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

  @media only screen and (min-width: 1070px) {
    margin-bottom: 20px;
    padding: 7px 5px;
    width: calc(100% - 28px);
    height: initial;
  }
`;

const ProductSwitcherDialog = styled.div<{ open: boolean }>`
  display: ${({ open }) => (open ? "flex" : "none")};
  flex: 1 1 100%;
  flex-direction: column;
  padding: 0 10px;
  background-color: #fff;

  @media only screen and (min-width: 1070px) {
    top: 135px;
    position: fixed;
    z-index: 10;
    flex-direction: row;
    flex-wrap: wrap;
    margin: 0 14px;
    border-radius: 5px;
    padding: 10px;
    width: 700px;
    height: initial;
    box-shadow: 0 3px 6px 0 rgba(0, 0, 0, 0.25);
  }
`;

const ProductBase = css`
  flex: 0 0 auto;
  border: 1px solid #ccc;
  border-radius: 5px;
  margin: 5px;
  padding: 10px;
  font-size: 0.833em;
  color: #666;
  cursor: pointer;

  @media only screen and (min-width: 1070px) {
    flex: 0 0 calc(50% - 32px);
  }
`;

const ActiveProduct = styled.div`
  ${ProductBase};
  background-color: #ddd;
`;

const ProductLink = styled(Link)`
  ${ProductBase};
  transition: background-color 0.2s ease-in-out;

  :hover {
    background-color: #ddd;
  }
`;

const ProductTitle = styled.h6`
  font-size: 1em;
`;

const ProductDescription = styled.p`
  margin-bottom: 0;
`;

const ProductVersions = styled.ol`
  display: flex;
  flex-direction: row;
  margin: 0;
  padding: 0 25px 10px;
  list-style-type: none;

  @media only screen and (min-width: 1070px) {
    padding: 0 20px 10px;
  }
`;

const ProductVersionLink = styled(Link)`
  font-size: 0.833em;
  color: #666;

  :hover {
    color: #000;
  }
`;

const ProductVersion = styled.li`
  flex: 0 0 auto;
  margin: 5px 20px 5px 0;
  padding: 0;
  min-height: 20px;
  line-height: initial;

  &.active {
    > ${ProductVersionLink} {
      font-weight: bold;
    }
  }
`;

const NavigationList = styled.ol`
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0 25px 20px;
  list-style-type: none;

  @media only screen and (min-width: 1070px) {
    display: flex;
    padding: 0 20px 20px;
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

const NavigationItem = styled.li`
  flex: 0 0 auto;
  margin: 5px 0;
  padding: 0;
  min-height: 20px;
  line-height: initial;

  &.active {
    > ${NavigationLink}, > ${NavigationGroup} > ${NavigationGroupToggle} {
      font-weight: bold;
    }
  }
`;
