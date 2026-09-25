<template>
  <div class="label-selector">
    <div class="selected-labels" aria-live="polite">
      <button v-for="tag in modelValue" :key="tag" type="button" class="selected-label" :aria-label="`Remove ${tag} tag`" @click="removeLabel(tag)">
        {{ tag }} <span aria-hidden="true">×</span>
      </button>
      <span v-if="!modelValue.length" class="label-placeholder">No tags selected</span>
    </div>

    <div class="label-selector-controls">
      <select aria-label="Choose an existing tag" value="" @change="selectLabel">
        <option value="">Add existing tag</option>
        <option v-for="tag in availableTags" :key="tag.id || tag.name" :value="tag.name">{{ tag.name }}</option>
      </select>
      <input v-model="newLabel" type="text" placeholder="New tag" aria-label="New tag name" @keydown.enter.prevent="addNewLabel" />
      <button type="button" class="label-add-button" @click="addNewLabel">Add</button>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from "vue";
import { useContactsStore } from "../stores/contacts";

const props = defineProps({
  modelValue: { type: Array, default: () => [] },
});
const emit = defineEmits(["update:modelValue"]);
const store = useContactsStore();
const newLabel = ref("");

const availableTags = computed(() => {
  const selected = new Set(props.modelValue.map((label) => label.toLowerCase()));
  return store.tags.filter((tag) => !selected.has(tag.name.toLowerCase()));
});

function update(labels) {
  emit("update:modelValue", [...new Set(labels.map((label) => label.trim()).filter(Boolean))]);
}
function selectLabel(event) {
  if (event.target.value) update([...props.modelValue, event.target.value]);
  event.target.value = "";
}
function addNewLabel() {
  const label = newLabel.value.trim();
  if (!label) return;
  update([...props.modelValue, label]);
  newLabel.value = "";
}
function removeLabel(label) {
  update(props.modelValue.filter((item) => item !== label));
}

onMounted(async () => {
  if (!store.tags.length) {
    try {
      await store.fetchTags();
    } catch {
      // New labels can still be entered when the API is unavailable.
    }
  }
});
</script>
