using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class OldController : MonoBehaviour {

    private VMGValues vals;
    private bool debugSkeletonLeft = false;
    private bool debugSkeletonRight = false;
    public bool recording = false;
    public bool autopilot = false; //Boolean for the movement of to be conducted by the neural network
    public bool online = false;

    public GameObject chosenObject;

    private bool vmg10mode = false;

    public int COMPORT_LeftGlove = 3;
    public int COMPORT_RightGlove = 10;

    private bool UseRotation_Left = true;
    private bool UseRotation_Right = true;

    private StreamWriter writer1 = null;
    private StreamWriter writer2 = null;

    //root is common to both left and right hand
    //these will be used for future purposes
    public Transform root;
    public Transform spine;

    //right hand joints
    public Transform clavicleR;
    public Transform upperArmR;
    public Transform lowerArmR;
    public Transform handR;
    public Transform[] ThumbR = new Transform[3];
    public Transform[] IndexR = new Transform[3];
    public Transform[] MiddleR = new Transform[3];
    public Transform[] RingR = new Transform[3];
    public Transform[] LittleR = new Transform[3];

    //right hand fingers joint angles
    Vector3[] thumbFlexAnglesR = new Vector3[3];
    Vector3[] indexFlexAnglesR = new Vector3[3];
    Vector3[] middleFlexAnglesR = new Vector3[3];
    Vector3[] ringFlexAnglesR = new Vector3[3];
    Vector3[] littleFlexAnglesR = new Vector3[3];

    Vector3 handAnglesR = new Vector3(0, 0, 0);

    //left hand joint
    public Transform clavicleL;
    public Transform upperArmL;
    public Transform lowerArmL;
    public Transform handL;
    public Transform[] ThumbL = new Transform[3];
    public Transform[] IndexL = new Transform[3];
    public Transform[] MiddleL = new Transform[3];
    public Transform[] RingL = new Transform[3];
    public Transform[] LittleL = new Transform[3];

    //left hand finger joint angles
    Vector3[] thumbFlexAnglesL = new Vector3[3];
    Vector3[] indexFlexAnglesL = new Vector3[3];
    Vector3[] middleFlexAnglesL = new Vector3[3];
    Vector3[] ringFlexAnglesL = new Vector3[3];
    Vector3[] littleFlexAnglesL = new Vector3[3];

    //communicaton between glove and unity
    VMG30_Driver gloveL = new VMG30_Driver(), gloveR = new VMG30_Driver();

    private int fingersTouching = 0;
    [HideInInspector]
    public int sensorvalues = -1;
    [HideInInspector]
    public float tableHeight = 1.55f;

    public int openTreshold = 1000; //How much does the hand need to open to drop the object
    private bool[] touchingFingers = new bool[5];
    private GameObject grabbedObject;

    float lastPackageR, lastPackageL;

    int[] VibroStatusR = new int[5];

    private bool canStartRec = false;

    float TimeThumbRDown;
    float TimeThumbLDown;
    bool ThumbRDown;
    bool ThumbRFire;
    bool ThumbLDown;
    bool ThumbLFire;

    float curTime = 0f, timeInterval = 0.2f, timer = 0f;

    private Vector3 posRef = new Vector3();
    private Quaternion rotRef = new Quaternion();

    private bool last = false;

    // Use this for initialization
    void Start()
    {
        TimeThumbRDown = 0.0f;
        TimeThumbLDown = 0.0f;
        ThumbRDown = false;
        ThumbLDown = false;
        ThumbRFire = false;
        ThumbLFire = false;

        int i = 0;
        for (i = 0; i < 5; i++)
        {
            VibroStatusR[i] = 0;
            //VibroStatusL[i] = 0;
        }

        Debug.Log("Start\n");
        if (!autopilot)
        {
            lastPackageR = Time.fixedTime;
            /*
            lastPackageL = Time.fixedTime;

            gloveL.Init(COMPORT_LeftGlove, Constants.LeftHanded, Constants.PKG_QUAT_FINGER);
            gloveL.StartCommunication();
            */

            gloveR.Init(COMPORT_RightGlove, Constants.RightHanded, Constants.PKG_QUAT_FINGER);
            gloveR.StartCommunication();
        }
        else
        {
            DemoReader.Init(online);
            if (!online)
                DemoReader.ReadNext();
            else
                this.GetComponent<SocketFloat>().ServerRequest(new float[DemoReader.dataSize]); //Send zeros at the start

            //decople hand
            handR.transform.parent = null;
        }


    }

    void OnApplicationQuit()
    {
        Debug.Log("Close app\n");

        if (writer2 != null)
        {
            RecordDemostration(vals, true);
            writer2.Close();
        }

        if (writer1 != null)
            writer1.Close();


        if (!autopilot)
        {
            gloveR.StopCommunication();
            gloveL.StopCommunication();


            while (gloveR.ThreadStatus)
            {

            }
            Debug.Log("Thread right glove terminated\n");

            while (gloveL.ThreadStatus)
            {

            }
            Debug.Log("Thread left glove terminated\n");
        }
        else
        {
            DemoReader.End(online);
        }


    }

    //module angle from sensor values (0 = valmin  1000 = valmax)
    float ModulateAngle(VMGValues values, int sensorindex, float valmin, float valmax)
    {
        int sensval = 0;

        //int sensval = values.SensorValues[sensorindex];
        if (!autopilot)
        {
            sensval = Calibrate(values, sensorindex);
        }
        else
        {
            if (sensorindex < 11)
            {
                sensval = (int)DemoReader.curVals[sensorindex];
            }
            else
            {
                sensval = (int)DemoReader.curVals[sensorindex - 8];
            }
        }

        return valmin + (valmax - valmin) * sensval / 1000.0f;

    }


    void UpdateHandAnglesLeft(VMGValues values)
    {
        //thumb1_l
        if (!vmg10mode)
        {
            thumbFlexAnglesL[0][0] = ModulateAngle(values, SensorIndexLeftHanded.AbdThumb, 40.0f, 90.0f); //roll over X is controller by abduction value 
            thumbFlexAnglesL[0][1] = ModulateAngle(values, SensorIndexLeftHanded.AbdThumb, -110.0f, -100.0f); //roll over X is controller by abduction value 
            thumbFlexAnglesL[0][2] = ModulateAngle(values, SensorIndexLeftHanded.PalmArch, -60.0f, -120.0f); //roll over X is controller by abduction value 
        }
        else
        {
            thumbFlexAnglesL[0][0] = 75.0f;// ModulateAngle(values, SensorIndexLeftHanded.AbdThumb, 50.0f, 90.0f); //roll over X is controller by abduction value 
            thumbFlexAnglesL[0][1] = -105.0f;// ModulateAngle(values, SensorIndexLeftHanded.AbdThumb, -110.0f, -100.0f); //roll over X is controller by abduction value 
            thumbFlexAnglesL[0][2] = ModulateAngle(values, SensorIndexLeftHanded.ThumbPh2, -50.0f, -100.0f); //roll over X is controller by abduction value 
        }

        //thumb2_l
        if (!vmg10mode)
        {
            thumbFlexAnglesL[1][0] = 0.0f;
            thumbFlexAnglesL[1][1] = 0.0f;
            thumbFlexAnglesL[1][2] = ModulateAngle(values, SensorIndexLeftHanded.PalmArch, 10.0f, -30.0f); //roll over X is controller by abduction value 
        }
        else
        {
            thumbFlexAnglesL[1][0] = 0.0f;
            thumbFlexAnglesL[1][1] = 0.0f;
            thumbFlexAnglesL[1][2] = ModulateAngle(values, SensorIndexLeftHanded.ThumbPh2, 10.0f, -30.0f); //roll over X is controller by abduction value 
        }

        //thumb3_l
        thumbFlexAnglesL[2][0] = 0.0f;
        thumbFlexAnglesL[2][1] = 0.0f;
        thumbFlexAnglesL[2][2] = ModulateAngle(values, SensorIndexLeftHanded.ThumbPh2, 15.0f, -50.0f); //roll over X is controller by abduction value 


        //index1_l
        if (!vmg10mode)
        {
            indexFlexAnglesL[0][0] = 15.0f;
            indexFlexAnglesL[0][1] = ModulateAngle(values, SensorIndexLeftHanded.AbdIndex, -20.0f, 5.0f); //roll over X is controller by abduction value 
            indexFlexAnglesL[0][2] = ModulateAngle(values, SensorIndexLeftHanded.IndexPh1, 0.0f, -45.0f); //roll over X is controller by abduction value 
        }
        else
        {
            indexFlexAnglesL[0][0] = 15.0f;
            indexFlexAnglesL[0][1] = -10.0f;// ModulateAngle(values, SensorIndexLeftHanded.AbdIndex, -20.0f, 5.0f); //roll over X is controller by abduction value 
            indexFlexAnglesL[0][2] = ModulateAngle(values, SensorIndexLeftHanded.IndexPh2, 0.0f, -50.0f); //roll over X is controller by abduction value 
        }

        //index2_l
        indexFlexAnglesL[1][0] = 0.0f;
        indexFlexAnglesL[1][1] = 0.0f;
        indexFlexAnglesL[1][2] = ModulateAngle(values, SensorIndexLeftHanded.IndexPh2, 0.0f, -50.0f); //roll over X is controller by abduction value 

        //index3_l
        indexFlexAnglesL[2][0] = 0.0f;
        indexFlexAnglesL[2][1] = 0.0f;
        indexFlexAnglesL[2][2] = ModulateAngle(values, SensorIndexLeftHanded.IndexPh2, 0.0f, -55.0f); //roll over X is controller by abduction value 

        //Debug.Log("LLLLThumbAbd: " + values.SensorValues[SensorIndexLeftHanded.AbdThumb] + "  PalmArch: " + values.SensorValues[SensorIndexLeftHanded.PalmArch] + "  IndexAdb: " + values.SensorValues[SensorIndexLeftHanded.AbdIndex] + "  IndexPh: " + values.SensorValues[SensorIndexLeftHanded.IndexPh1]);

        //middle1_l
        if (!vmg10mode)
        {
            middleFlexAnglesL[0][0] = 4.50f;
            middleFlexAnglesL[0][1] = 0.0f;
            middleFlexAnglesL[0][2] = ModulateAngle(values, SensorIndexLeftHanded.MiddlePh1, 0.0f, -50.0f); //roll over X is controller by abduction value 
        }
        else
        {
            middleFlexAnglesL[0][0] = 4.50f;
            middleFlexAnglesL[0][1] = 0.0f;
            middleFlexAnglesL[0][2] = ModulateAngle(values, SensorIndexLeftHanded.MiddlePh2, 0.0f, -50.0f); //roll over X is controller by abduction value 
        }

        //middle2_l
        middleFlexAnglesL[1][0] = 0.0f;
        middleFlexAnglesL[1][1] = 0.0f;
        middleFlexAnglesL[1][2] = ModulateAngle(values, SensorIndexLeftHanded.MiddlePh2, 0.0f, -50.0f); //roll over X is controller by abduction value 

        //middle3_l
        middleFlexAnglesL[2][0] = 0.0f;
        middleFlexAnglesL[2][1] = 0.0f;
        middleFlexAnglesL[2][2] = ModulateAngle(values, SensorIndexLeftHanded.MiddlePh2, 0.0f, -55.0f); //roll over X is controller by abduction value 


        //ring1_l
        if (!vmg10mode)
        {
            ringFlexAnglesL[0][0] = 5.0f;
            ringFlexAnglesL[0][1] = ModulateAngle(values, SensorIndexLeftHanded.AbdRing, 20.0f, -5.0f); //roll over X is controller by abduction value 
            ringFlexAnglesL[0][2] = ModulateAngle(values, SensorIndexLeftHanded.RingPh1, 0.0f, -50.0f); //roll over X is controller by abduction value 
        }
        else
        {
            ringFlexAnglesL[0][0] = 5.0f;
            ringFlexAnglesL[0][1] = 10.0f;// ModulateAngle(values, SensorIndexLeftHanded.AbdRing, 20.0f, -5.0f); //roll over X is controller by abduction value 
            ringFlexAnglesL[0][2] = ModulateAngle(values, SensorIndexLeftHanded.RingPh2, 0.0f, -50.0f); //roll over X is controller by abduction value 
        }

        //ring2_l
        ringFlexAnglesL[1][0] = 0.0f;
        ringFlexAnglesL[1][1] = 0.0f;
        ringFlexAnglesL[1][2] = ModulateAngle(values, SensorIndexLeftHanded.RingPh2, 0.0f, -50.0f); //roll over X is controller by abduction value 

        //ring3_l
        ringFlexAnglesL[2][0] = 0.0f;
        ringFlexAnglesL[2][1] = 0.0f;
        ringFlexAnglesL[2][2] = ModulateAngle(values, SensorIndexLeftHanded.RingPh2, 0.0f, -55.0f); //roll over X is controller by abduction value 


        //little1_l
        if (vmg10mode)
        {
            littleFlexAnglesL[0][0] = 5.0f;
            littleFlexAnglesL[0][1] = 10.0f;// ModulateAngle(values, SensorIndexLeftHanded.AbdLittle, 25.0f, -5.0f); //roll over X is controller by abduction value 
            littleFlexAnglesL[0][2] = ModulateAngle(values, SensorIndexLeftHanded.LittlePh1, 0.0f, -50.0f); //roll over X is controller by abduction value 
        }
        else
        {
            littleFlexAnglesL[0][0] = 5.0f;
            littleFlexAnglesL[0][1] = ModulateAngle(values, SensorIndexLeftHanded.AbdLittle, 25.0f, -5.0f); //roll over X is controller by abduction value 
            littleFlexAnglesL[0][2] = ModulateAngle(values, SensorIndexLeftHanded.LittlePh2, 0.0f, -50.0f); //roll over X is controller by abduction value 
        }
        //little2_l
        littleFlexAnglesL[1][0] = 0.0f;
        littleFlexAnglesL[1][1] = 0.0f;
        littleFlexAnglesL[1][2] = ModulateAngle(values, SensorIndexLeftHanded.LittlePh2, 0.0f, -110.0f); //roll over X is controller by abduction value 

        //little3_l
        littleFlexAnglesL[2][0] = 0.0f;
        littleFlexAnglesL[2][1] = 0.0f;
        littleFlexAnglesL[2][2] = ModulateAngle(values, SensorIndexLeftHanded.LittlePh2, 0.0f, -55.0f); //roll over X is controller by abduction value 
    }


    void UpdateHandAnglesRight(VMGValues values)
    {

        //thumb1_l
        if (!(touchingFingers[0] && sensorvalues != -1))
        {
            thumbFlexAnglesR[0][0] = ModulateAngle(values, SensorIndexRightHanded.AbdThumb, 40.0f, 95.0f); //roll over X is controller by abduction value 
                                                                                                           //thumbFlexAnglesR[0][1] = ModulateAngle(values, SensorIndexRightHanded.AbdThumb, -110.0f, -100.0f); //roll over X is controller by abduction value 
            thumbFlexAnglesR[0][1] = ModulateAngle(values, SensorIndexRightHanded.AbdThumb, -90.0f, -80.0f);
            thumbFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.PalmArch, -60.0f, -100.0f); //roll over X is controller by abduction value 

            thumbFlexAnglesR[1][0] = 0.0f;
            thumbFlexAnglesR[1][1] = 0.0f;
            //thumbFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.PalmArch, 10.0f, -30.0f); //roll over X is controller by abduction value 
            thumbFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.ThumbPh1, 10.0f, -70.0f);

            thumbFlexAnglesR[2][0] = 0.0f;
            thumbFlexAnglesR[2][1] = 0.0f;
            thumbFlexAnglesR[2][2] = ModulateAngle(values, SensorIndexRightHanded.ThumbPh2, 15.0f, -50.0f); //roll over X is controller by abduction value 
        }

        //index1_l
        if (!(touchingFingers[1] && sensorvalues != -1))
        {
            indexFlexAnglesR[0][0] = 15.0f;
            indexFlexAnglesR[0][1] = ModulateAngle(values, SensorIndexRightHanded.AbdIndex, -10.0f, 0.0f); //roll over X is controller by abduction value 
            indexFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.IndexPh1, 0.0f, -70.0f); //roll over X is controller by abduction value 

            //index2_l
            indexFlexAnglesR[1][0] = 0.0f;
            indexFlexAnglesR[1][1] = 0.0f;
            indexFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.IndexPh2, 0.0f, -100.0f); //roll over X is controller by abduction value 

            //index3_l
            indexFlexAnglesR[2][0] = 0.0f;
            indexFlexAnglesR[2][1] = 0.0f;
            indexFlexAnglesR[2][2] = ModulateAngle(values, SensorIndexRightHanded.IndexPh2, 0.0f, -55.0f); //roll over X is controller by abduction value 
        }

        //middle1_l
        if (!(touchingFingers[2] && sensorvalues != -1))
        {
            middleFlexAnglesR[0][0] = 0.0f;// 4.50f;
            middleFlexAnglesR[0][1] = 0.0f;
            middleFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.MiddlePh1, 0.0f, -70.0f); //roll over X is controller by abduction value 

            //middle2_l
            middleFlexAnglesR[1][0] = 0.0f;
            middleFlexAnglesR[1][1] = 0.0f;
            middleFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.MiddlePh2, 0.0f, -100.0f); //roll over X is controller by abduction value 

            //middle3_l
            middleFlexAnglesR[2][0] = 0.0f;
            middleFlexAnglesR[2][1] = 0.0f;
            middleFlexAnglesR[2][2] = ModulateAngle(values, SensorIndexRightHanded.MiddlePh2, 0.0f, -55.0f); //roll over X is controller by abduction value
        }

        //ring1_l
        if (!(touchingFingers[3] && sensorvalues != -1))
        {
            ringFlexAnglesR[0][0] = 0.0f;// 5.0f;
            ringFlexAnglesR[0][1] = ModulateAngle(values, SensorIndexRightHanded.AbdRing, 20.0f, -5.0f); //roll over X is controller by abduction value 
            ringFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.RingPh1, 0.0f, -70.0f); //roll over X is controller by abduction value 

            //ring2_l
            ringFlexAnglesR[1][0] = 0.0f;
            ringFlexAnglesR[1][1] = 0.0f;
            ringFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.RingPh2, 0.0f, -100.0f); //roll over X is controller by abduction value 

            //ring3_l
            ringFlexAnglesR[2][0] = 0.0f;
            ringFlexAnglesR[2][1] = 0.0f;
            ringFlexAnglesR[2][2] = ModulateAngle(values, SensorIndexRightHanded.RingPh2, 0.0f, -55.0f); //roll over X is controller by abduction value 
        }

        //little1_l
        if (!(touchingFingers[4] && sensorvalues != -1))
        {
            littleFlexAnglesR[0][0] = 0.0f;// 5.0f;                                                                                 
            littleFlexAnglesR[0][1] = ModulateAngle(values, SensorIndexRightHanded.AbdLittle, 25.0f, 5.0f); //rotation over Y is controller by abduction value 
            littleFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.LittlePh1, 0.0f, -70.0f); //rotation over Z is controller by little1 flexion 

            //little2_l
            littleFlexAnglesR[1][0] = 0.0f;
            littleFlexAnglesR[1][1] = 0.0f;
            littleFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.LittlePh2, 0.0f, -100.0f); //roll over Z is controller by a2nd phalange flexion

            //little3_l
            littleFlexAnglesR[2][0] = 0.0f;
            littleFlexAnglesR[2][1] = 0.0f;
            littleFlexAnglesR[2][2] = ModulateAngle(values, SensorIndexRightHanded.LittlePh2, 0.0f, -55.0f); //
        }
    }


    void Update()
    {
        //Quaternion prod = chosenObject.transform.rotation * Quaternion.Inverse(handR.transform.rotation);
        //Debug.Log("Co: " + chosenObject.transform.rotation + " H: " + handR.transform.rotation + " Prod: " + prod + " Al: " + chosenObject.transform.rotation * Quaternion.Inverse(prod));
        //Debug.Log("Al: " + chosenObject.transform.rotation * Quaternion.Inverse(prod) + " m: " + prod * Quaternion.Inverse(chosenObject.transform.rotation) + " " + Quaternion.Inverse(prod) * Quaternion.Inverse(chosenObject.transform.rotation) + " " + prod * chosenObject.transform.rotation);
        timer += Time.deltaTime;

        if (!autopilot)
        {
            //check pressure value L and R
            VMGValues vl = gloveL.GetPackage();
            VMGValues vr = gloveR.GetPackage();

            ////////Talvez depois fazer isto frame a frame, mas por agora fazer com intervalos de tempo 
            /////////////(tenho medo que de frame a frame faça com que esteja a treinar muito para a coisa ficar no mesmo sítio visto os frames serem muito rápidos)

            //GameObject game = GameObject.FindGameObjectWithTag("GameController");
            //GameController scriptg = game.GetComponent<GameController>();

            if (gloveR.Connected)
            {
                //DrawSkeletonRight(vr);
                if (recording && canStartRec && ((curTime + timeInterval) < timer))
                {
                    RecordDemostration(vr, false);

                    curTime = timer;
                }
            }

            if (gloveL.Connected)
            {
                //DrawSkeletonLeft(vl);
            }

            //Debug.Log("C:" + chosenObject.transform.rotation + " T:" + this.transform.rotation + " Inv:" + (chosenObject.transform.rotation * Quaternion.Inverse(this.transform.rotation) * this.transform.rotation));
        }
        else
        {
            if ((curTime + timeInterval) < timer)
            {
                if (!online)
                    DemoReader.ReadNext();
                else
                    this.GetComponent<SocketFloat>().ServerRequest(GetCurVals());

                //New Hand Rotation
                Quaternion difAngle = new Quaternion(DemoReader.GetRot().x, DemoReader.GetRot().y, DemoReader.GetRot().z, DemoReader.GetRot().w);
                Quaternion newAngle = new Quaternion();

                Quaternion referential;

                if (sensorvalues == -1)
                    referential = chosenObject.transform.rotation;
                else
                    referential = rotRef;

                newAngle = Quaternion.Inverse(difAngle) * referential;

                handR.transform.rotation = newAngle;


                float tableBoundary = tableHeight + Mathf.Abs(Mathf.Sin(handR.transform.eulerAngles.z * Mathf.Deg2Rad)) * handR.GetComponent<BoxCollider>().size.z;

                //Update hand position
                if (sensorvalues == -1)
                {
                    //handR.transform.position = chosenObject.transform.position - DemoReader.GetPos();
                    if ((chosenObject.transform.position - DemoReader.GetPos()).y > tableBoundary)
                        handR.transform.position = chosenObject.transform.position - DemoReader.GetPos();
                    else
                        handR.transform.position = new Vector3((chosenObject.transform.position - DemoReader.GetPos()).x, tableBoundary, (chosenObject.transform.position - DemoReader.GetPos()).z);
                }
                else
                {
                    //handR.transform.position = posRef - DemoReader.GetPos();
                    if ((posRef - DemoReader.GetPos()).y > tableBoundary)
                        handR.transform.position = posRef - DemoReader.GetPos();
                    else
                        handR.transform.position = new Vector3((posRef - DemoReader.GetPos()).x, tableBoundary, (posRef - DemoReader.GetPos()).z);
                }

                //To avoid the hand sinking on the table
                //if (handR.transform.position.y < tableBoundary)
                //    handR.transform.position = new Vector3(handR.transform.position.x, tableBoundary, handR.transform.position.z);

                curTime = timer;
            }
        }
    }

    public int[] ReturnSensorVals()
    {
        int[] recVals = new int[8];

        for (int i = 2; i < 10; i++)
        {
            recVals[i - 2] = vals.SensorValues[i];
        }

        return recVals;
    }

    void RecordDemostration(VMGValues v, bool final)
    {
        //if writer1 is null then writer2 is also null, so create both 
        if (writer1 == null)
        {
            string path = "Assets/Recordings/";
            int num = 1;

            //Get name for next file 
            while (true)
            {
                if (!File.Exists(path + "reach" + num.ToString() + ".txt"))
                    break;
                else
                    num++;
            }

            File.WriteAllText(path + "reach" + num.ToString() + ".txt", "");

            writer1 = new StreamWriter(path + "reach" + num.ToString() + ".txt", true);

            //start writer1 at 0
            writer1.WriteLine("-");

            for (int i = 0; i < 21; i++)
                writer1.WriteLine("0");

            //writer2
            num = 1;

            //Get name for next file 
            while (true)
            {
                if (!File.Exists(path + "manip" + num.ToString() + ".txt"))
                    break;
                else
                    num++;
            }

            File.WriteAllText(path + "manip" + num.ToString() + ".txt", "");

            writer2 = new StreamWriter(path + "manip" + num.ToString() + ".txt", true);
        }

        StreamWriter writer = null;

        //record on reach in case the object hasnt been grabbed yet, on manipulation in case the object is grabbed
        if (sensorvalues == -1)
            writer = writer1;
        else
            writer = writer2;

        //Step separator
        writer.WriteLine("-");

        //Write the values for each sensor
        for (int i = 0; i < 11; i++)
            writer.WriteLine(Calibrate(v, i));

        for (int i = 19; i < 21; i++)
            writer.WriteLine(Calibrate(v, i));

        //If object is not grabbed, use object as reference, else use saved referencial 
        if (sensorvalues == -1)
        {
            writer.WriteLine(chosenObject.transform.position.x - handR.transform.position.x); //pos22-24
            writer.WriteLine(chosenObject.transform.position.y - handR.transform.position.y); //pos22-24
            writer.WriteLine(chosenObject.transform.position.z - handR.transform.position.z); //pos22-24


            Quaternion invQ = Quaternion.Inverse(new Quaternion(handR.transform.rotation.x, handR.transform.rotation.y, handR.transform.rotation.z, handR.transform.rotation.w));
            Quaternion difAngle = new Quaternion(chosenObject.transform.rotation.x, chosenObject.transform.rotation.y, chosenObject.transform.rotation.z, chosenObject.transform.rotation.w) * invQ;

            writer.WriteLine(difAngle.x);
            writer.WriteLine(difAngle.y);
            writer.WriteLine(difAngle.z);
            writer.WriteLine(difAngle.w);
        }
        else
        {
            writer.WriteLine(posRef.x - handR.transform.position.x); //pos22-24
            writer.WriteLine(posRef.y - handR.transform.position.y); //pos22-24
            writer.WriteLine(posRef.z - handR.transform.position.z); //pos22-24


            Quaternion invQ = Quaternion.Inverse(new Quaternion(handR.transform.rotation.x, handR.transform.rotation.y, handR.transform.rotation.z, handR.transform.rotation.w));
            Quaternion difAngle = new Quaternion(rotRef.x, rotRef.y, rotRef.z, rotRef.w) * invQ;

            writer.WriteLine(difAngle.x);
            writer.WriteLine(difAngle.y);
            writer.WriteLine(difAngle.z);
            writer.WriteLine(difAngle.w);
        }

        if (final)
            writer.WriteLine("1");
        else
            writer.WriteLine("0");

        //writer.WriteLine(new Quaternion(chosenObject.transform.rotation.w, chosenObject.transform.rotation.x, chosenObject.transform.rotation.y, chosenObject.transform.rotation.z) * invQ); //pos25-29

    }

    public void UpdateVibro(int gloveindex, int index, int newvalue)
    {
        if (!autopilot)
        {
            if (gloveindex == 0)
            {
                gloveR.SetVibroTactile(index, newvalue);
            }
            else
            {
                gloveL.SetVibroTactile(index, newvalue);
            }
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        int i = 0;
        //update index

        //float[] sensVal = new float[10];
        //WGetWinTracker(ref sensVal, 0);

        if (!autopilot)
        {
            if (gloveL.Reconnect)
            {
                //Debug.Log("Restart gloveL\n");
                gloveL.Reconnect = false;
                lastPackageL = Time.fixedTime;
                gloveL.StartCommunication();
            }


            if (gloveL.StartNewCalibrationFlag)
            {
                if (!gloveL.Connected)
                {
                    bool ret = gloveL.SendCalibrationPackage();
                    if (!ret)
                    {
                        Debug.Log("Error, cannot open glove serial port. Calibration not started\n");
                    }
                }
            }

            if (gloveL.NewPackageAvailable())
            {


                lastPackageL = Time.fixedTime;
                VMGValues v = gloveL.GetPackage();

                //get thumb value
                int sensval = v.SensorValues[SensorIndexLeftHanded.ThumbPh2];
                if (sensval > 600)
                {
                    if (!ThumbLDown)
                    {
                        ThumbLDown = true;
                        TimeThumbLDown = Time.fixedTime;
                    }
                }
                else if (sensval < 400)
                {
                    if (ThumbLDown)
                    {
                        ThumbLDown = false;
                        float dtime = Time.fixedTime - TimeThumbLDown;
                        if (dtime < 0.5)
                        {
                            ThumbLFire = true;
                        }
                    }
                }



                //update values, this depends on the hand bones definition, please chenge this part in your application
                UpdateHandAnglesLeft(v);

                //update finger rendering
                for (i = 0; i < 3; i++)
                {
                    ThumbL[i].localRotation = Quaternion.Euler(thumbFlexAnglesL[i]);
                    IndexL[i].localRotation = Quaternion.Euler(indexFlexAnglesL[i]);
                    MiddleL[i].localRotation = Quaternion.Euler(middleFlexAnglesL[i]);
                    RingL[i].localRotation = Quaternion.Euler(ringFlexAnglesL[i]);
                    LittleL[i].localRotation = Quaternion.Euler(littleFlexAnglesL[i]);
                }

                //Debug.Log("thumb 1: " + ThumbL[0].localRotation + "  thumb 2: " + ThumbL[1].localRotation + "  thumb 3: " + ThumbL[2].localRotation);



                if (UseRotation_Left)
                {
                    Vector3 Zaxis = new Vector3(0.0f, 0.0f, 1.0f);
                    Vector3 Zrot = new Vector3();

                    if (!autopilot)
                        Zrot = Quaternion.Euler(v.pitchW, -v.yawW + gloveL.GetYaw0(), v.rollW) * Zaxis;
                    else
                        Zrot = Quaternion.Euler(v.pitchW, -v.yawW + gloveL.GetYaw0(), v.rollW) * Zaxis;


                    float yaw = Mathf.Rad2Deg * Mathf.Atan2(-Zrot[0], Zrot[2]);

                    float yawD = 90.0f - yaw;
                    float yawUpper = 1.5f * yawD / 3.0f;
                    float yawLower = 1.5f * yawD / 3.0f;
                    if (yawLower < 0.0f)
                    {
                        yawLower = 0.0f;
                        yawUpper = yawD;
                    }

                    //eseguo roll 0.5 su hand 0.4 su lowerarm e 0.1 su upper arm
                    upperArmL.localRotation = Quaternion.Euler(0.0f, 0.0f, yawUpper);
                    lowerArmL.localRotation = Quaternion.Euler(0.0f, 0.0f, yawLower);


                    //apply to upperArm another rotation around xaxis
                    //get xaxis on global coordinate
                    Vector3 xaxis = upperArmL.TransformVector(new Vector3(1.0f, 0.0f, 0.0f));
                    upperArmL.RotateAround(upperArmL.position, xaxis, v.pitchW);

                    //roll, rotation along xaxis of lowerArm and xaxis of hand
                    xaxis = lowerArmL.TransformVector(new Vector3(1.0f, 0.0f, 0.0f));
                    upperArmL.RotateAround(upperArmL.position, xaxis, 0.25f * v.rollW);
                    lowerArmL.RotateAround(lowerArmL.position, xaxis, 0.65f * v.rollW);
                    handL.localRotation = Quaternion.Euler(-90.0f + 0.1f * v.rollW, 0.0f, 0.0f);


                    //compute hand pitch angle (roll and yaw come from wrist)
                    Zrot = Quaternion.Euler(v.pitchH, -v.yawW + gloveL.GetYaw0(), v.rollW) * Zaxis;

                    //get ZRot in the lowerhand reference frame
                    Vector3 ZrotLowerArm = lowerArmL.InverseTransformVector(Zrot);

                    float pitchHandRel = Mathf.Rad2Deg * Mathf.Atan2(ZrotLowerArm[2], -ZrotLowerArm[0]);

                    //rotate hand along its own Z axis
                    Vector3 ZaxisHand = handL.TransformVector(new Vector3(0.0f, 0.0f, 1.0f));
                    handL.RotateAround(handL.position, ZaxisHand, pitchHandRel);
                }
                if (debugSkeletonLeft)
                {
                    //Debug.Log("New package L\n");
                    DrawSkeletonLeft(v);
                }
            }
            else
            {
                float dtime = Time.fixedTime - lastPackageL;
                if (dtime > Constants.GLOVE_TIMEOUT)
                {
                    //Debug.Log("Stop glove L\n");
                    lastPackageL = Time.fixedTime;
                    gloveL.StopCommunication();
                }
            }

            if (gloveR.Reconnect)
            {
                //Debug.Log("Restart glove R\n");
                gloveR.Reconnect = false;
                lastPackageR = Time.fixedTime;
                gloveR.StartCommunication();
            }
            //check if a new package is arrived from glove
            if (gloveR.NewPackageAvailable())
            {
                if (!canStartRec)
                    canStartRec = true;

                lastPackageR = Time.fixedTime;
                //Debug.Log("New package R\n");
                VMGValues v = gloveR.GetPackage();
                //temp
                vals = v;

                int sensval = v.SensorValues[SensorIndexRightHanded.ThumbPh2];
                if (sensval > 600)
                {
                    if (!ThumbRDown)
                    {
                        ThumbRDown = true;
                        TimeThumbRDown = Time.fixedTime;
                    }
                }
                else if (sensval < 400)
                {
                    if (ThumbRDown)
                    {
                        ThumbRDown = false;
                        float dtime = Time.fixedTime - TimeThumbRDown;
                        if (dtime < 0.5)
                        {
                            ThumbRFire = true;
                        }
                    }
                }

                //update values, this depends on the hand bones definition, please chenge this part in your application
                UpdateHandAnglesRight(v);

                if (sensorvalues != -1)
                {
                    //Free object in case the hand opens more than the treshold
                    if (SumSensors() < (sensorvalues - openTreshold))
                    {
                        sensorvalues = -1;
                        grabbedObject.transform.parent = null;
                        grabbedObject.GetComponent<Rigidbody>().useGravity = true;
                        grabbedObject.GetComponent<Rigidbody>().isKinematic = false;
                        grabbedObject.GetComponent<Rigidbody>().AddForce(handR.gameObject.GetComponent<Rigidbody>().velocity);
                    }
                }


                //update fingers rendering position and rotation
                for (i = 0; i < 3; i++)
                {
                    ThumbR[i].localRotation = Quaternion.Euler(thumbFlexAnglesR[i]);
                    IndexR[i].localRotation = Quaternion.Euler(indexFlexAnglesR[i]);
                    MiddleR[i].localRotation = Quaternion.Euler(middleFlexAnglesR[i]);
                    RingR[i].localRotation = Quaternion.Euler(ringFlexAnglesR[i]);
                    LittleR[i].localRotation = Quaternion.Euler(littleFlexAnglesR[i]);
                }

                //Debug.Log("thumb 1: " + thumbFlexAnglesR[0] + "  thumb 2: " + thumbFlexAnglesR[1] + "  thumb 3: " + thumbFlexAnglesR[2]);

                if (UseRotation_Right)
                {
                    //compute wrist orientation vector taking into consideration reset yaw0
                    Vector3 Zaxis = new Vector3(0.0f, 0.0f, 1.0f);
                    Vector3 Zrot = Quaternion.Euler(v.pitchW, -v.yawW + gloveR.GetYaw0(), v.rollW) * Zaxis;

                    float yaw = Mathf.Rad2Deg * Mathf.Atan2(-Zrot[0], Zrot[2]);

                    float yawD = 90.0f + yaw;
                    float yawUpper = 1.5f * yawD / 3.0f;
                    float yawLower = 1.5f * yawD / 3.0f;

                    //yawLower cannot be less than 0
                    if (yawLower < 0.0f)
                    {
                        yawLower = 0.0f;
                        yawUpper = yawD;
                    }


                    //fix yaw value, rotation along local Z axis
                    upperArmR.localRotation = Quaternion.Euler(0.0f, 0.0f, yawUpper);
                    lowerArmR.localRotation = Quaternion.Euler(0.0f, 0.0f, yawLower);


                    //apply to upperArm another rotation around xaxis
                    //get xaxis on global coordinate
                    Vector3 xaxis = upperArmR.TransformVector(new Vector3(1.0f, 0.0f, 0.0f));
                    upperArmR.RotateAround(upperArmR.position, xaxis, v.pitchW);

                    //roll, rotation along xaxis of lowerArm and xaxis of hand
                    xaxis = lowerArmR.TransformVector(new Vector3(1.0f, 0.0f, 0.0f));
                    upperArmR.RotateAround(upperArmR.position, xaxis, -0.25f * v.rollW);
                    lowerArmR.RotateAround(lowerArmR.position, xaxis, -0.65f * v.rollW);
                    handR.localRotation = Quaternion.Euler(-90.0f - 0.1f * v.rollW, 0.0f, 0.0f);


                    //compute hand pitch relative angle
                    //get ZRot in the lowerhand reference frame
                    Zrot = Quaternion.Euler(v.pitchH, -v.yawW + gloveR.GetYaw0(), v.rollW) * Zaxis;
                    Vector3 ZrotLowerArm = lowerArmR.InverseTransformVector(Zrot);

                    float pitchHandRel = Mathf.Rad2Deg * Mathf.Atan2(-ZrotLowerArm[2], ZrotLowerArm[0]);

                    //rotate hand along its own Z axis
                    Vector3 ZaxisHand = handR.TransformVector(new Vector3(0.0f, 0.0f, 1.0f));
                    handR.RotateAround(handR.position, ZaxisHand, pitchHandRel);

                    //Debug.Log("Yaw0:" + gloveR.GetYaw0() + "  YawH:" + v.yawH + "  YawW:" + v.yawW);
                    //Debug.Log("PitchH:" + v.pitchH + "  *PitchW:" + v.pitchW + "  rollH:" + v.rollH + "  rollW:" + v.rollW);
                }
                if (debugSkeletonRight)
                {
                    ///Debug.Log("New package R\n");
                    DrawSkeletonRight(v);
                }
            }
            else
            {
                float dtime = Time.fixedTime - lastPackageR;
                if (dtime > Constants.GLOVE_TIMEOUT)
                {
                    //Debug.Log("Stop glove R\n");
                    lastPackageR = Time.fixedTime;
                    gloveR.StopCommunication();
                }
            }
        }
        else
        {
            ///Autopilot

            VMGValues v = null;

            UpdateHandAnglesRight(v);

            if (sensorvalues != -1)
            {
                //Free object in case the hand opens more than the treshold
                if (SumSensors() < (sensorvalues - openTreshold))
                {
                    sensorvalues = -1;
                    grabbedObject.transform.parent = null;
                    grabbedObject.GetComponent<Rigidbody>().useGravity = true;
                    grabbedObject.GetComponent<Rigidbody>().isKinematic = false;
                    grabbedObject.GetComponent<Rigidbody>().AddForce(handR.gameObject.GetComponent<Rigidbody>().velocity);
                    //Talvez adicionar uma força para o objeto ser arremessado
                }
            }


            //update fingers rendering position and rotation
            for (i = 0; i < 3; i++)
            {
                ThumbR[i].localRotation = Quaternion.Euler(thumbFlexAnglesR[i]);
                IndexR[i].localRotation = Quaternion.Euler(indexFlexAnglesR[i]);
                MiddleR[i].localRotation = Quaternion.Euler(middleFlexAnglesR[i]);
                RingR[i].localRotation = Quaternion.Euler(ringFlexAnglesR[i]);
                LittleR[i].localRotation = Quaternion.Euler(littleFlexAnglesR[i]);
            }
        }



    }


    private void DrawSkeletonLeft(VMGValues v)
    {
        //Debug.Log("RPY_W:" + v.rollW + " " + v.pitchW + " " + v.yawW + " YAW0:" + gloveL.GetYaw0() + "\n");
        Debug.DrawRay(spine.position, new Vector3(0.1f, 0.0f, 0.0f), Color.magenta);
        Debug.DrawRay(spine.position, new Vector3(0.0f, 0.1f, 0.0f), Color.green);
        Debug.DrawRay(spine.position, new Vector3(0.0f, 0.0f, 0.1f), Color.blue);
        Debug.DrawRay(spine.position, clavicleL.position - spine.position, Color.red);
        Debug.DrawRay(clavicleL.position, upperArmL.position - clavicleL.position, Color.red);
        Debug.DrawRay(upperArmL.position, lowerArmL.position - upperArmL.position, Color.red);
        Debug.DrawRay(lowerArmL.position, handL.position - lowerArmL.position, Color.red);
        Debug.DrawRay(handL.position, IndexL[0].position - handL.position, Color.red);
        Debug.DrawRay(IndexL[0].position, IndexL[1].position - IndexL[0].position, Color.red);
        Debug.DrawRay(IndexL[1].position, IndexL[2].position - IndexL[1].position, Color.red);

        Vector3 X = new Vector3(0.0f, 0.0f, 0.3f);
        Vector3 Xrot = Quaternion.Euler(v.pitchW, -v.yawW + gloveL.GetYaw0(), v.rollW) * X;

        Debug.DrawRay(lowerArmL.position, Xrot, Color.magenta);

        Xrot = Quaternion.Euler(v.pitchH, -v.yawW + gloveL.GetYaw0(), v.rollW) * X;
        Debug.DrawRay(handL.position, Xrot, Color.magenta);

        //get XRot in the lowerhand reference frame
        Vector3 XrotLowerArm = lowerArmL.InverseTransformVector(Xrot);

        float pitchHandRel = Mathf.Rad2Deg * Mathf.Atan2(XrotLowerArm[2], -XrotLowerArm[0]);

        Vector3 xaxis = handL.TransformDirection(new Vector3(0.1f, 0f, 0f));
        Vector3 yaxis = handL.TransformDirection(new Vector3(0.0f, 0.1f, 0.0f));
        Vector3 zaxis = handL.TransformDirection(new Vector3(0.0f, 0.0f, 0.1f));

        Debug.DrawRay(handL.position, xaxis, Color.red);
        Debug.DrawRay(handL.position, yaxis, Color.green);
        Debug.DrawRay(handL.position, zaxis, Color.blue);

        xaxis = lowerArmL.TransformDirection(new Vector3(0.1f, 0f, 0f));
        yaxis = lowerArmL.TransformDirection(new Vector3(0.0f, 0.1f, 0.0f));
        zaxis = lowerArmL.TransformDirection(new Vector3(0.0f, 0.0f, 0.1f));

        Debug.DrawRay(lowerArmL.position, xaxis, Color.red);
        Debug.DrawRay(lowerArmL.position, yaxis, Color.green);
        Debug.DrawRay(lowerArmL.position, zaxis, Color.blue);

        //Debug.Log("Pitch Hand Rel:" + pitchHandRel + "\n");
    }


    private void DrawSkeletonRight(VMGValues v)
    {
        //Debug.Log("RPY_W:" + v.rollW + " " + v.pitchW + " " + v.yawW + "\n");
        //Debug.Log("RPY_H:" + v.rollH + " " + v.pitchH + " " + v.yawH + "\n");
        Debug.DrawRay(spine.position, new Vector3(0.1f, 0.0f, 0.0f), Color.magenta);
        Debug.DrawRay(spine.position, new Vector3(0.0f, 0.1f, 0.0f), Color.green);
        Debug.DrawRay(spine.position, new Vector3(0.0f, 0.0f, 0.1f), Color.blue);
        Debug.DrawRay(spine.position, clavicleR.position - spine.position, Color.red);
        Debug.DrawRay(clavicleR.position, upperArmR.position - clavicleR.position, Color.red);
        Debug.DrawRay(upperArmR.position, lowerArmR.position - upperArmR.position, Color.red);
        Debug.DrawRay(lowerArmR.position, handR.position - lowerArmR.position, Color.red);

        Debug.DrawRay(handR.position, ThumbR[0].position - handR.position, Color.red);
        Debug.DrawRay(ThumbR[0].position, ThumbR[1].position - ThumbR[0].position, Color.red);
        Debug.DrawRay(ThumbR[1].position, ThumbR[2].position - ThumbR[1].position, Color.red);

        Debug.DrawRay(handR.position, IndexR[0].position - handR.position, Color.red);
        Debug.DrawRay(IndexR[0].position, IndexR[1].position - IndexR[0].position, Color.red);
        Debug.DrawRay(IndexR[1].position, IndexR[2].position - IndexR[1].position, Color.red);

        Debug.DrawRay(handR.position, LittleR[0].position - handR.position, Color.red);
        Debug.DrawRay(LittleR[0].position, LittleR[1].position - LittleR[0].position, Color.red);
        Debug.DrawRay(LittleR[1].position, LittleR[2].position - LittleR[1].position, Color.red);

        Debug.DrawRay(IndexR[0].position, LittleR[0].position - IndexR[0].position, Color.green);

        Vector3 medpos = (IndexR[0].position + LittleR[0].position);

        medpos[0] = medpos[0] / 2.0f;
        medpos[1] = medpos[1] / 2.0f;
        medpos[2] = medpos[2] / 2.0f;

        Debug.DrawRay(handR.position, medpos - handR.position, Color.green);


        Vector3 X = new Vector3(0.0f, 0.0f, 0.3f);
        Vector3 Xrot = Quaternion.Euler(v.pitchW, -v.yawW + gloveR.GetYaw0(), v.rollW) * X;

        Debug.DrawRay(lowerArmR.position, Xrot, Color.magenta);

        Xrot = Quaternion.Euler(v.pitchH, -v.yawW + gloveR.GetYaw0(), v.rollW) * X;
        Debug.DrawRay(handR.position, Xrot, Color.magenta);

        //get XRot in the lowerhand reference frame
        Vector3 XrotLowerArm = lowerArmR.InverseTransformVector(Xrot);

        float pitchHandRel = Mathf.Rad2Deg * Mathf.Atan2(XrotLowerArm[2], XrotLowerArm[0]);

        //Debug.Log("Pitch Hand Rel:" + pitchHandRel + "\n");

        Xrot.Normalize();
        float yawRot = 180.0f * ((float)System.Math.Atan2(Xrot[0], Xrot[2])) / 3.14159f;


        Vector3 xaxis = lowerArmR.TransformDirection(new Vector3(0.1f, 0f, 0f));
        Vector3 yaxis = lowerArmR.TransformDirection(new Vector3(0.0f, 0.1f, 0.0f));
        Vector3 zaxis = lowerArmR.TransformDirection(new Vector3(0.0f, 0.0f, 0.1f));

        Debug.DrawRay(lowerArmR.position, xaxis, Color.red);
        Debug.DrawRay(lowerArmR.position, yaxis, Color.green);
        Debug.DrawRay(lowerArmR.position, zaxis, Color.blue);

        xaxis = upperArmR.TransformDirection(new Vector3(0.1f, 0f, 0f));
        yaxis = upperArmR.TransformDirection(new Vector3(0.0f, 0.1f, 0.0f));
        zaxis = upperArmR.TransformDirection(new Vector3(0.0f, 0.0f, 0.1f));

        Debug.DrawRay(upperArmR.position, xaxis, Color.red);
        Debug.DrawRay(upperArmR.position, yaxis, Color.green);
        Debug.DrawRay(upperArmR.position, zaxis, Color.blue);

        xaxis = handR.TransformDirection(new Vector3(0.1f, 0f, 0f));
        yaxis = handR.TransformDirection(new Vector3(0.0f, 0.1f, 0.0f));
        zaxis = handR.TransformDirection(new Vector3(0.0f, 0.0f, 0.1f));

        Debug.DrawRay(handR.position, xaxis, Color.red);
        Debug.DrawRay(handR.position, yaxis, Color.green);
        Debug.DrawRay(handR.position, zaxis, Color.blue);


        //Debug.Log("QUATW:" + v.q0w.ToString("F3") + " " + v.q1w.ToString("F3") + " " + v.q2w.ToString("F3") + " " + v.q3w.ToString("F3") + "\n");
        //Debug.Log("QUATH:" + v.q0h.ToString("F3") + " " + v.q1h.ToString("F3") + " " + v.q2h.ToString("F3") + " " + v.q3h.ToString("F3") + "\n");

    }

    //Converts the values read from the sensors to values between 0 and 1000
    private int Calibrate(VMGValues values, int sensor)
    {
        float aux = 0f;

        //special case of lateral finger movement
        if (sensor == 20 || sensor == 21 || sensor == 22)
        {
            aux = (float)(values.SensorValues[20] - Calibrator.offset[20]) / (float)(Calibrator.max[20] - Calibrator.offset[20]);
            aux += (float)(values.SensorValues[21] - Calibrator.offset[21]) / (float)(Calibrator.max[21] - Calibrator.offset[21]);
            aux += (float)(values.SensorValues[22] - Calibrator.offset[22]) / (float)(Calibrator.max[22] - Calibrator.offset[22]);
            aux = aux / 3f;
        }
        else
            aux = (float)(values.SensorValues[sensor] - Calibrator.offset[sensor]) / (float)(Calibrator.max[sensor] - Calibrator.offset[sensor]);

        float res = aux * 1000f;

        return (int)res;

    }

    public void AddnRemoveContact(int val, int finger, GameObject gObject)
    {
        fingersTouching += val;

        ////Não faço ideia porquê mas as funçoes deles tem os dedos do meio e anelar trocados, isto é só emendar as coisas
        int patch = 0;

        if (finger == 2)
            patch = 3;
        else if (finger == 3)
            patch = 2;

        if (patch != 0)
            finger = patch;

        if (val > 0)
            touchingFingers[finger] = true;
        else
            touchingFingers[finger] = false;

        //save the openess offset
        if (fingersTouching >= 6) //Condition of thumb + other finger touching the object
        {
            if (sensorvalues == -1 && val > 0)
            {
                if (recording)
                    RecordDemostration(vals, true);

                sensorvalues = SumSensors();
                grabbedObject = chosenObject;
                grabbedObject.GetComponent<Rigidbody>().useGravity = false;
                grabbedObject.GetComponent<Rigidbody>().isKinematic = true;
                grabbedObject.transform.SetParent(handR);

                posRef = grabbedObject.transform.position;
                rotRef = grabbedObject.transform.rotation;

                if (!last)
                    last = true;

            }
            else
            {
                sensorvalues = SumSensors();
            }
        }
    }

    private int SumSensors()
    {
        int sum = 0;

        //Consider the first 10 sensors for the hand openess measurement
        for (int i = 0; i < 5; i++)
        {
            if (touchingFingers[i])
            {
                if (!autopilot)
                {
                    sum = Calibrate(vals, 2 * i) + sum;
                    sum = Calibrate(vals, 2 * i + 1) + sum;
                }
                else
                {
                    sum += (int)DemoReader.curVals[2 * i];
                    sum += (int)DemoReader.curVals[2 * i + 1];
                }
            }
        }

        //PalmArch value
        if (touchingFingers[0])
        {
            if (!autopilot)
                sum = Calibrate(vals, 10) + sum;
            else
                sum += (int)DemoReader.curVals[10];
        }


        return sum;
    }

    private float[] GetCurVals()
    {
        float[] cVals = new float[DemoReader.dataSize];

        for (int i = 0; i < 13; i++)
            cVals[i] = DemoReader.curVals[i];



        //If object is not grabbed, use object as reference, else use saved referencial 
        if (sensorvalues == -1)
        {
            cVals[13] = chosenObject.transform.position.x - handR.transform.position.x;
            cVals[14] = chosenObject.transform.position.y - handR.transform.position.y;
            cVals[15] = chosenObject.transform.position.z - handR.transform.position.z;


            Quaternion invQ = Quaternion.Inverse(new Quaternion(handR.transform.rotation.x, handR.transform.rotation.y, handR.transform.rotation.z, handR.transform.rotation.w));
            Quaternion difAngle = new Quaternion(chosenObject.transform.rotation.x, chosenObject.transform.rotation.y, chosenObject.transform.rotation.z, chosenObject.transform.rotation.w) * invQ;

            cVals[16] = difAngle.x;
            cVals[17] = difAngle.y;
            cVals[18] = difAngle.z;
            cVals[19] = difAngle.w;
        }
        else
        {
            cVals[13] = posRef.x - handR.transform.position.x;
            cVals[14] = posRef.y - handR.transform.position.y;
            cVals[15] = posRef.z - handR.transform.position.z;


            Quaternion invQ = Quaternion.Inverse(new Quaternion(handR.transform.rotation.x, handR.transform.rotation.y, handR.transform.rotation.z, handR.transform.rotation.w));
            Quaternion difAngle = new Quaternion(rotRef.x, rotRef.y, rotRef.z, rotRef.w) * invQ;

            cVals[16] = difAngle.x;
            cVals[17] = difAngle.y;
            cVals[18] = difAngle.z;
            cVals[19] = difAngle.w;
        }

        if (last)
        {
            cVals[20] = 1.0f;
            last = false;
        }
        else
            cVals[20] = 0.0f;


        return cVals;
    }
}
