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
    id: "online-231010",
    title: "Fullstack GraphQL",
    teaser:
      "Learn to build modern APIs like Facebook and Netflix in our Fullstack GraphQL workshop.",
    date: "10 - 11 October 2023",
    host: "ONLINE",
    place: "9AM-5PM CDT",
    url: "https://www.eventbrite.com/e/fullstack-graphql-usa-mountain-time-tickets-710559090367",
    image: "online",
    hero: false,
    banner: true,
    active: isActive("2023-10-10"),
  },
];

function isActive(until: string) {
  return new Date() < new Date(until);
}
