import { spawn } from "node:child_process";
import http from "node:http";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const API_ROOT = "http://127.0.0.1:8000/";

function get(url) {
  return new Promise((resolve) => {
    const req = http.get(url, (res) => {
      let data = "";
      res.on("data", (chunk) => {
        data += chunk;
      });
      res.on("end", () => resolve({ status: res.statusCode, data }));
    });
    req.on("error", () => resolve(null));
    req.setTimeout(2000, () => {
      req.destroy();
      resolve(null);
    });
  });
}

function isPhonebook(payload) {
  try {
    const body = JSON.parse(payload);
    return body.message === "Phonebook API is running";
  } catch {
    return typeof payload === "string" && payload.includes("Phonebook API is running");
  }
}

function keepAlive() {
  setInterval(() => {}, 1 << 30);
}

const existing = await get(API_ROOT);

if (existing && isPhonebook(existing.data)) {
  console.log("Phonebook API is already running on port 8000.");
  keepAlive();
} else if (existing) {
  console.error(
    [
      "Port 8000 is already in use by another application, not the Phonebook Java API.",
      "Stop that process and rerun the tests.",
      `Response: ${String(existing.data).slice(0, 200)}`,
    ].join("\n"),
  );
  process.exit(1);
} else {
  console.log("Starting Phonebook PostgreSQL and Java backend with Docker Compose...");
  const child = spawn("docker", ["compose", "up", "--build", "db", "backend"], {
    cwd: root,
    stdio: "inherit",
    shell: true,
  });
  child.on("exit", (code) => process.exit(code ?? 1));
}
