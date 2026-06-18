export function generateRoutes(menuTree) {
    const routes = []
    for (const menu of menuTree) {
        if (menu.menu_type === 'F') continue

        // 目录：递归子菜单，不生成自身路由
        if (menu.menu_type === 'M') {
            if (menu.children && menu.children.length) {
                const childRoutes = generateRoutes(menu.children)
                routes.push(...childRoutes)
            }
            continue
        }

        // 菜单（C 类型）
        if (!menu.component) {
            console.warn(`菜单 ${menu.name} 缺少 component，跳过`)
            continue
        }

        // 关键：将路径开头的 / 去掉，变成相对路径
        let relativePath = menu.path
        if (relativePath.startsWith('/')) {
            relativePath = relativePath.slice(1)
        }

        const route = {
            path: relativePath,
            name: menu.name,
            component: () => import(`../views/${menu.component}`),
            meta: { title: menu.name, icon: menu.icon, perms: menu.perms }
        }

        if (menu.children && menu.children.length) {
            route.children = generateRoutes(menu.children)
        }
        routes.push(route)
    }
    return routes
}