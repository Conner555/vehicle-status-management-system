<script setup>

defineProps({
  vehicles: {
    type: Array,
    required: true
  },

  loading: {
    type: Boolean,
    default: false
  },

  currentRole: {
    type: String,
    default: ''
  }
})


const emit = defineEmits([
  'edit',
  'delete'
])

</script>


<template>

  <el-card class="table-card">

    <el-table
      v-loading="loading"
      :data="vehicles"
      style="width: 100%"
    >

      <el-table-column
        prop="id"
        label="ID"
        width="80"
      />


      <el-table-column
        prop="plateNumber"
        label="Plate Number"
        min-width="160"
      />


      <el-table-column
        prop="model"
        label="Model"
        min-width="180"
      />


      <el-table-column
        label="Status"
        width="140"
      >

        <template #default="scope">

          <el-tag
            :type="
              scope.row.status === 'Active'
                ? 'success'
                : 'info'
            "
          >
            {{ scope.row.status }}
          </el-tag>

        </template>

      </el-table-column>


      <el-table-column
        label="Actions"
        width="220"
      >

        <template #default="scope">

          <el-button
            v-if="
              currentRole === 'Admin'
            "
            size="small"
            @click="
              emit(
                'edit',
                scope.row
              )
            "
          >
            Edit
          </el-button>


          <el-button
            v-if="
              currentRole === 'Admin'
            "
            type="danger"
            size="small"
            @click="
              emit(
                'delete',
                scope.row.id
              )
            "
          >
            Delete
          </el-button>

        </template>

      </el-table-column>


      <template #empty>

        <el-empty
          description="
            No vehicles found
          "
        />

      </template>

    </el-table>

  </el-card>

</template>