# apps/equipment/serializers.py
from rest_framework import serializers
from .models import (
    Equipment, EquipmentStatus, MaintenancePlan,
    MaintenanceRecord, EquipmentFault
)


class EquipmentSerializer(serializers.ModelSerializer):
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = Equipment
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class EquipmentStatusSerializer(serializers.ModelSerializer):
    equipment_name = serializers.CharField(source='equipment.equipment_name', read_only=True)
    operator_name = serializers.CharField(source='operator.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = EquipmentStatus
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class MaintenancePlanSerializer(serializers.ModelSerializer):
    equipment_name = serializers.CharField(source='equipment.equipment_name', read_only=True)
    assignee_name = serializers.CharField(source='assignee.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = MaintenancePlan
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']


class MaintenanceRecordSerializer(serializers.ModelSerializer):
    equipment_name = serializers.CharField(source='equipment.equipment_name', read_only=True)
    plan_code = serializers.CharField(source='plan.plan_code', read_only=True)
    maintenance_by_name = serializers.CharField(source='maintenance_by.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = MaintenanceRecord
        fields = '__all__'
        read_only_fields = ['id', 'created_at']


class EquipmentFaultSerializer(serializers.ModelSerializer):
    equipment_name = serializers.CharField(source='equipment.equipment_name', read_only=True)
    repair_by_name = serializers.CharField(source='repair_by.name', read_only=True)
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = EquipmentFault
        fields = '__all__'
        read_only_fields = ['id', 'created_at', 'updated_at']