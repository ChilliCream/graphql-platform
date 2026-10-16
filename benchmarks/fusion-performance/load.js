import http from 'k6/http';
import { Counter, Trend, Rate } from 'k6/metrics';
import { fail } from 'k6';

const payload = open(__ENV.REQUEST);
const completed = new Counter('completed');
const latency = new Trend('latency_ms', true);
const bytes = new Trend('body_bytes');
const success = new Rate('success');
const base = 'http://127.0.0.1:5320';
const headers = {'Content-Type':'application/json', Accept:__ENV.DEFER === '1' ? 'multipart/mixed; incrementalSpec=v0.2' : 'application/graphql-response+json'};
export const options = {
  scenarios: {load: __ENV.RATE ?
    {executor:'constant-arrival-rate', rate:Number(__ENV.RATE),timeUnit:__ENV.TIME_UNIT || '1s',preAllocatedVUs:64,maxVUs:64,duration:__ENV.DURATION || '60s',gracefulStop:'120s'} :
    {executor:'constant-vus', vus:Number(__ENV.VUS || 1), duration:__ENV.DURATION || '60s', gracefulStop:'120s'}},
  summaryTrendStats:['avg','min','med','max','p(50)','p(95)','p(99)'],
  thresholds:{success:['rate==1'],http_req_failed:['rate==0']},
};
function send() { return http.post(base+'/graphql',payload,{headers,timeout:'120s'}); }
export function setup() {
  const r = send();
  if (r.status !== 200 || r.body.includes('"errors":')) { fail('Invalid response: '+r.body.slice(0,1000)); }
  return http.get(base+'/perf/state').json();
}
export default function() {
  const r=send();
  completed.add(1);
  latency.add(r.timings.duration);
  bytes.add(r.body.length);
  success.add(r.status===200 && !r.body.includes('"errors":') && (__ENV.DEFER !== '1' || r.body.includes('"hasNext":false')));
}
export function teardown(before) {
  const after=http.get(base+'/perf/state').json();
  console.log('ACCOUNTING '+JSON.stringify({before,after}));
}
export function handleSummary(data) {
  return {[__ENV.SUMMARY]:JSON.stringify(data,null,2),stdout:JSON.stringify(data.metrics)+'\n'};
}
