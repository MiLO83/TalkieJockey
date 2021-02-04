using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;
using System;
public class BasicAudio : MonoBehaviour {

	public Text GameDialogText;
	public int currentDialogNum = 0;
	public List<string> textLinesRaw = new List<string>();
	public List<string> textLines = new List<string>();
	public List<string> filenameLines = new List<string>();
	public string curText = "";
	public AudioSource currentAsrc;
	private bool isRecording = false;
	public Dropdown recordDropdown;
	public GameObject popUp;
	private int micRecordNum = 0;
	private int audioListenerRecordNum = 0;
	private int exportClipRecordNum = 0;
	private int recordNum = 0;
	private List<AudioClip> myClips = new List<AudioClip> ();
	private bool isplaying;
	//text information
	public Text info;
	public Text time;
	//sliders
	public Slider rightcropSli;
	public Slider leftcropSli;
	public Slider playbackSli;
	//buttons
	public GameObject trimButton;
	public GameObject removeSilenceButton;
	public GameObject micRecordButton;
	public GameObject playRecordingButton;
	public GameObject exportRecordingButton;
	public GameObject previousDialogButton;
	public GameObject nextDialogButton;
	//waveform variables
	private float tracklength = 0.0f;
	private float currentTrackTime = 0.0f;
	private float MicTime;
	private bool PlayHeadTouch;
	public WaveFormDraw wfDraw; 

