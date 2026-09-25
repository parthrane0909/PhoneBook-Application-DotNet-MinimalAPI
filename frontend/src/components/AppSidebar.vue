<template>
  <aside class="sidebar" :class="{ open }">
    <div class="sidebar-brand">
      <div class="brand-icon">
        P
      </div>

      <div>
        <strong>Phonebook</strong>
        <span>Contact workspace</span>
      </div>
    </div>

    <nav class="sidebar-nav">
      <RouterLink
        class="sidebar-item"
        to="/contacts"
        @click="emit('close')"
      >
        <span class="sidebar-icon">▦</span>
        <span>All Contacts</span>
      </RouterLink>

      <RouterLink
        class="sidebar-item"
        to="/favorites"
        @click="emit('close')"
      >
        <span class="sidebar-icon">★</span>
        <span>Favorites</span>
      </RouterLink>

      <RouterLink
        class="sidebar-item"
        to="/recently-viewed"
        @click="emit('close')"
      >
        <span class="sidebar-icon">◷</span>
        <span>Recently viewed</span>
      </RouterLink>

      <RouterLink
        class="sidebar-item"
        to="/settings"
        @click="emit('close')"
      >
        <span class="sidebar-icon">⚙</span>
        <span>Settings</span>
      </RouterLink>
    </nav>
  </aside>
</template>

<script setup>
defineProps({
  open: Boolean,
});

const emit = defineEmits(["close"]);
</script>