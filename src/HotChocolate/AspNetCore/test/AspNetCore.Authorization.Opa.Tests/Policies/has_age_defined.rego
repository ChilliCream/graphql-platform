package graphql.authz.has_age_defined

import input.request

default allow = false

input["token"] = replace(request.headers["Authorization"], "Bearer ", "")

claims := io.jwt.decode(input.token)[1]

allow {
  claims.birthdate
}
