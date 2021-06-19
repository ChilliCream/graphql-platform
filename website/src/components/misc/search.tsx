import algoliasearch from "algoliasearch/lite";
import React, {
  createRef,
  FocusEvent,
  FunctionComponent,
  RefObject,
  useEffect,
  useState,
} from "react";
import { BasicDoc, SearchBoxProvided } from "react-instantsearch-core";
import {
  connectSearchBox,
  connectStateResults,
  Hits,
  HitsProps,
  Index,
  InstantSearch,
  Snippet,
} from "react-instantsearch-dom";
import styled from "styled-components";
import AlgoliaLogoSvg from "../../images/algolia-logo.svg";
import { Link } from "./link";

const searchClient = algoliasearch(
  process.env.GATSBY_ALGOLIA_APP_ID!,
  process.env.GATSBY_ALGOLIA_SEARCH_KEY!
);

interface SearchProperties {
  siteUrl: string;
}

export const Search: FunctionComponent<SearchProperties> = ({ siteUrl }) => {
  const ref = createRef<HTMLDivElement>();
  const [focus, setFocus] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");

  useClickOutside(ref, () => setFocus(false));

  return (
    <Container ref={ref}>
      <InstantSearch
        searchState={{ query: searchQuery }}
        searchClient={searchClient}
        indexName={process.env.GATSBY_ALGOLIA_INDEX!}
        onSearchStateChange={({ query }) => setSearchQuery(query)}
      >
        <SearchBox onFocus={() => setFocus(true)} />

        {searchQuery && focus ? (
          <HitsWrapper>
            <Index indexName={process.env.GATSBY_ALGOLIA_INDEX!}>
              <ResultHeader>
                <Stats />
              </ResultHeader>
              <Results>
                <Hits hitComponent={DocHit(() => setFocus(false))} />
              </Results>
            </Index>
            <PoweredBy>
              Powered by
              <Link to="https://algolia.com" onClick={() => setFocus(false)}>
                <AlgoliaLogoSvg />
              </Link>
            </PoweredBy>
          </HitsWrapper>
        ) : null}
      </InstantSearch>
    </Container>
  );
};

type EventName = keyof DocumentEventMap;

function useClickOutside(
  ref: RefObject<HTMLDivElement>,
  handler: () => void,
  events?: EventName[]
) {
  const eventNames = events || ["mousedown", "touchstart"];

  const detectClickOutside = (event: DocumentEventMap[EventName]) =>
    !ref.current!.contains(event.target as Node) && handler();

  useEffect(() => {
    for (const eventName of eventNames) {
      document.addEventListener(eventName, detectClickOutside);
    }

    return () => {
      for (const eventName of eventNames) {
        document.removeEventListener(eventName, detectClickOutside);
      }
    };
  }, [detectClickOutside]);
}

interface SearchBoxProperties extends SearchBoxProvided {
  onFocus: (event: FocusEvent<HTMLInputElement>) => void;
}

const SearchBox = connectSearchBox<SearchBoxProperties>(
  ({ currentRefinement, onFocus, refine }) => (
    <SearchField
      type="text"
      value={currentRefinement}
      placeholder="Search..."
      aria-label="Search"
      onChange={(e) => refine(e.target.value)}
      onFocus={onFocus}
    />
  )
);

const Results = connectStateResults((state) => {
  const numberOfResults = state.searchResults?.nbHits;

  if (numberOfResults > 0) {
    return <>{state.children}</>;
  }

  return <>No results for '{state.searchState.query}'</>;
});

const Stats = connectStateResults((state) => {
  const numberOfResults = state.searchResults?.nbHits;

  if (!numberOfResults || numberOfResults < 1) {
    return null;
  }

  return (
    <>
      {numberOfResults} result{numberOfResults > 1 ? "s" : null}
    </>
  );
});

type DocHitComponent = HitsProps<BasicDoc>["hitComponent"];

const DocHit =
  (onClick: () => void): DocHitComponent =>
  ({ hit }) => {
    return (
      <Link to={hit.slug} onClick={onClick}>
        <Snippet attribute="excerpt" hit={hit} tagName="mark" />
      </Link>
    );
  };

const Container = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: row;
  align-items: center;
  justify-content: flex-end;

  @media only screen and (min-width: 992px) {
    display: flex;
    flex: 0 0 auto;
  }
`;

const SearchField = styled.input`
  border: 0;
  border-radius: 4px;
  padding: 10px 15px;
  width: 100%;
  font-family: "Roboto", sans-serif;
  font-size: 0.833em;
  background-color: #fff;
`;

const HitsWrapper = styled.div`
  position: fixed;
  top: 60px;
  right: 0;
  left: 0;
  z-index: 1;
  display: grid;
  padding: 15px 20px;
  max-height: 80vh;
  overflow-y: initial;
  background: white;
  box-shadow: 0px 3px 6px 0px rgba(0, 0, 0, 0.25);

  > * + * {
    margin-top: 5px;
    border-top: 1px solid #aaa;
  }

  li {
    margin: 0;
    padding: 10px 0;
    line-height: 1.667em;

    > a {
      color: var(--brand-color);

      &:hover {
        color: #667;
      }
    }
  }

  li + li {
    border-top: 1px solid #aaa;
  }

  * {
    margin-top: 0;
    padding: 0;
  }

  ul {
    margin: 0;
    list-style: none;
  }

  mark {
    display: inline-block;
    padding: 3px 2px;
    background: var(--brand-color);
    color: var(--text-color-contrast);
  }

  @media only screen and (min-width: 600px) {
    right: initial;
    left: initial;
    border-radius: 4px;
    padding: 10px 15px;
    width: 400px;
  }
`;

const ResultHeader = styled.div`
  display: flex;
  justify-content: space-between;
  margin: 10px 0;
`;

const PoweredBy = styled.div`
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding-top: 10px;
  font-size: 0.688em;
  line-height: 1.667em;

  svg {
    margin-left: 10px;
    width: 70px;
    height: auto;
  }
`;
