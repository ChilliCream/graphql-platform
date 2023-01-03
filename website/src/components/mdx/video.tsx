import {
  ArticleContentVideoContainer,
  ArticleVideo,
} from "@/components/articles/article-elements";
import React, { FC } from "react";

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
