# apps/core/urls.py
from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import (
    FactoryViewSet, CustomerViewSet, DepartmentViewSet,
    EmployeeViewSet, UserViewSet
)

router = DefaultRouter()
router.register(r'factories', FactoryViewSet, basename='factory')
router.register(r'customers', CustomerViewSet, basename='customer')
router.register(r'departments', DepartmentViewSet, basename='department')
router.register(r'employees', EmployeeViewSet, basename='employee')
router.register(r'users', UserViewSet, basename='user')

urlpatterns = [
    path('', include(router.urls)),
]