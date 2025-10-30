using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using Unity.VisualScripting;
using UnityEngine;

public class RDWScript : MonoBehaviour
{
    #region Variables
    [Header("Global RDW toggle")]
    public bool RDWEnabled = false;

    [Header("RDW Parameters")]
    [Tooltip("Distance at which point you'll always turn independant of linear or rotational movement")]
    [Range(0, 1)]
    public float minDistance;
    [Tooltip("rotation speed multiplier when closer as minDistance")]
    public float tooCloseMultiplier;
    [Tooltip("Total RDW strength")]
    public float totalRDWStrength;
    [Tooltip("Minimum rotational speed before activating rotationalRDW contribution")] 
    public float rotationalThreshold;
    [Tooltip("Minimum linear speed before activating linearRDW contribution")] 
    public float linearThreshold;
    [Tooltip("Multiplier for rotationalRDW contribution")] 
    public float rotationalMultiplier;
    [Tooltip("Multiplier for linearRDW contribution")] 
    public float linearMultiplier;

    [Header("Boundary RDW Parameters")]
    [Tooltip("Weight assigned to boundry RDW (versus the other techniques)")] 
    [Range(0, 1)]
    public float boundaryRDWWeight;

    [Header("Player Direciton")]
    [Tooltip("Weight assigned to player walking direction versus the facing direction)")]
    public float facingAlpha = 0.5f;
    public float posUpdateTimeMs = 0.1f;
    private Transform headTransform;
    private Vector3 prevFramePlayerPosition;
    private Vector3 prevCounterPlayerPosition;
    private Quaternion prevPlayerRotation;
    public Vector3 playerDirection;
    private Vector3 walkingVec;
    private Vector3 facingVec;

    [Header("Player RDW Parameters")]
    [Tooltip("Weight assigned to player RDW (versus the other techniques)")]
    [Range(0, 1)]
    public float playerRDWWeight;

    [Header("Circle RDW Parameters")]
    [Tooltip("Wieght assigned to circle RDW (versus the other techniques)")]
    [Range(0, 1)]
    public float circleRDWWeight;
    [Tooltip("Radius that the circle RDW determines an ideal distance from the middle")]
    [Range(10,90)]
    public float circleRDWRadius;

    [Header("Logarithmic Weight Parameters")]
    [Tooltip("Intersection with Y-axis")]
    public float a;
    [Tooltip("Steepness of the curve")]
    public float b;
    [Tooltip("Eagerness to approach asymptote")]
    public float c;
    [Tooltip("Steepness of the asymptote")]
    public float d;

    [Header("RDW contributions (readOnly)")]
    public float playerToRDWAngle;
    public float LinearRDW;
    public Vector3 totalRDWVector;

    [Header("Current Camera Info (readOnly)")]
    public Quaternion currentRotation;
    public Vector3 linearSpeed;

    [Header("Required objects")]
    public GameObject head;
    public LineDrawer lineDrawer;
    public MenuManager menuManager;

    [Header("Circle direction")]
    public float secondsToSwitch = 0.1f;
    private float cicleMultiplier = 0.1f;

    private bool arrowIsDir = true;
    private float counter = 0;
    #endregion

    #region RDW execution
    void Start()
    {
        headTransform = Camera.main.transform;

        // Save the initial rotation for reference
        prevPlayerRotation = headTransform.rotation;

        // Save the initial position for reference
        prevFramePlayerPosition = headTransform.position;
        prevCounterPlayerPosition = headTransform.position;
    }

    void Update()
    {
        counter += Time.deltaTime;

        updatePlayerDirection();

        RDW();

        // Update the initial rotation and position for the next frame
        prevPlayerRotation = headTransform.rotation;
        prevFramePlayerPosition = headTransform.position;
    }

