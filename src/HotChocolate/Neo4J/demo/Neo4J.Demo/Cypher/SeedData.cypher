CREATE (johnSmith:User {name:"John Smith"})
CREATE (janeDoe:User {name:"Jane Doe"})
CREATE (alexHartman:User {name: "Alex Hartman"})
CREATE (johnFoster:User {name: "John Foster"})
CREATE (kenMaxwell:User {name: "Ken Maxwell"})
CREATE (maxPowers:User {name: "Max Powers"})

CREATE (tileCompany:Business {name: "Tile Company", city: "Rockville", state: "MD"})
CREATE (softwareCompany:Business {name: "Software Company", city: "Seattle", state: "WA"})
CREATE (drillingCompany:Business {name: "Drilling Company", city: "Madison", state: "WI"})
CREATE (financeCompany:Business {name: "Finance Company", city: "New York", state: "NY"})
CREATE (movieCompany:Business {name: "Movie Company", city: "Los Angeles", state: "CA"})

CREATE (tileCompanyReview1:Review {rating: 1.5, text: "Terrible, did not do it the job right!"})
CREATE (tileCompanyReview2:Review {rating: 4, text: "Great company, good leadership"})
CREATE (softwareCompanyReview1:Review {rating: 1.5, text: "Terrible, did not do it the job right!"})
CREATE (softwareCompanyReview2:Review {rating: 4, text: "Great company, good leadership"})
CREATE (movieCompanyReview1:Review {rating: 1.5, text: "Terrible, did not do it the job right!"})
CREATE (movieCompanyReview2:Review {rating: 4, text: "Great company, good leadership"})

CREATE
  (johnSmith)-[:WROTE]->(tileCompanyReview1)-[:REVIEWS]->(tileCompany),
  (janeDoe)-[:WROTE]->(tileCompanyReview2)-[:REVIEWS]->(tileCompany),
  (alexHartman)-[:WROTE]->(softwareCompanyReview1)-[:REVIEWS]->(softwareCompany),
  (johnFoster)-[:WROTE]->(softwareCompanyReview2)-[:REVIEWS]->(softwareCompany),
  (kenMaxwell)-[:WROTE]->(movieCompanyReview1)-[:REVIEWS]->(movieCompany),
  (maxPowers)-[:WROTE]->(movieCompanyReview2)-[:REVIEWS]->(movieCompany)
