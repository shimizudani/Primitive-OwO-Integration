using OWOGame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


public class OwoPrimitiveSensations : MonoBehaviour
{
    public class BaseAttribute : PropertyAttribute
    {
        public float min;
        public float max;

        public BaseAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
    public class MillisecondsMappingAttribute : BaseAttribute
    {
        public MillisecondsMappingAttribute(float min = 0f, float max = 2f) : base(min, max) { }
    }
    public class FrequencyAttribute : BaseAttribute
    {
        public FrequencyAttribute(float min = 1, float max = 100) : base(min, max) { }
    }
    public class IntensityPercentageAttribute : BaseAttribute
    {
        public IntensityPercentageAttribute(float min = 1, float max = 100) : base(min, max) { }
    }
    public abstract class BaseDrawer : PropertyDrawer
    {
        protected abstract string FormatDisplayValue(SerializedProperty property);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            BaseAttribute attr = (BaseAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);

            Rect labelPosition = new Rect(position.x, position.y, position.width * 0.4f, position.height);
            Rect sliderPosition = new Rect(position.x + position.width * 0.4f + 5, position.y, position.width * 0.45f - 5, position.height);

            EditorGUI.LabelField(labelPosition, label);

            EditorGUI.BeginChangeCheck();
            if (property.propertyType == SerializedPropertyType.Float)
            {
                float newValue = GUI.HorizontalSlider(sliderPosition, property.floatValue, attr.min, attr.max);
                if (newValue != property.floatValue)
                {
                    property.floatValue = Mathf.Round(newValue * 10f) / 10f;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                int newValue = Mathf.RoundToInt(GUI.HorizontalSlider(sliderPosition, property.intValue, attr.min, attr.max));
                if (newValue != property.intValue)
                {
                    property.intValue = newValue;
                }
            }

            string displayValue = FormatDisplayValue(property);
            Rect displayValuePosition = new Rect(sliderPosition.xMax + 5, position.y, position.width * 0.15f, position.height);
            EditorGUI.LabelField(displayValuePosition, displayValue);

            EditorGUI.EndProperty();
        }
    }
    [CustomPropertyDrawer(typeof(MillisecondsMappingAttribute))]
    public class MillisecondsMappingDrawer : BaseDrawer
    {
        protected override string FormatDisplayValue(SerializedProperty property)
        {
            return (property.floatValue * 1000).ToString("0.##") + "ms";
        }
    }
    [CustomPropertyDrawer(typeof(FrequencyAttribute))]
    public class FrequencyDrawer : BaseDrawer
    {
        protected override string FormatDisplayValue(SerializedProperty property)
        {
            return property.intValue + " Hz";
        }
    }
    [CustomPropertyDrawer(typeof(IntensityPercentageAttribute))]
    public class IntensityPercentageDrawer : BaseDrawer
    {
        protected override string FormatDisplayValue(SerializedProperty property)
        {
            return property.intValue + "%";
        }
    }

    [System.Serializable]
    public class HapticData
    {
        public String sensationName;
        public bool allMuscles;
        public bool backMuscles;
        public bool frontMuscles;
        public int frequency;
        public float duration;
        public int intensityPercentage;
        public float rampUpInMills;
        public float rampDownInMills;
        public float exitDelay;
        public MuscleData pectoral_R;
        public MuscleData pectoral_L;
        public MuscleData abdominal_R;
        public MuscleData abdominal_L;
        public MuscleData arm_R;
        public MuscleData arm_L;
        public MuscleData dorsal_R;
        public MuscleData dorsal_L;
        public MuscleData lumbar_R;
        public MuscleData lumbar_L;
    }
    [System.Serializable]
    public struct MuscleData
    {
        public bool isUsed;
        public bool useOveride;
        public int intensityOveride;
    }
    [System.Serializable]
    public class AppendedMicroSensations
    {
        public string data;
    }

