<template>
  <div
    class="contact-row"
    @click="openContact"
    tabindex="0"
    @keydown.enter="openContact"
  >
    <div class="avatar">
      {{ initials }}
    </div>

    <div class="contact-main">
      <div class="contact-name">
        {{ contact.name }}
      </div>

      <div class="contact-meta">
        {{ contact.phone_number }}

        <span v-if="contact.email">
          · {{ contact.email }}
        </span>
      </div>

      <div
        v-if="contact.address"
        class="contact-address"
      >
        {{ contact.address }}
      </div>
    </div>

    <button
      class="favorite-button"
      :class="{ active: contact.is_favorite }"
      @click.stop="toggleFavorite"
      :aria-label="
        contact.is_favorite
          ? 'Remove from favorites'
          : 'Add to favorites'
      "
    >
      {{ contact.is_favorite ? "★" : "☆" }}
    </button>

    <button
      class="more-button"
      @click.stop
      aria-label="More options"
    >
      ⋮
    </button>
  </div>
</template>

<script setup>
import { computed } from "vue";
import { useContactsStore } from "../stores/contacts";

const props = defineProps({
  contact: {
    type: Object,
    required: true,
  },
});

const emit = defineEmits(["select"]);

const store = useContactsStore();

const initials = computed(() => {
  return props.contact.name
    .split(" ")
    .map((word) => word[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();
});

function openContact() {
  emit("select", props.contact);
}

async function toggleFavorite() {
  try {
    await store.toggleFavorite(props.contact);
  } catch (error) {
    console.error("Unable to update favorite:", error);
  }
}

</script>