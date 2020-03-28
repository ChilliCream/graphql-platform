import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";
import { DocPageNavigationFragment } from "../../../graphql-types";
import { State } from "../../state";
import { toggleNavigationGroup } from "../../state/common";
import { IconContainer } from "./icon-container";
import { Link } from "./link";

import ArrowDownIconSvg from "../../images/arrow-down.svg";
import ArrowUpIconSvg from "../../images/arrow-up.svg";

interface DocPageNavigationProperties {
  data: DocPageNavigationFragment;
}

export const DocPageNavigation: FunctionComponent<DocPageNavigationProperties> = ({
  data,
}) => {
  const expandedPaths = useSelector<State, string[]>(
    (state) => state.common.expandedPaths
  );
  const dispatch = useDispatch();

  const handleToggleExpand = (path: string) => {
    dispatch(toggleNavigationGroup({ path }));
  };

  const buildNavigationStructure = (items: Item[], basePath: string) => (
    <NavigationList>
      {items.map(({ path, title, items: subItems }) => {
        const itemPath =
          !subItems && path === "index" ? basePath : basePath + "/" + path;

        return (
          <NavigationItem key={itemPath}>
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

  return (
    <Navigation>
      <FixedContainer>
        {data.config?.products &&
          data.config.products[0]?.items &&
          buildNavigationStructure(
            data.config.products[0].items
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
            "/docs/" + data.config.products[0].path!
          )}
      </FixedContainer>
    </Navigation>
  );
};

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

const Navigation = styled.nav`
  display: flex;
  flex: 0 0 250px;
  flex-direction: column;

  * {
    user-select: none;
  }

  @media only screen and (min-width: 992px) {
    display: flex;
  }
`;

const FixedContainer = styled.div`
  position: fixed;
  padding: 25px 0 250px;
  width: 250px;
`;

const NavigationList = styled.ol`
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0 20px 20px;
`;

const NavigationItem = styled.li`
  flex: 0 0 auto;
  margin: 5px 0;
  padding: 0;
  min-height: 20px;
  list-style-type: none;
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
    padding: 5px 0;
  }
`;

const NavigationGroup = styled.div<{ expanded: boolean }>`
  display: flex;
  flex-direction: column;
  cursor: pointer;

  > ${NavigationGroupContent} {
    display: ${(props) => (props.expanded ? "initial" : "none")};
  }

  > ${NavigationGroupToggle} > ${IconContainer} {
    margin-left: auto;

    > .arrow-down {
      display: ${(props) => (props.expanded ? "none" : "initial")};
      fill: #666;
    }

    > .arrow-up {
      display: ${(props) => (props.expanded ? "initial" : "none")};
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
