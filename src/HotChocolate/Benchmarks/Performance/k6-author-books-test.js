import http from 'k6/http';
import {check, group, sleep, fail } from 'k6';
import { Counter, Trend } from 'k6/metrics';
import { parseHTML } from 'k6/html';

// process arguments
var TEST_2_RUN = 'smoke'; // default
if (__ENV.TEST_2_RUN){
    TEST_2_RUN =  __ENV.TEST_2_RUN;
}

var ENV_2_RUN = 'dev'; // default
if (__ENV.ENV_2_RUN){
	ENV_2_RUN =  __ENV.ENV_2_RUN;
}

const BASE_URL = 'http://52.152.58.75/graphql/';
const DEBUG = true;
const start = Date.now();

// Custom metrics 
// Counters
var BasicAuthorQuery_Counter = new Counter('BasicbookQuery_Counter');
var BasicAuthorQuerySuccess_Counter = new Counter('BasicbookQuerySuccess_Counter');
var BasicbookQueryFail_Counter = new Counter('BasicbookQueryFail_Counter');
// Trends
var BasicAuthorQuery_Trend = new Trend('BasicAuthorQuery_Trend');

const ExecutionType = {
    load:   'load',
    smoke:  'smoke',
    stress: 'stress',
    soak:   'soak'
  }

  var Execution = TEST_2_RUN; // ExecutionType.smoke;
  var ExecutionOptions_Scenarios;

  switch(Execution){
	case ExecutionType.smoke:
		ExecutionOptions_Scenarios = {
			SimpleQuery_scenario: {
                exec: 'SimpleQueryTest',
                executor: 'ramping-arrival-rate',
                startTime: '0s',
                startRate: 1,
                preAllocatedVUs: 4,
                stages: [
                { duration: '10s', target: 1},
                { duration: '10s', target: 2}
                ]
            }, 
        }; 
        break; // end case ExecutionType.smoke
    case ExecutionType.stress:        
        ExecutionOptions_Scenarios = {
            SimpleQuery_scenario: {
                exec: 'SimpleQueryTest',
                executor: 'ramping-arrival-rate',
                startTime: '0s',
                startRate: 1,
                preAllocatedVUs: 200, //1600,
                stages: [
                { duration: '1m', target: 50},
                { duration: '1m', target: 100} //,
                //{ duration: '1m', target: 200},
                //{ duration: '1m', target: 400},
                //{ duration: '1m', target: 800},
                ]
            }, 
        }; 
        break; // end case ExecutionType.smoke        

    default:
        DebugOrLog(`no test to run was selected or could be determined...  - Execution: ${Execution} `);
        break;          
  }        


export let options ={
    scenarios: ExecutionOptions_Scenarios,
    thresholds: {
        http_req_failed: ['rate<0.05'],   
        'http_req_duration': ['p(95)<500', 'p(99)<1500'],
        'http_req_duration': ['avg<400'],  
    }
};  

function formatDate(date) {
    var hours = date.getHours();
    var minutes = date.getMinutes();
    var ampm = hours >= 12 ? 'pm' : 'am';
    hours = hours % 12;
    hours = hours ? hours : 12; // the hour '0' should be '12'
    minutes = minutes < 10 ? '0'+ minutes : minutes;
    var strTime = hours + ':' + minutes + ' ' + ampm;
    return (date.getMonth()+1) + "/" + date.getDate() + "/" + date.getFullYear() + "  " + strTime;
}

function DebugOrLog(textToLog){
    if (DEBUG){
        var millis = Date.now() - start; // we get the ms ellapsed from the start of the test
        var time = Math.floor(millis / 1000); // in seconds
        // console.log(`${time}se: ${textToLog}`); // se = Seconds elapsed
        console.log(`${textToLog}`); 
    }
}

// Testing the backend just with reads
export function SimpleQueryTest(authToken){
    let headers = {
        // Authorization: `Bearer ${authToken}`, // This GraphQL server does not require authentication so we pass no token in the header
        'Content-Type': 'application/json',
      };

    let Query_allallAuthorsAndBooks = ` 
    query {
        authors {
          id,
          company,
          name,
          books {
            id,
            name,
            pages
          }
        }
      }`;
    
    let res = http.post(
        BASE_URL, 
        JSON.stringify({ query: Query_allallAuthorsAndBooks }), 
        { headers: headers }
      );  
    BasicAuthorQuery_Counter.add(1); // this are the post counts.

    const isSuccessfulRequest = check(res, {
        "HTML post succeed": () => res.status == 200
    });

    if (isSuccessfulRequest) {
        let body = JSON.parse(res.body);
        let errors = body.errors;
        let GraphQLerrors = false; 
        

        if (errors){
            BasicAuthorQueryFail_Counter.add(1);
            DebugOrLog(`Found a GraphQL Error: ${errors[0].message}`); //Could be more than one, should iterate through them...
            GraphQLerrors = true; 
        }
        
        const hasNoGraphQLErrors = check(body, {
            "GraphQL request succeed": () => GraphQLerrors == false
        });	
        
        if (hasNoGraphQLErrors){
            // Update counters and trends
            BasicAuthorQuery_Trend.add(res.timings.duration);
            BasicAuthorQuerySuccess_Counter.add(1);

            // DebugOrLog(`The response of the GraphQL API is:${res.body}`);    
            let numAuthors = Object.keys(body.data.authors).length;         
            // DebugOrLog(`The number of authors retrieved is is:${numAuthors}`);    
        }
        else {
            BasicAuthorQueryFail_Counter.add(1);
        }  
    }
    else	
    {
      DebugOrLog(`The http.Post failed!!!`);
    } 

    sleep(1);
}   


// setup configuration
export function setup() {
    DebugOrLog(`== SETUP BEGIN ===========================================================`)
    // log the date & time start of the test
    DebugOrLog(`Start of test: ${formatDate(new Date())}`);

    // log the test type
    DebugOrLog(`Test executed: ${Execution}`);

    // log the endpoint
    DebugOrLog(`Endpoint to hit: ${BASE_URL}`)

    // Log the environment
    DebugOrLog(`This test will run on the ${ENV_2_RUN} environment.`);

    DebugOrLog(`== SETUP END ===========================================================`);
    let authToken = "Nothing here ;)";
    return authToken; // this will be passed as parameter to all the exported functions
}