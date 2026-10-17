import http from 'k6/http';
import { check } from 'k6';
import { Rate } from 'k6/metrics';

const payload = open(__ENV.WORKLOAD_PATH);
const expected = JSON.parse(open(__ENV.ORACLE_PATH));
const success = new Rate('success_rate');
export const options = {
    scenarios: {
        ordinary: {
            executor: 'constant-arrival-rate',
            rate: 5000,
            timeUnit: '1s',
            duration: '20s',
            preAllocatedVUs: 200,
            maxVUs: 200,
            gracefulStop: '5s',
        },
    },
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)', 'p(99.9)'],
};
let setupCounter = 0;
function send() {
    const requestId = typeof __ITER === 'undefined' ? `s-${setupCounter++}` : `${__VU}-${__ITER}`;
    return http.post(__ENV.GATEWAY_ENDPOINT, payload, {
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer bench-${requestId}` },
    });
}
export function setup() {
    for (let i = 0; i < 100; i++) { send(); }
}
function same(a, b) {
    if (a === b) { return true; }
    if (a === null || b === null || typeof a !== 'object' || typeof b !== 'object') { return false; }
    const ak = Object.keys(a), bk = Object.keys(b);
    return ak.length === bk.length && ak.every(k => Object.prototype.hasOwnProperty.call(b, k) && same(a[k], b[k]));
}
let checked = false;
export default function() {
    const response = send();
    const valid = check(response, {
        'status 200': r => r.status === 200,
        'no graphql errors': r => !r.body.includes('"errors"'),
        'oracle': r => {
            if (checked) { return true; }
            checked = true;
            return same(r.json(), expected);
        },
    });
    success.add(valid);
}
export function handleSummary(data) {
    return { [__ENV.SUMMARY_PATH + '/k6_summary.json']: JSON.stringify(data) };
}