    void RDW()
    {
        bool tooClose = true;
        float tooCloseDistance = -1;

        Vector3 playerRDW = CalculateRDWPlayers();
        Vector3 boundaryRDW = CalculateRDWBoundary(ref tooClose, ref tooCloseDistance);
        Vector3 circleRDW = CalculateRDWCircle();

        totalRDWVector = totalRDWStrength * 
            ( 
            playerRDWWeight * playerRDW
            + boundaryRDWWeight * boundaryRDW
            + circleRDWWeight * circleRDW
            );

        DebugPanel.Instance.SetTextAt(4, "totRDW", (totalRDWVector.magnitude).ToString());
        DebugPanel.Instance.SetTextAt(5, "boundaryRDW", (boundaryRDWWeight * boundaryRDW).magnitude.ToString());
        DebugPanel.Instance.SetTextAt(6, "playerRDW", (playerRDWWeight * playerRDW).magnitude.ToString());
        DebugPanel.Instance.SetTextAt(7, "circleRDW", (circleRDWWeight * circleRDW).magnitude.ToString());


        if (!arrowIsDir) lineDrawer.UpdateCanvasRotation(totalRDWVector);

        if (tooClose && tooCloseDistance != -1) RotateCamera(tooCloseMultiplier * playerToRDWAngle / 180 / 3 / tooCloseDistance);
        else RotateCamera(CalculateRotation(totalRDWVector));
    }

    float CalculateRotation(Vector3 v)
    {
        float angle = 0f;

        // ----- Dependent on rotational speed ----- //
        playerToRDWAngle = Vector3.SignedAngle(playerDirection, v.normalized, Vector3.up);
        DebugPanel.Instance.SetTextAt(1, "playerDir -> RDW angle [°]", playerToRDWAngle.ToString());

        // Extract current rotation
        currentRotation = headTransform.rotation;

        // Calculate rotational speed
        float angleChange = Quaternion.Angle(prevPlayerRotation, currentRotation);
        float rotationalSpeed = angleChange / Time.deltaTime;

        if (rotationalSpeed > rotationalThreshold)
        {
            angle += (angleChange * rotationalMultiplier * playerToRDWAngle / 180) * (v.magnitude / 5);
        }

        // ----- Dependent on linear speed ----- //
        Vector3 currentPosition = headTransform.position;

        // Calculate linear motion
        linearSpeed = (currentPosition - prevFramePlayerPosition) / Time.deltaTime;
        LinearRDW = linearSpeed.magnitude;

        if (linearSpeed.magnitude > linearThreshold)
        {
            angle += playerToRDWAngle / 180 * linearMultiplier * v.magnitude;
        }

        if(arrowIsDir) lineDrawer.UpdateCanvasRotation(playerDirection);

        return angle;
    }

    void RotateCamera(float angle)
    {
        DebugPanel.Instance.SetTextAt(9, "RDWEnabled", RDWEnabled.ToString());
        DebugPanel.Instance.SetTextAt(0, "rotation [°/sec]", (GetRotWeight(angle)/Time.deltaTime).ToString());
        if (RDWEnabled && !menuManager.IsMenuActive())        
            transform.RotateAround(head.transform.position, Vector3.down, GetRotWeight(angle)); // rotate around the head game object
    }

    private void updatePlayerDirection()
    {
        // alternative to walking direction
        if (counter > posUpdateTimeMs)
        {
            walkingVec = (headTransform.position - prevCounterPlayerPosition) / counter;
            prevCounterPlayerPosition = headTransform.position;
            counter = 0;
        }
        facingVec = headTransform.forward;
        playerDirection = facingAlpha * facingVec + (1 - facingAlpha) * walkingVec;
        playerDirection.y = 0;
        playerDirection = playerDirection.normalized;
    }
    #endregion

    #region Vector Calculations
    Vector3 CalculateRDWBoundary(ref bool tooClose, ref float tooCloseDistance)
    {
        
        GameObject[] boundaries = lineDrawer.GetBoundaryObjects();
        
        Vector3 averageWeightedDistance = Vector3.zero;

        foreach (GameObject boundary in boundaries)
        {
            Vector3 distanceVector = headTransform.position - boundary.transform.position;
            Vector3 projection = Vector3.Project(distanceVector, boundary.transform.up);
            distanceVector -= projection;
            distanceVector.y = 0;
            if (distanceVector.magnitude < minDistance)
            {
                tooCloseDistance = distanceVector.magnitude;
                tooClose = true;
            }
            float weight = GetExpWeight(distanceVector.magnitude);
            averageWeightedDistance += distanceVector.normalized * weight;
        }
        return averageWeightedDistance;
    }

