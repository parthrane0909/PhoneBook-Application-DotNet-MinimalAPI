<template>

  <div class="contact-list">

    <div
      v-if="loading"
      class="state-message"
    >
      <div class="spinner"></div>
      <p>Loading contacts...</p>
    </div>


    <div
      v-else-if="error"
      class="state-message error"
    >
      <p>{{ error }}</p>

      <button @click="$emit('retry')">
        Try again
      </button>
    </div>


    <div
      v-else-if="contacts.length === 0"
      class="state-message"
    >
      <div class="empty-icon">
        ⌕
      </div>

      <h3>No contacts found</h3>

      <p>
        Try a different search or add a new contact.
      </p>
    </div>


    <template v-else>

      <ContactRow
        v-for="contact in contacts"
        :key="contact.id"
        :contact="contact"
        @select="$emit('select', $event)"
      />

    </template>

  </div>

</template>


<script setup>

import ContactRow from "./ContactRow.vue";


defineProps({

  contacts: {
    type: Array,
    required: true,
  },

  loading: Boolean,

  error: {
    type: String,
    default: null,
  },

});


defineEmits([
  "retry",
  "select",
]);

</script>