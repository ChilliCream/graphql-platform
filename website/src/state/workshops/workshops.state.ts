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
  readonly self: boolean;
}

export type WorkshopsState = Workshop[];

export const initialState: WorkshopsState = [
  {
    id: "dometrain",
    title: "Getting Started with GraphQL in .NET",
    teaser:
      "Learn to build modern APIs like those used by Facebook and Netflix in our self-paced getting started course on Dometrain.",
    date: "",
    host: "Dometrain",
    place: "",
    url: "https://courses.chillicream.com",
    image: "online",
    hero: true,
    banner: true,
    active: true,
    self: true,
  },
  {
    id: "online-231106",
    title: "Fullstack GraphQL",
    teaser:
      "Learn to build modern APIs like Facebook and Netflix in our Fullstack GraphQL workshop.",
    date: "18 - 19 June 2024",
    host: "ONLINE",
    place: "9AM-5PM CEST",
    url: "https://www.eventbrite.com/e/2-day-fullstack-graphql-workshop-europe-cest-tickets-896304309317",
    image: "online",
    hero: true,
    banner: true,
    active: isActive("2024-06-18"),
    self: false,
  },
  {
    id: "online-241022",
    title: "Enterprise GraphQL with DDD, CQRS and Clean Architecture",
    teaser:
      "We dive deep into the world of GraphQL, Domain-Driven Design (DDD), Command Query Responsibility Segregation (CQRS), and Clean Architecture? This online event is perfect for developers looking to level up their skills and learn how to implement these cutting-edge techniques in enterprise applications.",
    date: "22 October 2024",
    host: "ONLINE",
    place: "9AM-5PM CDL",
    url: "https://www.eventbrite.com/e/901548685387",
    image: "online",
    hero: true,
    banner: true,
    active: isActive("2024-10-22"),
    self: false,
  },
];

function isActive(until: string) {
  return new Date() < new Date(until);
}
