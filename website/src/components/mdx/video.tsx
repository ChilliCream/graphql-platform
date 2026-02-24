import React, { FC } from "react";
import LiteYouTubeEmbed from "react-lite-youtube-embed";
import "react-lite-youtube-embed/dist/LiteYouTubeEmbed.css";

import { ArticleContentVideoContainer } from "@/components/article-elements";

export interface VideoProps {
  readonly videoId?: string;
  // rehype-raw lowercases HTML attributes, so accept both casings
  readonly videoid?: string;
}

export const Video: FC<VideoProps> = ({ videoId, videoid }) => {
  const id = videoId || videoid;

  if (!id) return null;

  return (
    <ArticleContentVideoContainer>
      <LiteYouTubeEmbed id={id} title="YouTube video player" />
    </ArticleContentVideoContainer>
  );
};
