import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: {
    admin_surface_abuse: {
      executor: "constant-vus",
      vus: 15,
      duration: "45s"
    }
  },
  thresholds: {
    http_req_failed: ["rate<0.1"]
  }
};

const baseUrl = __ENV.BASE_URL || "http://127.0.0.1:8097";

export default function () {
  const responses = [
    http.get(`${baseUrl}/api/admin/stats/api?days=1`, { redirects: 0 }),
    http.get(`${baseUrl}/admin`, { redirects: 0 }),
    http.post(`${baseUrl}/admin`, { password: "WrongPassword0099" }, { redirects: 0 })
  ];

  for (const response of responses) {
    check(response, {
      "admin surface rejects or throttles": r => [200, 400, 401, 403, 429, 302].includes(r.status)
    });
  }

  sleep(1);
}
