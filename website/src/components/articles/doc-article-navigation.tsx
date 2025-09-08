import { graphql } from "gatsby";
import React, {
  FC,
  MouseEvent,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";
import { useDispatch } from "react-redux";
import styled, { css } from "styled-components";

import { ScrollContainer } from "@/components/article-elements";
import { IconContainer, Link } from "@/components/misc";
import { Icon } from "@/components/sprites";
import { DocArticleNavigationFragment } from "@/graphql-types";
import { closeTOC } from "@/state/common";
import { FONT_FAMILY_HEADING, THEME_COLORS } from "@/style";
import {
  NavigationGroup,
  NavigationGroupContent,
  NavigationGroupToggle,
  NavigationItem,
  NavigationLink,
  NavigationList,
} from "./article-navigation-elements";

// Icons
import ChevronDownIconSvg from "@/images/icons/chevron-down.svg";
import ChevronUpIconSvg from "@/images/icons/chevron-up.svg";
import Grid2IconSvg from "@/images/icons/grid-2.svg";

export interface DocArticleNavigationProps {
  readonly data: DocArticleNavigationFragment;
  readonly selectedPath: string;
  readonly selectedProduct: string;
  readonly selectedVersion: string;
}

export const DocArticleNavigation: FC<DocArticleNavigationProps> = ({
  data,
  selectedPath,
  selectedProduct,
  selectedVersion,
}) => {
  const dispatch = useDispatch();

  const [productSwitcherOpen, setProductSwitcherOpen] = useState(false);
  const [versionSwitcherOpen, setVersionSwitcherOpen] = useState(false);

  const products = data.config?.products || [];
  const activeProduct = products.find((p) => p?.path === selectedProduct);
  const activeVersion = activeProduct?.versions?.find(
    (v) => v?.path === selectedVersion
  );

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

  const subItems: Item[] =
    activeVersion?.items
      ?.filter((item) => !!item)
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
      })) ?? [];

  const basePath = `/docs/${activeProduct!.path!}${
    !!activeVersion?.path?.length ? "/" + activeVersion.path : ""
  }`;

  return (
    <>
      <ProductSwitcher>
        <ProductSwitcherButton
          fullWidth={!hasVersions}
          onClick={toggleProductSwitcher}
        >
          {activeProduct?.title}
          <IconContainer $size={14}>
            <Icon {...Grid2IconSvg} />
          </IconContainer>
        </ProductSwitcherButton>
        {hasVersions && (
          <ProductSwitcherButton onClick={toggleVersionSwitcher}>
            {activeVersion?.title}
            <IconContainer $size={14}>
              {versionSwitcherOpen ? (
                <Icon {...ChevronUpIconSvg} />
              ) : (
                <Icon {...ChevronDownIconSvg} />
              )}
            </IconContainer>
          </ProductSwitcherButton>
        )}
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
              to={`/docs/${product.path!}${
                product.latestStableVersion
                  ? "/" + product.latestStableVersion
                  : ""
              }`}
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
        {activeProduct?.versions?.map((version, index) => {
          const newVersionUrl = selectedPath.replace(
            selectedVersion ? "/" + selectedVersion : "",
            version?.path ? "/" + version.path : ""
          );

          return (
            <VersionLink
              active={version === activeVersion}
              key={version!.path! + index}
              to={newVersionUrl}
            >
              {version!.title!}
            </VersionLink>
          );
        })}
      </ProductVersionDialog>
      {!productSwitcherOpen && activeVersion?.items && (
        <ScrollContainer>
          <div>
            <NavigationContainer
              basePath={basePath}
              items={subItems}
              selectedPath={selectedPath}
            />
          </div>
        </ScrollContainer>
      )}
    </>
  );
};

interface NavigationContainerProps {
  readonly basePath: string;
  readonly selectedPath: string;
  readonly items: Item[];
}

const NavigationContainer: FC<NavigationContainerProps> = ({
  items: rawItems,
  basePath,
  selectedPath,
}) => {
  const [expandedPaths, setExpandedPaths] = useState<string[]>([]);
  const dispatch = useDispatch();

  const items = useMemo(
    () =>
      rawItems.map<EnhancedItem>((i) => {
        let fullpath = basePath;

        if (i.path && i.path !== "index") {
          fullpath += "/" + i.path;
        }

        return {
          ...i,
          fullpath,
        };
      }),
    [rawItems]
  );

  useEffect(() => {
    for (const item of items) {
      if (containsActiveItem(selectedPath, item.fullpath)) {
        setExpandedPaths((old) => [...old, item.fullpath]);
      }
    }
  }, [selectedPath, basePath, items]);

  const toggleItem = (itemPath: string) => () => {
    if (expandedPaths.includes(itemPath)) {
      setExpandedPaths((old) => old.filter((e) => e !== itemPath));
    } else {
      setExpandedPaths((old) => [...old, itemPath]);
    }
  };

  return (
    <NavigationList>
      {items.map(({ fullpath, title, items: subItems }) => {
        const isActive = subItems ? containsActiveItem : isActiveItem;

        return (
          <NavigationItem
            key={fullpath}
            active={isActive(selectedPath, fullpath)}
            onClick={() => !subItems && dispatch(closeTOC())}
          >
            {subItems ? (
              <NavigationGroup expanded={expandedPaths.includes(fullpath)}>
                <NavigationGroupToggle onClick={toggleItem(fullpath)}>
                  {title}
                  <IconContainer $size={16}>
                    <Icon {...ChevronDownIconSvg} className="expand" />
                    <Icon {...ChevronUpIconSvg} className="collapse" />
                  </IconContainer>
                </NavigationGroupToggle>
                <NavigationGroupContent>
                  <NavigationContainer
                    items={subItems}
                    basePath={fullpath}
                    selectedPath={selectedPath}
                  />
                </NavigationGroupContent>
              </NavigationGroup>
            ) : (
              <NavigationLink to={fullpath}>{title}</NavigationLink>
            )}
          </NavigationItem>
        );
      })}
    </NavigationList>
  );
};

