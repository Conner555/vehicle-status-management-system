<script setup>

import {
  ref,
  onMounted
} from 'vue'

import {
  useRouter
} from 'vue-router'

import {
  ElMessage,
  ElMessageBox
} from 'element-plus'

import api from '../services/api'

import VehicleTable
  from '../components/VehicleTable.vue'

import VehicleDialog
  from '../components/VehicleDialog.vue'


// ======================================================
// Router
// ======================================================

const router = useRouter()


// ======================================================
// Current User
// ======================================================

const currentUsername = ref(
  localStorage.getItem('username') || ''
)

const currentRole = ref(
  localStorage.getItem('role') || ''
)


// ======================================================
// Vehicle Data
// ======================================================

const vehicles = ref([])

const plateNumber = ref('')
const model = ref('')
const status = ref('')

const editingId = ref(null)

const dialogVisible = ref(false)

const loading = ref(false)


// ======================================================
// Logout
// ======================================================

function logout() {

  localStorage.removeItem('token')
  localStorage.removeItem('username')
  localStorage.removeItem('role')

  ElMessage.success(
    'Logged out successfully.'
  )

  router.push('/login')
}


// ======================================================
// GET Vehicles
// ======================================================

async function loadVehicles() {

  loading.value = true

  try {

    const response =
      await api.get('/vehicles')

    vehicles.value =
      response.data

  }
  catch (error) {

    const statusCode =
      error.response?.status

    console.error(
      'Load vehicles error:',
      statusCode,
      error.response?.data
    )

    if (statusCode === 401) {

      ElMessage.error(
        'Login expired. Please login again.'
      )

      logout()
    }
    else {

      ElMessage.error(
        'Failed to load vehicles.'
      )
    }

  }
  finally {

    loading.value = false
  }
}


// ======================================================
// Open Add Dialog
// ======================================================

function openAddDialog() {

  resetForm()

  dialogVisible.value = true
}


// ======================================================
// POST Vehicle
// ======================================================

async function addVehicle() {

  if (
    !plateNumber.value ||
    !model.value ||
    !status.value
  ) {

    ElMessage.warning(
      'Please complete all fields.'
    )

    return
  }


  const newVehicle = {

    plateNumber:
      plateNumber.value,

    model:
      model.value,

    status:
      status.value
  }


  try {

    await api.post(
      '/vehicles',
      newVehicle
    )

    await loadVehicles()

    resetForm()

    dialogVisible.value = false

    ElMessage.success(
      'Vehicle created successfully.'
    )

  }
  catch (error) {

    handleVehicleError(
      error,
      'Create'
    )
  }
}


// ======================================================
// Edit Vehicle
// ======================================================

function editVehicle(vehicle) {

  editingId.value =
    vehicle.id

  plateNumber.value =
    vehicle.plateNumber

  model.value =
    vehicle.model

  status.value =
    vehicle.status

  dialogVisible.value = true
}


// ======================================================
// PUT Vehicle
// ======================================================

async function updateVehicle() {

  if (
    editingId.value === null
  ) {

    ElMessage.warning(
      'No vehicle selected.'
    )

    return
  }


  if (
    !plateNumber.value ||
    !model.value ||
    !status.value
  ) {

    ElMessage.warning(
      'Please complete all fields.'
    )

    return
  }


  const updatedVehicle = {

    plateNumber:
      plateNumber.value,

    model:
      model.value,

    status:
      status.value
  }


  try {

    await api.put(
      `/vehicles/${editingId.value}`,
      updatedVehicle
    )

    await loadVehicles()

    resetForm()

    dialogVisible.value = false

    ElMessage.success(
      'Vehicle updated successfully.'
    )

  }
  catch (error) {

    handleVehicleError(
      error,
      'Update'
    )
  }
}


// ======================================================
// DELETE Vehicle
// ======================================================

async function deleteVehicle(id) {

  try {

    await ElMessageBox.confirm(
      'Are you sure you want to delete this vehicle?',
      'Delete Vehicle',
      {
        confirmButtonText:
          'Delete',

        cancelButtonText:
          'Cancel',

        type:
          'warning'
      }
    )


    await api.delete(
      `/vehicles/${id}`
    )


    await loadVehicles()


    ElMessage.success(
      'Vehicle deleted successfully.'
    )

  }
  catch (error) {

    // 点击 Cancel / Close
    if (
      error === 'cancel' ||
      error === 'close'
    ) {
      return
    }


    handleVehicleError(
      error,
      'Delete'
    )
  }
}


