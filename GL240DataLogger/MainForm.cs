//using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using GL240Lib;
using SwiftUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;



namespace GL240DataLogger
{
    public partial class MainForm : Form
    {
        private bool SIMULATE_GL240 = false;
        private bool SIMULATE_PLC = false;
        private bool _DEBUG = true;

        //Class instantiate of DevIo.Net (Only once is carried out at the time of an application start.)
        private DevIo m_DevIo = new DevIo();

        stDeviceList TcpParam = new stDeviceList();
        CommandHistory commandHistory { get; set; } = new CommandHistory();

        // Add this field to MainForm class
        private System.Windows.Forms.Timer AcquireDataTimer;

        // Global variable: List of ChannelProperty
        private List<ChannelProperty> channelPropertyList = new List<ChannelProperty>();

        // Measurement DataTable with 12 columns
        private DataTable dtMeasurement = new DataTable();
        private DataTable dtTemp = new DataTable();

        private int metaDataIndex = 0; // to store the MetaDataIndex of the current measurement session

        private string _gL240SettingName = "DefaultSetting";
        private bool _toUseLocalInflux = true;

        // Hard-coded InfluxDB connection details (global)
        private string InfluxUrl = "http://localhost:8086";
        private string InfluxBucket = "TestBucket";
        private string InfluxOrg = "Learners";
        private string InfluxToken = "VosIiWtf9r8_IqD30tu3H7H_5yrtebGXwc1R4Kc72igHBLlqodHnr6EWtsVN0edTrtC0ByhOt9a0sQaJ-UjBpA==";

        private string _localInfluxUrl = "http://localhost:8086";
        private string _localInfluxBucket = "TestBucket";
        private string _localInfluxOrg = "Learners";
        private string _localInfluxToken = "VosIiWtf9r8_IqD30tu3H7H_5yrtebGXwc1R4Kc72igHBLlqodHnr6EWtsVN0edTrtC0ByhOt9a0sQaJ-UjBpA==";

        private string _swiftInfluxUrl = "http://35.208.5.245:8086";
        private string _swiftInfluxBucket = "laminator-datalogger";
        private string _swiftInfluxOrg = "swift";
        private string _swiftInfluxToken = "FQwJbWHHTIwFDOdhG1VeXXySL5youmLQ4TYV40ZbKAp3wkZ6FponZS-xDzwn_jBzjZuhBdCQbZ-2dGmKSdSoMQ==";


        //private string conntionString = "Host=10.10.5.100;_port=5432;Database=DataCollection;Username=postgres;Password=Swift981";
        private string conntionStringLocal = "Host=localhost;Database=DataCollection;Username=postgres;Password=Swift981";
        private string conntionStringServer = "Host=localhost;Database=DataCollection;Username=postgres;Password=Swift981";
        private string conntionString;

        private TwinCAT.Ads.AdsClient adsClient;
        private string _netId = "5.71.90.14.1.1";
        private int _port = 851;

        List<KeyValuePair<int, string>> recipeCommandList = new List<KeyValuePair<int, string>>();

        bool GL240Connected = false;
        bool PLCConnected = false;

        DateTime runTime;

        private List<RecipeStep> _recipeStepList = new List<RecipeStep>();
        private string _recipeName;
        private float _targetTemp;


        private List<Tuple<int, int>> _recipeStepNumAndDurationList = new List<Tuple<int, int>>();

        private float[] _lastMeasurement = new float[10];

        private List<string> symbolPathList = new List<string>
        {
            "HMI.CurrentStepNum",
            "HMI.ActualDuration",
            "AI.rTopDiffPress_PSI",
            "AI.rBotDiffPress_PSI",
            "AI.PressPressure",
            "TCI.rTemp_Avg_degC"
        };

        private List<string> symbolNameList = new List<string>
        {
            "Current Step Num",
            "Actual Duration",
            "Upper Chamber Vacuum",
            "Lower Chamber Vacuum",
            "Press Pressure",
            "Platen Temperature"
        };

        private List<string> symbolTypeList = new List<string>
        {
            "int",
            "int",
            "float",
            "float",
            "float",
            "float"
        };

        private List<string> recipeSymbolPathList = new List<string>
        {
            "RECIPE2.Recipe.RecipeName",
            "RECIPE2.Recipe.TargetTemp"
            // step info added inside MainForm()
        };

        private List<string> recipeSymbolNameList = new List<string>
        {
            "RecipeName",
            "TargetTemp"
            // step info added inside MainForm()
        };


        //>>>> Event processing
        public MainForm()
        {
            InitializeComponent();

            // default to use server connection string
            conntionString = conntionStringServer;

            // Timer initialization and event hookup
            AcquireDataTimer = new System.Windows.Forms.Timer();
            AcquireDataTimer.Interval = 1000; // Set interval as needed (milliseconds)
            AcquireDataTimer.Tick += AcquireDataTimer_Tick;

            // Initialize dtMeasurement columns
            dtMeasurement.Columns.Add("SampleId", typeof(string));
            dtMeasurement.Columns.Add("TimeStamp", typeof(DateTime));
            for (int i = 1; i <= 10; i++)
            {
                dtMeasurement.Columns.Add($"Temp{i}", typeof(float));
            }

            // Add columns based on symbolNameList
            dtMeasurement.Columns.Add("CurrentStepNum", typeof(int));
            dtMeasurement.Columns.Add("ActualDuration", typeof(int));
            dtMeasurement.Columns.Add("UpperChamberVacuum", typeof(float));
            dtMeasurement.Columns.Add("LowerChamberVacuum", typeof(float));
            dtMeasurement.Columns.Add("PressPressure", typeof(float));
            dtMeasurement.Columns.Add("PlatenTemperature", typeof(float));


            //An original value of a connection parameter is set.
            TcpParam.IfType = enIfType.IF_USB; // gyin: we only use USB for now, need to delete code related to TCP/IP
            TcpParam.Tcp_IpAdd = new IpAddr();
            TcpParam.Port = 8023;
            TcpParam.Tcp_IpAdd.ipAddr = TcpParam.Tcp_IpAdd.ConvertInt4ToInt(192, 168, 0, 1);

            // these are saved into table LaminatorRecipeStepName, need to read from table
            recipeCommandList.Add(new KeyValuePair<int, string>(0, "END"));
            recipeCommandList.Add(new KeyValuePair<int, string>(1, "Vac Lower Chamber"));
            recipeCommandList.Add(new KeyValuePair<int, string>(2, "Vent Lower Chamber"));
            recipeCommandList.Add(new KeyValuePair<int, string>(3, "Wait Time (sec)"));
            recipeCommandList.Add(new KeyValuePair<int, string>(4, "Wait Min Temp (degC)"));
            recipeCommandList.Add(new KeyValuePair<int, string>(5, "Raise Pins"));
            recipeCommandList.Add(new KeyValuePair<int, string>(6, "Lower Pins"));
            recipeCommandList.Add(new KeyValuePair<int, string>(7, "Slow Press"));
            recipeCommandList.Add(new KeyValuePair<int, string>(8, "StepNum8"));
            recipeCommandList.Add(new KeyValuePair<int, string>(9, "Press Max"));
            recipeCommandList.Add(new KeyValuePair<int, string>(10, "StepNum10"));
            recipeCommandList.Add(new KeyValuePair<int, string>(11, "Fast Release Press"));
            recipeCommandList.Add(new KeyValuePair<int, string>(12, "Lid Open"));

            // Add Step2 to Step25 entries to recipeSymbolNameList and recipeSymbolPathList
            for (int i = 1; i <= 25; i++)
            {
                recipeSymbolNameList.Add($"Step{i}Command");
                recipeSymbolNameList.Add($"Step{i}Target");
                recipeSymbolNameList.Add($"Step{i}Duration");

                recipeSymbolPathList.Add($"RECIPE2.Recipe.Step[{i}].Command");
                recipeSymbolPathList.Add($"RECIPE2.Recipe.Step[{i}].Target");
                recipeSymbolPathList.Add($"RECIPE2.Recipe.Step[{i}].Duration");
            }

            for (int i = 0; i < _lastMeasurement.Length; i++)
            {
                _lastMeasurement[i] = NOT_VALID_TEMP;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            InitializeDgvChannelProperties();
            InitializeDgvPLC();
            InitializeDgvData();
            InitializeDgvMetadata();
            InitializeDgvRecipe(); // Initialize recipe DataGridView

            // get last raw from meta data table and update channelPropertyList and dgvChannelProperties
            //GetLastRawFromMetaTable();

            GetConfigurationParameters();

            GetGL240Settings(_gL240SettingName);
            SetLocalInfluxDB(_toUseLocalInflux);

            textBoxSetupToLoad.Text = _gL240SettingName;
            checkBoxDBSelection.Checked = _toUseLocalInflux;
            SetLocalInfluxDB(_toUseLocalInflux);

            adsClient = new AdsClient();
        }

        private void GetConfigurationParameters()
        {
            try
            {
                var app = System.Configuration.ConfigurationManager.AppSettings;

                string v;

                v = app["DefaultGL240Settings"];
                if (!string.IsNullOrWhiteSpace(v))
                    _gL240SettingName = v;

                v = app["LocalInfluxURL"];
                if (!string.IsNullOrWhiteSpace(v))
                    _localInfluxUrl = v;

                v = app["LocalInfluxBucket"];
                if (!string.IsNullOrWhiteSpace(v))
                    _localInfluxBucket = v;

                v = app["LocalInfluxOrg"];
                if (!string.IsNullOrWhiteSpace(v))
                    _localInfluxOrg = v;

                v = app["LocalInfluxToken"];
                if (!string.IsNullOrWhiteSpace(v))
                    _localInfluxToken = v;

                v = app["SwiftInfluxURL"];
                if (!string.IsNullOrWhiteSpace(v))
                    _swiftInfluxUrl = v;

                v = app["SwiftInfluxBucket"];
                if (!string.IsNullOrWhiteSpace(v))
                    _swiftInfluxBucket = v;

                v = app["SwiftInfluxOrg"];
                if (!string.IsNullOrWhiteSpace(v))
                    _swiftInfluxOrg = v;

                v = app["SwiftInfluxToken"];
                if (!string.IsNullOrWhiteSpace(v))
                    _swiftInfluxToken = v;

                // Read TwinCAT NetId from configuration if present
                v = app["NetId"];
                if (!string.IsNullOrWhiteSpace(v))
                    _netId = v;

                // Optionally read TwinCAT port (integer)
                v = app["TwinCATPort"];
                if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out int parsedPort))
                    _port = parsedPort;

                // Get flag to use local Influx
                v = app["ToUseLocalInflux"];
                if (!string.IsNullOrWhiteSpace(v))
                {
                    var s = v.Trim();
                    if (s.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                        s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                        s == "1")
                    {
                        _toUseLocalInflux = true;
                    }
                    else if (s.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                             s.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                             s == "0")
                    {
                        _toUseLocalInflux = false;
                    }
                    // otherwise keep existing default
                }

                // Apply selected Influx DB settings
                SetLocalInfluxDB(_toUseLocalInflux);

                SetRichText("Configuration parameters loaded from App.config.");
            }
            catch (Exception ex)
            {
                SetRichText($"Failed to load configuration parameters: {ex.Message}");
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_DevIo.Close();
        }

