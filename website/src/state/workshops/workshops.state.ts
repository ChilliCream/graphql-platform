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
    id: "online-231106",
    title: "Fullstack GraphQL from Vienna",
    teaser:
      "Learn to build modern APIs like Facebook and Netflix in our Fullstack GraphQL workshop.",
    date: "6 - 7 November 2023",
    host: "ONLINE",
    place: "9AM-5PM CET",
    url: "https://www.eventbrite.com/e/fullstack-graphql-vienna-tickets-734869182507",
    image: "online",
    hero: true,
    banner: true,
    active: isActive("2023-11-06"),
  },
];

function isActive(until: string) {
  return new Date() < new Date(until);
}
