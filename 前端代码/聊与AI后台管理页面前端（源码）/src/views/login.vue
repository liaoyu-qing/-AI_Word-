<template>
  <div class="login-container">
    <div class="login-card">
      <div class="login-header">
        <img src="https://via.placeholder.com/50" alt="Logo" class="logo" />
        <h1>欢迎登录</h1>
        <p>请输入您的账号和密码</p>
      </div>

      <el-form
        ref="loginForm"
        :model="form"
        :rules="rules"
        class="login-form"
        @keyup.enter="handleLogin"
      >
        <el-form-item prop="username">
          <el-input
            v-model="form.username"
            placeholder="用户名"
            prefix-icon="User"
            size="large"
            clearable
          />
        </el-form-item>
        <div style="height: 40px"></div>
        <el-form-item prop="password">
          <el-input
            v-model="form.password"
            placeholder="密码"
            prefix-icon="Lock"
            size="large"
            show-password
            type="password"
          />
        </el-form-item>

        <div class="login-options">
          <el-checkbox v-model="form.remember">记住密码</el-checkbox>
        </div>

        <el-button
          type="primary"
          size="large"
          class="login-btn"
          :loading="loading"
          @click="handleLogin"
        >
          登 录
        </el-button>
      </el-form>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from "vue";
import { ElMessage } from "element-plus";
import router from "../router";

const loading = ref(false);
const loginForm = ref(null);

const form = reactive({
  username: "",
  password: "",
  remember: false,
});

const rules = reactive({
  username: [
    { required: true, message: "请输入用户名", trigger: "blur" },
    { min: 3, max: 16, message: "长度在 3 到 16 个字符", trigger: "blur" },
  ],
  password: [
    { required: true, message: "请输入密码", trigger: "blur" },
    { min: 6, max: 20, message: "长度在 6 到 20 个字符", trigger: "blur" },
  ],
});

// 处理登录
const handleLogin = () => {
  console.log(111);
  localStorage.setItem("token", "scseddw2s");
  loginForm.value.validate((valid) => {
    if (!valid) return;
    loading.value = true;
    // 模拟登录请求
    setTimeout(() => {
      loading.value = false;

      if (form.remember) {
        // 记住密码 - 实际项目中应该更安全地处理
        localStorage.setItem(
          "loginInfo",
          JSON.stringify({
            username: form.username,
            remember: true,
          })
        );
      } else {
        localStorage.removeItem("loginInfo");
      }

      ElMessage.success("登录成功");
      // 这里应该跳转到首页
      router.push("/index");
      console.log("登录信息:", form);
    }, 1500);
  });
};

// 初始化记住的用户名
onMounted(() => {
  const savedInfo = localStorage.getItem("loginInfo");
  if (savedInfo) {
    const { username, remember } = JSON.parse(savedInfo);
    form.username = username;
    form.remember = remember;
  }
  const goIndex = localStorage.getItem("token");
  if (goIndex) {
    router.push("/index");
  }
});
</script>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background-color: #f0f2f5;
  background-image: url("https://source.unsplash.com/random/1600x900/?nature");
  background-size: cover;
  background-position: center;
}

.login-card {
  width: 420px;
  padding: 40px;
  background-color: rgba(255, 255, 255, 0.95);
  border-radius: 12px;
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.1);
  backdrop-filter: blur(8px);
}

.login-header {
  text-align: center;
  margin-bottom: 30px;
}

.login-header .logo {
  margin-bottom: 15px;
}

.login-header h1 {
  font-size: 24px;
  color: #333;
  margin-bottom: 8px;
}

.login-header p {
  font-size: 14px;
  color: #999;
}

.login-form {
  margin-top: 20px;
}

.login-options {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.login-btn {
  width: 100%;
  margin-top: 10px;
  height: 48px;
  font-size: 16px;
  letter-spacing: 2px;
}

.login-footer {
  margin-top: 30px;
  text-align: center;
}

.social-login {
  display: flex;
  justify-content: center;
  gap: 20px;
  margin: 20px 0;
  color: #666;
  cursor: pointer;
}

.social-login .el-icon:hover {
  color: var(--el-color-primary);
}

.register-tip {
  font-size: 14px;
  color: #999;
  margin-top: 20px;
}

:deep(.el-input__wrapper) {
  border-radius: 8px;
  height: 48px;
}

:deep(.el-divider__text) {
  background-color: transparent;
  color: #999;
  font-size: 12px;
}
</style>