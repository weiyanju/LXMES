# screen/urls.py（无需修改，若想统一可加斜杠）
from django.urls import path
from .views import (
    DefectRecordsAPI, Top5PerformanceAPI, SalesOrderRatioAPI,
    DefectPieAPI, ProcessTop5API, WorkOrdersAPI
)

urlpatterns = [
    path('defect-records/', DefectRecordsAPI.as_view()),
    path('top5-performance/', Top5PerformanceAPI.as_view()),
    path('sales-order-ratio/', SalesOrderRatioAPI.as_view()),
    path('defect-pie/', DefectPieAPI.as_view()),
    path('process-top5/', ProcessTop5API.as_view()),
    path('work-orders/', WorkOrdersAPI.as_view()),
]