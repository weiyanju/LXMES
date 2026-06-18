import { createRouter, createWebHashHistory } from 'vue-router'
import { generateRoutes } from '../util/dynamicRoutes'

// 公共路由（所有登录用户都能访问）
const constantRoutes = [
  {
    path: '/login',
    name: 'login',
    component: () => import('../views/Login.vue')
  },
  {
    path: '/',
    name: '主页',          // 添加这一行
    component: () => import('../layout/index.vue'),
    redirect: '/index',
    children: [
      { path: '/index', name: '首页', component: () => import('../views/index/index.vue') },
      { path: '/userCenter', name: '个人中心', component: () => import('../views/userCenter/index') },
      // 临时添加用户管理静态路由
      { path: '/sys/user', name: '用户管理', component: () => import('../views/sys/user/index.vue') }
    ]
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes: constantRoutes
})

router.beforeEach(async (to, from, next) => {
  const token = sessionStorage.getItem('access_token')
  if (to.path === '/login') return next()
  if (!token) return next('/login')

  const hasRoutes = sessionStorage.getItem('hasRoutes') === 'true'
  if (!hasRoutes) {
    const menuList = JSON.parse(sessionStorage.getItem('menuList') || '[]')
    if (menuList.length) {
      const asyncRoutes = generateRoutes(menuList)
      // 将每个路由作为 '主页' 的子路由添加
      asyncRoutes.forEach(route => {
        router.addRoute('主页', route)
      })
      sessionStorage.setItem('hasRoutes', 'true')
      // 重新跳转到目标路由，确保路由生效
      return next({ ...to, replace: true })
    }
  }
  next()
})

export default router