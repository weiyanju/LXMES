# apps/equipment/views.py
from rest_framework import viewsets, filters
from rest_framework.pagination import PageNumberPagination
from django_filters.rest_framework import DjangoFilterBackend
from .models import (
    Equipment, EquipmentStatus, MaintenancePlan,
    MaintenanceRecord, EquipmentFault
)
from .serializers import (
    EquipmentSerializer, EquipmentStatusSerializer,
    MaintenancePlanSerializer, MaintenanceRecordSerializer,
    EquipmentFaultSerializer
)


class StandardPagination(PageNumberPagination):
    page_size = 10
    page_size_query_param = 'pageSize'
    page_query_param = 'page'


class EquipmentViewSet(viewsets.ModelViewSet):
    queryset = Equipment.objects.select_related('factory').all().order_by('-id')
    serializer_class = EquipmentSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['equipment_code', 'equipment_name']
    filterset_fields = ['type', 'status', 'factory']


class EquipmentStatusViewSet(viewsets.ModelViewSet):
    queryset = EquipmentStatus.objects.select_related('equipment', 'operator', 'factory').all().order_by('-created_at')
    serializer_class = EquipmentStatusSerializer
    pagination_class = StandardPagination
    filter_backends = [DjangoFilterBackend]
    filterset_fields = ['equipment', 'status', 'factory']


class MaintenancePlanViewSet(viewsets.ModelViewSet):
    queryset = MaintenancePlan.objects.select_related('equipment', 'assignee', 'factory').all().order_by('-scheduled_date')
    serializer_class = MaintenancePlanSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['plan_code', 'plan_name']
    filterset_fields = ['maintenance_type', 'status', 'equipment', 'factory']


class MaintenanceRecordViewSet(viewsets.ModelViewSet):
    queryset = MaintenanceRecord.objects.select_related('equipment', 'plan', 'maintenance_by', 'factory').all().order_by('-maintenance_date')
    serializer_class = MaintenanceRecordSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['maintenance_content']
    filterset_fields = ['maintenance_type', 'maintenance_result', 'equipment', 'factory']


class EquipmentFaultViewSet(viewsets.ModelViewSet):
    queryset = EquipmentFault.objects.select_related('equipment', 'repair_by', 'factory').all().order_by('-fault_time')
    serializer_class = EquipmentFaultSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['fault_code', 'fault_description']
    filterset_fields = ['fault_level', 'status', 'equipment', 'factory']