    private MicroSensation startup;
    private MicroSensation sensationEvent;
    [Header("MicroSensation File Names to Append ")]
    [SerializeField]
    private List<string> fileNames;
    [Header("Appended File Name To Save or Load")]
    [SerializeField]
    private string appendFileName;

    [Header("File Settings")]
    [SerializeField, Tooltip("Filename to Save or load MicroSensation.")]
    private string sensationName = "DefaultMicroSensation";

    [Header("MicroSensation Settings")]
    [SerializeField, Tooltip("Frequency for the MicroSensation."), Frequency(1, 100)]
    private int frequency = 100;
    [SerializeField, Tooltip("Duration of the MicroSensation.")]
    private float duration = 1.0f;
    [SerializeField, Tooltip("Intensity percentage for the MicroSensation."), IntensityPercentage(1, 100)]
    private int intensityPercentage = 25;
    [SerializeField, Tooltip("Set individual Muscle Intensitys.")]
    private bool useMuscleIntOveride;
    [MillisecondsMapping(0, 2)]
    [SerializeField, Tooltip("Ramp up time in milliseconds.")]
    private float rampUpInMills = 0f;
    [MillisecondsMapping(0, 2)]
    [SerializeField, Tooltip("Ramp down time in milliseconds.")]
    private float rampDownInMills = 0f;
    [SerializeField, Tooltip("Exit delay for the MicroSensation.")]
    private float exitDelay = 0f;

    [Header("Muscle Groups")]
    [SerializeField, Tooltip("If Used All Other Muscle Settings are Ignored")]
    private bool allMuscles = true;
    [SerializeField, Tooltip("If Used Settings of Muscles On the Back are Ignored")]
    private bool backMuscles;
    [SerializeField, Tooltip("If Used Settings of Muscle On the Front Ignored")]
    private bool frontMuscles;

    [Header("Front Muscles")]
    public MuscleData pectoral_R;
    public MuscleData pectoral_L;
    public MuscleData abdominal_R;
    public MuscleData abdominal_L;
    public MuscleData arm_R;
    public MuscleData arm_L;

    [Header("Back Muscles")]
    public MuscleData dorsal_R;
    public MuscleData dorsal_L;
    public MuscleData lumbar_R;
    public MuscleData lumbar_L;

    private bool prevAllMuscles = false;
    private bool prevBackMuscles = false;
    private bool prevFrontMuscles = false;

    [HideInInspector, Tooltip("List of available MicroSensations.")]
    public string[] availableFiles;
    [HideInInspector, Tooltip("Currently selected MicroSensation.")]
    public int selectedFileIndex = 0;

    private void Start()
    {
        // "Sartup Sensation" I added For Debugging
        startup = SensationsFactory.Create(100, 1, 25, 1, 1, 0);
        RefreshAvailableFiles();
    }

    private void Update()
    {
        // Handle allMuscles, backMuscles, frontMuscles flags first
        if (allMuscles && !prevAllMuscles)
        {
            ClearIndividualMuscles();
            ClearGroupMusclesExcept("allMuscles");
        }
        else if (backMuscles && !prevBackMuscles)
        {
            ClearIndividualMuscles();
            ClearGroupMusclesExcept("backMuscles");
        }
        else if (frontMuscles && !prevFrontMuscles)
        {
            ClearIndividualMuscles();
            ClearGroupMusclesExcept("frontMuscles");
        }
        // If any specific muscle is active, clear all main muscle flags
        if (AnySpecificMuscleActive())
        {
            allMuscles = false;
            backMuscles = false;
            frontMuscles = false;
        }

        prevAllMuscles = allMuscles;
        prevBackMuscles = backMuscles;
        prevFrontMuscles = frontMuscles;
    }

