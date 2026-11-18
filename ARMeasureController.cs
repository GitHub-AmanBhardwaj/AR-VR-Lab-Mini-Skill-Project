using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARMeasureController : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("UI Components")]
    public Image centerReticle;
    public Button measureButton;
    public TMP_Text buttonText;

    [Header("Materials")]
    public Material lineMaterial;
    public Material startMaterial;
    public Material endMaterial;

    [Header("Appearance")]
    public float lineWidth = 0.002f;
    public float dotSize = 0.005f;

    private enum MeasureState
    {
        Ready,
        Measuring,
        Complete
    }

    private MeasureState currentState = MeasureState.Ready;

    private static List<ARRaycastHit> hits =
        new List<ARRaycastHit>();

    private Pose currentHitPose;
    private bool hasCurrentHit;

    private Vector3 startPosition;
    private Vector3 endPosition;

    private GameObject startDot;
    private GameObject endDot;
    private GameObject lineObject;

    private LineRenderer lineRenderer;
    private TextMeshPro distanceLabel;

    private Color normalColor = Color.white;
    private Color measuringColor = Color.green;

    void Start()
    {
        measureButton.onClick.AddListener(OnButtonPressed);

        SetButtonText("START MEASURE");

        centerReticle.color = normalColor;
    }

    void Update()
    {
        if (currentState == MeasureState.Complete)
        {
            return;
        }

        UpdateCenterRaycast();

        if (currentState == MeasureState.Measuring)
        {
            UpdateMeasurement();
        }
    }

    // Find the real-world point beneath
    // the center of the screen.

    void UpdateCenterRaycast()
    {
        Vector2 screenCenter = new Vector2(
            Screen.width / 2f,
            Screen.height / 2f
        );

        hasCurrentHit = raycastManager.Raycast(
            screenCenter,
            hits,
            TrackableType.PlaneWithinPolygon
        );

        if (hasCurrentHit)
        {
            currentHitPose = hits[0].pose;
        }

        // Button availability
        if (currentState == MeasureState.Ready)
        {
            measureButton.interactable = hasCurrentHit;
        }
    }

    // Handle the single bottom-right button.

    void OnButtonPressed()
    {
        if (currentState == MeasureState.Ready)
        {
            if (!hasCurrentHit)
                return;

            StartMeasurement();
        }
        else if (currentState == MeasureState.Measuring)
        {
            StopMeasurement();
        }
        else if (currentState == MeasureState.Complete)
        {
            ResetMeasurement();
        }
    }

    // FIRST STATE -> SECOND STATE

    void StartMeasurement()
    {
        startPosition = currentHitPose.position;
        endPosition = startPosition;

        // Create fixed starting dot.
        startDot = CreateDot(
            "Starting Point",
            startPosition,
            startMaterial
        );

        // Create moving endpoint.
        endDot = CreateDot(
            "Ending Point",
            endPosition,
            endMaterial
        );

        // Create the 3D line.
        lineObject = new GameObject("Measurement Line");

        lineRenderer =
            lineObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        lineRenderer.material = lineMaterial;

        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;

        lineRenderer.numCapVertices = 8;

        // Initialize the line.
        UpdateLine();

        // Change the reticle.
        centerReticle.color = measuringColor;

        currentState = MeasureState.Measuring;

        SetButtonText("STOP MEASURE");
    }

    // Update the endpoint as the camera moves.

    void UpdateMeasurement()
    {
        if (!hasCurrentHit)
            return;

        endPosition = currentHitPose.position;

        endDot.transform.position = endPosition;

        UpdateLine();
    }

    // Draw the white line between both points.

    void UpdateLine()
    {
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
    }

    // SECOND STATE -> THIRD STATE

    void StopMeasurement()
    {
        currentState = MeasureState.Complete;

        // Freeze the final endpoint.
        endPosition = endDot.transform.position;

        // Calculate real-world distance.
        float distance = Vector3.Distance(
            startPosition,
            endPosition
        );

        // Create the floating distance label.
        CreateDistanceLabel(distance);

        // Keep the line and both dots visible.
        centerReticle.color = measuringColor;

        SetButtonText("RESET");
    }

    // Calculate and display the measurement.

    void CreateDistanceLabel(float distance)
    {
        GameObject labelObject =
            new GameObject("Distance Label");

        distanceLabel =
            labelObject.AddComponent<TextMeshPro>();

        distanceLabel.text = distance.ToString("F2") + " m";

        distanceLabel.fontSize = 0.20f;

        distanceLabel.color = Color.white;

        distanceLabel.alignment =
            TextAlignmentOptions.Center;

        distanceLabel.fontStyle = FontStyles.Bold;

        distanceLabel.outlineWidth = 0.15f;
        distanceLabel.outlineColor = Color.black;

        Vector3 midpoint =
            (startPosition + endPosition) / 2f;

        labelObject.transform.position =
            midpoint + Vector3.up * 0.06f;

        // Face the phone camera.
        Vector3 direction =
            labelObject.transform.position -
            arCamera.transform.position;

        labelObject.transform.rotation =
            Quaternion.LookRotation(direction);
    }

    // Create a small 3D sphere.

    GameObject CreateDot(
        string objectName,
        Vector3 position,
        Material material
    )
    {
        GameObject dot = GameObject.CreatePrimitive(
            PrimitiveType.Sphere
        );

        dot.name = objectName;

        dot.transform.position = position;

        dot.transform.localScale =
            Vector3.one * dotSize;

        Renderer dotRenderer =
            dot.GetComponent<Renderer>();

        dotRenderer.material = material;

        // We do not need physical collisions.
        Collider dotCollider =
            dot.GetComponent<Collider>();

        if (dotCollider != null)
        {
            Destroy(dotCollider);
        }

        return dot;
    }

    // THIRD STATE -> FIRST STATE

    void ResetMeasurement()
    {
        if (startDot != null)
            Destroy(startDot);

        if (endDot != null)
            Destroy(endDot);

        if (lineObject != null)
            Destroy(lineObject);

        if (distanceLabel != null)
            Destroy(distanceLabel.gameObject);

        startDot = null;
        endDot = null;
        lineObject = null;
        lineRenderer = null;
        distanceLabel = null;

        centerReticle.color = normalColor;

        currentState = MeasureState.Ready;

        SetButtonText("START MEASURE");

        measureButton.interactable = hasCurrentHit;
    }

    void SetButtonText(string text)
    {
        buttonText.text = text;
    }
}