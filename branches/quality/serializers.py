# apps/quality/serializers.py
from rest_framework import serializers
from .models import InspectionPlan, InspectionTask


class InspectionPlanSerializer(serializers.ModelSerializer):
    product_name = serializers.CharField(source='product.product_name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = InspectionPlan
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class InspectionTaskSerializer(serializers.ModelSerializer):
    plan_code = serializers.CharField(source='plan.plan_code', read_only=True)
    product_name = serializers.CharField(source='product.product_name', read_only=True)
    assignee_name = serializers.CharField(source='assignee.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = InspectionTask
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']

from .models import (
    IncomingInspection, ProcessInspection,
    ReturnInspection, ShippingInspection
)

class IncomingInspectionSerializer(serializers.ModelSerializer):
    task_code = serializers.CharField(source='task.task_code', read_only=True)
    supplier_name = serializers.CharField(source='supplier.customer_name', read_only=True)
    inspector_name = serializers.CharField(source='inspector.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = IncomingInspection
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class ProcessInspectionSerializer(serializers.ModelSerializer):
    task_code = serializers.CharField(source='task.task_code', read_only=True)
    work_order_number = serializers.CharField(source='work_order.order_number', read_only=True)
    process_name = serializers.CharField(source='process.process_name', read_only=True)
    inspector_name = serializers.CharField(source='inspector.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = ProcessInspection
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class ReturnInspectionSerializer(serializers.ModelSerializer):
    task_code = serializers.CharField(source='task.task_code', read_only=True)
    inspector_name = serializers.CharField(source='inspector.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = ReturnInspection
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class ShippingInspectionSerializer(serializers.ModelSerializer):
    task_code = serializers.CharField(source='task.task_code', read_only=True)
    order_number = serializers.CharField(source='order.order_number', read_only=True)
    product_name = serializers.CharField(source='product.product_name', read_only=True)
    inspector_name = serializers.CharField(source='inspector.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = ShippingInspection
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']