    private bool AnySpecificMuscleActive()
    {
        return pectoral_R.isUsed || pectoral_L.isUsed || abdominal_R.isUsed || abdominal_L.isUsed ||
               arm_R.isUsed || arm_L.isUsed || dorsal_R.isUsed || dorsal_L.isUsed || lumbar_R.isUsed || lumbar_L.isUsed;
    }
    private bool IsAnyBoolTrue(HapticData data)
    {
        return data.allMuscles || data.backMuscles || data.frontMuscles || data.pectoral_R.isUsed || data.pectoral_L.isUsed ||
               data.abdominal_R.isUsed || data.abdominal_L.isUsed || data.arm_R.isUsed || data.arm_L.isUsed || data.dorsal_R.isUsed ||
               data.dorsal_L.isUsed || data.lumbar_R.isUsed || data.lumbar_L.isUsed;
    }
    private void ClearGroupMusclesExcept(string except)
    {
        if (except != "allMuscles") allMuscles = false;
        if (except != "backMuscles") backMuscles = false;
        if (except != "frontMuscles") frontMuscles = false;
    }
    private void ClearIndividualMuscles()
    {
        pectoral_R.isUsed = false;
        pectoral_L.isUsed = false;
        abdominal_R.isUsed = false;
        abdominal_L.isUsed = false;
        arm_R.isUsed = false;
        arm_L.isUsed = false;
        dorsal_R.isUsed = false;
        dorsal_L.isUsed = false;
        lumbar_R.isUsed = false;
        lumbar_L.isUsed = false;
    }
    public void InitializeSuitButtonPressed()
    {
        InitializeOWO();
    }
    public void SendHapticButtonPressed()
    {
        SendHapticEventBasedOnMuscles();
    }
    public void StopHapticButtonPressed()
    {
        StopHapticEventBasedOnMuscles();
    }
    public void DisconnectVestButtonPressed()
    {
        OWO.Disconnect();
        Debug.Log("Vest Disconnected");
    }
    private Muscle[] GetSelectedMuscles()
    {
        var muscles = new List<Muscle>();

        if (allMuscles)
        {
            muscles.AddRange(Muscle.All);
        }
        else
        {
            if (frontMuscles)
            {
                muscles.AddRange(Muscle.Front);
            }
            else
            {
                AddMuscleWithOverride(muscles, pectoral_R, Muscle.Pectoral_R);
                AddMuscleWithOverride(muscles, pectoral_L, Muscle.Pectoral_L);
                AddMuscleWithOverride(muscles, abdominal_R, Muscle.Abdominal_R);
                AddMuscleWithOverride(muscles, abdominal_L, Muscle.Abdominal_L);
                AddMuscleWithOverride(muscles, arm_R, Muscle.Arm_R);
                AddMuscleWithOverride(muscles, arm_L, Muscle.Arm_L);
            }
            if (backMuscles)
            {
                muscles.AddRange(Muscle.Back);
            }
            else
            {
                AddMuscleWithOverride(muscles, dorsal_R, Muscle.Dorsal_R);
                AddMuscleWithOverride(muscles, dorsal_L, Muscle.Dorsal_L);
                AddMuscleWithOverride(muscles, lumbar_R, Muscle.Lumbar_R);
                AddMuscleWithOverride(muscles, lumbar_L, Muscle.Lumbar_L);
            }
        }
        return muscles.ToArray();
    }

    private void AddMuscleWithOverride(List<Muscle> muscles, MuscleData muscleData, Muscle muscleType)
    {
        if (muscleData.isUsed)
        {
            if (muscleData.useOveride)
            {
                muscles.Add(muscleType.WithIntensity(muscleData.intensityOveride));
            }
            else
            {
                muscles.Add(muscleType);
            }
        }
    }

    public async void InitializeOWO()
    {
        Debug.Log("Initializing suit");

        var connectTask = OWO.AutoConnect();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

        var completedTask = await Task.WhenAny(connectTask, timeoutTask);

        if (completedTask == connectTask && OWO.ConnectionState == ConnectionState.Connected)
        {
            Debug.Log("OWO suit connected.");
            StartCoroutine(StartupPulse());
        }
        else
        {
            Debug.LogError("Failed to connect to OWO suit within 10 seconds.");
        }
    }

