import { defineStore } from "pinia";
import api from "../services/api";

export const useContactsStore = defineStore("contacts", {
  state: () => ({
    contacts: [],
    selectedContact: null,
    search: "",
    page: 1,
    limit: 10,
    total: 0,
    view: "all",
    favorite: null,
    tag: "",
    sort: "name_asc",
    unlabeled: false,
    recent: false,
    tags: [],
    metrics: {
      total: 0,
      favorites: 0,
      recently_added: 0,
      unlabeled: 0,
    },
    selectedIds: [],
    loading: false,
    error: null,
    deleting: false,
  }),

  getters: {
    totalPages: (state) => Math.ceil(state.total / state.limit),
    favoriteContacts: (state) =>
      state.contacts.filter((contact) => contact.is_favorite),
  },

  actions: {
    currentQuery(overrides = {}) {
      return {
        search: this.search || undefined,
        favorite: this.favorite === null ? undefined : this.favorite,
        tag: this.tag || undefined,
        unlabeled: this.unlabeled || undefined,
        recent: this.recent || undefined,
        sort: this.sort,
        ...overrides,
      };
    },

    setView(view) {
      this.view = view;
      this.favorite = view === "favorites" ? true : null;
      this.tag = "";
      this.page = 1;
    },

    setSort(sort) {
      this.sort = sort;
      this.page = 1;
    },

    setTag(tag) {
      this.tag = tag;
      this.favorite = null;
      this.view = "all";
      this.page = 1;
    },

    async fetchContacts() {
      this.loading = true;
      this.error = null;

      try {
        const response = await api.get("/contacts/", {
          params: this.currentQuery({
            page: this.page,
            limit: this.limit,
          }),
        });

        this.contacts = response.data.contacts;
        this.total = response.data.total;

        const lastPage = Math.max(1, Math.ceil(this.total / this.limit));

        if (this.page > lastPage) {
          this.page = lastPage;
          return this.fetchContacts();
        }
      } catch (error) {
        console.error(error);
        this.error = "Unable to load contacts.";
      } finally {
        this.loading = false;
      }
    },

    async fetchTags() {
      const response = await api.get("/contacts/tags");
      this.tags = response.data;
      return this.tags;
    },

    async fetchMetrics() {
      const response = await api.get("/contacts/metrics");
      this.metrics = response.data;
      return this.metrics;
    },

    async refreshDerivedState() {
      await Promise.all([this.fetchMetrics(), this.fetchTags()]);
    },

    async createContact(contact) {
      const response = await api.post("/contacts/", contact);
      try {
        await this.fetchContacts();
        await this.refreshDerivedState();
      } catch (error) {
        console.error("Contact saved, but refreshing the list failed:", error);
      }
      return response.data;
    },

    async updateContact(id, contact) {
      const response = await api.put(`/contacts/${id}`, contact);
      const index = this.contacts.findIndex((item) => item.id === id);

      if (index !== -1) this.contacts[index] = response.data;
      if (this.selectedContact?.id === id) {
        this.selectedContact = response.data;
      }

      try {
        await this.fetchContacts();
        await this.refreshDerivedState();
      } catch (error) {
        console.error("Contact updated, but refreshing the list failed:", error);
      }
      return response.data;
    },

    async deleteContact(id) {
      this.deleting = true;

      try {
        await api.delete(`/contacts/${id}`);
        this.contacts = this.contacts.filter((item) => item.id !== id);
        this.selectedIds = this.selectedIds.filter((item) => item !== id);
        this.total = Math.max(0, this.total - 1);
        await this.fetchContacts();
        await this.refreshDerivedState();
      } catch (error) {
        console.error(error);
        throw error;
      } finally {
        this.deleting = false;
      }
    },

    async restoreContact(contact) {
      try {
        const response = await api.post("/contacts/", {
          name: contact.name,
          phone_number: contact.phone_number,
          email: contact.email,
          address: contact.address,
          tags: (contact.tags || []).map((tag) => tag.name),
        });

        await this.fetchContacts();
        await this.refreshDerivedState();
        return response.data;
      } catch (error) {
        console.error(error);
        throw error;
      }
    },

    async getContact(id) {
      const response = await api.get(`/contacts/${id}`);
      this.selectedContact = response.data;
      await this.markViewed(id);
      return this.selectedContact;
    },

    async markViewed(id) {
      const response = await api.patch(`/contacts/${id}/viewed`);
      const updatedContact = response.data;
      const index = this.contacts.findIndex((item) => item.id === id);

      if (index !== -1) this.contacts[index] = updatedContact;
      if (this.selectedContact?.id === id) {
        this.selectedContact = updatedContact;
      }

      return updatedContact;
    },

    async toggleFavorite(contact) {
      try {
        const response = await api.patch(
          `/contacts/${contact.id}/favorite`,
          {
            is_favorite: !contact.is_favorite,
          },
        );

        const updatedContact = response.data;
        const index = this.contacts.findIndex(
          (item) => item.id === updatedContact.id,
        );

        if (index !== -1) this.contacts[index] = updatedContact;

        if (
          this.selectedContact &&
          this.selectedContact.id === updatedContact.id
        ) {
          this.selectedContact = updatedContact;
        }

        this.metrics.favorites += updatedContact.is_favorite ? 1 : -1;
        await this.fetchContacts();
        return updatedContact;
      } catch (error) {
        console.error(error);
        throw error;
      }
    },

    async importContacts(rows) {
      const response = await api.post("/contacts/import", { rows });
      await this.fetchContacts();
      await this.refreshDerivedState();
      return response.data;
    },

    async deleteContacts(ids) {
      this.deleting = true;

      try {
        await Promise.all(ids.map((id) => api.delete(`/contacts/${id}`)));
        const selected = new Set(ids);
        this.contacts = this.contacts.filter(
          (contact) => !selected.has(contact.id),
        );
        this.selectedIds = this.selectedIds.filter(
          (id) => !selected.has(id),
        );
        this.total = Math.max(0, this.total - ids.length);
        await this.fetchContacts();
        await this.refreshDerivedState();
      } finally {
        this.deleting = false;
      }
    },

    async exportContacts() {
      const contacts = [];
      let page = 1;
      const limit = 100;

      while (true) {
        const response = await api.get("/contacts/", {
          params: this.currentQuery({ page, limit }),
        });

        contacts.push(...response.data.contacts);

        if (
          contacts.length >= response.data.total ||
          !response.data.contacts.length
        ) {
          break;
        }

        page += 1;
      }

      const escapeCsv = (value) => {
        const text = value == null ? "" : String(value);
        return /[",\n\r]/.test(text)
          ? `"${text.replaceAll('"', '""')}"`
          : text;
      };

      const header = [
        "name",
        "phone_number",
        "email",
        "address",
        "tags",
      ];

      const csv = [
        header.join(","),
        ...contacts.map((contact) =>
          [
            contact.name,
            contact.phone_number,
            contact.email,
            contact.address,
            (contact.tags || []).map((tag) => tag.name).join(", "),
          ]
            .map(escapeCsv)
            .join(","),
        ),
      ].join("\r\n");

      const url = URL.createObjectURL(
        new Blob([`\ufeff${csv}`], {
          type: "text/csv;charset=utf-8",
        }),
      );

      const link = document.createElement("a");
      link.href = url;
      link.download = "phonebook-contacts.csv";
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);

      return contacts.length;
    },
  },
});