    Vector3 CalculateRDWPlayers()
    {
        GameObject[] players = lineDrawer.getPlayerCylinders();
        Vector3 averageWeightedDistance = Vector3.zero;
        foreach (GameObject player in players)
        {
            if (player == null) continue;
            Vector3 distanceVector = headTransform.position - player.transform.position;
            distanceVector.y = 0;
            float weight = GetExpWeight(distanceVector.magnitude);
            averageWeightedDistance += distanceVector.normalized * weight;
        }
        return averageWeightedDistance;
    }
	
  Vector3 CalculateRDWCircle()
    {
        Vector3 originToHead = headTransform.localPosition;
        float radius = GetRadius();
        Vector3 originToCenter = lineDrawer.GetBoundaryCenter();

        Vector3 centerToPlayerVector = originToHead - originToCenter;
        centerToPlayerVector.y = 0;

        float magnitude = centerToPlayerVector.magnitude / radius;
        if (magnitude > 1) magnitude = 1;
        if (magnitude < 0) magnitude = 0;

        Vector3 circleVec = transform.TransformVector(Vector3.Cross(Vector3.up, centerToPlayerVector).normalized * magnitude);

        if (Mathf.Abs(Vector3.SignedAngle(circleVec, playerDirection, Vector3.up)) < 90) 
        {
            // Clockwise direction
            if (cicleMultiplier < 1 && playerDirection.magnitude > .002f)
            {
                cicleMultiplier += 2 * Time.deltaTime / secondsToSwitch;
            }
        }
        else 
        {
            // Counter clockwise direction
            if (cicleMultiplier > -1 && playerDirection.magnitude > .002f)
            {
                cicleMultiplier -= 2 * Time.deltaTime / secondsToSwitch;
            }
        }
        cicleMultiplier = Mathf.Clamp(cicleMultiplier, -1, 1);

        circleVec *= cicleMultiplier * 5;

        return circleVec;
    }

    public float GetRadius()
    {
        // Ensure percentage is between 0 and 100
        float radiusPercentage = Mathf.Clamp(circleRDWRadius, 0, 100);
        Vector3 center = lineDrawer.GetBoundaryCenter();
        GameObject[] boundaries = lineDrawer.GetBoundaryObjects();

        // Find the closest boundary distance
        float minDistance = float.MaxValue;
        foreach (GameObject boundary in boundaries)
        {
            Vector3 distanceVector = lineDrawer.GetBoundaryCenter() - boundary.transform.localPosition;
            distanceVector.y = 0;
            if (distanceVector.magnitude < minDistance) minDistance = distanceVector.magnitude;
        }

        return minDistance * radiusPercentage / 100;
    }

    public float GetExpWeight(float magnitude) {
        /*
         * a: Intersection with Y-axis
         * b: Steepness of the curve
         */

        return Mathf.Clamp((-magnitude + b) * (a / b), 0, int.MaxValue);
        //return Mathf.Pow(0.5f, (magnitude / b)) * a;
    }

    public float GetRotWeight(float x)
    {
        /*
         * c: Eagerness to approach asymptote
         * d: Steepness of the asymptote
         */
        if (x < 0) return -GetRotWeight(-x);
        return x * Tanh(x / 2 * c) * d;
    }

    float Tanh(float x)
    {
        return (Mathf.Exp(x) - Mathf.Exp(-x)) / (Mathf.Exp(x) + Mathf.Exp(-x));
    }
    #endregion

    public void SetArrowDirection() {
        arrowIsDir = true;
    }

    public void SetArrowRDW()
    {
        arrowIsDir = false;
    }
}
