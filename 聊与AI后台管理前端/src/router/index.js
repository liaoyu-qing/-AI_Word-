import { createRouter, createWebHistory } from 'vue-router'

// 导入你的 Vue 组件（按需引入）

const Index = () => import('../views/Index.vue')

const Login = () => import('../views/login.vue')
const routes = [
  {
    path: '/',
    name: 'Index',
    component: Index
  },
 
  {
    path: '/login',
    name: 'login',
    component: Login,
  },

  {
    path: '/index',
    name: 'index',
    component: Index,
  }
]

const router = createRouter({
  history: createWebHistory(),  // 使用 HTML5 History 模式（需服务器支持）
  routes
})

export default router