//rulervisualizer.cs
using UnityEngine;
using TMPro; 
using System.Collections.Generic;

public class RulerVisualizer : MonoBehaviour
{
    [Header("Ruler Endpoints")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Visual Settings")]
    public LineRenderer lineRenderer;
    public TextMeshPro textMeshPro; 
    public float rulerWidth = 1f; 
    public Material lineMaterial; 

    [Header("Text Offset & Orientation")]
    [Tooltip("Distance the measurement text is offset towards the main camera.")]
    public float textOffsetTowardsCamera = 0.01f; 
    [Tooltip("If true, the text will always face the camera. If false, it will be aligned with the ruler line.")]
    public bool alwaysFaceCamera = true; 
    
    private const float MillimetersPerUnit = 1000f; 

    void Start()
    {
        InitializeComponents();
        UpdateRuler();
    }

    void LateUpdate() 
    {
        UpdateRuler();
    }

    private void InitializeComponents()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
        }

        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }
        else
        {
            Material defaultMat = new Material(Shader.Find("Sprites/Default"));
            defaultMat.color = Color.yellow;
            lineRenderer.material = defaultMat;
        }
        lineRenderer.startWidth = rulerWidth;
        lineRenderer.endWidth = rulerWidth;
        lineRenderer.positionCount = 2; 

        if (textMeshPro == null)
        {
            textMeshPro = GetComponentInChildren<TextMeshPro>();
            if (textMeshPro == null)
            {
                GameObject textObj = new GameObject("MeasurementText");
                textObj.transform.SetParent(transform);
                textMeshPro = textObj.AddComponent<TextMeshPro>();
                
                textMeshPro.fontSize = 0.1f; 
                textMeshPro.fontStyle = FontStyles.Bold; 
                textMeshPro.rectTransform.sizeDelta = new Vector2(0.4f, 0.2f); 
                textMeshPro.color = Color.white;
                textMeshPro.alignment = TextAlignmentOptions.Center;
                textMeshPro.rectTransform.localPosition = Vector3.zero;
                textMeshPro.rectTransform.localRotation = Quaternion.identity;
            }
        }
    }

    void UpdateRuler()
    {
        if (startPoint == null || endPoint == null)
        {
            lineRenderer.enabled = false;
            textMeshPro.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        textMeshPro.enabled = true;

        Vector3 p1Pos = startPoint.position;
        Vector3 p2Pos = endPoint.position;

        lineRenderer.SetPosition(0, p1Pos);
        lineRenderer.SetPosition(1, p2Pos);

        float currentDistance = Vector3.Distance(p1Pos, p2Pos);
        
        UpdateMeasurementText(p1Pos, p2Pos, currentDistance);
    }

    void UpdateMeasurementText(Vector3 p1Pos, Vector3 p2Pos, float distance)
    {
        
        Vector3 midpoint = (p1Pos + p2Pos) / 2f;
        
        
        Camera mainCamera = Camera.main;

        
        Vector3 finalPosition = midpoint;
        if (mainCamera != null && textOffsetTowardsCamera != 0f)
        {
            
            Vector3 directionToMidpoint = (midpoint - mainCamera.transform.position).normalized;
            
            
            finalPosition = midpoint + directionToMidpoint * textOffsetTowardsCamera;
        }

        textMeshPro.rectTransform.position = finalPosition;
        
        
        if (mainCamera != null && alwaysFaceCamera)
        {
             
             textMeshPro.rectTransform.rotation = mainCamera.transform.rotation;
        }
        else
        {
             
             Quaternion rotation = Quaternion.LookRotation((p1Pos - p2Pos).normalized);
             
             textMeshPro.rectTransform.rotation = rotation * Quaternion.Euler(0, 90, 0); 
        }

        
        float distanceInMm = distance * MillimetersPerUnit; 
        textMeshPro.text = $"{distanceInMm:F1}mm"; 
    }

    public void SetPoints(Transform p1, Transform p2)
    {
        startPoint = p1;
        endPoint = p2;
        UpdateRuler();
    }
}