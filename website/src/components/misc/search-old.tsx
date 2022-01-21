import algoliasearch from "algoliasearch/lite";
import React, {
  createRef,
  FC,
  FocusEvent,
  RefObject,
  useCallback,
  useEffect,
  useState,
} from "react";
import { Configure, SearchBoxProvided } from "react-instantsearch-core";
import {
  connectSearchBox,
  connectStateResults,
  Hits,
  Index,
  InstantSearch,
  Snippet,
} from "react-instantsearch-dom";
import { useDispatch, useSelector, useStore } from "react-redux";
import styled from "styled-components";
import AlgoliaLogoSvg from "../../images/algolia-logo.svg";
import { FONT_FAMILY_HEADING, THEME_COLORS } from "../../shared-style";
import { State } from "../../state";
import { changeSearchQuery } from "../../state/common";
import { Link } from "./link";

export interface SearchProps {
  readonly siteUrl: string;
}

export const Search: FC<SearchProps> = ({ siteUrl }) => {
  const ref = createRef<HTMLDivElement>();
  const initialQuery = useStore<State>().getState().common.searchQuery;
  const query = useSelector<State, string>((state) => state.common.searchQuery);
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
        <Configure
          attributesToRetrieve={["url", "anchor"]}
          attributesToHighlight={[]}
          attributesToSnippet={[
            "content:10",
            "hierarchy.lvl0:10",
            "hierarchy.lvl1:10",
            "hierarchy.lvl2:10",
            "hierarchy.lvl3:10",
            "hierarchy.lvl4:10",
            "hierarchy.lvl5:10",
          ]}
          snippetEllipsisText="..."
          distinct
          analytics={false}
        />
        <SearchBox onFocus={() => setFocus(true)} />
        <HitsWrapper show={query.length > 0 && focus}>
          <Index indexName="chillicream">
            <ResultHeader>
              <Stats />
            </ResultHeader>
            <Results>
              <Hits hitComponent={DocHit(siteUrl, () => setFocus(false))} />
            </Results>
          </Index>
          <PoweredBy>
            Powered by
            <Link to="https://algolia.com" onClick={() => setFocus(false)}>
              <AlgoliaLogoSvg />
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

interface SearchBoxProps extends SearchBoxProvided {
  onFocus: (event: FocusEvent<HTMLInputElement>) => void;
}

const SearchBox = connectSearchBox<SearchBoxProps>(
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

const Results = connectStateResults((comp) =>
  comp.searchResults && comp.searchResults.nbHits > 0
    ? (comp.children as any)
    : (`No results for '${comp.searchState.query}'` as any)
);

const Stats = connectStateResults(
  (comp) =>
    comp.searchResults &&
    comp.searchResults.nbHits > 0 &&
    (`${comp.searchResults.nbHits} result${
      comp.searchResults.nbHits > 1 ? `s` : ``
    }` as any)
);

const DocHit =
  (siteUrl: string, clickHandler: () => void) =>
  ({ hit }: HitComponentProps) => {
    const slug = (hit.url as string).replace(siteUrl, "");
    const snippetResult = hit?._snippetResult ?? {};

    let attribute = "content";

    if (!snippetResult.content && !!snippetResult.hierarchy) {
      const hierarchyKeys = Object.keys(snippetResult.hierarchy);

      for (const key of hierarchyKeys) {
        if (!snippetResult.hierarchy[key]) {
          break;
        }

        attribute = "hierarchy." + key;
      }
    }

    return (
      <Link to={slug} onClick={clickHandler}>
        <Snippet attribute={attribute} hit={hit} tagName="mark" />
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
    flex: 0 0 auto;
  }
`;

const SearchField = styled.input`
  border: 0;
  border-radius: var(--border-radius);
  width: 100%;
  padding: 10px 15px;
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 0.833em;
  background-color: ${THEME_COLORS.secondary};
  color: ${THEME_COLORS.textContrast};
  transition: background-color 0.2s ease-in-out;

  ::placeholder {
    color: ${THEME_COLORS.textContrast};
  }

  :hover,
  :focus {
    background-color: ${THEME_COLORS.tertiary};
  }
`;

interface HitComponentProps {
  readonly hit: any;
}

const HitsWrapper = styled.div<{ readonly show: boolean }>`
  position: fixed;
  top: 60px;
  right: 0;
  left: 0;
  z-index: 1;
  display: ${(props) => (props.show ? `grid` : `none`)};
  padding: 15px 20px;
  max-height: 80vh;
  overflow-y: initial;
  background: ${THEME_COLORS.textContrast};
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
      color: ${THEME_COLORS.primary};

      &:hover {
        color: ${THEME_COLORS.text};
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
    background: ${THEME_COLORS.primary};
    color: ${THEME_COLORS.textContrast};
  }

  @media only screen and (min-width: 600px) {
    right: initial;
    left: initial;
    border-radius: var(--border-radius);
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
