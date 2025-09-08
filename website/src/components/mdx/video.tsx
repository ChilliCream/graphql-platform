import React, { FC } from "react";

import {
  ArticleContentVideoContainer,
  ArticleVideo,
} from "@/components/article-elements";

export interface VideoProps {
  readonly videoId: string;
}

export const Video: FC<VideoProps> = ({ videoId }) => {
  return (
    <ArticleContentVideoContainer>
      <ArticleVideo videoId={videoId} />
    </ArticleContentVideoContainer>
  );
};
