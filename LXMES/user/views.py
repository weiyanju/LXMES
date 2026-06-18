import json
from datetime import datetime
import os

from django.core.paginator import Paginator
from django.http import JsonResponse
from django.views import View
from django.forms.models import model_to_dict
from django.contrib.auth.hashers import make_password, check_password

from rest_framework import viewsets, status
from rest_framework.decorators import action
from rest_framework.response import Response
from rest_framework.pagination import PageNumberPagination
from rest_framework.permissions import IsAuthenticated
from rest_framework.parsers import MultiPartParser, FormParser, JSONParser
from rest_framework_simplejwt.tokens import RefreshToken

from LXMES import settings
from menu.models import SysMenu, SysMenuSerializer
from role.models import SysRole, SysUserRole
from user.models import SysUser
from user.serializers import SysUserSerializer


class LoginView(View):

    def buildTreeMenu(self, sysMenuList):
        resultMenuList: list[SysMenu] = list()
        for menu in sysMenuList:
            for e in sysMenuList:
                if e.parent_id == menu.id:
                    if not hasattr(menu, "children"):
                        menu.children = list()
                    menu.children.append(e)
            if menu.parent_id == 0:
                resultMenuList.append(menu)
        return resultMenuList

    def post(self, request):
        username = request.GET.get("username")
        password = request.GET.get("password")
        try:
            user = SysUser.objects.get(username=username, password=password)

            # 状态检查（假设 status=1 正常，0 禁用，根据实际调整）
            if user.status != 1:
                return JsonResponse({'code': 500, 'info': '账号已被禁用，请联系管理员'})

            # 更新最后登录时间
            user.login_date = datetime.now().date()
            user.save(update_fields=['login_date'])

            refresh = RefreshToken.for_user(user)
            token = str(refresh.access_token)

            roleList = SysRole.objects.raw(
                "SELECT id ,NAME FROM sys_role WHERE id IN (SELECT role_id FROM sys_user_role WHERE user_id=" + str(
                    user.id) + ")")
            print(roleList)

            roles = ",".join([role.name for role in roleList])

            menuSet: set[SysMenu] = set()
            for row in roleList:
                print(row.id, row.name)
                menuList = SysMenu.objects.raw(
                    "SELECT * FROM sys_menu WHERE id IN (SELECT menu_id FROM sys_role_menu WHERE role_id=" + str(
                        row.id) + ")")
                for row2 in menuList:
                    print(row2.id, row2.name)
                    menuSet.add(row2)
            print(menuSet)
            menuList: list[SysMenu] = list(menuSet)
            sorted_menuList = sorted(menuList)
            print(sorted_menuList)
            sysMenuList: list[SysMenu] = self.buildTreeMenu(sorted_menuList)
            print(sysMenuList)
            serializerMenuList = list()
            for sysMenu in sysMenuList:
                serializerMenuList.append(SysMenuSerializer(sysMenu).data)

        except Exception as e:
            print(e)
            return JsonResponse({'code': 500, 'info': '用户名或者密码错误！'})
        return JsonResponse({'code': 200, 'token': token, 'user': SysUserSerializer(user).data,
                             'info': '登录成功！', 'roles': roles, 'menuList': serializerMenuList})


class TestView(View):
    def get(self, request):
        token = request.META.get('HTTP_AUTHORIZATION')
        print("token:", token)
        if token != None and token != '':
            userList_obj = SysUser.objects.all()
            print(userList_obj, type(userList_obj))
            userlist_dict = userList_obj.values()
            print(userlist_dict, type(userlist_dict))
            userlist = list(userlist_dict)
            print(userlist, type(userlist))
            return JsonResponse({'code': 200, 'info': '测试', 'data': userlist})
        else:
            return JsonResponse({'code': 401, 'info': '没有访问权限！'})


class JwtTestView(View):
    def get(self, request):
        user = SysUser.objects.get(username='python222', password='123456')
        refresh = RefreshToken.for_user(user)
        token = str(refresh.access_token)
        return JsonResponse({'code': 200, 'token': token})

# ---------- DRF 分页类（如需保持前端兼容可自定义） ----------
class UserPagination(PageNumberPagination):
    page_size = 10
    page_size_query_param = 'page_size'
    page_query_param = 'page'
    max_page_size = 100