function isActiveItem(selectedPath: string, itemPath: string) {
  return selectedPath === itemPath;
}

function containsActiveItem(selectedPath: string, itemPath: string) {
  return (selectedPath + "/").startsWith(itemPath + "/");
}

export const DocArticleNavigationGraphQLFragment = graphql`
  fragment DocArticleNavigation on Query {
    config: file(
      sourceInstanceName: { eq: "docs" }
      relativePath: { eq: "docs.json" }
    ) {
      products: childrenDocsJson {
        path
        title
        description
        latestStableVersion
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

type EnhancedItem = Item & {
  fullpath: string;
};

const ProductSwitcherButton = styled.button<{ readonly fullWidth?: boolean }>`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  box-sizing: border-box;
  border-radius: var(--button-border-radius);
  border: 2px solid ${THEME_COLORS.primaryButtonBorder};
  min-width: 62px;
  height: 38px;
  padding-right: 8px;
  padding-left: 8px;
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 0.875rem;
  font-weight: 500;
  color: ${THEME_COLORS.primaryButtonText};
  background-color: ${THEME_COLORS.primaryButton};
  transition: background-color 0.2s ease-in-out, color 0.2s ease-in-out;
  ${({ fullWidth }) => fullWidth && `width: 100%;`}

  > ${IconContainer} {
    margin-left: auto;
    padding-left: 6px;

    > svg {
      fill: ${THEME_COLORS.primaryButtonText};
    }
  }

  :hover:enabled {
    background-color: ${THEME_COLORS.primaryButtonHover};

    > ${IconContainer} > svg {
      fill: ${THEME_COLORS.primaryButtonHoverText};
    }
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
  flex-shrink: 0;

  > ${ProductSwitcherButton}:not(:last-child) {
    margin-right: 4px;
    flex: 1;
  }

  @media only screen and (min-width: 1070px) {
    margin: 0 0 20px 0;
  }
`;

const ProductSwitcherDialog = styled.div<{
  readonly open: boolean;
}>`
  display: ${({ open }) => (open ? "flex" : "none")};
  flex: 1 1 100%;
  flex-direction: column;
  gap: 2px;
  padding: 2px;
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    to right bottom,
    #379dc83d,
    #2b80ad3d,
    #2263903d,
    #1a48743d,
    #112f573d
  );

  @media only screen and (min-width: 1070px) {
    position: fixed;
    top: 135px;
    z-index: 10;
    flex-direction: row;
    flex-wrap: wrap;
    margin-right: 2px;
    margin-left: 2px;
    border: 1px solid ${THEME_COLORS.boxBorder};
    border-radius: var(--button-border-radius);
    width: 220px;
    height: initial;
  }
`;

const ProductVersionDialog = styled.div<{
  readonly open: boolean;
}>`
  position: absolute;
  top: 110px;
  right: 14px;
  display: ${({ open }) => (open ? "flex" : "none")};
  flex-direction: column;
  gap: 2px;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--button-border-radius);
  padding: 2px;
  width: 59px;
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    to right bottom,
    #379dc83d,
    #2b80ad3d,
    #2263903d,
    #1a48743d,
    #112f573d
  );

  @media only screen and (min-width: 1070px) {
    top: 63px;
    right: 2px;
  }
`;

interface LinkProps {
  readonly active: boolean;
}

const ProductLink = styled(Link).withConfig<LinkProps>({
  shouldForwardProp(prop, defaultValidatorFn) {
    return prop === "active" ? false : defaultValidatorFn(prop);
  },
})`
  flex: 0 0 auto;
  border-radius: var(--button-border-radius);
  width: calc(100% - 18px);
  padding: 6px 9px;
  color: ${THEME_COLORS.text};
  cursor: pointer;
  transition: background-color 0.2s ease-in-out, color 0.2s ease-in-out;

  ${({ active }) =>
    active &&
    css`
      color: ${THEME_COLORS.primaryButtonHoverText};
      background-color: ${THEME_COLORS.primaryButtonHover};
    `}

  :hover {
    color: ${THEME_COLORS.primaryButtonHoverText};
    background-color: ${THEME_COLORS.primaryButtonHover};
  }

  @media only screen and (min-width: 1070px) {
    flex: 0 0 auto;
  }
`;

const VersionLink = styled(Link).withConfig<LinkProps>({
  shouldForwardProp(prop, defaultValidatorFn) {
    return prop === "active" ? false : defaultValidatorFn(prop);
  },
})`
  color: ${THEME_COLORS.text};
  border-radius: var(--button-border-radius);
  padding: 6px 9px;
  cursor: pointer;
  transition: background-color 0.2s ease-in-out, color 0.2s ease-in-out;

  ${({ active }) =>
    active &&
    css`
      color: ${THEME_COLORS.primaryButtonHoverText};
      background-color: ${THEME_COLORS.primaryButtonHover};
    `}

  :hover {
    color: ${THEME_COLORS.primaryButtonHoverText};
    background-color: ${THEME_COLORS.primaryButtonHover};
  }
`;

const ProductTitle = styled.h6`
  font-size: 1em;
`;

const ProductDescription = styled.p`
  margin-bottom: 0;
  line-height: 1.2em;
`;
