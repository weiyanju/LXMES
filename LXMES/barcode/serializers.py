from rest_framework import serializers
from .models import BarcodeRecord

class BarcodeRecordSerializer(serializers.ModelSerializer):
    class Meta:
        model = BarcodeRecord
        fields = '__all__'