import { graphql } from "gatsby";
import React, {
  FunctionComponent,
  MouseEvent,
  useCallback,
  useEffect,
  useState,
} from "react";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";
import { DocPageNavigationFragment } from "../../../graphql-types";
import ArrowDownIconSvg from "../../images/arrow-down.svg";
import ArrowUpIconSvg from "../../images/arrow-up.svg";
import ProductSwitcherIconSvg from "../../images/th-large.svg";
import { BoxShadow, IsTablet } from "../../shared-style";
import { State } from "../../state";
import {
  closeTOC,
  expandNavigationGroup,
  toggleNavigationGroup,
} from "../../state/common";
import { IconContainer } from "../misc/icon-container";
import { Link } from "../misc/link";
import {
  DocPageStickySideBarStyle,
  MostProminentSection,
} from "./doc-page-elements";
import { DocPagePaneHeader } from "./doc-page-pane-header";

interface DocPageNavigationProperties {
  data: DocPageNavigationFragment;
  selectedPath: string;
  selectedProduct: string;
  selectedVersion: string;
}

export const DocPageNavigation: FunctionComponent<DocPageNavigationProperties> =
  ({ data, selectedPath, selectedProduct, selectedVersion }) => {
    const expandedPaths = useSelector<State, string[]>(
      (state) => state.common.expandedPaths
    );
    const height = useSelector<State, string>((state) => {
      return state.common.articleViewportHeight;
    });
    const showTOC = useSelector<State, boolean>(
      (state) => state.common.showTOC
    );
    const dispatch = useDispatch();

    const [productSwitcherOpen, setProductSwitcherOpen] = useState(false);
    const [versionSwitcherOpen, setVersionSwitcherOpen] = useState(false);

    const products = data.config?.products ?? [];
    const activeProduct = products.find((p) => p?.path === selectedProduct);
    const activeVersion = activeProduct?.versions?.find(
      (v) => v?.path === selectedVersion
    );

    const handleClickNavigationItem = useCallback(() => {
      dispatch(closeTOC());
    }, []);

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

    // catch if someone clicks outside of the product switcher
    // to close the modal
    const handleClick = useCallback(() => {
      setProductSwitcherOpen(false);
      setVersionSwitcherOpen(false);
    }, []);

    useEffect(() => {
      window.addEventListener("click", handleClick);

      return () => {
        window.removeEventListener("click", handleClick);
      };
    }, [handleClick]);

    const toggleProductSwitcher = (e: MouseEvent) => {
      e.stopPropagation();

      setVersionSwitcherOpen(false);
      setProductSwitcherOpen((old) => !old);
    };

    const toggleVersionSwitcher = (e: MouseEvent) => {
      e.stopPropagation();

      setProductSwitcherOpen(false);
      setVersionSwitcherOpen((old) => !old);
    };

    const hasVersions =
      !activeProduct?.versions || activeProduct.versions.length > 1;

    return (
      // todo: use show flag
      <Navigation calculatedHeight={height} className={showTOC ? "show" : ""}>
        <DocPagePaneHeader
          title="Table of contents"
          showWhenScreenWidthIsSmallerThan={1111}
          onClose={() => dispatch(closeTOC())}
        />

        <ProductSwitcher>
          <ProductSwitcherButton onClick={toggleProductSwitcher}>
            {activeProduct?.title}

            <IconContainer size={16}>
              <ProductSwitcherIconSvg />
            </IconContainer>
          </ProductSwitcherButton>

          <ProductSwitcherButton
            disabled={!hasVersions}
            onClick={toggleVersionSwitcher}
          >
            {activeVersion?.title}

            {hasVersions && (
              <IconContainer size={12}>
                {versionSwitcherOpen ? (
                  <ArrowUpIconSvg />
                ) : (
                  <ArrowDownIconSvg />
                )}
              </IconContainer>
            )}
          </ProductSwitcherButton>
        </ProductSwitcher>

        <ProductSwitcherDialog
          open={productSwitcherOpen}
          onClick={() => dispatch(closeTOC())}
        >
          {products.map((product) => {
            if (!product) {
              return null;
            }

            return (
              <ProductLink
                active={product === activeProduct}
                key={product.path!}
                to={`/docs/${product.path!}`}
              >
                <ProductTitle>{product.title!}</ProductTitle>
                <ProductDescription>{product.description!}</ProductDescription>
              </ProductLink>
            );
          })}
        </ProductSwitcherDialog>

        <ProductVersionDialog
          open={versionSwitcherOpen}
          onClick={() => dispatch(closeTOC())}
        >
          {activeProduct?.versions?.map((version, index) => (
            <VersionLink
              key={version!.path! + index}
              to={`/docs/${activeProduct.path}/${version!.path!}`}
            >
              {version!.title!}
            </VersionLink>
          ))}
        </ProductVersionDialog>

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
                !!activeVersion?.path?.length ? "/" + activeVersion.path! : ""
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
  return itemPath === selectedPath;
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

export const Navigation = styled.nav<{ calculatedHeight: string }>`
  ${DocPageStickySideBarStyle}
  padding: 25px 0 0;
  transition: margin-left 250ms;
  background-color: white;
  min-height: 50%;

  &.show {
    margin-left: 0;
  }

  ${({ calculatedHeight }) =>
    IsTablet(`
      margin-left: -105%;
      height: ${calculatedHeight};
      position: fixed;
      top: 60px;
      left: 0;
      ${BoxShadow}
    `)}
`;

const ProductSwitcherButton = styled.button`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  border: 1px solid #ccc;
  border-radius: 5px;
  padding: 7px 10px;
  height: 38px;
  font-size: 0.833em;
  transition: background-color 0.2s ease-in-out;

  > ${IconContainer} {
    margin-left: auto;
    padding-left: 6px;

    > svg {
      fill: #666;
    }
  }

  :hover:enabled {
    background-color: #ddd;
  }

  :disabled {
    cursor: default;
  }
`;

const ProductSwitcher = styled.div`
  margin: 6px 14px 20px 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;

  > ${ProductSwitcherButton}:not(:last-child) {
    margin-right: 4px;
    flex: 1;
  }

  @media only screen and (min-width: 1070px) {
    margin: 0 0 20px 0;
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
    border-radius: 5px;
    padding: 10px;
    width: 700px;
    height: initial;
    box-shadow: 0 3px 6px 0 rgba(0, 0, 0, 0.25);
  }
`;

const ProductVersionDialog = styled.div<{ open: boolean }>`
  display: ${({ open }) => (open ? "flex" : "none")};
  flex-direction: column;
  padding: 10px;
  background-color: #fff;
  position: absolute;
  border-radius: 5px;
  border: 1px solid #ccc;
  top: 110px;
  right: 14px;

  @media only screen and (min-width: 1070px) {
    top: 66px;
    right: 0;
  }

  > :not(:last-child) {
    margin-bottom: 2px;
  }
`;

interface LinkProperties {
  readonly active: boolean;
}

const ProductLink = styled(Link)<LinkProperties>`
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

  transition: background-color 0.2s ease-in-out;

  ${({ active }) => active && `background-color: #ddd;`}

  :hover {
    background-color: #ddd;
  }
`;

const VersionLink = styled(Link)`
  font-size: 0.833em;
  color: #666;
  cursor: pointer;
  padding: 4px 9px;
  transition: background-color 0.2s ease-in-out;
  border-radius: 5px;

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

const NavigationList = styled.ol`
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0 14px 20px;
  list-style-type: none;

  @media only screen and (min-width: 1070px) {
    display: flex;
    padding: 0;
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
