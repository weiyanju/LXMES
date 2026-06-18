import json
from datetime import datetime

from django.http import JsonResponse
from django.views import View

from menu.models import SysMenu, SysMenuSerializer, SysRoleMenu
from role.models import SysUserRole  # 🔥 修复：补上缺失的导入


class TreeListView(View):
    def buildTreeMenu(self, sysMenuList):
        resultMenuList = []
        for menu in sysMenuList:
            for e in sysMenuList:
                if e.parent_id == menu.id:
                    if not hasattr(menu, "children"):
                        menu.children = []
                    menu.children.append(e)
            if menu.parent_id == 0:
                resultMenuList.append(menu)
        return resultMenuList

    def get(self, request):
        user = request.user

        # 获取用户的所有角色ID
        try:
            user_roles = SysUserRole.objects.filter(user=user)
            # 检查是否为超级管理员
            is_admin = any(
                ur.role and ur.role.code == "admin"
                for ur in user_roles
            )
        except:
            is_admin = True

        if is_admin:
            # 超级管理员：返回所有菜单
            menuQuerySet = SysMenu.objects.order_by("order_num")
        else:
            # 普通用户：根据角色获取菜单
            role_ids = user_roles.values_list('role_id', flat=True)
            if not role_ids:
                # 没有角色，返回空菜单
                menuQuerySet = SysMenu.objects.none()
            else:
                # 获取这些角色关联的菜单ID（去重）
                menu_ids = SysRoleMenu.objects.filter(role_id__in=role_ids).values_list('menu_id', flat=True).distinct()
                if not menu_ids:
                    menuQuerySet = SysMenu.objects.none()
                else:
                    menuQuerySet = SysMenu.objects.filter(id__in=menu_ids).order_by("order_num")
        sysMenuList = self.buildTreeMenu(menuQuerySet)
        serializerMenuList = [SysMenuSerializer(m).data for m in sysMenuList]
        return JsonResponse({'code': 200, 'treeList': serializerMenuList})

class SaveView(View):
    def post(self, request):
        data = json.loads(request.body.decode("utf-8"))
        # 处理 parent_id：空字符串或 None 转为 0
        parent_id = data.get('parent_id', 0)
        if parent_id == '' or parent_id is None:
            parent_id = 0

        if data['id'] == -1:
            obj_sysMenu = SysMenu(
                name=data['name'],
                icon=data['icon'],
                parent_id=parent_id,   # 使用处理后的值
                order_num=data['order_num'],
                path=data['path'],
                component=data['component'],
                menu_type=data['menu_type'],
                perms=data['perms'],
                remark=data['remark']
            )
            obj_sysMenu.create_time = datetime.now().date()
            obj_sysMenu.save()
        else:
            obj_sysMenu = SysMenu(
                id=data['id'],
                name=data['name'],
                icon=data['icon'],
                parent_id=parent_id,   # 使用处理后的值
                order_num=data['order_num'],
                path=data['path'],
                component=data['component'],
                menu_type=data['menu_type'],
                perms=data['perms'],
                remark=data['remark'],
                create_time=data['create_time'],
                update_time=data['update_time']
            )
            obj_sysMenu.update_time = datetime.now().date()
            obj_sysMenu.save()
        return JsonResponse({'code': 200})


class ActionView(View):
    def get(self, request):
        id = request.GET.get("id")
        menu_object = SysMenu.objects.get(id=id)
        return JsonResponse({'code': 200, 'menu': SysMenuSerializer(menu_object).data})

    def delete(self, request):
        id = json.loads(request.body.decode("utf-8"))
        if SysMenu.objects.filter(parent_id=id).count() > 0:
            return JsonResponse({'code': 500, 'msg': '请先删除子菜单！'})
        else:
            SysRoleMenu.objects.filter(menu_id=id).delete()
            SysMenu.objects.get(id=id).delete()
            return JsonResponse({'code': 200})