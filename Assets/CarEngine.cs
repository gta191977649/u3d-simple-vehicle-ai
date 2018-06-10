using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEngine : MonoBehaviour
{
    public float maxSteerAngle = 45f;
    public float maxSpeed = 45f;
    public float turningSpeed = 5;
    [Header("Debug绘制颜色")]
    public Color pathColor;
    public Color textColor;
    public Color RaycastHitColor;
    public Color RaycastNormalColor;
    public float decellarationSpeed = 50f;
    public Transform pathParent;
    public Transform car;
    public WheelCollider leftWheelCollider;
    public WheelCollider rightWheelCollider;
    public Transform leftWheel;
    public Transform rightWheel;
    
    public Transform backrightWheel;
    public Transform backLeftWheel;
    public float power = 10.0f;
    private List<Transform> nodes;
    private int currentNode = 0;
    public bool isBreaking = false;
    public bool isAvoiding;
    //障碍物传感器
    public float sensorLength = 3;
    public Vector3 fronSensorPos = new Vector3(0,0,0);
    public float sideSensorOffset = 5f;
    public float frontSensorAngle = 30.0f;
    // Use this for initialization
    private float targetSteerAngle = 0;

    public bool driveBackwords = false;
    private bool isReversing = false;
    private float reverseCouter = 0.0f;
    public float waitTimeToReverse = 2.0f;
    public float reverFor = 1.5f; 
    private Rigidbody carRig = null;
    public float waypointCheckOffset = 2.0f;
    private void Start()
    {
        //获得Rigbody
        this.carRig = this.GetComponent<Rigidbody>();
        Transform[] pathsTransforms = pathParent.GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        for (int i = 0; i < pathsTransforms.Length; i++)
        {
            if (pathsTransforms[i] != pathParent.transform)
            {
                nodes.Add(pathsTransforms[i]);
            }
        }
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        sensors();
        applySteer();
        drive();
        breaking();
        applyRotationToModel();
        checkWayPoint();
        learpToSteerAngle();
    }
    private void sensors() {
        RaycastHit hit;
        Vector3 sensorsStartPos = transform.position;
        sensorsStartPos += transform.forward * fronSensorPos.y;
        sensorsStartPos += transform.up * fronSensorPos.y;

        float avoidMultiplier = 0;
        isAvoiding = false;
        //Breaking　Sensors
        if (Physics.Raycast(sensorsStartPos, transform.forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Ground"))
            {
                reverseCouter += Time.deltaTime;
                if(reverseCouter >= waitTimeToReverse){
                    reverseCouter = 0;
                    isReversing = true;
                    isAvoiding = true;
                   
                }
                Debug.DrawLine(sensorsStartPos, hit.point, RaycastHitColor);
            }
            
        }
        //右传感器
        sensorsStartPos += transform.right * sideSensorOffset;
        if(Physics.Raycast(sensorsStartPos,transform.forward,out hit,sensorLength)) {
            if(!hit.collider.CompareTag("Ground")) {
                //Debug.DrawLine(sensorsStartPos,hit.point);
                Debug.DrawLine(sensorsStartPos,hit.point,RaycastHitColor);
                isAvoiding = true;
                avoidMultiplier -= 0.5f;
            } else {
                Debug.DrawLine(sensorsStartPos,sensorsStartPos + new Vector3(0,sensorLength,0),RaycastNormalColor);
            }
        }
        //Right angle 
        else if(Physics.Raycast(sensorsStartPos,Quaternion.AngleAxis(frontSensorAngle,transform.up) * transform.forward ,out hit,sensorLength)) {
            if(!hit.collider.CompareTag("Ground")) {
                Debug.DrawLine(sensorsStartPos,hit.point,RaycastHitColor);
                isAvoiding = true;
                avoidMultiplier -= 0.5f;
            } else {
                Debug.DrawLine(sensorsStartPos,sensorsStartPos + new Vector3(0,sensorLength,0),RaycastNormalColor);
            }
        }
        
        //左传感器
        sensorsStartPos -= transform.right * sideSensorOffset * 2;
        if(Physics.Raycast(sensorsStartPos,transform.forward,out hit,sensorLength)) {
            if(!hit.collider.CompareTag("Ground")) {
               
                Debug.DrawLine(sensorsStartPos,hit.point,RaycastHitColor);
                isAvoiding = true;
                avoidMultiplier +=1f;
            } else {
                Debug.DrawLine(sensorsStartPos,sensorsStartPos + new Vector3(sensorLength,0,0),RaycastNormalColor);
            }
        }
        //Left angle 
        else if(Physics.Raycast(sensorsStartPos,Quaternion.AngleAxis(-frontSensorAngle,transform.up) * transform.forward ,out hit,sensorLength)) {
            if(!hit.collider.CompareTag("Ground")) {
                Debug.DrawLine(sensorsStartPos,hit.point,RaycastHitColor);
                isAvoiding = true;
                avoidMultiplier += 1f;
            }
        }
        if(avoidMultiplier == 0) {
         //前中间传感器
            if(Physics.Raycast(sensorsStartPos,transform.forward,out hit,sensorLength)) {
                if(!hit.collider.CompareTag("Ground")) {
                    Debug.DrawLine(sensorsStartPos,hit.point,RaycastHitColor);
                    isAvoiding = true;
                    if(hit.normal.x < 0) {
                        avoidMultiplier = -1f;
                    } else {
                        avoidMultiplier = 1f;
                    }
                } else {
                    Debug.DrawLine(sensorsStartPos,sensorsStartPos + new Vector3(sensorLength,0,0),RaycastNormalColor);
                }
            }
        }
        //倒车判定
       
        if(isReversing) {
            Debug.Log("Reversing");
            avoidMultiplier *= -1f;
            reverseCouter += Time.deltaTime;
            if(reverseCouter >= reverFor) {
                reverseCouter = 0;
                isReversing = false;
            }
        }
        if(isAvoiding &&!isReversing) { 
            targetSteerAngle = maxSteerAngle * avoidMultiplier;
        }
    }
    private void applySteer()
    {
        if(isAvoiding) return;
        Vector3 relativeVector = transform.InverseTransformPoint(nodes[currentNode].position);
        relativeVector = relativeVector / relativeVector.magnitude;
        //计算转向角度
        float turningSteer = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle;
        targetSteerAngle = turningSteer;

    }
    private void drive()
    {
        if(currentSpeed() > maxSpeed) isBreaking = true;
        else isBreaking = false;
        if(isReversing) { //倒车，给负向速度
            leftWheelCollider.motorTorque = -power;
            rightWheelCollider.motorTorque = -power;
        } else if(!isBreaking) {
            leftWheelCollider.motorTorque = power;
            rightWheelCollider.motorTorque = power;
        }
        
    }
    private float currentSpeed() {
        return 2 * Mathf.PI * leftWheelCollider.radius * rightWheelCollider.rpm * 60 / 1000;
    }
    private void breaking() {
        if(isBreaking) {
            leftWheelCollider.brakeTorque = power + 20f;
            rightWheelCollider.brakeTorque = power + 20f;
        } else {
            leftWheelCollider.brakeTorque = 0;
            rightWheelCollider.brakeTorque = 0;
        }
    }
    private void learpToSteerAngle() {
        rightWheelCollider.steerAngle = Mathf.Lerp(rightWheelCollider.steerAngle,targetSteerAngle,Time.deltaTime * turningSpeed);
        leftWheelCollider.steerAngle = Mathf.Lerp(leftWheelCollider.steerAngle,targetSteerAngle,Time.deltaTime * turningSpeed);
    }
    private void checkWayPoint()
    {
        if (Vector3.Distance(transform.position, nodes[currentNode].position) < waypointCheckOffset)
        {
            if(!driveBackwords) {
                if (currentNode == nodes.Count - 1)
                {
                    currentNode = 0;
                }
                else
                {
                    currentNode++;
                }
            } else {
                if (currentNode == 0)
                {
                    currentNode = nodes.Count - 1;
                }
                else
                {
                    currentNode--;
                }
            }
           

        }

    }
    private void applyRotationToModel() {
        leftWheel.localEulerAngles = new Vector3(leftWheel.localEulerAngles.x, leftWheelCollider.steerAngle - leftWheel.localEulerAngles.z, leftWheel.localEulerAngles.z);
        rightWheel.localEulerAngles = new Vector3(rightWheel.localEulerAngles.x, rightWheelCollider.steerAngle - rightWheel.localEulerAngles.z, rightWheel.localEulerAngles.z);
        leftWheel.Rotate(leftWheelCollider.rpm / 60 * 360 * Time.deltaTime, 0, 0);
        rightWheel.Rotate(rightWheelCollider.rpm / 60 * 360 * Time.deltaTime, 0, 0);
        backrightWheel.Rotate(rightWheelCollider.rpm / 60 * 360 * Time.deltaTime, 0, 0);
        backLeftWheel.Rotate(leftWheelCollider.rpm / 60 * 360 * Time.deltaTime, 0, 0);
        
        /* 
        rlWheel.Rotate(rlWheelCollider.rpm / 60 * 360 * Time.deltaTime, 0, 0);
        rrWheel.Rotate(rrWheelCollider.rpm / 60 * 360 * Time.deltaTime, 0, 0);
        */
    }
    private void OnDrawGizmos()
    {
        if (nodes != null)
        {
            Gizmos.color = pathColor;
            Gizmos.DrawLine(transform.position, nodes[currentNode].position);
            Gizmos.DrawSphere(nodes[currentNode].position, 0.5f);
            drawString("到节点距离: " +  Vector3.Distance(car.position, nodes[currentNode].position).ToString("F3")+ "速度: " + currentSpeed().ToString("F3"), transform.position, pathColor);
        }
        if (isBreaking) {
            Gizmos.DrawSphere(transform.position, 0.5f);
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
