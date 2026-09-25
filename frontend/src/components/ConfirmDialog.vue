<template>
  <div class="modal-backdrop" @click.self="$emit('cancel')">
    <div class="confirm-dialog">
      <div class="confirm-icon">
        !
      </div>

      <h3>Delete contact?</h3>

      <p>
        Are you sure you want to delete
        <strong>{{ contact.name }}</strong>?
      </p>

      <p class="confirm-warning">
        You can undo this action for a few seconds.
      </p>

      <div class="confirm-actions">
        <button
          class="secondary-button"
          @click="$emit('cancel')"
        >
          Cancel
        </button>

        <button
          class="danger-button"
          :disabled="loading"
          @click="$emit('confirm')"
        >
          {{ loading ? "Deleting..." : "Delete" }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  contact: {
    type: Object,
    required: true,
  },

  loading: {
    type: Boolean,
    default: false,
  },
});

defineEmits(["cancel", "confirm"]);
</script>