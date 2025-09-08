import React, { FC, useState } from "react";
import styled from "styled-components";

import { IconContainer } from "@/components/misc";
import { Icon } from "@/components/sprites";
import { FONT_FAMILY, THEME_COLORS } from "@/style";

// Icons
import CopyIconSvg from "@/images/icons/copy.svg";

function copyToClipboard(content: string): void {
  const el = document.createElement(`textarea`);
  el.value = content;
  el.setAttribute(`readonly`, ``);
  el.style.position = `absolute`;
  el.style.left = `-9999px`;
  document.body.appendChild(el);
  el.select();
  document.execCommand(`copy`);
  document.body.removeChild(el);
}

export interface CopyProps {
  readonly content: string;
}

export const Copy: FC<CopyProps> = ({ content }) => {
  const [showToast, setShowToast] = useState(false);

  return (
    <>
      <CopyIconButton
        onClick={() => {
          copyToClipboard(content);

          setShowToast(true);
          setTimeout(() => {
            setShowToast(false);
          }, 3000);
        }}
      >
        <IconContainer $size={20}>
          <Icon {...CopyIconSvg} />
        </IconContainer>
      </CopyIconButton>
      {showToast && <CopySuccessToast />}
    </>
  );
};

const CopySuccessToast: FC = () => {
  return (
    <ToastContainer>
      <ToastText>Copied code example</ToastText>
    </ToastContainer>
  );
};

const ToastText = styled.div`
  font-size: 1.25rem;
  font-family: ${FONT_FAMILY};
  font-weight: 600;
  color: ${THEME_COLORS.textContrast};
`;

const ToastContainer = styled.div`
  position: fixed;
  left: 50%;
  bottom: 30px;
  transform: translateX(-50%);
  z-index: 9999;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--box-border-radius);
  padding: 20px;
  backdrop-filter: blur(4px);
  background-color: ${THEME_COLORS.backdrop};
  opacity: 0;
  animation: animation 3s cubic-bezier(0.98, 0.01, 0.53, 0.47);

  @keyframes animation {
    0%,
    50% {
      opacity: 1;
    }

    50%,
    100% {
      opacity: 0;
    }
  }
`;

const CopyIconButton = styled.button`
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0 var(--box-border-radius) 0 var(--button-border-radius);
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-top: 0 none;
  border-right: 0 none;
  padding: 6px;

  svg {
    fill: #eb64b9;
  }
`;
