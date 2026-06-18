# apps/production/urls.py
from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import (
    ProductionPlanViewSet, WorkOrderViewSet, BarcodeRelationViewSet,
    BarcodeScanViewSet, ProductionDataViewSet
)

router = DefaultRouter()
router.register(r'plans', ProductionPlanViewSet, basename='production-plan')
router.register(r'work-orders', WorkOrderViewSet, basename='work-order')
router.register(r'barcode-relations', BarcodeRelationViewSet, basename='barcode-relation')
router.register(r'barcode-scans', BarcodeScanViewSet, basename='barcode-scan')
router.register(r'production-data', ProductionDataViewSet, basename='production-data')

urlpatterns = [
    path('', include(router.urls)),
]