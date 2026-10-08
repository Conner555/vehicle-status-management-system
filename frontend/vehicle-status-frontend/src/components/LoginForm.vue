<script setup>
defineProps({
  username: {
    type: String,
    required: true
  },

  password: {
    type: String,
    required: true
  },

  loginMessage: {
    type: String,
    default: ''
  }
})

const emit = defineEmits([
  'update:username',
  'update:password',
  'login'
])
</script>


<template>

  <div class="login-page">

    <el-card class="login-card">

      <h2 class="login-title">
        Vehicle Management System
      </h2>

      <p class="login-subtitle">
        Please sign in to continue
      </p>


      <el-input
        :model-value="username"
        placeholder="Username"
        size="large"
        @update:model-value="
          value => emit(
            'update:username',
            value
          )
        "
      />


      <el-input
        :model-value="password"
        type="password"
        placeholder="Password"
        show-password
        size="large"
        @update:model-value="
          value => emit(
            'update:password',
            value
          )
        "
        @keyup.enter="
          emit('login')
        "
      />


      <el-button
        type="primary"
        size="large"
        class="login-button"
        @click="
          emit('login')
        "
      >
        Login
      </el-button>


      <p
        v-if="loginMessage"
        class="login-message"
      >
        {{ loginMessage }}
      </p>

    </el-card>

  </div>

</template>