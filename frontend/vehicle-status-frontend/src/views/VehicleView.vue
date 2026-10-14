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

// ======================================================
// Search block
// ======================================================

const searchKeyword = ref('')
const selectedStatus = ref('')

// ======================================================
// obset page
// ======================================================

const currentPage = ref(1)
const pageSize = ref(2)
const totalCount = ref(0)
const totalPages = ref(0)


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
    const response = await api.get('/vehicles/paged', {
      params: {
        keyword: searchKeyword.value.trim(),
        status: selectedStatus.value,
        page: currentPage.value,
        pageSize: pageSize.value
      }
    })

    vehicles.value = response.data.items
    totalCount.value = response.data.totalCount
    totalPages.value = response.data.totalPages

    console.log('Pagination:', response.data)
  }
  catch (error) {
    console.error('Load vehicles error:', error)

    if (error.response?.status === 401) {
      ElMessage.error('Login expired. Please login again.')
      logout()
    }
    else {
      ElMessage.error('Failed to load vehicles.')
    }
  }
  finally {
    loading.value = false
  }
}

// ======================================================
// SearchKeywowrd//offset page//check detail
// ======================================================

async function searchVehicles() {
  currentPage.value = 1

  await loadVehicles()
}

async function resetSearch() {
  searchKeyword.value = ''
  selectedStatus.value = ''
  currentPage.value = 1

  await loadVehicles()
}


async function handleCurrentChange(newPage) {
  currentPage.value = newPage

  await loadVehicles()
}

async function handleSizeChange(newSize) {
  pageSize.value = newSize

  currentPage.value = 1

  await loadVehicles()
}


//vhicleDetail的具体内容
function viewVehicleDetail(id) {
  console.log('Parent received ID:', id)

  router.push(`/vehicles/${id}`)
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

    if (currentPage.value > Math.max(1, totalPages.value)) {
      currentPage.value = Math.max(1, totalPages.value)

      await loadVehicles()

    }

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
  console.log('VehicleView mounted')

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


        <!-- ============================================= -->
        <!-- Search Keyword -->
        <!-- ============================================= -->

        
        <div class="search-area">

          <el-input
            v-model="searchKeyword"
            placeholder="Search plate number or model"
            clearable
            style="width: 260px"
            @keyup.enter="searchVehicles"
          />

          <el-button
            type="primary"
            @click="searchVehicles"
          >
            Search
          </el-button>

          <el-button @click="resetSearch">
            Reset
          </el-button>


          <el-select
            v-model="selectedStatus"
            placeholder="Select status"
            style="width: 160px"
          >
            <el-option
              label="All Status"
              value=""
            />

            <el-option
              label="Active"
              value="Active"
            />

            <el-option
              label="Inactive"
              value="Inactive"
            />
          </el-select>
        </div>

        <div class="pagination-area">
          <el-pagination
            v-model:current-page="currentPage"
            v-model:page-size="pageSize"
            :total="totalCount"
            :page-sizes="[2, 5, 10, 20]"
            layout="total, sizes, prev, pager, next"
            background
            @current-change="handleCurrentChange"
            @size-change="handleSizeChange"
          />
        </div>

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

          :current-page="
            currentPage
          "

          :page-size="
            pageSize
          "

          @detail="
          viewVehicleDetail
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


<style scoped>
.pagination-area {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
  padding: 12px 0;
}
</style>