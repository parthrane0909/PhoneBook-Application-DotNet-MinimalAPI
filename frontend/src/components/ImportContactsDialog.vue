<template>
  <div class="modal-backdrop" @click.self="close">
    <div
      class="contact-form import-dialog"
      role="dialog"
      aria-modal="true"
      aria-labelledby="import-title"
    >
      <div class="form-header">
        <div>
          <p class="eyebrow">CONTACTS</p>
          <h2 id="import-title">Import contacts</h2>
        </div>

        <button
          class="close-button"
          type="button"
          aria-label="Close import dialog"
          @click="close"
        >
          ×
        </button>
      </div>

      <p class="page-subtitle import-help">
        Import contacts from CSV or Excel. Supported columns are name,
        phone_number, email, address, and tags. Other columns are ignored.
      </p>

      <input
        ref="fileInput"
        class="file-input-hidden"
        type="file"
        accept=".csv,.xlsx,.xls,text/csv,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/vnd.ms-excel"
        @change="readFile"
      />

      <button
        type="button"
        class="file-dropzone"
        :class="{ 'is-dragging': isDragging, 'has-file': !!fileName }"
        @click="openFilePicker"
        @dragover.prevent="isDragging = true"
        @dragleave.prevent="isDragging = false"
        @drop.prevent="handleDrop"
      >
        <span class="file-dropzone-icon" aria-hidden="true">↑</span>
        <strong>{{ fileName || "Drop your file here" }}</strong>
        <span v-if="!fileName">or click to browse</span>
        <span v-else>Click to choose a different file</span>
        <small>CSV, XLSX, or XLS</small>
      </button>

      <div v-if="parseError" class="server-error">
        {{ parseError }}
      </div>

      <div v-if="rows.length" class="import-summary">
        <strong>{{ validRows.length }} ready to import</strong>
        <span>
          {{ errors.length }} row{{ errors.length === 1 ? "" : "s" }} need attention
        </span>
      </div>

      <div v-if="rows.length" class="import-preview">
        <div class="import-preview-header">
          <strong>Preview</strong>
          <span>Showing first {{ Math.min(validRows.length, 5) }} rows</span>
        </div>

        <div class="import-preview-table-wrap">
          <table class="import-preview-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Phone</th>
                <th>Email</th>
                <th>Tags</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in validRows.slice(0, 5)" :key="`${row.phone_number}-${row.email || ''}`">
                <td>{{ row.name }}</td>
                <td>{{ row.phone_number }}</td>
                <td>{{ row.email || "-" }}</td>
                <td>{{ row.tags.length ? row.tags.join(", ") : "-" }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <ul v-if="errors.length" class="import-errors">
        <li
          v-for="error in errors.slice(0, 8)"
          :key="`${error.row}-${error.reason}`"
        >
          Row {{ error.row }}: {{ error.reason }}
        </li>
        <li v-if="errors.length > 8">
          and {{ errors.length - 8 }} more...
        </li>
      </ul>

      <div class="form-actions">
        <button
          class="cancel-button"
          type="button"
          :disabled="saving"
          @click="close"
        >
          Cancel
        </button>

        <button
          class="save-button"
          type="button"
          :disabled="!validRows.length || saving"
          @click="submit"
        >
          {{ saving ? "Importing..." : `Import ${validRows.length || "contacts"}` }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref } from "vue";
import * as XLSX from "xlsx";
import { useContactsStore } from "../stores/contacts";

const emit = defineEmits(["close", "imported"]);
const store = useContactsStore();

const fileInput = ref(null);
const fileName = ref("");
const rows = ref([]);
const errors = ref([]);
const parseError = ref("");
const saving = ref(false);
const isDragging = ref(false);

const supportedHeaders = new Set([
  "name",
  "phone_number",
  "email",
  "address",
  "tags",
]);

const phonePattern = /^\+?[0-9\s()\-]{7,20}$/;

const validRows = computed(() => rows.value);

function close() {
  if (!saving.value) emit("close");
}

function openFilePicker() {
  if (!saving.value) fileInput.value?.click();
}

function handleDrop(event) {
  isDragging.value = false;
  if (saving.value) return;

  const file = event.dataTransfer?.files?.[0];
  if (file) processFile(file);
}

function readFile(event) {
  const file = event.target.files?.[0];
  if (file) processFile(file);
  event.target.value = "";
}

function processFile(file) {
  fileName.value = file.name;
  parseError.value = "";
  rows.value = [];
  errors.value = [];

  const extension = file.name.split(".").pop()?.toLowerCase();

  if (!["csv", "xlsx", "xls"].includes(extension)) {
    parseError.value = "Please choose a CSV or Excel file.";
    return;
  }

  if (file.size > 5 * 1024 * 1024) {
    parseError.value = "The file is too large. Please use a file smaller than 5 MB.";
    return;
  }

  if (extension === "csv") {
    const reader = new FileReader();
    reader.onload = () => validateRows(parseCsv(String(reader.result || "")));
    reader.onerror = () => {
      parseError.value = "Unable to read this file.";
    };
    reader.readAsText(file);
    return;
  }

  const reader = new FileReader();
  reader.onload = () => {
    try {
      const workbook = XLSX.read(reader.result, { type: "array" });
      const firstSheet = workbook.Sheets[workbook.SheetNames[0]];

      if (!firstSheet) {
        parseError.value = "The Excel file does not contain a worksheet.";
        return;
      }

      const records = XLSX.utils.sheet_to_json(firstSheet, {
        header: 1,
        defval: "",
        raw: false,
      });

      validateRows(records);
    } catch (error) {
      console.error(error);
      parseError.value = "Unable to read this Excel file.";
    }
  };
  reader.onerror = () => {
    parseError.value = "Unable to read this file.";
  };
  reader.readAsArrayBuffer(file);
}

function parseCsv(text) {
  const records = [];
  let record = [];
  let field = "";
  let quoted = false;

  for (let index = 0; index < text.length; index += 1) {
    const character = text[index];
    const next = text[index + 1];

    if (character === '"' && quoted && next === '"') {
      field += '"';
      index += 1;
    } else if (character === '"') {
      quoted = !quoted;
    } else if (character === "," && !quoted) {
      record.push(field);
      field = "";
    } else if ((character === "\n" || character === "\r") && !quoted) {
      if (character === "\r" && next === "\n") index += 1;
      record.push(field);
      if (record.some((value) => String(value).trim())) records.push(record);
      record = [];
      field = "";
    } else {
      field += character;
    }
  }

  if (field || record.length) {
    record.push(field);
    if (record.some((value) => String(value).trim())) records.push(record);
  }

  return records;
}

function normalizeHeader(header) {
  return String(header || "")
    .trim()
    .toLowerCase()
    .replace(/^\uFEFF/, "");
}

function parseTags(value) {
  const names = String(value || "")
    .split(/[,;|]/)
    .map((tag) => tag.trim())
    .filter(Boolean);

  const unique = [];
  const seen = new Set();

  for (const tag of names) {
    const key = tag.toLowerCase();
    if (seen.has(key)) continue;
    seen.add(key);
    unique.push(tag);
  }

  return unique;
}

function validateRows(records) {
  parseError.value = "";

  if (!records.length || records.length < 2) {
    parseError.value = "The file must contain a header row and at least one contact.";
    return;
  }

  const headers = records[0].map(normalizeHeader);

  if (!headers.includes("name") || !headers.includes("phone_number")) {
    parseError.value = "Headers must include name and phone_number.";
    return;
  }

  const valid = [];
  const invalid = [];
  const phoneNumbers = new Set();
  const emails = new Set();

  records.slice(1).forEach((record, index) => {
    const rowNumber = index + 2;
    const values = {};

    headers.forEach((header, valueIndex) => {
      if (supportedHeaders.has(header)) {
        values[header] = String(record[valueIndex] ?? "").trim();
      }
    });

    const name = values.name || "";
    const phoneNumber = values.phone_number || "";
    const email = values.email || null;
    const address = values.address || null;
    const tags = parseTags(values.tags);

    let reason = "";

    if (!name) {
      reason = "Name is required";
    } else if (name.length > 255) {
      reason = "Name must be 255 characters or fewer";
    } else if (!phoneNumber) {
      reason = "Phone number is required";
    } else if (!phonePattern.test(phoneNumber)) {
      reason = "Invalid phone number";
    } else {
      const digitCount = phoneNumber.replace(/\D/g, "").length;
      if (digitCount < 7 || digitCount > 15) {
        reason = "Phone number must contain 7–15 digits";
      }
    }

    if (!reason && email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      reason = "Invalid email";
    }

    if (!reason && phoneNumbers.has(phoneNumber)) {
      reason = "Duplicate phone number in file";
    }

    if (!reason && email && emails.has(email.toLowerCase())) {
      reason = "Duplicate email in file";
    }

    if (!reason && tags.length > 10) {
      reason = "A contact can have at most 10 tags";
    }

    if (!reason && tags.some((tag) => tag.length > 80)) {
      reason = "Each tag must be 80 characters or fewer";
    }

    if (reason) {
      invalid.push({ row: rowNumber, reason });
      return;
    }

    phoneNumbers.add(phoneNumber);
    if (email) emails.add(email.toLowerCase());

    valid.push({
      name,
      phone_number: phoneNumber,
      email,
      address,
      tags,
    });
  });

  rows.value = valid;
  errors.value = invalid;
}

async function submit() {
  if (!validRows.value.length || saving.value) return;

  saving.value = true;
  parseError.value = "";

  try {
    const result = await store.importContacts(validRows.value);
    emit("imported", result);
  } catch (error) {
    parseError.value =
      error.response?.data?.detail || "Unable to import contacts.";
  } finally {
    saving.value = false;
  }
}
</script>