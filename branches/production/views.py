# apps/production/views.py
from rest_framework import viewsets, filters
from rest_framework.pagination import PageNumberPagination
from django_filters.rest_framework import DjangoFilterBackend
from .models import (
    ProductionPlan, WorkOrder, BarcodeRelation,
    BarcodeScan, ProductionData
)
from .serializers import (
    ProductionPlanSerializer, WorkOrderSerializer,
    BarcodeRelationSerializer, BarcodeScanSerializer,
    ProductionDataSerializer
)


class StandardPagination(PageNumberPagination):
    page_size = 10
    page_size_query_param = 'pageSize'
    page_query_param = 'page'


class ProductionPlanViewSet(viewsets.ModelViewSet):
    queryset = ProductionPlan.objects.select_related('customer', 'created_by', 'factory', 'product').all().order_by('-id')
    serializer_class = ProductionPlanSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = [
        'plan_name', 'product_type',
        'start_barcode', 'end_barcode',
        'customer_start_barcode', 'customer_end_barcode',
        'product__product_code', 'product__product_name'
    ]
    filterset_fields = ['status', 'source', 'factory', 'product']


class WorkOrderViewSet(viewsets.ModelViewSet):
    queryset = WorkOrder.objects.select_related('plan', 'process_file', 'factory', 'product').all().order_by('-id')
    serializer_class = WorkOrderSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = [
        'order_number', 'product_type',
        'start_barcode', 'end_barcode',
        'customer_start_barcode', 'customer_end_barcode',
        'product__product_code', 'product__product_name'
    ]
    filterset_fields = ['status', 'plan', 'factory', 'product']


class BarcodeRelationViewSet(viewsets.ModelViewSet):
    queryset = BarcodeRelation.objects.select_related('product', 'work_order', 'factory').all().order_by('-id')
    serializer_class = BarcodeRelationSerializer
    pagination_class = StandardPagination
    filter_backends = [DjangoFilterBackend]
    filterset_fields = ['main_barcode_type', 'sub_barcode_type', 'work_order', 'factory']


class BarcodeScanViewSet(viewsets.ModelViewSet):
    queryset = BarcodeScan.objects.select_related('work_order', 'process', 'scanner', 'factory').all().order_by('-created_at')
    serializer_class = BarcodeScanSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['barcode']
    filterset_fields = ['barcode_type', 'work_order', 'process', 'factory']


class ProductionDataViewSet(viewsets.ModelViewSet):
    queryset = ProductionData.objects.select_related('work_order', 'equipment', 'operator', 'factory').all().order_by('-timestamp')
    serializer_class = ProductionDataSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['product_code', 'parameter_name']
    filterset_fields = ['work_order', 'equipment', 'factory']
