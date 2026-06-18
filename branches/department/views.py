from rest_framework import viewsets, status
from rest_framework.decorators import action
from rest_framework.response import Response
from .models import Department
from .serializers import DepartmentSerializer

class DepartmentViewSet(viewsets.ModelViewSet):
    """
    部门管理 API
    提供列表（分页+搜索）、新增、详情、修改、删除、批量删除
    """
    queryset = Department.objects.all()
    serializer_class = DepartmentSerializer

    def get_queryset(self):
        queryset = super().get_queryset()
        search_query = self.request.query_params.get('query', None)
        if search_query:
            queryset = queryset.filter(name__icontains=search_query)
        return queryset.order_by('id')

    @action(detail=False, methods=['delete'], url_path='batch-delete')
    def batch_delete(self, request):
        """
        批量删除部门，请求体格式：{"ids": [1,2,3]}
        成功时返回 204 No Content
        """
        ids = request.data.get('ids', [])
        if not ids:
            return Response(
                {'detail': '请提供要删除的ID列表'},
                status=status.HTTP_400_BAD_REQUEST
            )
        # 执行删除
        deleted_count, _ = Department.objects.filter(id__in=ids).delete()
        # 标准 RESTful 删除成功返回 204，无响应体
        return Response(status=status.HTTP_204_NO_CONTENT)