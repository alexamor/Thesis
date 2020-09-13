using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;


namespace VML
{
    public class VMG30_Controller : MonoBehaviour
    {
        private VMGValues vals;
        private bool debugSkeletonLeft = true;
        private bool debugSkeletonRight = true;

        private int calibrationPhase = 0;

        public bool vmg10mode = true;

        public bool CalibrateLeft = false;
        public bool CalibrateRight = false;
        public int COMPORT_LeftGlove = 1;
        public int COMPORT_RightGlove = 2;

        public bool UseRotation_Left = false;
        public bool UseRotation_Right = false;

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

        Vector3 handAnglesL = new Vector3(0, 0, 0);


        //communicaton between glove and unity
        VMG30_Driver gloveL = new VMG30_Driver(), gloveR = new VMG30_Driver();

        private int fingersTouching = 0;
        private int sensorvalues = -1;
        public int openTreshold = 1000; //How much does the hand need to open to drop the object
        private GameObject grabbedObject;
       

        float lastPackageR, lastPackageL;


        int[] VibroStatusR = new int[5];
        int[] VibroStatusL = new int[5];


        float TimeThumbRDown;
        float TimeThumbLDown;
        bool ThumbRDown;
        bool ThumbRFire;
        bool ThumbLDown;
        bool ThumbLFire;

        // Use this for initialization
        void Start()
        {
            TimeThumbRDown = 0.0f;
            TimeThumbLDown = 0.0f;
            ThumbRDown = false;
            ThumbLDown = false;
            ThumbRFire = false;
            ThumbLFire = false;

            int i =0;
            for (i=0;i<5;i++){
                VibroStatusR[i] = 0;
                //VibroStatusL[i] = 0;
            }

            Debug.Log("Start\n");
            lastPackageR = Time.fixedTime;
            /*
            lastPackageL = Time.fixedTime;
            
            gloveL.Init(COMPORT_LeftGlove, Constants.LeftHanded, Constants.PKG_QUAT_FINGER);
            gloveL.StartCommunication();
            */

            gloveR.Init(COMPORT_RightGlove, Constants.RightHanded, Constants.PKG_QUAT_FINGER);
            gloveR.StartCommunication();

            
        }

        void OnApplicationQuit()
        {
            Debug.Log("Close app\n");
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

        //module angle from sensor values (0 = valmin  1000 = valmax)
        float ModulateAngle(VMGValues values, int sensorindex, float valmin, float valmax)
        {
            //int sensval = values.SensorValues[sensorindex];
            int sensval = Calibrate(values, sensorindex);
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
                indexFlexAnglesL[0][2] = ModulateAngle(values, SensorIndexLeftHanded.IndexPh1, 0.0f, -50.0f); //roll over X is controller by abduction value 
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
            indexFlexAnglesL[1][2] = ModulateAngle(values, SensorIndexLeftHanded.IndexPh2, 0.0f, -110.0f); //roll over X is controller by abduction value 

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
            middleFlexAnglesL[1][2] = ModulateAngle(values, SensorIndexLeftHanded.MiddlePh2, 0.0f, -110.0f); //roll over X is controller by abduction value 

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
            ringFlexAnglesL[1][2] = ModulateAngle(values, SensorIndexLeftHanded.RingPh2, 0.0f, -110.0f); //roll over X is controller by abduction value 

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
            if (!vmg10mode)
            {
                thumbFlexAnglesR[0][0] = ModulateAngle(values, SensorIndexRightHanded.AbdThumb,  50.0f, 95.0f); //roll over X is controller by abduction value 
                //thumbFlexAnglesR[0][1] = ModulateAngle(values, SensorIndexRightHanded.AbdThumb, -110.0f, -100.0f); //roll over X is controller by abduction value 
                thumbFlexAnglesR[0][1] = ModulateAngle(values, SensorIndexRightHanded.AbdThumb, -90.0f, -80.0f);
                thumbFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.PalmArch, -60.0f, -100.0f); //roll over X is controller by abduction value 
            }
            else
            {
                thumbFlexAnglesR[0][0] = 65.0f;// ModulateAngle(values, SensorIndexRightHanded.AbdThumb, -20.0f, 90.0f); //roll over X is controller by abduction value 
                thumbFlexAnglesR[0][1] = -105.0f;// ModulateAngle(values, SensorIndexRightHanded.AbdThumb, -110.0f, -100.0f); //roll over X is controller by abduction value 
                thumbFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.ThumbPh2, -50.0f, -100.0f); //roll over X is controller by abduction value 
            }

