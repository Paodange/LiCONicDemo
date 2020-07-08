using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mgi.Cytomat.LiCONiC
{
    public class LiCONiCCytomat
    {
        private const char RESPONSE_SEPERATE_CHAR = ';';
        private AsynchronousClient client;

        public string IP { get; }
        public ushort Port { get; }
        public string DeviceId { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="deviceId">Identifier of a device.</param>
        public LiCONiCCytomat(string ip, ushort port, string deviceId)
        {
            IP = ip;
            Port = port;
            DeviceId = deviceId;
        }
        public void Open()
        {
            if (client == null)
            {
                client = new AsynchronousClient();
            }
            if (!client.getSocket().Connected)
            {
                client.StartClient(IP, Port);
            }
        }
        /// <summary>
        /// Opens Serial Communication and Initialises the StoreX (opens the PLC connection, initialises the handling, reads StoreX system constants).
        /// </summary>
        public void STX2Activate()
        {
            var result = client.Send($"STX2Activate({DeviceId})\r");
            var array = result.Split(new char[] { RESPONSE_SEPERATE_CHAR }, StringSplitOptions.RemoveEmptyEntries);
            if (!"1".Equals(array[0]))
            {
                throw new Exception(activateReturnCodeDict[array[0]]);
            }
            if (!"1".Equals(array[1]))
            {
                throw new Exception(barcodeReaderReturnCodeDict[array[1]]);
            }
        }
        /// <summary>
        /// Closes serial communication through the active Serial Port. This function also closes Serial communication for Barcode Reader.
        /// </summary>
        public void STX2Deactivate()
        {
            client.Send($"STX2Deactivate({DeviceId})\r");
        }
        /// <summary>
        /// Reset the StoreX after the error. Puts the StoreX in the idle state.The user should call the STX2Reset method after any error of the machine.The user should call STX2Activate again to continue operations, or press manually the "Reset" button of the machine.
        /// </summary>
        public void STX2Reset()
        {
            client.Send($"STX2Reset({DeviceId})\r");
        }
        /// <summary>
        /// Read the actual climate values
        /// </summary>
        /// <returns></returns>
        public Climate STX2ReadActualClimate()
        {
            var resp = client.Send($"STX2ReadActualClimate({DeviceId})\r");
            return Climate.FromResponseString(resp);
        }
        /// <summary>
        /// Read the target of climate values
        /// </summary>
        /// <returns></returns>
        public Climate STX2ReadSetClimate()
        {
            var resp = client.Send($"STX2ReadSetClimate({DeviceId})\r");
            return Climate.FromResponseString(resp);
        }
        /// <summary>
        /// set the climate values
        /// </summary>
        /// <returns></returns>
        public void STX2WriteSetClimate(Climate climate)
        {
            client.Send($"STX2WriteSetClimate({DeviceId},{climate.Temperature},{climate.RelativeHumidity},{climate.CO2Percent},{climate.N2Percent})\r");
        }
        /// <summary>
        /// Writes shaker speed settings value andswitches shaker on.
        /// </summary>
        /// <param name="speed">Shaker speed (range 1...50).</param>
        public void STX2ActivateShaker(int speed)
        {
            if (speed < 1 || speed > 50)
            {
                throw new Exception("Speed is out of range:[1,50]");
            }
            client.Send($"STX2ActivateShaker({DeviceId},{speed})\r");
        }
        /// <summary>
        /// Switches shaker off.
        /// </summary>
        public void STX2DeactivateShaker()
        {
            client.Send($"STX2DeactivateShaker({DeviceId})\r");
        }
        /// <summary>
        /// Returns shaker speed value
        /// </summary>
        /// <returns></returns>
        public int STX2ReadSetShakerSpeed()
        {
            var resp = client.Send($"STX2ReadSetShakerSpeed({DeviceId})\r").TrimEnd();
            if ("-1".Equals(resp))
            {
                throw new Exception("read shaker speed error");
            }
            return resp.ToInt32();
        }
        /// <summary>
        /// Rotates the swap station on 180 degree.
        /// </summary>
        public void STX2SwapIn()
        {
            var resp = client.Send($"STX2SwapIn({DeviceId})\r").Trim();
            if ("1".Equals(resp))
            {
                throw new Exception("STX2SwapIn error");
            }
        }
        /// <summary>
        /// Rotates the swap station back to home position
        /// </summary>
        public void STX2SwapOut()
        {
            var resp = client.Send($"STX2SwapOut({DeviceId})\r").Trim();
            if ("1".Equals(resp))
            {
                throw new Exception("STX2SwapOut error");
            }
        }
        /// <summary>
        /// Locks the door and reads User Door Switch
        /// </summary>
        public UserDoorStatus STX2Lock()
        {
            //"1" - User's door is opened.
            //"0" - User's door is closed.
            //"-1" - Error.
            var resp = client.Send($"STX2Lock({DeviceId})\r").Trim();
            if ("1".Equals(resp))
            {
                return UserDoorStatus.Opened;
            }
            else if ("0".Equals(resp))
            {
                return UserDoorStatus.Closed;
            }
            else
            {
                throw new Exception("STX2Lock error");
            }
        }
        /// <summary>
        ///Unlock the door
        /// </summary>
        public void STX2UnLock()
        {
            var resp = client.Send($"STX2UnLock({DeviceId})\r").Trim();
            if ("1".Equals(resp))
            {
                throw new Exception("STX2UnLock error");
            }
        }
        /// <summary>
        /// Abandons to perform the prior Load Plate Operation
        /// </summary>
        public void STX2AbandonAccess()
        {
            client.Send($"STX2AbandonAccess({DeviceId})\r");
        }
        /// <summary>
        /// Continues to perform the prior Load Plate Operation
        /// </summary>
        public void STX2ContinueAccess()
        {
            client.Send($"STX2ContinueAccess({DeviceId})\r");
        }
        /// <summary>
        ///
        /// </summary>
        public void STX2GetSysStatus()
        {
            // Return value: DM202(Status Register) or "-1" in case of an error and CR + LF.
            //Bit Comment
            //00 System Ready
            //01 Plate Ready
            //02 System Initialized
            //03 XferStn status change
            //04 Gate closed
            //05 User door
            //06 Warning
            //07 Error
            //08
            //09
            //10
            //11
            //12
            //13
            //14
            //15
            var resp = client.Send($"STX2GetSysStatus({DeviceId})\r");
        }
        /// <summary>
        /// Reads the barcode of a plate at specified location.
        /// </summary>
        /// <returns></returns>
        public string STX2ServiceReadBarcode(int slot, int level)
        {
            var resp = client.Send($"STX2ServiceReadBarcode({DeviceId},{slot},{level})\r").TrimEnd();
            if (readBarcodeErrorDict.ContainsKey(resp))
            {
                throw new Exception(readBarcodeErrorDict[resp]);
            }
            return resp;
        }
        /// <summary>
        /// Implements inventory of entire unit,
        /// the result is saved in the file– InvFileName.If the name of the file is not
        /// assigned, it will be generated automatically.The name of the file consists of a
        /// Serial number of device, date (e.g. "3298_A7010101.inv", where last two
        /// digits are number of the file).
        /// </summary>
        /// <param name="fileName">name of file for saving results of inventory</param>
        /// <param name="lppd">sets whether Plate Present Detector will be
        /// used for Inventory.
        /// true - Inventory with Plate Presents Detector.
        /// false - Inventory without Plate Presents Detector.
        /// </param>
        /// <param name="bcr">sets whether Barcode Reader will be used for Inventory.
        /// true - Inventory with Barcode Reader.
        /// false - Inventory without Barcode Reader.
        ///  </param>
        public void STX2Inventory(string fileName, bool lppd, bool bcr)
        {
            // The result of the STX2Inventory is a file which consists of four columns separated by coma.
            // The first column is number of Cassette, the second column is a value of Level,
            // the third column is a value of Plate Present Detector(1 - plate is present; 0 -
            // plate is not present or Plate Present Detector is off), the fourth column is a
            // value of a Barcode(<null> - No Barcode or Barcode Reader is switched off).
            var l = lppd ? 1 : 0;
            var b = bcr ? 1 : 0;
            var resp = client.Send($"STX2Inventory({DeviceId},{fileName},{l},{b})\r").TrimEnd();
            if (!"1".Equals(resp))
            {
                throw new Exception(stx2InventoryErrorCodeDict[resp]);
            }
        }
        /// <summary>
        /// Moves a plate from position SrcPos to position TrgPos. This operation allows
        /// to move the plate within the bound of one device or between the cascader system.
        /// </summary>
        public void STX2ServiceMovePlate(TransferInfo src, TransferInfo target)
        {
            var resp = client.Send("\r");
            if ("1".Equals(resp))
            {
                return;
            }
            var err = resp.Contains(";") ? resp.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)[1] : resp;
            throw new Exception(movePlateErrorCodeDict[err]);
        }
        /// <summary>
        /// Loads plate from Transfer Station to specified target location.
        /// </summary>
        /// <param name="slot">Traget slot position</param>
        /// <param name="level">Target level position</param>
        public void STX2LoadPlate(int slot, int level)
        {
            var resp = client.Send($"STX2LoadPlate({DeviceId},{slot},{level})\r").Trim();
            if ("1".Equals(resp))
            {
                throw new Exception(STX2LoadPlateErrorCodeDict[resp]);
            }
        }
        /// <summary>
        /// Unloads plate from specified source location to Transfer Station.
        /// </summary>
        /// <param name="slot">Source slot position.</param>
        /// <param name="level">Source level position.</param>
        public void STX2UnloadPlate(int slot, int level)
        {
            var resp = client.Send($"STX2UnloadPlate({DeviceId},{slot},{level})\r").Trim();
            if ("1".Equals(resp))
            {
                throw new Exception(STX2UnloadPlateErrorCodeDict[resp]);
            }
        }
        /// <summary>
        /// Checks whether previous long operation is running.The user can check whether long operations like STX2ServiceMovePlate, STX2Inventory are running by means of usage this method.
        /// </summary>
        /// <returns>True if Previous long operation is still running 
        /// False if Previous long operation is completed or cancelled (or Device has the Error Status)</returns>
        public bool STX2IsOperationRunning()
        {
            var resp = client.Send($"STX2IsOperationRunning({DeviceId})\r").Trim();
            return "1".Equals(resp);
        }
        /// <summary>
        /// Interrogates Error Flag and Error code of System.
        /// </summary>
        public void STX2ReadErrorCode()
        {
            //"STX2ReadErrorCode(ID)" - Interrogates Error Flag and Error code of System.
            //Parameter: ID – Identifier of a device.
            //Return values: Result of operation and CR + LF.
            //"0" - No Errors.
            //If returns the System status code then see descriptions of error
            //codes.
            //"-1" – Error.
        }
        /// <summary>
        /// Implements Soft Reset command.
        /// </summary>
        public void STX2SoftReset()
        {
            client.Send($"STX2SoftReset({DeviceId})\r");
        }
        /// <summary>
        /// Reads whether door is opened.
        /// </summary>
        /// <returns></returns>
        public UserDoorStatus STX2ReadUserDoorFlag()
        {
            //"1" - User's door is opened.
            //"0" - User's door is closed.
            //"-1" - Error.
            var resp = client.Send($"STX2ReadUserDoorFlag({DeviceId})\r").Trim();
            if ("1".Equals(resp))
            {
                return UserDoorStatus.Opened;
            }
            else if ("0".Equals(resp))
            {
                return UserDoorStatus.Closed;
            }
            else
            {
                throw new Exception("STX2ReadUserDoorFlag error");
            }
        }
        /// <summary>
        /// Reads whether plate is present on the Shovel. If
        /// Plate Shovel Detector is not assigned in Unit configuration file section Sensor Configuration value 
        /// PlateShovelSensor = 1 this function returns value "0" by default.
        /// </summary>
        /// <returns></returns>
        public DetectorStatus STX2ReadShovelDetector()
        {
            //Return values: Result of operation and CR + LF.
            //"1" - Plate presents on the Shovel.
            //"0" - Plate doesn't present on the Shovel.
            //"-1" – Error.
            var resp = client.Send($"STX2ReadShovelDetector({DeviceId})\r").Trim();
            if ("1".Equals(resp))
            {
                return DetectorStatus.Occupied;
            }
            else if ("0".Equals(resp))
            {
                return DetectorStatus.Empty;
            }
            else
            {
                throw new Exception("STX2ReadUserDoorFlag error");
            }
        }
        /// <summary>
        /// Reads whether plate is present on the Transfer Station.
        /// If Plate Station Detector is not assigned in Unit configuration file section Sensor Configuration value 
        /// PlateXferStSensor1 = 1 this function returns value "0" by default.
        /// </summary>
        /// <returns></returns>
        public DetectorStatus STX2ReadXferStationDetector1()
        {
            //Return values: Result of operation and CR + LF.
            //"1" - Plate presents on the Shovel.
            //"0" - Plate doesn't present on the Shovel.
            //"-1" – Error.
            var resp = client.Send($"STX2ReadXferStationDetector1({DeviceId})\r").Trim();
            if ("1".Equals(resp))
            {
                return DetectorStatus.Occupied;
            }
            else if ("0".Equals(resp))
            {
                return DetectorStatus.Empty;
            }
            else
            {
                throw new Exception("STX2ReadUserDoorFlag error");
            }
        }
        /// <summary>
        /// Reads whether plate is present on the Transfer Station.If Second Plate Station Detector is not assigned in
        /// Unit configuration file section Sensor Configuration value
        /// PlateXferStSensor2 = 1 this function returns value "0" by default.
        /// </summary>
        /// <returns></returns>
        public DetectorStatus STX2ReadXferStationDetector2()
        {
            //Return values: Result of operation and CR + LF.
            //"1" - Plate presents on the Shovel.
            //"0" - Plate doesn't present on the Shovel.
            //"-1" – Error.
            var resp = client.Send($"STX2ReadXferStationDetector2({DeviceId})\r").Trim();
            if ("1".Equals(resp))
            {
                return DetectorStatus.Occupied;
            }
            else if ("0".Equals(resp))
            {
                return DetectorStatus.Empty;
            }
            else
            {
                throw new Exception("STX2ReadUserDoorFlag error");
            }
        }
        /// <summary>
        /// Turns Bepper Alarm on.
        /// </summary>
        public void STX2BeeperOn()
        {
            client.Send($"STX2BeeperOn({DeviceId})\r");
        }
        /// <summary>
        /// Turns Bepper Alarm off.
        /// </summary>
        public void STX2BeeperOff()
        {
            client.Send($"STX2BeeperOff({DeviceId})\r");
        }
        /// <summary>
        /// Reads barcode of the Plate at TransferStation.
        /// </summary>
        /// <returns></returns>
        public string STX2ReadBarcodeAtTransferStation()
        {
            var resp = client.Send($"STX2ReadBarcodeAtTransferStation({DeviceId})\r").TrimEnd();
            if (readBarcodeErrorDict.ContainsKey(resp))
            {
                throw new Exception(readBarcodeAtTransferStationErrorCodeDict[resp]);
            }
            return resp;
        }

        #region Error Code
        private readonly Dictionary<string, string> activateReturnCodeDict = new Dictionary<string, string>()
        {
            { "1" , "Communication is opened and device is initialised." },
            { "-1" , "Error of opening Serial Port."},
            { "-2" , "Error of opening Serial Port (serial Port is already opened)."},
            { "-3" , "No communication."},
            { "-4" , "Communication Error."},
            { "-5" , "System Error (System Error Flag is true)."},
            { "-6" , "User Door is opened (or cannot read User Door status)."},
            { "-7" , "System is in undefined status (Error and Ready Flags are false)"}
        };
        private readonly Dictionary<string, string> barcodeReaderReturnCodeDict = new Dictionary<string, string>()
        {
            {"1" , "BCR Serial Port is successfully opened." },
            {"-1" , "Error of opening BCR port."},
            {"-2" , "Wrong value of BCR port."}
        };
        private readonly Dictionary<string, string> readBarcodeErrorDict = new Dictionary<string, string>()
        {
            { "BCRError" , "Barcode reader is not initialised." },
            { "InitError" , "StoreX is not initialized."},
            { "Device Status Error" , "Device has the Error Status."},
            { "Device not Ready" , "Device not ready."},
            { "Error" , "Cassette or Level value not set or previous long operation is not finished."},
            { "No Plate" ,"There is no Plate at the specified position."},
            { "No Barcode" ,"There is no Barcode on the Plate."}
        };
        private readonly Dictionary<string, string> stx2InventoryErrorCodeDict = new Dictionary<string, string>()
        {
            { "1", "STX2InventoryCassets operation is started."},
            { "-1", "Device is not initialised."},
            { "-2", "Previous long operation is not finished."},
            { "-3", "Device not ready."},
            { "-4", "Device has the Error Status." }
        };
        private readonly Dictionary<string, string> movePlateErrorCodeDict = new Dictionary<string, string>()
        {
           {"-1","Previous long operation is not finished." },
           {"-2","One of input parameters is not a valid integer value." },
           {"-3","A Source or a Target Device is not specified or not initialised." },
           {"-4","One or more of devices is not defined in a system." },
           {"-5","One of transport slot is not specified." },
           {"-6","Wrong value of a target transport slot." },
           {"-7","Wrong value of a source transport slot." },
           {"-8","Wrong value of a source position." },
           {"-9","Wrong value of a target position." },
           {"1","Error during LoadPlate operation." },
           {"2","Error during UnloadPlate operation." },
           {"3","Error during PickPlate operation." },
           {"4","Error during PlacePlate operation." },
           {"5","Error during SetPlate operation." },
           {"6","Error during GetPlate operation." },
           {"7","Device is not ready." },
           {"8","Devece has the Error Status." }
        };
        private readonly Dictionary<string, string> STX2LoadPlateErrorCodeDict = new Dictionary<string, string>()
        {
            {"1", "Plate has been loaded." },
            {"-1", "Previous long operation is not finished." },
            {"-2", "Device is not initialised." },
            {"-3", "Device has the Error Status." },
            {"-4", "Wrong value of a target position." },
            {"-5", "Error of loading a plate." }
        };
        private readonly Dictionary<string, string> STX2UnloadPlateErrorCodeDict = new Dictionary<string, string>()
        {
            {"1", "Plate has been loaded." },
            {"-1", "Previous long operation is not finished." },
            {"-2", "Device is not initialised." },
            {"-3", "Device has the Error Status." },
            {"-4", "Wrong value of a target position." },
            {"-5", "Error of unloading a plate." }
        };
        private readonly Dictionary<string, string> readBarcodeAtTransferStationErrorCodeDict = new Dictionary<string, string>()
        {
            { "BCRError" , "Barcode reader is not initialised." },
            { "InitError" , "StoreX is not initialized."},
            { "Device Status Error" , "Device has the Error Status."},
            { "Device not Ready" , "Device not ready."},
            { "Error" , "Cassette or Level value not set or previous long operation is not finished."},
            //{ "No Plate" ,"There is no Plate at the specified position."},
            { "No Barcode" ,"There is no Barcode on the Plate."}
        };
        #endregion
    }


    public class TransferInfo
    {
        //SrcInstr - Identifier of a source Device.
        //SrcPos - Source position
        //{1-TransferStation, 2-Slot-Level
        //Position,3 – Shovel, 4-Tunnel, 5-Tube Picker
        //}.
        //SrcSlot - plate slot position of source.
        //SrcLevel - plate Level position of source.
        //TransSrcSlot – number of a transport slot of a source device.It is
        //obligatory for moving a plate between devices Base, Extended
        //in Cascader.This Parameter is even for Extended Device and
        //odd for Base Device.
        //In case of Tube Picker this parameter defines a Source Plate
        //Position on a Tube Picker Device { 0,1}
        //0-Target Position.
        //1 - Source Position.
        //SrcPlType - Type of plate of source position {
        //            0 - MTP, 1 - DWP,
        //3 - P28}.
        //TrgInstr - Identifier of a target device.
        //TrgPos – Target position
        //        {1-TransferStation, 2-Slot-Level
        //Positioan, 3 – Shovel, 4-Tunnel, 5-Tube Picker
        //        }.
        //TrgSlot - plate slot position of target.
        //TrgLevel - plate level position of target.
        //TransTrgSlot – number of a transport slot of a Target Device.It is
        //obligatory for moving a plate between devices Base, Extended
        //in Cascader.This Parameter is even for Extended Device and
        //odd for Base Device.
        //In case of Tube Picker this parameter defines a Target Plate
        //Position on a Tube Picker Device { 0,1}
        //0-Target Position.
        //1 - Source Position.
        //TrgPltType - Type of plate of source position { 0 - MTP, 1 - DWP, 3 - P28}.
        public string DeviceId { get; set; }
        public int Position { get; set; }
        public int Slot { get; set; }
        public int Level { get; set; }
        public int TransSlot { get; set; }
        public int PlateType { get; set; }
    }
    public class Climate
    {
        public double Temperature { get; set; }
        public double RelativeHumidity { get; set; }
        public double CO2Percent { get; set; }
        public double N2Percent { get; set; }

        public override string ToString()
        {
            return $"Temperature:{Temperature},RelativeHumidity:{RelativeHumidity},CO2Percent:{CO2Percent},N2Percent:{N2Percent}";
        }

        internal static Climate FromResponseString(string response)
        {
            var array = response.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return new Climate()
            {
                Temperature = array[0].ToDouble(),
                RelativeHumidity = array[1].ToDouble(),
                CO2Percent = array[2].ToDouble(),
                N2Percent = array[3].ToDouble()
            };
        }
    }
    public enum UserDoorStatus
    {
        Closed,
        Opened
    }
    public enum DetectorStatus
    {
        Occupied,
        Empty
    }
    public static class StringExtention
    {
        public static double ToDouble(this string s, double defaultValue = 0d)
        {
            if (double.TryParse(s, out var d))
            {
                return d;
            }
            return defaultValue;
        }
        public static int ToInt32(this string s, int defaultValue = 0)
        {
            if (int.TryParse(s, out var d))
            {
                return d;
            }
            return defaultValue;
        }
    }
}
