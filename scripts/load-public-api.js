import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: {
    public_read_burst: {
      executor: "ramping-vus",
      startVUs: 5,
      stages: [
        { duration: "30s", target: 20 },
        { duration: "45s", target: 40 },
        { duration: "30s", target: 0 }
      ]
    }
  },
  thresholds: {
    http_req_failed: ["rate<0.02"],
    http_req_duration: ["p(95)<900"]
  }
};

const baseUrl = __ENV.BASE_URL || "http://127.0.0.1:8097";

export default function () {
  const responses = [
    http.get(`${baseUrl}/api/v1/items/list`),
    http.get(`${baseUrl}/api/v1/creatures/list`),
    http.get(`${baseUrl}/api/v1/hunting-places/list`),
    http.get(`${baseUrl}/api/v1/categories/list`)
  ];

  for (const response of responses) {
    check(response, {
      "status is 200 or 429": r => r.status === 200 || r.status === 429
    });
  }

  sleep(1);
}