    private IEnumerator StartupPulse()
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log("Startup Pulse");
        float elapsedTime = 0f;
        const float timeInterval = 1f;

        while (elapsedTime < 3f)
        {
            OWO.Send(startup.WithMuscles(Muscle.All));
            yield return new WaitForSeconds(timeInterval);
            elapsedTime += timeInterval;
        }
    }

    private void SendHapticEventBasedOnMuscles()
    {
        sensationEvent = SensationsFactory.Create(frequency, duration, intensityPercentage, rampUpInMills, rampDownInMills, exitDelay);
        OWO.Send(sensationEvent.WithMuscles(GetSelectedMuscles().ToArray()));
    }

    private void StopHapticEventBasedOnMuscles()
    {
        OWO.Stop();
    }

    public void RefreshAvailableFiles()
    {
        string directoryPath = "Assets/MicroSensation Events";
        if (Directory.Exists(directoryPath))
        {
            availableFiles = Directory.GetFiles(directoryPath, "*.json").Select(Path.GetFileNameWithoutExtension).ToArray();
        }
        else
        {
            availableFiles = new string[0];
        }
    }

    public void SaveHapticEvent()
    {
        SaveHapticEventToFile(sensationName);
    }

    public void LoadHapticEvent()
    {
        LoadHapticEventFromFile();
    }

    public void SaveHapticEventToFile(string fileName)
    {
        HapticData data = new()
        {
            sensationName = this.sensationName,
            allMuscles = this.allMuscles,
            backMuscles = this.backMuscles,
            frontMuscles = this.frontMuscles,
            frequency = this.frequency,
            duration = this.duration,
            intensityPercentage = this.intensityPercentage,
            rampUpInMills = this.rampUpInMills,
            rampDownInMills = this.rampDownInMills,
            exitDelay = this.exitDelay,
            pectoral_R = this.pectoral_R,
            pectoral_L = this.pectoral_L,
            abdominal_R = this.abdominal_R,
            abdominal_L = this.abdominal_L,
            arm_R = this.arm_R,
            arm_L = this.arm_L,
            dorsal_R = this.dorsal_R,
            dorsal_L = this.dorsal_L,
            lumbar_R = this.lumbar_R,
            lumbar_L = this.lumbar_L
        };
        // Check if at least one of the bool values is true
        bool isAnyBoolTrue = IsAnyBoolTrue(data);
        // If none of the booleans are true, set allMuscles to true
        if (!isAnyBoolTrue)
        {
            data.allMuscles = true;
        }
        string directoryPath = "Assets/MicroSensation Events";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        // using .json extension for saving
        string path = Path.Combine(directoryPath, fileName + ".json");
        string jsonData = JsonUtility.ToJson(data, true); // true to make it nicely formatted
        File.WriteAllText(path, jsonData);
        AssetDatabase.Refresh();
        RefreshAvailableFiles();
        Debug.Log("File Saved");
    }

