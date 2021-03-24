CREATE (johnSmith:User {Name:"John Smith"})
CREATE (janeDoe:User {Name:"Jane Doe"})
CREATE (alexHartman:User {Name: "Alex Hartman"})
CREATE (johnFoster:User {Name: "John Foster"})
CREATE (kenMaxwell:User {Name: "Ken Maxwell"})
CREATE (maxPowers:User {Name: "Max Powers"})

CREATE (tileCompany:Business {Name: "Tile Company", City: "Rockville", State: "MD"})
CREATE (softwareCompany:Business {Name: "Software Company", City: "Seattle", State: "WA"})
CREATE (drillingCompany:Business {Name: "Drilling Company", City: "Madison", State: "WI"})
CREATE (financeCompany:Business {Name: "Finance Company", City: "New York", State: "NY"})
CREATE (movieCompany:Business {Name: "Movie Company", City: "Los Angeles", State: "CA"})

CREATE (tileCompanyReview1:Review {Rating: 1.5, Text: "Terrible, did not do it the job right!"})
CREATE (tileCompanyReview2:Review {Rating: 4, Text: "Great company, good leadership"})
CREATE (softwareCompanyReview1:Review {Rating: 1.5, Text: "Terrible, did not do it the job right!"})
CREATE (softwareCompanyReview2:Review {Rating: 4, Text: "Great company, good leadership"})
CREATE (movieCompanyReview1:Review {Rating: 1.5, Text: "Terrible, did not do it the job right!"})
CREATE (movieCompanyReview2:Review {Rating: 4, Text: "Great company, good leadership"})

CREATE
  (johnSmith)-[:WROTE]->(tileCompanyReview1)-[:REVIEWS]->(tileCompany),
  (janeDoe)-[:WROTE]->(tileCompanyReview2)-[:REVIEWS]->(tileCompany),
  (alexHartman)-[:WROTE]->(softwareCompanyReview1)-[:REVIEWS]->(softwareCompany),
  (johnFoster)-[:WROTE]->(softwareCompanyReview2)-[:REVIEWS]->(softwareCompany),
  (kenMaxwell)-[:WROTE]->(movieCompanyReview1)-[:REVIEWS]->(movieCompany),
  (maxPowers)-[:WROTE]->(movieCompanyReview2)-[:REVIEWS]->(movieCompany)
