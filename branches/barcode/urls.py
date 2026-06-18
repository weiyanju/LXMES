from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import BarcodeRecordViewSet

router = DefaultRouter()
router.register(r'records', BarcodeRecordViewSet, basename='barcode-record')

urlpatterns = [
    path('', include(router.urls)),   # 把所有自动生成的路由挂载到当前路径下
]