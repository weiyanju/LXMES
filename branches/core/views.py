# apps/core/views.py
from rest_framework import viewsets, filters
from rest_framework.pagination import PageNumberPagination
from django_filters.rest_framework import DjangoFilterBackend
from .models import Factory, Customer, Department, Employee, User
from .serializers import (
    FactorySerializer, CustomerSerializer, DepartmentSerializer,
    EmployeeSerializer, UserSerializer
)


class StandardPagination(PageNumberPagination):
    page_size = 10
    page_size_query_param = 'pageSize'
    page_query_param = 'page'


class FactoryViewSet(viewsets.ModelViewSet):
    queryset = Factory.objects.all().order_by('-id')
    serializer_class = FactorySerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['factory_code', 'factory_name']
    filterset_fields = ['status']


class CustomerViewSet(viewsets.ModelViewSet):
    queryset = Customer.objects.all().order_by('-id')
    serializer_class = CustomerSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['customer_code', 'customer_name']
    filterset_fields = ['status']


class DepartmentViewSet(viewsets.ModelViewSet):
    queryset = Department.objects.all().order_by('-id')
    serializer_class = DepartmentSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['department_code', 'department_name']
    filterset_fields = ['status', 'factory']


class EmployeeViewSet(viewsets.ModelViewSet):
    queryset = Employee.objects.select_related('department', 'factory').all().order_by('-id')
    serializer_class = EmployeeSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['employee_code', 'name']
    filterset_fields = ['status', 'factory', 'department']


class UserViewSet(viewsets.ModelViewSet):
    queryset = User.objects.select_related('factory').all().order_by('-id')
    serializer_class = UserSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['username', 'name']
    filterset_fields = ['role', 'factory', 'is_active']