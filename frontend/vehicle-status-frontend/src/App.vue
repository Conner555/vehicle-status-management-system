<template>

  <router-view />

</template>


<style>

/* ===================================================== */
/* Global */
/* ===================================================== */

* {
  box-sizing: border-box;
}


body {
  margin: 0;

  font-family:
    Arial,
    Helvetica,
    sans-serif;

  background:
    #f5f7fa;
}


/* ===================================================== */
/* Login */
/* ===================================================== */

.login-page {

  width: 100%;
  height: 100vh;

  display: flex;

  justify-content: center;
  align-items: center;

  background:
    #f2f4f7;
}


.login-card {

  width: 420px;

  padding: 20px;
}


.login-title {

  text-align: center;

  margin-top: 5px;
  margin-bottom: 5px;
}


.login-subtitle {

  text-align: center;

  color:
    #909399;

  margin-bottom:
    25px;
}


.login-card .el-input {

  margin-bottom:
    16px;
}


.login-button {

  width: 100%;
}


.login-message {

  text-align: center;

  color:
    #f56c6c;

  margin-top:
    15px;
}


/* ===================================================== */
/* Main Layout */
/* ===================================================== */

.app-layout {

  height: 100vh;
}


.sidebar {

  background:
    #ffffff;

  border-right:
    1px solid #e4e7ed;
}


.logo {

  height: 64px;

  display: flex;

  align-items: center;
  justify-content: center;

  font-size:
    18px;

  font-weight:
    bold;

  border-bottom:
    1px solid #e4e7ed;
}


.side-menu {

  border-right: none;
}


/* ===================================================== */
/* Header */
/* ===================================================== */

.top-header {

  height: 64px;

  display: flex;

  align-items: center;
  justify-content: space-between;

  background:
    #ffffff;

  border-bottom:
    1px solid #e4e7ed;
}


.page-title {

  margin: 0;

  font-size:
    20px;
}


.user-area {

  display: flex;

  align-items: center;

  gap: 12px;
}


/* ===================================================== */
/* Main */
/* ===================================================== */

.main-content {

  padding: 24px;

  background:
    #f5f7fa;
}


.toolbar {

  display: flex;

  align-items: center;
  justify-content: space-between;

  margin-bottom:
    20px;
}


.toolbar h3 {

  margin: 0;

  font-size:
    22px;
}


.section-description {

  margin-top:
    6px;

  margin-bottom:
    0;

  color:
    #909399;

  font-size:
    14px;
}


.table-card {

  border-radius:
    8px;
}

</style>