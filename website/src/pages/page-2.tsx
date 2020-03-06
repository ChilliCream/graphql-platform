import { Link } from "gatsby";
import React, { FunctionComponent } from "react";
import SEO from "../components/misc/seo";
import Layout from "../components/structure/layout";

const SecondPage: FunctionComponent = () => (
  <Layout>
    <SEO title="Page two" />
    <h1>Hi from the second page</h1>
    <p>Welcome to page 2</p>
    <Link to="/">Go back to the homepage</Link>
  </Layout>
);

export default SecondPage;