            //thumb2_l
            if (!vmg10mode)
            {
                thumbFlexAnglesR[1][0] = 0.0f;
                thumbFlexAnglesR[1][1] = 0.0f;
                //thumbFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.PalmArch, 10.0f, -30.0f); //roll over X is controller by abduction value 
                thumbFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.ThumbPh1, 10.0f, -70.0f);
            }
            else
            {
                thumbFlexAnglesR[1][0] = 0.0f;
                thumbFlexAnglesR[1][1] = 0.0f;
                thumbFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.ThumbPh2, 10.0f, -30.0f); //roll over X is controller by abduction value 
            }

            //thumb3_l
            thumbFlexAnglesR[2][0] = 0.0f;
            thumbFlexAnglesR[2][1] = 0.0f;
            thumbFlexAnglesR[2][2] = ModulateAngle(values, SensorIndexRightHanded.ThumbPh2, 15.0f, -50.0f); //roll over X is controller by abduction value 


            //index1_l
            if (!vmg10mode)
            {
                indexFlexAnglesR[0][0] = 15.0f;
                indexFlexAnglesR[0][1] = ModulateAngle(values, SensorIndexRightHanded.AbdIndex, -10.0f, 0.0f); //roll over X is controller by abduction value 
                indexFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.IndexPh1, 0.0f, -70.0f); //roll over X is controller by abduction value 
            }
            else
            {
                indexFlexAnglesR[0][0] = 15.0f;
                indexFlexAnglesR[0][1] = -5.0f;// ModulateAngle(values, SensorIndexRightHanded.AbdIndex, 0.0f, -10.0f); //roll over X is controller by abduction value 
                indexFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.IndexPh2, 0.0f, -50.0f); //roll over X is controller by abduction value 
            }

            //index2_l
            indexFlexAnglesR[1][0] = 0.0f;
            indexFlexAnglesR[1][1] = 0.0f;
            indexFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.IndexPh2, 0.0f, -130.0f); //roll over X is controller by abduction value 

            //index3_l
            indexFlexAnglesR[2][0] = 0.0f;
            indexFlexAnglesR[2][1] = 0.0f;
            indexFlexAnglesR[2][2] = ModulateAngle(values, SensorIndexRightHanded.IndexPh2, 0.0f, -55.0f); //roll over X is controller by abduction value 

            //Debug.Log("RRRRTAbd: " + values.SensorValues[SensorIndexRightHanded.AbdThumb] + "  PA: " + values.SensorValues[SensorIndexRightHanded.PalmArch] + "  ThumbPh1: " + values.SensorValues[SensorIndexRightHanded.ThumbPh1] + "  TPh2: " + values.SensorValues[SensorIndexRightHanded.ThumbPh2]);


            //middle1_l
            if (!vmg10mode)
            {
                middleFlexAnglesR[0][0] = 0.0f;// 4.50f;
                middleFlexAnglesR[0][1] = 0.0f;
                middleFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.MiddlePh1, 0.0f, -70.0f); //roll over X is controller by abduction value 
            }
            else
            {
                middleFlexAnglesR[0][0] = 0.0f;// 4.50f;
                middleFlexAnglesR[0][1] = 0.0f;
                middleFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.MiddlePh2, 0.0f, -50.0f); //roll over X is controller by abduction value 
            }

            //middle2_l
            middleFlexAnglesR[1][0] = 0.0f;
            middleFlexAnglesR[1][1] = 0.0f;
            middleFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.MiddlePh2, 0.0f, -130.0f); //roll over X is controller by abduction value 

            //middle3_l
            middleFlexAnglesR[2][0] = 0.0f;
            middleFlexAnglesR[2][1] = 0.0f;
            middleFlexAnglesR[2][2] = ModulateAngle(values, SensorIndexRightHanded.MiddlePh2, 0.0f, -55.0f); //roll over X is controller by abduction value 


            //ring1_l
            if (!vmg10mode)
            {
                ringFlexAnglesR[0][0] = 0.0f;// 5.0f;
                ringFlexAnglesR[0][1] = ModulateAngle(values, SensorIndexRightHanded.AbdRing, 20.0f, -5.0f); //roll over X is controller by abduction value 
                ringFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.RingPh1, 0.0f, -70.0f); //roll over X is controller by abduction value 
            }
            else
            {
                ringFlexAnglesR[0][0] = 0.0f;// 5.0f;
                ringFlexAnglesR[0][1] = 10.0f;// ModulateAngle(values, SensorIndexRightHanded.AbdRing, 20.0f, -5.0f); //roll over X is controller by abduction value 
                ringFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.RingPh2, 0.0f, -50.0f); //roll over X is controller by abduction value 
            }

            //ring2_l
            ringFlexAnglesR[1][0] = 0.0f;
            ringFlexAnglesR[1][1] = 0.0f;
            ringFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.RingPh2, 0.0f, -130.0f); //roll over X is controller by abduction value 

            //ring3_l
            ringFlexAnglesR[2][0] = 0.0f;
            ringFlexAnglesR[2][1] = 0.0f;
            ringFlexAnglesR[2][2] = ModulateAngle(values, SensorIndexRightHanded.RingPh2, 0.0f, -55.0f); //roll over X is controller by abduction value 


            //little1_l
            if (!vmg10mode)
            {
                littleFlexAnglesR[0][0] = 0.0f;// 5.0f;                                                                                 
                littleFlexAnglesR[0][1] = ModulateAngle(values, SensorIndexRightHanded.AbdLittle, 25.0f, 5.0f); //rotation over Y is controller by abduction value 
                littleFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.LittlePh1, 0.0f, -70.0f); //rotation over Z is controller by little1 flexion 
            }
            else
            {
                littleFlexAnglesR[0][0] = 0.0f;// 5.0f;                                                                                 
                littleFlexAnglesR[0][1] = 10.0f;// ModulateAngle(values, SensorIndexRightHanded.AbdLittle, 25.0f, 5.0f); //rotation over Y is controller by abduction value 
                littleFlexAnglesR[0][2] = ModulateAngle(values, SensorIndexRightHanded.LittlePh2, 0.0f, -50.0f); //rotation over Z is controller by little1 flexion 
            }

            //little2_l
            littleFlexAnglesR[1][0] = 0.0f;
            littleFlexAnglesR[1][1] = 0.0f;
            littleFlexAnglesR[1][2] = ModulateAngle(values, SensorIndexRightHanded.LittlePh2, 0.0f, -130.0f); //roll over Z is controller by a2nd phalange flexion

            //little3_l
            littleFlexAnglesR[2][0] = 0.0f;
            littleFlexAnglesR[2][1] = 0.0f;
            littleFlexAnglesR[2][2] = ModulateAngle(values, SensorIndexRightHanded.LittlePh2, 0.0f, -55.0f); //

        }


        void Update()
        {
            //check pressure value L and R
            VMGValues vl = gloveL.GetPackage();
            VMGValues vr = gloveR.GetPackage();

            float calibTimestamp = 0, curTime = 0;

            //GameObject game = GameObject.FindGameObjectWithTag("GameController");
            //GameController scriptg = game.GetComponent<GameController>();

            if (gloveR.Connected)
            {
                //DrawSkeletonRight(vr);

            }

            if (gloveL.Connected)
            {
                //DrawSkeletonLeft(vl);

                if(Input.GetKeyDown("1"))
                {
                    CalibMessages(1);
                    gloveL.StartCalibration(1);
                }

                if (Input.GetKeyDown("2"))
                {
                    CalibMessages(2);
                    gloveL.StartCalibration(2);
                }

                if (Input.GetKeyDown("3"))
                {
                    CalibMessages(3);
                    gloveL.StartCalibration(3);
                }

                if (Input.GetKeyDown("4"))
                {
                    CalibMessages(4);
                    gloveL.StartCalibration(4);
                }

                if (Input.GetKeyDown("0"))
                {
                    CalibMessages(0);
                    gloveL.StartCalibration(0);
                }

                //There is a 3 second timer somewhere
                if (CalibrateLeft && Time.time > 5)
                {
                    //Start Calibration
                    if (calibrationPhase == 0)
                    {
                        calibrationPhase = 1;
                        calibTimestamp = Time.time;
                        CalibMessages(1);
                    }

                    curTime = Time.time;

                    //Do a second per calibration phase
                    if(curTime > (calibTimestamp + 1))
                    {
                        gloveL.StartCalibration(0);
                        gloveL.StartCalibration(calibrationPhase);

                        calibrationPhase++;

                        if(calibrationPhase == 8)
                        {
                            calibrationPhase = 0;
                            CalibrateLeft = false;
                        }

                        CalibMessages(calibrationPhase);
                        
                        calibTimestamp = Time.time;
                    }

                    
                }
            }
        }

        private void CalibMessages(int phase)
        {
            if (phase == 1)
                Debug.Log("Fingers Calibration");
            else if (phase == 2)
                Debug.Log("Abduction calibration");
            else if (phase == 3)
                Debug.Log("Thumb Calibration");
            else if (phase == 4)
                Debug.Log("Touch Little and Thumb");
            else if (phase == 5)
                Debug.Log("Touch Ring and Thumb");
            else if (phase == 6)
                Debug.Log("Touch Middle and Thumb");
            else if (phase == 7)
                Debug.Log("Touch Index and Thumb");
            else
                Debug.Log("Done!");
        }

        public void UpdateVibro(int gloveindex, int index, int newvalue)
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

        // Update is called once per frame
        void LateUpdate()
        {
            int i = 0;
            //update index

            //float[] sensVal = new float[10];
            //WGetWinTracker(ref sensVal, 0);

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
                    if (!ret){
                        Debug.Log("Error, cannot open glove serial port. Calibration not started\n");
                    }
                }
            }

            if (gloveL.NewPackageAvailable())
            {
                

                lastPackageL = Time.fixedTime;
                VMGValues v = gloveL.GetPackage();

                //get thumb value
                int sensval =  v.SensorValues[SensorIndexLeftHanded.ThumbPh2];
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
                    Vector3 Zrot = Quaternion.Euler(v.pitchW, -v.yawW + gloveL.GetYaw0(), v.rollW) * Zaxis;

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

                //Only update finger movement in case an object is not being grabbed
                if(sensorvalues == -1)
                {
                    //update values, this depends on the hand bones definition, please chenge this part in your application
                    UpdateHandAnglesRight(v);
                }
                else
                {
                    //Free object in case the hand opens more than the treshold
                    if(SumSensors() < (sensorvalues - openTreshold))
                    {
                        sensorvalues = -1;
                        grabbedObject.transform.parent = null;
                        grabbedObject.GetComponent<Rigidbody>().useGravity = true;
                        grabbedObject.GetComponent<Rigidbody>().isKinematic = false;
                        grabbedObject.GetComponent<Rigidbody>().AddForce(handR.gameObject.GetComponent<Rigidbody>().velocity);

                        for (int j = 0; j < 5; j++)
                            UpdateVibro(0, j, 0);
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
            Vector3 Xrot = Quaternion.Euler(v.pitchW, -v.yawW+gloveL.GetYaw0(), v.rollW) * X;

            Debug.DrawRay(lowerArmL.position, Xrot, Color.magenta);

            Xrot = Quaternion.Euler(v.pitchH, -v.yawW+gloveL.GetYaw0(), v.rollW) * X;
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
            Vector3 Xrot = Quaternion.Euler(v.pitchW, -v.yawW+gloveR.GetYaw0(), v.rollW) * X;

            Debug.DrawRay(lowerArmR.position, Xrot, Color.magenta);

            Xrot = Quaternion.Euler(v.pitchH, -v.yawW+gloveR.GetYaw0(), v.rollW) * X;
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
            //Sensor indexes
            /* 0 = ThumbPh2
             * 1 = ThumbPh1
             * 2 = IndexPh2
             * 3 = IndexPh1
             * 4 = MiddlePh2
             * 5 = MiddlePh1
             * 6 = RingPh2
             * 7 = RingPh1
             * 8 = LittlePh2
             * 9 = LittlePh1
             * 10 = PalmArch
             * 11 = 
             * 12 = 
             * 13 = 
             * 14 = 
             * 15 = 
             * 16 = 
             * 17 = 
             * 18 = 
             * 19 = AbdThumb
             * 20 = AbdIndex
             * 21 = AbdRing
             * 22 = AbdLittle
             */

            //values from which sensor starts
            int[] offset = {310,250,450,320,320,280,400,300,300,380,380,0,0,0,0,0,0,0,0,700,955,900,825};

            //max value returned by each sensor
            int[] max = {830,550,950,800,880,800,910,710,900,700,750,1000,1000,1000,1000,1000,1000,1000,1000,910,980,1000,880};

            //to avoid rounding errors
            float aux = (float)(values.SensorValues[sensor] - offset[sensor]) / (float)(max[sensor] - offset[sensor]);
            float res = aux * 1000f;

            return (int) res;

        }

        public void AddnRemoveContact(bool add, GameObject gObject)
        {
            if(add)
            {
                fingersTouching++;

                //save the openess offset
                if (fingersTouching == 5)
                {
                    sensorvalues = SumSensors();
                    grabbedObject = gObject;
                    grabbedObject.GetComponent<Rigidbody>().useGravity = false;
                    grabbedObject.GetComponent<Rigidbody>().isKinematic = true;
                    grabbedObject.transform.SetParent(handR);
                    
                    for(int i = 0; i < 5; i++)
                        UpdateVibro(0, i, 90);
                }
            }
            else
            {
                if (fingersTouching > 0)
                    fingersTouching--;
            }

            Debug.Log("Fingers: " + fingersTouching);
        }

        private int SumSensors()
        {
            int sum = 0;

            //Consider the first 10 sensors for the hand openess measurement
            for (int i = 0; i < 11; i++)
                sum = Calibrate(vals, i) + sum;

            return sum;
        }
    }

}
