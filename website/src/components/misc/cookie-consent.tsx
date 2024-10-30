import React, { FC, useCallback, useEffect, useState } from "react";
import { useCookies } from "react-cookie";

import styled from "styled-components";
import { Dialog, DialogButton, DialogContainer, LearnMoreLink } from "./dialog";

const COOKIE_NAME = "chillicream_website_cookie_consent_shown";

export const CookieConsent: FC = () => {
  const [cookies, setCookie] = useCookies([COOKIE_NAME]);
  const [show, setShow] = useState(false);

  const clickDismiss = useCallback(() => {
    const expires = new Date();

    expires.setFullYear(expires.getFullYear() + 1);

    setCookie(COOKIE_NAME, true, {
      path: "/",
      expires,
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
      aria-label="cookieconsent"
      aria-describedby="cookieconsent:desc"
      show
    >
      <Container>
        <Message id="cookieconsent:desc">
          This website uses cookies to ensure you get the best experience on our
          website.{" "}
          <LearnMoreLink prefetch={false} to="/legal/cookie-policy.html">
            Learn more
          </LearnMoreLink>
        </Message>
        <DialogButton
          aria-label="dismiss cookie message"
          role="button"
          onClick={clickDismiss}
        >
          Got it!
        </DialogButton>
      </Container>
    </Dialog>
  ) : null;
};

const Container = styled(DialogContainer)`
  justify-content: space-between;

  @media only screen and (min-width: 400px) {
    flex-direction: row;

    ${DialogButton} {
      flex: 0 0 160px;
    }
  }
`;

const Message = styled.div`
  flex: 0 1 auto;
  padding-bottom: 20px;
  font-size: var(--font-size);
  line-height: 1.667em;
  color: #0b0722;

  @media only screen and (min-width: 400px) {
    padding-bottom: initial;
    padding-right: 20px;
  }
`;
