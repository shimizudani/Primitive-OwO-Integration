#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(OwoPrimitiveSensations))]
public class OwoPrimitiveSensationsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        OwoPrimitiveSensations myScript = (OwoPrimitiveSensations)target;
        GUILayout.Space(10); // Add 10 pixels of space
        if (GUILayout.Button("Initialize Haptic Suit"))
        {
            myScript.InitializeSuitButtonPressed();
        }
        GUILayout.Space(15);
        GUILayout.BeginHorizontal(); // Begin horizontal group
        if (GUILayout.Button("Save A Haptic Event \nBy Name"))
        {
            myScript.SaveHapticEvent();
        }
        GUILayout.Space(10); // Add space between the buttons
        if (GUILayout.Button("Load A Haptic Event \nBy Drop Down Menu"))
        {
            myScript.LoadHapticEvent();
        }
        GUILayout.EndHorizontal(); // End horizontal group
        GUILayout.Space(10); // Add 10 pixels of space after the horizontal group
        myScript.selectedFileIndex = EditorGUILayout.Popup("Select Haptic Event File", myScript.selectedFileIndex, myScript.availableFiles);
        GUILayout.Space(10);
        if (GUILayout.Button("Send A Haptic Event \nWith Inspector Settings"))
        {
            myScript.SendHapticButtonPressed();
        }
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Send A Haptic Event \nWith Drop Down Menu"))
        {
            myScript.SendHapticEventFromFileDropDown();
        }
        GUILayout.Space(20);
        if (GUILayout.Button("Send A Haptic Event \nWith Name From File"))
        {
            myScript.SendHapticEventFromFileName();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(15);
        if (GUILayout.Button("Stop Haptic Event"))
        {
            myScript.StopHapticButtonPressed();
        }
        GUILayout.Space(10); // Add 10 pixels of space
        if (GUILayout.Button("Disconnect Vest"))
        {
            myScript.DisconnectVestButtonPressed();
        }
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Append MicroSensations And Save\n to Json By Append File Name"))
        {
            myScript.AppendSensationsFromFiles();
        }
        GUILayout.Space(15);
        if (GUILayout.Button("Send Sensation From \nJson By Append File Name"))
        {
            myScript.SendSensationFromJson();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        DrawDefaultInspector(); // This draws the default properties

    }
}


#endif
