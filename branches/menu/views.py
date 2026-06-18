import json
from rest_framework import viewsets, status
from rest_framework.decorators import action
from rest_framework.response import Response
from rest_framework.permissions import IsAuthenticated
from .serializers import SysMenuSerializer
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
class SysMenuViewSet(viewsets.ModelViewSet):
    """
    菜单管理 ViewSet
    提供列表、新增、详情、修改、删除、批量删除、树形菜单、根据角色获取菜单树
    """
    queryset = SysMenu.objects.all().order_by('order_num')
    serializer_class = SysMenuSerializer
    permission_classes = [IsAuthenticated]

    def get_queryset(self):
        queryset = super().get_queryset()
        # 搜索：根据菜单名称模糊查询
        search = self.request.query_params.get('query', None)
        if search:
            queryset = queryset.filter(name__icontains=search)
        return queryset

    def perform_destroy(self, instance):
        # 删除前检查是否有子菜单
        if SysMenu.objects.filter(parent_id=instance.id).exists():
            from rest_framework.exceptions import ValidationError
            raise ValidationError({'detail': '请先删除子菜单！'})
        # 删除角色菜单关联
        SysRoleMenu.objects.filter(menu_id=instance.id).delete()
        instance.delete()

    # ---------- 批量删除 ----------
    @action(detail=False, methods=['delete'], url_path='batch-delete')
    def batch_delete(self, request):
        ids = request.data.get('ids', [])
        if not ids:
            return Response({'detail': '请提供要删除的ID列表'}, status=status.HTTP_400_BAD_REQUEST)
        # 检查是否有菜单包含子菜单
        for menu_id in ids:
            if SysMenu.objects.filter(parent_id=menu_id).exists():
                menu = SysMenu.objects.get(id=menu_id)
                return Response(
                    {'detail': f'菜单“{menu.name}”下存在子菜单，请先删除子菜单！'},
                    status=status.HTTP_400_BAD_REQUEST
                )
        SysRoleMenu.objects.filter(menu_id__in=ids).delete()
        SysMenu.objects.filter(id__in=ids).delete()
        return Response(status=status.HTTP_204_NO_CONTENT)

    # ---------- 树形菜单列表（用于菜单管理页面展示）----------
    @action(detail=False, methods=['get'], url_path='tree')
    def tree_list(self, request):
        """返回完整的树形菜单，不区分用户权限"""
        menus = self.get_queryset()
        tree = self._build_tree(menus)
        serializer = self.get_serializer(tree, many=True)
        return Response(serializer.data)

    # ---------- 根据当前用户返回菜单树（用于前端动态路由）----------
    @action(detail=False, methods=['get'], url_path='user-tree')
    def user_menu_tree(self, request):
        """根据当前登录用户的角色返回菜单树"""
        user = request.user
        # 获取用户角色
        user_roles = SysUserRole.objects.filter(user=user)
        is_admin = any(ur.role and ur.role.code == 'admin' for ur in user_roles)

        if is_admin:
            menus = SysMenu.objects.all().order_by('order_num')
        else:
            role_ids = user_roles.values_list('role_id', flat=True)
            if not role_ids:
                menus = SysMenu.objects.none()
            else:
                menu_ids = SysRoleMenu.objects.filter(role_id__in=role_ids).values_list('menu_id', flat=True).distinct()
                menus = SysMenu.objects.filter(id__in=menu_ids).order_by('order_num')

        tree = self._build_tree(menus)
        serializer = self.get_serializer(tree, many=True)
        return Response(serializer.data)

    # ---------- 根据指定角色ID返回菜单树（用于角色分配权限回显）----------
    @action(detail=False, methods=['get'], url_path='role-tree')
    def role_menu_tree(self, request):
        """根据传入的 role_id 返回该角色拥有的菜单树"""
        role_id = request.query_params.get('role_id')
        if not role_id:
            return Response({'detail': '缺少 role_id 参数'}, status=status.HTTP_400_BAD_REQUEST)

        # 获取该角色拥有的菜单ID
        menu_ids = SysRoleMenu.objects.filter(role_id=role_id).values_list('menu_id', flat=True)
        menus = SysMenu.objects.filter(id__in=menu_ids).order_by('order_num')
        tree = self._build_tree(menus)
        serializer = self.get_serializer(tree, many=True)
        return Response(serializer.data)

    # ---------- 内部方法：构建树形结构 ----------
    def _build_tree(self, menus):
        """将菜单查询集转换为树形结构"""
        menu_dict = {menu.id: menu for menu in menus}
        tree = []
        for menu in menus:
            if menu.parent_id and menu.parent_id in menu_dict:
                parent = menu_dict[menu.parent_id]
                if not hasattr(parent, 'children'):
                    parent.children = []
                parent.children.append(menu)
            else:
                tree.append(menu)
        # 按 order_num 排序
        tree.sort(key=lambda x: x.order_num or 0)
        return tree