import React, { FunctionComponent } from "react";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

const NotFoundPage: FunctionComponent = () => (
  <Layout>
    <SEO title="404: Not found" />
    <h1>NOT FOUND</h1>
    <p>You just hit a route that doesn&#39;t exist... the sadness.</p>
  </Layout>
);

export default NotFoundPage;
