from django.urls import path, include
from rest_framework.routers import DefaultRouter
from .views import ProductionPlanViewSet

router = DefaultRouter()
router.register(r'production-plans', ProductionPlanViewSet, basename='productionplan')

urlpatterns = [
    path('', include(router.urls)),
]