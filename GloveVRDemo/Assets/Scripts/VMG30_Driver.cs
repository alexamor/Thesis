using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;




public class VMGValues
{
    public float q0h, q1h, q2h, q3h;    //!< hand quaternion representing hand orientation
    public float q0w, q1w, q2w, q3w;    //!< wrist quaternion representing wrist rotation

    public float rollH, pitchH, yawH;   //!< hand orientation;
    public float rollW, pitchW, yawW;   //!< wrist orientation

    public int [] SensorValues = new int[Constants.NumSensors];      //!< all values from dataglove sensors

    public int[] VibroPower = new int[Constants.NumVibroTactile];

    public int timestamp   ;   //!< values representing package generation tick in ms

    public int VibroMod;

    public int BatteryStatus;

    public void ResetValues()
    {
        //reset allsensor values
        q0h = 0.0f; q1h = 0.0f; q2h = 0.0f; q3h = 0.0f;
        q0w = 0.0f; q1w = 0.0f; q2w = 0.0f; q3w = 0.0f;

        rollH = 0.0f; pitchH = 0.0f; yawH = 0.0f;
        rollW = 0.0f; pitchW = 0.0f; yawW = 0.0f;

        int i = 0;
        for (i = 0; i < Constants.NumSensors; i++) SensorValues[i] = 0;
        for (i = 0; i < Constants.NumVibroTactile; i++) VibroPower[i] = 0;
        VibroMod = 0;
        timestamp = 0;
        BatteryStatus = 500;
    }
}

public class VMG30_Driver {

    private int ComPort = 1;                                //!< comport used for dataglove communication
    private int GloveType = Constants.RightHanded;          //!< Rightor Left Handed
    private int GloveStreamMode = Constants.PKG_QUAT_FINGER;  //!< streaming package mode

    public VMGValues sensorValues = new VMGValues();

    private bool _newPackageAvailable;
    private bool _updateVibroTactile = false;

    private bool ComThreadRunning = false;
    private Thread comThread;

    private object _lock = new object();

  
    private object _lockVibro = new object();


    private float YAW0; //set initial yaw angle in order to correctly align dataglove to the environment
    private Quaternion q0;

    //imu values mean filtering
    private int IMUFilter;
    List<Quaternion> lQuatW = new List<Quaternion>();
    List<Quaternion> lQuatH = new List<Quaternion>();


    private SerialPort vmgcom;

    public bool Connected = false, Reconnect = false;

    public int CalibrationPhase = 0;
    public bool StartNewCalibrationFlag = false;
    private float CalibrationStartTime;

    public bool ThreadStatus;

    /*** driver initialization 
     /pars comport dataglove communication port
     /pars type dataglove type (RightHanded or LeftHanded)
     /pars stream streaming type
     ***/
    public void Init(int comport, int type, int stream)
    {
        ComPort = comport;
        GloveType = type;
        GloveStreamMode = stream;
        sensorValues.ResetValues();
        _newPackageAvailable = false;
        _updateVibroTactile = true; //force vibro to reset values
        IMUFilter = FilterConst.Filter_High ;
    }

    /*** Start dataglove communication */
    public void StartCommunication()
    {
        //open comport
        
        //start stream reading thread
        ComThreadRunning = true;
        comThread = new Thread(GloveCommunication) { Name = "GloveCommunication" };
        comThread.Start();
        ThreadStatus = true;
    }

    public void SetYaw0(float yaw0)
    {
        YAW0 = yaw0;
    }

    public float GetYaw0()
    {
        return YAW0;
    }

    public Quaternion GetQ0()
    {
        return q0;
    }

    public void StopCommunication()
    {
        ComThreadRunning = false;
        Connected = false;
    }


    /*** return true if a new package is available from the streaming */
    public bool NewPackageAvailable()
    {
        bool retval;
        lock (_lock)
        {
            retval = _newPackageAvailable;
            _newPackageAvailable = false;
        }
        return retval;
    }

    public VMGValues GetPackage()
    {
        VMGValues ret;
        lock (_lock)
        {
            ret = sensorValues;
        }
        return ret;
    }

    public void SetIMUFilter(int value)
    {
        IMUFilter = value;
    }

    public void SetVibroTactile(int index, int value)
    {
        //Debug.Log("Update vibro " + index + " " + value + "\n");
        lock (_lockVibro)
        {
            if ((index >= 0) && (index < 5))
            {
                if (value != sensorValues.VibroPower[index])
                {
                    _updateVibroTactile = true;
                    sensorValues.VibroPower[index] = 255*value/100;
                }
            }
        }
    }

