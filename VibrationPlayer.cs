using UnityEngine;
using Valve.VR;
using NAudio.Midi;
using System.IO;
using System.Linq;

public class VibrationPlayer : MonoBehaviour {
	public SteamVR_Action_Vibration vibration;
	/// <summary>
	/// midi文件的路径
	/// </summary>
	public string midiPath;
	/// <summary>
	/// 通过这个Track寻找TempoEvent，从而获取Tick的真实时间
	/// </summary>
	public int tempoTrackIndex;
	/// <summary>
	/// 左手手柄的音轨，设为-1不读取
	/// </summary>
	public int leftTrackIndex = -1;
	/// <summary>
	/// 左手的震动大小
	/// </summary>
	[Range(0, 1)]
	public float leftAmplitude = 0.1f;
	/// <summary>
	/// 右手手柄的音轨，设为-1不读取
	/// </summary>
	public int rightTrackIndex = -1;
	/// <summary>
	/// 左手的震动大小
	/// </summary>
	[Range(0, 1)]
	public float rightAmplitude = 0.1f;
	/// <summary>
	/// 当前左手播放到的index
	/// </summary>
	public int currentLeftNoteIndex;
	/// <summary>
	/// 当前左手播放到的index
	/// </summary>
	public int currentRightNoteIndex;

	private Note[] leftNotes, rightNotes;

	/// <summary>
	/// 音符频率对照表，index就是midi中的NoteNumber
	/// From http://subsynth.sourceforge.net/midinote2freq.html
	/// </summary>
	private static readonly float[] FrequencyTable = {
		8.1757989156f, 8.6619572180f, 9.1770239974f, 9.7227182413f, 10.3008611535f, 10.9133822323f, 
		11.5623257097f, 12.2498573744f, 12.9782717994f, 13.7500000000f, 14.5676175474f, 15.4338531643f, 
		16.3515978313f, 17.3239144361f, 18.3540479948f, 19.4454364826f, 20.6017223071f, 21.8267644646f, 
		23.1246514195f, 24.4997147489f, 25.9565435987f, 27.5000000000f, 29.1352350949f, 30.8677063285f, 
		32.7031956626f, 34.6478288721f, 36.7080959897f, 38.8908729653f, 41.2034446141f, 43.6535289291f, 
		46.2493028390f, 48.9994294977f, 51.9130871975f, 55.0000000000f, 58.2704701898f, 61.7354126570f, 
		65.4063913251f, 69.2956577442f, 73.4161919794f, 77.7817459305f, 82.4068892282f, 87.3070578583f, 
		92.4986056779f, 97.9988589954f, 103.8261743950f, 110.0000000000f, 116.5409403795f, 123.4708253140f, 
		130.8127826503f, 138.5913154884f, 146.8323839587f, 155.5634918610f, 164.8137784564f, 174.6141157165f, 
		184.9972113558f, 195.9977179909f, 207.6523487900f, 220.0000000000f, 233.0818807590f, 246.9416506281f, 
		261.6255653006f, 277.1826309769f, 293.6647679174f, 311.1269837221f, 329.6275569129f, 349.2282314330f, 
		369.9944227116f, 391.9954359817f, 415.3046975799f, 440.0000000000f, 466.1637615181f, 493.8833012561f, 
		523.2511306012f, 554.3652619537f, 587.3295358348f, 622.2539674442f, 659.2551138257f, 698.4564628660f, 
		739.9888454233f, 783.9908719635f, 830.6093951599f, 880.0000000000f, 932.3275230362f, 987.7666025122f, 
		1046.5022612024f, 1108.7305239075f, 1174.6590716696f, 1244.5079348883f, 1318.5102276515f, 1396.9129257320f, 
		1479.9776908465f, 1567.9817439270f, 1661.2187903198f, 1760.0000000000f, 1864.6550460724f, 1975.5332050245f, 
		2093.0045224048f, 2217.4610478150f, 2349.3181433393f, 2489.0158697766f, 2637.0204553030f, 2793.8258514640f, 
		2959.9553816931f, 3135.9634878540f, 3322.4375806396f, 3520.0000000000f, 3729.3100921447f, 3951.0664100490f,
		4186.0090448096f, 4434.9220956300f, 4698.6362866785f, 4978.0317395533f, 5274.0409106059f, 5587.6517029281f, 
		5919.9107633862f, 6271.9269757080f, 6644.8751612791f, 7040.0000000000f, 7458.6201842894f, 7902.1328200980f, 
		8372.0180896192f, 8869.8441912599f, 9397.2725733570f, 9956.0634791066f, 10548.0818212118f, 11175.3034058561f, 
		11839.8215267723f, 12543.8539514160f
	};

