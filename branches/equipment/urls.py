# apps/equipment/urls.py
from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import (
    EquipmentViewSet, EquipmentStatusViewSet, MaintenancePlanViewSet,
    MaintenanceRecordViewSet, EquipmentFaultViewSet
)

router = DefaultRouter()
router.register(r'equipment', EquipmentViewSet, basename='equipment')
router.register(r'equipment-status', EquipmentStatusViewSet, basename='equipment-status')
router.register(r'maintenance-plans', MaintenancePlanViewSet, basename='maintenance-plan')
router.register(r'maintenance-records', MaintenanceRecordViewSet, basename='maintenance-record')
router.register(r'equipment-faults', EquipmentFaultViewSet, basename='equipment-fault')

urlpatterns = [
    path('', include(router.urls)),
]