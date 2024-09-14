import React, { FC, useEffect } from "react";
import { useCookies } from "react-cookie";
import { useDispatch, useSelector } from "react-redux";

import { State } from "@/state";
import { hideCookieConsent, showCookieConsent } from "@/state/common";
import styled from "styled-components";
import { Dialog, DialogButton, DialogContainer, LearnMoreLink } from "./dialog";

export const CookieConsent: FC = () => {
  const show = useSelector<State, boolean>(
    (state) => state.common.showCookieConsent
  );
  const dispatch = useDispatch();
  const cookieName = "chillicream-cookie-consent";
  const [cookies, setCookie] = useCookies([cookieName]);
  const consentCookieValue = cookies[cookieName];

  const clickDismiss = () => {
    const expires = new Date();

    expires.setFullYear(new Date().getFullYear() + 1);

    setCookie(cookieName, "true", { path: "/", expires });
  };

  useEffect(() => {
    if (consentCookieValue === "true") {
      dispatch(hideCookieConsent());
    } else {
      dispatch(showCookieConsent());
    }
  }, [consentCookieValue]);

  return (
    <Dialog
      role="dialog"
      aria-live="polite"
      aria-label="cookieconsent"
      aria-describedby="cookieconsent:desc"
      show={show}
    >
      {show && (
        <Container>
          <Message id="cookieconsent:desc">
            This website uses cookies to ensure you get the best experience on
            our website.{" "}
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
      )}
    </Dialog>
  );
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
