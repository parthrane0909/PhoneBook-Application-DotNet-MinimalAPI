import { createRouter, createWebHistory } from "vue-router";
import ContactsView from "../views/ContactsView.vue";
import SettingsView from "../views/SettingsView.vue";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/", redirect: "/contacts" },
    { path: "/contacts", name: "contacts", component: ContactsView, meta: { title: "All contacts", view: "all" } },
    { path: "/favorites", name: "favorites", component: ContactsView, meta: { title: "Favorites", view: "favorites" } },
    { path: "/recently-viewed", name: "recently-viewed", component: ContactsView, meta: { title: "Recently viewed", view: "recent" } },
    { path: "/settings", name: "settings", component: SettingsView, meta: { title: "Settings" } },
  ],
});

export default router;
