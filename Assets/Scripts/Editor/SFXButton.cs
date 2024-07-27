using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(SFX))]
public class SFXBUTTON : Editor
{
	public SFX targetScript;
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		targetScript = GameObject.Find("SFX").GetComponent<SFX>();
		var myScript = targetScript as SFX;
		if (targetScript == null) return;
		myScript.reloadAudio = EditorGUILayout.Toggle("[Editor] Reload Audio Button", targetScript.reloadAudio); //Returns true when user clicks

		if (myScript.reloadAudio) {
			myScript.LoadAllAudioFiles();
			myScript.reloadAudio = false;
			EditorUtility.SetDirty(myScript);
		}
	}
}
