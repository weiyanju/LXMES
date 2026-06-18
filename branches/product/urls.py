# apps/product/urls.py
from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import ProductViewSet, ProcessViewSet, ProductProcessViewSet, ProcessFileViewSet

router = DefaultRouter()
router.register(r'products', ProductViewSet, basename='product')
router.register(r'processes', ProcessViewSet, basename='process')
router.register(r'product-processes', ProductProcessViewSet, basename='productprocess')
router.register(r'process-files', ProcessFileViewSet, basename='processfile')

urlpatterns = [
    path('', include(router.urls)),
]