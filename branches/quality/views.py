# apps/quality/views.py
from rest_framework import viewsets, filters
from rest_framework.pagination import PageNumberPagination
from django_filters.rest_framework import DjangoFilterBackend

from .models import (
    InspectionPlan, InspectionTask,
    IncomingInspection, ProcessInspection,
    ReturnInspection, ShippingInspection
)
from .serializers import (
    InspectionPlanSerializer, InspectionTaskSerializer,
    IncomingInspectionSerializer, ProcessInspectionSerializer,
    ReturnInspectionSerializer, ShippingInspectionSerializer
)


class StandardPagination(PageNumberPagination):
    page_size = 10
    page_size_query_param = 'pageSize'
    page_query_param = 'page'


class InspectionPlanViewSet(viewsets.ModelViewSet):
    queryset = InspectionPlan.objects.select_related('product', 'factory').all().order_by('-id')
    serializer_class = InspectionPlanSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['plan_code', 'plan_name']
    filterset_fields = ['inspection_type', 'status', 'factory']


class InspectionTaskViewSet(viewsets.ModelViewSet):
    queryset = InspectionTask.objects.select_related('plan', 'product', 'assignee', 'factory').all().order_by('-id')
    serializer_class = InspectionTaskSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['task_code', 'batch_number']
    filterset_fields = ['inspection_type', 'status', 'plan', 'factory']


class IncomingInspectionViewSet(viewsets.ModelViewSet):
    queryset = IncomingInspection.objects.select_related('task', 'supplier', 'inspector', 'factory').all().order_by('-id')
    serializer_class = IncomingInspectionSerializer
    pagination_class = StandardPagination
    filter_backends = [DjangoFilterBackend]
    filterset_fields = ['task', 'inspection_result', 'factory']


class ProcessInspectionViewSet(viewsets.ModelViewSet):
    queryset = ProcessInspection.objects.select_related('task', 'work_order', 'process', 'inspector', 'factory').all().order_by('-id')
    serializer_class = ProcessInspectionSerializer
    pagination_class = StandardPagination
    filter_backends = [DjangoFilterBackend]
    filterset_fields = ['task', 'work_order', 'process', 'inspection_result', 'factory']


class ReturnInspectionViewSet(viewsets.ModelViewSet):
    queryset = ReturnInspection.objects.select_related('task', 'inspector', 'factory').all().order_by('-id')
    serializer_class = ReturnInspectionSerializer
    pagination_class = StandardPagination
    filter_backends = [DjangoFilterBackend]
    filterset_fields = ['task', 'inspection_result', 'factory']


class ShippingInspectionViewSet(viewsets.ModelViewSet):
    queryset = ShippingInspection.objects.select_related('task', 'order', 'product', 'inspector', 'factory').all().order_by('-id')
    serializer_class = ShippingInspectionSerializer
    pagination_class = StandardPagination
    filter_backends = [DjangoFilterBackend]
    filterset_fields = ['task', 'order', 'product', 'inspection_result', 'factory']