using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;
using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class TranslationResult
{
	public DetectedLanguage DetectedLanguage { get; set; }
	public TextResult SourceText { get; set; }
	public Translation[] Translations { get; set; }
}

public class DetectedLanguage
{
	public string Language { get; set; }
	public float Score { get; set; }
}

public class TextResult
{
	public string Text { get; set; }
	public string Script { get; set; }
}

public class Translation
{
	public string Text { get; set; }
	public TextResult Transliteration { get; set; }
	public string To { get; set; }
	public Alignment Alignment { get; set; }
	public SentenceLength SentLen { get; set; }
}

public class Alignment
{
	public string Proj { get; set; }
}

public class SentenceLength
{
	public int[] SrcSentLen { get; set; }
	public int[] TransSentLen { get; set; }
}

public class BasicAudio : MonoBehaviour {
	public static InputField keyInput;
	public static InputField locationInput;
	public static InputField languageInput;
	public Button BatchTranslateBtn;
	public static string translatedText = "";
	private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com/";
	private static readonly string location = "canadacentral";
	public string[] langSeparator = new string[] { "%J" };
	public UnityEngine.UI.Text NoInput;
	public UnityEngine.UI.Text NoInputJ;
	public Toggle skipExistingWAVs;
	public InputField GameDialogTextEng;
	public InputField GameDialogTextEtc;
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
	private List<AudioClip> myClips = new List<AudioClip>();
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


