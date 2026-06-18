# apps/parameter/serializers.py
from rest_framework import serializers
from .models import MeterParameter, InverterParameter


class MeterParameterSerializer(serializers.ModelSerializer):
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = MeterParameter
        fields = '__all__'
        read_only_fields = ['id', 'created_at']


class InverterParameterSerializer(serializers.ModelSerializer):
    factory_name = serializers.CharField(source='factory.factory_name', read_only=True)

    class Meta:
        model = InverterParameter
        fields = '__all__'
        read_only_fields = ['id', 'created_at']