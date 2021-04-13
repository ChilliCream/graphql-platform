import { startServer } from "graphql-language-service-server";

const start = async () => {
  await startServer({
    method: "stream",
  });
};

start();