    /// REquest to start a new calibration process
    /// 
    /// <param name="phase"> 0 = stop calibration, 1...7 calibration phase
    /// Phase 1: fingers calibration
    /// Phase 2: abduction calibration
    /// Phase 3: thuimb calibration
    /// Phase 4: Touch little and thumb
    /// Phase 5: Touch ring and thumb
    /// Phase 6: Touch midlle and thumb
    /// Phase 7: Touch ring and thumb
    public void StartCalibration(int phase)
    {
        if (Connected)
        {
            StopCommunication();
        }
        CalibrationPhase = phase;
        StartNewCalibrationFlag = true;
    }

    /// <summary>Send a package to the dataglove in order to start or update a new calibrationphase
    /// Send phase = 0 to stop calibration
    /// <returns>True if package sent, false otherwise</returns>
    public bool SendCalibrationPackage()
    {
        byte[] SendBuffer = new byte[256];
        string str; str = "COM" + ComPort;
        Debug.Log("cal " + str);

        StartNewCalibrationFlag = false;

        CalibrationStartTime = Time.fixedTime;
        /*
        if (vmgcom.IsOpen)
            vmgcom.Close();

        vmgcom = new SerialPort(str, 230400);
        //vmgcom = new SerialPort(str, 230400, Parity.None, 8, StopBits.One);
        //Debug.Log("Status: " + vmgcom.IsOpen);

        try
        {
            vmgcom.Open();
            //Debug.Log("Open");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }

        Debug.Log("Status: " + vmgcom.IsOpen);
        */
        if (vmgcom.IsOpen)
        {
            Debug.Log("Cal Serial port correctly opened\n");
            SendBuffer[0] = (byte)'$';
            SendBuffer[1] = (byte)0x21;
            SendBuffer[2] = (byte)0x03;
            SendBuffer[3] = (byte)CalibrationPhase;
            SendBuffer[4] = (byte)(SendBuffer[0] + SendBuffer[1] + SendBuffer[2] + SendBuffer[3]);
            SendBuffer[5] = (byte)'#';
            vmgcom.Write(SendBuffer, 0, 6);
            vmgcom.Close();

            //vmgcom = new SerialPort(str, 230400, Parity.None, 8, StopBits.One);
            //vmgcom.Open();

            return true;
        }
        return false;
    }

