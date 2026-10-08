<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'

import api from '../services/api'
import LoginForm from '../components/LoginForm.vue'


const router = useRouter()

const username = ref('')
const password = ref('')
const loginMessage = ref('')


async function login() {

  if (
    !username.value ||
    !password.value
  ) {
    ElMessage.warning(
      'Please enter username and password.'
    )

    return
  }


  const loginData = {
    username: username.value,
    password: password.value
  }


  try {

    const response = await api.post(
      '/auth/login',
      loginData
    )


    localStorage.setItem(
      'token',
      response.data.token
    )

    localStorage.setItem(
      'username',
      response.data.username
    )

    localStorage.setItem(
      'role',
      response.data.role
    )


    ElMessage.success(
      `Welcome, ${response.data.username}`
    )


    // 登录成功后跳转 Vehicles
    router.push('/vehicles')

  }
  catch (error) {

    console.error(
      error.response?.status,
      error.response?.data
    )

    loginMessage.value =
      'Invalid username or password.'

    ElMessage.error(
      'Login failed.'
    )
  }
}
</script>


<template>

  <LoginForm
    v-model:username="username"
    v-model:password="password"
    :login-message="loginMessage"
    @login="login"
  />

</template>