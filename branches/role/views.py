import json
from datetime import datetime

from django.core.paginator import Paginator
from django.http import JsonResponse
from django.shortcuts import render
from django.views import View

from role.models import SysRoleSerializer, SysUserRole
from rest_framework import viewsets, status
from rest_framework.decorators import action
from rest_framework.response import Response
from rest_framework.permissions import IsAuthenticated
from .models import SysRole
from .serializers import SysRoleSerializer
from menu.models import SysRoleMenu

# Create your views here.
# 查询所有角色信息
class ListAllView(View):

    def get(self, request):
        obj_roleList = SysRole.objects.all().values()  # 转成字典
        roleList = list(obj_roleList)  # 把外层的容器转为List
        return JsonResponse(
            {'code': 200, 'roleList': roleList})

# 角色信息查询
class SearchView(View):

    def post(self, request):
        data = json.loads(request.body.decode("utf-8"))
        pageNum = data['pageNum']  # 当前页
        pageSize = data['pageSize']  # 每页大小
        query = data['query']  # 查询参数
        print(pageSize, pageNum)
        roleListPage = Paginator(SysRole.objects.filter(name__icontains=query), pageSize).page(pageNum)
        obj_roles = roleListPage.object_list.values()  # 转成字典
        roles = list(obj_roles)  # 把外层的容器转为List
        total = SysRole.objects.filter(name__icontains=query).count()
        return JsonResponse(
            {'code': 200, 'roleList': roles, 'total': total})

class SaveView(View):

    def post(self, request):
        data = json.loads(request.body.decode("utf-8"))
        if data['id'] == -1:  # 添加
            obj_sysRole = SysRole(name=data['name'], code=data['code'], remark=data['remark'])
            obj_sysRole.create_time = datetime.now().date()
            obj_sysRole.save()
        else:  # 修改
            obj_sysRole = SysRole(id=data['id'], name=data['name'], code=data['code'],
                                  remark=data['remark'], create_time=data['create_time'],
                                  update_time=data['update_time'])
            obj_sysRole.update_time = datetime.now().date()
            obj_sysRole.save()
        return JsonResponse({'code': 200})

# 角色基本操作
class ActionView(View):

    def get(self, request):
        """
        根据id获取角色信息
        :param request:
        :return:
        """
        id = request.GET.get("id")
        role_object = SysRole.objects.get(id=id)
        return JsonResponse({'code': 200, 'role': SysRoleSerializer(role_object).data})

    def delete(self, request):
        """
        删除操作
        :param request:
        :return:
        """
        idList = json.loads(request.body.decode("utf-8"))
        SysUserRole.objects.filter(role_id__in=idList).delete()
        SysRoleMenu.objects.filter(role_id__in=idList).delete()
        SysRole.objects.filter(id__in=idList).delete()
        return JsonResponse({'code': 200})


# 根据角色查询菜单权限
class MenusView(View):

    def get(self, request):
        id = request.GET.get("id")
        menuList = SysRoleMenu.objects.filter(role_id=id).values("menu_id")
        menuIdList = [m['menu_id'] for m in menuList]
        print("menuIdList=", menuIdList)
        return JsonResponse(
            {'code': 200, 'menuIdList': menuIdList})


# 角色权限授权
class GrantMenu(View):

    def post(self, request):
        data = json.loads(request.body.decode("utf-8"))
        role_id = data['id']
        menuIdList = data['menuIds']
        print(role_id, menuIdList)
        SysRoleMenu.objects.filter(role_id=role_id).delete()  # 删除角色菜单关联表中的指定角色数据
        for menuId in menuIdList:
            roleMenu = SysRoleMenu(role_id=role_id, menu_id=menuId)
            roleMenu.save()
        return JsonResponse({'code': 200})

class SysRoleViewSet(viewsets.ModelViewSet):
    """
    角色管理 ViewSet
    提供列表、新增、详情、修改、删除、批量删除、查询角色菜单、授权菜单等功能
    """
    queryset = SysRole.objects.all().order_by('id')
    serializer_class = SysRoleSerializer
    permission_classes = [IsAuthenticated]

    def get_queryset(self):
        queryset = super().get_queryset()
        # 搜索：根据 name 模糊查询
        search = self.request.query_params.get('query', None)
        if search:
            queryset = queryset.filter(name__icontains=search)
        return queryset

    # ---------- 批量删除 ----------
    @action(detail=False, methods=['delete'], url_path='batch-delete')
    def batch_delete(self, request):
        ids = request.data.get('ids', [])
        if not ids:
            return Response({'detail': '请提供要删除的ID列表'}, status=status.HTTP_400_BAD_REQUEST)
        # 删除角色前，先清除关联的用户角色和菜单权限
        SysUserRole.objects.filter(role_id__in=ids).delete()
        SysRoleMenu.objects.filter(role_id__in=ids).delete()
        SysRole.objects.filter(id__in=ids).delete()
        return Response(status=status.HTTP_204_NO_CONTENT)

    # ---------- 查询角色拥有的菜单权限 ----------
    @action(detail=True, methods=['get'], url_path='menus')
    def get_role_menus(self, request, pk=None):
        role = self.get_object()
        menu_ids = SysRoleMenu.objects.filter(role_id=role.id).values_list('menu_id', flat=True)
        return Response({'menuIds': list(menu_ids)})

    # ---------- 授权菜单 ----------
    @action(detail=True, methods=['post'], url_path='grant-menus')
    def grant_menus(self, request, pk=None):
        role = self.get_object()
        menu_ids = request.data.get('menuIds', [])
        # 清除原有权限
        SysRoleMenu.objects.filter(role_id=role.id).delete()
        for menu_id in menu_ids:
            SysRoleMenu.objects.create(role_id=role.id, menu_id=menu_id)
        return Response({'detail': '菜单授权成功'})

    def perform_destroy(self, instance):
        # 删除角色前，先清除关联的用户角色和菜单权限
        from role.models import SysUserRole
        from menu.models import SysRoleMenu
        SysUserRole.objects.filter(role_id=instance.id).delete()
        SysRoleMenu.objects.filter(role_id=instance.id).delete()
        instance.delete()