	static async Task TranslateString(string language, string textToTranslate)
    {
		string route = "/translate?api-version=3.0&from=en&to=" + language + "& to=it";
		object[] body = new object[] { new { Text = textToTranslate } };
		var requestBody = JsonConvert.SerializeObject(body);

		using (var client = new HttpClient())
		using (var request = new HttpRequestMessage())
		{
			
			// Build the request.
			request.Method = HttpMethod.Post;
			request.RequestUri = new Uri(endpoint + route);
			request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
			request.Headers.Add("Ocp-Apim-Subscription-Key", keyInput.text);
			request.Headers.Add("Ocp-Apim-Subscription-Region", locationInput.text);

			// Send the request and get response.
			HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
			// Read response as a string.
			string result = await response.Content.ReadAsStringAsync();
			TranslationResult[] deserializedOutput = JsonConvert.DeserializeObject<TranslationResult[]>(result);
			// Iterate over the deserialized results.
			translatedText = deserializedOutput[0].Translations[0].Text;
			Debug.Log(deserializedOutput[0].Translations[0].Text);
			PlayerPrefs.SetInt("Characters", PlayerPrefs.GetInt("Characters") + textToTranslate.Length);
			PlayerPrefs.Save();
		}
	}
	void Start()
	{
		string[] stringSeparators = new string[] { " = FILE : " };

		exportRecordingButton.SetActive(false);
		nextDialogButton.SetActive(true);
		previousDialogButton.SetActive(true);
		trimButton.SetActive(false);
		playRecordingButton.SetActive(false);
		removeSilenceButton.SetActive(false);
		DirectoryInfo dirInfo = new DirectoryInfo(Application.dataPath + "/../Input/");
		foreach (FileInfo fi in dirInfo.EnumerateFiles())
		{
			if (fi.Extension == ".txt")
			{
				string[] fileLines = File.ReadAllLines(fi.FullName, Encoding.GetEncoding("shift-jis"));
				for (int i = 0; i < fileLines.Length; i++)
				{
					if (fileLines[i] != null)
					{
						if (fileLines[i].Contains(stringSeparators[0]))
						{
							curText += fileLines[i].Split(stringSeparators, StringSplitOptions.None)[0];
							if (curText.Length > 0)
							{
								if (fileLines[i] != "\n" && fileLines[i].Split(stringSeparators, StringSplitOptions.None).Length > 1)
								{
									textLines.Add(curText);
									filenameLines.Add(fileLines[i].Split(stringSeparators, StringSplitOptions.None)[1].Remove(fileLines[i].Split(stringSeparators, StringSplitOptions.None)[1].Length - 4, 4));
									Debug.Log(curText);
									curText = "";
                                    NoInput.enabled = false;
									NoInputJ.enabled = false;
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
			if (!File.Exists(Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".txt"))
			{
				string[] splitText = textLines[currentDialogNum].Split(langSeparator, StringSplitOptions.None);
				if (splitText != null)
				{
					if (splitText.Length > 0)
						GameDialogTextEng.text = splitText[0];
					if (splitText.Length > 1 && splitText[1] != null)
					{
						GameDialogTextEtc.text = splitText[1];
					}
					else
					{
						GameDialogTextEtc.text = "";
					}
				}
			}
			else
			{
				string[] splitText = File.ReadAllText(Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".txt").Split(langSeparator, StringSplitOptions.None);
				if (splitText != null)
				{
					if (splitText.Length > 0)
						GameDialogTextEng.text = splitText[0];
					if (splitText.Length > 1 && splitText[1] != null)
					{
						GameDialogTextEtc.text = splitText[1];
					}
					else
					{
						GameDialogTextEtc.text = "";
					}
				}
			}
		Debug.Log("Aprox Translated Char : " + PlayerPrefs.GetInt("Characters").ToString());
	}
	public void BatchTranslateButton()
    {
		BatchTranslateBtn.enabled = false;
		StartCoroutine(BatchTranslate());
    }
	public IEnumerator BatchTranslate()
    {
		while (currentDialogNum < textLines.Count)
		{
			if (!File.Exists(Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".txt"))
			{

				string[] splitText = textLines[currentDialogNum].Split(langSeparator, StringSplitOptions.None);
				if (splitText != null)
				{
					if (splitText.Length > 0)
					{
						TranslateString(languageInput.text, splitText[0]);
						GameDialogTextEng.text = translatedText;
						if (splitText.Length > 1 && splitText[1] != null)
						{
							GameDialogTextEtc.text = splitText[1];
						}
						else
						{
							GameDialogTextEtc.text = "";
						}
					}
					File.WriteAllText(Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".txt", GameDialogTextEng.text + "%J" + GameDialogTextEtc.text);
				}
			}

			NextDialog();
			yield return new WaitForSeconds(1.75f);
		}
	}

	void Update() {
		//if recording
		if (isRecording == true)
		{
			MicTime += Time.deltaTime;
			string minutes = Mathf.Floor(MicTime / 60).ToString("0");
			string seconds = (MicTime % 60).ToString("00");
			time.text = minutes + ":" + seconds;
		} else {
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
			PlayStopRecording();
		}
		if (currentAsrc.clip != null && (leftcropSli.value > 0 || rightcropSli.value < currentAsrc.clip.samples * currentAsrc.clip.channels)) {
			trimButton.SetActive(true);
		} else {
			trimButton.SetActive(false);
		}
		if (leftcropSli.value > rightcropSli.value) {
			leftcropSli.value = rightcropSli.value;
		}
		if (rightcropSli.value <= leftcropSli.value) {
			rightcropSli.value = leftcropSli.value;
		}
		if (currentAsrc.clip == null) {
			return;
		}
		if ((currentAsrc.timeSamples * currentAsrc.clip.channels) >= (int)rightcropSli.value && currentAsrc.isPlaying == true) {
			currentAsrc.timeSamples = (int)leftcropSli.value / currentAsrc.clip.channels;
			playbackSli.value = currentAsrc.timeSamples * currentAsrc.clip.channels;
		}
		if (isplaying && PlayHeadTouch == false) { //updates the playhead
			playbackSli.value = currentAsrc.timeSamples * currentAsrc.clip.channels;
		}
	}

	public void MicStartStop() {
		if (isRecording) {
			playRecordingButton.SetActive(true);
			RARE.Instance.StopMicRecording(CheckFileName("Mic Recording"), ClipLoaded, popUp);
			recordNum++;
			isRecording = false;
			info.text = "Done.";
			micRecordButton.GetComponentInChildren<Text>().text = "1. Mic Record";
			exportRecordingButton.SetActive(true);
			nextDialogButton.SetActive(true);
			previousDialogButton.SetActive(true);
			trimButton.SetActive(true);
			removeSilenceButton.SetActive(true);
		} else {
			if (currentAsrc.isPlaying) {
				PlayStopRecording();
			}
			playRecordingButton.SetActive(false);
			info.text = "Mic recording...";
			isRecording = true;
			RARE.Instance.StartMicRecording(599);
			micRecordButton.GetComponentInChildren<Text>().text = "Stop";
			exportRecordingButton.SetActive(false);
			nextDialogButton.SetActive(false);
			previousDialogButton.SetActive(false);
			trimButton.SetActive(false);
			removeSilenceButton.SetActive(false);
		}
	}
	public void NextDialog()
	{
		if (skipExistingWAVs.isOn)
		{
			bool advance = true;
			while (advance)
			{
				if (File.Exists(Application.dataPath + "/../Output/" + filenameLines[currentDialogNum++] + ".wav"))
				{
					if (currentDialogNum > textLines.Count - 1)
					{
						currentDialogNum = 0;
					}
					Debug.Log("File " + Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".wav EXISTS!");
				}
				else
				{
					if (currentDialogNum > textLines.Count - 1)
					{
						currentDialogNum = 0;
					}
					Debug.Log("File " + Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".wav DOES NOT EXIST!");
					advance = false;
				}
			}
		}
		else
		{
			currentDialogNum++;
		}
		if (currentDialogNum > textLines.Count - 1)
		{
			currentDialogNum = 0;
		}
		if (!File.Exists(Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".txt"))
		{

			string[] splitText = textLines[currentDialogNum].Split(langSeparator, StringSplitOptions.None);
			if (splitText != null)
			{
				if (splitText.Length > 0)
					GameDialogTextEng.text = splitText[0];
				if (splitText.Length > 1 && splitText[1] != null)
				{
					GameDialogTextEtc.text = splitText[1];
				}
				else
				{
					GameDialogTextEtc.text = "";
				}
			}
		}
		else
		{
			string[] splitText = File.ReadAllText(Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".txt").Split(langSeparator, StringSplitOptions.None);
			if (splitText != null)
			{
				if (splitText.Length > 0)
					GameDialogTextEng.text = splitText[0];
				if (splitText.Length > 1 && splitText[1] != null)
				{
					GameDialogTextEtc.text = splitText[1];
				}
				else
				{
					GameDialogTextEtc.text = "";
				}
			}
		}


		exportRecordingButton.SetActive(false);
		playRecordingButton.SetActive(false);
		removeSilenceButton.SetActive(false);

	}
	public void PreviousDialog()
{
	if (skipExistingWAVs.isOn)
	{
		bool advance = true;
		while (advance)
		{
			if (File.Exists(Application.dataPath + "/../Output/" + filenameLines[currentDialogNum--] + ".wav"))
			{

				if (currentDialogNum < 0)
				{
					currentDialogNum = filenameLines.Count - 1;
				}
				Debug.Log("File " + Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".wav EXISTS!");
			}
			else
			{
				if (currentDialogNum < 0)
				{
					currentDialogNum = filenameLines.Count - 1;
				}
				Debug.Log("File " + Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".wav DOES NOT EXIST!");
				advance = false;
			}
		}
	}
	else
	{
		currentDialogNum--;
	}
	if (currentDialogNum < 0)
	{
		currentDialogNum = filenameLines.Count - 1;
	}
		if (!File.Exists(Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".txt"))
		{
			string[] splitText = textLines[currentDialogNum].Split(langSeparator, StringSplitOptions.None);
			if (splitText != null)
			{
				if (splitText.Length != 0)
					GameDialogTextEng.text = splitText[0];
				if (splitText.Length > 0 && splitText[1] != null)
				{
					GameDialogTextEtc.text = splitText[1];
				}
				else
				{
					GameDialogTextEtc.text = "";
				}
			}
		}
		else
		{
			string[] splitText = File.ReadAllText(Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".txt").Split(langSeparator, StringSplitOptions.None);
			if (splitText != null)
			{
				if (splitText.Length != 0)
					GameDialogTextEng.text = splitText[0];
				if (splitText.Length > 0 && splitText[1] != null)
				{
					GameDialogTextEtc.text = splitText[1];
				}
				else
				{
					GameDialogTextEtc.text = "";
				}
			}
		}

	exportRecordingButton.SetActive(false);
	playRecordingButton.SetActive(false);
	removeSilenceButton.SetActive(false);
}
	public void ExportToWavFile(){
		string filename = CheckFileName("Export Clip");
		RARE.Instance.ExportClip (filenameLines[currentDialogNum], currentAsrc.clip, ClipLoaded, popUp);
		File.WriteAllText(Application.dataPath + "/../Output/" + filenameLines[currentDialogNum] + ".txt", GameDialogTextEng.text + "%J" + GameDialogTextEtc.text);
		recordNum++;
		isRecording = false;
		info.text = "Exported to : " + filename;
		GameDialogTextEng.text = textLines[currentDialogNum];
		playRecordingButton.SetActive(false);
		NextDialog();
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
