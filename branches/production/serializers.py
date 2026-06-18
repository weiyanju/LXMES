# apps/production/serializers.py
from rest_framework import serializers
from .models import (
    ProductionPlan, WorkOrder, BarcodeRelation,
    BarcodeScan, ProductionData
)


class ProductionPlanSerializer(serializers.ModelSerializer):
    customer_name = serializers.CharField(source='customer.customer_name', read_only=True)
    created_by_name = serializers.CharField(source='created_by.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)
    product_name = serializers.CharField(source='product.product_name', read_only=True)

    class Meta:
        model = ProductionPlan
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class WorkOrderSerializer(serializers.ModelSerializer):
    plan_name = serializers.CharField(source='plan.plan_name', read_only=True)
    process_file_name = serializers.CharField(source='process_file.file_name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)
    product_name = serializers.CharField(source='product.product_name', read_only=True)

    class Meta:
        model = WorkOrder
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class BarcodeRelationSerializer(serializers.ModelSerializer):
    product_name = serializers.CharField(source='product.product_name', read_only=True)
    work_order_number = serializers.CharField(source='work_order.order_number', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = BarcodeRelation
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class BarcodeScanSerializer(serializers.ModelSerializer):
    work_order_number = serializers.CharField(source='work_order.order_number', read_only=True)
    process_name = serializers.CharField(source='process.process_name', read_only=True)
    scanner_name = serializers.CharField(source='scanner.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = BarcodeScan
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class ProductionDataSerializer(serializers.ModelSerializer):
    work_order_number = serializers.CharField(source='work_order.order_number', read_only=True)
    equipment_name = serializers.CharField(source='equipment.equipment_name', read_only=True)
    operator_name = serializers.CharField(source='operator.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = ProductionData
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']
