import React, { FC } from "react";

import { ScrollContainer } from "@/components/article-elements";
import {
  NavigationItem,
  NavigationLink,
  NavigationList,
} from "./article-navigation-elements";

interface NavLink {
  path: string;
  title: string;
}

interface DefaultArticleNavigationData {
  navigation?: {
    links?: NavLink[] | null;
  } | null;
  [key: string]: any;
}

export interface DefaultArticleNavigationProps {
  readonly data: DefaultArticleNavigationData;
  readonly selectedPath: string;
}

export const DefaultArticleNavigation: FC<DefaultArticleNavigationProps> = ({
  data,
  selectedPath,
}) => {
  const links = (data.navigation?.links ?? []) as NavLink[];

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
