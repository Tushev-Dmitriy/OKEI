using UnityEngine;

public class PrimitiveLockBuilder : MonoBehaviour
{
    [Header("Build")]
    [SerializeField] private bool buildOnAwake = true;
    [SerializeField] private bool buildOnlyIfMissing = true;
    [SerializeField] private string rootName = "PrimitiveLockRig";

    [Header("Layout")]
    [SerializeField] private Vector3 chamberCenter = new Vector3(500f, 5f, 500f);
    [SerializeField] private Vector3 chamberSize = new Vector3(24f, 10f, 32f);
    [SerializeField] private float wallThickness = 1f;
    [SerializeField] private Vector3 gateSize = new Vector3(10f, 8f, 1f);
    [SerializeField] private Vector3 controlPanelOffset = new Vector3(-9f, 0f, -11f);

    [Header("Water")]
    [SerializeField] private Transform existingWaterTransform;
    [SerializeField] private bool createWaterIfMissing = true;

    [Header("Ship")]
    [SerializeField] private Transform existingShipTransform;
    [SerializeField] private bool createShipIfMissing;
    [SerializeField] private Vector3 shipStartOffset = new Vector3(0f, 0f, -24f);
    [SerializeField] private Vector3 shipHoldOffset = new Vector3(0f, 0f, -10f);
    [SerializeField] private Vector3 shipExitOffset = new Vector3(0f, 0f, 26f);

    [Header("Colors")]
    [SerializeField] private Color wallColor = new Color(0.42f, 0.47f, 0.52f, 1f);
    [SerializeField] private Color floorColor = new Color(0.28f, 0.31f, 0.34f, 1f);
    [SerializeField] private Color gateColor = new Color(0.58f, 0.5f, 0.3f, 1f);
    [SerializeField] private Color panelColor = new Color(0.17f, 0.2f, 0.24f, 1f);
    [SerializeField] private Color controlColor = new Color(0.86f, 0.27f, 0.21f, 1f);

    [Header("Output")]
    [SerializeField] private Transform builtRoot;
    [SerializeField] private Transform gateTransform;
    [SerializeField] private Transform waterTransform;
    [SerializeField] private Transform shipTransform;
    [SerializeField] private Transform shipHoldPoint;
    [SerializeField] private Transform shipExitPoint;

    public Transform GateTransform => gateTransform;
    public Transform WaterTransform => waterTransform;
    public Transform ShipHoldPoint => shipHoldPoint;
    public Transform ShipExitPoint => shipExitPoint;

    private void Awake()
    {
        if (buildOnAwake)
            BuildIfNeeded();
    }

    public void BuildIfNeeded()
    {
        if (buildOnlyIfMissing)
        {
            GameObject existingRootObject = GameObject.Find(rootName);
            if (existingRootObject != null)
            {
                builtRoot = existingRootObject.transform;
                CacheExistingChildren();
            }
        }

        if (builtRoot == null)
            BuildPrimitives();

        ResolveWater();
        ResolveShip();
    }

