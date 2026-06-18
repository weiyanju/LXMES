# apps/quality/urls.py
from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import (
    InspectionPlanViewSet, InspectionTaskViewSet,
    IncomingInspectionViewSet,ProcessInspectionViewSet,
    ReturnInspectionViewSet,ShippingInspectionViewSet
)

router = DefaultRouter()
router.register(r'plans', InspectionPlanViewSet, basename='inspection-plan')
router.register(r'tasks', InspectionTaskViewSet, basename='inspection-task')
# 在 router 注册部分追加
router.register(r'incoming', IncomingInspectionViewSet, basename='incoming-inspection')
router.register(r'process', ProcessInspectionViewSet, basename='process-inspection')
router.register(r'return', ReturnInspectionViewSet, basename='return-inspection')
router.register(r'shipping', ShippingInspectionViewSet, basename='shipping-inspection')
urlpatterns = [
    path('', include(router.urls)),
]