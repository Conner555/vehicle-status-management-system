import {
  createRouter,
  createWebHistory
} from 'vue-router'

import LoginView
  from '../views/LoginView.vue'

import VehicleView
  from '../views/VehicleView.vue'

import VehicleDetailView
  from '../views/VehicleDetailView.vue'

import DashboardView 
  from '../views/DashboardView.vue'


const routes = [

  {
    path: '/',
    redirect: '/vehicles'
  },

  {
    path: '/login',
    component: LoginView
  },

  {
    path: '/vehicles',
    component: VehicleView,

    meta: {
      requiresAuth: true
    }
  },

  {
    path: '/dashboard',
    name: 'Dashboard',
    component: DashboardView,
    meta: {
      requiresAuth: true
    }
  },

  {
    path: '/vehicles/:id',
    name: 'VehicleDetail',
    component:  VehicleDetailView,
    
    meta:{
      requiresAuth :true
    }
  }

]


const router = createRouter({

  history:
    createWebHistory(),

  routes
})


// ======================================================
// Navigation Guard
// ======================================================


router.beforeEach((to) => {

  const token = localStorage.getItem('token')

  // 未登录，跳转登录页面
  if (to.meta.requiresAuth && !token) {
    return '/login'
  }

  // 已登录，不需要重复进入登录页面
  if (to.path === '/login' && token) {
    return '/vehicles'
  }

  // 允许正常跳转
  return true
})



export default router