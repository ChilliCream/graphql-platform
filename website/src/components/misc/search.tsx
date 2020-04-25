import algoliasearch from "algoliasearch/lite";
import React, {
  createRef,
  FocusEvent,
  FunctionComponent,
  RefObject,
  useCallback,
  useEffect,
  useState
} from "react";
import { SearchBoxProvided } from "react-instantsearch-core";
import {
  connectSearchBox,
  connectStateResults,
  Hits,
  Index,
  InstantSearch,
  Snippet,
} from "react-instantsearch-dom";
import { useDispatch, useStore, useSelector } from "react-redux";
import styled from "styled-components";
import { State } from "../../state";
import { changeSearchQuery } from "../../state/common";
import { Link } from "./link";

export const Search: FunctionComponent = () => {
  const ref = createRef<HTMLDivElement>();
  const initialQuery = useStore<State>().getState().common.searchQuery;
  const query = useSelector<State, string>(
    state => state.common.searchQuery
  );
  const dispatch = useDispatch();
  const [focus, setFocus] = useState(false);
  const searchClient = algoliasearch(
    "BH4D9OD16A",
    "3ed63973f167d1fc290b9a1aaa85a793"
  );

  const handleChangeQuery = useCallback((query: string) => {
    dispatch(changeSearchQuery({ query }));
  }, []);

  useClickOutside(ref, () => setFocus(false));

  return (
    <Container ref={ref}>
      <InstantSearch
        searchState={{ query: initialQuery }}
        searchClient={searchClient}
        indexName="chillicream"
        onSearchStateChange={({ query }) => handleChangeQuery(query)}
      >
        <SearchBox
          onFocus={() => setFocus(true)}
        />
        <HitsWrapper show={query.length > 0 && focus}>
          <Index indexName="chillicream">
            <ResultHeader>
              <Stats />
            </ResultHeader>
            <Results>
              <Hits hitComponent={DocHit(() => setFocus(false))} />
            </Results>
          </Index>
          <PoweredBy>
            Powered by{` `}
            <Link to="https://algolia.com" onClick={() => setFocus(false)}>
              Algolia
            </Link>
          </PoweredBy>
        </HitsWrapper>
      </InstantSearch>
    </Container>
  );
};

type EventName = keyof DocumentEventMap;

function useClickOutside(
  ref: RefObject<HTMLDivElement>,
  handler: () => void,
  events?: EventName[]) {
    const eventNames = events || ["mousedown", "touchstart"];

    const detectClickOutside = (event: DocumentEventMap[EventName]) =>
      !ref.current!.contains(event.target as Node) && handler();

    useEffect(() => {
      for (const eventName of eventNames) {
        document.addEventListener(eventName, detectClickOutside)
      }

      return () => {
        for (const eventName of eventNames) {
          document.removeEventListener(eventName, detectClickOutside)
        }
      }
    }, [detectClickOutside]);
}

interface SearchBoxProperties extends SearchBoxProvided {
  value: string;
  onFocus: (event: FocusEvent<HTMLInputElement>) => void;
}

const SearchBox = connectSearchBox<SearchBoxProperties>(({
  currentRefinement,
  onFocus,
  refine
}) => (
  <SearchField
    type="text"
    value={currentRefinement}
    placeholder="Search..."
    aria-label="Search"
    onChange={e => refine(e.target.value)}
    onFocus={onFocus}
  />
));

const Results = connectStateResults(comp =>
    comp.searchResults && comp.searchResults.nbHits > 0
      ? comp.children as any
      : `No results for '${comp.searchState.query}'` as any);

const Stats = connectStateResults(comp =>
    comp.searchResults && comp.searchResults.nbHits > 0 &&
      `${comp.searchResults.nbHits} result${comp.searchResults.nbHits > 1 ? `s` : ``}` as any);

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

interface HitComponentProperties {
  hit: any;
}

export const DocHit = (clickHandler: () => void) => ({ hit }: HitComponentProperties) => (
  <div>
    <Link to={hit.url} onClick={clickHandler}>
      <Snippet attribute="content" hit={hit} tagName="mark" />
    </Link>
  </div>
);

const HitsWrapper = styled.div<{ show: boolean }>`
  position: fixed;
  top: 60px;
  z-index: 2;
  display: ${props => (props.show ? `grid` : `none`)};
  border-radius: 4px;
  padding: 10px 15px;
  width: 80vw;
  max-width: 400px;
  max-height: 80vh;
  overflow: scroll;
  -webkit-overflow-scrolling: touch;
  background: white;
  box-shadow: 0px 3px 6px 0px rgba(0, 0, 0, 0.25);

  > * + * {
    border-top: 2px solid ${props => props.theme.darkGray};
    padding-top: 10px !important;
  }

  li + li {
    margin-top: 5px;
    border-top: 1px solid ${props => props.theme.lightGray};
    padding-top: 5px;
  }

  * {
    margin-top: 0;
    padding: 0;
  }

  ul {
    list-style: none;
  }

  mark {
    background: black;
    color: white;
  }
`;

const ResultHeader = styled.div`
  display: flex;
  justify-content: space-between;
  margin-bottom: 0.3em;
`;

const PoweredBy = styled.div`
  font-size: 0.833em;
`;