    private void GloveCommunication()
    {
        bool FirstPackage = true;
        byte[] SendBuffer = new byte[256];
        byte[] RecvBuffer = new byte[1024];
        int NumBytesRecv = 0;
        int NumPkgRecv = 0;
       
        bool vmgcomOk = false;

        foreach (string str2 in SerialPort.GetPortNames())
        {
            Debug.Log(string.Format("Existing COM port: {0}", str2));
        }

        string str; str = "COM" + ComPort;

        if (ComPort >= 10) str = "\\\\.\\COM" + ComPort;

        Debug.Log("Open " + str + "\n");

        vmgcom = new SerialPort(str, 230400, Parity.None, 8, StopBits.One);
        try
        {
            vmgcom.Open();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }

        Thread.Sleep(4000);

        if (vmgcom.IsOpen)
        {
            Debug.Log("Serial port correctly opened\n");
            vmgcomOk = true;
            Connected = true;

        }
        else
        {
            Debug.Log("Serial port error\n");
            vmgcomOk = false;
            Connected = false;
        }




        //wait 3 seconds before sending first package
       
        //if comport opened succesfully then send start streaming command
        if (vmgcomOk)
        {
            vmgcom.ReadTimeout = 1;
            //send start streaming
            SendBuffer[0] = (byte)'$';
            SendBuffer[1] = (byte)0x0a;
            SendBuffer[2] = (byte)0x03;
            SendBuffer[3] = (byte)Constants.PKG_QUAT_FINGER;
            SendBuffer[4] = (byte)(SendBuffer[0] + SendBuffer[1] + SendBuffer[2] + SendBuffer[3]);
            SendBuffer[5] = (byte)'#';
            vmgcom.Write(SendBuffer, 0, 6);
        }
        else
        {
            while (ComThreadRunning)
            {
                
            }
        }
       
        _updateVibroTactile = true;
        int u = 0;
        for (u = 0; u < 5; u++) sensorValues.VibroPower[u] = 0;

        //Debug.Log("Thread Started\n");

        long vibrotime = System.DateTime.Now.Ticks + 300000000;

        while ((ComThreadRunning)&&(vmgcomOk))
        {
            //Debug.Log("Read Bytes\n");
            try
            {
                //read bytes from the dataglove stream
                long currvibroTime = System.DateTime.Now.Ticks;
                long dvibrotime = currvibroTime - vibrotime;
                //attendo 1.5s prima di mandare il pacchetto
                if ((_updateVibroTactile)||(dvibrotime>15000000))
                {
                    //Debug.Log("Update vibrotactile\n");
                    vibrotime = System.DateTime.Now.Ticks;

                    int v0, v1, v2, v3, v4;
                    lock (_lockVibro)
                    {
                        _updateVibroTactile = false;
                        //invio pacchetto con valori vibrotattile
                        v0 = sensorValues.VibroPower[0];
                        v1 = sensorValues.VibroPower[1];
                        v2 = sensorValues.VibroPower[2];
                        v3 = sensorValues.VibroPower[3];
                        v4 = sensorValues.VibroPower[4];
                    }

                    //Debug.Log("UPDATE VIBRO " + ComPort + " "+  v0 + " " + v1 + " " + v2 + " " + v3 + " " + v4 + "\n");
                    SendBuffer[0] = (byte)'$';
                    SendBuffer[1] = 0x60;
                    SendBuffer[2] = 0x08;
                    SendBuffer[3] = (byte)v0;
                    SendBuffer[4] = (byte)v1;
                    SendBuffer[5] = (byte)v2;
                    SendBuffer[6] = (byte)v3;
                    SendBuffer[7] = (byte)v4;
                    SendBuffer[8] = (byte)v4;
                    SendBuffer[9] = (byte)('$' + 0x60 + 0x08 + v0 + v1 + v2 + v3 + v4 + v4);//0xba + 0x02);
                    SendBuffer[10] = (byte)'#';
                    vmgcom.Write(SendBuffer, 0, 11);

                }
                
                int bytesRead = vmgcom.Read(RecvBuffer, NumBytesRecv, 20);
                if (bytesRead > 0)
                {
                    NumBytesRecv += bytesRead;
                    //check if a valid package is present in the buffer
                    if (NumBytesRecv > Constants.VMG30_PKG_SIZE)
                    {
                        //check header
                        int i = 0;
                        byte bcc = 0;
                        bool HeaderFound = false;
                        while ((!HeaderFound) && (i < (NumBytesRecv - 2)))
                        {
                            if ((RecvBuffer[i] == '$') && (RecvBuffer[i + 1] == 0x0a)) HeaderFound = true;
                            else i++;
                        }
                        //if header found then parse package
                        if (HeaderFound)
                        {
                            //i == pos header
                            //bcc
                            int pospackage = i + 2;
                            bcc = ((byte)'$') + 0x0a;
                            //package len
                            byte pkglen = RecvBuffer[pospackage];
                            if ((pkglen + pospackage) < NumBytesRecv)
                            {
                                //package found
                                //see dataglove datasheet for package definitions

                                //check bcc
                                byte bccrecv = RecvBuffer[pospackage + pkglen - 1];
                                for (i = 0; i < pkglen - 1; i++)
                                {
                                    bcc += RecvBuffer[pospackage + i];
                                }

                                //if bcc is correct and package termination found then check for sensors values
                                if ((bcc == bccrecv) && (RecvBuffer[pospackage + pkglen] == '#'))
                                {
                                    //parse package
                                    int datastart = pospackage + 1;

                                    //check initial information, package type, glove id and package timestamp
                                    int pkgtype = RecvBuffer[datastart]; datastart++;
                                    int id = RecvBuffer[datastart] * 256 + RecvBuffer[datastart + 1]; datastart += 2;
                                    int timestamp = (RecvBuffer[datastart] << 24) + (RecvBuffer[datastart + 1] << 16) + (RecvBuffer[datastart + 2] << 8) + (RecvBuffer[datastart + 3]); datastart += 4;

                                    //Debug.Log("ID:" + id + " Time:" + timestamp + "\n");


                                    if (pkgtype == Constants.PKG_QUAT_FINGER)
                                    {
                                        //get quaternion values
                                        int q0w = (RecvBuffer[datastart] << 24) + (RecvBuffer[datastart + 1] << 16) + (RecvBuffer[datastart + 2] << 8) + (RecvBuffer[datastart + 3]); datastart += 4;
                                        int q1w = (RecvBuffer[datastart] << 24) + (RecvBuffer[datastart + 1] << 16) + (RecvBuffer[datastart + 2] << 8) + (RecvBuffer[datastart + 3]); datastart += 4;
                                        int q2w = (RecvBuffer[datastart] << 24) + (RecvBuffer[datastart + 1] << 16) + (RecvBuffer[datastart + 2] << 8) + (RecvBuffer[datastart + 3]); datastart += 4;
                                        int q3w = (RecvBuffer[datastart] << 24) + (RecvBuffer[datastart + 1] << 16) + (RecvBuffer[datastart + 2] << 8) + (RecvBuffer[datastart + 3]); datastart += 4;

                                        int q0h = (RecvBuffer[datastart] << 24) + (RecvBuffer[datastart + 1] << 16) + (RecvBuffer[datastart + 2] << 8) + (RecvBuffer[datastart + 3]); datastart += 4;
                                        int q1h = (RecvBuffer[datastart] << 24) + (RecvBuffer[datastart + 1] << 16) + (RecvBuffer[datastart + 2] << 8) + (RecvBuffer[datastart + 3]); datastart += 4;
                                        int q2h = (RecvBuffer[datastart] << 24) + (RecvBuffer[datastart + 1] << 16) + (RecvBuffer[datastart + 2] << 8) + (RecvBuffer[datastart + 3]); datastart += 4;
                                        int q3h = (RecvBuffer[datastart] << 24) + (RecvBuffer[datastart + 1] << 16) + (RecvBuffer[datastart + 2] << 8) + (RecvBuffer[datastart + 3]); datastart += 4;

                                        //get fingers values
                                        int[] sensors = new int[Constants.NumSensors];
                                        for (i = 0; i < Constants.NumSensors; i++)
                                        {
                                            sensors[i] = (RecvBuffer[datastart] << 8) + RecvBuffer[datastart + 1]; datastart += 2;
                                        }

                                        int battery = (RecvBuffer[datastart] << 8) + RecvBuffer[datastart + 1];

                                        //Debug.Log("Battery" + battery + "\n");

                                        //convert quaternions to float
                                        float q00H = (float)(q0h / 65536.0);
                                        float q11H = (float)(q1h / 65536.0);
                                        float q22H = (float)(q2h / 65536.0);
                                        float q33H = (float)(q3h / 65536.0);

                                        float q00W = (float)(q0w / 65536.0);
                                        float q11W = (float)(q1w / 65536.0);
                                        float q22W = (float)(q2w / 65536.0);
                                        float q33W = (float)(q3w / 65536.0);

                                        lQuatW.Add(new Quaternion(q00W, q11W, q22W, q33W));
                                        lQuatH.Add(new Quaternion(q00H, q11H, q22H, q33H));

                                        if (IMUFilter > 0)
                                        {
                                            if (lQuatW.Count > IMUFilter)
                                            {
                                                lQuatW.RemoveAt(0);
                                            }

                                            if (lQuatH.Count > IMUFilter)
                                            {
                                                lQuatH.RemoveAt(0);
                                            }

                                            //get new values, filter quaternions for wrist and hand
                                            float q0wsum = 0.0f, q1wsum = 0.0f, q2wsum = 0.0f, q3wsum = 0.0f;
                                            int numval = lQuatW.Count;
                                            for (i = 0; i < numval; i++)
                                            {
                                                Quaternion q = lQuatW[i];
                                                q0wsum += q.x;
                                                q1wsum += q.y;
                                                q2wsum += q.z;
                                                q3wsum += q.w;
                                            }

                                            q00W = q0wsum / numval;
                                            q11W = q1wsum / numval;
                                            q22W = q2wsum / numval;
                                            q33W = q3wsum / numval;

                                            //hand quanternion
                                            float q0hsum = 0.0f, q1hsum = 0.0f, q2hsum = 0.0f, q3hsum = 0.0f;
                                            numval = lQuatH.Count;
                                            for (i = 0; i < numval; i++)
                                            {
                                                Quaternion q = lQuatH[i];
                                                q0hsum += q.x;
                                                q1hsum += q.y;
                                                q2hsum += q.z;
                                                q3hsum += q.w;
                                            }

                                            q00H = q0hsum / numval;
                                            q11H = q1hsum / numval;
                                            q22H = q2hsum / numval;
                                            q33H = q3hsum / numval;


                                        }
 
                                        //compute hand roll pitch and yaw
                                        float rollH = -Mathf.Rad2Deg * Mathf.Atan2(2.0f * (q00H * q11H + q22H * q33H), 1.0f - 2.0f * (q11H * q11H + q22H * q22H));
                                        float pitchH = -Mathf.Rad2Deg*Mathf.Asin(2.0f * (q00H * q22H - q33H * q11H));
                                        float yawH = Mathf.Rad2Deg*Mathf.Atan2(2.0f * (q00H * q33H + q11H * q22H), 1.0f - 2.0f * (q22H * q22H + q33H * q33H));

                                        //compute wrist roll pitch and yaw
                                        float rollW = -Mathf.Rad2Deg*Mathf.Atan2(2.0f * (q00W * q11W + q22W * q33W), 1.0f - 2.0f * (q11W * q11W + q22W * q22W));
                                        float pitchW = -Mathf.Rad2Deg*Mathf.Asin(2.0f * (q00W * q22W - q33W * q11W));
                                        float yawW = Mathf.Rad2Deg*Mathf.Atan2(2.0f * (q00W * q33W + q11W * q22W), 1.0f - 2.0f * (q22W * q22W + q33W * q33W));

                                        //bound on yaw (drift problem)
                                        if (yawW >= 180.0f) yawW = -360.0f + yawW;
                                        if (yawW <= -180.0f) yawW = 360.0f + yawW;

                                        if (yawH >= 180.0f) yawH = -360.0f + yawH;
                                        if (yawH <= -180.0f) yawH = 360.0f + yawH;

                                        if (FirstPackage)
                                        {
                                            FirstPackage = false;
                                            q0 = Quaternion.Euler(rollW, pitchW, -yawW);
                                            SetYaw0(yawW);
                                        }

                                        

                                        //update sensor values (protected)
                                        lock(_lock)
                                        {
                                            sensorValues.BatteryStatus = battery;

                                            sensorValues.timestamp = timestamp;

                                           
                                            sensorValues.pitchH = pitchH;
                                            sensorValues.rollH = rollH;
                                            sensorValues.yawH = yawH;// -YAW0;

                                            sensorValues.pitchW = pitchW;
                                            sensorValues.rollW = rollW;
                                            sensorValues.yawW = yawW;// -YAW0;


                                            sensorValues.q0h = q00H;
                                            sensorValues.q1h = q11H;
                                            sensorValues.q2h = q22H;
                                            sensorValues.q3h = q33H;

                                            sensorValues.q0w = q00W;
                                            sensorValues.q1w = q11W;
                                            sensorValues.q2w = q22W;
                                            sensorValues.q3w = q33W;

                                            

                                            for (i = 0; i < Constants.NumSensors; i++)
                                            {
                                                sensorValues.SensorValues[i] = sensors[i];
                                            }

                                            _newPackageAvailable = true;
                                        }
                                    }
                                    NumPkgRecv++;
                                }
                                // Debug.Log("PKGRECV: " + NumPkgRecv + "\n");

                                //shift streaming buffer
                                int finpos = pospackage + pkglen;
                                int bytesrem = NumBytesRecv - finpos - 1;
                                for (i = 0; i < bytesrem; i++)
                                {
                                    RecvBuffer[i] = RecvBuffer[finpos + 1 + i];
                                }
                                NumBytesRecv = bytesrem;


                            }
                        }
                        else
                        {
                            Debug.Log("Header not found\n");
                            NumBytesRecv = 0;
                        }
                    }
                }
            }
            catch
            {
                //serial port generates an exeption, do nothing
            }
        }



        //trhead completed, send end streaming and close the port
        if (vmgcomOk)
        {
            vmgcom.ReadTimeout = 1;
            //send stop streaming
            SendBuffer[0] = (byte)'$';
            SendBuffer[1] = (byte)0x0a;
            SendBuffer[2] = (byte)0x03;
            SendBuffer[3] = (byte)Constants.PKG_NONE;
            SendBuffer[4] = (byte)(SendBuffer[0] + SendBuffer[1] + SendBuffer[2] + SendBuffer[3]);
            SendBuffer[5] = (byte)'#';
            vmgcom.Write(SendBuffer, 0, 6);
        }
        Debug.Log("End\n");
        Thread.Sleep(2000);
        vmgcom.Close();
        Connected = false;
        Reconnect = true;
        //Debug.Log("Thread end\n");
        ThreadStatus = false;
    }
}