// ======================================================
// Reset Form
// ======================================================

function resetForm() {

  plateNumber.value = ''

  model.value = ''

  status.value = ''

  editingId.value = null
}


// ======================================================
// Common HTTP Error Handling
// ======================================================

function handleVehicleError(
  error,
  operation
) {

  const statusCode =
    error.response?.status


  console.error(
    `${operation} vehicle error:`,
    statusCode,
    error.response?.data
  )


  if (statusCode === 400) {

    ElMessage.error(
      'Invalid vehicle data.'
    )
  }

  else if (statusCode === 401) {

    ElMessage.error(
      'Please login first.'
    )

    logout()
  }

  else if (statusCode === 403) {

    ElMessage.error(
      'You do not have permission.'
    )
  }

  else if (statusCode === 404) {

    ElMessage.error(
      'Vehicle not found.'
    )
  }

  else if (statusCode === 409) {

    ElMessage.error(
      'The operation conflicts with an existing vehicle or business rule.'
    )
  }

  else {

    ElMessage.error(
      `${operation} failed.`
    )
  }
}


// ======================================================
// Initial Load
// ======================================================

onMounted(() => {

  loadVehicles()

})

</script>


<template>

  <el-container
    class="app-layout"
  >

    <!-- ================================================= -->
    <!-- Sidebar -->
    <!-- ================================================= -->

    <el-aside
      width="220px"
      class="sidebar"
    >

      <div class="logo">

        Vehicle System

      </div>


      <el-menu
        default-active="vehicles"
        class="side-menu"
      >

        <el-menu-item
          index="dashboard"
        >
          Dashboard
        </el-menu-item>


        <el-menu-item
          index="vehicles"
        >
          Vehicles
        </el-menu-item>


        <el-menu-item
          index="orders"
        >
          Rental Orders
        </el-menu-item>


        <el-menu-item
          index="reviews"
        >
          Reviews
        </el-menu-item>


        <el-menu-item
          v-if="
            currentRole === 'Admin'
          "
          index="users"
        >
          Users
        </el-menu-item>

      </el-menu>

    </el-aside>


    <!-- ================================================= -->
    <!-- Right Side -->
    <!-- ================================================= -->

    <el-container>


      <!-- =============================================== -->
      <!-- Header -->
      <!-- =============================================== -->

      <el-header
        class="top-header"
      >

        <h2
          class="page-title"
        >
          Vehicle Management
        </h2>


        <div
          class="user-area"
        >

          <span>

            {{ currentUsername }}

          </span>


          <el-tag
            :type="
              currentRole === 'Admin'
                ? 'danger'
                : 'info'
            "
          >

            {{ currentRole }}

          </el-tag>


          <el-button
            size="small"
            @click="logout"
          >
            Logout
          </el-button>

        </div>

      </el-header>


      <!-- =============================================== -->
      <!-- Main Content -->
      <!-- =============================================== -->

      <el-main
        class="main-content"
      >

        <!-- Toolbar -->

        <div
          class="toolbar"
        >

          <div>

            <h3>
              Vehicles
            </h3>

            <p
              class="section-description"
            >
              Manage vehicle
              information and status.
            </p>

          </div>


          <el-button
            v-if="
              currentRole === 'Admin'
            "
            type="primary"
            @click="openAddDialog"
          >
            Add Vehicle
          </el-button>

        </div>


        <!-- ============================================= -->
        <!-- Vehicle Table -->
        <!-- ============================================= -->

        <VehicleTable

          :vehicles="
            vehicles
          "

          :loading="
            loading
          "

          :current-role="
            currentRole
          "

          @edit="
            editVehicle
          "

          @delete="
            deleteVehicle
          "

        />


      </el-main>

    </el-container>


    <!-- ================================================= -->
    <!-- Vehicle Dialog -->
    <!-- ================================================= -->

    <VehicleDialog

      v-model:visible="
        dialogVisible
      "

      v-model:plate-number="
        plateNumber
      "

      v-model:model="
        model
      "

      v-model:status="
        status
      "

      :editing-id="
        editingId
      "

      @save="
        editingId === null
          ? addVehicle()
          : updateVehicle()
      "

    />

  </el-container>

</template>