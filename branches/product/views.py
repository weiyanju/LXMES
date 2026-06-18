# apps/product/views.py
from rest_framework import viewsets, filters
from rest_framework.pagination import PageNumberPagination
from django_filters.rest_framework import DjangoFilterBackend
from .models import Product, Process, ProductProcess, ProcessFile
from .serializers import (
    ProductSerializer, ProcessSerializer,
    ProductProcessSerializer, ProcessFileSerializer
)


class StandardPagination(PageNumberPagination):
    page_size = 10
    page_size_query_param = 'pageSize'
    page_query_param = 'page'


class ProductViewSet(viewsets.ModelViewSet):
    queryset = Product.objects.select_related('factory').all().order_by('-id')
    serializer_class = ProductSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['product_code', 'product_name']
    filterset_fields = ['product_type', 'status', 'factory']


class ProcessViewSet(viewsets.ModelViewSet):
    queryset = Process.objects.select_related('factory').all().order_by('-id')
    serializer_class = ProcessSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['process_code', 'process_name']
    filterset_fields = ['factory']


class ProductProcessViewSet(viewsets.ModelViewSet):
    queryset = ProductProcess.objects.select_related('product', 'process').all().order_by('product', 'sequence')
    serializer_class = ProductProcessSerializer
    pagination_class = StandardPagination
    filter_backends = [DjangoFilterBackend]
    filterset_fields = ['product', 'process']


class ProcessFileViewSet(viewsets.ModelViewSet):
    queryset = ProcessFile.objects.select_related('factory', 'uploaded_by').all().order_by('-id')
    serializer_class = ProcessFileSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['file_code', 'file_name']
    filterset_fields = ['status', 'product_type', 'factory']