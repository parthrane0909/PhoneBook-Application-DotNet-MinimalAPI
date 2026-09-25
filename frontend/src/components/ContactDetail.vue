<template>
  <div class="detail-overlay" @click.self="close">

    <aside class="contact-detail">

      <!-- HEADER -->

      <div class="detail-header">

        <button
          class="detail-close"
          @click="close"
          aria-label="Close contact"
        >
          ×
        </button>

        <div class="detail-actions">
          <button
            v-if="!editing"
            class="detail-edit-button"
            @click="startEditing"
          >
            Edit
          </button>
        </div>

      </div>


      <!-- VIEW MODE -->

      <template v-if="!editing">

        <div class="profile-section">

          <div class="large-avatar">
            {{ initials }}
          </div>

          <h2>{{ contact.name }}</h2>

          <button class="favorite-large" type="button" @click="toggleFavorite">
            {{ contact.is_favorite ? "★" : "☆" }}
            <span>{{ contact.is_favorite ? "Favorited" : "Favorite" }}</span>
          </button>

        </div>


        <div class="detail-information">

          <div class="information-block">
            <span class="information-label">PHONE</span>

            <div class="information-value">
              {{ contact.phone_number }}
            </div>
          </div>


          <div
            v-if="contact.email"
            class="information-block"
          >
            <span class="information-label">EMAIL</span>

            <div class="information-value">
              {{ contact.email }}
            </div>
          </div>


          <div
            v-if="contact.address"
            class="information-block"
          >
            <span class="information-label">ADDRESS</span>

            <div class="information-value">
              {{ contact.address }}
            </div>
          </div>

          <div class="information-block">
            <span class="information-label">TAGS</span>
            <LabelSelector :model-value="detailLabels" @update:model-value="updateLabels" />
          </div>


          <div class="information-block">
            <span class="information-label">CREATED</span>

            <div class="information-value">
              {{ formattedDate }}
            </div>
          </div>

        </div>


        <div class="detail-footer">
          <button
            class="delete-detail-button"
            @click="requestDelete"
          >
            Delete Contact
          </button>
        </div>

      </template>


      <!-- EDIT MODE -->

      <template v-else>

        <div class="edit-section">

          <p class="eyebrow">EDIT CONTACT</p>

          <h2>Edit Contact</h2>


          <form @submit.prevent="saveChanges">

            <div class="form-field">
              <label for="edit-name">
                Name <span>*</span>
              </label>

              <input
                id="edit-name"
                v-model="form.name"
                type="text"
                :class="{ invalid: errors.name }"
              />

              <small v-if="errors.name">
                {{ errors.name }}
              </small>
            </div>


            <div class="form-field">
              <label for="edit-phone">
                Phone number <span>*</span>
              </label>

              <input
                id="edit-phone"
                v-model="form.phone_number"
                type="tel"
                :class="{ invalid: errors.phone_number }"
              />

              <small v-if="errors.phone_number">
                {{ errors.phone_number }}
              </small>
            </div>


            <div class="form-field">
              <label for="edit-email">
                Email
              </label>

              <input
                id="edit-email"
                v-model="form.email"
                type="email"
                :class="{ invalid: errors.email }"
              />

              <small v-if="errors.email">
                {{ errors.email }}
              </small>
            </div>


            <div class="form-field">
              <label for="edit-address">
                Address
              </label>

              <textarea
                id="edit-address"
                v-model="form.address"
                rows="4"
              ></textarea>
            </div>

            <div class="form-field">
              <label for="edit-tags">Tags</label>
              <LabelSelector v-model="form.tags" />
            </div>


            <div v-if="serverError" class="server-error">
              {{ serverError }}
            </div>


            <div class="edit-actions">

              <button
                type="button"
                class="cancel-button"
                @click="cancelEditing"
                :disabled="saving"
              >
                Cancel
              </button>

              <button
                type="submit"
                class="save-button"
                :disabled="saving"
              >
                {{ saving ? "Saving..." : "Save Changes" }}
              </button>

            </div>

          </form>

        </div>

      </template>

    </aside>

  </div>
</template>


<script setup>
import { computed, reactive, ref, watch } from "vue";
import { useContactsStore } from "../stores/contacts";
import LabelSelector from "./LabelSelector.vue";

const props = defineProps({
  contact: {
    type: Object,
    required: true,
  },
});

const emit = defineEmits([
  "close",
  "updated",
  "delete",
]);

const store = useContactsStore();

const editing = ref(false);
const saving = ref(false);
const serverError = ref("");
const detailLabels = ref((props.contact.tags || []).map((tag) => tag.name));

const form = reactive({
  name: "",
  phone_number: "",
  email: "",
  address: "",
  tags: [],
});

const errors = reactive({
  name: "",
  phone_number: "",
  email: "",
});


const initials = computed(() => {
  return props.contact.name
    .split(" ")
    .map((word) => word[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();
});


const formattedDate = computed(() => {
  if (!props.contact.created_at) {
    return "Unknown";
  }

  return new Date(
    props.contact.created_at
  ).toLocaleDateString();
});


async function toggleFavorite() {
  const updated = await store.toggleFavorite(props.contact);
  emit("updated", updated);
}


async function updateLabels(labels) {
  detailLabels.value = labels;
  const updated = await store.updateContact(props.contact.id, { tags: labels });
  emit("updated", updated);
}


function close() {
  if (!saving.value) {
    emit("close");
  }
}


function startEditing() {

  form.name = props.contact.name || "";
  form.phone_number = props.contact.phone_number || "";
  form.email = props.contact.email || "";
  form.address = props.contact.address || "";
  form.tags = (props.contact.tags || []).map((tag) => tag.name);

  errors.name = "";
  errors.phone_number = "";
  errors.email = "";

  serverError.value = "";

  editing.value = true;
}


function cancelEditing() {

  if (!saving.value) {
    editing.value = false;
  }
}


function validate() {

  errors.name = "";
  errors.phone_number = "";
  errors.email = "";

  let valid = true;


  if (!form.name.trim()) {
    errors.name = "Name is required.";
    valid = false;
  }


  if (!form.phone_number.trim()) {
    errors.phone_number = "Phone number is required.";
    valid = false;
  } else if (
    !/^\+?[0-9\s-]{7,20}$/.test(
      form.phone_number
    )
  ) {
    errors.phone_number =
      "Enter a valid phone number.";

    valid = false;
  }


  if (
    form.email &&
    !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(
      form.email
    )
  ) {
    errors.email =
      "Enter a valid email address.";

    valid = false;
  }


  return valid;
}


async function saveChanges() {

  serverError.value = "";

  if (!validate()) {
    return;
  }

  saving.value = true;

  try {

    const updated = await store.updateContact(
      props.contact.id,
      {
        name: form.name.trim(),
        phone_number: form.phone_number.trim(),
        email: form.email.trim() || null,
        address: form.address.trim() || null,
        tags: form.tags,
      }
    );

    editing.value = false;

    emit("updated", updated);

  } catch (error) {

    console.error(error);

    if (error.response?.data?.detail) {
      serverError.value = error.response.data.detail;
    } else if (!error.response) {
      serverError.value =
        "Unable to reach the phonebook API. Make sure the Java backend is running on port 8000.";
    } else {
      serverError.value = "Unable to update contact.";
    }

  } finally {
    saving.value = false;
  }
}


function requestDelete() {
  emit("delete", props.contact);
}


watch(
  () => props.contact.tags,
  (tags) => {
    detailLabels.value = (tags || []).map((tag) => tag.name);
  },
  { deep: true },
);
</script>