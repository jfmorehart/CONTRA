using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIBuildConfirm : UIMenu
{
    float laf;//last active frameTime

    public static int buildType;
	public string[] types;
    public Texture[] icons;
    public string[] subtitles;

    public TMP_Text type_text;
	public TMP_Text sub_text;
	public RawImage disp_icon;



	// Update is called once per frame
	void Update()
    {
        if(Time.time - laf > Time.deltaTime) {
			//hack we store the buildtype in target nation lmao
			SwitchType();
		}

        laf = Time.time;
    }

    void SwitchType()
    {
        type_text.text = types[buildType];
        disp_icon.texture = icons[buildType];
        sub_text.text = subtitles[buildType];

    }

    public void Confirm() {
        PlayerInput.ins.BuildBase(buildType);
    }
}
