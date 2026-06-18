# screen/views.py
from rest_framework.views import APIView
from rest_framework.response import Response
from django.db.models import Count
from .models import (
    DefectRecord, EmployeePerformance, SalesOrder,
    DefectTypeStat, ProcessPlan, WorkOrder
)
from .serializers import (
    DefectRecordSerializer, EmployeePerformanceSerializer,
    DefectTypeStatSerializer, ProcessPlanSerializer, WorkOrderSerializer
)

class DefectRecordsAPI(APIView):
    """不良记录列表"""
    def get(self, request):
        records = DefectRecord.objects.order_by('-date')
        serializer = DefectRecordSerializer(records, many=True)
        return Response(serializer.data)


class Top5PerformanceAPI(APIView):
    """员工绩效 Top5"""
    def get(self, request):
        top5 = EmployeePerformance.objects.order_by('-score')[:5]
        data = [{'name': p.employee_name, 'value': p.score} for p in top5]
        return Response(data)


class SalesOrderRatioAPI(APIView):
    """销售订单状态占比"""
    def get(self, request):
        stats = SalesOrder.objects.values('status').annotate(count=Count('id'))
        total = sum(item['count'] for item in stats)
        data = [{'name': item['status'], 'value': round(item['count'] / total * 100, 2)} for item in stats]
        return Response(data)


class DefectPieAPI(APIView):
    """不良品类型饼图数据"""
    def get(self, request):
        stats = DefectTypeStat.objects.all()
        serializer = DefectTypeStatSerializer(stats, many=True)
        # 为匹配前端可能的字段名，可保留 name/value 格式
        data = [{'name': item['name'], 'value': item['value']} for item in serializer.data]
        return Response(data)


class ProcessTop5API(APIView):
    """工序计划 Top5"""
    def get(self, request):
        top5 = ProcessPlan.objects.order_by('-plan_qty')[:5]
        data = [{'name': p.process_name, 'value': p.plan_qty} for p in top5]
        return Response(data)


class WorkOrdersAPI(APIView):
    """工单列表"""
    def get(self, request):
        orders = WorkOrder.objects.all()
        serializer = WorkOrderSerializer(orders, many=True)
        return Response(serializer.data)