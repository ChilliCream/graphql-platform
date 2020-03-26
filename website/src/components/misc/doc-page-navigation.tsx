import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";
import { DocPageNavigationFragment } from "../../../graphql-types";
import { State } from "../../state";
import { toggleNavigationGroup } from "../../state/navigation/navigation.actions";
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
    state => state.navigation.expandedPaths,
    () => true
  );
  const dispatch = useDispatch();

  const handleToggleExpand = (path: string) => {
    dispatch(toggleNavigationGroup({ path }));
  };

  const buildNavigationStructure = (items: Item[], basePath: string) => (
    <NavigationList>
      {items.map(({ path, title, items: subItems }) => {
        const itemPath = basePath + "/" + path;

        return (
          <NavigationItem key={itemPath}>
            {subItems ? (
              <NavigationGroup open={expandedPaths.indexOf(itemPath) !== -1}>
                <NavigationGroupToggle
                  onClick={() => handleToggleExpand(itemPath)}
                >
                  {title}
                  <IconContainer size={16}>
                    <ArrowDownIconSvg className="arrow-down" />
                    <ArrowUpIconSvg className="arrow-up" />
                  </IconContainer>
                </NavigationGroupToggle>
                {buildNavigationStructure(subItems, itemPath)}
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
              .filter(item => !!item)
              .map<Item>(item => ({
                path: item!.path!,
                title: item!.title!,
                items: item!.items
                  ? item?.items
                      .filter(item => !!item)
                      .map<Item>(item => ({
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
  list-style-type: none;
`;

const NavigationGroupToggle = styled.summary`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 5px 0;
  padding: 0;
  font-size: 0.833em;

  ::-webkit-details-marker {
    display: none;
  }
`;

const NavigationGroup = styled.details`
  margin: 0;
  padding: 0;
  cursor: pointer;

  > ${NavigationList} {
    padding: 0;
  }

  > ${NavigationGroupToggle} > ${IconContainer} {
    margin-left: auto;

    > .arrow-down {
      display: initial;
      fill: #666;
    }

    > .arrow-up {
      display: none;
      fill: #666;
    }
  }

  &[open] > ${NavigationGroupToggle} {
    > ${IconContainer} {
      > .arrow-down {
        display: none;
      }

      > .arrow-up {
        display: initial;
      }
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
