<script setup>

defineProps({
  visible: {
    type: Boolean,
    required: true
  },

  editingId: {
    default: null
  },

  plateNumber: {
    type: String,
    default: ''
  },

  model: {
    type: String,
    default: ''
  },

  status: {
    type: String,
    default: ''
  }
})


const emit = defineEmits([
  'update:visible',
  'update:plateNumber',
  'update:model',
  'update:status',
  'save'
])

</script>


<template>

  <el-dialog
    :model-value="visible"

    :title="
      editingId === null
        ? 'Add Vehicle'
        : 'Edit Vehicle'
    "

    width="500px"

    @update:model-value="
      value =>
        emit(
          'update:visible',
          value
        )
    "
  >

    <el-form label-width="120px">


      <el-form-item
        label="Plate Number"
      >

        <el-input
          :model-value="
            plateNumber
          "

          placeholder="
            Enter plate number
          "

          @update:model-value="
            value =>
              emit(
                'update:plateNumber',
                value
              )
          "
        />

      </el-form-item>


      <el-form-item
        label="Model"
      >

        <el-input
          :model-value="
            model
          "

          placeholder="
            Enter vehicle model
          "

          @update:model-value="
            value =>
              emit(
                'update:model',
                value
              )
          "
        />

      </el-form-item>


      <el-form-item
        label="Status"
      >

        <el-select
          :model-value="
            status
          "

          placeholder="
            Select status
          "

          style="
            width: 100%
          "

          @update:model-value="
            value =>
              emit(
                'update:status',
                value
              )
          "
        >

          <el-option
            label="Active"
            value="Active"
          />

          <el-option
            label="Inactive"
            value="Inactive"
          />

        </el-select>

      </el-form-item>

    </el-form>


    <template #footer>

      <el-button
        @click="
          emit(
            'update:visible',
            false
          )
        "
      >
        Cancel
      </el-button>


      <el-button
        type="primary"
        @click="
          emit('save')
        "
      >
        Save
      </el-button>

    </template>

  </el-dialog>

</template>