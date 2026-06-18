"""
URL configuration for LXMES project.

The `urlpatterns` list routes URLs to views. For more information please see:
    https://docs.djangoproject.com/en/5.0/topics/http/urls/
Examples:
Function views
    1. Add an import:  from my_app import views
    2. Add a URL to urlpatterns:  path('', views.home, name='home')
Class-based views
    1. Add an import:  from other_app.views import Home
    2. Add a URL to urlpatterns:  path('', Home.as_view(), name='home')
Including another URLconf
    1. Import the include() function: from django.urls import include, path
    2. Add a URL to urlpatterns:  path('blog/', include('blog.urls'))
"""
from django.contrib import admin
from django.urls import path, include, re_path
from django.views.static import serve
from rest_framework_simplejwt.views import TokenRefreshView
from LXMES import settings

urlpatterns = [
    # path('admin/', admin.site.urls),
    path('api/token/refresh/', TokenRefreshView.as_view(), name='token_refresh'),
    path('api/user/',include('user.urls')), #用户模块
    path('api/role/',include('role.urls')), #角色模块
    path('api/menu/',include('menu.urls')), #权限模块
    path('api/screen/', include('screen.urls')), #大屏
    path('api/department/', include('department.urls')),#部门
    path('api/barcode/', include('barcode.urls')),#条码录入
    # EMS
    path('api/core/', include('core.urls')),
    path('api/product/', include('product.urls')),
    path('api/equipment/', include('equipment.urls')),
    path('api/production/', include('production.urls')),
    path('api/parameter/', include('parameter.urls')),
    path('api/quality/', include('quality.urls')),
    path('api/batch-tracking/', include('batch_tracking.urls')),
    # 配置媒体文件的路由地址
    re_path('media/(?P<path>.*)', serve, {'document_root': settings.MEDIA_ROOT}, name='media')
]
