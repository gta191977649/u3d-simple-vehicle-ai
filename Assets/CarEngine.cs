using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEngine : MonoBehaviour {
    public float maxSteerAngle = 45f;
    public Color debugColor;
	public Transform pathParent;
    public Transform car;
    public Rigidbody carRig;
    public WheelCollider leftWheel;
    public WheelCollider rightWheel;
    public float power = 10.0f;
	private List<Transform> nodes;
	private int currentNode = 0;
	// Use this for initialization
	private void Start () {
		Transform[] pathsTransforms = pathParent.GetComponentsInChildren<Transform>();
		nodes = new List<Transform>();

		for(int i = 0; i < pathsTransforms.Length; i++) {
            if(pathsTransforms[i] != pathParent.transform) {
				nodes.Add(pathsTransforms[i]);
			}
		}
	}
	
	// Update is called once per frame
	private void FixedUpdate () {
        applySteer();
        drive();
    }
	private void applySteer() {
        Vector3 relativeVector = transform.InverseTransformPoint(nodes[currentNode].position);
        relativeVector = relativeVector / relativeVector.magnitude;
        //计算转向角度
        float turningAngle = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle;
        leftWheel.steerAngle = turningAngle;
        rightWheel.steerAngle = turningAngle;
        //car.eulerAngles = new Vector3(0,turningAngle,0);
        //carRig.angularVelocity = new Vector3(relativeVector.x * 5, 0, relativeVector.y * 5);
        //carRig.AddForce(5,0,0);
	}
    private void drive() {
        leftWheel.motorTorque = power;
        rightWheel.motorTorque = power;
    }
    private void checkWayPoint() {
        if(Vector3.Distance(car.position,nodes[currentNode].position) < 0.05f) {
            if(currentNode  == nodes.Count-1) {
                currentNode = 0;
            } else {
                currentNode++;
            }

        }

    }

	private void OnDrawGizmos() {
        if (nodes != null) {
            Gizmos.color = debugColor;
            Gizmos.DrawLine(transform.position, nodes[currentNode].position);
            Gizmos.DrawSphere(nodes[currentNode].position, 0.5f);
            drawString("Distance to waypoint: "+Vector3.Distance(car.position, nodes[currentNode].position),transform.position,debugColor);
        }
	}
    private void drawString(string text, Vector3 worldPos, Color? colour = null)
    {
        UnityEditor.Handles.BeginGUI();

        var restoreColor = GUI.color;

        if (colour.HasValue) GUI.color = colour.Value;
        var view = UnityEditor.SceneView.currentDrawingSceneView;
        Vector3 screenPos = view.camera.WorldToScreenPoint(worldPos);

        if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
        {
            GUI.color = restoreColor;
            UnityEditor.Handles.EndGUI();
            return;
        }

        Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
        GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height + 4, size.x, size.y), text);
        GUI.color = restoreColor;
        UnityEditor.Handles.EndGUI();
    }
}
