from django.contrib.auth.models import User
from django.urls import path

import base.views

urlpatterns = [
    path("location/", base.views.storeLocation, name="location"),
    path("locations/", base.views.getLocations, name="locations"),
    path('login', base.views.MyTokenObtainPairView.as_view(), name='token_obtain_pair'),
    path('register', base.views.registerUser, name='register'),
    path('directlogin',base.views.loginWithToken,name = "direct-login"),
    path("upload/", base.views.uploadImage, name="image-upload"),
    path("image/<str:pk>/", base.views.imageExist, name="image-check"),
]
