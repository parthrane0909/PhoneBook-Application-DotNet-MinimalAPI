<template>
  <div class="pagination">

    <span class="pagination-info">
      Showing {{ startItem }}–{{ endItem }} of {{ total }}
    </span>

    <div class="pagination-controls">

      <!-- PREVIOUS PAGE -->
      <button
        class="pagination-arrow"
        type="button"
        :disabled="page === 1"
        @click="$emit('change', page - 1)"
        aria-label="Previous page"
      >
        ←
      </button>


      <!-- PAGE NUMBERS -->
      <button
        v-for="pageNumber in visiblePages"
        :key="pageNumber"
        type="button"
        class="page-number"
        :class="{ active: pageNumber === page }"
        :aria-current="pageNumber === page ? 'page' : undefined"
        @click="$emit('change', pageNumber)"
      >
        {{ pageNumber }}
      </button>


      <!-- NEXT PAGE -->
      <button
        class="pagination-arrow"
        type="button"
        :disabled="page >= totalPages"
        @click="$emit('change', page + 1)"
        aria-label="Next page"
      >
        →
      </button>


      <!-- GO TO PAGE -->
      <div class="page-jump">
        <input
          v-model="pageInput"
          type="number"
          min="1"
          :max="totalPages || 1"
          :aria-label="`Current page, page ${page} of ${totalPages}`"
          @keydown.enter="goToPage"
          @blur="goToPage"
        />

        <span>of {{ totalPages }}</span>
      </div>

    </div>

  </div>
</template>


<script setup>
import { computed, ref, watch } from "vue";

const props = defineProps({
  page: Number,
  limit: Number,
  total: Number,
  totalPages: Number,
});

const emit = defineEmits(["change"]);


/*
 * ---------------------------------------------------------
 * ITEM RANGE
 * ---------------------------------------------------------
 */

const startItem = computed(() => {
  if (props.total === 0) {
    return 0;
  }

  return (props.page - 1) * props.limit + 1;
});


const endItem = computed(() => {
  return Math.min(
    props.page * props.limit,
    props.total
  );
});


/*
 * ---------------------------------------------------------
 * VISIBLE PAGE NUMBERS
 *
 * Keeps the existing pagination behaviour:
 *
 * ← 1 2 [3] 4 5 →
 *
 * and moves the five-page window as the user navigates.
 * ---------------------------------------------------------
 */

const visiblePages = computed(() => {
  const pageCount = props.totalPages || 0;

  if (pageCount <= 5) {
    return Array.from(
      { length: pageCount },
      (_, index) => index + 1
    );
  }

  let start = Math.max(
    1,
    props.page - 2
  );

  let end = Math.min(
    pageCount,
    props.page + 2
  );


  if (props.page <= 3) {
    start = 1;
    end = 5;
  } else if (props.page >= pageCount - 2) {
    start = pageCount - 4;
    end = pageCount;
  }


  return Array.from(
    { length: end - start + 1 },
    (_, index) => start + index
  );
});


/*
 * ---------------------------------------------------------
 * PAGE INPUT
 * ---------------------------------------------------------
 */

const pageInput = ref(String(props.page));


/*
 * Keep the input synchronized when the user changes
 * the page by clicking an arrow or page number.
 */

watch(
  () => props.page,
  (newPage) => {
    pageInput.value = String(newPage);
  }
);


/*
 * ---------------------------------------------------------
 * GO TO PAGE
 * ---------------------------------------------------------
 *
 * The user can type:
 *
 * 57
 *
 * and press Enter.
 *
 * The value is restricted between:
 *
 * 1
 * and
 * totalPages
 * ---------------------------------------------------------
 */

function goToPage() {
  if (!props.totalPages) {
    pageInput.value = "1";
    return;
  }


  let requestedPage = Number(
    pageInput.value
  );


  if (!Number.isInteger(requestedPage)) {
    pageInput.value = String(props.page);
    return;
  }


  requestedPage = Math.max(
    1,
    Math.min(
      requestedPage,
      props.totalPages
    )
  );


  pageInput.value = String(
    requestedPage
  );


  if (requestedPage !== props.page) {
    emit(
      "change",
      requestedPage
    );
  }
}
</script>