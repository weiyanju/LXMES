# apps/parameter/views.py
from rest_framework import viewsets, filters
from rest_framework.pagination import PageNumberPagination
from django_filters.rest_framework import DjangoFilterBackend
from .models import MeterParameter, InverterParameter
from .serializers import MeterParameterSerializer, InverterParameterSerializer


class StandardPagination(PageNumberPagination):
    page_size = 10
    page_size_query_param = 'pageSize'
    page_query_param = 'page'


class MeterParameterViewSet(viewsets.ModelViewSet):
    queryset = MeterParameter.objects.select_related('factory').all().order_by('-id')
    serializer_class = MeterParameterSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['product_code']
    filterset_fields = ['factory']


class InverterParameterViewSet(viewsets.ModelViewSet):
    queryset = InverterParameter.objects.select_related('factory').all().order_by('-id')
    serializer_class = InverterParameterSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['product_code']
    filterset_fields = ['factory']