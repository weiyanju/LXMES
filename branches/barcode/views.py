# barcode/views.py
from rest_framework import viewsets
from .models import BarcodeRecord
from .serializers import BarcodeRecordSerializer

class BarcodeRecordViewSet(viewsets.ModelViewSet):
    """
    条码记录 API
    提供标准的增删改查接口，分页由全局配置自动处理
    """
    queryset = BarcodeRecord.objects.all()
    serializer_class = BarcodeRecordSerializer