	private void Start() {
		if (!File.Exists(midiPath)) {
			Debug.LogError(midiPath + " dose not exist or access denied.");
			return;
		}
		var midi = new MidiFile(midiPath, true);
		Debug.Log("Track count: " + midi.Tracks);
		var tempoEvent = midi.Events.GetTrackEvents(tempoTrackIndex).FirstOrDefault(e => e is TempoEvent);
		if (tempoEvent == null) {
			Debug.LogError("Cannot determine note's realtime");
			return;
		}
		var timeRatio = ((TempoEvent)tempoEvent).MicrosecondsPerQuarterNote / (float)midi.DeltaTicksPerQuarterNote / 1000000f;

		Note[] ParseNotes(int track) {
			return midi.Events.GetTrackEvents(track).Where(e => {
				if (e is NoteOnEvent noe) {
					if (noe.NoteNumber < FrequencyTable.Length) {
						return true;
					}
					Debug.LogError($"Note {noe.NoteName} is too high to play!");
				}
				return false;
			}).Cast<NoteOnEvent>().Select(noe => new Note(timeRatio, noe)).ToArray();
		}

		if (leftTrackIndex != -1) {
			leftNotes = ParseNotes(leftTrackIndex);
		}
		if (rightTrackIndex != -1) {
			rightNotes = ParseNotes(rightTrackIndex);
		}
	}

	private void Update() {
		if (leftNotes != null && currentLeftNoteIndex < leftNotes.Length) {
			var note = leftNotes[currentLeftNoteIndex];
			var currentTime = Time.time;
			if (note.StartTime <= currentTime) {
				currentLeftNoteIndex++;
				while (currentLeftNoteIndex < leftNotes.Length && leftNotes[currentLeftNoteIndex].StartTime <= currentTime) {
					Debug.LogWarning("1 left note skipped");
					note = leftNotes[currentLeftNoteIndex];
					currentLeftNoteIndex++;
				}
				vibration.Execute(0, note.Duration, note.Frequency, leftAmplitude, SteamVR_Input_Sources.LeftHand);
			}
		}
		if (rightNotes != null && currentRightNoteIndex < rightNotes.Length) {
			var note = rightNotes[currentRightNoteIndex];
			var currentTime = Time.time;
			if (note.StartTime > currentTime) {
				return;
			}
			currentRightNoteIndex++;
			while (currentRightNoteIndex < rightNotes.Length && rightNotes[currentRightNoteIndex].StartTime <= currentTime) {
				Debug.LogWarning("1 left note skipped");
				note = rightNotes[currentRightNoteIndex];
				currentRightNoteIndex++;
			}
			vibration.Execute(0, note.Duration, note.Frequency, rightAmplitude, SteamVR_Input_Sources.RightHand);
		}
	}

	private readonly struct Note {
		public float Frequency { get; }

		public float StartTime { get; }

		public float Duration { get; }

		public Note(float timeRatio, NoteOnEvent noe) {
			Frequency = FrequencyTable[noe.NoteNumber];
			if (Frequency < 320f || Frequency > 4000f) {
				Debug.LogWarning($"Note {noe.NoteName}'s frequency {Frequency} is too low or too high, may be distorted.");
			}
			StartTime = noe.AbsoluteTime * timeRatio;
			Duration = (noe.OffEvent.AbsoluteTime - noe.AbsoluteTime) * timeRatio;
		}

		public override string ToString() {
			return $"{StartTime}-{StartTime + Duration}, {Frequency}Hz";
		}
	}
}
