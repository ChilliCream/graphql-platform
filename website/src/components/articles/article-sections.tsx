import { graphql, Link } from "gatsby";
import React, {
  Fragment,
  FunctionComponent,
  useCallback,
  useEffect,
  useState,
} from "react";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";
import { ArticleSectionsFragment } from "../../../graphql-types";
import { State } from "../../state";
import { closeAside } from "../../state/common";
import { MostProminentSection } from "../doc-page/doc-page-elements";

interface ArticleSectionsProperties {
  data: ArticleSectionsFragment;
}

export const ArticleSections: FunctionComponent<ArticleSectionsProperties> = ({
  data,
}) => {
  const dispatch = useDispatch();
  const [activeHeadingId, setActiveHeadingId] = useState<string>();
  const [headings, setHeadings] = useState<Heading[]>([]);
  const yScrollPosition = useSelector<State, number>(
    (state) => state.common.yScrollPosition
  );

  const handleCloseClick = useCallback(() => {
    dispatch(closeAside());
  }, []);

  useEffect(() => {
    const result = ((data.tableOfContents.items as TableOfContentItem[]) ?? [])
      .flatMap((item) => [item, ...(item.items ?? [])])
      .map((item) => ({
        id: item.url,
        position:
          (document.getElementById(item.url.substring(1))?.offsetTop ?? 80) -
          80,
      }))
      .reverse();

    setHeadings(result);
  }, [data]);

  useEffect(() => {
    const activeHeading = headings.find((id) => yScrollPosition >= id.position)
      ?.id;
    window.history.pushState(
      undefined,
      activeHeading || "ChilliCream Docs",
      "./" + (activeHeading || "")
    );
    setActiveHeadingId(activeHeading);
  }, [headings, yScrollPosition]);

  const tocItems: TableOfContentItem[] = data.tableOfContents.items ?? [];

  return tocItems.length > 0 ? (
    <Container>
      <Title>In this article</Title>
      <MostProminentSection>
        <div onClick={handleCloseClick}>
          <TableOfContent items={tocItems} activeHeadingId={activeHeadingId} />
        </div>
      </MostProminentSection>
    </Container>
  ) : null;
};

interface Heading {
  id: string;
  position: number;
}

interface TableOfContentProps {
  items: TableOfContentItem[];
  activeHeadingId: string | undefined;
}

const TableOfContent: FunctionComponent<TableOfContentProps> = ({
  items,
  activeHeadingId,
}) => {
  return (
    <TocItemContainer>
      {items.map((item) => (
        <Fragment key={item.url}>
          <TocListItem
            className={activeHeadingId === item.url ? "active" : undefined}
          >
            <TocLink to={item.url}>{item.title}</TocLink>
          </TocListItem>
          {item.items && (
            <TableOfContent
              items={item.items ?? []}
              activeHeadingId={activeHeadingId}
            />
          )}
        </Fragment>
      ))}
    </TocItemContainer>
  );
};

interface TableOfContentItem {
  title: string;
  url: string;
  items?: TableOfContentItem[];
}

export const ArticleSectionsGraphQLFragment = graphql`
  fragment ArticleSections on Mdx {
    tableOfContents(maxDepth: 2)
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
  position: absolute;
  display: block;
  margin: 0;
  padding: 0 25px 10px;
  list-style-type: none;

  @media only screen and (min-width: 1320px) {
    padding: 0 20px 10px;
  }
`;

const TocLink = styled((props) => <Link {...props} />)`
  font-size: 0.833em;
  color: #666;

  :hover {
    color: #000;
  }
`;

const TocListItem = styled.li`
  flex: 0 0 auto;
  margin: 5px 0;
  padding: 0;
  line-height: initial;

  > ${TocItemContainer} {
    padding-right: 0;
  }

  &.active > ${TocLink} {
    font-weight: bold;
  }
`;
