interface Workshop {
  readonly id: string;
  readonly title: string;
  readonly teaser: string;
  readonly date: string;
  readonly host: string;
  readonly place: string;
  readonly url: string;
  readonly image: string;
  readonly hero: boolean;
  readonly banner: boolean;
  readonly active: boolean;
}

export type WorkshopsState = Workshop[];

export const initialState: WorkshopsState = [
  {
    id: "ndc-oslo-2023",
    title: "Fullstack GraphQL",
    teaser:
      "Learn to build modern APIs like Facebook and Netflix in our Fullstack GraphQL workshop.",
    date: "22 - 23 May 2023",
    host: "NDC",
    place: "{ Oslo }",
    url: "https://ndcoslo.com/workshops/building-modern-applications-with-graphql-using-asp-net-core-6-hot-chocolate-and-relay/cb7ce0173d8f",
    image: "ndc-oslo",
    hero: true,
    banner: false,
    active: isActive("2023-05-22"),
  },
  {
    id: "ndc-copenhagen-2023",
    title: "Fullstack GraphQL",
    teaser:
      "Learn to build modern APIs like Facebook and Netflix in our Fullstack GraphQL workshop.",
    date: "28 - 29 August 2023",
    host: "NDC",
    place: "{ Copenhagen }",
    url: "https://cphdevfest.com/workshops/building-modern-applications-with-graphql-using-asp-net-core-6-hot-chocolate-and-relay/b7d68a9db642",
    image: "ndc-copenhagen",
    hero: true,
    banner: false,
    active: isActive("2023-08-28"),
  },
  {
    id: "online-230907",
    title: "Fullstack GraphQL",
    teaser:
      "Learn to build modern APIs like Facebook and Netflix in our Fullstack GraphQL workshop.",
    date: "7 - 8 September 2023",
    host: "ONLINE",
    place: "9AM-5PM CEST",
    url: "https://www.eventbrite.com/e/fullstack-graphql-europe-cest-tickets-633258783067",
    image: "online",
    hero: false,
    banner: true,
    active: isActive("2023-09-07"),
  },
];

function isActive(until: string) {
  return new Date() < new Date(until);
}
