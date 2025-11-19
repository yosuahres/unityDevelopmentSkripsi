using UnityEngine;
using TMPro; // For TextMeshPro
using System.Collections.Generic;

public class RulerVisualizer : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public LineRenderer lineRenderer;
    public TextMeshPro textMeshPro; 

    public float rulerWidth = 0.001f; 
    public Material lineMaterial; 
    public GameObject tickMarkPrefab; 

    private List<GameObject> tickMarks = new List<GameObject>();
    private float updateThreshold = 0.001f; 
    private float lastMeasuredDistance = -1f;

    void Start()
    {
        InitializeComponents();
        UpdateRuler();
    }

    void Update()
    {
        UpdateRuler();
    }

    private void InitializeComponents()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        if (textMeshPro == null)
        {
            textMeshPro = GetComponentInChildren<TextMeshPro>();
            if (textMeshPro == null)
            {
                GameObject textObj = new GameObject("MeasurementText");
                textObj.transform.SetParent(transform);
                textMeshPro = textObj.AddComponent<TextMeshPro>();
                textMeshPro.fontSize = 0.05f; 
                textMeshPro.color = Color.white;
                textMeshPro.alignment = TextAlignmentOptions.Center;
                textMeshPro.rectTransform.localPosition = Vector3.zero;
                textMeshPro.rectTransform.localRotation = Quaternion.identity;
                textMeshPro.rectTransform.sizeDelta = new Vector2(0.2f, 0.1f); 
            }
        }

        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }
        else
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
        }
        lineRenderer.startWidth = rulerWidth;
        lineRenderer.endWidth = rulerWidth;
        lineRenderer.positionCount = 2; 
    }

    public void SetPoints(Transform p1, Transform p2)
    {
        startPoint = p1;
        endPoint = p2;
        UpdateRuler();
    }

    void UpdateRuler()
    {
        if (startPoint == null || endPoint == null)
        {
            lineRenderer.enabled = false;
            textMeshPro.enabled = false;
            ClearTickMarks();
            return;
        }

        lineRenderer.enabled = true;
        textMeshPro.enabled = true;

        Vector3 p1Pos = startPoint.position;
        Vector3 p2Pos = endPoint.position;

        lineRenderer.SetPosition(0, p1Pos);
        lineRenderer.SetPosition(1, p2Pos);

        float currentDistance = Vector3.Distance(p1Pos, p2Pos);

        if (Mathf.Abs(currentDistance - lastMeasuredDistance) > updateThreshold)
        {
            lastMeasuredDistance = currentDistance;
            UpdateMeasurementText(currentDistance);
            GenerateTickMarks(p1Pos, p2Pos, currentDistance);
        }
    }

    void UpdateMeasurementText(float distance)
    {
        float distanceInMm = distance * 1000f; 
        textMeshPro.text = $"{distanceInMm:F1} mm"; 

        textMeshPro.rectTransform.position = (startPoint.position + endPoint.position) / 2f;
        textMeshPro.rectTransform.rotation = Quaternion.LookRotation((startPoint.position - endPoint.position).normalized);
        textMeshPro.rectTransform.Rotate(0, 90, 0); 
    }

    void GenerateTickMarks(Vector3 p1, Vector3 p2, float distance)
    {
        ClearTickMarks();

        Vector3 direction = (p2 - p1).normalized;
        float tickSpacing = 0.001f; 
        
        Quaternion tickRotation = Quaternion.LookRotation(direction);
        tickRotation *= Quaternion.Euler(0, 90, 0); 

        for (float d = tickSpacing; d < distance; d += tickSpacing)
        {
            Vector3 tickPosition = p1 + direction * d;
            GameObject tick;

            if (tickMarkPrefab != null)
            {
                tick = Instantiate(tickMarkPrefab, tickPosition, tickRotation, this.transform);
            }
            else
            {
                tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tick.transform.SetParent(this.transform);
                tick.transform.position = tickPosition;
                tick.transform.rotation = tickRotation;
            }
            tick.transform.localScale = new Vector3(0.0005f, 0.002f, 0.0005f); 
            tickMarks.Add(tick);
        }
    }

    void ClearTickMarks()
    {
        foreach (GameObject tick in tickMarks)
        {
            Destroy(tick);
        }
        tickMarks.Clear();
    }

    void OnDestroy()
    {
        ClearTickMarks();
    }
}