    private void LoadHapticEventFromFile()
    {
        string path = Path.Combine("Assets/MicroSensation Events", availableFiles[selectedFileIndex] + ".json");
        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            HapticData data = JsonUtility.FromJson<HapticData>(jsonData);
            sensationName = data.sensationName;
            // Set the Sensation fields
            frequency = data.frequency;
            duration = data.duration;
            intensityPercentage = data.intensityPercentage;
            rampUpInMills = data.rampUpInMills;
            rampDownInMills = data.rampDownInMills;
            exitDelay = data.exitDelay;
            // Set the muscle bool fields
            allMuscles = data.allMuscles;
            backMuscles = data.backMuscles;
            frontMuscles = data.frontMuscles;
            //set the muscleData fields
            pectoral_R = data.pectoral_R;
            pectoral_L = data.pectoral_L;
            abdominal_R = data.abdominal_R;
            abdominal_L = data.abdominal_L;
            arm_R = data.arm_R;
            arm_L = data.arm_L;
            dorsal_R = data.dorsal_R;
            dorsal_L = data.dorsal_L;
            lumbar_R = data.lumbar_R;
            lumbar_L = data.lumbar_L;
            // If none of the Muscles are true, set allMuscles to true
            bool isAnyBoolTrue = IsAnyBoolTrue(data);
            if (!isAnyBoolTrue) allMuscles = true;
        }
        else
        {
            Debug.LogError("Haptic event file not found: " + path);
        }
    }
    private (MicroSensation, Muscle[]) LoadHapticEventFromFileForUse(string sensationName)
    {
        string path = Path.Combine("Assets/MicroSensation Events", sensationName + ".json");
        if (!File.Exists(path)) return (null, null);

        string jsonData = File.ReadAllText(path);
        HapticData data = JsonUtility.FromJson<HapticData>(jsonData);

        MicroSensation loadedHapticEvent = SensationsFactory.Create(
            data.frequency,
            data.duration,
            data.intensityPercentage,
            data.rampUpInMills,
            data.rampDownInMills,
            data.exitDelay
        );

        Muscle[] selectedMuscles = GetSelectedMusclesFromData(data);

        return (loadedHapticEvent, selectedMuscles);
    }
    private Muscle[] GetSelectedMusclesFromData(HapticData data)
    {
        List<Muscle> selectedMuscles = new();
        if (data.allMuscles)
        {
            selectedMuscles.AddRange(Muscle.All);
        }
        else
        {
            if (data.frontMuscles)
            {
                selectedMuscles.AddRange(Muscle.Front);
            }
            else
            {
                AddMuscleWithOverride(selectedMuscles, data.pectoral_R, Muscle.Pectoral_R);
                AddMuscleWithOverride(selectedMuscles, data.pectoral_L, Muscle.Pectoral_L);
                AddMuscleWithOverride(selectedMuscles, data.abdominal_R, Muscle.Abdominal_R);
                AddMuscleWithOverride(selectedMuscles, data.abdominal_L, Muscle.Abdominal_L);
                AddMuscleWithOverride(selectedMuscles, data.arm_R, Muscle.Arm_R);
                AddMuscleWithOverride(selectedMuscles, data.arm_L, Muscle.Arm_L);
            }
            if (data.backMuscles)
            {
                selectedMuscles.AddRange(Muscle.Back);
            }
            else
            {
                AddMuscleWithOverride(selectedMuscles, data.dorsal_R, Muscle.Dorsal_R);
                AddMuscleWithOverride(selectedMuscles, data.dorsal_L, Muscle.Dorsal_L);
                AddMuscleWithOverride(selectedMuscles, data.lumbar_R, Muscle.Lumbar_R);
                AddMuscleWithOverride(selectedMuscles, data.lumbar_L, Muscle.Lumbar_L);
            }
        }
        return selectedMuscles.ToArray();
    }
    public void SendHapticEventFromFileName()
    {
        (MicroSensation loadedEvent, Muscle[] muscles) = LoadHapticEventFromFileForUse(sensationName);
        if (loadedEvent != null && muscles != null && muscles.Length > 0)
        {
            OWO.Send(loadedEvent.WithMuscles(muscles));
        }
        else
        {
            if (loadedEvent == null)
            {
                Debug.Log("Loaded Event Null");
            }
            if (muscles == null)
            {
                Debug.Log("Muscle Array Null");
            }
            if (muscles.Length <= 0)
            {
                Debug.Log("Muscle Array length Issue");
            }
            Debug.LogError("Failed to send haptic event from file.");
        }
    }
    public void AppendSensationsFromFiles()
    {
        List<(MicroSensation sensation, Muscle[] muscles)> loadedData = new List<(MicroSensation, Muscle[])>();

        foreach (var fileName in fileNames)
        {
            (MicroSensation loadedEvent, Muscle[] muscles) = LoadSensationsForAppend(fileName);
            if (loadedEvent != null && muscles != null && muscles.Length > 0)
            {
                loadedData.Add((loadedEvent, muscles));
            }
            else
            {
                LogFailedLoadingSensation(loadedEvent, muscles);
            }
        }

        if (loadedData.Count < 2)
        {
            Debug.Log("Invalid number of loaded sensations. Must be 2 or Above.");
            return;
        }

        var combinedSensations = CombineAppendedSensations(loadedData, 0);

        // Serialize and save to JSON file
        string directoryPath = "Assets/Sensation Events";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        AppendedMicroSensations sensationtojson = new()
        {
            data = combinedSensations
        };

        string path = Path.Combine(directoryPath, appendFileName + ".json");
        string jsonString = JsonUtility.ToJson(sensationtojson, true);
        File.WriteAllText(path, jsonString);

        AssetDatabase.Refresh();
        Debug.Log("Sensation Json Saved");
    }

    private void LogFailedLoadingSensation(MicroSensation loadedEvent, Muscle[] muscles)
    {
        if (loadedEvent == null)
        {
            Debug.Log("Loaded Event Null");
        }
        if (muscles == null)
        {
            Debug.Log("Muscle Array Null");
        }
        if (muscles.Length <= 0)
        {
            Debug.Log("Muscle Array length Issue");
        }
        Debug.LogError("Failed to send haptic event from file.");
    }

    public void SendSensationFromJson()
    {
        string directoryPath = "Assets/Sensation Events";
        if (!Directory.Exists(directoryPath))
        {
            Debug.Log("Folder Not Found");
        }
        string path = Path.Combine(directoryPath, appendFileName + ".json");

        string fromjsonString = File.ReadAllText(path);
        AppendedMicroSensations dataObject = JsonUtility.FromJson<AppendedMicroSensations>(fromjsonString);

        // to send saved json appened files
        OWO.Send(Sensation.Parse(dataObject.data));
    }

    private string CombineAppendedSensations(List<(MicroSensation sensation, Muscle[] muscles)> data, int index)
    {
        if (index == data.Count - 1) // base case, if we're at the last item
        {
            return data[index].sensation.WithMuscles(data[index].muscles);
        }

        return data[index].sensation.WithMuscles(data[index].muscles).Append(CombineAppendedSensations(data, index + 1));
    }
    
    private (MicroSensation, Muscle[]) LoadSensationsForAppend(string sensationName)
    {
        string path = Path.Combine("Assets/MicroSensation Events", sensationName + ".json");
        if (!File.Exists(path)) return (null, null);

        string jsonData = File.ReadAllText(path);
        HapticData data = JsonUtility.FromJson<HapticData>(jsonData);

        MicroSensation loadedHapticEvent = SensationsFactory.Create(
            data.frequency,
            data.duration,
            data.intensityPercentage,
            data.rampUpInMills,
            data.rampDownInMills,
            data.exitDelay
        );

        Muscle[] selectedMuscles = GetSelectedMusclesFromData(data);

        return (loadedHapticEvent, selectedMuscles);
    }
    public void SendHapticEventFromFileDropDown()
    {
        (MicroSensation loadedEvent, Muscle[] muscles) = LoadHapticEventFromFileForUse(availableFiles[selectedFileIndex]);
        if (loadedEvent != null && muscles != null && muscles.Length > 0)
        {
            OWO.Send(loadedEvent.WithMuscles(muscles));
        }
        else
        {
            if (loadedEvent == null)
            {
                Debug.Log("Loaded Event Null");
            }
            if (muscles == null)
            {
                Debug.Log("Muscle Array Null");
            }
            if (muscles.Length <= 0)
            {
                Debug.Log("Muscle Array length Issue");
            }
            Debug.LogError("Failed to send haptic event from file.");
        }
    }
}
