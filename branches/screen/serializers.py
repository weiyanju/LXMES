from rest_framework import serializers
from .models import (
    DefectRecord, EmployeePerformance, SalesOrder,
    DefectTypeStat, ProcessPlan, WorkOrder
)

class DefectRecordSerializer(serializers.ModelSerializer):
    date = serializers.DateTimeField(format='%Y-%m-%d %H:%M:%S')
    reportQty = serializers.IntegerField(source='report_qty')
    goodQty = serializers.IntegerField(source='good_qty')
    badQty = serializers.IntegerField(source='bad_qty')

    class Meta:
        model = DefectRecord
        fields = ['date', 'product', 'reportQty', 'goodQty', 'badQty']


class EmployeePerformanceSerializer(serializers.ModelSerializer):
    name = serializers.CharField(source='employee_name')
    value = serializers.IntegerField(source='score')

    class Meta:
        model = EmployeePerformance
        fields = ['name', 'value']


class DefectTypeStatSerializer(serializers.ModelSerializer):
    class Meta:
        model = DefectTypeStat
        fields = ['name', 'value']


class ProcessPlanSerializer(serializers.ModelSerializer):
    name = serializers.CharField(source='process_name')
    value = serializers.IntegerField(source='plan_qty')

    class Meta:
        model = ProcessPlan
        fields = ['name', 'value']


class WorkOrderSerializer(serializers.ModelSerializer):
    orderNo = serializers.CharField(source='order_no')
    productCode = serializers.CharField(source='product_code')
    productName = serializers.CharField(source='product_name')
    planQty = serializers.IntegerField(source='plan_qty')
    actualQty = serializers.IntegerField(source='actual_qty')

    class Meta:
        model = WorkOrder
        fields = ['orderNo', 'status', 'productCode', 'productName', 'spec', 'planQty', 'actualQty']