        private int GetGL240Settings(string settingName)
        {
            try
            {
                var influxDbAccess = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);

                // Call the pivot helper (async) and wait for result synchronously.
                // Assumes QueryPivotedAsync returns a DataTable where each setting field is a column:
                // e.g. CH01Name, CH01Description, CH01Enabled, ... CH10Enabled
                DataTable dt = influxDbAccess.QueryPivotedAsync("LaminatorGL240Settings", "SettingName", settingName).GetAwaiter().GetResult();

                if (dt == null || dt.Rows.Count == 0)
                {
                    SetRichText($"No InfluxDB settings found for '{settingName}'.");
                    return -1;
                }

                // Use the first row as the latest/pivoted setting row
                DataRow row = dt.Rows[0];

                // Clear UI and internal list
                channelPropertyList.Clear();
                dgvChannelProperties.Rows.Clear();

                for (int i = 0; i < 10; i++)
                {
                    int chNum = i + 1;
                    // Add leading zero for channels 1..9 so column names become CH01Name..CH09Name, CH10Name stays CH10Name
                    string chStr = chNum < 10 ? $"0{chNum}" : chNum.ToString();
                    string nameCol = $"CH{chStr}Name";
                    string descCol = $"CH{chStr}Description";
                    string enabledCol = $"CH{chStr}Enabled";

                    string channelName = "";
                    string description = "";
                    bool enabled = true;

                    if (dt.Columns.Contains(nameCol) && row[nameCol] != DBNull.Value)
                        channelName = row[nameCol].ToString();

                    if (dt.Columns.Contains(descCol) && row[descCol] != DBNull.Value)
                        description = row[descCol].ToString();

                    if (dt.Columns.Contains(enabledCol) && row[enabledCol] != DBNull.Value)
                    {
                        var val = row[enabledCol].ToString().Trim();
                        if (!bool.TryParse(val, out enabled))
                        {
                            if (int.TryParse(val, out int ival))
                                enabled = ival != 0;
                            else
                                enabled = !string.IsNullOrEmpty(val) && (val == "1");
                        }
                    }

                    // Ensure defaults if empty
                    if (string.IsNullOrEmpty(channelName))
                        channelName = $"TC{chNum:D2}";

                    dgvChannelProperties.Rows.Add(chNum, channelName, description, enabled);
                    channelPropertyList.Add(new ChannelProperty(chNum, channelName, description, enabled));
                }

                SyncChannelPropertyListWithDGV();

                SetRichText($"Loaded GL240 settings '{settingName}' from InfluxDB.");
                return 0;
            }
            catch (Exception ex)
            {
                SetRichText($"Error querying InfluxDB for settings '{settingName}': {ex.Message}");
                return -1;
            }
        }


