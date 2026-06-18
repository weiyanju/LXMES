# apps/parameter/urls.py
from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import MeterParameterViewSet, InverterParameterViewSet

router = DefaultRouter()
router.register(r'meter', MeterParameterViewSet, basename='meter-parameter')
router.register(r'inverter', InverterParameterViewSet, basename='inverter-parameter')

urlpatterns = [
    path('', include(router.urls)),
]