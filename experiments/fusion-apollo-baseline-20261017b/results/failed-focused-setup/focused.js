import http from 'k6/http';
import { check } from 'k6';
import { Rate } from 'k6/metrics';
const payload = open(__ENV.WORKLOAD_PATH);
const expected = JSON.parse(open(__ENV.ORACLE_PATH));
const success = new Rate('success_rate');
const vus = parseInt(__ENV.BENCH_VUS || '50', 10);
export const options = { vus, duration: __ENV.BENCH_OVER_TIME || '20s', summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.9)'] };
function send() { return http.post(__ENV.GATEWAY_ENDPOINT, payload, {headers: {'Content-Type':'application/json', Authorization:`Bearer bench-${__VU}-${__ITER}`}}); }
export function setup() { for (let i=0;i<vus*2;i++) { send(); } }
function same(a,b) {
    if (a === b) { return true; }
    if (a === null || b === null || typeof a !== 'object' || typeof b !== 'object') { return false; }
    const ak = Object.keys(a), bk = Object.keys(b);
    return ak.length === bk.length && ak.every(k => Object.prototype.hasOwnProperty.call(b,k) && same(a[k],b[k]));
}
let checked = false;
export default function() {
    const response=send();
    const valid=check(response, {'status 200': r=>r.status===200, 'no graphql errors': r=>!r.body.includes('"errors"'), 'oracle':r=>{if(checked){return true;}checked=true;return same(r.json(), expected);}});
    success.add(valid);
}
export function handleSummary(data) { return { [__ENV.SUMMARY_PATH+'/k6_summary.json']: JSON.stringify(data) }; }
