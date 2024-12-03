import { graphql } from "gatsby";
import React, { FC } from "react";

import { ScrollContainer } from "@/components/article-elements";
import { Link } from "@/components/misc";
import { DefaultArticleNavigationFragment } from "@/graphql-types";
import {
  NavigationItem,
  NavigationLink,
  NavigationList,
} from "./article-navigation-elements";

export interface DefaultArticleNavigationProps {
  readonly data: DefaultArticleNavigationFragment;
  readonly selectedPath: string;
}

export const DefaultArticleNavigation: FC<DefaultArticleNavigationProps> = ({
  data,
  selectedPath,
}) => {
  const links = (data.navigation?.links ?? []) as Link[];

  return (
    <ScrollContainer>
      <NavigationList>
        {links.map(({ title, path }) => {
          const absolutePath = path.startsWith("/") ? path : "/" + path;

          return (
            <NavigationItem
              key={absolutePath}
              active={selectedPath === absolutePath}
            >
              <NavigationLink to={absolutePath}>{title}</NavigationLink>
            </NavigationItem>
          );
        })}
      </NavigationList>
    </ScrollContainer>
  );
};

export const DefaultArticleNavigationGraphQLFragment = graphql`
  fragment DefaultArticleNavigation on Query {
    navigation: file(
      sourceInstanceName: { eq: "basic" }
      relativePath: { eq: "basic.json" }
    ) {
      links: childrenBasicJson {
        path
        title
      }
    }
  }
`;

interface Link {
  path: string;
  title: string;
}
