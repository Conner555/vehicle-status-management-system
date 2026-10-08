import {
  createRouter,
  createWebHistory
} from 'vue-router'

import LoginView
  from '../views/LoginView.vue'

import VehicleView
  from '../views/VehicleView.vue'


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

router.beforeEach(
  (to, from, next) => {

    const token =
      localStorage.getItem('token')


    if (
      to.meta.requiresAuth &&
      !token
    ) {

      next('/login')

      return
    }


    if (
      to.path === '/login' &&
      token
    ) {

      next('/vehicles')

      return
    }


    next()
  }
)


export default router