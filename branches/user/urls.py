from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import (
    LoginView, TestView, JwtTestView, SysUserViewSet
)

router = DefaultRouter()
router.register(r'users', SysUserViewSet, basename='user')

urlpatterns = [
    # 保留原有登录、测试路由
    path('login', LoginView.as_view(), name='login'),
    path('test', TestView.as_view(), name='test'),
    path('jwt_test', JwtTestView.as_view(), name='jwt_test'),

    # DRF 路由（包含所有用户管理接口）
    path('', include(router.urls)),
]