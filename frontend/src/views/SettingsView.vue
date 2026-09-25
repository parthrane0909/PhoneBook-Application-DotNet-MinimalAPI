<template>
  <section class="page-section settings-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">WORKSPACE</p>
        <h1>Settings</h1>
        <p class="page-subtitle">Tune the workspace to match the way you work.</p>
      </div>
    </header>

    <div class="settings-list">
      <label class="setting-row">
        <span><strong>Appearance</strong><small>Choose how Phonebook looks.</small></span>
        <select v-model="preferences.theme" @change="save">
          <option value="system">System</option>
          <option value="light">Light</option>
          <option value="dark">Dark</option>
        </select>
      </label>
      <label class="setting-row">
        <span><strong>Contacts per page</strong><small>Control how many rows appear in the table.</small></span>
        <select v-model.number="preferences.limit" @change="save">
          <option :value="10">10</option>
          <option :value="25">25</option>
          <option :value="50">50</option>
        </select>
      </label>
      <label class="setting-row setting-toggle">
        <span><strong>Confirm before deleting</strong><small>Ask before removing a contact.</small></span>
        <input v-model="preferences.confirmDelete" type="checkbox" @change="save" />
      </label>
      <label class="setting-row setting-toggle">
        <span><strong>Keyboard shortcuts</strong><small>Enable quick actions such as / and n.</small></span>
        <input v-model="preferences.shortcuts" type="checkbox" @change="save" />
      </label>
    </div>
  </section>
</template>

<script setup>
import { onMounted, reactive } from "vue";
import { applyTheme } from "../utils/theme";
import { useContactsStore } from "../stores/contacts";

const defaults = { theme: "system", limit: 10, confirmDelete: true, shortcuts: true };
const preferences = reactive({ ...defaults, ...JSON.parse(localStorage.getItem("phonebook-preferences") || "{}") });
const store = useContactsStore();

function save() {
  localStorage.setItem("phonebook-preferences", JSON.stringify(preferences));
  localStorage.setItem("phonebook-limit", String(preferences.limit));
  store.limit = preferences.limit;
  store.page = 1;
  applyTheme(preferences.theme);
}

onMounted(() => applyTheme(preferences.theme));
</script>
