import { graphql } from "gatsby";
import React, { FunctionComponent, useCallback, useEffect } from "react";
import { useDispatch } from "react-redux";
import styled from "styled-components";
import { ArticleSectionsFragment } from "../../../graphql-types";
import { closeAside } from "../../state/common";
import { MostProminentSection } from "./doc-page-elements";

interface ArticleSectionsProperties {
  data: ArticleSectionsFragment;
}

export const ArticleSections: FunctionComponent<ArticleSectionsProperties> = ({
  data,
}) => {
  const dispatch = useDispatch();

  const handleCloseClick = useCallback(() => {
    dispatch(closeAside());
  }, []);

  useEffect(() => {
    const ids = data
      .tableOfContents!.split(/"|\//)
      .filter((item) => item.indexOf("#") === 0)
      .map((item) => {
        const id = item.substring(1);

        return {
          id: `toc-${id}`,
          position: document.getElementById(id)!.offsetTop - 80,
        };
      })
      .reverse();
    let currentActiveId: string | undefined;

    const handler = () => {
      const currentScrollPosition =
        document.body.scrollTop || document.documentElement.scrollTop;
      let newActiveId: string | undefined;

      for (let i = 0; i < ids.length; i++) {
        if (currentScrollPosition >= ids[i].position) {
          newActiveId = ids[i].id;
          break;
        }
      }

      if (currentActiveId !== newActiveId) {
        if (currentActiveId) {
          document.getElementById(currentActiveId)!.parentElement!.className =
            "";
        }

        currentActiveId = newActiveId;

        if (currentActiveId) {
          document.getElementById(currentActiveId)!.parentElement!.className =
            "active";
        }
      }
    };

    if (ids.length > 0) {
      document.addEventListener("scroll", handler);
    }

    return () => {
      if (ids.length > 0) {
        document.removeEventListener("scroll", handler);
      }
    };
  }, [data]);

  return data.tableOfContents!.length > 0 ? (
    <Container>
      <Title>In this article</Title>
      <MostProminentSection>
        <Content
          onClick={handleCloseClick}
          dangerouslySetInnerHTML={{
            __html: data.tableOfContents!.replace(
              /href=\"(.*?#)(.*?)\"/gi,
              'id="toc-$2" href="/docs$1$2"'
            ),
          }}
        />
      </MostProminentSection>
    </Container>
  ) : (
    <></>
  );
};

export const ArticleSectionsGraphQLFragment = graphql`
  fragment ArticleSections on MarkdownRemark {
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

const Content = styled.div`
  ul {
    display: flex;
    flex-direction: column;
    margin: 0;
    padding: 0 25px 10px;
    list-style-type: none;

    li {
      flex: 0 0 auto;
      margin: 5px 0;
      padding: 0;
      line-height: initial;

      > p {
        margin: 0;
        padding: 0;
      }

      > ul {
        padding-right: 0;
      }

      &.active > a,
      > p.active > a {
        font-weight: bold;
      }

      > a,
      > p > a {
        font-size: 0.833em;
        color: #666;

        :hover {
          color: #000;
        }
      }
    }

    @media only screen and (min-width: 1320px) {
      padding: 0 20px 10px;
    }
  }
`;
