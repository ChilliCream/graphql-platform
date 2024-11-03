import React, { FC, useCallback, useEffect, useState } from "react";
import { useCookies } from "react-cookie";
import styled from "styled-components";

import { Link } from "@/components/misc/link";
import { Icon } from "@/components/sprites";

// Icons
import XmarkIconSvg from "@/images/icons/xmark.svg";

const COOKIE_NAME = "chillicream_website_legacy_doc_shown";

export const DocArticleLegacy: FC = () => {
  const [cookies, setCookie] = useCookies([COOKIE_NAME]);
  const [show, setShow] = useState(false);

  const clickDismiss = useCallback(() => {
    setCookie(COOKIE_NAME, true, {
      path: "/",
      sameSite: "lax",
    });
    setShow(false);
  }, [setCookie, setShow]);

  useEffect(() => {
    setShow(!cookies[COOKIE_NAME]);
  }, [cookies, setShow]);

  return show ? (
    <Dialog
      role="dialog"
      aria-live="polite"
      aria-label="legacydoc"
      aria-describedby="legacydoc:desc"
      show
    >
      <Container>
        <Message id="legacydoc:desc">
          <strong>Important:</strong> This documentation covers Hot Chocolate
          11. For version 10 and lower click{" "}
          <LegacyDocLink to="https://hotchocolate.io">here</LegacyDocLink>.
        </Message>
        <CloseButton
          aria-label="dismiss cookie message"
          role="button"
          onClick={clickDismiss}
        >
          <Icon {...XmarkIconSvg} />
        </CloseButton>
      </Container>
    </Dialog>
  ) : null;
};

const Dialog = styled.div<{ show: boolean }>`
  display: ${({ show }) => (show ? "initial" : "none")};
  background-color: #ffb806;

  @media only screen and (min-width: 860px) {
    > .gatsby-image-wrapper {
      border-radius: var(--border-radius) var(--border-radius) 0 0;
    }
  }
`;

const Container = styled.div`
  display: flex;
  flex-direction: row;
  padding: 10px 20px;

  @media only screen and (min-width: 860px) {
    padding: 10px 50px;
  }
`;

const Message = styled.div`
  flex: 0 1 auto;
  font-size: 0.875rem;
  color: #4f3903;
`;

const LegacyDocLink = styled(Link)`
  text-decoration: underline;
  color: #4f3903;
`;

const CloseButton = styled.button`
  flex: 0 0 auto;
  margin-left: auto;
  cursor: pointer;

  > svg {
    fill: #4f3903;
    width: 24px;
    height: 24px;
  }
`;
