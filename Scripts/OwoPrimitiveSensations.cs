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

            // Move this block outside of the EditorGUI.EndChangeCheck() to always display the value.
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
        public bool allMuscles;
        public bool backMuscles;
        public bool frontMuscles;
        public int frequency;
        public float duration;
        public int intensityPercentage;
        public float rampUpInMills;
        public float rampDownInMills;
        public float exitDelay;
        public bool pectoral_R;
        public bool pectoral_L;
        public bool abdominal_R;
        public bool abdominal_L;
        public bool arm_R;
        public bool arm_L;
        public bool dorsal_R;
        public bool dorsal_L;
        public bool lumbar_R;
        public bool lumbar_L;
    }

    private MicroSensation startup;
    private Sensation bulletshot;
    private MicroSensation hapticEvent;
    [Header("File Settings")]
    [SerializeField, Tooltip("Filename to save or load the haptic event.")]
    private string fileName = "hapticEvent";

    [Header("Haptic Event Settings")]
    [SerializeField, Tooltip("Frequency for the haptic event."), Frequency(1, 100)]
    private int frequency = 100;
    [SerializeField, Tooltip("Duration of the haptic event.")]
    private float duration = 1.0f;
    [SerializeField, Tooltip("Intensity percentage for the haptic event."), IntensityPercentage(1, 100)]
    private int intensityPercentage = 25;
    [MillisecondsMapping(0, 2)]
    [SerializeField, Tooltip("Ramp up time in milliseconds.")]
    private float rampUpInMills = 0f;
    [MillisecondsMapping(0, 2)]
    [SerializeField, Tooltip("Ramp down time in milliseconds.")]
    private float rampDownInMills = 0f;
    [SerializeField, Tooltip("Exit delay for the haptic event.")]
    private float exitDelay = 0f;

    [Header("Muscle Groups")]
    [SerializeField, Tooltip("If Used All Other Muscle Settings are Ignored")]
    private bool allMuscles=true;
    [SerializeField, Tooltip("If Used Settings of Muscles On the Back are Ignored")]
    private bool backMuscles;
    [SerializeField, Tooltip("If Used Settings of Muscle On the Front Ignored")]
    private bool frontMuscles;
    [Header("Front Muscles")]
    [SerializeField, Tooltip("Right Pectoral muscle.")]
    private bool pectoral_R;
    [SerializeField, Tooltip("Left Pectoral muscle.")]
    private bool pectoral_L;
    [SerializeField, Tooltip("Right Abdominal muscle.")]
    private bool abdominal_R;
    [SerializeField, Tooltip("Left Abdominal muscle.")]
    private bool abdominal_L;
    [SerializeField, Tooltip("Right Arm.")]
    private bool arm_R;
    [SerializeField, Tooltip("Left Arm.")]
    private bool arm_L;
    [Header("Back Muscles")]
    [SerializeField, Tooltip("Right Dorsal muscle.")]
    private bool dorsal_R;
    [SerializeField, Tooltip("Left Dorsal muscle.")]
    private bool dorsal_L;
    [SerializeField, Tooltip("Right Lumbar muscle.")]
    private bool lumbar_R;
    [SerializeField, Tooltip("Left Lumbar muscle.")]
    private bool lumbar_L;
    private bool prevAllMuscles = false;
    private bool prevBackMuscles = false;
    private bool prevFrontMuscles = false;

    [HideInInspector, Tooltip("List of available haptic event files.")]
    public string[] availableFiles;
    [HideInInspector, Tooltip("Currently selected haptic event file.")]
    public int selectedFileIndex = 0;

    private void Start()
    {
        // "Sartup Sensation" I added For Debugging
        startup = SensationsFactory.Create(100, 1, 25, 1, 1, 0);
        // Copy Of the Shot with exit Default Sensation From the Sensation Creator App
        bulletshot = Sensation.Parse("30,1,100,0,0,0,|0%100&20,1,100,0,0,0,|6%100&50,5,80,0,300,0,|6%100,0%100");
        hapticEvent = SensationsFactory.Create(frequency, duration, intensityPercentage, rampUpInMills, rampDownInMills, exitDelay);
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
        return pectoral_R || pectoral_L || abdominal_R || abdominal_L ||
               arm_R || arm_L || dorsal_R || dorsal_L || lumbar_R || lumbar_L;
    }

    private void ClearGroupMusclesExcept(string except)
    {
        if (except != "allMuscles") allMuscles = false;
        if (except != "backMuscles") backMuscles = false;
        if (except != "frontMuscles") frontMuscles = false;
    }

    private void ClearIndividualMuscles()
    {
        pectoral_R = false;
        pectoral_L = false;
        abdominal_R = false;
        abdominal_L = false;
        arm_R = false;
        arm_L = false;
        dorsal_R = false;
        dorsal_L = false;
        lumbar_R = false;
        lumbar_L = false;
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
    }

    private List<Muscle> GetSelectedMuscles()
    {
        var muscles = new List<Muscle>();

        if (allMuscles)
        {
            muscles.AddRange(Muscle.All);
            return muscles;
        }

        if (frontMuscles)
        {
            muscles.AddRange(Muscle.Front);
        }
        else
        {
            if (pectoral_R) muscles.Add(Muscle.Pectoral_R);
            if (pectoral_L) muscles.Add(Muscle.Pectoral_L);
            if (abdominal_R) muscles.Add(Muscle.Abdominal_R);
            if (abdominal_L) muscles.Add(Muscle.Abdominal_L);
            if (arm_R) muscles.Add(Muscle.Arm_R);
            if (arm_L) muscles.Add(Muscle.Arm_L);
        }

        if (backMuscles)
        {
            muscles.AddRange(Muscle.Back);
        }
        else
        {
            if (dorsal_R) muscles.Add(Muscle.Dorsal_R);
            if (dorsal_L) muscles.Add(Muscle.Dorsal_L);
            if (lumbar_R) muscles.Add(Muscle.Lumbar_R);
            if (lumbar_L) muscles.Add(Muscle.Lumbar_L);
        }

        return muscles;
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
        hapticEvent = SensationsFactory.Create(frequency, duration, intensityPercentage, rampUpInMills, rampDownInMills, exitDelay);
        OWO.Send(hapticEvent.WithMuscles(GetSelectedMuscles().ToArray()));
    }

    private void StopHapticEventBasedOnMuscles()
    {
        OWO.Stop();
    }

    public void RefreshAvailableFiles()
    {
        string directoryPath = "Assets/Haptic Events";
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
        SaveHapticEventToFile(fileName);
    }
    public void SendTestingEvent()
    {
        
        OWO.Send(bulletshot);
    }

    public void LoadHapticEvent()
    {
        LoadHapticEventFromFile();
    }

    public void SaveHapticEventToFile(string fileName)
    {
        HapticData data = new()
        {
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
        bool isAnyBoolTrue = data.allMuscles || data.backMuscles || data.frontMuscles || data.pectoral_R || data.pectoral_L ||
                             data.abdominal_R || data.abdominal_L || data.arm_R || data.arm_L || data.dorsal_R ||
                             data.dorsal_L || data.lumbar_R || data.lumbar_L;

        // If none of the booleans are true, set allMuscles to true
        if (!isAnyBoolTrue)
        {
            data.allMuscles = true;
        }

        string directoryPath = "Assets/Haptic Events";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string path = Path.Combine(directoryPath, fileName + ".json"); // using .json extension
        string jsonData = JsonUtility.ToJson(data, true); // true to make it nicely formatted
        File.WriteAllText(path, jsonData);

        AssetDatabase.Refresh();
        RefreshAvailableFiles();
    }

    private void LoadHapticEventFromFile()
    {
        string path = Path.Combine("Assets/Haptic Events", availableFiles[selectedFileIndex] + ".json");
        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            HapticData data = JsonUtility.FromJson<HapticData>(jsonData);

            frequency = data.frequency;
            duration = data.duration;
            intensityPercentage = data.intensityPercentage;
            rampUpInMills = data.rampUpInMills;
            rampDownInMills = data.rampDownInMills;
            exitDelay = data.exitDelay;

            // Set the muscle boolean fields
            allMuscles = data.allMuscles;
            backMuscles = data.backMuscles;
            frontMuscles = data.frontMuscles;
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

            // Check if at least one of the bool values is true
            bool isAnyBoolTrue = data.allMuscles || data.backMuscles || data.frontMuscles || data.pectoral_R || data.pectoral_L ||
                                 data.abdominal_R || data.abdominal_L || data.arm_R || data.arm_L || data.dorsal_R ||
                                 data.dorsal_L || data.lumbar_R || data.lumbar_L;

            // If none of the booleans are true, set allMuscles to true
            if (!isAnyBoolTrue)
            {
                allMuscles = true;
            }

            // Update the haptic event based on loaded settings
            hapticEvent = SensationsFactory.Create(frequency, duration, intensityPercentage, rampUpInMills, rampDownInMills, exitDelay);
            fileName = availableFiles[selectedFileIndex];
        }
        else
        {
            Debug.LogError("Haptic event file not found: " + path);
        }
    }
    private (MicroSensation, Muscle[]) LoadHapticEventFromFileForUse(string fileName)
    {
        string path = Path.Combine("Assets/Haptic Events", fileName + ".json");
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
                if (data.pectoral_R) selectedMuscles.Add(Muscle.Pectoral_R);
                if (data.pectoral_L) selectedMuscles.Add(Muscle.Pectoral_L);
                if (data.abdominal_R) selectedMuscles.Add(Muscle.Abdominal_R);
                if (data.abdominal_L) selectedMuscles.Add(Muscle.Abdominal_L);
                if (data.arm_R) selectedMuscles.Add(Muscle.Arm_R);
                if (data.arm_L) selectedMuscles.Add(Muscle.Arm_L);
            }

            if (data.backMuscles)
            {
                selectedMuscles.AddRange(Muscle.Back);
            }
            else
            {
                if (data.dorsal_R) selectedMuscles.Add(Muscle.Dorsal_R);
                if (data.dorsal_L) selectedMuscles.Add(Muscle.Dorsal_L);
                if (data.lumbar_R) selectedMuscles.Add(Muscle.Lumbar_R);
                if (data.lumbar_L) selectedMuscles.Add(Muscle.Lumbar_L);
            }
        }

        return selectedMuscles.ToArray();
    }


    public void SendHapticEventFromFileName()
    {
        (MicroSensation loadedEvent, Muscle[] muscles) = LoadHapticEventFromFileForUse(fileName);
        if (loadedEvent != null && muscles != null && muscles.Length > 0)
        {
            OWO.Send(loadedEvent.WithMuscles(muscles));
        }
        else
        {
            if(loadedEvent == null)
            {
                Debug.Log("Loaded Event Null");
            }
            if(muscles == null)
            {
                Debug.Log("Muscle Array Null");
            }
            if(muscles.Length <= 0)
            {
                Debug.Log("Muscle Array length Issue");
            }
            Debug.LogError("Failed to send haptic event from file.");
        }
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
