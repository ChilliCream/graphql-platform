import React, { FC } from "react";

import {
  ArticleContentVideoContainer,
  ArticleVideo,
} from "@/components/article-elements";

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
      <ArticleVideo videoId={id} />
    </ArticleContentVideoContainer>
  );
};
