from django.http import JsonResponse
from django.views import View
from django.db.models import Count
from .models import (
    DefectRecord, EmployeePerformance, SalesOrder,
    DefectTypeStat, ProcessPlan, WorkOrder
)

class DefectRecordsAPI(View):
    """返回日期表格数据"""
    def get(self, request):
        records = DefectRecord.objects.order_by('-date')
        data = [{
            'date': r.date.strftime('%Y-%m-%d %H:%M:%S'),
            'product': r.product,
            'reportQty': r.report_qty,
            'goodQty': r.good_qty,
            'badQty': r.bad_qty,
        } for r in records]
        return JsonResponse({'code': 200, 'data': data})

class Top5PerformanceAPI(View):
    """返回员工绩效Top5"""
    def get(self, request):
        top5 = EmployeePerformance.objects.order_by('-score')[:5]
        data = [{'name': p.employee_name, 'value': p.score} for p in top5]
        return JsonResponse({'code': 200, 'data': data})

class SalesOrderRatioAPI(View):
    """返回销售订单占比（百分比）"""
    def get(self, request):
        stats = SalesOrder.objects.values('status').annotate(count=Count('id'))
        total = sum(item['count'] for item in stats)
        data = [{'name': item['status'], 'value': round(item['count'] / total * 100, 2)} for item in stats]
        return JsonResponse({'code': 200, 'data': data})

class DefectPieAPI(View):
    """返回不良品饼图数据"""
    def get(self, request):
        stats = DefectTypeStat.objects.all()
        data = [{'name': s.name, 'value': s.value} for s in stats]
        return JsonResponse({'code': 200, 'data': data})

class ProcessTop5API(View):
    """返回工序计划Top5"""
    def get(self, request):
        top5 = ProcessPlan.objects.order_by('-plan_qty')[:5]
        data = [{'name': p.process_name, 'value': p.plan_qty} for p in top5]
        return JsonResponse({'code': 200, 'data': data})

class WorkOrdersAPI(View):
    """返回工单表格数据"""
    def get(self, request):
        orders = WorkOrder.objects.all()
        data = [{
            'orderNo': o.order_no,
            'status': o.status,
            'productCode': o.product_code,
            'productName': o.product_name,
            'spec': o.spec,
            'planQty': o.plan_qty,
            'actualQty': o.actual_qty,
        } for o in orders]
        return JsonResponse({'code': 200, 'data': data})