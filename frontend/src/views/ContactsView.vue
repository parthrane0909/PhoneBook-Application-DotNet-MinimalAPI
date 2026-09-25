<template>
  <section class="page-section">
    <header class="page-header">
      <div>
        <p class="eyebrow">CONTACTS / {{ sectionLabel }}</p>
        <h1>{{ sectionLabel }}</h1>
        <p class="page-subtitle">A clear, current view of the people in your circle.</p>
      </div>

      <div class="header-actions">
        <button class="secondary-button" type="button" @click="showImport = true">
          Import
        </button>
        <button class="secondary-button" type="button" @click="exportContacts">
          Export
        </button>
        <button class="primary-button" type="button" @click="showCreateForm = true">
          <span>+</span> Add contact
        </button>
      </div>
    </header>

    <div class="summary-grid">
      <div class="summary-card accent-card">
        <span>{{ resultCardTitle }}</span>
        <strong>{{ store.total }}</strong>
        <small>{{ resultCardSubtitle }}</small>
      </div>

      <div class="summary-card">
        <span>Favorites</span>
        <strong>{{ store.metrics.favorites }}</strong>
        <small>Worth keeping close</small>
      </div>

      <div class="summary-card">
        <span>Recently added</span>
        <strong>{{ store.metrics.recently_added }}</strong>
        <small>Added in the last 7 days</small>
      </div>

      <div class="summary-card">
        <span class="metric-label">Tagged contacts</span>
        <strong>{{ taggedContactsCount }}</strong>
        <span class="metric-description">Contacts organized with tags</span>
        </div>
    </div>

    <div class="toolbar">
      <label class="search-box">
        <span aria-hidden="true">⌕</span>
        <input
          ref="searchInput"
          v-model="store.search"
          type="search"
          placeholder="Search by name, phone number, or tag"
          aria-label="Search contacts by name, phone number, or tag"
          @input="handleSearch"
        />
        <kbd>/</kbd>
      </label>

      <label class="select-control">
        <span>Sort</span>
        <select v-model="store.sort" @change="reload">
          <option value="name_asc">Name A-Z</option>
          <option value="name_desc">Name Z-A</option>
          <option value="recently_viewed">Recently viewed</option>
          <option value="recently_added">Recently added</option>
          <option value="recently_updated">Recently updated</option>
        </select>
      </label>

      <label class="select-control">
        <span>Filter</span>
        <select v-model="filterValue" @change="applyFilter">
          <option value="all">All contacts</option>
          <option value="favorites">Favorites</option>
          <option value="unlabeled">Untagged</option>
        </select>
      </label>

      <label class="select-control">
        <span>Tag</span>
        <select v-model="tagFilter" aria-label="Filter by tag" @change="applyTag">
          <option value="">All Tags</option>
          <option v-for="tag in store.tags" :key="tag.name" :value="tag.name">
            {{ tag.name }}
          </option>
        </select>
      </label>

      <button
        v-if="hasActiveFilters"
        class="clear-filters-button"
        type="button"
        @click="clearFilters"
      >
        Clear filters
      </button>
    </div>

    <div v-if="store.selectedIds.length" class="bulk-bar">
      <strong>{{ store.selectedIds.length }} selected</strong>
      <button type="button" @click="bulkFavorite">Favorite selected</button>
      <button type="button" class="danger-link" @click="bulkDelete">Delete selected</button>
      <button type="button" class="quiet-button" @click="store.selectedIds = []">Clear</button>
    </div>

    <div class="table-frame">
      <div v-if="store.loading" class="state-message">
        <div class="spinner"></div>
        <p>Loading contacts...</p>
      </div>

      <div v-else-if="store.error" class="state-message error">
        <p>{{ store.error }}</p>
        <button class="secondary-button" type="button" @click="reload">Try again</button>
      </div>

      <div v-else-if="!store.contacts.length" class="state-message">
        <div class="empty-icon">⌕</div>
        <h3>{{ emptyStateTitle }}</h3>
        <p>{{ emptyStateMessage }}</p>
        <div class="empty-state-actions">
          <button
            v-if="hasActiveFilters"
            class="secondary-button"
            type="button"
            @click="clearFilters"
          >
            Clear filters
          </button>
          <button
            v-else
            class="secondary-button"
            type="button"
            @click="showCreateForm = true"
          >
            Add contact
          </button>
        </div>
      </div>

      <table v-else class="contact-table">
        <thead>
          <tr>
            <th>
              <input
                type="checkbox"
                :checked="allSelected"
                aria-label="Select all contacts"
                @change="toggleAll"
              />
            </th>
            <th>Name</th>
            <th>Phone</th>
            <th>Email</th>
            <th>Tags</th>
            <th>Updated</th>
            <th><span class="sr-only">Actions</span></th>
          </tr>
        </thead>

        <tbody>
          <tr
            v-for="contact in store.contacts"
            :key="contact.id"
            class="contact-table-row"
            @click="openContact(contact)"
          >
            <td @click.stop>
              <input
                type="checkbox"
                :checked="store.selectedIds.includes(contact.id)"
                :aria-label="`Select ${contact.name}`"
                @change="toggleSelected(contact.id)"
              />
            </td>

            <td>
              <div class="contact-identity">
                <span class="avatar">{{ initials(contact.name) }}</span>
                <span>
                  <strong>{{ contact.name }}</strong>
                  <small v-if="contact.is_favorite">Favorite</small>
                </span>
              </div>
            </td>

            <td>{{ contact.phone_number }}</td>
            <td class="muted-cell">{{ contact.email || "-" }}</td>

            <td>
              <div class="tag-list">
                <span
                  v-for="tag in contact.tags || []"
                  :key="tag.id"
                  class="tag-pill"
                >
                  {{ tag.name }}
                </span>
                <span v-if="!contact.tags?.length" class="muted-cell">-</span>
              </div>
            </td>

            <td class="muted-cell">{{ formatDate(contact.updated_at) }}</td>

            <td @click.stop>
              <button
                class="icon-button"
                type="button"
                :aria-label="contact.is_favorite ? 'Remove favorite' : 'Add favorite'"
                @click="toggleFavorite(contact)"
              >
                {{ contact.is_favorite ? "★" : "☆" }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <Pagination
      v-if="!store.loading && store.total > 0"
      :page="store.page"
      :limit="store.limit"
      :total="store.total"
      :total-pages="store.totalPages"
      @change="changePage"
    />

    <ContactForm
      v-if="showCreateForm"
      @close="showCreateForm = false"
      @created="created"
    />

    <ImportContactsDialog
      v-if="showImport"
      @close="showImport = false"
      @imported="imported"
    />

    <ContactDetail
      v-if="selectedContact"
      :contact="selectedContact"
      @close="selectedContact = null"
      @updated="updated"
      @delete="handleDeleteRequest"
    />

    <ConfirmDialog
      v-if="showDeleteDialog"
      :contact="contactToDelete"
      :loading="store.deleting"
      @cancel="showDeleteDialog = false"
      @confirm="confirmDelete"
    />

    <div v-if="toast" class="toast" role="status">
      {{ toast }}
      <button v-if="deletedContact" type="button" @click="undoDelete">Undo</button>
    </div>
  </section>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import ContactDetail from "../components/ContactDetail.vue";
import ContactForm from "../components/ContactForm.vue";
import ConfirmDialog from "../components/ConfirmDialog.vue";
import ImportContactsDialog from "../components/ImportContactsDialog.vue";
import Pagination from "../components/Pagination.vue";
import { useContactsStore } from "../stores/contacts";

const route = useRoute();
const router = useRouter();
const store = useContactsStore();

const searchInput = ref(null);
const showCreateForm = ref(false);
const showImport = ref(false);
const selectedContact = ref(null);
const contactToDelete = ref(null);
const showDeleteDialog = ref(false);
const toast = ref("");
const deletedContact = ref(null);
const filterValue = ref("all");
const tagFilter = ref("");

let searchTimeout;
let undoTimeout;

const sectionLabel = computed(() => route.meta.title || "Contacts");

const allSelected = computed(() =>
  store.contacts.length > 0 &&
  store.contacts.every((contact) => store.selectedIds.includes(contact.id)),
);

const hasActiveFilters = computed(() => {
  return Boolean(
    store.search.trim() ||
    tagFilter.value ||
    filterValue.value !== "all",
  );
});

const resultCardTitle = computed(() => {
  return hasActiveFilters.value ? "Search results" : "Total contacts";
});

const resultCardSubtitle = computed(() => {
  if (!hasActiveFilters.value) return "In your phonebook";
  return `${store.total} matching contact${store.total === 1 ? "" : "s"}`;
});

const emptyStateTitle = computed(() => {
  if (store.search.trim()) return "No contacts found";
  if (tagFilter.value) return `No contacts tagged “${tagFilter.value}”`;
  if (filterValue.value === "favorites") return "No favorite contacts";
  if (filterValue.value === "unlabeled") return "No untagged contacts";
  return "No contacts here";
});

const emptyStateMessage = computed(() => {
  const search = store.search.trim();

  if (search) {
    return `We couldn't find any contacts matching “${search}” by name, phone number, or tag.`;
  }

  if (tagFilter.value) {
    return `There aren't any contacts with the “${tagFilter.value}” tag.`;
  }

  if (filterValue.value === "favorites") {
    return "There aren't any contacts marked as favorites yet.";
  }

  if (filterValue.value === "unlabeled") {
    return "Every contact currently has at least one tag.";
  }

  return "Add your first contact to get started.";
});

const taggedContactsCount = computed(() => {
  return Math.max(
    0,
    store.metrics.total - store.metrics.unlabeled
  );
});

function syncRouteView() {
  const routeFilter = typeof route.query.filter === "string" ? route.query.filter : "all";
  const validFilter = ["all", "favorites", "unlabeled"].includes(routeFilter)
    ? routeFilter
    : "all";

  store.favorite = route.meta.view === "favorites" || validFilter === "favorites" ? true : null;
  store.tag = typeof route.query.tag === "string" ? route.query.tag : "";
  store.search = typeof route.query.search === "string" ? route.query.search : "";
  store.unlabeled = validFilter === "unlabeled";
  store.recent = route.meta.view === "recent";

  filterValue.value = route.meta.view === "favorites" ? "favorites" : validFilter;
  tagFilter.value = store.tag;

  store.sort = route.meta.view === "recent" ? "recently_viewed" : "name_asc";
  store.page = 1;
  store.selectedIds = [];

  Promise.all([reload(), store.fetchMetrics(), store.fetchTags()]);
}

function reload() {
  return store.fetchContacts();
}

async function applyFilter() {
  store.favorite = filterValue.value === "favorites" ? true : null;
  store.unlabeled = filterValue.value === "unlabeled";
  store.recent = false;
  store.page = 1;
  store.selectedIds = [];

  await router.replace({
    path: route.path === "/contacts" ? "/contacts" : route.path,
    query: {
      ...route.query,
      filter: filterValue.value === "all" ? undefined : filterValue.value,
      tag: store.tag || undefined,
      search: store.search || undefined,
      page: undefined,
    },
  });

  await reload();
}

async function applyTag() {
  store.tag = tagFilter.value;
  store.page = 1;
  store.selectedIds = [];

  await router.replace({
    path: "/contacts",
    query: {
      ...route.query,
      tag: store.tag || undefined,
      search: store.search || undefined,
      filter: filterValue.value === "all" ? undefined : filterValue.value,
      page: undefined,
    },
  });

  await reload();
}

function handleSearch() {
  clearTimeout(searchTimeout);
  store.page = 1;

  searchTimeout = setTimeout(async () => {
    await router.replace({
      path: route.path,
      query: {
        ...route.query,
        search: store.search || undefined,
        page: undefined,
      },
    });

    await reload();
  }, 250);
}

function clearFilters() {
  clearTimeout(searchTimeout);
  store.search = "";
  store.tag = "";
  tagFilter.value = "";
  filterValue.value = "all";
  store.favorite = null;
  store.unlabeled = false;
  store.recent = false;
  store.page = 1;
  store.selectedIds = [];

  router.replace({
    path: route.meta.view === "favorites" || route.meta.view === "recent" ? "/contacts" : route.path,
    query: {},
  });
}

function changePage(page) {
  store.page = page;
  reload();
}

function formatDate(value) {
  return value
    ? new Date(value).toLocaleDateString(undefined, {
        month: "short",
        day: "numeric",
        year: "numeric",
      })
    : "-";
}

function initials(name) {
  return name
    .split(" ")
    .map((word) => word[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();
}

async function openContact(contact) {
  selectedContact.value = await store.getContact(contact.id);
}

async function toggleFavorite(contact) {
  await store.toggleFavorite(contact);
  toast.value = contact.is_favorite
    ? "Removed from favorites"
    : "Added to favorites";
  clearToast();
}

function toggleSelected(id) {
  store.selectedIds = store.selectedIds.includes(id)
    ? store.selectedIds.filter((item) => item !== id)
    : [...store.selectedIds, id];
}

function toggleAll() {
  store.selectedIds = allSelected.value
    ? []
    : store.contacts.map((contact) => contact.id);
}

async function bulkFavorite() {
  await Promise.all(
    store.contacts
      .filter(
        (contact) =>
          store.selectedIds.includes(contact.id) && !contact.is_favorite,
      )
      .map((contact) => store.toggleFavorite(contact)),
  );

  toast.value = "Favorites updated";
  store.selectedIds = [];
  clearToast();
}

async function bulkDelete() {
  const ids = [...store.selectedIds];
  if (!ids.length) return;

  await store.deleteContacts(ids);
  store.selectedIds = [];
  toast.value = `${ids.length} contacts deleted`;
  clearToast();
}

async function handleDeleteRequest(contact) {
  contactToDelete.value = contact;
  const preferences = JSON.parse(
    localStorage.getItem("phonebook-preferences") || "{}",
  );

  if (preferences.confirmDelete === false) {
    await confirmDelete();
    return;
  }

  showDeleteDialog.value = true;
}

async function confirmDelete() {
  if (!contactToDelete.value) return;

  const contact = contactToDelete.value;
  await store.deleteContact(contact.id);
  deletedContact.value = contact;
  selectedContact.value = null;
  contactToDelete.value = null;
  showDeleteDialog.value = false;
  toast.value = "Contact deleted";

  clearTimeout(undoTimeout);
  undoTimeout = setTimeout(() => {
    deletedContact.value = null;
    toast.value = "";
  }, 5000);
}

async function undoDelete() {
  if (!deletedContact.value) return;

  await store.restoreContact(deletedContact.value);
  deletedContact.value = null;
  toast.value = "Contact restored";
  clearToast();
}

async function created() {
  showCreateForm.value = false;
  toast.value = "Contact created";
  clearToast();
}

async function updated(contact) {
  selectedContact.value = contact;
  toast.value = "Contact updated";
  clearToast();
}

function clearToast() {
  setTimeout(() => {
    if (!deletedContact.value) toast.value = "";
  }, 3000);
}

async function exportContacts() {
  try {
    const count = await store.exportContacts();
    toast.value = count
      ? `Exported ${count} contacts`
      : "No contacts to export";
  } catch {
    toast.value = "Unable to export contacts";
  }

  clearToast();
}

async function imported(result) {
  showImport.value = false;
  toast.value = `Imported ${result.imported}; skipped ${result.skipped}`;
  clearToast();
}

function handleKeydown(event) {
  const preferences = JSON.parse(
    localStorage.getItem("phonebook-preferences") || "{}",
  );

  if (preferences.shortcuts === false) return;

  const typing = ["INPUT", "TEXTAREA", "SELECT"].includes(
    document.activeElement?.tagName,
  );

  if (event.key === "/" && !typing) {
    event.preventDefault();
    nextTick(() => searchInput.value?.focus());
  }

  if (event.key.toLowerCase() === "n" && !typing) {
    showCreateForm.value = true;
  }

  if (event.key === "Escape") {
    showCreateForm.value = false;
    showImport.value = false;
    selectedContact.value = null;
    showDeleteDialog.value = false;
  }
}

watch(() => route.fullPath, syncRouteView);

onMounted(() => {
  store.limit = Number(localStorage.getItem("phonebook-limit")) || store.limit;
  syncRouteView();
  window.addEventListener("keydown", handleKeydown);
});

onBeforeUnmount(() => {
  clearTimeout(searchTimeout);
  clearTimeout(undoTimeout);
  window.removeEventListener("keydown", handleKeydown);
});
</script>