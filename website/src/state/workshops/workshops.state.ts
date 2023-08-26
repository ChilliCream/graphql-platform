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
];

function isActive(until: string) {
  return new Date() < new Date(until);
}
