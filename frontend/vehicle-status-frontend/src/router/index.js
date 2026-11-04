
import {
  createRouter,
  createWebHistory
} from 'vue-router'

import LoginView from '../views/LoginView.vue'
import VehicleView from '../views/VehicleView.vue'
import VehicleDetailView from '../views/VehicleDetailView.vue'
import DashboardView from '../views/DashboardView.vue'

import AdminLayout from '../layouts/AdminLayout.vue'
import AdminOrdersView from '../views/AdminOrdersView.vue'
import InspectionView from '../views/InspectionView.vue'

import OperatorLayout from '../layouts/OperatorLayout.vue'
import OperatorDashboardView from '../views/OperatorDashboardView.vue'
import component from 'element-plus/es/components/tree-select/src/tree-select-option.mjs'
import MyInspectionsView from '@/views/MyInspectionsView.vue'
import InspectionRemindersView from '../views/InspectionRemindersView.vue'




function getHomePath(role) {
  if (role === 'Admin') return '/admin/dashboard'
  if (role === 'Operator') return '/operator/dashboard'
  return '/vehicles'
}

const routes = [
  {
    path: '/',
    redirect: () => {
      return getHomePath(localStorage.getItem('role'))
    }
  },

  {
    path: '/login',
    name: 'Login',
    component: LoginView
  },

  // 保留 V2 原有页面
  {
    path: '/vehicles',
    name: 'Vehicles',
    component: VehicleView,
    meta: {
      requiresAuth: true
    }
  },

  {
    path: '/vehicles/:id',
    name: 'VehicleDetail',
    component: VehicleDetailView,
    meta: {
      requiresAuth: true
    }
  },

  {
    path: '/dashboard',
    redirect: '/admin/dashboard'
  },

  // V3.6 管理后台
  {
    path: '/admin',
    component: AdminLayout,
    meta: {
      requiresAuth: true,
      roles: ['Admin']
    },
    children: [
      {
        path: '',
        redirect: '/admin/dashboard'
      },
      {
        path: 'dashboard',
        name: 'AdminDashboard',
        component: DashboardView
      },
      {
        path: 'orders',
        name: 'AdminOrders',
        component: AdminOrdersView
      },
      {
        path: 'inspections',
        name: 'AdminInspections',
        component: InspectionView
      }
    ]
  },

  {
    path: '/:pathMatch(.*)*',
    redirect: '/'
  },
  //OPEARATOR管理者看板

  {
    path: '/operator',
    component: OperatorLayout,
    meta: {
      requiresAuth: true,
      roles: ['Operator']
    },
    children: [
      {
        path: '',
        redirect: '/operator/dashboard'
      },
      {
        path: 'dashboard',
        name: 'OperatorDashboard',
        component: OperatorDashboardView
      },
      {
        path: 'inspections',
        name: 'OperatorInspections',
        component: InspectionView
      },
      {
        path: 'my-inspections',
        name: 'OperatorMyInspections',
        component: MyInspectionsView
      },
      {
        path: 'reminders',
        name: 'OperatorReminders',
        component: InspectionRemindersView
      }
    ]
  }

]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to) => {
  const token = localStorage.getItem('token')
  const role = localStorage.getItem('role')

  if (to.meta.requiresAuth && !token) {
    return {
      path: '/login',
      query: { redirect: to.fullPath }
    }
  }

  if (
    to.meta.roles &&
    !to.meta.roles.includes(role)
  ) {
    return getHomePath(role)
  }

  if (to.path === '/login' && token) {
    return getHomePath(role)
  }

  return true
})

export default router