	void Start()
    {
		string[] stringSeparators = new string[] { " = FILE : " };
		exportRecordingButton.SetActive(false);
		DirectoryInfo dirInfo = new DirectoryInfo(Application.dataPath + "/../Input/");
		foreach (FileInfo fi in dirInfo.EnumerateFiles())
        {
			if (fi.Extension == ".txt")
            {
				string[] fileLines = File.ReadAllLines(fi.FullName);
				for (int i = 0; i < fileLines.Length; i++)
                {
					if (fileLines[i] != null)
					{
						if (fileLines[i].Contains(stringSeparators[0]))
						{
							curText += fileLines[i].Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries)[0];
							if (curText.Length > 0)
							{
								if (fileLines[i] != "\n" && fileLines[i].Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries).Length > 1)
								{
									textLines.Add(curText);
									filenameLines.Add(fileLines[i].Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries)[1]);
									Debug.Log(curText);
									curText = "";
								}
							}
						}
						else
						{
							curText += fileLines[i];
						}
					}
                }
            }
        }
		if (textLines.Count > 0)
		GameDialogText.text = textLines[0];
	}

	void Update(){
		//if recording
		if (isRecording == true)
		{
			MicTime  += Time.deltaTime;
			string minutes = Mathf.Floor(MicTime / 60).ToString("0");
			string seconds = (MicTime % 60).ToString("00");
			time.text = minutes + ":" + seconds;
		}  else {
			MicTime = 0;
		}
		if (currentAsrc.clip != null && currentAsrc.isPlaying) {
			currentTrackTime = (float)currentAsrc.time;
			string minutes = Mathf.Floor(tracklength / 60).ToString("0");
			string seconds = (tracklength % 60).ToString("00");
			string tminutes = Mathf.Floor(currentTrackTime / 60).ToString("0");
			string tseconds = (currentTrackTime % 60).ToString("00");
			time.text = tminutes + ":" + tseconds + " / " + minutes + ":" + seconds;
		}
		//if clips is supposed to be playing and is playing make timesamples = the left clip value and then play so looping is enabled
		if (currentAsrc.isPlaying == false && isplaying == true) {
			currentAsrc.timeSamples = (int)leftcropSli.value / currentAsrc.clip.channels;
			PlayStopRecording ();
		}
		if (currentAsrc.clip != null && (leftcropSli.value > 0 || rightcropSli.value < currentAsrc.clip.samples * currentAsrc.clip.channels)) {
			trimButton.SetActive(true);
		} else {
			trimButton.SetActive(false);
		}
		if (leftcropSli.value > rightcropSli.value) {
			leftcropSli.value = rightcropSli.value;
		} 
		if(rightcropSli.value <= leftcropSli.value){
			rightcropSli.value = leftcropSli.value ;
		}
		if (currentAsrc.clip == null) {
			return;
		}
		if ((currentAsrc.timeSamples * currentAsrc.clip.channels) >= (int)rightcropSli.value  && currentAsrc.isPlaying == true ) {
			currentAsrc.timeSamples = (int)leftcropSli.value/currentAsrc.clip.channels;
			playbackSli.value = currentAsrc.timeSamples* currentAsrc.clip.channels;
		}
		if (isplaying && PlayHeadTouch == false) { //updates the playhead
			playbackSli.value = currentAsrc.timeSamples * currentAsrc.clip.channels;
		} 
	}

	public void MicStartStop(){
		if (isRecording) {
			playRecordingButton.SetActive(true);
			RARE.Instance.StopMicRecording (CheckFileName("Mic Recording"), ClipLoaded, popUp);
			recordNum++;
			isRecording = false;
			info.text = "Done.";
			micRecordButton.GetComponentInChildren<Text> ().text = "1. Mic Record";
			exportRecordingButton.SetActive(true);
			nextDialogButton.SetActive(true);
			previousDialogButton.SetActive(true);
			trimButton.SetActive(true);
			playRecordingButton.SetActive(true);
			removeSilenceButton.SetActive(true);
		} else {
			if (currentAsrc.isPlaying) {
				PlayStopRecording();
			}
			playRecordingButton.SetActive(false);
			info.text = "Mic recording...";
			isRecording = true;
			RARE.Instance.StartMicRecording (599);
			micRecordButton.GetComponentInChildren<Text> ().text = "Stop";
			exportRecordingButton.SetActive(false);
			nextDialogButton.SetActive(false);
			previousDialogButton.SetActive(false);
			trimButton.SetActive(false);
			playRecordingButton.SetActive(false);
			removeSilenceButton.SetActive(false);
		}
	}
	public void NextDialog()
    {
		currentDialogNum++;
		if (currentDialogNum > textLines.Count - 1)
		{
			currentDialogNum = 0;
		}
		GameDialogText.text = textLines[currentDialogNum];
		exportRecordingButton.SetActive(false);
		playRecordingButton.SetActive(false);
		removeSilenceButton.SetActive(false);
	}
	public void PreviousDialog()
	{
		currentDialogNum--;
		if (currentDialogNum < 0)
		{
			currentDialogNum = textLines.Count - 1;
		}
		GameDialogText.text = textLines[currentDialogNum];
		exportRecordingButton.SetActive(false);
		playRecordingButton.SetActive(false);
		removeSilenceButton.SetActive(false);
	}
	public void ExportToWavFile(){
		string filename = CheckFileName("Export Clip");
		RARE.Instance.ExportClip (filenameLines[currentDialogNum].Remove(filenameLines[currentDialogNum].Length - 4, 4), currentAsrc.clip, ClipLoaded, popUp);
        recordNum++;
		isRecording = false;
		info.text = "Exported to : " + filename;
		currentDialogNum++;		
		if (currentDialogNum > textLines.Count - 1)
        {
			currentDialogNum = 0;
        }
		GameDialogText.text = textLines[currentDialogNum];
		exportRecordingButton.SetActive(false);
	}

	public void PlayStopRecording(){
		if (currentAsrc.isPlaying) {
			currentAsrc.Pause();
			info.text = "Stopped playback.";
			playRecordingButton.GetComponentInChildren<Text>().text = "Play Recording";
			isplaying = false;

		} else {			
			currentAsrc.Play();
			info.text = "Looping playback...";
			playRecordingButton.GetComponentInChildren<Text>().text = "Stop";
			isplaying = true;
		}
	}

	public void ListenerStartStop()
	{
		if (isRecording)
		{
			micRecordButton.SetActive(true);
			playRecordingButton.SetActive(true);
			RARE.Instance.StopAudioListenerRecording(CheckFileName("Audio Recording"), ClipLoaded, popUp);
			recordNum++;
			isRecording = false;
			info.text = "Done.";
		}
		else
		{
			if (currentAsrc.isPlaying)
			{
				PlayStopRecording();
			}
			micRecordButton.SetActive(false);
			playRecordingButton.SetActive(false);
			info.text = "Audio recording...";
			isRecording = true;
			RARE.Instance.StartAudioListenerRecording();
		}
	}
	public void DropdownChanged(int val){
		//we need to shut off recording if a recording session is in progress
		if (isRecording == true) {
			if (micRecordButton.activeSelf == true) {
				MicStartStop();
			}
			else
			{
				ListenerStartStop();
			}
		}
		currentAsrc.Stop ();
		currentAsrc.timeSamples = 0;
		playRecordingButton.SetActive(true);
		tracklength = (float)((myClips[val - 1].length));
		//time stuff also in update and slider changed convert to minutes and seconds
		string minutes = Mathf.Floor(tracklength / 60).ToString("0");
		string seconds = (tracklength % 60).ToString("00");
		info.text = "Stopped playback.";
		playRecordingButton.GetComponentInChildren<Text>().text = "Play Recording";
		if (val > 0) {
			currentAsrc.clip = myClips [val - 1];
			//where the waveform is drawn
			wfDraw.StartWaveFormGeneration (myClips [val - 1]);
		}
		playbackSli.value = 0;
	}

	public void ClipLoaded(AudioClip myClip, string clipName = null){
		if (clipName != null) {
			myClip.name = clipName;
		} else {
			myClip.name = "untitled";
		}
		for (int i = 1; i < recordDropdown.options.Count; i++) {
			if (recordDropdown.options [i].text.Equals (myClip.name)) {
				myClips.RemoveAt (i-1);
				recordDropdown.options.RemoveAt (i);
				i = recordDropdown.options.Count;
			}
		}
		myClips.Add (myClip);
		recordDropdown.options.Add (new Dropdown.OptionData () { text = myClip.name });
		if (recordDropdown.value == recordNum) {
			DropdownChanged (recordNum);
		} else {
			recordDropdown.value = recordNum;
		}
		info.text = "Clip loaded.";
	}

	public void TrimClip() {
		RARE.Instance.CropAudioClip (currentAsrc.clip.name, (int)leftcropSli.value, (int)rightcropSli.value, currentAsrc.clip, ClipLoaded, popUp);
		info.text = "Trimmed & replaced file";
	}

	public void OutputVolumeChange(float input) {
		RARE.Instance.OutputVolume (input);
		info.text = "Volume: " + input;
	}

	public void RemoveSilenceFromEnds() {
		currentAsrc.clip = RARE.Instance.RemoveSilenceFromFrontOfAudioClip (currentAsrc.clip);
		currentAsrc.clip = RARE.Instance.RemoveSilenceFromEndOfAudioClip (currentAsrc.clip);
		//Notice to actually save the clip as a file and import it into the waveform editor you have to export it
		//In the function TrimClip() this is done in RARE.cs itself.. check out the CropAudioClip() function in RARE.cs
		RARE.Instance.ExportClip (currentAsrc.clip.name, currentAsrc.clip, ClipLoaded, popUp);
		info.text = "Trimmed & replaced file";
	}

	public string CheckFileName(string input){
		if (!Directory.Exists(Application.dataPath + "/../Output/"))
        {
			Directory.CreateDirectory(Application.dataPath + "/../Output/");
		}
		return input;
	}

	public void PlayHeadPointerDown() {
		PlayHeadTouch = true;
	}

	public void PlayHeadPointerUp() {
		PlayHeadTouch = false;
	}

	public void PlayHeadPositionChange() {
		if (PlayHeadTouch == true) {
			if (currentAsrc.isPlaying) {
				PlayStopRecording();
			}
			if (currentAsrc.clip != null) {
				currentAsrc.timeSamples = (int)(playbackSli.value) / currentAsrc.clip.channels;
				currentTrackTime = currentAsrc.time;
				string minutes = Mathf.Floor(tracklength / 60).ToString("0");
				string seconds = (tracklength % 60).ToString("00");
				string tminutes = Mathf.Floor(currentTrackTime / 60).ToString("0");
				string tseconds = (currentTrackTime % 60).ToString("00");
				time.text = tminutes + ":" + tseconds + " / " + minutes + ":" + seconds;
			}
		}
	}
}
