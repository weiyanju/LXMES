from rest_framework import viewsets, filters
from rest_framework.pagination import PageNumberPagination
from django_filters.rest_framework import DjangoFilterBackend
from rest_framework.permissions import IsAuthenticated
from .models import ProductionPlan
from .serializers import ProductionPlanSerializer

class StandardPagination(PageNumberPagination):
    page_size = 10
    page_size_query_param = 'pageSize'
    max_page_size = 100

class ProductionPlanViewSet(viewsets.ModelViewSet):
    queryset = ProductionPlan.objects.all().order_by('-id')
    serializer_class = ProductionPlanSerializer
    #permission_classes = [IsAuthenticated]
    permission_classes = []  # 👈 改成允许所有人访问
    pagination_class = StandardPagination
    filter_backends = [filters.SearchFilter, DjangoFilterBackend]
    search_fields = ['plan_name', 'product_type']
    filterset_fields = ['status', 'source']