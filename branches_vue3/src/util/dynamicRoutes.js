export function generateRoutes(menuTree) {
    const routes = []
    for (const menu of menuTree) {
        // 跳过按钮类型
        if (menu.menu_type === 'F') continue

        // 目录：递归子菜单，不生成自身路由
        if (menu.menu_type === 'M') {
            if (menu.children && menu.children.length) {
                const childRoutes = generateRoutes(menu.children)
                routes.push(...childRoutes)
            }
            continue
        }

        // 菜单（C 类型）必须有组件路径
        if (!menu.component) {
            console.warn(`菜单 ${menu.name} 缺少 component，跳过`)
            continue
        }

        // 处理路径：确保以 / 开头，但不重复
        let routePath = menu.path
        if (!routePath.startsWith('/')) {
            routePath = '/' + routePath
        }

        const route = {
            path: routePath,
            name: menu.name,
            // 组件路径自动补全 .vue 后缀（如果未提供）
            component: () => import(`../views/${menu.component}.vue`),
            meta: {
                title: menu.name,
                icon: menu.icon,
                perms: menu.perms
            }
        }

        if (menu.children && menu.children.length) {
            route.children = generateRoutes(menu.children)
        }

        routes.push(route)
    }
    return routes
}