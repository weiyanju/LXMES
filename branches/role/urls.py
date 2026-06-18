# role/urls.py
from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import (
    ListAllView, SearchView, SaveView, ActionView, MenusView, GrantMenu,
    SysRoleViewSet   # 新增
)

router = DefaultRouter()
router.register(r'roles', SysRoleViewSet, basename='role')

urlpatterns = [
    # 旧版 URL（保留兼容）
    path('listAll', ListAllView.as_view(), name='listAll'),
    path('search', SearchView.as_view(), name='search'),
    path('save', SaveView.as_view(), name='save'),
    path('action', ActionView.as_view(), name='action'),
    path('menus', MenusView.as_view(), name='menus'),
    path('grant', GrantMenu.as_view(), name='grant'),

    # 新版 DRF 路由
    path('', include(router.urls)),
]