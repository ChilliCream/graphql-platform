import { IconContainer } from "@/components/misc";
import { Icon } from "@/components/sprites";
import { THEME_COLORS } from "@/style";
import React, { FC } from "react";
import styled from "styled-components";

// Icons
import PlayIconSvg from "@/images/icons/debug-start.svg";

export interface PlayProps {
  readonly content: string;
  readonly url: string;
  readonly className?: string;
}

export const Play: FC<PlayProps> = ({ content, url, className }) => {
  return (
    <PlayIconButton
      className={className}
      title="Open in Nitro"
      onClick={() => {
        // TODO: Open Nitro with url and content

        alert(`Open '${content}' in Nitro with URL '${url}'`);
      }}
    >
      <IconContainer $size={20}>
        <Icon {...PlayIconSvg} />
      </IconContainer>
    </PlayIconButton>
  );
};

const PlayIconButton = styled.button`
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
