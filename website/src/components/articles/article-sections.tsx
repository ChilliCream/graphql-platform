import { graphql, Link } from "gatsby";
import GithubSlugger from "github-slugger";
import React, { FunctionComponent, useEffect, useMemo, useState } from "react";
import { useDispatch } from "react-redux";
import styled, { css } from "styled-components";
import { ArticleSectionsFragment } from "../../../graphql-types";
import { useObservable } from "../../state";
import { closeAside } from "../../state/common";
import { MostProminentSection } from "../doc-page/doc-page-elements";

// todo: place these methods elsewhere
const MAX_DEPTH = 2;

function getTocItemsFromHeadings(
  headings: ArticleSectionsFragment["headings"]
): TableOfContentItem[] {
  const items: TableOfContentItem[] = [];

  if (!headings || headings?.length < 1) {
    return items;
  }

  const slugger = new GithubSlugger();

  // this represents a path to the current item
  let parents: TableOfContentItem[] = [];

  for (const heading of headings) {
    if (!heading?.value) {
      continue;
    }

    const headingDepth = heading.depth ?? 0;

    if (headingDepth > MAX_DEPTH) {
      continue;
    }

    const item: TableOfContentItem = {
      title: heading.value,
      slug: slugger.slug(heading.value),
      items: [],
    };

    if (headingDepth === 1) {
      items.push(item);

      parents = [item];
    } else {
      // we went up in depth, so lets remove parents until we find the parent
      // directly above us
      while (parents.length >= headingDepth) {
        parents.pop();
      }

      const parent = parents[parents.length - 1];

      parent.items?.push(item);

      parents.push(item);
    }
  }

  return items;
}

export function getElementIdFromSlug(slug: string): string {
  return "link-" + slug;
}

function useActiveSlug(items: TableOfContentItem[]) {
  const [activeSlug, setActiveSlug] = useState<string>();

  const yScrollPosition$ = useObservable(
    (state) => state.common.yScrollPosition
  );

  useEffect(() => {
    if (items.length < 1) {
      return;
    }

    const headings = items
      .flatMap((item) => [item, ...(item.items ?? [])])
      .map<Heading>((item) => {
        const elementId = getElementIdFromSlug(item.slug);
        const element = document.getElementById(elementId);

        const offsetTop = element?.offsetTop ?? 80;

        return {
          slug: item.slug,
          position: offsetTop - 80,
        };
      })
      .reverse();

    const subscription = yScrollPosition$.subscribe((yScrollPosition) => {
      for (let i = 0; i < headings.length; i++) {
        if (yScrollPosition < headings[i].position) {
          if (i === headings.length - 1) {
            setActiveSlug(undefined);
          }

          continue;
        }

        const activeHeading = headings[i];

        if (!activeHeading || activeHeading.slug === activeSlug) {
          return;
        }

        setActiveSlug(activeHeading.slug);
        break;
      }
    });

    return () => {
      subscription.unsubscribe();
    };
  }, [items]);

  return activeSlug;
}

interface ArticleSectionsProperties {
  readonly data: ArticleSectionsFragment;
}

export const ArticleSections: FunctionComponent<ArticleSectionsProperties> = ({
  data,
}) => {
  const dispatch = useDispatch();

  const tocItems = useMemo(() => getTocItemsFromHeadings(data.headings), [
    data.headings,
  ]);

  const activeSlug = useActiveSlug(tocItems);

  if (tocItems.length < 1) {
    return null;
  }

  const handleCloseClick = () => {
    dispatch(closeAside());
  };

  return (
    <Container>
      <Title>In this article</Title>
      <MostProminentSection>
        <div onClick={handleCloseClick}>
          <TableOfContent items={tocItems} activeSlug={activeSlug} />
        </div>
      </MostProminentSection>
    </Container>
  );
};

interface Heading {
  readonly slug: string;
  readonly position: number;
}

interface TableOfContentProps {
  readonly items: TableOfContentItem[];
  readonly activeSlug?: string;
}

const TableOfContent: FunctionComponent<TableOfContentProps> = ({
  items,
  activeSlug,
}) => {
  return (
    <TocItemContainer>
      {items.map((item) => (
        <TocListItem key={item.slug} active={activeSlug === item.slug}>
          <TocLink to={"#" + item.slug}>{item.title}</TocLink>
          {item.items && (
            <TableOfContent items={item.items ?? []} activeSlug={activeSlug} />
          )}
        </TocListItem>
      ))}
    </TocItemContainer>
  );
};

interface TableOfContentItem {
  readonly title: string;
  readonly slug: string;
  readonly items?: TableOfContentItem[];
}

export const ArticleSectionsGraphQLFragment = graphql`
  fragment ArticleSections on Mdx {
    headings {
      depth
      value
    }
  }
`;

const Container = styled.section`
  margin-bottom: 20px;
`;

const Title = styled.h6`
  padding: 0 25px;
  font-size: 0.833em;

  @media only screen and (min-width: 1320px) {
    padding: 0 20px;
  }
`;

const TocItemContainer = styled.ul`
  display: block;
  margin: 0;
  padding: 0 25px 2px;
  list-style-type: none;

  @media only screen and (min-width: 1320px) {
    padding: 0 20px 2px;
  }
`;

const TocLink = styled((props) => <Link {...props} />)`
  font-size: 0.833em;
  color: #666;

  :hover {
    color: #000;
  }
`;

interface TocListItemProperties {
  active: boolean;
}

const TocListItem = styled.li<TocListItemProperties>`
  flex: 0 0 auto;
  margin: 5px 0;
  padding: 0;
  line-height: initial;

  > ${TocItemContainer} {
    padding-right: 0;
  }

  ${({ active }) =>
    active &&
    css`
      > ${TocLink} {
        font-weight: bold;
      }
    `}
`;