        private int GetRecipeComandList()
        {
            try
            {
                var influx = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);

                // Query pivoted points for the measurement. Passing empty tagValue to retrieve all CommandId tag values.
                DataTable dt = influx.QueryPivotedAsync("LaminatorRecipeCommandList", "CommandId", "").GetAwaiter().GetResult();

                recipeCommandList.Clear();

                if (dt == null || dt.Rows.Count == 0)
                {
                    SetRichText("No recipe command entries found in InfluxDB.");
                    return 0;
                }

                foreach (DataRow row in dt.Rows)
                {
                    // Get CommandId (tag) and convert to int
                    string cmdIdRaw = null;
                    if (dt.Columns.Contains("CommandId"))
                        cmdIdRaw = row["CommandId"]?.ToString();
                    else if (dt.Columns.Contains("commandid")) // tolerate lowercase variant
                        cmdIdRaw = row["commandid"]?.ToString();

                    if (string.IsNullOrWhiteSpace(cmdIdRaw))
                        continue;

                    if (!int.TryParse(cmdIdRaw, out int commandId))
                    {
                        // try parsing as double then cast (defensive)
                        if (double.TryParse(cmdIdRaw, out double d))
                            commandId = (int)d;
                        else
                            continue; // skip rows with non-numeric CommandId
                    }

                    // Get CommandName (field)
                    string commandName = "";
                    if (dt.Columns.Contains("CommandName") && row["CommandName"] != DBNull.Value)
                        commandName = row["CommandName"]?.ToString() ?? "";
                    else if (dt.Columns.Contains("Command") && row["Command"] != DBNull.Value)
                        commandName = row["Command"]?.ToString() ?? "";

                    recipeCommandList.Add(new KeyValuePair<int, string>(commandId, commandName));
                }

                SetRichText($"Loaded {recipeCommandList.Count} recipe commands from InfluxDB.");
                return 0;
            }
            catch (Exception ex)
            {
                SetRichText($"Error querying LaminatorRecipeCommandList: {ex.Message}");
                return -1;
            }
        }

        private bool TestPlcConnection()
        {
            try
            {
                // Read the symbol value as float (REAL in TwinCAT/IEC = float in C#)
                float value = (float)adsClient.ReadValue("TCI.rTemp_Avg_degC", typeof(float));
                SetRichText($"Symbol '{textBoxPath.Text}' value: {value}");
                return true;
            }
            catch (Exception ex)
            {
                SetRichText($"Error reading symbol '{textBoxPath.Text}': {ex.Message}");
                return false;
            }

        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (!GL240Connected && !PLCConnected)
            {
                Param param = new Param();
                m_DevIo.Create(enIfType.IF_USB);
                m_DevIo.SetBinaryCrPoint(5);

                param.UsbId.usbId = 0;
                param.ComId.ComParam.portName = "";
                param.PortNumber = (uint)8023;

                bool Flag = m_DevIo.Open(param);
                if (Flag == false)
                {
                    SetRichText("Connect failed.");
                }
                else
                {
                    string s;
                    s = m_DevIo.GetClassName();
                    s = m_DevIo.SendQuery("*IDN?");

                    SetRichText(s);
                    GL240Connected = true;
                }

                // Set up AdsClient
                try
                {
                    adsClient.Connect(_netId, _port); // Connect to the local TwinCAT system
                    bool ok = TestPlcConnection();
                    if (ok)
                    {
                        SetRichText("Connected to TwinCAT ADS server.");
                        PLCConnected = true;
                    }
                    else
                    {
                        // gyin, never get here even there is no hardware connection to PLC
                        SetRichText("Failed to connect to TwinCAT ADS server.");

                        if(GL240Connected)
                        {
                            SetRichText("Disconnecting GL240...");
                            m_DevIo.Close();
                            GL240Connected = false;
                        }

                        return;
                    }
                }
                catch (Exception ex)
                {
                    SetRichText($"Error connecting to TwinCAT ADS server: {ex.Message}");
                }


                buttonConnect.Text = "Disconnect";
                buttonConnect.BackColor = Color.Khaki;
            }
            else // if at lease one device is connected
            {
                m_DevIo.Close();
                SetRichText("Disconnected from GL240.");
                GL240Connected = false;

                if (adsClient != null && adsClient.IsConnected)
                {
                    try
                    {
                        adsClient.Disconnect();
                        SetRichText("Disconnected from PLC.");
                        PLCConnected = false;
                    }
                    catch
                    {
                        SetRichText("Failed to disconnected from PLC.");
                    }


                }


                buttonConnect.Text = "Connect";
                buttonConnect.BackColor = Color.LightGreen;
            }

        }

        private string GetStepNameFromNumber(int stepTypeNumber)
        {
            var pair = recipeCommandList.FirstOrDefault(kv => kv.Key == stepTypeNumber);
            return pair.Equals(default(KeyValuePair<int, string>)) ? "Unknown" : pair.Value;
        }

        private void SyncChannelPropertyListWithDGV()
        {
            channelPropertyList.Clear();
            for (int i = 0; i < dgvChannelProperties.Rows.Count; i++)
            {
                var row = dgvChannelProperties.Rows[i];
                if (row.IsNewRow) continue;

                int channelNumber = i + 1;
                int.TryParse(row.Cells[0].Value?.ToString(), out channelNumber);
                string channelName = row.Cells[1].Value?.ToString() ?? "";
                string description = row.Cells[2].Value?.ToString() ?? "";
                bool enabled = false;
                if (row.Cells[3].Value != null)
                    enabled = Convert.ToBoolean(row.Cells[3].Value);
                channelPropertyList.Add(new ChannelProperty(channelNumber, channelName, description, enabled));
            }
        }


        private async void InsertMetaData(string sampleID, string operatorName)
        {
            // new implementation with influxdb
            var influxDbAccess = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);

            // construct a line protocol string
            string line = "LaminatorMetaData,";
            line += $"SampleId={sampleID} ";
            line += $"Operator=\"{operatorName}\",";
            line += $"GL240Setting=\"{_gL240SettingName}\",";
            line += $"RecipeName=\"{_recipeName}\",";
            line += $"TargetTemp={_targetTemp}";

            string resultMessage;
            try
            {
                // await writer and get returned message
                resultMessage = await influxDbAccess.WriteDataPointAsync(line).ConfigureAwait(false);
                SetRichText(resultMessage);
            }
            catch (Exception ex)
            {
                resultMessage = $"Exception calling WriteDataPointAsync -> {ex.Message}";
                SetRichText(resultMessage);
            }

        }


        private void UpdateMetaData()
        {
            var postgresAccess = new PostgresAccess(conntionString);
            string sql = "UPDATE \"LaminatorMetaData\" SET \"RecipeName\" = @RecipeName, \"TargetTemp\" = @TargetTemp WHERE \"MetaDataIndex\" = @MetaDataIndex";
            var parameters = new Dictionary<string, object>
            {
                { "@RecipeName", _recipeName },
                { "@TargetTemp", _targetTemp },
                { "@MetaDataIndex", metaDataIndex }
            };

            try
            {
                postgresAccess.ExecuteNonQuery(sql, parameters);
                SetRichText($"MetaDataIndex {metaDataIndex} updated with RecipeName and TargetTemp.");
            }
            catch (Exception ex)
            {
                SetRichText($"Failed to update MetaData: {ex.Message}");
            }
        }

        private void GetRecipeInfo()
        {
            if (SIMULATE_PLC)
            {
                _recipeName = "Simulated Recipe";
                _targetTemp = 100.0f;
                _recipeStepList = new List<RecipeStep>();
                for (int i = 1; i <= 1; i++)
                {
                    _recipeStepList.Add(new RecipeStep(1, 1, "step1", 50.0f, 60, 0)); // Simulated values
                }
                return;
            }

            if (adsClient == null || !adsClient.IsConnected)
            {
                SetRichText("ADS client is not connected.");
                _recipeName = "No Recipe";
                _targetTemp = NOT_VALID_TEMP;
                _recipeStepList = new List<RecipeStep>();
                return;
            }

            try
            {
                ISymbolLoader loader = SymbolLoaderFactory.Create(adsClient, SymbolLoaderSettings.Default);
                var allSymbols = loader.Symbols;

                // Read RecipeName and TargetTemp
                SymbolCollection recipeSymbols = new SymbolCollection();
                foreach (var symbolPath in recipeSymbolPathList)
                {
                    recipeSymbols.Add(allSymbols[symbolPath]);
                }
                SumSymbolRead recipeReadCommand = new SumSymbolRead(adsClient, recipeSymbols);
                object[] recipeValues = recipeReadCommand.Read();

                _recipeName = recipeValues[0]?.ToString() ?? "Unknown";
                _targetTemp = recipeValues[1] != null ? Convert.ToSingle(recipeValues[1]) : NOT_VALID_TEMP;

                // Read steps
                _recipeStepList = new List<RecipeStep>();
                for (int i = 1; i <= 25; i++)
                {
                    string commandPath = $"RECIPE2.Recipe.Step[{i}].Command";
                    string targetPath = $"RECIPE2.Recipe.Step[{i}].Target";
                    string durationPath = $"RECIPE2.Recipe.Step[{i}].Duration";
                    string actualDurationPath = $"RECIPE2.Recipe.Step[{i}].ActualDuration";

                    SymbolCollection stepSymbols = new SymbolCollection();
                    stepSymbols.Add(allSymbols[commandPath]);
                    stepSymbols.Add(allSymbols[targetPath]);
                    stepSymbols.Add(allSymbols[durationPath]);
                    //stepSymbols.Add(allSymbols.ContainsKey(actualDurationPath) ? allSymbols[actualDurationPath] : null);

                    SumSymbolRead stepReadCommand = new SumSymbolRead(adsClient, stepSymbols);
                    object[] stepValues = stepReadCommand.Read();

                    int command = stepValues[0] != null ? Convert.ToInt32(stepValues[0]) : -1;
                    float target = stepValues[1] != null ? Convert.ToSingle(stepValues[1]) : 0.0f;
                    int duration = stepValues[2] != null ? Convert.ToInt32(stepValues[2]) : 0;
                    //int actualDuration will be updated somewhere esle, assigned value will be the last HMI ActualDuration of a specifi step;

                    // gyin, assign -1 as actual duration for now, it needs to be calculate later
                    // gyin, 10-10-2025, changed to 0
                    string commandName = GetStepNameFromNumber(command);
                    _recipeStepList.Add(new RecipeStep(i, command, commandName, target, duration, 0));
                    if (command == 0) // "END", last step of the recipe
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                SetRichText($"Error reading recipe info: {ex.Message}");
                _recipeName = "Error";
                _targetTemp = NOT_VALID_TEMP;
                _recipeStepList.Clear();
            }
        }

        private bool SampleIdExist(string sampleId)
        {
            // to use influxdb to check if SampleID exist
            InfluxDbAccess influxDbAccess = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);

            string measurement = "LaminatorMetaData";
            string tagKey = "SampleId";
            string tagValue = sampleId;
            DataTable dt = influxDbAccess.QueryPivotedAsync(measurement, tagKey, tagValue, TimeSpan.FromDays(365)).GetAwaiter().GetResult();

            if (dt != null && dt.Rows.Count > 0)
            {
                return true;
            }

            return false;
        }

        string _sampleId = string.Empty;
        string _operator = string.Empty;
        private void buttonStart_Click(object sender, EventArgs e)
        {
            _totalPLCReadErrors = 0;
            _totalTempMeasurementError = 0;

            _sampleId = textBoxSampleID.Text.Trim();
            if (_sampleId == "")
            {
                SetRichText("Please enter Sample ID before starting data acquisition.");
                return;
            }

            if (SampleIdExist(_sampleId))
            {
                SetRichText("Sample ID exist in database, quit!");
                return;
            }
            
            _operator = textBoxOperatorName.Text.Trim();

            dtMeasurement.Rows.Clear();
            _recipeStepList.Clear();
            _recipeStepNumAndDurationList.Clear();
            _recipeName = "No Recipe Loaded";
            _targetTemp = NOT_VALID_TEMP;

            //gyin: influxdb has no primary/foreign key concept, so we force the SampleID unique at app level


            //SyncChannelPropertyListWithDGV(); // gyin: after separate setup and metadata, how do we handle channel properties?
            // call this function when loading a setup or saving a setup?

            GetRecipeInfo(); // gyin, are we sure that recipe information are updated at this point?
                             // if not, when do we call this? when we hit StepNum = 0 ("END")?

            InsertMetaData(_sampleId, _operator); //gyin: how do we know the insertion is successful?

            runTime = DateTime.Now;
            AcquireDataTimer.Enabled = true;
            SetRichText("Start data acquisition.");
        }

        private void InsertMeasurementData()
        {
            var influxDbAccess = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);

            // Choose source table: prefer dtTemp (used by the timer thread), otherwise use dtMeasurement.
            DataTable source;
            bool clearSourceAfterWrite = false;
            lock (this) // simple synchronization to avoid races with UI thread
            {
                if (dtTemp != null && dtTemp.Rows.Count > 0)
                {
                    source = dtTemp.Copy();
                    clearSourceAfterWrite = true;
                }
                else if (dtMeasurement != null && dtMeasurement.Rows.Count > 0)
                {
                    source = dtMeasurement.Copy();
                    clearSourceAfterWrite = true;
                }
                else
                {
                    // Nothing to write
                    return;
                }
            }

            if (source == null || source.Rows.Count == 0)
                return;

            static string EscapeTag(string v)
            {
                if (v == null) return "";
                return v.Replace("\\", "\\\\").Replace(",", "\\,").Replace(" ", "\\ ").Replace("=", "\\=");
            }

            var lines = new List<string>(source.Rows.Count);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            foreach (DataRow row in source.Rows)
            {
                try
                {
                    // Tag: SampleId (use as tag so queries can filter by sample)
                    string sampleId = row.Table.Columns.Contains("SampleId") && row["SampleId"] != DBNull.Value
                        ? row["SampleId"].ToString()
                        : "";

                    var fields = new List<string>();

                    // temps Temp1..Temp10 (floats)
                    for (int i = 1; i <= 10; i++)
                    {
                        string col = $"Temp{i}";
                        if (row.Table.Columns.Contains(col) && row[col] != DBNull.Value)
                        {
                            // use invariant culture for decimal separator
                            fields.Add($"{col}={Convert.ToSingle(row[col]).ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                        }
                    }

                    // PLC / symbol fields
                    if (row.Table.Columns.Contains("CurrentStepNum") && row["CurrentStepNum"] != DBNull.Value)
                        fields.Add($"CurrentStepNum={(Convert.ToInt32(row["CurrentStepNum"]))}i");
                    if (row.Table.Columns.Contains("ActualDuration") && row["ActualDuration"] != DBNull.Value)
                        fields.Add($"ActualDuration={(Convert.ToInt32(row["ActualDuration"]))}i");
                    if (row.Table.Columns.Contains("UpperChamberVacuum") && row["UpperChamberVacuum"] != DBNull.Value)
                        fields.Add($"UpperChamberVacuum={Convert.ToSingle(row["UpperChamberVacuum"]).ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    if (row.Table.Columns.Contains("LowerChamberVacuum") && row["LowerChamberVacuum"] != DBNull.Value)
                        fields.Add($"LowerChamberVacuum={Convert.ToSingle(row["LowerChamberVacuum"]).ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    if (row.Table.Columns.Contains("PressPressure") && row["PressPressure"] != DBNull.Value)
                        fields.Add($"PressPressure={Convert.ToSingle(row["PressPressure"]).ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    if (row.Table.Columns.Contains("PlatenTemperature") && row["PlatenTemperature"] != DBNull.Value)
                        fields.Add($"PlatenTemperature={Convert.ToSingle(row["PlatenTemperature"]).ToString(System.Globalization.CultureInfo.InvariantCulture)}");

                    if (fields.Count == 0)
                        continue; // nothing to write for this row

                    string fieldSection = string.Join(",", fields);

                    // Timestamp: dtMeasurement.TimeStamp is already UTC.
                    // Handle Kind carefully to avoid accidental double-conversion:
                    // - If Kind == Unspecified, treat it as UTC (per user's note).
                    // - If Kind == Local, convert to UTC.
                    // - If Kind == Utc, use as-is.
                    long timestampNs = -1;
                    if (row.Table.Columns.Contains("TimeStamp") && row["TimeStamp"] != DBNull.Value)
                    {
                        DateTime ts = Convert.ToDateTime(row["TimeStamp"]);
                        DateTime tsUtc;
                        if (ts.Kind == DateTimeKind.Unspecified)
                        {
                            // The app stores UTC values in the DataTable but they may be Unspecified kind.
                            // Treat these values as UTC (do not convert).
                            tsUtc = DateTime.SpecifyKind(ts, DateTimeKind.Utc);
                        }
                        else if (ts.Kind == DateTimeKind.Local)
                        {
                            // Convert local to UTC.
                            tsUtc = ts.ToUniversalTime();
                        }
                        else
                        {
                            tsUtc = ts; // already Utc
                        }

                        // compute nanoseconds since Unix epoch (1 tick = 100 ns)
                        var span = tsUtc - epoch;
                        if (span.TotalMilliseconds >= 0)
                        {
                            timestampNs = checked(span.Ticks * 100L);
                        }
                    }

                    string tagSection = $"SampleId={EscapeTag(sampleId)}";
                    string line = timestampNs >= 0
                        ? $"LaminatorData,{tagSection} {fieldSection} {timestampNs}"
                        : $"LaminatorData,{tagSection} {fieldSection}";

                    lines.Add(line);
                }
                catch (Exception exRow)
                {
                    // Skip row on error but log it
                    SetRichText($"Skipping row while building Influx line: {exRow.Message}");
                }
            }

            if (lines.Count == 0)
                return;

            try
            {
                // Write in batch, block until complete (matches pattern used elsewhere in the project)
                var writeResults = influxDbAccess.WriteDataPointsAsync(lines.ToArray()).GetAwaiter().GetResult();
                if (writeResults != null && writeResults.Length > 0)
                {
                    SetRichText($"Wrote {lines.Count} points to InfluxDB. First result: {writeResults[0]}");
                }
                else
                {
                    SetRichText($"Wrote {lines.Count} points to InfluxDB (no result details).");
                }

                // clear the original source table now that we've written a copy
                if (clearSourceAfterWrite)
                {
                    lock (this)
                    {
                        if (dtTemp != null && dtTemp.Rows.Count > 0)
                            dtTemp.Clear();
                        else if (dtMeasurement != null && dtMeasurement.Rows.Count > 0)
                            dtMeasurement.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                SetRichText($"Error writing measurement points to InfluxDB: {ex.Message}");
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            AcquireDataTimer.Enabled = false;
            SetRichText("Stop data acquisition.");

            // Save dtMeasurement to LaminatorData table
            if (dtMeasurement.Rows.Count > 0)
            {
                // what if BulkInsert just got called in _tick()?
                SetRichText($"Total temperature measurement error: {_totalTempMeasurementError}");

                InsertMeasurementData();
                SetRichText($"Inserted {dtMeasurement.Rows.Count} rows into LaminatorData.");
            }
            else
            {
                SetRichText("No measurement data to insert into LaminatorData.");
            }

            // save recipe info into LaminatorRecipeStep table
            UpdateActualDuration();
            SaveRecipeExecution();
        }



        public string SendQueryDevice(string command, bool enlog)
        {
            var ret = m_DevIo?.SendQuery(command);
            if (enlog == true) commandHistory.PushHistory(command);
            return ret;
        }

        private float NOT_VALID_TEMP = -1000.0f;

        private void GetOneMeasurement()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var row = dtMeasurement.NewRow();
            row["SampleId"] = _sampleId;
            row["TimeStamp"] = DateTime.UtcNow;

            // Prepare all Temp columns as null
            for (int i = 0; i < 10; i++)
                row[$"Temp{i + 1}"] = DBNull.Value;

            float[] tempData = ReadGL240Data();

            // Fill Temp columns with tempData values
            for (int i = 0; i < tempData.Length && i < 10; i++)
            {
                if (tempData[i] != NOT_VALID_TEMP)
                    row[$"Temp{i + 1}"] = tempData[i];
            }

            SymbolData symbolData;
            int ret = ReadPLCSymbolsPerTick(symbolPathList, out symbolData);

            // Add symbolData fields to row
            row["CurrentStepNum"] = symbolData.CurrentStepNum;
            row["ActualDuration"] = symbolData.ActualDuration;
            row["UpperChamberVacuum"] = symbolData.UpperChamberVacuum;
            row["LowerChamberVacuum"] = symbolData.LowerChamberVacuum;
            row["PressPressure"] = symbolData.PressPressure;
            row["PlatenTemperature"] = symbolData.PlatenTemperature;

            UpdateRecipeStepAndDurationList(symbolData.CurrentStepNum, symbolData.ActualDuration);

            dtMeasurement.Rows.Add(row);

            stopwatch.Stop();

            SetRichText($"Measurement time: {stopwatch.ElapsedMilliseconds} ms");
        }

        private void UpdateRecipeStepAndDurationList(int stepNum, int actualDuration)
        {
            if (stepNum == 0) return;

            // Find the index of the tuple with matching stepNum
            int index = _recipeStepNumAndDurationList.FindIndex(t => t.Item1 == stepNum);

            if (index == -1)
            {
                // Not found, add new tuple
                _recipeStepNumAndDurationList.Add(new Tuple<int, int>(stepNum, actualDuration));
            }
            else
            {
                // Found, update the tuple
                _recipeStepNumAndDurationList[index] = new Tuple<int, int>(stepNum, actualDuration);
            }
        }


        // for performance comparison only
        private void ReadPLCSymbolsByMultiCalls()
        {
            if (SIMULATE_PLC)
            {
                for (int i = 0; i < symbolNameList.Count; i++)
                {
                    SetRichText($"Simulation Mode, Channel '{symbolNameList[i]}' value: {i}");
                }

                return;
            }

            if (adsClient == null || !adsClient.IsConnected)
            {
                SetRichText("ADS client is not connected.");
                return;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            foreach (var symbol in symbolPathList)
            {
                try
                {
                    // Read the symbol value as float (REAL in TwinCAT/IEC = float in C#)
                    float value = (float)adsClient.ReadValue(symbol, typeof(float));
                    SetRichText($"Symbol '{symbol}' value: {value}");
                }
                catch (Exception ex)
                {
                    SetRichText($"Error reading symbol '{symbol}': {ex.Message}");
                }
            }


            stopwatch.Stop();
            SetRichText($"ADS read total time: {stopwatch.ElapsedMilliseconds} ms");

        }


        private void button1Measure_Click(object sender, EventArgs e)
        {
            GetOneMeasurement();
        }

        private void AcquireDataTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - runTime;
            labelTimeSpan.Text = $"{elapsed:hh\\:mm\\:ss}";

            GetOneMeasurement();

            // Offload to database if dtMeasurement has >= 60 rows
            if (dtMeasurement.Rows.Count >= 60)
            {
                // Copy dtMeasurement to a new DataTable
                if (dtTemp == null || dtTemp.Columns.Count == 0)
                {
                    dtTemp = dtMeasurement.Clone();
                }
                else
                {
                    dtTemp.Clear();
                }
                foreach (DataRow r in dtMeasurement.Rows)
                {
                    dtTemp.ImportRow(r);
                }
                dtMeasurement.Rows.Clear();

                // Save dtTemp to database in a new thread
                var tempTable = dtTemp.Copy();
                var connStr = conntionString;
                new System.Threading.Thread(() =>
                {
                    InsertMeasurementData();
                })
                { IsBackground = true }.Start();
            }

        }


        private void SetRichText(string Text, bool CrFlag = true)
        {
            // Marshal to UI thread if required
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetRichText(Text, CrFlag)));
                return;
            }

            // Clear if number of lines exceeds 1000
            if (richTextBox.Lines.Length > 1000)
            {
                richTextBox.Clear();
            }

            // Use AppendText to avoid unnecessary reassignments
            richTextBox.AppendText(Text.TrimEnd());
            if (CrFlag == true)
            {
                richTextBox.AppendText(Environment.NewLine);
            }
            richTextBox.SelectionStart = richTextBox.Text.Length;
            richTextBox.ScrollToCaret();
        }


        // Example initialization for a DataGridView named dgvChannelProperties
        private void InitializeDgvChannelProperties()
        {
            dgvChannelProperties.AllowUserToAddRows = false; // Prevent new row at the bottom

            dgvChannelProperties.Columns.Clear();

            dgvChannelProperties.Columns.Add("ChannelNumber", "Chnnel Number");
            dgvChannelProperties.Columns.Add("ChannelName", "Channel Name");
            dgvChannelProperties.Columns.Add("Description", "Description");

            // Add the "Enabled" column as a checkbox column
            var enabledColumn = new DataGridViewCheckBoxColumn();
            enabledColumn.Name = "Enabled";
            enabledColumn.HeaderText = "Enabled";
            enabledColumn.Width = 80;
            enabledColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvChannelProperties.Columns.Add(enabledColumn);

            // Set fixed width for other columns
            dgvChannelProperties.Columns[0].Width = 80;
            dgvChannelProperties.Columns[1].Width = 120;
            dgvChannelProperties.Columns[2].Width = 230;

            dgvChannelProperties.Rows.Clear();
            for (int i = 0; i < 10; i++)
            {
                dgvChannelProperties.Rows.Add((i + 1).ToString(), $"TC{(i + 1):D2}", "", true);
            }
        }

        private void buttonSaveSetup_Click(object sender, EventArgs e)
        {
            // Validate setup name
            string setupName = textBoxSetupToSave.Text.Trim();
            if (string.IsNullOrEmpty(setupName))
            {
                SetRichText("Please enter a setup name before saving.");
                return;
            }

            try
            {
                var influx = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);

                // Query pivoted record(s) for this setting
                DataTable dt = influx.QueryPivotedAsync("LaminatorGL240Settings", "SettingName", setupName).GetAwaiter().GetResult();

                // Helper: escape tag value (measurement/tag must escape comma, space, =, and backslash)
                static string EscapeTagValue(string v)
                {
                    if (v == null) return string.Empty;
                    return v.Replace("\\", "\\\\").Replace(",", "\\,").Replace(" ", "\\ ").Replace("=", "\\=");
                }

                // Helper: escape string field value (inside quotes, escape backslash and quote)
                static string EscapeFieldString(string v)
                {
                    if (v == null) return string.Empty;
                    return v.Replace("\\", "\\\\").Replace("\"", "\\\"");
                }

                // Build a single line-protocol record from dgvChannelProperties
                string BuildLineProtocol(string measurement, string tagKey, string tagValue)
                {
                    var fields = new List<string>();
                    for (int i = 0; i < dgvChannelProperties.Rows.Count && i < 10; i++)
                    {
                        var row = dgvChannelProperties.Rows[i];
                        if (row.IsNewRow) continue;

                        int chNum = i + 1;
                        string chStr = chNum < 10 ? $"0{chNum}" : chNum.ToString(); // zero-pad 1..9 to 01..09

                        var name = row.Cells[1].Value?.ToString() ?? "";
                        var desc = row.Cells[2].Value?.ToString() ?? "";
                        bool enabled = true;
                        if (row.Cells[3].Value != null)
                        {
                            // handle bool or string/number
                            if (row.Cells[3].Value is bool b) enabled = b;
                            else if (!bool.TryParse(row.Cells[3].Value.ToString(), out enabled))
                            {
                                if (int.TryParse(row.Cells[3].Value.ToString(), out int ival)) enabled = ival != 0;
                                else enabled = !string.IsNullOrEmpty(row.Cells[3].Value.ToString()) && row.Cells[3].Value.ToString() != "0";
                            }
                        }

                        // Use CHxxName / CHxxDescription / CHxxEnabled fields to match QueryPivotedAsync expectations
                        fields.Add($"CH{chStr}Name=\"{EscapeFieldString(name)}\"");
                        fields.Add($"CH{chStr}Description=\"{EscapeFieldString(desc)}\"");
                        // boolean literal (true/false)
                        fields.Add($"CH{chStr}Enabled={(enabled ? "true" : "false")}");
                    }

                    var fieldSection = string.Join(",", fields);
                    var tagSection = $"{tagKey}={EscapeTagValue(tagValue)}";
                    // no timestamp -> server assigns time (UpdateInfluxDataRow will append timestamp if needed)
                    return $"{measurement},{tagSection} {fieldSection}";
                }

                const string measurementName = "LaminatorGL240Settings";
                string line = BuildLineProtocol(measurementName, "SettingName", setupName);

                if (dt == null || dt.Rows.Count == 0)
                {
                    // create new point(s)
                    var writeResults = influx.WriteDataPointsAsync(new[] { line }).GetAwaiter().GetResult();
                    if (writeResults != null && writeResults.Length > 0 && writeResults[0].StartsWith("OK:", StringComparison.OrdinalIgnoreCase))
                    {
                        SetRichText($"Created new InfluxDB setting '{setupName}'.");
                        SyncChannelPropertyListWithDGV();
                    }
                    else
                    {
                        SetRichText($"Failed to write new InfluxDB setting '{setupName}': {(writeResults != null && writeResults.Length > 0 ? writeResults[0] : "no response")}");
                    }
                }
                else if (dt.Rows.Count == 1)
                {
                    // Ask user whether to create a new record when dt is null
                    var dlg = MessageBox.Show(
                        $"SettingName='{setupName}' Exists.\nDo you want to update the setting?",
                        "Update existing setting?",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);

                    if (dlg == DialogResult.Yes)
                    {
                        // update existing row (use UpdateInfluxDataRow to locate and replace)
                        int ret = influx.UpdateInfluxPoint(line);
                        if (ret == 0)
                        {
                            SetRichText($"Updated InfluxDB setting '{setupName}'.");
                            SyncChannelPropertyListWithDGV();
                        }
                        else
                        {
                            SetRichText($"Failed to update InfluxDB setting '{setupName}'. UpdateInfluxDataRow returned {ret}.");
                        }
                    }
                    else
                    {
                        SetRichText($"setting '{setupName}'. not updated.");
                    }
                }
                else
                {
                    // ambiguous: multiple existing rows for that SettingName
                    SetRichText($"Multiple InfluxDB rows found for SettingName='{setupName}'. Aborting save. Rows returned: {dt.Rows.Count}");
                }
            }
            catch (Exception ex)
            {
                SetRichText($"Error saving setup to InfluxDB: {ex.Message}");
            }
        }


        private void buttonLoadSetup_Click(object sender, EventArgs e)
        {
            // check if SampleID exist, load setting from the meta data table
            if (textBoxSetupToLoad.Text.Trim() == "")
            {
                SetRichText("Please enter a setup name before loading.");
                return;
            }

            int ret = GetGL240Settings(textBoxSetupToLoad.Text.Trim());
            SetRichText("Return code from GetGL240Settings: " + ret.ToString());

            SyncChannelPropertyListWithDGV();
        }

        private void InitializeDgvData()
        {
            dgvData.AllowUserToAddRows = false;
            dgvData.Columns.Clear();

            // Add columns matching LaminatorData table
            dgvData.Columns.Add("SampleId", "SampleId");
            dgvData.Columns.Add("TimeStamp", "TimeStamp");
            for (int i = 1; i <= 10; i++)
            {
                dgvData.Columns.Add($"Temp{i}", $"Temp{i}");
            }
            dgvData.Columns.Add("CurrentStepNum", "CurrentStepNum");
            dgvData.Columns.Add("ActualDuration", "ActualDuration");
            dgvData.Columns.Add("UpperChamberVacuum", "UpperChamberVacuum");
            dgvData.Columns.Add("LowerChamberVacuum", "LowerChamberVacuum");
            dgvData.Columns.Add("PressPressure", "PressPressure");
            dgvData.Columns.Add("PlatenTemperature", "PlatenTemperature");

            // Set column types and widths
            dgvData.Columns["SampleId"].ValueType = typeof(string);
            dgvData.Columns["TimeStamp"].ValueType = typeof(DateTime);
            for (int i = 1; i <= 10; i++)
            {
                dgvData.Columns[$"Temp{i}"].ValueType = typeof(float);
                dgvData.Columns[$"Temp{i}"].Width = 80;
            }
            dgvData.Columns["CurrentStepNum"].ValueType = typeof(int);
            dgvData.Columns["ActualDuration"].ValueType = typeof(int);
            dgvData.Columns["UpperChamberVacuum"].ValueType = typeof(float);
            dgvData.Columns["LowerChamberVacuum"].ValueType = typeof(float);
            dgvData.Columns["PressPressure"].ValueType = typeof(float);
            dgvData.Columns["PlatenTemperature"].ValueType = typeof(float);

            dgvData.Columns["SampleId"].Width = 120;
            dgvData.Columns["TimeStamp"].Width = 160;
        }

        private void InitializeDgvMetadata()
        {
            dgvMetaData.AllowUserToAddRows = false;
            dgvMetaData.Columns.Clear();

            // Add columns matching LaminatorMetaData table
            dgvMetaData.Columns.Add("SampleId", "SampleId");
            dgvMetaData.Columns.Add("TimeStamp", "TimeStamp");
            dgvMetaData.Columns.Add("Operator", "Operator");
            dgvMetaData.Columns.Add("GL240Setting", "GL240Setting");

            dgvMetaData.Columns.Add("RecipeName", "RecipeName");
            dgvMetaData.Columns.Add("TargetTemp", "TargetTemp");

            // Set column types and widths
            dgvMetaData.Columns["SampleId"].ValueType = typeof(string);
            dgvMetaData.Columns["TimeStamp"].ValueType = typeof(DateTime);
            dgvMetaData.Columns["Operator"].ValueType = typeof(string);
            dgvMetaData.Columns["GL240Setting"].ValueType = typeof(string);

            dgvMetaData.Columns["RecipeName"].ValueType = typeof(string);
            dgvMetaData.Columns["RecipeName"].Width = 200;
            dgvMetaData.Columns["TargetTemp"].ValueType = typeof(float);
            dgvMetaData.Columns["TargetTemp"].Width = 120;

            dgvMetaData.Columns["SampleId"].Width = 100;
            dgvMetaData.Columns["TimeStamp"].Width = 160;
            dgvMetaData.Columns["Operator"].Width = 100;
            dgvMetaData.Columns["GL240Setting"].Width = 120;
        }

        private void InitializeDgvRecipe()
        {
            dgvRecipe.AllowUserToAddRows = false;
            dgvRecipe.Columns.Clear();

            // Add columns matching LaminatorrecipeCommandList table
            dgvRecipe.Columns.Add("SampleId", "SampleId");
            dgvRecipe.Columns.Add("StepNum", "StepNum");
            dgvRecipe.Columns.Add("CommandId", "CommandId");
            dgvRecipe.Columns.Add("CommandName", "CommandName");
            dgvRecipe.Columns.Add("Target", "Target");
            dgvRecipe.Columns.Add("Duration", "Duration");
            dgvRecipe.Columns.Add("ActualDuration", "ActualDuration");

            // Set column types and widths

            dgvRecipe.Columns["SampleId"].ValueType = typeof(int);
            dgvRecipe.Columns["StepNum"].ValueType = typeof(int);
            dgvRecipe.Columns["CommandId"].ValueType = typeof(int);
            dgvRecipe.Columns["CommandName"].ValueType = typeof(string);
            dgvRecipe.Columns["Target"].ValueType = typeof(float);
            dgvRecipe.Columns["Duration"].ValueType = typeof(int);
            dgvRecipe.Columns["ActualDuration"].ValueType = typeof(int);

            dgvRecipe.Columns["SampleId"].Width = 120;
            dgvRecipe.Columns["StepNum"].Width = 80;
            dgvRecipe.Columns["CommandId"].Width = 80;
            dgvRecipe.Columns["CommandName"].Width = 240;
            dgvRecipe.Columns["Target"].Width = 80;
            dgvRecipe.Columns["Duration"].Width = 80;
            dgvRecipe.Columns["ActualDuration"].Width = 120;
        }

        private void buttonQueryDada_Click(object sender, EventArgs e)
        {
            string sampleId = textBoxSampleID4Query.Text.Trim();
            if (string.IsNullOrEmpty(sampleId))
            {
                SetRichText("Please enter Sample ID for query.");
                return;
            }

            try
            {
                var influx = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);

                // Query pivoted LaminatorData points for the SampleId (look back 365 days)
                DataTable dt = influx.QueryPivotedAsync("LaminatorData", "SampleId", sampleId, TimeSpan.FromDays(365)).GetAwaiter().GetResult();

                if (dt == null || dt.Rows.Count == 0)
                {
                    SetRichText($"No LaminatorData found for SampleId: {sampleId}");
                    return;
                }

                // Display result in dgvData
                dgvData.Rows.Clear();
                bool hasTimeColumn = dt.Columns.Contains("_time") || dt.Columns.Contains("TimeStamp") || dt.Columns.Contains("time");
                foreach (DataRow dataRow in dt.Rows)
                {
                    int rowIdx = dgvData.Rows.Add();
                    for (int colIdx = 0; colIdx < dgvData.Columns.Count; colIdx++)
                    {
                        string colName = dgvData.Columns[colIdx].Name;

                        // map Influx _time to the UI column "TimeStamp"
                        if (colName == "TimeStamp")
                        {
                            DateTime? ts = null;
                            if (dt.Columns.Contains("_time") && dataRow["_time"] != DBNull.Value)
                            {
                                ts = Convert.ToDateTime(dataRow["_time"]);
                            }
                            else if (dt.Columns.Contains("time") && dataRow["time"] != DBNull.Value)
                            {
                                ts = Convert.ToDateTime(dataRow["time"]);
                            }
                            else if (dt.Columns.Contains("TimeStamp") && dataRow["TimeStamp"] != DBNull.Value)
                            {
                                ts = Convert.ToDateTime(dataRow["TimeStamp"]);
                            }

                            if (ts.HasValue)
                                dgvData.Rows[rowIdx].Cells[colIdx].Value = ts.Value.ToString("yyyy/MM/dd HH:mm:ss");
                            else
                                dgvData.Rows[rowIdx].Cells[colIdx].Value = null;
                        }
                        else
                        {
                            // If the returned table contains this column, show it; otherwise leave blank.
                            if (dt.Columns.Contains(colName) && dataRow[colName] != DBNull.Value)
                            {
                                dgvData.Rows[rowIdx].Cells[colIdx].Value = dataRow[colName];
                            }
                            else
                            {
                                dgvData.Rows[rowIdx].Cells[colIdx].Value = null;
                            }
                        }
                    }
                }

                // Set visibility
                dgvRecipe.Visible = false;
                dgvData.Visible = true;
                dgvMetaData.Visible = false;

                SetRichText($"Loaded {dgvData.Rows.Count} rows from InfluxDB for SampleId: {sampleId}");
            }
            catch (Exception ex)
            {
                SetRichText($"Error querying LaminatorData for SampleId '{sampleId}': {ex.Message}");
            }
        }

        private void buttonSaveToCSV_Click(object sender, EventArgs e)
        {
            DataGridView activeDgv = null;
            string defaultFileName = "";
            string sampleId = textBoxSampleID4Query.Text.Trim();

            if (dgvData.Visible)
            {
                activeDgv = dgvData;
                defaultFileName = $"LaminatorData_{sampleId}.csv";
            }
            else if (dgvMetaData.Visible)
            {
                activeDgv = dgvMetaData;
                defaultFileName = $"LaminatorMetaData_{sampleId}.csv";
            }
            else if (dgvRecipe.Visible)
            {
                activeDgv = dgvRecipe;
                defaultFileName = $"LaminatoRecipeExecution_{sampleId}.csv";
            }
            else
            {
                SetRichText("No data grid is visible for export.");
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save CSV File";
                saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                saveFileDialog.FileName = defaultFileName;
                saveFileDialog.DefaultExt = "csv";
                saveFileDialog.AddExtension = true;
                saveFileDialog.OverwritePrompt = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;

                    try
                    {
                        using (var writer = new System.IO.StreamWriter(filePath, false, Encoding.UTF8))
                        {
                            // Write header
                            var header = new StringBuilder();
                            for (int i = 0; i < activeDgv.Columns.Count; i++)
                            {
                                header.Append(activeDgv.Columns[i].HeaderText);
                                if (i < activeDgv.Columns.Count - 1)
                                    header.Append(",");
                            }
                            writer.WriteLine(header.ToString());

                            // Write rows
                            foreach (DataGridViewRow row in activeDgv.Rows)
                            {
                                if (row.IsNewRow) continue;
                                var line = new StringBuilder();
                                for (int i = 0; i < activeDgv.Columns.Count; i++)
                                {
                                    var cellValue = row.Cells[i].Value?.ToString() ?? "";
                                    // Escape quotes and commas
                                    cellValue = cellValue.Replace("\"", "\"\"");
                                    if (cellValue.Contains(",") || cellValue.Contains("\""))
                                        cellValue = $"\"{cellValue}\"";
                                    line.Append(cellValue);
                                    if (i < activeDgv.Columns.Count - 1)
                                        line.Append(",");
                                }
                                writer.WriteLine(line.ToString());
                            }
                        }
                        SetRichText($"CSV file saved: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        SetRichText($"Error saving CSV: {ex.Message}");
                    }
                }
                else
                {
                    SetRichText("CSV save cancelled by user.");
                }
            }
        }

        private void buttonReadSymbol_Click(object sender, EventArgs e)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Read the symbol value as float (REAL in TwinCAT/IEC = float in C#)
                float value = (float)adsClient.ReadValue(textBoxPath.Text, typeof(float));
                SetRichText($"Symbol '{textBoxPath.Text}' value: {value}");
            }
            catch (Exception ex)
            {
                SetRichText($"Error reading symbol '{textBoxPath.Text}': {ex.Message}");
            }
            stopwatch.Stop();
            SetRichText($"ADS read time: {stopwatch.ElapsedMilliseconds} ms");
        }

        private SymbolData ReadPLCSymbolsPerTick(List<string> pathList)
        {
            // If simulation mode, return dummy values
            if (SIMULATE_PLC)
            {
                return new SymbolData(
                    1, // CurrentStepNum
                    10, // ActualDuration
                    0.1f, // UpperChamberVacuum
                    0.2f, // LowerChamberVacuum
                    0.3f, // PressPressure
                    21.5f  // PlatenTemperature
                );
            }

            if (adsClient == null || !adsClient.IsConnected)
            {
                SetRichText("ADS client is not connected.");
                return new SymbolData();
            }

            try
            {
                ISymbolLoader loader = SymbolLoaderFactory.Create(adsClient, SymbolLoaderSettings.Default);
                var allSymbols = loader.Symbols;

                SymbolCollection symbolsToRead = new SymbolCollection();
                foreach (var symbolPath in pathList)
                {
                    symbolsToRead.Add(allSymbols[symbolPath]);
                }

                SumSymbolRead readCommand = new SumSymbolRead(adsClient, symbolsToRead);
                object[] values = readCommand.Read();

                // Map values to SymbolData fields based on symbolTypeList
                int currentStepNum = 0;
                int actualDuration = 0;
                float upperChamberVacuum = 0.0f;
                float lowerChamberVacuum = 0.0f;
                float pressPressure = 0.0f;
                float platenTemperature = 0.0f;

                for (int i = 0; i < values.Length && i < symbolTypeList.Count; i++)
                {
                    switch (i)
                    {
                        case 0: // Current Step Num
                            currentStepNum = Convert.ToInt32(values[i]);
                            break;
                        case 1: // Actual Duration
                            actualDuration = Convert.ToInt32(values[i]);
                            break;
                        case 2: // Upper Chamber Vacuum
                            upperChamberVacuum = Convert.ToSingle(values[i]);
                            break;
                        case 3: // Lower Chamber Vacuum
                            lowerChamberVacuum = Convert.ToSingle(values[i]);
                            break;
                        case 4: // Press Pressure
                            pressPressure = Convert.ToSingle(values[i]);
                            break;
                        case 5: // Platen Temperature
                            platenTemperature = Convert.ToSingle(values[i]);
                            break;
                    }
                }

                return new SymbolData(
                    currentStepNum,
                    actualDuration,
                    upperChamberVacuum,
                    lowerChamberVacuum,
                    pressPressure,
                    platenTemperature
                );
            }
            catch (Exception ex)
            {
                SetRichText($"Error reading PLC symbols: {ex.Message}");
                return new SymbolData();
            }
        }

        private int _totalPLCReadErrors = 0;
        private int ReadPLCSymbolsPerTick(List<string> pathList, out SymbolData symbolData)
        {
            // If simulation mode, return dummy values
            if (SIMULATE_PLC)
            {
                symbolData = new SymbolData(
                    1, // CurrentStepNum
                    10, // ActualDuration
                    0.1f, // UpperChamberVacuum
                    0.2f, // LowerChamberVacuum
                    0.3f, // PressPressure
                    21.5f  // PlatenTemperature
                );
                return 0;
            }

            if (adsClient == null || !adsClient.IsConnected)
            {
                SetRichText("ADS client is not connected.");
                symbolData = new SymbolData();
                return -1;
            }

            try
            {
                ISymbolLoader loader = SymbolLoaderFactory.Create(adsClient, SymbolLoaderSettings.Default);
                var allSymbols = loader.Symbols;

                SymbolCollection symbolsToRead = new SymbolCollection();
                foreach (var symbolPath in pathList)
                {
                    symbolsToRead.Add(allSymbols[symbolPath]);
                }

                SumSymbolRead readCommand = new SumSymbolRead(adsClient, symbolsToRead);
                object[] values = readCommand.Read();

                // Map values to SymbolData fields based on symbolTypeList
                int currentStepNum = 0;
                int actualDuration = 0;
                float upperChamberVacuum = 0.0f;
                float lowerChamberVacuum = 0.0f;
                float pressPressure = 0.0f;
                float platenTemperature = 0.0f;

                for (int i = 0; i < values.Length && i < symbolTypeList.Count; i++)
                {
                    switch (i)
                    {
                        case 0: // Current Step Num
                            currentStepNum = Convert.ToInt32(values[i]);
                            break;
                        case 1: // Actual Duration
                            actualDuration = Convert.ToInt32(values[i]);
                            break;
                        case 2: // Upper Chamber Vacuum
                            upperChamberVacuum = Convert.ToSingle(values[i]);
                            break;
                        case 3: // Lower Chamber Vacuum
                            lowerChamberVacuum = Convert.ToSingle(values[i]);
                            break;
                        case 4: // Press Pressure
                            pressPressure = Convert.ToSingle(values[i]);
                            break;
                        case 5: // Platen Temperature
                            platenTemperature = Convert.ToSingle(values[i]);
                            break;
                    }
                }

                symbolData = new SymbolData(
                    currentStepNum,
                    actualDuration,
                    upperChamberVacuum,
                    lowerChamberVacuum,
                    pressPressure,
                    platenTemperature
                );
                return 0;
            }
            catch (Exception ex)
            {
                _totalPLCReadErrors++;
                SetRichText($"Error reading PLC symbols: {ex.Message}");
                symbolData = new SymbolData(-99, -99, -99f, -99f, -99f, -99f);
                return -1;
            }
        }


        private void ReadPLCSymbols(List<string> pathList)
        {
            if (SIMULATE_PLC)
            {
                for (int i = 0; i < pathList.Count; i++)
                {
                    SetRichText($"Simulation Mode, Symbol '{pathList[i]}' value: {i}");
                }

                return;
            }

            // one call to read multiple symbols
            try
            {
                // Create a symbol loader to access the symbolic information from the PLC.
                // gyin, this can be done once after adsClient.Connect() 
                ISymbolLoader loader = SymbolLoaderFactory.Create(adsClient, SymbolLoaderSettings.Default);
                var allSymbols = loader.Symbols;

                //ISymbol s1 = allSymbols["MAIN.Input1"];

                // Define the symbols you want to read.
                // Replace "MAIN.Input1", "MAIN.Output1", and "MAIN.Counter" with your actual symbol names.

                SymbolCollection symbolsToRead = new SymbolCollection();
                foreach (var symbolPath in pathList)
                {
                    symbolsToRead.Add(allSymbols[symbolPath]);
                }

                // Create the Sum Command for reading multiple symbols.
                SumSymbolRead readCommand = new SumSymbolRead(adsClient, symbolsToRead);

                // gyin, above this, done once after adsClient.Connect()

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Execute the read command.
                // The 'values' array will contain the results in the same order as the 'symbolsToRead' collection.
                object[] values = readCommand.Read();

                // Print the results.
                if (values != null && values.Length == symbolsToRead.Count)
                {
                    for (int i = 0; i < symbolsToRead.Count; i++)
                    {
                        if (symbolTypeList[i] == "int")
                        {
                            int intValue = Convert.ToInt32(values[i]);
                            SetRichText($"Symbol '{symbolsToRead[i].InstancePath}' value: {intValue}");
                        }
                        else if (symbolTypeList[i] == "float")
                        {
                            float value = Convert.ToSingle(values[i]);
                            SetRichText($"Symbol '{symbolsToRead[i].InstancePath}' value: {value}");
                        }
                    }
                }
                else
                {
                    SetRichText("Failed to read all symbol values.");
                }

                stopwatch.Stop();
                SetRichText($"ADS read total time: {stopwatch.ElapsedMilliseconds} ms");

            }
            catch (AdsException ex)
            {
                SetRichText($"An ADS error occurred: {ex.Message}");
            }
            catch (Exception ex)
            {
                SetRichText($"An unexpected error occurred: {ex.Message}");
            }
        }

        private void buttonReadList_Click(object sender, EventArgs e)
        {
            ReadPLCSymbolsByMultiCalls();
        }

        private void buttonPLC1Call_Click(object sender, EventArgs e)
        {
            ReadPLCSymbols(symbolPathList);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void InitializeDgvPLC()
        {
            dgvPLC.AllowUserToAddRows = false;
            dgvPLC.Columns.Clear();

            // 1st column: Symbol Number (int)
            var colSymbolNumber = new DataGridViewTextBoxColumn();
            colSymbolNumber.Name = "SymbolNumber";
            colSymbolNumber.HeaderText = "Symbol Number";
            colSymbolNumber.ValueType = typeof(int);
            colSymbolNumber.Width = 100;
            dgvPLC.Columns.Add(colSymbolNumber);

            // 2nd column: Symbol Name (string)
            var colSymbolName = new DataGridViewTextBoxColumn();
            colSymbolName.Name = "SymbolName";
            colSymbolName.HeaderText = "Symbol Name";
            colSymbolName.ValueType = typeof(string);
            colSymbolName.Width = 180;
            dgvPLC.Columns.Add(colSymbolName);

            // 3rd column: Symbol Path (string)
            var colSymbolPath = new DataGridViewTextBoxColumn();
            colSymbolPath.Name = "SymbolPath";
            colSymbolPath.HeaderText = "Symbol Path";
            colSymbolPath.ValueType = typeof(string);
            colSymbolPath.Width = 260;
            dgvPLC.Columns.Add(colSymbolPath);

            // 4th column: Extra (string)
            var colExtra = new DataGridViewTextBoxColumn();
            colExtra.Name = "ExtraColumn";
            colExtra.HeaderText = "Extra Column";
            colExtra.ValueType = typeof(string);
            colExtra.Width = 120;
            dgvPLC.Columns.Add(colExtra);

            // Add 4 rows using symbolNameList for the 2nd column
            dgvPLC.Rows.Clear();
            for (int i = 0; i < symbolNameList.Count && i < symbolPathList.Count; i++)
            {
                dgvPLC.Rows.Add(i + 1, symbolNameList[i], symbolPathList[i], "");
            }
        }

        private void textBoxPath_TextChanged(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private int _totalTempMeasurementError = 0;
        private float[] ReadGL240Data()
        {
            bool[] selectedChannels = channelPropertyList.Select(cp => cp.enabled).ToArray();
            int channelCount = selectedChannels.Length;
            float[] result = new float[channelCount];

            if (SIMULATE_GL240)
            {
                for (int i = 0; i < channelCount; i++)
                {
                    // corrected constant name
                    result[i] = selectedChannels[i] ? i : NOT_VALID_TEMP;
                }
                return result;
            }

            CommandOneData cmd = new CommandOneData();
            cmd.SetChannelSelections(selectedChannels);
            var ret = cmd.Exec(SendQueryDevice);


            // Initialize all as NOT_VALID_TEMP
            for (int i = 0; i < channelCount; i++)
                result[i] = NOT_VALID_TEMP;

            if (ret.StartsWith("error"))
            {
                SetRichText(ret);
                _totalTempMeasurementError++;
                for (int i = 0; i < channelCount; i++)
                {
                    result[i] = _lastMeasurement[i];
                }
                return result;
            }

            if (!string.IsNullOrEmpty(ret))
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(ret, @"CH(\d+):([\d\.]+)\s*degC", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Success)
                    {
                        int chIndex = int.Parse(match.Groups[1].Value); // 1-based index
                        float tempValue;
                        if (float.TryParse(match.Groups[2].Value, out tempValue))
                        {
                            int i = chIndex - 1;
                            if (i >= 0 && i < channelCount && selectedChannels[i])
                            {
                                result[i] = tempValue;
                                _lastMeasurement[i] = tempValue;
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void UpdateActualDuration()
        {
            if (_recipeStepList == null || _recipeStepList.Count == 0 || _recipeStepNumAndDurationList == null || _recipeStepNumAndDurationList.Count == 0)
                return;

            for (int i = 0; i < _recipeStepList.Count; i++)
            {
                for (int j = 0; j < _recipeStepNumAndDurationList.Count; j++)
                {
                    var tuple = _recipeStepNumAndDurationList[j];
                    //gyin: bug here
                    // index i+1 == tuple.Item1
                    //if (_recipeStepList[i].Command == tuple.Item1)
                    if (i == tuple.Item1 - 1) // HMI.CurrentStepNum starts from 1
                    {
                        //_recipeStepList[i].ActualDuration = tuple.Item2;  // gyin: indexer returns a copy of the data, not reference to original
                        // Workaround for CS1612: copy struct, modify, then assign back
                        var step = _recipeStepList[i];
                        step.ActualDuration = tuple.Item2;
                        _recipeStepList[i] = step;
                        break;
                    }
                }
            }
        }

        private void SaveRecipeExecution()
        {
            if (_recipeStepList == null || _recipeStepList.Count == 0)
            {
                SetRichText("No recipe steps to save.");
                return;
            }

            if (string.IsNullOrEmpty(_sampleId))
            {
                SetRichText("SampleId is empty - cannot save recipe execution.");
                return;
            }

            var influxDbAccess = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            static string EscapeTag(string v)
            {
                if (v == null) return "";
                return v.Replace("\\", "\\\\").Replace(",", "\\,").Replace(" ", "\\ ").Replace("=", "\\=");
            }

            static string EscapeStringField(string v)
            {
                if (v == null) return "";
                return v.Replace("\\", "\\\\").Replace("\"", "\\\"");
            }

            foreach (var step in _recipeStepList)
            {
                try
                {
                    var fields = new List<string>
            {
                $"StepNum={step.StepNum}i",
                $"CommandId={step.CommandId}i",
                $"CommandName=\"{EscapeStringField(step.CommandName)}\"",
                $"Target={step.Target.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                $"Duration={step.Duration}i",
                $"ActualDuration={step.ActualDuration}i"
            };

                    string fieldSection = string.Join(",", fields);
                    string tagSection = $"SampleId={EscapeTag(_sampleId)}";

                    // Use current system time (UTC) as timestamp in nanoseconds
                    DateTime nowUtc = DateTime.UtcNow;
                    long timestampNs = checked((long)((nowUtc - epoch).Ticks * 100L)); // 1 tick = 100 ns

                    string line = $"LaminatorRecipeExecution,{tagSection} {fieldSection} {timestampNs}";

                    // write single point and wait 20ms to avoid overwrite
                    var writeResults = influxDbAccess.WriteDataPointsAsync(new[] { line }).GetAwaiter().GetResult();
                    if (writeResults != null && writeResults.Length > 0)
                        SetRichText($"Saved recipe step {step.StepNum}: {writeResults[0]}");
                    else
                        SetRichText($"Saved recipe step {step.StepNum}: no response");

                }
                catch (Exception ex)
                {
                    SetRichText($"Failed to save recipe step {step.StepNum}: {ex.Message}");
                }

                // avoid potential overwrite by spacing inserts
                System.Threading.Thread.Sleep(20);
            }

            SetRichText($"Saved {_recipeStepList.Count} recipe steps to LaminatorRecipeExecution.");
        }

        private void buttonQueryMetaData_Click(object sender, EventArgs e)
        {
            string sampleId = textBoxSampleID4Query.Text.Trim();
            if (string.IsNullOrEmpty(sampleId))
            {
                SetRichText("Please enter Sample ID for query.");
                return;
            }

            try
            {
                var influx = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);

                // Query pivoted LaminatorMetaData points for the SampleId (look back 365 days)
                DataTable dt = influx.QueryPivotedAsync("LaminatorMetaData", "SampleId", sampleId, TimeSpan.FromDays(365)).GetAwaiter().GetResult();

                if (dt == null || dt.Rows.Count == 0)
                {
                    SetRichText($"No LaminatorMetaData found for SampleId: {sampleId}");
                    return;
                }

                // Display result in dgvMetaData
                dgvMetaData.Rows.Clear();
                foreach (DataRow dataRow in dt.Rows)
                {
                    int rowIdx = dgvMetaData.Rows.Add();
                    for (int colIdx = 0; colIdx < dgvMetaData.Columns.Count; colIdx++)
                    {
                        string colName = dgvMetaData.Columns[colIdx].Name;

                        if (colName == "TimeStamp")
                        {
                            DateTime? ts = null;
                            if (dt.Columns.Contains("_time") && dataRow["_time"] != DBNull.Value)
                                ts = Convert.ToDateTime(dataRow["_time"]);
                            else if (dt.Columns.Contains("time") && dataRow["time"] != DBNull.Value)
                                ts = Convert.ToDateTime(dataRow["time"]);
                            else if (dt.Columns.Contains("TimeStamp") && dataRow["TimeStamp"] != DBNull.Value)
                                ts = Convert.ToDateTime(dataRow["TimeStamp"]);

                            dgvMetaData.Rows[rowIdx].Cells[colIdx].Value = ts.HasValue ? ts.Value.ToString("yyyy/MM/dd HH:mm:ss") : null;
                        }
                        else
                        {
                            if (dt.Columns.Contains(colName) && dataRow[colName] != DBNull.Value)
                                dgvMetaData.Rows[rowIdx].Cells[colIdx].Value = dataRow[colName];
                            else
                                dgvMetaData.Rows[rowIdx].Cells[colIdx].Value = null;
                        }
                    }
                }

                // Set visibility
                dgvMetaData.Visible = true;
                dgvData.Visible = false;
                dgvRecipe.Visible = false;

                SetRichText($"Loaded {dgvMetaData.Rows.Count} metadata rows from InfluxDB for SampleId: {sampleId}");
            }
            catch (Exception ex)
            {
                SetRichText($"Error querying LaminatorMetaData for SampleId '{sampleId}': {ex.Message}");
            }
        }

        private void buttonRecipe_Click(object sender, EventArgs e)
        {
            string sampleId = textBoxSampleID4Query.Text.Trim();
            if (string.IsNullOrEmpty(sampleId))
            {
                SetRichText("Please enter Sample ID for query.");
                return;
            }

            try
            {
                var influx = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);

                // Query pivoted LaminatorRecipeExecution points for the SampleId (look back 365 days)
                DataTable dt = influx.QueryPivotedAsync("LaminatorRecipeExecution", "SampleId", sampleId, TimeSpan.FromDays(365)).GetAwaiter().GetResult();

                if (dt == null || dt.Rows.Count == 0)
                {
                    SetRichText($"No LaminatorRecipeExecution found for SampleId: {sampleId}");
                    return;
                }

                // Helper to get column value with case-insensitive fallback (returns null if not present or DBNull)
                object GetField(DataRow row, DataTable table, string colName)
                {
                    if (table.Columns.Contains(colName) && row[colName] != DBNull.Value) return row[colName];
                    // try lowercase/uppercase variants
                    string lower = colName.ToLowerInvariant();
                    foreach (DataColumn c in table.Columns)
                    {
                        if (c.ColumnName.Equals(lower, StringComparison.OrdinalIgnoreCase) ||
                            c.ColumnName.Equals(colName, StringComparison.OrdinalIgnoreCase))
                        {
                            return row[c.ColumnName] != DBNull.Value ? row[c.ColumnName] : null;
                        }
                    }
                    return null;
                }

                // Display result in dgvRecipe
                dgvRecipe.Rows.Clear();
                foreach (DataRow dataRow in dt.Rows)
                {
                    int rowIdx = dgvRecipe.Rows.Add();
                    for (int colIdx = 0; colIdx < dgvRecipe.Columns.Count; colIdx++)
                    {
                        string colName = dgvRecipe.Columns[colIdx].Name;

                        // Map possible Influx time column to nothing for recipe (we don't show time here),
                        // but keep the general mapping for known fields.
                        object val = GetField(dataRow, dt, colName);

                        // Some numeric fields from Influx may come as double; convert to expected types for display
                        if (val != null)
                        {
                            if ((colName == "StepNum" || colName == "CommandId" || colName == "Duration" || colName == "ActualDuration") && val is IConvertible)
                            {
                                try
                                {
                                    dgvRecipe.Rows[rowIdx].Cells[colIdx].Value = Convert.ToInt32(val);
                                }
                                catch
                                {
                                    dgvRecipe.Rows[rowIdx].Cells[colIdx].Value = val.ToString();
                                }
                            }
                            else if ((colName == "Target") && val is IConvertible)
                            {
                                try
                                {
                                    dgvRecipe.Rows[rowIdx].Cells[colIdx].Value = Convert.ToSingle(val);
                                }
                                catch
                                {
                                    dgvRecipe.Rows[rowIdx].Cells[colIdx].Value = val.ToString();
                                }
                            }
                            else
                            {
                                dgvRecipe.Rows[rowIdx].Cells[colIdx].Value = val;
                            }
                        }
                        else
                        {
                            dgvRecipe.Rows[rowIdx].Cells[colIdx].Value = null;
                        }
                    }
                }

                // Set visibility
                dgvRecipe.Visible = true;
                dgvData.Visible = false;
                dgvMetaData.Visible = false;

                SetRichText($"Loaded {dgvRecipe.Rows.Count} recipe execution rows from InfluxDB for SampleId: {sampleId}");
            }
            catch (Exception ex)
            {
                SetRichText($"Error querying LaminatorRecipeExecution for SampleId '{sampleId}': {ex.Message}");
            }
        }

        private void SetLocalInfluxDB(bool useLocal)
        {
            if (useLocal)
            {
                InfluxUrl = _localInfluxUrl;
                InfluxOrg = _localInfluxOrg;
                InfluxBucket = _localInfluxBucket;
                InfluxToken = _localInfluxToken;
            }
            else
            {
                InfluxUrl = _swiftInfluxUrl;
                InfluxOrg = _swiftInfluxOrg;
                InfluxBucket = _swiftInfluxBucket;
                InfluxToken = _swiftInfluxToken;
            }
        }
        private void checkBoxDBSelection_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxDBSelection.Checked)
                SetLocalInfluxDB(true);
            else
                SetLocalInfluxDB(false);
        }

        private void buttonTest_Click(object sender, EventArgs e)
        {
            int ret = GetRecipeComandList();
            SetRichText($"GetRecipeComandList returned {ret} steps.");
        }

        private void buttonUpdateSetup_Click(object sender, EventArgs e)
        {
            // Validate setup name
            string setupName = textBoxSetupToLoad.Text.Trim();
            if (string.IsNullOrEmpty(setupName))
            {
                SetRichText("Please enter a setup name before saving.");
                return;
            }

            try
            {
                var influx = new InfluxDbAccess(InfluxUrl, InfluxToken, InfluxOrg, InfluxBucket);

                // Query pivoted record(s) for this setting
                DataTable dt = influx.QueryPivotedAsync("LaminatorGL240Settings", "SettingName", setupName).GetAwaiter().GetResult();

                // Helper: escape tag value (measurement/tag must escape comma, space, =, and backslash)
                static string EscapeTagValue(string v)
                {
                    if (v == null) return string.Empty;
                    return v.Replace("\\", "\\\\").Replace(",", "\\,").Replace(" ", "\\ ").Replace("=", "\\=");
                }

                // Helper: escape string field value (inside quotes, escape backslash and quote)
                static string EscapeFieldString(string v)
                {
                    if (v == null) return string.Empty;
                    return v.Replace("\\", "\\\\").Replace("\"", "\\\"");
                }

                // Build a single line-protocol record from dgvChannelProperties
                string BuildLineProtocol(string measurement, string tagKey, string tagValue)
                {
                    var fields = new List<string>();
                    for (int i = 0; i < dgvChannelProperties.Rows.Count && i < 10; i++)
                    {
                        var row = dgvChannelProperties.Rows[i];
                        if (row.IsNewRow) continue;

                        int chNum = i + 1;
                        string chStr = chNum < 10 ? $"0{chNum}" : chNum.ToString(); // zero-pad 1..9 to 01..09

                        var name = row.Cells[1].Value?.ToString() ?? "";
                        var desc = row.Cells[2].Value?.ToString() ?? "";
                        bool enabled = true;
                        if (row.Cells[3].Value != null)
                        {
                            // handle bool or string/number
                            if (row.Cells[3].Value is bool b) enabled = b;
                            else if (!bool.TryParse(row.Cells[3].Value.ToString(), out enabled))
                            {
                                if (int.TryParse(row.Cells[3].Value.ToString(), out int ival)) enabled = ival != 0;
                                else enabled = !string.IsNullOrEmpty(row.Cells[3].Value.ToString()) && row.Cells[3].Value.ToString() != "0";
                            }
                        }

                        // Use CHxxName / CHxxDescription / CHxxEnabled fields to match QueryPivotedAsync expectations
                        fields.Add($"CH{chStr}Name=\"{EscapeFieldString(name)}\"");
                        fields.Add($"CH{chStr}Description=\"{EscapeFieldString(desc)}\"");
                        // boolean literal (true/false)
                        fields.Add($"CH{chStr}Enabled={(enabled ? "true" : "false")}");
                    }

                    var fieldSection = string.Join(",", fields);
                    var tagSection = $"{tagKey}={EscapeTagValue(tagValue)}";
                    // no timestamp -> server assigns time (UpdateInfluxDataRow will append timestamp if needed)
                    return $"{measurement},{tagSection} {fieldSection}";
                }

                const string measurementName = "LaminatorGL240Settings";
                string line = BuildLineProtocol(measurementName, "SettingName", setupName);

                if (dt == null || dt.Rows.Count == 0)
                {
                    // Ask user whether to create a new record when dt is null
                    var dlg = MessageBox.Show(
                        $"No existing InfluxDB setting found for SettingName='{setupName}'.\nDo you want to create a new setting?",
                        "Create new setting?",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);

                    if (dlg != DialogResult.Yes)
                    {
                        SetRichText($"Create new InfluxDB setting '{setupName}' cancelled by user.");
                        return;
                    }

                    // create new point(s)
                    var writeResults = influx.WriteDataPointsAsync(new[] { line }).GetAwaiter().GetResult();
                    if (writeResults != null && writeResults.Length > 0 && writeResults[0].StartsWith("OK:", StringComparison.OrdinalIgnoreCase))
                    {
                        SetRichText($"Created new InfluxDB setting '{setupName}'.");
                        SyncChannelPropertyListWithDGV();
                    }
                    else
                    {
                        SetRichText($"Failed to write new InfluxDB setting '{setupName}': {(writeResults != null && writeResults.Length > 0 ? writeResults[0] : "no response")}");
                    }
                }
                else if (dt.Rows.Count == 1)
                {
                    // update existing row (use UpdateInfluxDataRow to locate and replace)
                    int ret = influx.UpdateInfluxPoint(line);
                    if (ret == 0)
                    {
                        SetRichText($"Updated InfluxDB setting '{setupName}'.");
                        SyncChannelPropertyListWithDGV();
                    }
                    else
                    {
                        SetRichText($"Failed to update InfluxDB setting '{setupName}'. UpdateInfluxDataRow returned {ret}.");
                    }
                }
                else
                {
                    // ambiguous: multiple existing rows for that SettingName
                    SetRichText($"Multiple InfluxDB rows found for SettingName='{setupName}'. Aborting save. Rows returned: {dt.Rows.Count}");
                }
            }
            catch (Exception ex)
            {
                SetRichText($"Error saving setup to InfluxDB: {ex.Message}");
            }
        }
    }

    public struct ChannelProperty
    {
        public int channelNumber;
        public string channelName;
        public string description;
        public bool enabled;

        public ChannelProperty(int channelNumber, string channelName, string description, bool enabled)
        {
            this.channelNumber = channelNumber;
            this.channelName = channelName;
            this.description = description;
            this.enabled = enabled;
        }
    }

    public struct RecipeStep
    {
        public int StepNum;
        public int CommandId;
        public string CommandName;
        public float Target;
        public int Duration;
        public int ActualDuration;

        public RecipeStep(int stepNum, int commandId, string commandName, float target, int duration, int actualDuration)
        {
            StepNum = stepNum;
            CommandId = commandId;
            CommandName = commandName;
            Target = target;
            Duration = duration;
            ActualDuration = actualDuration;
        }
    }

    public struct SymbolData
    {
        public int CurrentStepNum;
        public int ActualDuration;
        public float UpperChamberVacuum;
        public float LowerChamberVacuum;
        public float PressPressure;
        public float PlatenTemperature;

        public SymbolData(
            int currentStepNum,
            int actualDuration,
            float upperChamberVacuum,
            float lowerChamberVacuum,
            float pressPressure,
            float platenTemperature)
        {
            CurrentStepNum = currentStepNum;
            ActualDuration = actualDuration;
            UpperChamberVacuum = upperChamberVacuum;
            LowerChamberVacuum = lowerChamberVacuum;
            PressPressure = pressPressure;
            PlatenTemperature = platenTemperature;
        }
    }


}

