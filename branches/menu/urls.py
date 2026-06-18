# menu/urls.py
from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import TreeListView, SaveView, ActionView, SysMenuViewSet

router = DefaultRouter()
router.register(r'menus', SysMenuViewSet, basename='menu')

urlpatterns = [
    # 旧版 URL（保留兼容）
    path('treeList', TreeListView.as_view(), name='treeList'),
    path('save', SaveView.as_view(), name='save'),
    path('action', ActionView.as_view(), name='action'),

    # 新版 DRF 路由
    path('', include(router.urls)),
]