# ---------- 新版 DRF ViewSet ----------
class SysUserViewSet(viewsets.ModelViewSet):
    """
    用户管理 ViewSet
    提供列表、新增、详情、修改、删除、查重、重置密码、状态切换、角色分配、头像上传等功能
    """
    queryset = SysUser.objects.all().order_by('id')
    serializer_class = SysUserSerializer
    pagination_class = UserPagination
    permission_classes = [IsAuthenticated]   # 要求登录

    def get_queryset(self):
        queryset = super().get_queryset()
        # 搜索：根据 username 模糊查询
        search = self.request.query_params.get('query', None)
        if search:
            queryset = queryset.filter(username__icontains=search)
        return queryset

    # ---------- 重写 list 方法，返回格式与原 SearchView 兼容（可选） ----------
    def list(self, request, *args, **kwargs):
        queryset = self.filter_queryset(self.get_queryset())
        page = self.paginate_queryset(queryset)
        if page is not None:
            serializer = self.get_serializer(page, many=True)
            # 原前端可能需要额外的角色信息，这里在序列化器中或手动添加
            data = serializer.data
            # 如果需要像原接口那样添加角色列表，可在此处处理
            return self.get_paginated_response(data)
        serializer = self.get_serializer(queryset, many=True)
        return Response(serializer.data)

    # ---------- 详情：返回部门、角色等附加信息 ----------
    def retrieve(self, request, *args, **kwargs):
        instance = self.get_object()
        serializer = self.get_serializer(instance)
        data = serializer.data
        # 添加角色信息（与原 ActionView GET 保持一致）
        role_ids = SysUserRole.objects.filter(user_id=instance.id).values_list('role_id', flat=True)
        roles = SysRole.objects.filter(id__in=role_ids)
        data['roles'] = [{'id': r.id, 'name': r.name} for r in roles]
        return Response(data)

    # ---------- 用户名查重 ----------
    @action(detail=False, methods=['post'], url_path='check-username')
    def check_username(self, request):
        username = request.data.get('username')
        if not username:
            return Response({'detail': '缺少用户名'}, status=status.HTTP_400_BAD_REQUEST)
        exists = SysUser.objects.filter(username=username).exists()
        return Response({'exists': exists})

    # ---------- 重置密码（设为123456） ----------
    @action(detail=True, methods=['post'], url_path='reset-password')
    def reset_password(self, request, pk=None):
        user = self.get_object()
        user.password = make_password('123456')
        user.update_time = datetime.now().date()
        user.save()
        return Response({'detail': '密码已重置为123456'})

    # ---------- 修改密码（需验证原密码） ----------
    @action(detail=True, methods=['post'], url_path='change-password')
    def change_password(self, request, pk=None):
        user = self.get_object()
        old_pwd = request.data.get('oldPassword')
        new_pwd = request.data.get('newPassword')
        if not check_password(old_pwd, user.password):
            return Response({'detail': '原密码错误'}, status=status.HTTP_400_BAD_REQUEST)
        user.password = make_password(new_pwd)
        user.update_time = datetime.now().date()
        user.save()
        return Response({'detail': '密码修改成功'})

    # ---------- 切换状态 ----------
    @action(detail=True, methods=['patch'], url_path='toggle-status')
    def toggle_status(self, request, pk=None):
        user = self.get_object()
        new_status = request.data.get('status')
        if new_status not in [0, 1]:
            return Response({'detail': '状态值无效'}, status=status.HTTP_400_BAD_REQUEST)
        user.status = new_status
        user.save()
        return Response({'status': user.status})

    # ---------- 角色分配 ----------
    @action(detail=True, methods=['post'], url_path='grant-roles')
    def grant_roles(self, request, pk=None):
        user = self.get_object()
        role_ids = request.data.get('roleIds', [])
        # 清除原有角色关联
        SysUserRole.objects.filter(user_id=user.id).delete()
        for rid in role_ids:
            SysUserRole.objects.create(user_id=user.id, role_id=rid)
        return Response({'detail': '角色分配成功'})

    # ---------- 头像上传 ----------
    @action(detail=False, methods=['post'], url_path='upload-avatar',
            parser_classes=[MultiPartParser, FormParser])
    def upload_avatar(self, request):
        file = request.FILES.get('avatar')
        if not file:
            return Response({'detail': '请选择要上传的头像'}, status=status.HTTP_400_BAD_REQUEST)
        suffix = os.path.splitext(file.name)[1]
        new_filename = datetime.now().strftime('%Y%m%d%H%M%S') + suffix
        file_path = os.path.join(settings.MEDIA_ROOT, 'userAvatar', new_filename)
        os.makedirs(os.path.dirname(file_path), exist_ok=True)
        try:
            with open(file_path, 'wb') as f:
                for chunk in file.chunks():
                    f.write(chunk)
            return Response({'filename': new_filename})
        except Exception as e:
            return Response({'detail': f'文件保存失败: {str(e)}'}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

    # ---------- 更新当前用户头像（快捷方式） ----------
    @action(detail=False, methods=['patch'], url_path='update-avatar', parser_classes=[JSONParser])
    def update_avatar(self, request):
        user = request.user
        avatar = request.data.get('avatar')
        if not avatar:
            return Response({'detail': '缺少 avatar 参数'}, status=status.HTTP_400_BAD_REQUEST)
        user.avatar = avatar
        user.save(update_fields=['avatar', 'update_time'])
        return Response({'avatar': avatar})

    # ---------- 批量删除 ----------
    @action(detail=False, methods=['delete'], url_path='batch-delete')
    def batch_delete(self, request):
        ids = request.data.get('ids', [])
        if not ids:
            return Response({'detail': '请提供要删除的ID列表'}, status=status.HTTP_400_BAD_REQUEST)
        # 删除用户及其角色关联
        SysUserRole.objects.filter(user_id__in=ids).delete()
        SysUser.objects.filter(id__in=ids).delete()
        return Response(status=status.HTTP_204_NO_CONTENT)