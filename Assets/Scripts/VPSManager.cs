using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using System;
using UnityEngine.XR.ARFoundation;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class VPSManager : MonoBehaviour
{
    [SerializeField] private AREarthManager earthManager;
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private LookAtTarget arrowLookAtTarget;
    [SerializeField] private Camera ARCamera;
    [SerializeField] private List<TextAsset> kmlRoutes;
    [SerializeField] private List<TextAsset> kmlFloodSpots;
    [SerializeField] private GameObject arrowRoutePrefab;
    [SerializeField] private GameObject lastPointPrefab;
    [SerializeField] private TMP_InputField fineAltitudeAdjust; //meters
    [SerializeField] private Button btnGerarObjetos;
    [SerializeField] private TMP_Text tmpInformativo;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Image wrongDirectionScreen;
    [SerializeField] private Image floodZoneScreen;
    [SerializeField] private List<GeospatialObjectLists> geospatialRoutes = new();
    [SerializeField] private List<GeospatialObjectLists> floodSpots = new();


    private List<GameObject> instantiatedObjects = new();
    private OnScreenDebugger debugger;

    private double lastShortestDistanceBetweenUserAndRoute = 0;
    private double lastMeasuredDistance = 0;
    private double shortestDistanteBetweenUserAndRoute = 0;
    private bool stopCheckDistanceBetweenUserAndNearestPoint = false;
    private bool pulseWrongDirectionScreen = false;
    private bool pulseFloodZoneScreen = false;

    private GeospatialObject nearestPointToGo;
    private List<GeospatialObject> shortestRoute = new();


    [Serializable]
    public class GeospatialObjectLists
    {
        public List<GeospatialObject> list = new();
    }

    [Serializable]
    public class GeospatialObject
    {
        public GameObject objectPrefab;
        public EarthCoordinates coordinates;

        public GeospatialObject(GameObject obj, EarthCoordinates coordinates)
        {
            objectPrefab = obj;
            this.coordinates = coordinates;
        }
    }

    [Serializable]
    public class EarthCoordinates
    {
        public double longitude;
        public double latitude;
        public double altitude;

        public EarthCoordinates(double lon, double lat, double alt)
        {
            longitude = lon;
            latitude = lat;
            altitude = alt;
        }

        public void SetAltitude(float value)
        {
            altitude += value;
        }
    }


    private void Awake()
    {
        debugger = FindObjectOfType<OnScreenDebugger>();
        geospatialRoutes = CoordinateConverter(kmlRoutes);
        floodSpots = CoordinateConverter(kmlFloodSpots);
    }

    private void Start()
    {
        Input.location.Start(3f, 0.1f);
        VerifyGeospatialSupport();
    }

    private void Update()
    {
        if (pulseWrongDirectionScreen)
        {
            PulseColorScreen(Color.white, 1f, wrongDirectionScreen);
        }
        
        if (pulseFloodZoneScreen)
        {
            PulseColorScreen(Color.white, 1f, floodZoneScreen);
        }
    }

    /// <summary>
    /// Converte uma lista com arquivos de texto contendo coordenadas, para uma Lista de GeospatialObjetcs
    /// </summary>
    /// <param name="kmlFiles"> Lista de Arquivos de texto contendo as coordenadas (lon, lat, alt)</param>
    /// <returns></returns>
    private List<GeospatialObjectLists> CoordinateConverter(List<TextAsset> kmlFiles)
    {
        List<GeospatialObjectLists> returnList = new();

        if (kmlFiles != null && kmlFiles.Count > 0)
        {
            foreach (var kmlFile in kmlFiles)
            {
                List<GeospatialObject> geoList = new();

                string kmlString = kmlFile.text;

                string[] coordinatesVector = kmlString.Split(" ");

                foreach (var coordenate in coordinatesVector)
                {
                    string[] coordinatesplited = coordenate.Split(",");

                    for (int i = 0; i < coordinatesplited.Length; i++)
                    {
                        coordinatesplited[i] = coordinatesplited[i].Replace(".", ",");
                    }

                    if (coordinatesplited.Length == 3)
                    {
                        geoList.Add(CreateGeospatialObjects(double.Parse(coordinatesplited[0]), double.Parse(coordinatesplited[1]), double.Parse(coordinatesplited[2])));
                    }
                }

                returnList.Add(new GeospatialObjectLists { list = geoList });
            }
        }

        return returnList;
    }

    /// <summary>
    /// Cria um novo GeospatialObject baseado nas coordenadas fornecidas. (lon, lat, alt)
    /// </summary>
    public GeospatialObject CreateGeospatialObjects(double longitude, double latitude, double altitude)
    {
        //lon,lat,alt
        return new GeospatialObject(arrowRoutePrefab, new EarthCoordinates(longitude, latitude, altitude));
    }

    /// <summary>
    /// Verifica se o telefone possue suporte a API Geospacial do google
    /// </summary>
    private void VerifyGeospatialSupport()
    {
        tmpInformativo.text = "Verificando suporte VPS";

        var result = earthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);

        switch (result)
        {
            case FeatureSupported.Supported:

                tmpInformativo.text = "Verificando rota mais próxima";
                CheckNearestRoute();

                break;

            case FeatureSupported.Unsupported:

                tmpInformativo.text = "VPS não Suportado";
                break;

            case FeatureSupported.Unknown:

                Invoke(nameof(VerifyGeospatialSupport), 5.0f);

                break;
        }
    }

    /// <summary>
    /// Realiza a plotagem inicial dos objetos na rota selecionada.
    /// </summary>
    public void PlaceObject()
    {
        if (earthManager.EarthTrackingState == TrackingState.Tracking)
        {
            tmpInformativo.text = "";
            btnGerarObjetos.gameObject.SetActive(false);
            GameObject prefab;

            foreach (var geoPoint in shortestRoute)
            {
                if (geoPoint == shortestRoute.Last())
                { 
                    prefab = lastPointPrefab;
                }
                else
                {
                    prefab = arrowRoutePrefab;
                }

               instantiatedObjects.Add(InstantiateObjectWithAnchor(prefab, CreateAnchor(geoPoint)));
            }

            instantiatedObjects.Last().GetComponent<SpriteRenderer>().enabled = false;
            arrowLookAtTarget.SetTarget(InstantiateObjectWithAnchor(new GameObject(), CreateAnchor(nearestPointToGo)).transform);
            SetRotationOfRouteObjects();
            StartCoroutine(UpdateDistanceBetweenUserAndCoordenate(shortestRoute.Last()));
        }
        else if (earthManager.EarthTrackingState == TrackingState.None)
        {
            tmpInformativo.text = "Sem sinal ou GPS desligado";
            Invoke(nameof(PlaceObject), 5.0f);
        }
    }

    /// <summary>
    /// Posiciona a direção que as setas das rotas vão apontar.
    /// Atualmente a seta anterior aponta para a seguinte e a marcação final fica apontando para
    /// a camera do usuário
    /// </summary>
    private void SetRotationOfRouteObjects()
    {
        int listCount = instantiatedObjects.Count;

        for (int i = 0; i <= listCount - 1; i++)
        {
            if (i == listCount - 1)
            {
                instantiatedObjects[i].GetComponent<LookAtTarget>().SetTarget(ARCamera.transform);
            }
            else
            {
                instantiatedObjects[i].GetComponent<LookAtTarget>().SetTarget(instantiatedObjects[i + 1].transform);
            }
        }
    }

    /// <summary>
    /// Cria uma ancora geospacial baseado nos dados do GeospatialObject passado como parametro
    /// </summary>
    /// <returns>Uma ArGeospatialAnchor (google ARGeospatial API)</returns>
    private ARGeospatialAnchor CreateAnchor(GeospatialObject geospatialObject)
    {
        EarthCoordinates coordinates = geospatialObject.coordinates;
               
        return ARAnchorManagerExtensions.AddAnchor(
            anchorManager, coordinates.latitude,
            coordinates.longitude,
            coordinates.altitude,
            Quaternion.identity);
    }

    /// <summary>
    /// Instancia um objeto com sua respectiva ancora previamente criada.
    /// </summary>
    /// <param name="objectPrefab"> Objeto que vai ser usado como prefab para instanciar.</param>
    /// <param name="anchor">ARGeospatialAnchor</param>
    /// <returns>GameObject criado e instanciado na cena</returns>
    private GameObject InstantiateObjectWithAnchor(GameObject objectPrefab, ARGeospatialAnchor anchor)
    {
        return Instantiate(objectPrefab, anchor.transform);
    }

    /// <summary>
    /// Coroutine que verifica a posição do usuário em relação ao ultimo ponto da rota selecionada.
    /// </summary>
    /// <param name="lastPoint">Ultimo ponto da rota selecionada</param>
    /// <returns></returns>
    private IEnumerator UpdateDistanceBetweenUserAndCoordenate(GeospatialObject lastPoint)
    {
        double userLatitude = Input.location.lastData.latitude;
        double userLongitude = Input.location.lastData.longitude;

        double distance = GetDistanceInMeterBetweenCoordinates(
            userLatitude,
            userLongitude,
            lastPoint.coordinates.latitude,
            lastPoint.coordinates.longitude);

        VerifyCurrentDirection(distance);
        VerifyUserUnderFloodSpot(userLatitude, userLongitude) ;

        if(!stopCheckDistanceBetweenUserAndNearestPoint)
        {
            GetDistanceBetweenUserAndNearestPoint(userLatitude, userLongitude);
        }

        yield return new WaitForSeconds(2f);
        StartCoroutine(UpdateDistanceBetweenUserAndCoordenate(shortestRoute.Last()));
    }

    /// <summary>
    /// Ao selecionar a rota mais próxima do usuário, verifica qual a distancia entre o ponto selecionado
    /// e o usuário. Este ponto é utilizado para saber qual a rota mais próxima, verificando a distancia
    /// entre o usuário e o ponto.
    /// </summary>
    /// <param name="userLatitude"></param>
    /// <param name="userLongitude"></param>
    private void GetDistanceBetweenUserAndNearestPoint(double userLatitude, double userLongitude)
    {
        double distance = GetDistanceInMeterBetweenCoordinates(
            userLatitude,
            userLongitude,
            nearestPointToGo.coordinates.latitude,
            nearestPointToGo.coordinates.longitude);

        DisableArrowIndicator(distance);
    }

    /// <summary>
    /// Verifica direção que o usuário está indo em relação a rota, baseando-se na distancia até o ponto final da rota.
    /// </summary>
    /// <param name="currentDistance"> Distancia entre o usuário e o ponto final</param>
    private void VerifyCurrentDirection(double currentDistance)
    {
        tmpInformativo.text = $"{currentDistance}m";

        if (currentDistance > lastMeasuredDistance)
        {
            tmpInformativo.text += " (Direção errada)";
            wrongDirectionScreen.enabled = true;
            pulseWrongDirectionScreen = true;
        }
        else if (currentDistance < lastMeasuredDistance)
        {
            tmpInformativo.text += " (Direção correta)";
            pulseWrongDirectionScreen = false;
            wrongDirectionScreen.enabled = false;
        }
        else
        {
            tmpInformativo.text += " (Parado)";
            pulseWrongDirectionScreen = false;
            wrongDirectionScreen.enabled = false;
        }

        CheckDistanceToMeetingPoint(currentDistance);
        
        lastMeasuredDistance = currentDistance;
    }

    /// <summary>
    /// Checa se a distância do usuário até o ponto final é menor que um valor pré definido em metros.
    /// Se for, habilita o sprite da placa de ponto de encontro (para não ficar o tempo todo na frente do usuário)
    /// </summary>
    /// <param name="currentDistance"> Distância entre o usuário e ponto final da rota</param>
    private void CheckDistanceToMeetingPoint(double currentDistance)
    {
        instantiatedObjects.Last().GetComponent<SpriteRenderer>().enabled = currentDistance <= 10; 
    }

    /// <summary>
    /// Realiza a checagem da rota mais próxima do usuário baseado no ponto mais próximo de latitude e longitude dele.
    /// Ao selecionar o ponto mais próximo, a rota referente a este ponto é selecionada como a mais próxima.
    /// </summary>
    private void CheckNearestRoute()
    {
        foreach (var route in geospatialRoutes)
        {
            foreach (var geoPoint in route.list)
            {
                CalculateNearestRoute(geoPoint);
            }

            if (shortestDistanteBetweenUserAndRoute < lastShortestDistanceBetweenUserAndRoute || lastShortestDistanceBetweenUserAndRoute == 0)
            {
                lastShortestDistanceBetweenUserAndRoute = shortestDistanteBetweenUserAndRoute;
                shortestRoute = route.list;
            }
        }

        tmpInformativo.text = "Rota gerada. Clique em plotar.";
        btnGerarObjetos.enabled = true;

    }

    /// <summary>
    /// Calcula a distancia entre o usuário e o ponto da rota, verificando se é mais curto que o anterior.
    /// </summary>
    /// <param name="currentPointToTest"> Ponto atual sendo testado</param>
    private void CalculateNearestRoute(GeospatialObject currentPointToTest)
    {
        double distance = GetDistanceInMeterBetweenCoordinates(
            Input.location.lastData.latitude,
            Input.location.lastData.longitude,
            currentPointToTest.coordinates.latitude,
            currentPointToTest.coordinates.longitude);

        if (distance < shortestDistanteBetweenUserAndRoute || shortestDistanteBetweenUserAndRoute == 0)
        {
            shortestDistanteBetweenUserAndRoute = distance;
            nearestPointToGo = currentPointToTest;
        }
    }

    /// <summary>
    /// Calculo de Haversine. Calcula a distância em metros entre duas coordenadas.
    /// </summary>
    /// <param name="fromLat"></param>
    /// <param name="fromLong"></param>
    /// <param name="toLat"></param>
    /// <param name="toLong"></param>
    /// <returns>Distância em metros</returns>
    private double GetDistanceInMeterBetweenCoordinates(double fromLat, double fromLong, double toLat, double toLong)
    {
        // Converter graus para radianos
        double dLat = Math.PI * (toLat - fromLat) / 180;
        double dLon = Math.PI * (toLong - fromLong) / 180;

        // Raio da Terra em metros
        double radius = 6371000;

        // Fórmula de Haversine
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(Math.PI * (fromLat) / 180) * Math.Cos(Math.PI * (toLat) / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = Math.Floor(radius * c);

        return distance;
    }

    /// <summary>
    /// Calculo de "ponto dentro de um Poligono irregular". Verifica se o usuário está dentro de uma área demarcada.
    /// </summary>
    private void VerifyUserUnderFloodSpot(double userLatitude, double userLongitude)
    {
        for (int floodList = 0; floodList < floodSpots.Count; floodList++)
        {
            List<GeospatialObject> floodSpot = floodSpots[floodList].list;
            bool isInside = false;
            int count = floodSpot.Count;

            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                if (((floodSpot[i].coordinates.latitude > userLatitude) != (floodSpot[j].coordinates.latitude > userLatitude)) &&
                    (userLongitude < (floodSpot[j].coordinates.longitude - floodSpot[i].coordinates.longitude) * (userLatitude - floodSpot[i].coordinates.latitude) / (floodSpot[j].coordinates.latitude - floodSpot[i].coordinates.latitude) + floodSpot[i].coordinates.longitude))
                {
                    isInside = !isInside;
                }
            }

            floodZoneScreen.enabled = isInside;
            pulseFloodZoneScreen = isInside;

            if (isInside) break;

        }
    }

    /// <summary>
    /// Função para pulsar as placas (cor => transparente => cor)
    /// </summary>
    private void PulseColorScreen(Color color, float opacity, Image screen)
    {
        float lerp = Mathf.PingPong(Time.time * 0.8f, opacity);
        Color lerpedColor = Color.Lerp(Color.clear, color, lerp);
        screen.color = lerpedColor;
    }

    /// <summary>
    /// Desabilita a seta superior que indica onde está o ponto mais próximo da rota caso o usuário esteja a uma certa distância deste ponto.
    /// (longe de qualquer ponto da rota)
    /// </summary>
    private void DisableArrowIndicator(double distance)
    {
        if (distance < 10)
        {
            stopCheckDistanceBetweenUserAndNearestPoint = true;
            arrowLookAtTarget.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Atualiza a altitude dos objetos plotados em cena.
    /// </summary>
    public void UpdateAnchorAltitudePosition()
    {
        var value = fineAltitudeAdjust.text;

        if (float.TryParse(value, out float altitude))
        {

            foreach (GameObject go in instantiatedObjects)
            {
                go.transform.position += new Vector3(0, altitude, 0);
            }



            //foreach (var obj in instantiatedObjects)
            //{
            //    Destroy(obj);
            //}

            //instantiatedObjects.Clear();

            //for (int i = 0; i < shortestComputedRoute.Count; i++)
            //{
            //    shortestComputedRoute[i].coordinates.SetAltitude(altitude);
            //}

        }
    }

}

