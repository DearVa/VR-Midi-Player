using UnityEngine;
using Valve.VR;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FrequencyTest : MonoBehaviour {
	public SteamVR_Action_Vibration vibration;
	public float frequency, duration, amplitude;
	public SteamVR_Input_Sources inputSources;
}

#if UNITY_EDITOR
[CustomEditor(typeof(FrequencyTest))]
public class FrequencyTestEditor : Editor {
	public override void OnInspectorGUI() {
		base.OnInspectorGUI();
		var ft = (FrequencyTest)target;
		if (GUILayout.Button("Execute")) {
			ft.vibration.Execute(0, ft.duration, ft.frequency, ft.amplitude, ft.inputSources);
		}
		if (GUILayout.Button("Stop")) {
			ft.vibration.Execute(0, 0, 0, 0, ft.inputSources);
		}
	}
}
#endif