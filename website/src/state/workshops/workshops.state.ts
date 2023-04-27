interface Workshop {
  readonly id: string;
  readonly title: string;
  readonly teaser: string;
  readonly date: string;
  readonly host: string;
  readonly place: string;
  readonly url: string;
  readonly promo: boolean;
}

export type WorkshopsState = Workshop[];

export const initialState: WorkshopsState = [
  {
    id: "ONLINE",
    title: "Fullstack GraphQL",
    teaser:
      "Learn to build modern APIs like Facebook and Netflix in our Fullstack GraphQL workshop.",
    date: "10 - 11 May 2023",
    host: "ONLINE",
    place: "9AM-5PM CDT",
    url: "https://www.eventbrite.com/e/fullstack-graphql-tickets-583856048157",
    promo: false,
  },
  {
    id: "NDC_OSLO",
    title: "Fullstack GraphQL",
    teaser:
      "Learn to build modern APIs like Facebook and Netflix in our Fullstack GraphQL workshop.",
    date: "22 - 23 May 2023",
    host: "NDC",
    place: "{ Oslo }",
    url: "https://ndcoslo.com/workshops/building-modern-applications-with-graphql-using-asp-net-core-6-hot-chocolate-and-relay/cb7ce0173d8f",
    promo: true,
  },
  {
    id: "NDC_COPENHAGEN",
    title: "Fullstack GraphQL",
    teaser:
      "Learn to build modern APIs like Facebook and Netflix in our Fullstack GraphQL workshop.",
    date: "28 - 29 August 2023",
    host: "NDC",
    place: "{ Copenhagen }",
    url: "https://cphdevfest.com/workshops/building-modern-applications-with-graphql-using-asp-net-core-6-hot-chocolate-and-relay/b7d68a9db642",
    promo: true,
  },
];
