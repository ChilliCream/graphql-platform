import "@docsearch/css";
import { DocSearchModal } from "@docsearch/react";
import type {
  InternalDocSearchHit,
  StoredDocSearchHit,
} from "@docsearch/react/dist/esm/types";
import React, { FC, ReactNode } from "react";
import { createPortal } from "react-dom";
import { createGlobalStyle } from "styled-components";

import { THEME_COLORS } from "@/style";
import { Link } from "./link";

export interface SearchModalProps {
  readonly open: boolean;
  readonly siteUrl: string;
  readonly onClose: () => void;
}

export const SearchModal: FC<SearchModalProps> = ({
  open,
  siteUrl,
  onClose,
}) => {
  const resolveHit: (props: HitProps) => JSX.Element = ({ children, hit }) => {
    const slug = hit.url.replace(siteUrl, "");

    return <Link to={slug}>{children}</Link>;
  };

  return (
    <>
      <DocSearchStyleOverwrite />
      {open
        ? createPortal(
            <DocSearchModal
              appId="WQ7ZRCU9RS"
              apiKey="b40ebfd92eb180185aa52c192e4fbd86"
              indexName="chillicream"
              placeholder="Search..."
              hitComponent={resolveHit}
              initialScrollY={window ? window.scrollY : 0}
              onClose={onClose}
            />,
            document.body
          )
        : null}
    </>
  );
};

interface HitProps {
  readonly children: ReactNode;
  readonly hit: InternalDocSearchHit | StoredDocSearchHit;
}

const DocSearchStyleOverwrite = createGlobalStyle`
  :root {
    --docsearch-primary-color: ${THEME_COLORS.primary};
    --docsearch-text-color: ${THEME_COLORS.text};
    --docsearch-spacing: 12px;
    --docsearch-icon-stroke-width: 1.4;
    --docsearch-highlight-color: ${THEME_COLORS.primary};
    --docsearch-muted-color: ${THEME_COLORS.text};
    --docsearch-container-background: rgba(101,108,133,0.8);
    --docsearch-logo-color: ${THEME_COLORS.primary};
    --docsearch-modal-width: 560px;
    --docsearch-modal-height: 480px;
    --docsearch-modal-background: ${THEME_COLORS.background};
    --docsearch-modal-shadow: 0 1px 4px 0 ${THEME_COLORS.shadow};
    --docsearch-searchbox-height: 56px;
    --docsearch-searchbox-background: ${THEME_COLORS.background};
    --docsearch-searchbox-focus-background: ${THEME_COLORS.background};
    --docsearch-searchbox-shadow: 0 1px 4px 0 ${THEME_COLORS.shadow};
    --docsearch-hit-height: 56px;
    --docsearch-hit-color: ${THEME_COLORS.text};
    --docsearch-hit-active-color: ${THEME_COLORS.textContrast};
    --docsearch-hit-background: ${THEME_COLORS.textContrast};
    --docsearch-hit-shadow: none;
    --docsearch-key-gradient: none;
    --docsearch-key-shadow: none;
    --docsearch-footer-height: 44px;
    --docsearch-footer-background: ${THEME_COLORS.background};
    --docsearch-footer-shadow: none;
  }

  .DocSearch-SearchBar {
    padding-bottom: 10px;
  }

  .DocSearch-Commands {
    display: none;
  }

  .DocSearch-Hit {
    margin: 0;
  }
`;