    private void BuildPrimitives()
    {
        GameObject rootObject = new GameObject(rootName);
        builtRoot = rootObject.transform;

        float halfX = chamberSize.x * 0.5f;
        float halfZ = chamberSize.z * 0.5f;
        float halfY = chamberSize.y * 0.5f;

        Transform floor = CreateCube("LockFloor", chamberCenter + new Vector3(0f, -0.5f, 0f), new Vector3(chamberSize.x, 1f, chamberSize.z), builtRoot);
        SetColor(floor, floorColor);

        Transform leftWall = CreateCube("LockWall_Left", chamberCenter + new Vector3(-halfX, halfY, 0f), new Vector3(wallThickness, chamberSize.y, chamberSize.z), builtRoot);
        Transform rightWall = CreateCube("LockWall_Right", chamberCenter + new Vector3(halfX, halfY, 0f), new Vector3(wallThickness, chamberSize.y, chamberSize.z), builtRoot);
        Transform backWall = CreateCube("LockWall_Back", chamberCenter + new Vector3(0f, halfY, -halfZ), new Vector3(chamberSize.x, chamberSize.y, wallThickness), builtRoot);
        SetColor(leftWall, wallColor);
        SetColor(rightWall, wallColor);
        SetColor(backWall, wallColor);

        gateTransform = CreateCube("LockGate", chamberCenter + new Vector3(0f, gateSize.y * 0.5f, halfZ - 0.5f), gateSize, builtRoot);
        SetColor(gateTransform, gateColor);

        Transform panelRoot = CreateCube("ControlPanel_Base", chamberCenter + controlPanelOffset + new Vector3(0f, 1.1f, 0f), new Vector3(1.4f, 2.2f, 1.1f), builtRoot);
        SetColor(panelRoot, panelColor);

        Transform panelTop = CreateCube("ControlPanel_Top", panelRoot.position + new Vector3(0f, 1.25f, 0f), new Vector3(1.8f, 0.25f, 1.2f), builtRoot);
        SetColor(panelTop, panelColor);

        Transform lever = CreateCube("ControlLever", panelTop.position + new Vector3(-0.45f, 0.32f, 0f), new Vector3(0.12f, 0.6f, 0.12f), builtRoot);
        lever.localRotation = Quaternion.Euler(0f, 0f, -22f);
        SetColor(lever, new Color(0.95f, 0.95f, 0.95f, 1f));

        Transform buttonA = CreateCube("ControlButton_A", panelTop.position + new Vector3(0.1f, 0.2f, 0.25f), new Vector3(0.24f, 0.12f, 0.24f), builtRoot);
        Transform buttonB = CreateCube("ControlButton_B", panelTop.position + new Vector3(0.45f, 0.2f, 0.25f), new Vector3(0.24f, 0.12f, 0.24f), builtRoot);
        Transform buttonC = CreateCube("ControlButton_C", panelTop.position + new Vector3(0.28f, 0.2f, -0.18f), new Vector3(0.24f, 0.12f, 0.24f), builtRoot);
        SetColor(buttonA, controlColor);
        SetColor(buttonB, controlColor);
        SetColor(buttonC, controlColor);

        shipHoldPoint = CreatePoint("ShipHoldPoint", chamberCenter + shipHoldOffset, builtRoot);
        shipExitPoint = CreatePoint("ShipExitPoint", chamberCenter + shipExitOffset, builtRoot);
    }

    private void ResolveWater()
    {
        if (existingWaterTransform != null)
            waterTransform = existingWaterTransform;

        if (waterTransform == null)
        {
            GameObject namedWater = GameObject.Find("Water");
            if (namedWater != null)
                waterTransform = namedWater.transform;
        }

        if (waterTransform != null || !createWaterIfMissing)
            return;

        Transform waterPlane = CreatePlane("LockWater", chamberCenter + new Vector3(0f, 3f, 0f), new Vector3(chamberSize.x * 0.1f, 1f, chamberSize.z * 0.1f), builtRoot);
        SetColor(waterPlane, new Color(0.08f, 0.32f, 0.55f, 0.72f));
        waterTransform = waterPlane;
    }

    private void ResolveShip()
    {
        if (existingShipTransform != null)
            shipTransform = existingShipTransform;

        if (shipTransform == null)
        {
            GameObject sceneShip = GameObject.Find("Ship_Scene");
            if (sceneShip != null)
                shipTransform = sceneShip.transform;
        }

        if (shipTransform == null && createShipIfMissing)
        {
            Transform dummyShip = CreateCube("LockShipDummy", chamberCenter + shipStartOffset, new Vector3(3.5f, 1.6f, 8f), builtRoot);
            SetColor(dummyShip, new Color(0.44f, 0.28f, 0.18f, 1f));
            shipTransform = dummyShip;
        }

        if (shipTransform == null)
            return;
    }

    private void CacheExistingChildren()
    {
        gateTransform = FindChildByName(builtRoot, "LockGate");
        shipHoldPoint = FindChildByName(builtRoot, "ShipHoldPoint");
        shipExitPoint = FindChildByName(builtRoot, "ShipExitPoint");

        if (waterTransform == null)
            waterTransform = FindChildByName(builtRoot, "LockWater");
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildByName(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static Transform CreatePoint(string name, Vector3 worldPosition, Transform parent)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent, true);
        point.transform.position = worldPosition;
        return point.transform;
    }

    private static Transform CreateCube(string name, Vector3 worldPosition, Vector3 worldScale, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, true);
        cube.transform.position = worldPosition;
        cube.transform.localScale = worldScale;
        return cube.transform;
    }

    private static Transform CreatePlane(string name, Vector3 worldPosition, Vector3 localScale, Transform parent)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = name;
        plane.transform.SetParent(parent, true);
        plane.transform.position = worldPosition;
        plane.transform.localScale = localScale;

        Collider collider = plane.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        return plane.transform;
    }

    private static void SetColor(Transform target, Color color)
    {
        if (target == null)
            return;

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.material.color = color;
    }
}
