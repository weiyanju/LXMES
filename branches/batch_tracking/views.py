# apps/batch_tracking/views.py
from rest_framework import viewsets, filters
from rest_framework.pagination import PageNumberPagination
from django_filters.rest_framework import DjangoFilterBackend
from .models import BatchTracking
from .serializers import BatchTrackingSerializer


class StandardPagination(PageNumberPagination):
    page_size = 10
    page_size_query_param = 'pageSize'
    page_query_param = 'page'


class BatchTrackingViewSet(viewsets.ModelViewSet):
    queryset = BatchTracking.objects.select_related('product', 'factory').all().order_by('-production_date')
    serializer_class = BatchTrackingSerializer
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['batch_number']
    filterset_fields = ['status', 'product', 'factory']