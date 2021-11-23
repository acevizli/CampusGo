from rest_framework.decorators import api_view, permission_classes
from rest_framework.response import Response

from base.models import Location,Images

from rest_framework.permissions import IsAuthenticated, IsAdminUser
from django.contrib.auth.models import User
from django.contrib.auth.hashers import make_password
from rest_framework import status
from base.serializers import serializeUser, serializeUserWithToken,serializeLocations
from rest_framework_simplejwt.serializers import TokenObtainPairSerializer
from rest_framework_simplejwt.views import TokenObtainPairView
import django.contrib.auth.password_validation as validators

@permission_classes([IsAuthenticated])
@api_view(["POST"])
def storeLocation(request):
    data = request.data
    print(str(request.user) + data['imagename'])
    image = Images.objects.get(name = str(request.user) + data['imagename']) 
    Location.objects.create(
        user = request.user,
        latitude=data['latitude'],
        longitude=data['longitude'],
        accuracy=data['accuracy'],
        image = image
    )

    return Response(status=status.HTTP_200_OK)


@api_view(["POST"])
@permission_classes([IsAuthenticated])
def getLocations(request):
    data = request.data
    current_lat = data['latitude']
    current_long = data['longitude']
    degree = 0.0005
    nearLocations = Location.objects.filter(latitude__range=[current_lat - degree, current_lat +degree]).filter(longitude__range=[current_long - degree, current_long + degree]).exclude(user = request.user)
    print(nearLocations.count())
    print(nearLocations.query)
    print(nearLocations.values_list())
    serializer = serializeLocations(nearLocations,many = True)
    print({"List":serializer.data})
    return Response({"List":serializer.data})

@api_view(["GET"])
@permission_classes([IsAuthenticated])
def loginWithToken(request):
    return Response(status = status.HTTP_200_OK)
@api_view(["POST"])
def registerUser(request):
    data = request.data
    if User.objects.filter(username=data["username"]).exists():
        message = {"detail": "user with this usernmae already exists"}
        return Response(message, status=status.HTTP_400_BAD_REQUEST)
    user = User.objects.create(
        username=data["username"],
        password=make_password(data["password"]),
    )
    serializer = serializeUserWithToken(user, many=False)
    return Response(serializer.data)

class MyTokenObtainPairSerializer(TokenObtainPairSerializer):
    def validate(self, attrs):
        data = super().validate(attrs)

        serializer = serializeUserWithToken(self.user).data
        for k, v in serializer.items():
            data[k] = v

        return data


class MyTokenObtainPairView(TokenObtainPairView):
    serializer_class = MyTokenObtainPairSerializer

@permission_classes([IsAuthenticated])
@api_view(["POST"])
def uploadImage(request):
    print(request.POST)
    Images.objects.create(
        image = request.FILES['image'],
        user = request.user,
        name = request.POST.get('name')
    )
    return Response("Image is created")

@permission_classes([IsAuthenticated])
@api_view(["GET"])
def imageExist(request,pk):
    name = pk
    exists = Images.objects.filter(name = name).exists()
    if exists:
        message = {"detail": "Image Exists"}
        return Response(message, status=status.HTTP_200_OK)
    else:
        message = {"detail": "Image does not Exists"}
        return Response(message, status=status.HTTP_404_NOT_FOUND)
